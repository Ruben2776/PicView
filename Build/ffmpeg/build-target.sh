#!/usr/bin/env bash
# Builds the statically-linked picview-ffmpeg native library for one target.
# Invoked by Build-FFmpegNative.ps1; can also run standalone in MSYS2:
#   PV_ROOT=<work dir> PV_SRC=<ffmpeg source> PV_SHIM=<picview_ffmpeg.c> \
#   PV_OUT=<output dir> build-target.sh <target>
#
# Targets: win-arm64 | linux-x64 | linux-arm64 | osx-x64 | osx-arm64
# (win-x64 uses the same flow; it is just built with the native MINGW64 gcc.)
set -e

: "${PV_ROOT:?PV_ROOT (scratch dir) must be set}"
: "${PV_SRC:?PV_SRC (ffmpeg source dir) must be set}"
: "${PV_SHIM:?PV_SHIM (picview_ffmpeg.c path) must be set}"
: "${PV_OUT:?PV_OUT (ffmpeg-native output dir) must be set}"
MACHONM=${MACHONM:-$PV_ROOT/machonm.exe}

# Toolchain directories outside the MSYS2/standard PATH (e.g. zig): the hosting
# script sets PV_EXTRA_PATH. Scoop's zig shim and /usr/bin (nasm, make) must be
# reachable from MSYS2.
export PATH=/usr/bin${PV_EXTRA_PATH:+:$PV_EXTRA_PATH}:/c/Users/$USER/scoop/shims:$PATH

TARGET=$1
BUILD=$PV_ROOT/build-$TARGET

case $TARGET in
  win-x64)
    CONFIGURE_FLAGS=(--target-os=mingw64 --arch=x86_64 --x86asmexe=nasm)
    LINK_CMD=(gcc -shared)
    LINK_EXTRA=(/mingw64/lib/libwinpthread.a -lbcrypt -lm -static-libgcc)
    OUTLIB=picview-ffmpeg.dll
    ;;
  win-arm64)
    CONFIGURE_FLAGS=(--target-os=mingw64 --arch=aarch64 --enable-cross-compile --disable-x86asm "--cc=zig cc" "--ld=zig cc --target=aarch64-windows-gnu" "--extra-cflags=--target=aarch64-windows-gnu")
    LINK_CMD=(zig cc --target=aarch64-windows-gnu -shared)
    LINK_EXTRA=(-lbcrypt -lm)
    OUTLIB=picview-ffmpeg.dll
    ;;
  linux-x64)
    CONFIGURE_FLAGS=(--target-os=linux --arch=x86_64 --enable-cross-compile --enable-pic "--cc=zig cc" "--ld=zig cc --target=x86_64-linux-gnu.2.31" "--extra-cflags=--target=x86_64-linux-gnu.2.31" --x86asmexe=nasm)
    LINK_CMD=(zig cc --target=x86_64-linux-gnu.2.31 -shared)
    # -Bsymbolic: ffmpeg's x86 asm uses PC32 fixups that would otherwise be
    # rejected for preemptible symbols in a shared library.
    LINK_EXTRA=(-lm -lpthread -Wl,-Bsymbolic -Wl,-s -Wl,--version-script=exports.map)
    OUTLIB=libpicviewffmpeg.so
    ;;
  linux-arm64)
    CONFIGURE_FLAGS=(--target-os=linux --arch=aarch64 --enable-cross-compile --enable-pic --disable-x86asm "--cc=zig cc" "--ld=zig cc --target=aarch64-linux-gnu.2.31" "--extra-cflags=--target=aarch64-linux-gnu.2.31")
    LINK_CMD=(zig cc --target=aarch64-linux-gnu.2.31 -shared)
    LINK_EXTRA=(-lm -lpthread -Wl,-s -Wl,--version-script=exports.map)
    OUTLIB=libpicviewffmpeg.so
    ;;
  osx-x64)
    CONFIGURE_FLAGS=(--target-os=darwin --arch=x86_64 --enable-cross-compile --enable-pic "--ar=zig ar" "--ranlib=zig ranlib" "--nm=$MACHONM" "--cc=zig cc" "--ld=zig cc --target=x86_64-macos.11.0" "--extra-cflags=--target=x86_64-macos.11.0" --x86asmexe=nasm)
    if [ "$(uname)" = "Darwin" ]; then
      # Apple hosts link natively (zig cc rejects -exported_symbols_list)
      LINK_CMD=(cc -arch x86_64 -dynamiclib)
      LINK_EXTRA=(-lm -lpthread "-Wl,-install_name,@rpath/libpicviewffmpeg.dylib" -Wl,-exported_symbols_list,exports.lst)
    else
      LINK_CMD=(zig cc --target=x86_64-macos.11.0 -shared)
      LINK_EXTRA=(-lm -lpthread -Wl,-s "-Wl,-install_name,@rpath/libpicviewffmpeg.dylib" -Wl,-exported_symbols_list,exports.lst)
    fi
    OUTLIB=libpicviewffmpeg.dylib
    ;;
  osx-arm64)
    CONFIGURE_FLAGS=(--target-os=darwin --arch=aarch64 --enable-cross-compile --enable-pic "--ar=zig ar" "--ranlib=zig ranlib" "--nm=$MACHONM" --disable-x86asm "--cc=zig cc" "--ld=zig cc --target=aarch64-macos.11.0" "--extra-cflags=--target=aarch64-macos.11.0")
    if [ "$(uname)" = "Darwin" ]; then
      # Apple hosts link natively (zig cc rejects -exported_symbols_list)
      LINK_CMD=(cc -arch arm64 -dynamiclib)
      LINK_EXTRA=(-lm -lpthread "-Wl,-install_name,@rpath/libpicviewffmpeg.dylib" -Wl,-exported_symbols_list,exports.lst)
    else
      LINK_CMD=(zig cc --target=aarch64-macos.11.0 -shared)
      LINK_EXTRA=(-lm -lpthread -Wl,-s "-Wl,-install_name,@rpath/libpicviewffmpeg.dylib" -Wl,-exported_symbols_list,exports.lst)
    fi
    OUTLIB=libpicviewffmpeg.dylib
    ;;
  *)
    echo "unknown target $TARGET"; exit 1
    ;;
