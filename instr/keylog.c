/* keylog.c — 見 keylog.h。
 *
 * 每幀（frame_num = JE_showVGA 絕對呼叫次數）記錄：
 *   H: keysactive 為 true 的 scancode 清單（按住狀態）
 *   Q: 該幀緩衝到的 KeyDown 事件（sym:scancode:mod，含 SDL 鍵盤重複）
 * 只有「H 非空 或 Q 非空」的幀才寫 keylog.txt + 存截圖（無輸入的幀只前進 frame_num）。
 *
 * log 格式（每行一幀）：
 *   <frame>\tH:<sc,sc,...>\tQ:<sym:sc:mod,sym:sc:mod,...>
 *
 * 啟用：KEYLOG=1。KEYLOG_DIR=輸出夾(預設 keylog)。KEYLOG_NOSHOT=1 不截圖。
 *       KEYLOG_FORCE=1 每幀都捕捉（測試/定頻用）。
 */
#include "keylog.h"
#include "keyboard.h"   /* keysactive[] */
#include "video.h"      /* VGAScreen, main_window_tex_format */
#include "palette.h"    /* rgb_palette[] */

#include <SDL.h>
#include <stdio.h>
#include <string.h>

#ifdef _WIN32
#include <direct.h>
#define KL_MKDIR(d) _mkdir(d)
#else
#include <sys/stat.h>
#define KL_MKDIR(d) mkdir(d, 0755)
#endif

static unsigned long frame_num = 0;
static int enabled = -1;   /* -1=尚未判定, 0=停用, 1=啟用 */
static int shots = 1;
static int force = 0;
static int initialized = 0;
static FILE *kl_logf = NULL;
static char outdir[256] = "keylog";

/* 當幀 KeyDown 事件緩衝（含 repeat） */
#define KL_MAXQ 64
static struct { int sym, scancode, mod; } qbuf[KL_MAXQ];
static int qcount = 0;

static void check_enabled(void)
{
	if (enabled >= 0)
		return;
	const char *e = SDL_getenv("KEYLOG");
	enabled = (e && *e && *e != '0') ? 1 : 0;
	const char *ns = SDL_getenv("KEYLOG_NOSHOT");
	shots = (ns && *ns && *ns != '0') ? 0 : 1;
	const char *fc = SDL_getenv("KEYLOG_FORCE");
	force = (fc && *fc && *fc != '0') ? 1 : 0;
}

static void ensure_init(void)
{
	if (initialized)
		return;
	initialized = 1;

	const char *d = SDL_getenv("KEYLOG_DIR");
	if (d && *d)
	{
		strncpy(outdir, d, sizeof(outdir) - 1);
		outdir[sizeof(outdir) - 1] = '\0';
	}
	KL_MKDIR(outdir);

	char path[300];
	snprintf(path, sizeof(path), "%s/keylog.txt", outdir);
	kl_logf = fopen(path, "w");
	if (kl_logf)
	{
		fprintf(kl_logf, "# OpenTyrian keylog v2 — 每幀有輸入才記錄\n");
		fprintf(kl_logf, "# 欄位: frame<TAB>H:held_scancodes<TAB>Q:sym:scancode:mod(KeyDown,含repeat)\n");
		fflush(kl_logf);
	}
}

/* 由 instr/keyboard.c 的 SDL_KEYDOWN 呼叫（含 repeat 事件） */
void keylog_keydown(int sym, int scancode, int mod)
{
	check_enabled();
	if (!enabled)
		return;
	if (qcount < KL_MAXQ)
	{
		qbuf[qcount].sym = sym;
		qbuf[qcount].scancode = scancode;
		qbuf[qcount].mod = mod;
		qcount++;
	}
}

static void save_screenshot(unsigned long n)
{
	SDL_Surface *vs = VGAScreen;
	if (!vs)
		return;

	SDL_Surface *rgb = SDL_CreateRGBSurfaceWithFormat(0, vs->w, vs->h, 24, SDL_PIXELFORMAT_RGB24);
	if (!rgb)
		return;

	for (int y = 0; y < vs->h; y++)
	{
		const Uint8 *s = (const Uint8 *)vs->pixels + y * vs->pitch;
		Uint8 *d = (Uint8 *)rgb->pixels + y * rgb->pitch;
		for (int x = 0; x < vs->w; x++)
		{
			Uint8 r, g, b;
			SDL_GetRGB(rgb_palette[s[x]], main_window_tex_format, &r, &g, &b);
			d[x * 3 + 0] = r;
			d[x * 3 + 1] = g;
			d[x * 3 + 2] = b;
		}
	}

	char path[320];
	snprintf(path, sizeof(path), "%s/frame_%08lu.bmp", outdir, n);
	SDL_SaveBMP(rgb, path);
	SDL_FreeSurface(rgb);
}

void keylog_frame(void)
{
	check_enabled();
	if (!enabled)
	{
		qcount = 0;
		return;
	}

	frame_num++;

	/* H: 按住的鍵 */
	char held[1024];
	held[0] = '\0';
	int hany = 0;
	for (int sc = 0; sc < SDL_NUM_SCANCODES; sc++)
	{
		if (keysactive[sc])
		{
			char tmp[24];
			snprintf(tmp, sizeof(tmp), "%s%d", hany ? "," : "", sc);
			strncat(held, tmp, sizeof(held) - strlen(held) - 1);
			hany = 1;
		}
	}

	/* Q: 當幀 KeyDown 佇列事件 */
	char queue[2048];
	queue[0] = '\0';
	int qany = (qcount > 0);
	for (int i = 0; i < qcount; i++)
	{
		char tmp[48];
		snprintf(tmp, sizeof(tmp), "%s%d:%d:%d", i ? "," : "", qbuf[i].sym, qbuf[i].scancode, qbuf[i].mod);
		strncat(queue, tmp, sizeof(queue) - strlen(queue) - 1);
	}
	qcount = 0; /* 沖出 */

	if (!hany && !qany && !force)
		return; /* 此幀無輸入：只前進 frame_num */

	ensure_init();
	if (kl_logf)
	{
		fprintf(kl_logf, "%lu\tH:%s\tQ:%s\n", frame_num, held, queue);
		fflush(kl_logf);
	}
	if (shots)
		save_screenshot(frame_num);
}
