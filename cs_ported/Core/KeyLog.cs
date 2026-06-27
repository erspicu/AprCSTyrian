using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植對照工具（非遊戲邏輯）：與原版 instr/keylog.c 對稱的「重播 / 擷取」端。
/// 用環境變數啟用，未設時完全 no-op。
///
///  - KEYLOG=1            擷取模式：每幀有輸入才記 keylog.txt + 截圖（驗證 C# 端）
///  - KEYLOG_REPLAY=PATH  重播模式：讀原版 log，逐幀還原輸入並於相同 frame 截圖
///  - KEYLOG_DIR=DIR      輸出資料夾（預設 keylog_cs）
///  - KEYLOG_NOSHOT=1     不存截圖
///  - KEYLOG_FORCE=1      擷取模式下每幀都捕捉
///
/// 每幀記錄兩種輸入（選單讀佇列邊緣、關卡讀按住狀態，兩者都要）：
///   H: 按住的鍵（keysactive 快照）        Q: 該幀 KeyDown 佇列事件 sym:scancode:mod（含重複）
/// 重播時 keysactive 用 H 還原、佇列用 Q 注入（Keyboard.InjectQueueInput）。
/// frame 編號 = Video.JE_showVGA() 絕對呼叫次數（與原版一致）。
/// </summary>
internal static unsafe class KeyLog
{
    /// <summary>
    /// 原始碼總開關。開發/除錯期 = true（keylog 擷取 / 重播 / 截圖 可用，由環境變數啟用）；
    /// 正式發佈時 = false（整個機制完全關閉，環境變數也不生效、相關程式碼被視為死碼消除）。
    /// 平時保持 true；<c>tools/release.sh</c> 發佈時會自動以 <c>-p:KeyLogOff=true</c>
    /// 編譯出 <c>KEYLOG_OFF</c> 符號，讓發佈版自動關閉，不需手動改這裡。
    /// 若想手動關閉，直接把下面改成 <c>const bool Enabled = false;</c> 即可。
    /// </summary>
#if KEYLOG_OFF
    internal const bool Enabled = false;
#else
    internal const bool Enabled = true;
#endif

    private const int UNSET = -1, OFF = 0, CAPTURE = 1, REPLAY = 2;
    private static int mode = UNSET;
    private static long frameNum = 0;
    private static bool shots = true;
    private static bool force = false;
    private static string outDir = "keylog_cs";
    private static StreamWriter? logw;

    // 重播資料：每幀的按住鍵 + 佇列事件
    private static readonly Dictionary<long, int[]> replayHeld = new();
    private static readonly Dictionary<long, (int sym, int sc, int mod)[]> replayQueue = new();
    private static readonly HashSet<long> captureFrames = new();

    // 擷取模式：當幀 KeyDown 緩衝
    private static readonly List<(int sym, int sc, int mod)> qbuf = new();

    private static bool EnvOn(string name)
    {
        string? v = Environment.GetEnvironmentVariable(name);
        return v is { Length: > 0 } && v != "0";
    }

    private static void Init()
    {
        mode = OFF;
        if (!Enabled)
            return; // 總開關關閉：完全 no-op（不讀環境變數、不擷取、不重播、不截圖）
        shots = !EnvOn("KEYLOG_NOSHOT");
        force = EnvOn("KEYLOG_FORCE");
        string? dir = Environment.GetEnvironmentVariable("KEYLOG_DIR");
        if (dir is { Length: > 0 }) outDir = dir;

        string? rep = Environment.GetEnvironmentVariable("KEYLOG_REPLAY");
        if (rep is { Length: > 0 })
        {
            mode = REPLAY;
            LoadReplay(rep);
        }
        else if (EnvOn("KEYLOG"))
        {
            mode = CAPTURE;
        }

        if (mode != OFF)
        {
            Directory.CreateDirectory(outDir);
            if (mode == CAPTURE)
            {
                logw = new StreamWriter(Path.Combine(outDir, "keylog.txt")) { AutoFlush = true };
                logw.WriteLine("# C# port keylog v2 — frame<TAB>H:held<TAB>Q:sym:scancode:mod");
            }
            Console.Error.WriteLine($"[KeyLog] mode={(mode == REPLAY ? "REPLAY" : "CAPTURE")} dir={outDir} replayFrames={captureFrames.Count}");
        }
    }