esac

# zig's bundled glibc has no sys/sysctl.h; ffmpeg's check_func sysctl only tests
# linking, which would set HAVE_SYSCTL=1 and break the build of libavutil/cpu.c.
# (sed -i without a backup suffix is GNU-only; use the portable form.)
case $TARGET in
  linux-*)
    sed 's/^check_func  sysctl$/:/' "$PV_SRC/configure" > "$PV_SRC/configure.tmp" && mv "$PV_SRC/configure.tmp" "$PV_SRC/configure"
    ;;
  osx-*)
    # zig cc rejects -Wl,-dynamic (a ld64 default); the flag breaks every link test
    sed 's/add_ldflags -Wl,-dynamic,-search_paths_first/:/' "$PV_SRC/configure" > "$PV_SRC/configure.tmp" && mv "$PV_SRC/configure.tmp" "$PV_SRC/configure"
    ;;
esac

mkdir -p "$BUILD"
cd "$BUILD"

# Export only the shim ABI; everything statically linked stays internal.
cat > exports.map <<'EOF'
{
  global: pv_*;
  local: *;
};
EOF
cat > exports.lst <<'EOF'
_pv_open
_pv_decode_next
_pv_rewind
_pv_close
_pv_version
EOF

# dav1d decodes the AV1 of animated AVIF images. It is optional: without its
# source (PV_DAV1D_SRC), or when it fails to build for this target, the library
# is built without it and animated AVIFs are decoded by ImageMagick instead.
# Cross targets: zig target triple, then meson's name for the system and the cpu.
case $TARGET in
  win-x64)     MESON_CROSS=() ;;
  win-arm64)   MESON_CROSS=(aarch64-windows-gnu windows aarch64) ;;
  linux-x64)   MESON_CROSS=(x86_64-linux-gnu.2.31 linux x86_64) ;;
  linux-arm64) MESON_CROSS=(aarch64-linux-gnu.2.31 linux aarch64) ;;
  osx-x64)     MESON_CROSS=(x86_64-macos.11.0 darwin x86_64) ;;
  osx-arm64)   MESON_CROSS=(aarch64-macos.11.0 darwin aarch64) ;;
