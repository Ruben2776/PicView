/*
 * machonm.c - minimal nm replacement that understands Mach-O 64-bit objects.
 * Prints one line per symbol in the classic nm format:
 *   <value hex> <type letter> <name>
 * Used by ffmpeg's configure to detect the external symbol prefix on Apple
 * targets when cross-compiling from Windows (GNU nm cannot read Mach-O).
 */
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

struct mach_header_64 { uint32_t magic, cputype, cpusubtype, filetype; uint32_t ncmds, sizeofcmds, flags, reserved; };
struct load_command { uint32_t cmd, cmdsize; };
struct symtab_command { uint32_t cmd, cmdsize, symoff, nsyms, stroff, strsize; };
struct nlist_64 { uint32_t n_strx; uint8_t n_type, n_sect; uint16_t n_desc; uint64_t n_value; };

#define N_EXT 0x01
#define N_TYPE_MASK 0x0e
#define N_UNDF 0x00
#define N_ABS 0x02
#define N_SECT 0x0e

int main(int argc, char **argv)
{
    const char *path = NULL;
    for (int i = 1; i < argc; i++)
    {
        if (argv[i][0] != '-')
        {
            path = argv[i];
        }
    }
    if (!path)
    {
        return 1;
    }

    FILE *f = fopen(path, "rb");
    if (!f)
    {
        fprintf(stderr, "machonm: cannot open %s\n", path);
        return 1;
    }
    fseek(f, 0, SEEK_END);
    long size = ftell(f);
    fseek(f, 0, SEEK_SET);
    uint8_t *buf = (uint8_t *)malloc(size);
    if (fread(buf, 1, size, f) != (size_t)size)
    {
        fclose(f);
        return 1;
    }
    fclose(f);

    struct mach_header_64 *h = (struct mach_header_64 *)buf;
    if (h->magic != 0xfeedfacf)
    {
        fprintf(stderr, "machonm: %s is not a Mach-O 64 object\n", path);
        return 1;
    }

    uint8_t *p = buf + sizeof(*h);
    for (uint32_t i = 0; i < h->ncmds; i++)
    {
        struct load_command *lc = (struct load_command *)p;
        if (lc->cmd == 2 /* LC_SYMTAB */)
        {
            struct symtab_command *st = (struct symtab_command *)p;
            struct nlist_64 *syms = (struct nlist_64 *)(buf + st->symoff);
            const char *strs = (const char *)(buf + st->stroff);
            for (uint32_t s = 0; s < st->nsyms; s++)
            {
                uint8_t t = syms[s].n_type;
                char letter;
                if ((t & N_TYPE_MASK) == N_UNDF)
                {
                    letter = (t & N_EXT) ? 'U' : 'u';
                }
                else if ((t & N_TYPE_MASK) == N_ABS)
                {
                    letter = (t & N_EXT) ? 'A' : 'a';
                }
                else if ((t & N_TYPE_MASK) == N_SECT)
                {
                    letter = (t & N_EXT) ? 'T' : 't';
                }
                else
                {
                    letter = '?';
                }

                printf("%016llx %c %s\n", (unsigned long long)syms[s].n_value,
                       letter, strs + syms[s].n_strx);
            }
            free(buf);
            return 0;
        }
        p += lc->cmdsize;
    }

    free(buf);
    return 0;
}