    private static void LoadReplay(string path)
    {
        if (Directory.Exists(path)) path = Path.Combine(path, "keylog.txt");
        foreach (string line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            string[] parts = line.Split('\t');
            if (parts.Length < 1 || !long.TryParse(parts[0], out long f)) continue;

            var held = new List<int>();
            var queue = new List<(int, int, int)>();
            foreach (string field in parts)
            {
                if (field.StartsWith("H:"))
                {
                    string body = field[2..];
                    if (body.Length > 0)
                        foreach (string s in body.Split(','))
                            if (int.TryParse(s, out int sc)) held.Add(sc);
                }
                else if (field.StartsWith("Q:"))
                {
                    string body = field[2..];
                    if (body.Length > 0)
                        foreach (string ev in body.Split(','))
                        {
                            string[] t = ev.Split(':');
                            if (t.Length == 3 && int.TryParse(t[0], out int sym) && int.TryParse(t[1], out int sc) && int.TryParse(t[2], out int mod))
                                queue.Add((sym, sc, mod));
                        }
                }
            }
            replayHeld[f] = held.ToArray();
            replayQueue[f] = queue.ToArray();
            captureFrames.Add(f);
        }
    }

    /// <summary>C# handleSdlEvents 的 KeyDown 呼叫：擷取模式下緩衝佇列事件。</summary>
    public static void NoteKeyDown(int sym, int scancode, int mod)
    {
        if (mode == UNSET) Init();
        if (mode == CAPTURE) qbuf.Add((sym, scancode, mod));
    }

    /// <summary>Keyboard.handleSdlEvents 末尾呼叫：重播模式下用原版 log 還原 keysactive + 注入佇列。</summary>
    public static void InjectInput()
    {
        if (mode == UNSET) Init();
        if (mode != REPLAY) return;

        long f = frameNum + 1; // 即將處理的幀
        Array.Clear(Keyboard.keysactive, 0, Keyboard.keysactive.Length);
        if (replayHeld.TryGetValue(f, out int[]? held))
            foreach (int sc in held)
                if (sc >= 0 && sc < Keyboard.keysactive.Length)
                    Keyboard.keysactive[sc] = true;
        if (replayQueue.TryGetValue(f, out (int sym, int sc, int mod)[]? q))
            foreach (var e in q)
                Keyboard.InjectQueueInput(e.sym, e.sc, e.mod);
    }

    /// <summary>Video.JE_showVGA 末尾呼叫：前進 frame；於對應幀截圖 / 記錄。</summary>
    public static void OnShowVGA()
    {
        if (mode == UNSET) Init();
        if (mode == OFF) return;
        frameNum++;

        if (mode == CAPTURE)
        {
            var heldSb = new StringBuilder();
            bool hany = false;
            for (int sc = 0; sc < Keyboard.keysactive.Length; sc++)
                if (Keyboard.keysactive[sc])
                {
                    if (hany) heldSb.Append(',');
                    heldSb.Append(sc);
                    hany = true;
                }

            var qSb = new StringBuilder();
            for (int i = 0; i < qbuf.Count; i++)
            {
                if (i > 0) qSb.Append(',');
                qSb.Append(qbuf[i].sym).Append(':').Append(qbuf[i].sc).Append(':').Append(qbuf[i].mod);
            }
            bool qany = qbuf.Count > 0;
            qbuf.Clear();

            if (!hany && !qany && !force) return;
            logw?.WriteLine($"{frameNum}\tH:{heldSb}\tQ:{qSb}");
            if (shots) SaveShot(frameNum);
        }
        else // REPLAY：在原版有記錄的相同 frame 截圖
        {
            if (shots && captureFrames.Contains(frameNum))
                SaveShot(frameNum);
        }
    }

    private static void SaveShot(long n)
    {
        var vs = Video.VGAScreen;
        int w = vs.w, h = vs.h, pitch = vs.pitch;
        byte* px = vs.pixels;

        int rowSize = (w * 3 + 3) & ~3;
        int dataSize = rowSize * h;
        int fileSize = 54 + dataSize;
        var buf = new byte[fileSize];

        buf[0] = (byte)'B'; buf[1] = (byte)'M';
        WriteI32(buf, 2, fileSize);
        WriteI32(buf, 10, 54);
        WriteI32(buf, 14, 40);
        WriteI32(buf, 18, w);
        WriteI32(buf, 22, h);
        buf[26] = 1;
        buf[28] = 24;
        WriteI32(buf, 34, dataSize);

        for (int y = 0; y < h; y++)
        {
            byte* srow = px + (long)y * pitch;
            int drow = 54 + (h - 1 - y) * rowSize;
            for (int x = 0; x < w; x++)
            {
                uint rgb = Palette.rgb_palette[srow[x]];
                int o = drow + x * 3;
                buf[o + 0] = (byte)(rgb & 0xFF);
                buf[o + 1] = (byte)((rgb >> 8) & 0xFF);
                buf[o + 2] = (byte)((rgb >> 16) & 0xFF);
            }
        }

        File.WriteAllBytes(Path.Combine(outDir, $"frame_{n:D8}.bmp"), buf);
    }

    private static void WriteI32(byte[] b, int o, int v)
    {
        b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24);
    }
}
