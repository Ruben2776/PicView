/*
 * picview_ffmpeg.c
 *
 * Minimal statically-linked FFmpeg wrapper for motion photo video playback.
 * It exposes a tiny, stable C ABI so the managed side never touches FFmpeg
 * structs or version-dependent layouts:
 *
 *   pv_open         open a media stream via caller-supplied read/seek callbacks
 *   pv_decode_next  decode the next video frame, scaled to BGRA32
 *   pv_close        release everything
 *
 * Video-only by design: no audio decoding/output is built into this library
 * (the bundled FFmpeg configuration disables everything except the mov/mp4
 * demuxer, the h264/hevc decoders and libswscale).
 *
 * Threading: a session is driven by exactly one thread at a time.
 */

#include <libavformat/avformat.h>
#include <libavcodec/avcodec.h>
#include <libswscale/swscale.h>
#include <libavutil/display.h>
#include <libavutil/imgutils.h>
#include <libavutil/pixfmt.h>
#include <math.h>
#include <stdint.h>
#include <string.h>

#ifdef PV_DEBUG
#include <stdio.h>
#define PV_DBG(...) fprintf(stderr, "[pv] " __VA_ARGS__)
#else
#define PV_DBG(...) ((void)0)
#endif

#if defined(_WIN32)
#define PV_API __declspec(dllexport)
#else
#define PV_API __attribute__((visibility("default")))
#endif

#define PV_AVIO_BUFFER_SIZE (64 * 1024)

/* Returns bytes read (>0), 0 on EOF, negative on error. */
typedef int (*PvReadCb)(void *opaque, uint8_t *buf, int size);

/* Seeks the stream. whence is SEEK_SET/SEEK_CUR/SEEK_END, or AVSEEK_SIZE
 * (0x10000) which must return the total stream length. Negative on error. */
typedef int64_t (*PvSeekCb)(void *opaque, int64_t offset, int whence);

typedef struct PvVideoInfo
{
    int width;
    int height;
    double fps;
    double duration_sec;
} PvVideoInfo;

typedef struct PvSession
{
    AVFormatContext *fmt;
    AVCodecContext *dec;
    AVIOContext *avio;
    AVPacket *pkt;
    AVFrame *frame;
    struct SwsContext *sws;
    void *user_opaque;
    PvReadCb read_cb;
    PvSeekCb seek_cb;
    int video_index;
    int width;
    int height;
    int src_w;
    int src_h;
    enum AVPixelFormat src_fmt;
    int rotation;
    uint8_t *scratch;
    int scratch_capacity;
    double time_base;
    int64_t start_ts;
    int frame_count;
    int eof;
    int flushed;
} PvSession;

static int pv_read_trampoline(void *opaque, uint8_t *buf, int size)
{
    PvSession *s = (PvSession *)opaque;
    int r = s->read_cb(s->user_opaque, buf, size);
    /* FFmpeg's AVIO contract requires AVERROR_EOF (negative) at end of stream;
     * a zero return would be treated as "zero bytes of data" and can spin the
     * format probing loop forever. */
    return r == 0 ? AVERROR_EOF : r;
}

/* FFmpeg may OR internal flags (AVSEEK_FORCE) into whence; normalize before
 * handing the request to the caller so it only sees SEEK_SET/CUR/END or
 * AVSEEK_SIZE. */
static int64_t pv_seek_trampoline(void *opaque, int64_t offset, int whence)
{
    PvSession *s = (PvSession *)opaque;
    if (whence == AVSEEK_SIZE)
    {
        return s->seek_cb(s->user_opaque, offset, AVSEEK_SIZE);
    }
    whence &= ~AVSEEK_FORCE;
    return s->seek_cb(s->user_opaque, offset, whence);
}

static void pv_log_silence(void *avcl, int level, const char *fmt, va_list vl)
{
    (void)avcl;
    (void)level;
    (void)fmt;
    (void)vl;
}

static int pv_read_display_rotation(const AVStream *stream);
static void pv_rotate_bgra(const uint8_t *src, int w, int h, uint8_t *dst, int rotation);

PV_API void pv_close(PvSession *s);

PV_API const char *pv_version(void)
{
    return av_version_info();
}

