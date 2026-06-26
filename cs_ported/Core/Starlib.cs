using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/starlib.c —— 3D 星空（標題/螢幕保護）。
/// 星空模擬與繪製忠實移植；互動熱鍵（1-9 切換 setup 等）暫簡化為 ESC 離開，
/// 待完整 keyboard.c（中性事件佇列）再補。doChange 自動切換 setup 仍保留。
/// </summary>
internal static unsafe class Starlib
{
    private const int starlib_MAX_STARS = 1000;
    private const int MAX_TYPES = 14;

    private struct JE_StarType
    {
        public short spX, spY, spZ;
        public short lastX, lastY;
    }

    private static int tempX, tempY;
    private static bool run;
    private static readonly JE_StarType[] star = new JE_StarType[starlib_MAX_STARS];

    private static byte setup;
    private static ushort stepCounter;

    private static ushort nsp2;
    private static sbyte nspVar2Inc;

    /* JE: new sprite pointer */
    private static float nsp;
    private static float nspVarInc;
    private static float nspVarVarInc;

    private static ushort changeTime;
    private static bool doChange;

    private static bool grayB;

    private static short starlib_speed;
    private static sbyte speedChange;

    private static byte pColor;

    /// <summary>執行一幀星空模擬與繪製；回傳是否繼續執行（false=要求離開）。</summary>
    public static bool starLibMain()
    {
        JE_wackyCol();

        grayB = false;

        starlib_speed += speedChange;

        byte* surf = Video.VGAScreen.pixels;

        for (int idx = 0; idx < starlib_MAX_STARS; idx++)
        {
            ref JE_StarType stars = ref star[idx];

            // Calculate the offset to where we wish to draw
            int off = stars.lastX + stars.lastY * 320;

            // We don't want trails in our star field.  Erase the old graphic
            if (off >= 640 && off < (320 * 200) - 640)
            {
                surf[off] = 0;
                surf[off - 1] = 0;
                surf[off + 1] = 0;
                surf[off - 2] = 0;
                surf[off + 2] = 0;
                surf[off - 320] = 0;
                surf[off + 320] = 0;
                surf[off - 640] = 0;
                surf[off + 640] = 0;
            }

            // Move star
            int tempZ = stars.spZ;
            tempX = (stars.spX / tempZ) + 160;
            tempY = (stars.spY / tempZ) + 100;
            tempZ -= starlib_speed;

            // If star is out of range, make a new one
            if (tempZ <= 0 ||
                tempY == 0 || tempY > 198 ||
                tempX > 318 || tempX < 1)
            {
                stars.spZ = 500;
                JE_newStar();
                stars.spX = (short)tempX;
                stars.spY = (short)tempY;
            }
            else // Otherwise, update & draw it
            {
                stars.lastX = (short)tempX;
                stars.lastY = (short)tempY;
                stars.spZ = (short)tempZ;

                off = tempX + tempY * 320;

                byte tempCol;
                if (grayB)
                    tempCol = (byte)(tempZ >> 1);
                else
                    tempCol = (byte)(pColor + ((tempZ >> 4) & 31));

                // Draw the pixel!
                if (off >= 640 && off < (320 * 200) - 640)
                {
                    surf[off] = tempCol;

                    tempCol += 72;
                    surf[off - 1] = tempCol;
                    surf[off + 1] = tempCol;
                    surf[off - 320] = tempCol;
                    surf[off + 320] = tempCol;

                    tempCol += 72;
                    surf[off - 2] = tempCol;
                    surf[off + 2] = tempCol;
                    surf[off - 640] = tempCol;
                    surf[off + 640] = tempCol;
                }
            }
        }

        // 簡化輸入：ESC => 離開（事件已由外層 Keyboard.handleSdlEvents 處理）
        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_ESCAPE])
            run = false;

        if (doChange)
        {
            stepCounter++;
            if (stepCounter > changeTime)
                JE_changeSetup(0);
        }

        if ((MtRand.mt_rand() % 1000) == 1)
            nspVarVarInc = MtRand.mt_rand_1() * 0.01f - 0.005f;

        nspVarInc += nspVarVarInc;

        return run;
    }

    public static void JE_wackyCol() { /* YKS: Does nothing */ }

    private static bool initialized = false;

    public static void JE_starlib_init()
    {
        run = true;

        if (!initialized)
        {
            initialized = true;

            JE_resetValues();
            JE_changeSetup(2);
            doChange = true;

            for (int x = 0; x < starlib_MAX_STARS; x++)
            {
                star[x].spX = (short)((MtRand.mt_rand() % 64000) - 32000);
                star[x].spY = (short)((MtRand.mt_rand() % 40000) - 20000);
                star[x].spZ = (short)(x + 1);
            }
        }
    }

    public static void JE_resetValues()
    {
        nsp2 = 1;
        nspVar2Inc = 1;
        nspVarInc = 0.1f;
        nspVarVarInc = 0.0001f;
        nsp = 0;
        pColor = 32;
        starlib_speed = 2;
        speedChange = 0;
    }

    public static void JE_changeSetup(byte setupType)
    {
        stepCounter = 0;
        changeTime = (ushort)(MtRand.mt_rand() % 1000);

        if (setupType > 0)
            setup = setupType;
        else
            setup = (byte)(MtRand.mt_rand() % (MAX_TYPES + 1));

        if (setup == 1)
            nspVarInc = 0.1f;
        if (nspVarInc > 2.2f)
            nspVarInc = 0.1f;
    }

    public static void JE_newStar()
    {
        if (setup == 0)
        {
            tempX = (int)(MtRand.mt_rand() % 64000) - 32000;
            tempY = (int)(MtRand.mt_rand() % 40000) - 20000;
        }
        else
        {
            nsp = nsp + nspVarInc; // YKS: < lol
            switch (setup)
            {
            case 1:
                tempX = (int)(MathF.Sin(nsp / 30) * 20000);
                tempY = (int)(MtRand.mt_rand() % 40000) - 20000;
                break;
            case 2:
                tempX = (int)(MathF.Cos(nsp) * 20000);
                tempY = (int)(MathF.Sin(nsp) * 20000);
                break;
            case 3:
                tempX = (int)(MathF.Cos(nsp * 15) * 100) * ((int)(nsp / 6) % 200);
                tempY = (int)(MathF.Sin(nsp * 15) * 100) * ((int)(nsp / 6) % 200);
                break;
            case 4:
                tempX = (int)(MathF.Sin(nsp / 60) * 20000);
                tempY = (int)(MathF.Cos(nsp) * (int)(MathF.Sin(nsp / 200) * 300) * 100);
                break;
            case 5:
                tempX = (int)(MathF.Sin(nsp / 2) * 20000);
                tempY = (int)(MathF.Cos(nsp) * (int)(MathF.Sin(nsp / 200) * 300) * 100);
                break;
            case 6:
                tempX = (int)(MathF.Sin(nsp) * 40000);
                tempY = (int)(MathF.Cos(nsp) * 20000);
                break;
            case 8:
                tempX = (int)(MathF.Sin(nsp / 2) * 40000);
                tempY = (int)(MathF.Cos(nsp) * 20000);
                break;
            case 7:
                tempX = (int)(MtRand.mt_rand() % 65535);
                if ((MtRand.mt_rand() % 2) == 0)
                    tempY = (int)(MathF.Cos(nsp / 80) * 10000) + 15000;
                else
                    tempY = 50000 - (int)(MathF.Cos(nsp / 80) * 13000);
                break;
            case 9:
                nsp2 = (ushort)(nsp2 + nspVar2Inc);
                if ((nsp2 == 65535) || (nsp2 == 0))
                    nspVar2Inc = (sbyte)-nspVar2Inc;
                tempX = (int)(MathF.Cos(MathF.Sin(nsp2 / 10.0f) + (nsp / 500)) * 32000);
                tempY = (int)(MathF.Sin(MathF.Cos(nsp2 / 10.0f) + (nsp / 500)) * 30000);
                break;
            case 10:
                nsp2 = (ushort)(nsp2 + nspVar2Inc);
                if ((nsp2 == 65535) || (nsp2 == 0))
                    nspVar2Inc = (sbyte)-nspVar2Inc;
                tempX = (int)(MathF.Cos(MathF.Sin(nsp2 / 5.0f) + (nsp / 100)) * 32000);
                tempY = (int)(MathF.Sin(MathF.Cos(nsp2 / 5.0f) + (nsp / 100)) * 30000);
                break;
            case 11:
                nsp2 = (ushort)(nsp2 + nspVar2Inc);
                if ((nsp2 == 65535) || (nsp2 == 0))
                    nspVar2Inc = (sbyte)-nspVar2Inc;
                tempX = (int)(MathF.Cos(MathF.Sin(nsp2 / 1000.0f) + (nsp / 2)) * 32000);
                tempY = (int)(MathF.Sin(MathF.Cos(nsp2 / 1000.0f) + (nsp / 2)) * 30000);
                break;
            case 12:
                if (nsp != 0)
                {
                    nsp2 = (ushort)(nsp2 + nspVar2Inc);
                    if ((nsp2 == 65535) || (nsp2 == 0))
                        nspVar2Inc = (sbyte)-nspVar2Inc;
                    tempX = (int)(MathF.Cos(MathF.Sin(nsp2 / 2.0f) / (MathF.Sqrt(MathF.Abs(nsp)) / 10.0f + 1) + (nsp2 / 100.0f)) * 32000);
                    tempY = (int)(MathF.Sin(MathF.Cos(nsp2 / 2.0f) / (MathF.Sqrt(MathF.Abs(nsp)) / 10.0f + 1) + (nsp2 / 100.0f)) * 30000);
                }
                break;
            case 13:
                if (nsp != 0)
                {
                    nsp2 = (ushort)(nsp2 + nspVar2Inc);
                    if ((nsp2 == 65535) || (nsp2 == 0))
                        nspVar2Inc = (sbyte)-nspVar2Inc;
                    tempX = (int)(MathF.Cos(MathF.Sin(nsp2 / 10.0f) / 2 + (nsp / 20)) * 32000);
                    tempY = (int)(MathF.Sin(MathF.Sin(nsp2 / 11.0f) / 2 + (nsp / 20)) * 30000);
                }
                break;
            case 14:
                nsp2 = (ushort)(nsp2 + nspVar2Inc);
                tempX = (int)((MathF.Sin(nsp) + MathF.Cos(nsp2 / 1000.0f) * 3) * 12000);
                tempY = (int)(MathF.Cos(nsp) * 10000) + nsp2;
                break;
            }
        }
    }
}
