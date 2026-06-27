namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/jukebox.c —— 星空背景 (Starlib) + 曲名顯示 + 方向鍵換歌 + 各種快捷鍵。
/// 由 setupMenu 的 Jukebox 項進入。
///
/// 與原版差異：原版 starLibMain(&keyboardInput) 會在內部 keyboardGetInput 並回傳該事件，
/// 由 jukebox 與 starlib 共用；本專案的 Starlib.starLibMain() 已簡化為不消耗鍵盤佇列
/// （見 Starlib.cs 註解），故此處於 waitUntilElapsed 之後自行 keyboardGetInput 取得快捷鍵。
/// 行為對使用者等價（事件擷取時點相差一幀）。
/// </summary>
internal static unsafe class Jukebox
{
    public static void jukebox()  // FKA Setup.jukeboxGo
    {
        bool trigger_quit = false,  // true when user wants to quit
             quitting = false;

        bool hide_text = false;

        bool fade_looped_songs = true, fading_song = false;
        bool stopped = false;

        bool fx = false;
        int fx_num = 0;

        int palette_fade_steps = 15;

        int[,] diff = new int[256, 3];
        Palette.init_step_fade_palette(diff, Palette.vga_palette, 0, 255);

        Starlib.JE_starlib_init();

        int fade_volume = Nortsong.tyrMusicVolume;

        for (; ; )
        {
            if (!stopped && !Loudness.audio_disabled)
            {
                if (Lds.songlooped && fade_looped_songs)
                    fading_song = true;

                if (fading_song)
                {
                    if (fade_volume > 5)
                    {
                        fade_volume -= 2;
                    }
                    else
                    {
                        fade_volume = Nortsong.tyrMusicVolume;

                        fading_song = false;
                    }

                    Loudness.set_volume((byte)fade_volume, (byte)Nortsong.fxVolume);
                }

                if (!Lds.playing || (Lds.songlooped && fade_looped_songs && !fading_song))
                    Loudness.play_song(MtRand.mt_rand() % (uint)Musmast.MUSIC_NUM);
            }

            Nortsong.setFrameCount(1);

            Sdl.SDL_FillRect(Video.VGAScreen, null, 0);

            Starlib.starLibMain();

            if (!hide_text)
            {
                string buffer;

                if (fx)
                    buffer = $"{fx_num + 1} {Sndmast.soundTitle[fx_num]}";
                else
                    buffer = $"{Loudness.song_playing + 1} {Musmast.musicTitle[(int)Loudness.song_playing]}";

                int x = Video.VGAScreen.w / 2;

                FontDraw.drawFontHvAligned(Video.VGAScreen, x, 170, "Press ESC to quit the jukebox.",           Font.FONT_SMALL, FontAlignment.ALIGN_CENTER, 1, 0);
                FontDraw.drawFontHvAligned(Video.VGAScreen, x, 180, "Arrow keys change the song being played.", Font.FONT_SMALL, FontAlignment.ALIGN_CENTER, 1, 0);
                FontDraw.drawFontHvAligned(Video.VGAScreen, x, 190, buffer,                                     Font.FONT_SMALL, FontAlignment.ALIGN_CENTER, 1, 4);
            }

            if (palette_fade_steps > 0)
                Palette.step_fade_palette(diff, palette_fade_steps--, 0, 255);

            Video.JE_showVGA();

            Keyboard.waitUntilElapsed();

            // Quit on mouse click.
            if (Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out _))
                trigger_quit = true;

            if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                uint combo = SdlKeys.KEY_COMBO(keyboardInput.mod, keyboardInput.scancode);

                if (combo == SC(SdlKeys.SDL_SCANCODE_ESCAPE) ||
                    combo == SC(SdlKeys.SDL_SCANCODE_Q) || combo == SH(SdlKeys.SDL_SCANCODE_Q))
                {
                    trigger_quit = true;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_SPACE))
                {
                    hide_text = !hide_text;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_F) || combo == SH(SdlKeys.SDL_SCANCODE_F))
                {
                    fading_song = !fading_song;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_N) || combo == SH(SdlKeys.SDL_SCANCODE_N))
                {
                    fade_looped_songs = !fade_looped_songs;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_V) || combo == SH(SdlKeys.SDL_SCANCODE_V))
                {
                    // Not implemented.
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_T) || combo == SH(SdlKeys.SDL_SCANCODE_T))
                {
                    // Not implemented.
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_SLASH))
                {
                    fx = !fx;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_COMMA))
                {
                    if (fx && --fx_num < 0)
                        fx_num = Sndmast.SOUND_COUNT - 1;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_PERIOD))
                {
                    if (fx && ++fx_num >= Sndmast.SOUND_COUNT)
                        fx_num = 0;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_SEMICOLON))
                {
                    if (fx)
                        Nortsong.JE_playSampleNum((byte)(fx_num + 1));
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_LEFT) || combo == SC(SdlKeys.SDL_SCANCODE_UP))
                {
                    Loudness.play_song((Loudness.song_playing > 0 ? Loudness.song_playing : (uint)Musmast.MUSIC_NUM) - 1);
                    stopped = false;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_RETURN) ||
                         combo == SC(SdlKeys.SDL_SCANCODE_RIGHT) || combo == SC(SdlKeys.SDL_SCANCODE_DOWN))
                {
                    Loudness.play_song((Loudness.song_playing + 1) % (uint)Musmast.MUSIC_NUM);
                    stopped = false;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_S) || combo == SH(SdlKeys.SDL_SCANCODE_S))
                {
                    Loudness.stop_song();
                    stopped = true;
                }
                else if (combo == SC(SdlKeys.SDL_SCANCODE_R) || combo == SH(SdlKeys.SDL_SCANCODE_R))
                {
                    Loudness.restart_song();
                    stopped = false;
                }
            }

            // user wants to quit, start fade-out
            if (trigger_quit && !quitting)
            {
                palette_fade_steps = 15;

                SDL_Color black = new(0, 0, 0);
                Palette.init_step_fade_solid(diff, black, 0, 255);

                quitting = true;
            }

            // if fade-out finished, we can finally quit
            if (quitting && palette_fade_steps == 0)
                break;
        }

        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
    }

    // KEY_COMBO(KMOD_NONE, scancode) — 無修飾鍵
    private static uint SC(int scancode) => SdlKeys.KEY_COMBO(SdlKeys.KMOD_NONE, scancode);

    // KEY_COMBO(KMOD_SHIFT, scancode)
    private static uint SH(int scancode) => SdlKeys.KEY_COMBO(SdlKeys.KMOD_SHIFT, scancode);
}