PV_API PvSession *pv_open(void *opaque, PvReadCb read_cb, PvSeekCb seek_cb, PvVideoInfo *out_info)
{
    if (read_cb == NULL || seek_cb == NULL || out_info == NULL)
    {
        return NULL;
    }

    av_log_set_callback(pv_log_silence);

    PvSession *s = (PvSession *)av_mallocz(sizeof(PvSession));
    if (s == NULL)
    {
        return NULL;
    }
    s->video_index = -1;
    s->start_ts = AV_NOPTS_VALUE;
    s->user_opaque = opaque;
    s->read_cb = read_cb;
    s->seek_cb = seek_cb;
    int r;

    uint8_t *avio_buffer = (uint8_t *)av_malloc(PV_AVIO_BUFFER_SIZE);
    if (avio_buffer == NULL)
    {
        goto fail;
    }

    /* Ownership of avio_buffer passes to the AVIOContext; ffmpeg may replace
     * it and avio_context_free releases whichever buffer is current. */
    s->avio = avio_alloc_context(avio_buffer, PV_AVIO_BUFFER_SIZE, 0, s,
                                 pv_read_trampoline, NULL, pv_seek_trampoline);
    if (s->avio == NULL)
    {
        av_free(avio_buffer);
        goto fail;
    }

    s->fmt = avformat_alloc_context();
    if (s->fmt == NULL)
    {
        goto fail;
    }
    s->fmt->pb = s->avio;

    r = avformat_open_input(&s->fmt, NULL, NULL, NULL);
    PV_DBG("avformat_open_input -> %d\n", r);
    if (r < 0)
    {
        PV_DBG("avformat_open_input -> %d (%s)\n", r, av_err2str(r));
        s->fmt = NULL; /* freed by avformat_open_input on failure */
        goto fail;
    }

    r = avformat_find_stream_info(s->fmt, NULL);
    PV_DBG("avformat_find_stream_info -> %d\n", r);
    if (r < 0)
    {
        PV_DBG("avformat_find_stream_info -> %d (%s)\n", r, av_err2str(r));
        goto fail;
    }

    const AVCodec *decoder = NULL;
    int stream_index = av_find_best_stream(s->fmt, AVMEDIA_TYPE_VIDEO, -1, -1, &decoder, 0);
    if (stream_index < 0 || decoder == NULL)
    {
        PV_DBG("av_find_best_stream -> %d\n", stream_index);
        goto fail;
    }

    AVStream *stream = s->fmt->streams[stream_index];
    s->video_index = stream_index;

    s->dec = avcodec_alloc_context3(decoder);
    if (s->dec == NULL)
    {
        goto fail;
    }
    r = avcodec_parameters_to_context(s->dec, stream->codecpar);
    if (r < 0)
    {
        PV_DBG("avcodec_parameters_to_context -> %d (%s)\n", r, av_err2str(r));
        goto fail;
    }
    PV_DBG("codec=%s extradata_size=%d w=%d h=%d\n", decoder->name,
           s->dec->extradata_size, s->dec->width, s->dec->height);
    r = avcodec_open2(s->dec, decoder, NULL);
    if (r < 0)
    {
        PV_DBG("avcodec_open2 -> %d (%s)\n", r, av_err2str(r));
        goto fail;
    }

    s->pkt = av_packet_alloc();
    s->frame = av_frame_alloc();
    if (s->pkt == NULL || s->frame == NULL)
    {
        goto fail;
    }

    s->width = s->dec->width;
    s->height = s->dec->height;
    s->src_fmt = (enum AVPixelFormat)AV_PIX_FMT_NONE;
    s->rotation = pv_read_display_rotation(stream);
    s->time_base = av_q2d(stream->time_base);
    s->start_ts = stream->start_time != AV_NOPTS_VALUE ? stream->start_time : 0;

    out_info->width = s->width;
    out_info->height = s->height;

    AVRational rate = av_guess_frame_rate(s->fmt, stream, NULL);
    out_info->fps = (rate.den > 0 && rate.num > 0) ? (double)rate.num / rate.den : 30.0;

    if (stream->duration > 0 && s->time_base > 0)
    {
        out_info->duration_sec = stream->duration * s->time_base;
    }
    else if (s->fmt->duration > 0)
    {
        out_info->duration_sec = (double)s->fmt->duration / AV_TIME_BASE;
    }
    else
    {
        out_info->duration_sec = 0;
    }

    return s;

fail:
    pv_close(s);
    return NULL;
}

