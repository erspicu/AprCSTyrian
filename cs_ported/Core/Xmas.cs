namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/xmas.c —— 聖誕節偵測與啟用提示。
/// xmas_time：12 月偵測；xmas_prompt：雪花飄落動畫 + Yes/No 選擇 UI。
/// </summary>
internal static class Xmas
{
    public static bool xmas;

    // 對應 xmas.c:xmas_time —— 原版 localtime(time(NULL))->tm_mon == 11（12 月）。
    public static bool xmas_time() => System.DateTime.Now.Month == 12;

    // 雪花狀態（對應 xmas_prompt 內的 struct flakes[80]）。
    private struct Flake
    {
        public short x;     // Sint16
        public short y;     // Sint16
        public byte dyAcc;  // Uint8
        public byte dyAdd;  // Uint8
    }

    // 對應 xmas.c:xmas_prompt
    public static unsafe bool xmas_prompt()
    {
        string[] prompt =
        {
            "Christmas has been detected.",
            "Activate Christmas?",
        };
        string[] choices =
        {
            "Yes",
            "No",
        };

        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');  // need mouse pointer sprites

        bool restart = true;

        Flake[] flakes = new Flake[80];

        const byte dyDen = 128;

        const int xCenter = 320 / 2;
        const int yPrompt = 85;
        const int dyPrompt = 15;
        const int wChoice = 40;
        const int yChoice = 120;
        const int hChoice = 13;

        int selectedIndex = 0;

        Nortsong.setFrameCount(1);

        for (; ; )
        {
            while (true)
            {
                if (restart)
                {
                    for (int i = 0; i < flakes.Length; ++i)
                    {
                        flakes[i].y = (short)(200 + Destruct.c_rand() % 200);
                        flakes[i].dyAdd = (byte)(dyDen / 2 + i * dyDen / flakes.Length);
                    }
                }

                for (int i = 0; i < flakes.Length; ++i)
                {
                    if (flakes[i].y >= 200)
                    {
                        flakes[i].x = (short)(Destruct.c_rand() % 320);
                        flakes[i].y = (short)(200 - 14 - flakes[i].y);
                        flakes[i].dyAcc = flakes[i].dyAdd;
                    }

                    int temp = Destruct.c_rand() & 0xF;
                    if ((temp & 0xE) == 0)
                        flakes[i].x += (short)((temp & 1) * 2 - 1);

                    ushort dyNum = (ushort)(flakes[i].dyAcc + flakes[i].dyAdd);
                    byte dy = (byte)(dyNum / dyDen);
                    flakes[i].dyAcc = (byte)(dyNum % dyDen);
                    flakes[i].y += dy;
                }

                Vga256d.fill_rectangle_wh(Video.VGAScreen, 0, 0, 320, 200, 0x8F);

                // Draw background snowflakes.
                for (int i = 0; i < flakes.Length * 2 / 3; ++i)
                    Sprites.blit_sprite2_blend(Video.VGAScreen, flakes[i].x, flakes[i].y, Sprites.spriteSheet8, 225);

                // Draw prompt.
                for (int i = 0; i < prompt.Length; ++i)
                    FontDraw.drawFontHvFullShadowAligned(Video.VGAScreen, xCenter, yPrompt + dyPrompt * i, prompt[i], Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, (byte)((i % 2) != 0 ? 2 : 4), -2, true, 1);

                // Draw choices.
                for (int i = 0; i < choices.Length; ++i)
                {
                    int x = xCenter - wChoice / 2 + wChoice * i;

                    bool sel = (selectedIndex == i);

                    FontDraw.drawFontHvFullShadowAligned(Video.VGAScreen, x, yChoice, choices[i], Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, 15, (sbyte)(sel ? -2 : -4), true, 1);
                }

                // Draw foreground snowflakes.
                for (int i = flakes.Length * 2 / 3; i < flakes.Length; ++i)
                    Sprites.blit_sprite2_blend(Video.VGAScreen, flakes[i].x, flakes[i].y, Sprites.spriteSheet8, 226);

                if (restart)
                {
                    Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;

                    Palette.fade_palette(Palette.palettes[0], 10, 0, 255);

                    restart = false;
                }

                Mouse.JE_mouseStart();
                Video.JE_showVGA();
                Mouse.JE_mouseReplace();

                Keyboard.waitUntilElapsed();

                Nortsong.setFrameCount(1);

                if (Keyboard.hasInput(InputFlags.INPUT_ANY))
                    break;
            }

            // Handle interaction.

            bool action = false;
            bool cancel = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mouseInput))
            {
                // Find choice that was hovered or clicked.
                if (mouseInput.y >= yChoice && mouseInput.y < yChoice + hChoice)
                {
                    for (int i = 0; i < choices.Length; ++i)
                    {
                        int xChoice = xCenter - wChoice + wChoice * i;
                        if (mouseInput.x >= xChoice && mouseInput.x < xChoice + wChoice)
                        {
                            selectedIndex = i;

                            if (mouseInput.button == SdlKeys.SDL_BUTTON_LEFT &&
                                mouseInput.x >= xChoice && mouseInput.x < xChoice + wChoice &&
                                mouseInput.y >= yChoice && mouseInput.y < yChoice + hChoice)
                            {
                                action = true;
                            }

                            break;
                        }
                    }
                }

                if (mouseInput.button == SdlKeys.SDL_BUTTON_RIGHT)
                {
                    cancel = true;
                }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                switch (keyboardInput.scancode)
                {
                case SdlKeys.SDL_SCANCODE_LEFT:
                {
                    selectedIndex = selectedIndex == 0
                        ? choices.Length - 1
                        : selectedIndex - 1;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_RIGHT:
                {
                    selectedIndex = selectedIndex == choices.Length - 1
                        ? 0
                        : selectedIndex + 1;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_SPACE:
                case SdlKeys.SDL_SCANCODE_RETURN:
                {
                    action = true;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_ESCAPE:
                {
                    cancel = true;
                    break;
                }
                default:
                    break;
                }
            }

            if (action || cancel)
            {
                Palette.fade_black(10);

                return !cancel && selectedIndex == 0;
            }
        }
    }
}
