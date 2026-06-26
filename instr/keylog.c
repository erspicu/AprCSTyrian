/* keylog.c — 見 keylog.h。
 *
 * 機制：
 *  - frame_num：JE_showVGA() 的絕對呼叫次數（= 已呈現幀數）。每幀都 +1。
 *  - 「有輸入才紀錄」：掃描 keysactive[]；若有任何鍵按下，
 *      → 寫一行到 keylog.txt：  <frame>\t<scancode:name>,<scancode:name>,...
 *      → 同時存一張截圖 frame_<frame>.bmp（由 VGAScreen 索引色 + rgb_palette 還原真彩）。
 *  - 沒有任何鍵按下的幀：只遞增 frame_num，不寫 log、不截圖。
 *
 * 啟用：環境變數 KEYLOG=1（其他值/未設則整個機制為 no-op，與原版行為一致）。
 * 輸出目錄：KEYLOG_DIR（預設 "keylog"），相對於執行時的工作目錄。
 * 關閉截圖（只留 keylog.txt）：KEYLOG_NOSHOT=1。
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
static int shots = 1;      /* 是否存截圖 */
static int force = 0;      /* KEYLOG_FORCE=1：每幀都捕捉（測試/定頻參考用） */
static int initialized = 0;
static FILE *kl_logf = NULL;
static char outdir[256] = "keylog";

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
		fprintf(kl_logf, "# OpenTyrian keylog — 每幀有輸入才記錄\n");
		fprintf(kl_logf, "# 欄位: frame<TAB>scancode:name[,scancode:name...]\n");
		fflush(kl_logf);
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
	if (enabled < 0)
	{
		const char *e = SDL_getenv("KEYLOG");
		enabled = (e && *e && *e != '0') ? 1 : 0;
		const char *ns = SDL_getenv("KEYLOG_NOSHOT");
		shots = (ns && *ns && *ns != '0') ? 0 : 1;
		const char *fc = SDL_getenv("KEYLOG_FORCE");
		force = (fc && *fc && *fc != '0') ? 1 : 0;
	}
	if (!enabled)
		return;

	frame_num++;

	/* 收集目前按下的鍵（有輸入才紀錄） */
	char keys[1024];
	keys[0] = '\0';
	int any = 0;
	for (int sc = 0; sc < SDL_NUM_SCANCODES; sc++)
	{
		if (keysactive[sc])
		{
			const char *nm = SDL_GetScancodeName((SDL_Scancode)sc);
			char tmp[80];
			snprintf(tmp, sizeof(tmp), "%s%d:%s", any ? "," : "", sc, (nm && *nm) ? nm : "?");
			strncat(keys, tmp, sizeof(keys) - strlen(keys) - 1);
			any = 1;
		}
	}
	if (!any && !force)
		return; /* 此幀無輸入：只前進 frame_num（KEYLOG_FORCE=1 時仍捕捉，供測試/定頻參考用） */
	if (!any)
		strcpy(keys, "(none)");

	ensure_init();
	if (kl_logf)
	{
		fprintf(kl_logf, "%lu\t%s\n", frame_num, keys);
		fflush(kl_logf);
	}
	if (shots)
		save_screenshot(frame_num);
}