/*
 * Prepares the scaler for the given frame. Output dimensions are the frame's own
 * dimensions, except when the frame does not fit the caller's buffer (declared at
 * open time), in which case it is scaled to the declared size. Container metadata
 * dimensions occasionally disagree with the decoded dimensions (phone videos with
 * conformance-window cropping), so every frame reports its actual output size.
 */
/*
 * Reads the container's display rotation (displaymatrix side data, written by
 * phone cameras for portrait recordings). av_display_rotation_get() reports how
 * the transformation rotates the frame; to display the frame upright the decoded
 * pixels must be rotated by the negated angle. Returns that counterclockwise
 * angle normalized to 0/90/180/270.
 */
static int pv_read_display_rotation(const AVStream *stream)
{
    const AVPacketSideData *sd = av_packet_side_data_get(
        stream->codecpar->coded_side_data, stream->codecpar->nb_coded_side_data,
        AV_PKT_DATA_DISPLAYMATRIX);
    if (sd == NULL || sd->data == NULL)
    {
        return 0;
    }

    double theta = av_display_rotation_get((const int32_t *)sd->data);
    if (isnan(theta))
    {
        return 0;
    }

    int rotation = -(int)llround(theta);
    rotation %= 360;
    if (rotation < 0)
    {
        rotation += 360;
    }

    return rotation;
}

/*
 * Rotates a packed BGRA frame counterclockwise by 90/180/270 degrees into dst
 * (which must hold the rotated dimensions).
 */
static void pv_rotate_bgra(const uint8_t *src, int w, int h, uint8_t *dst, int rotation)
{
    const uint32_t *s = (const uint32_t *)src;
    uint32_t *d = (uint32_t *)dst;

    switch (rotation)
    {
    case 90:
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                d[(size_t)(w - 1 - x) * h + y] = s[(size_t)y * w + x];
            }
        }
        break;
    case 180:
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                d[(size_t)(h - 1 - y) * w + (w - 1 - x)] = s[(size_t)y * w + x];
            }
        }
        break;
    case 270:
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                d[(size_t)x * h + (h - 1 - y)] = s[(size_t)y * w + x];
            }
        }
        break;
    default:
        break;
    }
}

static int pv_ensure_sws(PvSession *s, const AVFrame *frame, int *out_w, int *out_h)
{
    enum AVPixelFormat fmt = (enum AVPixelFormat)frame->format;
    int dst_w = frame->width;
    int dst_h = frame->height;
    if ((int64_t)dst_w * dst_h * 4 > (int64_t)s->width * s->height * 4)
    {
        dst_w = s->width;
        dst_h = s->height;
    }

    *out_w = dst_w;
    *out_h = dst_h;

    if (s->sws != NULL && fmt == s->src_fmt &&
        frame->width == s->src_w && frame->height == s->src_h)
    {
        return 0;
    }

    if (s->sws != NULL)
    {
        sws_freeContext(s->sws);
        s->sws = NULL;
    }

    s->sws = sws_getContext(frame->width, frame->height, fmt,
                            dst_w, dst_h, AV_PIX_FMT_BGRA,
                            SWS_BILINEAR, NULL, NULL, NULL);
    if (s->sws == NULL)
    {
        return -1;
    }

    s->src_fmt = fmt;
    s->src_w = frame->width;
    s->src_h = frame->height;
    return 0;
}

/* Returns the presentation time of the frame in seconds relative to stream start;
 * falls back to a constant-fps estimate when the container carries no timestamps. */
static double pv_frame_pts(PvSession *s, const AVFrame *frame, double fps)
{
    int64_t ts = frame->best_effort_timestamp != AV_NOPTS_VALUE
                     ? frame->best_effort_timestamp
                     : frame->pts;
    if (ts != AV_NOPTS_VALUE && s->time_base > 0)
    {
        double t = (double)(ts - s->start_ts) * s->time_base;
        if (t >= 0)
        {
            return t;
        }
    }

    return fps > 0 ? (double)s->frame_count / fps : 0;
}

