using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植對照工具（非遊戲邏輯）：與原版 instr/keylog.c 對稱的「重播 / 擷取」端。
/// 用環境變數啟用，未設時完全 no-op（與正常遊戲行為一致）。
///
///  - KEYLOG=1            擷取模式：同原版，有輸入才記 keylog.txt + 截圖（驗證 C# 端）
///  - KEYLOG_REPLAY=PATH  重播模式：讀 PATH（資料夾或 keylog.txt）的原版 log，
///                        逐幀以相同 scancode 覆寫 keysactive，並在「原版有記錄的相同 frame」截圖，
///                        產出與原版 1:1 對應的 BMP 供逐張比對。
///  - KEYLOG_DIR=DIR      輸出資料夾（預設 keylog_cs）
///  - KEYLOG_NOSHOT=1     不存截圖（只記 keylog.txt）
///  - KEYLOG_FORCE=1      擷取模式下每幀都捕捉（測試 / 定頻參考用）
///
/// frame 編號定義與原版一致：Video.JE_showVGA() 的絕對呼叫次數（每幀 +1）。
/// </summary>
internal static unsafe class KeyLog
{
    private const int UNSET = -1, OFF = 0, CAPTURE = 1, REPLAY = 2;
    private static int mode = UNSET;
    private static long frameNum = 0;
    private static bool shots = true;
    private static bool force = false;
    private static string outDir = "keylog_cs";
    private static StreamWriter? logw;
    private static readonly Dictionary<long, int[]> replayInput = new();
    private static readonly HashSet<long> captureFrames = new();

    private static bool EnvOn(string name)
    {
        string? v = Environment.GetEnvironmentVariable(name);
        return v is { Length: > 0 } && v != "0";
    }

    private static void Init()
    {
        mode = OFF;
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
                logw.WriteLine("# C# port keylog — frame<TAB>scancode[,scancode...]");
            }
            Console.Error.WriteLine($"[KeyLog] mode={(mode == REPLAY ? "REPLAY" : "CAPTURE")} dir={outDir} replayFrames={replayInput.Count}");
        }
    }

    private static void LoadReplay(string path)
    {
        if (Directory.Exists(path)) path = Path.Combine(path, "keylog.txt");
        foreach (string line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            int tab = line.IndexOf('\t');
            if (tab < 0) continue;
            if (!long.TryParse(line.AsSpan(0, tab), out long f)) continue;

            string keys = line[(tab + 1)..].Trim();
            var scs = new List<int>();
            if (keys != "(none)")
            {
                foreach (string part in keys.Split(','))
                {
                    int colon = part.IndexOf(':'); // 格式 scancode:name（C# 端只取 scancode）
                    ReadOnlySpan<char> num = colon >= 0 ? part.AsSpan(0, colon) : part.AsSpan();
                    if (int.TryParse(num, out int sc)) scs.Add(sc);
                }
            }
            replayInput[f] = scs.ToArray();
            captureFrames.Add(f);
        }
    }

    /// <summary>在 Keyboard.handleSdlEvents 末尾呼叫：重播模式下用原版 log 覆寫 keysactive。</summary>
    public static void InjectInput()
    {
        if (mode == UNSET) Init();
        if (mode != REPLAY) return;

        long f = frameNum + 1; // 即將處理的幀
        Array.Clear(Keyboard.keysactive, 0, Keyboard.keysactive.Length);
        if (replayInput.TryGetValue(f, out int[]? scs))
            foreach (int sc in scs)
                if (sc >= 0 && sc < Keyboard.keysactive.Length)
                    Keyboard.keysactive[sc] = true;
    }

    /// <summary>在 Video.JE_showVGA 末尾呼叫：前進 frame；於對應幀截圖 / 記錄。</summary>
    public static void OnShowVGA()
    {
        if (mode == UNSET) Init();
        if (mode == OFF) return;
        frameNum++;

        if (mode == CAPTURE)
        {
            var sb = new StringBuilder();
            bool any = false;
            for (int sc = 0; sc < Keyboard.keysactive.Length; sc++)
                if (Keyboard.keysactive[sc])
                {
                    if (any) sb.Append(',');
                    sb.Append(sc);
                    any = true;
                }
            if (!any && !force) return; // 無輸入：只前進 frameNum
            logw?.WriteLine($"{frameNum}\t{(any ? sb.ToString() : "(none)")}");
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

        int rowSize = (w * 3 + 3) & ~3;       // 4-byte 對齊
        int dataSize = rowSize * h;
        int fileSize = 54 + dataSize;
        var buf = new byte[fileSize];

        buf[0] = (byte)'B'; buf[1] = (byte)'M';
        WriteI32(buf, 2, fileSize);
        WriteI32(buf, 10, 54);                 // pixel data offset
        WriteI32(buf, 14, 40);                 // DIB header size
        WriteI32(buf, 18, w);
        WriteI32(buf, 22, h);                  // 正值 = bottom-up
        buf[26] = 1;                            // planes
        buf[28] = 24;                           // bpp
        WriteI32(buf, 34, dataSize);

        for (int y = 0; y < h; y++)
        {
            byte* srow = px + (long)y * pitch;
            int drow = 54 + (h - 1 - y) * rowSize; // BMP bottom-up
            for (int x = 0; x < w; x++)
            {
                uint rgb = Palette.rgb_palette[srow[x]]; // 0xFFRRGGBB
                int o = drow + x * 3;
                buf[o + 0] = (byte)(rgb & 0xFF);          // B
                buf[o + 1] = (byte)((rgb >> 8) & 0xFF);   // G
                buf[o + 2] = (byte)((rgb >> 16) & 0xFF);  // R
            }
        }

        File.WriteAllBytes(Path.Combine(outDir, $"frame_{n:D8}.bmp"), buf);
    }

    private static void WriteI32(byte[] b, int o, int v)
    {
        b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24);
    }
}
