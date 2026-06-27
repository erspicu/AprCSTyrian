using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace XBRz_speed
{
    // ============================================================
    // xBRZ 濾鏡 - .NET 4.8.1 (C# 9.0) 極致多倍率支援版 (2X ~ 6X)
    // 架構：統一特徵偵測 -> O(1) 狀態分派函式 -> 專屬硬編碼混合管線
    // ============================================================
    unsafe public class HS_XBRz
    {
        // 原版 xBRZ 為浮點門檻：dominant = 3.6、steep = 2.2。
        // 以整數 (×10) 還原，避免用 4 / 2 近似而改變邊緣判定特徵（漏判對角線 / 過度混合）。
        const int dominantThresholdNum = 36, steepThresholdNum = 22, thresholdDen = 10;
        const int eqColorThres = 30;   // 官方 equalColorTolerance=30（線性距離；原 900 是 30² 之誤，配平方距離才對）

        const int BlendNone = 0;
        const int BlendNormal = 1;
        const int BlendDominant = 2;

        static int* lTable_dist;

        private const int _MAX_ROTS = 4;
        private const int _MAX_SCALE = 6;
        private const int _MAX_SCALE_SQUARED = _MAX_SCALE * _MAX_SCALE;

        private static readonly int[] _MATRIX_ROTATION;

        // 各倍率專屬的主函式指標陣列 (O(1) Jump Tables)
        private static readonly delegate*<uint, uint*, int, int, int, void>[] _blendFuncs2X;
        private static readonly delegate*<uint, uint*, int, int, int, void>[] _blendFuncs3X;
        private static readonly delegate*<uint, uint*, int, int, int, void>[] _blendFuncs4X;
        private static readonly delegate*<uint, uint*, int, int, int, void>[] _blendFuncs5X;
        private static readonly delegate*<uint, uint*, int, int, int, void>[] _blendFuncs6X;

        static HS_XBRz()
        {
            // 初始化旋轉矩陣 (所有倍率共用 6x6 的最大空間)
            _MATRIX_ROTATION = new int[(_MAX_SCALE - 1) * _MAX_SCALE_SQUARED * _MAX_ROTS];
            for (var n = 2; n < _MAX_SCALE + 1; n++)
                for (var r = 0; r < _MAX_ROTS; r++)
                {
                    var nr = (n - 2) * (_MAX_ROTS * _MAX_SCALE_SQUARED) + r * _MAX_SCALE_SQUARED;
                    for (var i = 0; i < _MAX_SCALE; i++)
                        for (var j = 0; j < _MAX_SCALE; j++)
                            _MATRIX_ROTATION[nr + i * _MAX_SCALE + j] = _BuildMatrixRotationPacked(r, i, j, n);
                }

            // 初始化 2X 函式表
            _blendFuncs2X = new delegate*<uint, uint*, int, int, int, void>[8];
            _blendFuncs2X[0] = _blendFuncs2X[2] = _blendFuncs2X[4] = _blendFuncs2X[6] = &BlendCorner2X;
            _blendFuncs2X[1] = &BlendLineDiagonal2X; _blendFuncs2X[3] = &BlendLineShallow2X;
            _blendFuncs2X[5] = &BlendLineSteep2X; _blendFuncs2X[7] = &BlendLineSteepAndShallow2X;

            // 初始化 3X 函式表
            _blendFuncs3X = new delegate*<uint, uint*, int, int, int, void>[8];
            _blendFuncs3X[0] = _blendFuncs3X[2] = _blendFuncs3X[4] = _blendFuncs3X[6] = &BlendCorner3X;
            _blendFuncs3X[1] = &BlendLineDiagonal3X; _blendFuncs3X[3] = &BlendLineShallow3X;
            _blendFuncs3X[5] = &BlendLineSteep3X; _blendFuncs3X[7] = &BlendLineSteepAndShallow3X;

            // 初始化 4X 函式表
            _blendFuncs4X = new delegate*<uint, uint*, int, int, int, void>[8];
            _blendFuncs4X[0] = _blendFuncs4X[2] = _blendFuncs4X[4] = _blendFuncs4X[6] = &BlendCorner4X;
            _blendFuncs4X[1] = &BlendLineDiagonal4X; _blendFuncs4X[3] = &BlendLineShallow4X;
            _blendFuncs4X[5] = &BlendLineSteep4X; _blendFuncs4X[7] = &BlendLineSteepAndShallow4X;

            // 初始化 5X 函式表
            _blendFuncs5X = new delegate*<uint, uint*, int, int, int, void>[8];
            _blendFuncs5X[0] = _blendFuncs5X[2] = _blendFuncs5X[4] = _blendFuncs5X[6] = &BlendCorner5X;
            _blendFuncs5X[1] = &BlendLineDiagonal5X; _blendFuncs5X[3] = &BlendLineShallow5X;
            _blendFuncs5X[5] = &BlendLineSteep5X; _blendFuncs5X[7] = &BlendLineSteepAndShallow5X;

            // 初始化 6X 函式表
            _blendFuncs6X = new delegate*<uint, uint*, int, int, int, void>[8];
            _blendFuncs6X[0] = _blendFuncs6X[2] = _blendFuncs6X[4] = _blendFuncs6X[6] = &BlendCorner6X;
            _blendFuncs6X[1] = &BlendLineDiagonal6X; _blendFuncs6X[3] = &BlendLineShallow6X;
            _blendFuncs6X[5] = &BlendLineSteep6X; _blendFuncs6X[7] = &BlendLineSteepAndShallow6X;
        }

        static int _BuildMatrixRotationPacked(int rotDeg, int i, int j, int n)
        {
            if (rotDeg == 0) return (i << 8) | j;
            var old = _BuildMatrixRotationPacked(rotDeg - 1, i, j, n);
            return ((n - 1 - (old & 0xFF)) << 8) | (old >> 8);
        }

        static byte* _preProcBuffer;
        static byte* preProcBuffer_local;
        static int width, height;
        static int capW, capH;          // 已配置的尺寸相依緩衝容量
        static uint* results_merged;

        /// <summary>
        /// 設定 xBRZ 輸入尺寸並確保緩衝就緒。可重複呼叫（切換解析度時）：
        ///  - 色彩距離查表（與尺寸無關）只配置/計算一次。
        ///  - 尺寸相依緩衝僅在「容量不足」時釋放舊的、重配更大的（grow-only），避免切換時殘留舊大小導致越界 crash。
        /// </summary>
        public static unsafe void initTable(int _width, int _height)
        {
            // 1) 色彩距離查表：一次性。
            if (lTable_dist == null)
            {
                lTable_dist = (int*)AprNes.NesCore.AllocUnmanaged(sizeof(int) * 0x1000000);
                for (int i = 0; i < 0x1000000; i++)
                {
                    int r_diff = ((i & 0xff0000) >> 16) * 2 - 255;
                    int g_diff = ((i & 0x00ff00) >> 8) * 2 - 255;
                    int b_diff = ((i & 0x0000ff) >> 0) * 2 - 255;
                    double kb = 0.0593, kr = 0.2627, kg = 1 - kb - kr;
                    double y = kr * r_diff + kg * g_diff + kb * b_diff;
                    double cb = (0.5 / (1 - kb)) * (b_diff - y);
                    double cr = (0.5 / (1 - kr)) * (r_diff - y);
                    lTable_dist[i] = (int)(y * y + cb * cb + cr * cr);
                }
            }

            // 2) 尺寸相依緩衝：容量不足才釋放舊的、重配（grow-only），確保不會用到已釋放/過小的緩衝。
            if (results_merged == null || _width > capW || _height > capH)
            {
                if (_preProcBuffer != null) AprNes.NesCore.FreeUnmanaged(_preProcBuffer);
                if (results_merged != null) AprNes.NesCore.FreeUnmanaged(results_merged);
                if (preProcBuffer_local != null) AprNes.NesCore.FreeUnmanaged(preProcBuffer_local);

                capW = Math.Max(_width, capW);
                capH = Math.Max(_height, capH);
                _preProcBuffer = (byte*)AprNes.NesCore.AllocUnmanaged(sizeof(byte) * capW * capH);
                results_merged = (uint*)AprNes.NesCore.AllocUnmanaged(sizeof(uint) * capW * capH);
                preProcBuffer_local = (byte*)AprNes.NesCore.AllocUnmanaged(sizeof(byte) * capW);
            }

            // 3) 設定本次輸入邏輯尺寸（≤ 容量；ScaleImage 依此跑迴圈/索引）。
            width = _width;
            height = _height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] static int GetTopR(byte b) => ((b >> 2) & 0x3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] static int GetBottomR(byte b) => ((b >> 4) & 0x3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] static int GetBottomL(byte b) => ((b >> 6) & 0x3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] static byte Rotate(byte b, int rot) => (byte)(b << (rot << 1) | b >> (8 - (rot << 1)));

        // —— 旋轉座標繪圖工具 ——
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _SetPixel(int i, int j, uint col, uint* trg, int outi, int outW, int nr)
        {
            int rot = _MATRIX_ROTATION[nr + i * _MAX_SCALE + j];
            trg[outi + (rot & 0xFF) + (rot >> 8) * outW] = col;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _AlphaBlend(int i, int j, uint col, uint n, uint m, uint* trg, int outi, int outW, int nr)
        {
            int rot = _MATRIX_ROTATION[nr + i * _MAX_SCALE + j];
            int targetIdx = outi + (rot & 0xFF) + (rot >> 8) * outW;
            uint dst = trg[targetIdx]; uint invN = m - n;
            // 逐通道混合（對應官方 gradientRGB）。不可把 R+B 打包成單一除法：
            // 角落混合常用 /100（非 2 次方），R 通道除法的餘數會溢進 B 通道 → 產生假藍點。
            uint r = (((col >> 16) & 0xFFu) * n + ((dst >> 16) & 0xFFu) * invN) / m;
            uint g = (((col >> 8) & 0xFFu) * n + ((dst >> 8) & 0xFFu) * invN) / m;
            uint b = ((col & 0xFFu) * n + (dst & 0xFFu) * invN) / m;
            trg[targetIdx] = 0xFF000000u | (r << 16) | (g << 8) | b;
        }

        // 對應官方 xbrz.cpp distYCbCr：BT.2020 浮點係數 + sqrt（**線性**距離）。
        // 關鍵：官方門檻（equalColorTolerance=30 / dominant=3.6 / steep=2.2 / centerBias=4）
        // 都是針對線性距離調的；若回傳平方距離(y²+cb²+cr²)，這些門檻單位就錯了。
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int DistYCbCr(uint p1, uint p2)
        {
            if (p1 == p2) return 0;
            int r_diff = (int)((p1 >> 16) & 0xFF) - (int)((p2 >> 16) & 0xFF);
            int g_diff = (int)((p1 >> 8) & 0xFF) - (int)((p2 >> 8) & 0xFF);
            int b_diff = (int)(p1 & 0xFF) - (int)(p2 & 0xFF);
            const double k_b = 0.0593, k_r = 0.2627, k_g = 1 - k_b - k_r;
            const double scale_b = 0.5 / (1 - k_b);
            const double scale_r = 0.5 / (1 - k_r);
            double y = k_r * r_diff + k_g * g_diff + k_b * b_diff;
            double c_b = scale_b * (b_diff - y);
            double c_r = scale_r * (r_diff - y);
            return (int)Math.Sqrt(y * y + c_b * c_b + c_r * c_r); // lumaWeight = 1
        }

        // 查表版本，目前未使用（保留供 benchmark）。注意：index 以 (色差+255)>>1 壓縮到 0..255，
        // 會遺失 1 bit 精度且破壞正負對稱（色差 0 與 -1 同 index）。正式請用上面的 inline DistYCbCr。
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int DistYCbCr_Table(uint p1, uint p2)
        {
            if (p1 == p2) return 0;
            return lTable_dist[((((((p1 & 0xff0000) >> 16) - ((p2 & 0xff0000) >> 16)) + 255) >> 1) << 16) | ((((((p1 & 0xff00) >> 8) - ((p2 & 0xff00) >> 8)) + 255) >> 1) << 8) | ((((p1 & 0xff) - (p2 & 0xff)) + 255) >> 1)];
        }

        // 無查表版本：純整數 ALU 運算，避免 16MB LUT 的 cache miss
        // 保留供未來 benchmark 切換使用
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int DistYCbCr_NoTable(uint p1, uint p2)
        {
            if (p1 == p2) return 0;
            int r_diff = (int)((p1 >> 16) & 0xFF) - (int)((p2 >> 16) & 0xFF);
            int g_diff = (int)((p1 >> 8) & 0xFF) - (int)((p2 >> 8) & 0xFF);
            int b_diff = (int)(p1 & 0xFF) - (int)(p2 & 0xFF);
            int y = (262 * r_diff + 678 * g_diff + 59 * b_diff) / 1000;
            int cb = (531 * (b_diff - y)) / 1000;
            int cr = (678 * (r_diff - y)) / 1000;
            return y * y + cb * cb + cr * cr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ColorEQ(uint p1, uint p2) => (p1 == p2) || DistYCbCr(p1, p2) < eqColorThres;

        // ============================================================
        // 核心門面函式 (Facade)
        // ============================================================
        public static void ScaleImage(uint* src, uint* trg, int scale)
        {
            if (scale < 2 || scale > 6) throw new ArgumentOutOfRangeException(nameof(scale));

            // 統一第一步：偵測邊緣特徵矩陣
            ComputeEdgeFeatures(src);

            // 流程第二步：依倍率分別渲染 (傳入各倍率旋轉偏移量 nBase)
            // nBase 公式：(scale - 2) * (4 rotations * 36 pixels)
            switch (scale)
            {
                case 2: RenderPipeline(src, trg, 2, 0); break;
                case 3: RenderPipeline(src, trg, 3, 144); break;
                case 4: RenderPipeline(src, trg, 4, 288); break;
                case 5: RenderPipeline(src, trg, 5, 432); break;
                case 6: RenderPipeline(src, trg, 6, 576); break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ComputeEdgeFeatures(uint* src)
        {
            // 清除跨幀殘留：preProcBuffer_local 帶上一幀垃圾會污染第 0 列的邊緣判定；
            // _preProcBuffer 每列最後一欄不在合併迴圈內被寫入，亦需歸零避免讀到上一幀資料。
            new Span<byte>(preProcBuffer_local, width).Clear();
            new Span<byte>(_preProcBuffer, width * height).Clear();

            // =========================================================
            // 第一階段：平行計算所有邊緣特徵與顏色距離
            // =========================================================
            Parallel.For(0, height, y =>
            {
                int sM1 = Math.Max(y - 1, 0);
                int s0 = y;
                int sP1 = Math.Min(y + 1, height - 1);
                int sP2 = Math.Min(y + 2, height - 1);

                // 【優化一：指標外提】預先算出這 4 個 Y 軸的指標起點
                uint* pM1 = src + sM1 * width;
                uint* p0 = src + s0 * width;
                uint* pP1 = src + sP1 * width;
                uint* pP2 = src + sP2 * width;

                // 【優化二：索引遞增】將 2D 座標轉換改為極速的 1D 累加
                int array_loc = y * width;

                // 用於快取上一個像素的距離計算
                int prev_dist_fk = 0;
                int prev_dist_jg = 0;

                for (int x = 0; x < width; ++x, ++array_loc)
                {
                    int xM1 = Math.Max(x - 1, 0);
                    int xP1 = Math.Min(x + 1, width - 1);
                    int xP2 = Math.Min(x + 2, width - 1);

                    // 1. 透過指標直接讀取相鄰像素
                    uint ker4b = pM1[x]; uint ker4c = pM1[xP1];
                    uint ker4e = p0[xM1]; uint ker4f = p0[x];
                    uint ker4g = p0[xP1]; uint ker4h = p0[xP2];
                    uint ker4i = pP1[xM1]; uint ker4j = pP1[x];
                    uint ker4k = pP1[xP1]; uint ker4l = pP1[xP2];
                    uint ker4n = pP2[x]; uint ker4o = pP2[xP1];

                    // 2. 預先計算並抽取這兩個距離
                    int dist_jg = DistYCbCr(ker4j, ker4g);
                    int dist_fk = DistYCbCr(ker4f, ker4k);

                    // 【優化三：水平滑動快取】
                    int dist_ej = (x == 0) ? DistYCbCr(ker4e, ker4j) : prev_dist_fk;
                    int dist_if = (x == 0) ? DistYCbCr(ker4i, ker4f) : prev_dist_jg;

                    bool diff = (ker4f != ker4g | ker4j != ker4k) & (ker4f != ker4j | ker4g != ker4k);

                    // 3. 代入快取好的距離，計算方向權重
                    int jg = dist_if + DistYCbCr(ker4f, ker4c) + DistYCbCr(ker4n, ker4k) + DistYCbCr(ker4k, ker4h) + (dist_jg << 2);
                    int fk = dist_ej + DistYCbCr(ker4j, ker4o) + DistYCbCr(ker4b, ker4g) + DistYCbCr(ker4g, ker4l) + (dist_fk << 2);

                    // 4. 將當前像素算完的結果儲存，給下一個像素 (x+1) 沿用
                    prev_dist_fk = dist_fk;
                    prev_dist_jg = dist_jg;

                    bool jg_lt_fk = jg < fk;
                    bool fk_lt_jg = fk < jg;
                    bool isDom_jg = (dominantThresholdNum * jg < thresholdDen * fk);
                    bool isDom_fk = (dominantThresholdNum * fk < thresholdDen * jg);

                    byte val_jg = (byte)(BlendNormal + *(byte*)&isDom_jg);
                    byte val_fk = (byte)(BlendNormal + *(byte*)&isDom_fk);

                    bool cond_f = diff & jg_lt_fk & (ker4f != ker4g) & (ker4f != ker4j);
                    bool cond_k = diff & jg_lt_fk & (ker4k != ker4j) & (ker4k != ker4g);
                    bool cond_j = diff & fk_lt_jg & (ker4j != ker4f) & (ker4j != ker4k);
                    bool cond_g = diff & fk_lt_jg & (ker4g != ker4f) & (ker4g != ker4k);

                    // 【優化四：記憶體寫入合併 (Write Combining)】
                    // 將 4 個狀態打包進一個 uint，一次性對齊寫入記憶體
                    uint merged_state = (uint)(
                        ((-(int)(*(byte*)&cond_f) & val_jg)) |
                        ((-(int)(*(byte*)&cond_k) & val_jg) << 8) |
                        ((-(int)(*(byte*)&cond_j) & val_fk) << 16) |
                        ((-(int)(*(byte*)&cond_g) & val_fk) << 24)
                    );
                    results_merged[array_loc] = merged_state;
                }
            });

            // =========================================================
            // 第二階段：合併特徵狀態至 _preProcBuffer
            // =========================================================
            for (int y = 0; y < height; ++y)
            {
                byte blendXy1 = 0;
                int array_loc = y * width;
                // 對應官方：處理每一個來源欄(x < srcWidth)，最後一欄也要寫入 _preProcBuffer
                // （承接前一欄帶過來的角落混合狀態），只在寫 x+1 時做邊界守衛，避免右緣一格漏混合。
                for (int x = 0; x < width; ++x, ++array_loc)
                {
                    // ★ 解壓縮：一次讀取 uint，再利用位元遮罩與移位還原 4 個狀態
                    uint state = results_merged[array_loc];
                    byte r_f = (byte)(state & 0xFF);
                    byte r_k = (byte)((state >> 8) & 0xFF);
                    byte r_j = (byte)((state >> 16) & 0xFF);
                    byte r_g = (byte)((state >> 24) & 0xFF);

                    // 套用原本的組裝邏輯
                    _preProcBuffer[array_loc] = (byte)(preProcBuffer_local[x] | (r_f << 4));
                    preProcBuffer_local[x] = blendXy1 = (byte)(blendXy1 | (r_j << 2));
                    blendXy1 = r_k;
                    if (x + 1 < width)
                        preProcBuffer_local[x + 1] = (byte)(preProcBuffer_local[x + 1] | (r_g << 6));
                }
            }
        }

        // —— 流程 2：渲染管線 (依據 Scale 倍率) ——
        private static void RenderPipeline(uint* src, uint* trg, int scale, int nBase)
        {
            int trgWidth = width * scale;

            Parallel.For(0, height, y =>
            {
                int trgi = scale * y * trgWidth;
                int sM1 = Math.Max(y - 1, 0); int s0 = y; int sP1 = Math.Min(y + 1, height - 1);

                for (int x = 0; x < width; ++x, trgi += scale)
                {
                    int xM1 = Math.Max(x - 1, 0); int xP1 = Math.Min(x + 1, width - 1);
                    byte blendXy = _preProcBuffer[x + y * width];

                    // 依照倍率進行 64-bit 區塊填充
                    switch (scale)
                    {
                        case 2: _FillBlock2x(trg, trgi, trgWidth, src[s0 * width + x]); break;
                        case 3: _FillBlock3x(trg, trgi, trgWidth, src[s0 * width + x]); break;
                        case 4: _FillBlock4x(trg, trgi, trgWidth, src[s0 * width + x]); break;
                        case 5: _FillBlock5x(trg, trgi, trgWidth, src[s0 * width + x]); break;
                        case 6: _FillBlock6x(trg, trgi, trgWidth, src[s0 * width + x]); break;
                    }

                    if (blendXy != 0)
                    {
                        uint ker3_0 = src[sM1 * width + xM1]; uint ker3_1 = src[sM1 * width + x]; uint ker3_2 = src[sM1 * width + xP1];
                        uint ker3_3 = src[s0 * width + xM1]; uint ker3_4 = src[s0 * width + x]; uint ker3_5 = src[s0 * width + xP1];
                        uint ker3_6 = src[sP1 * width + xM1]; uint ker3_7 = src[sP1 * width + x]; uint ker3_8 = src[sP1 * width + xP1];

                        // 呼叫專屬的 O(1) 門面函式
                        switch (scale)
                        {
                            case 2:
                                ProcessRotation2X(Rotate(blendXy, 0), ker3_1, ker3_2, ker3_3, ker3_4, ker3_5, ker3_6, ker3_7, ker3_8, trg, trgi, trgWidth, nBase);
                                ProcessRotation2X(Rotate(blendXy, 1), ker3_3, ker3_0, ker3_7, ker3_4, ker3_1, ker3_8, ker3_5, ker3_2, trg, trgi, trgWidth, nBase + 36);
                                ProcessRotation2X(Rotate(blendXy, 2), ker3_7, ker3_6, ker3_5, ker3_4, ker3_3, ker3_2, ker3_1, ker3_0, trg, trgi, trgWidth, nBase + 72);
                                ProcessRotation2X(Rotate(blendXy, 3), ker3_5, ker3_8, ker3_1, ker3_4, ker3_7, ker3_0, ker3_3, ker3_6, trg, trgi, trgWidth, nBase + 108);
                                break;
                            case 3:
                                ProcessRotation3X(Rotate(blendXy, 0), ker3_1, ker3_2, ker3_3, ker3_4, ker3_5, ker3_6, ker3_7, ker3_8, trg, trgi, trgWidth, nBase);
                                ProcessRotation3X(Rotate(blendXy, 1), ker3_3, ker3_0, ker3_7, ker3_4, ker3_1, ker3_8, ker3_5, ker3_2, trg, trgi, trgWidth, nBase + 36);
                                ProcessRotation3X(Rotate(blendXy, 2), ker3_7, ker3_6, ker3_5, ker3_4, ker3_3, ker3_2, ker3_1, ker3_0, trg, trgi, trgWidth, nBase + 72);
                                ProcessRotation3X(Rotate(blendXy, 3), ker3_5, ker3_8, ker3_1, ker3_4, ker3_7, ker3_0, ker3_3, ker3_6, trg, trgi, trgWidth, nBase + 108);
                                break;
                            case 4:
                                ProcessRotation4X(Rotate(blendXy, 0), ker3_1, ker3_2, ker3_3, ker3_4, ker3_5, ker3_6, ker3_7, ker3_8, trg, trgi, trgWidth, nBase);
                                ProcessRotation4X(Rotate(blendXy, 1), ker3_3, ker3_0, ker3_7, ker3_4, ker3_1, ker3_8, ker3_5, ker3_2, trg, trgi, trgWidth, nBase + 36);
                                ProcessRotation4X(Rotate(blendXy, 2), ker3_7, ker3_6, ker3_5, ker3_4, ker3_3, ker3_2, ker3_1, ker3_0, trg, trgi, trgWidth, nBase + 72);
                                ProcessRotation4X(Rotate(blendXy, 3), ker3_5, ker3_8, ker3_1, ker3_4, ker3_7, ker3_0, ker3_3, ker3_6, trg, trgi, trgWidth, nBase + 108);
                                break;
                            case 5:
                                ProcessRotation5X(Rotate(blendXy, 0), ker3_1, ker3_2, ker3_3, ker3_4, ker3_5, ker3_6, ker3_7, ker3_8, trg, trgi, trgWidth, nBase);
                                ProcessRotation5X(Rotate(blendXy, 1), ker3_3, ker3_0, ker3_7, ker3_4, ker3_1, ker3_8, ker3_5, ker3_2, trg, trgi, trgWidth, nBase + 36);
                                ProcessRotation5X(Rotate(blendXy, 2), ker3_7, ker3_6, ker3_5, ker3_4, ker3_3, ker3_2, ker3_1, ker3_0, trg, trgi, trgWidth, nBase + 72);
                                ProcessRotation5X(Rotate(blendXy, 3), ker3_5, ker3_8, ker3_1, ker3_4, ker3_7, ker3_0, ker3_3, ker3_6, trg, trgi, trgWidth, nBase + 108);
                                break;
                            case 6:
                                ProcessRotation6X(Rotate(blendXy, 0), ker3_1, ker3_2, ker3_3, ker3_4, ker3_5, ker3_6, ker3_7, ker3_8, trg, trgi, trgWidth, nBase);
                                ProcessRotation6X(Rotate(blendXy, 1), ker3_3, ker3_0, ker3_7, ker3_4, ker3_1, ker3_8, ker3_5, ker3_2, trg, trgi, trgWidth, nBase + 36);
                                ProcessRotation6X(Rotate(blendXy, 2), ker3_7, ker3_6, ker3_5, ker3_4, ker3_3, ker3_2, ker3_1, ker3_0, trg, trgi, trgWidth, nBase + 72);
                                ProcessRotation6X(Rotate(blendXy, 3), ker3_5, ker3_8, ker3_1, ker3_4, ker3_7, ker3_0, ker3_3, ker3_6, trg, trgi, trgWidth, nBase + 108);
                                break;
                        }
                    }
                }
            });
        }

        // ============================================================
        // 各倍率專屬的 Jump Table 分派函式 (ProcessRotationNX)
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRotation2X(byte blend, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint* trg, int trgi, int trgW, int nr)
        {
            if (GetBottomR(blend) == BlendNone) return;
            bool doLineBlend = (GetBottomR(blend) >= BlendDominant) | (!((GetTopR(blend) != BlendNone) & !ColorEQ(e, g)) & !((GetBottomL(blend) != BlendNone) & !ColorEQ(e, c)) & !(ColorEQ(g, h) & ColorEQ(h, i) & ColorEQ(i, f) & ColorEQ(f, c) & !ColorEQ(e, i)));
            uint px = DistYCbCr(e, f) <= DistYCbCr(e, h) ? f : h;
            int fg = DistYCbCr(f, g), hc = DistYCbCr(h, c);

            // 修正步驟：先存入變數，再轉換位移 (解決 CS0030 錯誤)
            bool haveShallow = (steepThresholdNum * fg <= thresholdDen * hc) & (e != g) & (d != g);
            bool haveSteep = (steepThresholdNum * hc <= thresholdDen * fg) & (e != c) & (b != c);
            int state = (*(byte*)&doLineBlend) | ((*(byte*)&haveShallow) << 1) | ((*(byte*)&haveSteep) << 2);

            _blendFuncs2X[state](px, trg, trgi, trgW, nr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRotation3X(byte blend, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint* trg, int trgi, int trgW, int nr)
        {
            if (GetBottomR(blend) == BlendNone) return;
            bool doLineBlend = (GetBottomR(blend) >= BlendDominant) | (!((GetTopR(blend) != BlendNone) & !ColorEQ(e, g)) & !((GetBottomL(blend) != BlendNone) & !ColorEQ(e, c)) & !(ColorEQ(g, h) & ColorEQ(h, i) & ColorEQ(i, f) & ColorEQ(f, c) & !ColorEQ(e, i)));
            uint px = DistYCbCr(e, f) <= DistYCbCr(e, h) ? f : h;
            int fg = DistYCbCr(f, g), hc = DistYCbCr(h, c);

            bool haveShallow = (steepThresholdNum * fg <= thresholdDen * hc) & (e != g) & (d != g);
            bool haveSteep = (steepThresholdNum * hc <= thresholdDen * fg) & (e != c) & (b != c);
            int state = (*(byte*)&doLineBlend) | ((*(byte*)&haveShallow) << 1) | ((*(byte*)&haveSteep) << 2);

            _blendFuncs3X[state](px, trg, trgi, trgW, nr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRotation4X(byte blend, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint* trg, int trgi, int trgW, int nr)
        {
            if (GetBottomR(blend) == BlendNone) return;
            bool doLineBlend = (GetBottomR(blend) >= BlendDominant) | (!((GetTopR(blend) != BlendNone) & !ColorEQ(e, g)) & !((GetBottomL(blend) != BlendNone) & !ColorEQ(e, c)) & !(ColorEQ(g, h) & ColorEQ(h, i) & ColorEQ(i, f) & ColorEQ(f, c) & !ColorEQ(e, i)));
            uint px = DistYCbCr(e, f) <= DistYCbCr(e, h) ? f : h;
            int fg = DistYCbCr(f, g), hc = DistYCbCr(h, c);

            bool haveShallow = (steepThresholdNum * fg <= thresholdDen * hc) & (e != g) & (d != g);
            bool haveSteep = (steepThresholdNum * hc <= thresholdDen * fg) & (e != c) & (b != c);
            int state = (*(byte*)&doLineBlend) | ((*(byte*)&haveShallow) << 1) | ((*(byte*)&haveSteep) << 2);

            _blendFuncs4X[state](px, trg, trgi, trgW, nr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRotation5X(byte blend, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint* trg, int trgi, int trgW, int nr)
        {
            if (GetBottomR(blend) == BlendNone) return;
            bool doLineBlend = (GetBottomR(blend) >= BlendDominant) | (!((GetTopR(blend) != BlendNone) & !ColorEQ(e, g)) & !((GetBottomL(blend) != BlendNone) & !ColorEQ(e, c)) & !(ColorEQ(g, h) & ColorEQ(h, i) & ColorEQ(i, f) & ColorEQ(f, c) & !ColorEQ(e, i)));
            uint px = DistYCbCr(e, f) <= DistYCbCr(e, h) ? f : h;
            int fg = DistYCbCr(f, g), hc = DistYCbCr(h, c);

            bool haveShallow = (steepThresholdNum * fg <= thresholdDen * hc) & (e != g) & (d != g);
            bool haveSteep = (steepThresholdNum * hc <= thresholdDen * fg) & (e != c) & (b != c);
            int state = (*(byte*)&doLineBlend) | ((*(byte*)&haveShallow) << 1) | ((*(byte*)&haveSteep) << 2);

            _blendFuncs5X[state](px, trg, trgi, trgW, nr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessRotation6X(byte blend, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint* trg, int trgi, int trgW, int nr)
        {
            if (GetBottomR(blend) == BlendNone) return;
            bool doLineBlend = (GetBottomR(blend) >= BlendDominant) | (!((GetTopR(blend) != BlendNone) & !ColorEQ(e, g)) & !((GetBottomL(blend) != BlendNone) & !ColorEQ(e, c)) & !(ColorEQ(g, h) & ColorEQ(h, i) & ColorEQ(i, f) & ColorEQ(f, c) & !ColorEQ(e, i)));
            uint px = DistYCbCr(e, f) <= DistYCbCr(e, h) ? f : h;
            int fg = DistYCbCr(f, g), hc = DistYCbCr(h, c);

            bool haveShallow = (steepThresholdNum * fg <= thresholdDen * hc) & (e != g) & (d != g);
            bool haveSteep = (steepThresholdNum * hc <= thresholdDen * fg) & (e != c) & (b != c);
            int state = (*(byte*)&doLineBlend) | ((*(byte*)&haveShallow) << 1) | ((*(byte*)&haveSteep) << 2);

            _blendFuncs6X[state](px, trg, trgi, trgW, nr);
        }

        // ============================================================
        // 各倍率的基礎方塊填充與極速 64-bit 寫入 (Block Fill)
        // ============================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _FillBlock2x(uint* trg, int trgi, int pitch, uint col)
        {
            ulong c64 = col | ((ulong)col << 32);
            *(ulong*)(trg + trgi) = c64; *(ulong*)(trg + trgi + pitch) = c64;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _FillBlock3x(uint* trg, int trgi, int pitch, uint col)
        {
            ulong c64 = col | ((ulong)col << 32);
            *(ulong*)(trg + trgi) = c64; trg[trgi + 2] = col; trgi += pitch;
            *(ulong*)(trg + trgi) = c64; trg[trgi + 2] = col; trgi += pitch;
            *(ulong*)(trg + trgi) = c64; trg[trgi + 2] = col;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _FillBlock4x(uint* trg, int trgi, int pitch, uint col)
        {
            // 黑魔法：將 uint 位元保留轉型為 float，利用 Vector4 廣播 4 個相同的像素 (128-bit)
            float fCol = *(float*)&col;
            Vector4 vCol = new Vector4(fCol);

            for (int r = 0; r < 4; r++)
            {
                Unsafe.WriteUnaligned(trg + trgi + r * pitch, vCol); // 一次寫入 4 像素
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _FillBlock5x(uint* trg, int trgi, int pitch, uint col)
        {
            float fCol = *(float*)&col;
            Vector4 vCol = new Vector4(fCol);

            for (int r = 0; r < 5; r++)
            {
                uint* p = trg + trgi + r * pitch;
                Unsafe.WriteUnaligned(p, vCol);        // 寫入像素 0,1,2,3
                Unsafe.WriteUnaligned(p + 1, vCol);    // 寫入像素 1,2,3,4 (非對齊安全)
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _FillBlock6x(uint* trg, int trgi, int pitch, uint col)
        {
            float fCol = *(float*)&col;
            Vector4 vCol = new Vector4(fCol);

            for (int r = 0; r < 6; r++)
            {
                uint* p = trg + trgi + r * pitch;
                Unsafe.WriteUnaligned(p, vCol);        // 寫入像素 0,1,2,3
                Unsafe.WriteUnaligned(p + 2, vCol);    // 寫入像素 2,3,4,5 (非對齊安全)
            }
        }

        // ============================================================
        // —— 2X 專屬混合函式 ——
        // ============================================================
        static void BlendLineShallow2X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(1, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 1, col, 3, 4, trg, outi, outW, nr);
        }
        static void BlendLineSteep2X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(0, 1, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 1, col, 3, 4, trg, outi, outW, nr);
        }
        static void BlendLineSteepAndShallow2X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(1, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(0, 1, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 1, col, 5, 6, trg, outi, outW, nr);
        }
        static void BlendLineDiagonal2X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(1, 1, col, 1, 2, trg, outi, outW, nr);
        }
        static void BlendCorner2X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(1, 1, col, 21, 100, trg, outi, outW, nr);
        }

        // ============================================================
        // —— 3X 專屬混合函式 ——
        // ============================================================
        static void BlendLineShallow3X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(2, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 1, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 2, col, trg, outi, outW, nr);
        }
        static void BlendLineSteep3X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(0, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 1, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 2, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 2, col, trg, outi, outW, nr);
        }
        static void BlendLineSteepAndShallow3X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(2, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 1, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(0, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 2, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 2, col, trg, outi, outW, nr);
        }
        static void BlendLineDiagonal3X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(1, 2, col, 1, 8, trg, outi, outW, nr);
            _AlphaBlend(2, 1, col, 1, 8, trg, outi, outW, nr);
            _AlphaBlend(2, 2, col, 7, 8, trg, outi, outW, nr);
        }
        static void BlendCorner3X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(2, 2, col, 45, 100, trg, outi, outW, nr);
        }

        // ============================================================
        // —— 4X 專屬混合函式 ——
        // ============================================================
        static void BlendLineShallow4X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(3, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 1, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 3, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(3, 2, col, trg, outi, outW, nr);
            _SetPixel(3, 3, col, trg, outi, outW, nr);
        }
        static void BlendLineSteep4X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(0, 3, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 3, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 2, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 3, col, trg, outi, outW, nr);
            _SetPixel(3, 3, col, trg, outi, outW, nr);
        }
        static void BlendLineSteepAndShallow4X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(3, 1, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 3, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(0, 3, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 2, col, 1, 3, trg, outi, outW, nr);
            _SetPixel(3, 3, col, trg, outi, outW, nr);
            _SetPixel(3, 2, col, trg, outi, outW, nr);
            _SetPixel(2, 3, col, trg, outi, outW, nr);
        }
        static void BlendLineDiagonal4X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(3, 2, col, 1, 2, trg, outi, outW, nr);
            _AlphaBlend(2, 3, col, 1, 2, trg, outi, outW, nr);
            _SetPixel(3, 3, col, trg, outi, outW, nr);
        }
        static void BlendCorner4X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(3, 3, col, 68, 100, trg, outi, outW, nr);
            _AlphaBlend(3, 2, col, 9, 100, trg, outi, outW, nr);
            _AlphaBlend(2, 3, col, 9, 100, trg, outi, outW, nr);
        }

        // ============================================================
        // —— 5X 專屬混合函式 ——
        // ============================================================
        static void BlendLineShallow5X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(4, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 4, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(4, 1, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 3, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(4, 2, col, trg, outi, outW, nr);
            _SetPixel(4, 3, col, trg, outi, outW, nr);
            _SetPixel(4, 4, col, trg, outi, outW, nr);
            _SetPixel(3, 4, col, trg, outi, outW, nr);
        }
        static void BlendLineSteep5X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(0, 4, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 3, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(4, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 4, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 3, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 4, col, trg, outi, outW, nr);
            _SetPixel(3, 4, col, trg, outi, outW, nr);
            _SetPixel(4, 4, col, trg, outi, outW, nr);
            _SetPixel(4, 3, col, trg, outi, outW, nr);
        }
        static void BlendLineSteepAndShallow5X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(0, 4, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 3, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 4, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(4, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(4, 1, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 4, col, trg, outi, outW, nr);
            _SetPixel(3, 4, col, trg, outi, outW, nr);
            _SetPixel(4, 2, col, trg, outi, outW, nr);
            _SetPixel(4, 3, col, trg, outi, outW, nr);
            _SetPixel(4, 4, col, trg, outi, outW, nr);
            _AlphaBlend(3, 3, col, 2, 3, trg, outi, outW, nr);
        }
        static void BlendLineDiagonal5X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(4, 2, col, 1, 8, trg, outi, outW, nr);
            _AlphaBlend(3, 3, col, 1, 8, trg, outi, outW, nr);
            _AlphaBlend(2, 4, col, 1, 8, trg, outi, outW, nr);
            _AlphaBlend(4, 3, col, 7, 8, trg, outi, outW, nr);
            _AlphaBlend(3, 4, col, 7, 8, trg, outi, outW, nr);
            _SetPixel(4, 4, col, trg, outi, outW, nr);
        }
        static void BlendCorner5X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(4, 4, col, 86, 100, trg, outi, outW, nr);
            _AlphaBlend(4, 3, col, 23, 100, trg, outi, outW, nr);
            _AlphaBlend(3, 4, col, 23, 100, trg, outi, outW, nr);
        }

        // ============================================================
        // —— 6X 專屬混合函式 ——
        // ============================================================
        static void BlendLineShallow6X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(5, 0, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(4, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 4, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(5, 1, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(4, 3, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 5, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(5, 2, col, trg, outi, outW, nr); _SetPixel(5, 3, col, trg, outi, outW, nr);
            _SetPixel(5, 4, col, trg, outi, outW, nr); _SetPixel(5, 5, col, trg, outi, outW, nr);
            _SetPixel(4, 4, col, trg, outi, outW, nr); _SetPixel(4, 5, col, trg, outi, outW, nr);
        }
        static void BlendLineSteep6X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(0, 5, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(2, 4, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(4, 3, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 5, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(3, 4, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(5, 3, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 5, col, trg, outi, outW, nr); _SetPixel(3, 5, col, trg, outi, outW, nr);
            _SetPixel(4, 5, col, trg, outi, outW, nr); _SetPixel(5, 5, col, trg, outi, outW, nr);
            _SetPixel(4, 4, col, trg, outi, outW, nr); _SetPixel(5, 4, col, trg, outi, outW, nr);
        }
        static void BlendLineSteepAndShallow6X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(0, 5, col, 1, 4, trg, outi, outW, nr); _AlphaBlend(2, 4, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(1, 5, col, 3, 4, trg, outi, outW, nr); _AlphaBlend(3, 4, col, 3, 4, trg, outi, outW, nr);
            _AlphaBlend(5, 0, col, 1, 4, trg, outi, outW, nr); _AlphaBlend(4, 2, col, 1, 4, trg, outi, outW, nr);
            _AlphaBlend(5, 1, col, 3, 4, trg, outi, outW, nr); _AlphaBlend(4, 3, col, 3, 4, trg, outi, outW, nr);
            _SetPixel(2, 5, col, trg, outi, outW, nr); _SetPixel(3, 5, col, trg, outi, outW, nr);
            _SetPixel(4, 5, col, trg, outi, outW, nr); _SetPixel(5, 5, col, trg, outi, outW, nr);
            _SetPixel(4, 4, col, trg, outi, outW, nr); _SetPixel(5, 4, col, trg, outi, outW, nr);
            _SetPixel(5, 2, col, trg, outi, outW, nr); _SetPixel(5, 3, col, trg, outi, outW, nr);
        }
        static void BlendLineDiagonal6X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(5, 3, col, 1, 2, trg, outi, outW, nr);
            _AlphaBlend(4, 4, col, 1, 2, trg, outi, outW, nr);
            _AlphaBlend(3, 5, col, 1, 2, trg, outi, outW, nr);
            _SetPixel(4, 5, col, trg, outi, outW, nr); _SetPixel(5, 5, col, trg, outi, outW, nr); _SetPixel(5, 4, col, trg, outi, outW, nr);
        }
        static void BlendCorner6X(uint col, uint* trg, int outi, int outW, int nr)
        {
            _AlphaBlend(5, 5, col, 97, 100, trg, outi, outW, nr);
            _AlphaBlend(4, 5, col, 42, 100, trg, outi, outW, nr);
            _AlphaBlend(5, 4, col, 42, 100, trg, outi, outW, nr);
            _AlphaBlend(5, 3, col, 6, 100, trg, outi, outW, nr);
            _AlphaBlend(3, 5, col, 6, 100, trg, outi, outW, nr);
        }
    }
}