PV_API int pv_decode_next(PvSession *s, uint8_t *dst, int dst_capacity, double *out_pts,
                          int *out_width, int *out_height)
{
    if (s == NULL || dst == NULL || dst_capacity < s->width * s->height * 4 ||
        out_pts == NULL || out_width == NULL || out_height == NULL)
    {
        return -1;
    }

    for (;;)
    {
        int r = avcodec_receive_frame(s->dec, s->frame);
        if (r < 0 && r != AVERROR(EAGAIN) && r != AVERROR_EOF)
        {
            PV_DBG("avcodec_receive_frame -> %d (%s)\n", r, av_err2str(r));
        }
        if (r == 0)
        {
            int dst_w, dst_h;
            if (pv_ensure_sws(s, s->frame, &dst_w, &dst_h) < 0)
            {
                av_frame_unref(s->frame);
                return -1;
            }

            int out_w = dst_w;
            int out_h = dst_h;
            if (s->rotation == 90 || s->rotation == 270)
            {
                out_w = dst_h;
                out_h = dst_w;
            }

            if (s->rotation != 0)
            {
                /* Rotate after conversion, via the scratch buffer. */
                int needed = dst_w * dst_h * 4;
                if (s->scratch_capacity < needed)
                {
                    av_free(s->scratch);
                    s->scratch = (uint8_t *)av_malloc(needed);
                    s->scratch_capacity = s->scratch != NULL ? needed : 0;
                }

                if (s->scratch == NULL)
                {
                    av_frame_unref(s->frame);
                    return -1;
                }

                uint8_t *scratch_data[1] = {s->scratch};
                int scratch_linesize[1] = {dst_w * 4};
                sws_scale(s->sws, (const uint8_t *const *)s->frame->data, s->frame->linesize,
                          0, dst_h, scratch_data, scratch_linesize);
                pv_rotate_bgra(s->scratch, dst_w, dst_h, dst, s->rotation);
            }
            else
            {
                uint8_t *dst_data[1] = {dst};
                int dst_linesize[1] = {dst_w * 4};
                sws_scale(s->sws, (const uint8_t *const *)s->frame->data, s->frame->linesize,
                          0, dst_h, dst_data, dst_linesize);
            }

            double fps = s->time_base > 0 ? 0 : 30.0;
            *out_pts = pv_frame_pts(s, s->frame, fps);
            *out_width = out_w;
            *out_height = out_h;
            s->frame_count++;
            av_frame_unref(s->frame);
            return out_w * out_h * 4;
        }

        if (r != AVERROR(EAGAIN))
        {
            /* AVERROR_EOF: the decoder is fully drained. */
            return 0;
        }

        /* The decoder needs more input. */
        r = av_read_frame(s->fmt, s->pkt);
        if (r < 0)
        {
            if (!s->flushed)
            {
                s->flushed = 1;
                s->eof = 1;
                avcodec_send_packet(s->dec, NULL);
                continue;
            }
            return 0;
        }

        if (s->pkt->stream_index != s->video_index)
        {
            av_packet_unref(s->pkt);
            continue;
        }

        r = avcodec_send_packet(s->dec, s->pkt);
        if (r < 0)
        {
            PV_DBG("avcodec_send_packet -> %d (%s) size=%d head=%02x%02x%02x%02x%02x\n",
                   r, av_err2str(r), s->pkt->size,
                   s->pkt->data[0], s->pkt->data[1], s->pkt->data[2],
                   s->pkt->data[3], s->pkt->data[4]);
        }
        av_packet_unref(s->pkt);
        if (r < 0 && r != AVERROR(EAGAIN))
        {
            return -1;
        }
    }
}

PV_API void pv_close(PvSession *s)
{
    if (s == NULL)
    {
        return;
    }

    if (s->sws != NULL)
    {
        sws_freeContext(s->sws);
    }
    av_free(s->scratch);
    if (s->frame != NULL)
    {
        av_frame_free(&s->frame);
    }
    if (s->pkt != NULL)
    {
        av_packet_free(&s->pkt);
    }
    if (s->dec != NULL)
    {
        avcodec_free_context(&s->dec);
    }
    if (s->fmt != NULL)
    {
        /* Does not free the externally supplied AVIOContext. */
        avformat_close_input(&s->fmt);
    }
    if (s->avio != NULL)
    {
        avio_context_free(&s->avio);
    }

    av_free(s);
}