esac
DAV1D_PREFIX=$PV_ROOT/dav1d-install-$TARGET
DAV1D_FLAGS=()
DAV1D_LIBS=()
build_dav1d() {
  local build=$PV_ROOT/dav1d-build-$TARGET
  local setup=(--buildtype=release --default-library=static --libdir=lib
    -Denable_tools=false -Denable_tests=false -Denable_examples=false -Denable_docs=false)
  rm -rf "$build" "$DAV1D_PREFIX"
  if [ ${#MESON_CROSS[@]} -gt 0 ]; then
    # zig cc is clang underneath; meson only needs to be told what it targets
    local cross=$PV_ROOT/dav1d-cross-$TARGET.ini
    cat > "$cross" <<EOF
[binaries]
c = ['zig', 'cc', '--target=${MESON_CROSS[0]}']
ar = ['zig', 'ar']
ranlib = ['zig', 'ranlib']
strip = ['zig', 'objcopy', '--strip-all']
nasm = 'nasm'

[host_machine]
system = '${MESON_CROSS[1]}'
cpu_family = '${MESON_CROSS[2]}'
cpu = '${MESON_CROSS[2]}'
endian = 'little'
EOF
    setup+=(--cross-file "$cross")
  fi
  meson setup "$build" "$PV_DAV1D_SRC" --prefix="$DAV1D_PREFIX" "${setup[@]}" > "$PV_ROOT/dav1d-$TARGET.log" 2>&1 &&
    ninja -C "$build" install >> "$PV_ROOT/dav1d-$TARGET.log" 2>&1
}

if [ -n "$PV_DAV1D_SRC" ] && command -v meson > /dev/null && command -v ninja > /dev/null; then
  echo "=== dav1d ($TARGET) ==="
  if build_dav1d; then
    export PKG_CONFIG_PATH=$DAV1D_PREFIX/lib/pkgconfig
    DAV1D_FLAGS=(--enable-libdav1d --enable-decoder=libdav1d --pkg-config=pkg-config --pkg-config-flags=--static)
    # The archive by its path, so nothing depends on how the linker searches for it
    DAV1D_LIBS=("$DAV1D_PREFIX/lib/libdav1d.a")
    # dav1d looks a glibc symbol up at run time
    case $TARGET in linux-*) DAV1D_LIBS+=(-ldl) ;; esac
  else
    echo "WARNING: dav1d failed to build for $TARGET; building without AV1 (animated AVIF falls back to ImageMagick):"
    tail -15 "$PV_ROOT/dav1d-$TARGET.log"
  fi
else
  echo "WARNING: dav1d source, meson or ninja missing; building $TARGET without AV1"
fi

configure_ffmpeg() {
  # Invoked through sh explicitly: some extraction paths drop the exec bit
  sh "$PV_SRC/configure" \
    --prefix="$PV_ROOT/install-$TARGET" \
    "${CONFIGURE_FLAGS[@]}" \
    --enable-static --disable-shared \
    --disable-programs --disable-doc --disable-debug \
    --disable-everything \
    --enable-demuxer=mov,mp4 \
    --enable-decoder=h264,hevc \
    --enable-parser=h264,hevc \
    --enable-swscale \
    --disable-autodetect --disable-network \
    --disable-avdevice --disable-avfilter --disable-swresample \
    "$@" \
    --extra-cflags="-O2" > configure.log 2>&1
}

if [ ${#DAV1D_FLAGS[@]} -gt 0 ] && ! configure_ffmpeg "${DAV1D_FLAGS[@]}"; then
  echo "WARNING: FFmpeg does not configure with dav1d for $TARGET; building without AV1:"
  tail -15 configure.log
  DAV1D_FLAGS=()
  DAV1D_LIBS=()
fi
if [ ${#DAV1D_FLAGS[@]} -eq 0 ]; then
  configure_ffmpeg || { echo "CONFIGURE FAILED ($TARGET):"; tail -25 configure.log; exit 1; }
fi

# nproc is GNU-only; macOS reports the CPU count via sysctl
JOBS=$(nproc 2>/dev/null || sysctl -n hw.ncpu)
make -j"$JOBS" > make.log 2>&1 || { echo "MAKE FAILED ($TARGET):"; tail -25 make.log; exit 1; }

echo "=== link shim ($TARGET) ==="
# Link the objects straight from the build tree instead of the static archives:
# Mach-O archives collide on duplicate basenames (cabac.o vs hevc/cabac.o) and
# archive symbol indexes are unreliable under cross toolchains. ops_asmgen.o is
# a host-side code generator, not target code.
find libavcodec libavformat libswscale libavutil -name '*.o' ! -name 'ops_asmgen.o' > objects.txt
# A library left by an earlier build must not pass for the result of this one
rm -f $OUTLIB
"${LINK_CMD[@]}" -O2 -fvisibility=hidden -o $OUTLIB "$PV_SHIM" -I "$PV_SRC" -I "$BUILD" \
  @objects.txt "${DAV1D_LIBS[@]}" "${LINK_EXTRA[@]}" > link.log 2>&1 || { echo "LINK FAILED ($TARGET):"; head -30 link.log; exit 1; }
head -30 link.log
ls -la $OUTLIB

mkdir -p "$PV_OUT/$TARGET"
cp $OUTLIB "$PV_OUT/$TARGET/"
echo "=== done: $TARGET ==="
