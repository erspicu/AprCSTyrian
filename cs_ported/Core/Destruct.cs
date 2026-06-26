namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/destruct.c —— 毀滅模式（DESTRUCT）bonus 迷你遊戲。
/// 兩名玩家以 Scorched Earth 風格對戰。逐函式忠實對照原始 C 碼。
///
/// 註：原版大量使用全域變數與指標算術；C# 端以 class（參考型別）陣列取代
/// 指標走訪，並在像素層級操作（superPixel/aliasDirt/stabilityCheck 等）
/// 沿用 unsafe byte* 直接讀寫 SDL_Surface.pixels，與原始位元組行為一致。
///
/// load_destruct_config：OpenTyrian 的 ConfigSection/ConfigOption API 尚未移植，
/// 故此函式維持「全部使用預設值」之行為（等同設定檔不存在 / 全新設定）。
/// </summary>
internal static unsafe class Destruct
{
    /*** Defines ***/
    private const int UNIT_HEIGHT = 12;
    private const int MAX_KEY_OPTIONS = 4;

    /*** Enums (以 const int 表示，便於原始碼之算術運算) ***/
    // de_state_t
    private const int STATE_INIT = 0, STATE_RELOAD = 1, STATE_CONTINUE = 2;
    // de_player_t
    private const int PLAYER_LEFT = 0, PLAYER_RIGHT = 1, MAX_PLAYERS = 2;
    // de_team_t
    private const int TEAM_LEFT = 0, TEAM_RIGHT = 1, MAX_TEAMS = 2;
    // de_mode_t
    private const int MODE_5CARDWAR = 0, MODE_TRADITIONAL = 1, MODE_HELIASSAULT = 2,
                      MODE_HELIDEFENSE = 3, MODE_OUTGUNNED = 4, MODE_CUSTOM = 5,
                      MODE_FIRST = MODE_5CARDWAR, MODE_LAST = MODE_CUSTOM, MAX_MODES = 6, MODE_NONE = -1;
    // de_unit_t
    private const int UNIT_TANK = 0, UNIT_NUKE = 1, UNIT_DIRT = 2, UNIT_SATELLITE = 3,
                      UNIT_MAGNET = 4, UNIT_LASER = 5, UNIT_JUMPER = 6, UNIT_HELI = 7,
                      UNIT_FIRST = UNIT_TANK, UNIT_LAST = UNIT_HELI, MAX_UNITS = 8, UNIT_NONE = -1;
    // de_shot_t
    private const int SHOT_TRACER = 0, SHOT_SMALL = 1, SHOT_LARGE = 2, SHOT_MICRO = 3,
                      SHOT_SUPER = 4, SHOT_DEMO = 5, SHOT_SMALLNUKE = 6, SHOT_LARGENUKE = 7,
                      SHOT_SMALLDIRT = 8, SHOT_LARGEDIRT = 9, SHOT_MAGNET = 10, SHOT_MINILASER = 11,
                      SHOT_MEGALASER = 12, SHOT_LASERTRACER = 13, SHOT_MEGABLAST = 14, SHOT_MINI = 15,
                      SHOT_BOMB = 16, SHOT_FIRST = SHOT_TRACER, SHOT_LAST = SHOT_BOMB,
                      MAX_SHOT_TYPES = 17, SHOT_INVALID = -1;
    // de_expl_t
    private const int EXPL_NONE = 0, EXPL_MAGNET = 1, EXPL_DIRT = 2, EXPL_NORMAL = 3;
    // de_trails_t
    private const int TRAILS_NONE = 0, TRAILS_NORMAL = 1, TRAILS_FULL = 2;
    // de_pixel_t
    private const byte PIXEL_BLACK = 0, PIXEL_DIRT = 25;
    // de_mapflags_t
    private const int MAP_NORMAL = 0x00, MAP_WALLS = 0x01, MAP_RINGS = 0x02,
                      MAP_HOLES = 0x04, MAP_FUZZY = 0x08, MAP_TALL = 0x10;
    // de_keys_t
    private const int KEY_LEFT = 0, KEY_RIGHT = 1, KEY_UP = 2, KEY_DOWN = 3,
                      KEY_CHANGE = 4, KEY_FIRE = 5, KEY_CYUP = 6, KEY_CYDN = 7, MAX_KEY = 8;
    // de_move_t
    private const int MOVE_LEFT = 0, MOVE_RIGHT = 1, MOVE_UP = 2, MOVE_DOWN = 3,
                      MOVE_CHANGE = 4, MOVE_FIRE = 5, MOVE_CYUP = 6, MOVE_CYDN = 7, MAX_MOVE = 8;

    /*** Structs ***/
    private sealed class destruct_config_s
    {
        public uint max_shots;
        public uint min_walls;
        public uint max_walls;
        public uint max_explosions;
        public uint max_installations;
        public bool allow_custom;
        public bool alwaysalias;
        public bool[] jumper_straight = new bool[2];
        public bool[] ai = new bool[2];
    }

    private sealed class destruct_unit_s
    {
        public uint unitX;       /* yep, one's an int and the other is a real */
        public float unitY;
        public float unitYMov;
        public bool isYInAir;

        public int unitType;     /* de_unit_t */
        public int shotType;     /* de_shot_t */

        public float angle;
        public float power;

        public int lastMove;
        public uint ani_frame;
        public int health;
    }

    private sealed class destruct_shot_s
    {
        public bool isAvailable;

        public float x;
        public float y;
        public float xmov;
        public float ymov;
        public bool gravity;
        public uint shottype;
        public uint[] trailx = new uint[4];
        public uint[] traily = new uint[4];
        public uint[] trailc = new uint[4];
    }

    private sealed class destruct_explo_s
    {
        public bool isAvailable;

        public uint x, y;
        public uint explowidth;
        public uint explomax;
        public uint explofill;
        public int exploType;    /* de_expl_t */
    }

    private sealed class destruct_moves_s
    {
        public bool[] actions = new bool[MAX_MOVE];
    }

    private sealed class destruct_keys_s
    {
        public int[,] Config = new int[MAX_KEY, MAX_KEY_OPTIONS];  /* SDL_Scancode */
    }

    private sealed class destruct_ai_s
    {
        public int c_Angle, c_Power, c_Fire;
        public uint c_noDown;
    }

    private sealed class destruct_player_s
    {
        public bool is_cpu;
        public destruct_ai_s aiMemory = new();

        public destruct_unit_s[] unit = System.Array.Empty<destruct_unit_s>();
        public destruct_moves_s moves = new();
        public destruct_keys_s keys = new();

        public int team;         /* de_team_t */
        public uint unitsRemaining;
        public uint unitSelected;
        public uint shotDelay;
        public uint score;
    }

    private sealed class destruct_wall_s
    {
        public bool wallExist;
        public uint wallX, wallY;
    }

    private sealed class destruct_world_s
    {
        public uint[] baseMap = new uint[320];
        public SDL_Surface VGAScreen = null!;
        public destruct_wall_s[] mapWalls = System.Array.Empty<destruct_wall_s>();

        public int destructMode; /* de_mode_t */
        public uint mapFlags;
    }

    /*** Weapon configurations ***/
    private static readonly bool[] demolish = { false, false, false, false, false, true, true, true, false, false, false, false, true, false, true, false, true };
    private static readonly int[] shotTrail = { TRAILS_NONE, TRAILS_NONE, TRAILS_NONE, TRAILS_NORMAL, TRAILS_NORMAL, TRAILS_NORMAL, TRAILS_FULL, TRAILS_FULL, TRAILS_NONE, TRAILS_NONE, TRAILS_NONE, TRAILS_NORMAL, TRAILS_FULL, TRAILS_NORMAL, TRAILS_FULL, TRAILS_NORMAL, TRAILS_NONE };
    private static readonly int[] shotDelay = { 10, 30, 80, 20, 60, 100, 140, 200, 20, 60, 5, 15, 50, 5, 80, 16, 0 };
    private static readonly int[] shotSound = { Sndmast.S_SELECT, Sndmast.S_WEAPON_2, Sndmast.S_WEAPON_1, Sndmast.S_WEAPON_7, Sndmast.S_WEAPON_7, Sndmast.S_EXPLOSION_9, Sndmast.S_EXPLOSION_22, Sndmast.S_EXPLOSION_22, Sndmast.S_WEAPON_5, Sndmast.S_WEAPON_13, Sndmast.S_WEAPON_10, Sndmast.S_WEAPON_15, Sndmast.S_WEAPON_15, Sndmast.S_WEAPON_26, Sndmast.S_WEAPON_14, Sndmast.S_WEAPON_7, Sndmast.S_WEAPON_7 };
    private static readonly int[] exploSize = { 4, 20, 30, 14, 22, 16, 40, 60, 10, 30, 0, 5, 10, 3, 15, 7, 0 };
    private static readonly bool[] shotBounce = { false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, false, true };
    private static readonly int[] exploDensity = { 2, 5, 10, 15, 20, 15, 25, 30, 40, 80, 0, 30, 30, 4, 30, 5, 0 };
    private static readonly int[] shotDirt = { EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_DIRT, EXPL_DIRT, EXPL_MAGNET, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NORMAL, EXPL_NONE };
    private static readonly int[] shotColor = { 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 10, 10, 10, 10, 16, 0 };

    private static readonly int[] defaultWeapon = { SHOT_SMALL, SHOT_MICRO, SHOT_SMALLDIRT, SHOT_INVALID, SHOT_MAGNET, SHOT_MINILASER, SHOT_MICRO, SHOT_MINI };
    private static readonly int[] defaultCpuWeapon = { SHOT_SMALL, SHOT_MICRO, SHOT_DEMO, SHOT_INVALID, SHOT_MAGNET, SHOT_MINILASER, SHOT_MICRO, SHOT_MINI };
    private static readonly int[] defaultCpuWeaponB = { SHOT_DEMO, SHOT_SMALLNUKE, SHOT_DEMO, SHOT_INVALID, SHOT_MAGNET, SHOT_MEGALASER, SHOT_MICRO, SHOT_MINI };
    private static readonly int[] systemAngle = { 1, 1, 1, 0, 0, 1, 0, 0 };  // bool as int
    private static readonly int[] baseDamage = { 200, 120, 400, 300, 80, 150, 600, 40 };
    private static readonly int[] systemAni = { 0, 0, 0, 1, 0, 0, 0, 1 };    // bool as int

    private static readonly bool[,] weaponSystems = new bool[MAX_UNITS, MAX_SHOT_TYPES]
    {
        { true, true, true, false, false, true, false, false, false, false, false, false, false, false, false, false, false }, // normal
        { false, false, false, true, true, true, true, true, false, false, false, false, false, false, false, false, false }, // nuke
        { false, false, false, false, false, true, false, false, true, true, false, false, false, false, false, false, false }, // dirt
        { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }, // worthless
        { false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false }, // magnet
        { false, false, false, false, false, false, false, false, false, false, false, true, true, false, false, false, false }, // laser
        { true, false, false, true, false, false, false, false, false, false, false, false, false, false, true, false, false }, // jumper
        { true, false, false, false, false, true, false, false, false, false, false, false, false, false, false, true, false }  // helicopter
    };

    /* Music that destruct will play. */
    private static readonly byte[] goodsel = { 1, 2, 6, 12, 13, 14, 17, 23, 24, 26, 28, 29, 32, 33 };

    /* Unit creation. [.,0] is amount of units */
    private static readonly byte[,] basetypes = new byte[10, 11]
    {
        { 5, UNIT_TANK, UNIT_TANK, UNIT_NUKE, UNIT_DIRT, UNIT_DIRT, UNIT_SATELLITE, UNIT_MAGNET, UNIT_LASER, UNIT_JUMPER, UNIT_HELI },   /*Normal*/
        { 1, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK },             /*Traditional*/
        { 4, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI },             /*Weak Heli attack fleet*/
        { 8, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_NUKE, UNIT_NUKE, UNIT_NUKE, UNIT_DIRT, UNIT_MAGNET, UNIT_LASER, UNIT_JUMPER },        /*Strong Heli defense fleet*/
        { 8, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI, UNIT_HELI },             /*Strong Heli attack fleet*/
        { 4, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_TANK, UNIT_NUKE, UNIT_NUKE, UNIT_DIRT, UNIT_MAGNET, UNIT_JUMPER, UNIT_JUMPER },       /*Weak Heli defense fleet*/
        { 8, UNIT_TANK, UNIT_NUKE, UNIT_DIRT, UNIT_SATELLITE, UNIT_MAGNET, UNIT_LASER, UNIT_JUMPER, UNIT_HELI, UNIT_TANK, UNIT_NUKE },   /*Overpowering fleet*/
        { 4, UNIT_TANK, UNIT_TANK, UNIT_NUKE, UNIT_DIRT, UNIT_TANK, UNIT_LASER, UNIT_JUMPER, UNIT_HELI, UNIT_NUKE, UNIT_JUMPER },        /*Weak fleet*/
        { 5, UNIT_TANK, UNIT_TANK, UNIT_NUKE, UNIT_DIRT, UNIT_DIRT, UNIT_SATELLITE, UNIT_MAGNET, UNIT_LASER, UNIT_JUMPER, UNIT_HELI },   /*Left custom*/
        { 5, UNIT_TANK, UNIT_TANK, UNIT_NUKE, UNIT_DIRT, UNIT_DIRT, UNIT_SATELLITE, UNIT_MAGNET, UNIT_LASER, UNIT_JUMPER, UNIT_HELI }    /*Right custom*/
    };

    private static readonly uint[,] baseLookup = new uint[MAX_PLAYERS, MAX_MODES]
    {
        { 0, 1, 3, 4, 6, 8 },
        { 0, 1, 2, 5, 7, 9 }
    };

    private static readonly int[,] GraphicBase = new int[MAX_PLAYERS, MAX_UNITS]
    {
        { 1, 6, 11, 58, 63, 68, 96, 153 },
        { 20, 25, 30, 77, 82, 87, 115, 172 }
    };

    private static readonly int[,] ModeScore = new int[MAX_PLAYERS, MAX_MODES]
    {
        { 1, 0, 0, 5, 0, 1 },
        { 1, 0, 5, 0, 1, 1 }
    };

    private static readonly int[,,] defaultKeyConfig = new int[MAX_PLAYERS, MAX_KEY, MAX_KEY_OPTIONS]
    {
        {
            { SdlKeys.SDL_SCANCODE_C, 0, 0, 0 },
            { SdlKeys.SDL_SCANCODE_V, 0, 0, 0 },
            { SdlKeys.SDL_SCANCODE_A, 0, 0, 0 },
            { SdlKeys.SDL_SCANCODE_Z, 0, 0, 0 },
            { SdlKeys.SDL_SCANCODE_LALT, 0, 0, 0 },
            { SdlKeys.SDL_SCANCODE_X, SdlKeys.SDL_SCANCODE_LSHIFT, 0, 0 },
            { SdlKeys.SDL_SCANCODE_LCTRL, 0, 0, 0 },
            { SdlKeys.SDL_SCANCODE_SPACE, 0, 0, 0 }
        },
        {
            { SdlKeys.SDL_SCANCODE_LEFT, SdlKeys.SDL_SCANCODE_KP_4, 0, 0 },
            { SdlKeys.SDL_SCANCODE_RIGHT, SdlKeys.SDL_SCANCODE_KP_6, 0, 0 },
            { SdlKeys.SDL_SCANCODE_UP, SdlKeys.SDL_SCANCODE_KP_8, 0, 0 },
            { SdlKeys.SDL_SCANCODE_DOWN, SdlKeys.SDL_SCANCODE_KP_2, 0, 0 },
            { SdlKeys.SDL_SCANCODE_BACKSLASH, SdlKeys.SDL_SCANCODE_KP_5, 0, 0 },
            { SdlKeys.SDL_SCANCODE_INSERT, SdlKeys.SDL_SCANCODE_RETURN, SdlKeys.SDL_SCANCODE_KP_0, SdlKeys.SDL_SCANCODE_KP_ENTER },
            { SdlKeys.SDL_SCANCODE_PAGEUP, SdlKeys.SDL_SCANCODE_KP_9, 0, 0 },
            { SdlKeys.SDL_SCANCODE_PAGEDOWN, SdlKeys.SDL_SCANCODE_KP_3, 0, 0 }
        }
    };

    /*** Globals ***/
    private static SDL_Surface destructTempScreen = null!;
    private static bool destructFirstTime;

    private static readonly destruct_config_s config = new()
    {
        max_shots = 40, min_walls = 20, max_walls = 20, max_explosions = 40, max_installations = 10,
        allow_custom = false, alwaysalias = false,
        jumper_straight = new[] { true, false }, ai = new[] { true, false }
    };
    private static readonly destruct_player_s[] destruct_player = { new(), new() };
    private static readonly destruct_world_s world = new();
    private static destruct_shot_s[] shotRec = System.Array.Empty<destruct_shot_s>();
    private static destruct_explo_s[] exploRec = System.Array.Empty<destruct_explo_s>();

    /* DE_RunTick static state */
    private static uint runTick_endDelay;
    /* JE_eSound static state */
    private static int exploSoundChannel = 0;

    /* libc rand() 替身：destruct 僅在 DE_generateWalls 用一次，且預設
     * max_walls==min_walls 時 (rand()%1)==0，結果無關。提供自足 LCG。 */
    private static uint c_rand_state = 1;
    // 公開供 Xmas（聖誕雪花）使用：原版 xmas.c 與 destruct.c 同樣呼叫 libc rand()，
    // 共用同一全域序列，故此處共用同一 c_rand 狀態以保持行為一致。
    internal static int c_rand()
    {
        c_rand_state = c_rand_state * 1103515245u + 12345u;
        return (int)((c_rand_state >> 16) & 0x7fff);
    }

    /* C roundf：四捨五入（half away from zero）。 */
    private static float roundf(float x) => MathF.Round(x, MidpointRounding.AwayFromZero);

    private static readonly string[] unit_names =
    {
        "tank", "nuke", "dirt", "satellite", "magnet", "laser", "jumper", "heli"
    };

    private static int get_unit_by_name(string unit_name)
    {
        for (int unit = UNIT_FIRST; unit < MAX_UNITS; ++unit)
            if (unit_name == unit_names[unit])
                return unit;

        return UNIT_NONE;
    }

    private static void load_destruct_config()
    {
        /* OpenTyrian 之 ConfigSection/ConfigOption API 尚未移植到本專案。
         * 在設定檔不存在（或全新設定）時，原版行為等同保留所有預設值：
         *  - config 已以建構式初始化為原版預設。
         *  - weaponSystems[UNIT_LASER][SHOT_LASERTRACER] 預設為 false。
         *  - defaultKeyConfig 已內建預設鍵位。
         *  - 自訂模式停用（allow_custom == false），basetypes[8/9] 維持預設。
         * 故此處不需額外動作。 */
    }

    /*** Startup ***/

    public static void JE_destructGame()
    {
        uint i;

        /* This is the entry function. */
        Video.JE_clr256(Video.VGAScreen);
        Video.JE_showVGA();

        load_destruct_config();

        // 配置可變大小的結構
        shotRec = new destruct_shot_s[config.max_shots];
        for (i = 0; i < config.max_shots; i++) shotRec[i] = new destruct_shot_s();
        exploRec = new destruct_explo_s[config.max_explosions];
        for (i = 0; i < config.max_explosions; i++) exploRec[i] = new destruct_explo_s();
        world.mapWalls = new destruct_wall_s[config.max_walls];
        for (i = 0; i < config.max_walls; i++) world.mapWalls[i] = new destruct_wall_s();

        // 配置足夠涵蓋本次所有需求的結構數量
        for (i = 0; i < 10; i++)
            config.max_installations = Math.Max(config.max_installations, basetypes[i, 0]);
        destruct_player[PLAYER_LEFT].unit = new destruct_unit_s[config.max_installations];
        destruct_player[PLAYER_RIGHT].unit = new destruct_unit_s[config.max_installations];
        for (i = 0; i < config.max_installations; i++)
        {
            destruct_player[PLAYER_LEFT].unit[i] = new destruct_unit_s();
            destruct_player[PLAYER_RIGHT].unit[i] = new destruct_unit_s();
        }

        destructTempScreen = Video.game_screen;
        world.VGAScreen = Video.VGAScreen;

        Sprites.JE_loadCompShapes(ref Sprites.destructSpriteSheet, '~');

        Palette.fade_black(1);

        JE_destructMain();

        Sprites.free_sprite2s(ref Sprites.destructSpriteSheet);
    }

    private static void JE_destructMain()
    {
        int curState;

        Picload.JE_loadPic(Video.VGAScreen, 11, false);
        JE_introScreen();

        DE_ResetPlayers();

        destruct_player[PLAYER_LEFT].is_cpu = config.ai[PLAYER_LEFT];
        destruct_player[PLAYER_RIGHT].is_cpu = config.ai[PLAYER_RIGHT];

        while (true)
        {
            world.destructMode = JE_modeSelect();

            if (world.destructMode == MODE_NONE)
                break; /* User is quitting */

            do
            {
                destructFirstTime = true;
                Picload.JE_loadPic(Video.VGAScreen, 11, false);

                DE_ResetUnits();
                DE_ResetLevel();
                do
                {
                    curState = DE_RunTick();
                } while (curState == STATE_CONTINUE);

                Palette.fade_black(25);
            } while (curState == STATE_RELOAD);
        }
    }

    private static void JE_introScreen()
    {
        ScreenCopy(Video.VGAScreen, Video.VGAScreen2);
        Fonthand.JE_outText(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.specialName[7], (uint)Sprites.TINY_FONT), 90, Helptext.specialName[7], 12, 5);
        Fonthand.JE_outText(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.miscText[64], (uint)Sprites.TINY_FONT), 180, Helptext.miscText[64], 15, 2);
        Fonthand.JE_outText(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.miscText[65], (uint)Sprites.TINY_FONT), 190, Helptext.miscText[65], 15, 2);
        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 15, 0, 255);

        while (true)
        {
            Nortsong.setFrameCount(1);

            Nortsong.delayUntilElapsed();

            Keyboard.handleSdlEvents();

            if (Keyboard.keyboardGetInput(out _))
                break;
        }

        Palette.fade_black(15);
        ScreenCopy(Video.VGAScreen2, Video.VGAScreen);
        Video.JE_showVGA();
    }

    /* JE_modeSelect: 列印 DESTRUCT 模式選單，回傳選擇的模式或 MODE_NONE。 */
    private static void DrawModeSelectMenu(int mode)
    {
        int i;

        for (i = 0; i < Helptext.DESTRUCT_MODES; i++)
            Fonthand.JE_textShade(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.destructModeName[i], (uint)Sprites.TINY_FONT), 82 + i * 12, Helptext.destructModeName[i], 12, (i == mode ? 4 : 0), Fonthand.FULL_SHADE);
        if (config.allow_custom == true)
            Fonthand.JE_textShade(Video.VGAScreen, Fonthand.JE_fontCenter("Custom", (uint)Sprites.TINY_FONT), 82 + i * 12, "Custom", 12, (i == mode ? 4 : 0), Fonthand.FULL_SHADE);
    }

    private static int JE_modeSelect()
    {
        int mode;

        ScreenCopy(Video.VGAScreen, Video.VGAScreen2);
        mode = MODE_5CARDWAR;

        // 畫出選單並淡入
        DrawModeSelectMenu(mode);

        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 15, 0, 255);

        /* 迴圈取得輸入 */
        while (true)
        {
            /* 每次重畫選單 */
            DrawModeSelectMenu(mode);
            Video.JE_showVGA();

            while (true)
            {
                Nortsong.setFrameCount(1);

                Nortsong.delayUntilElapsed();

                Keyboard.handleSdlEvents();

                if (Keyboard.keyboardHasInput())
                    break;
            }

            bool done = false;

            if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                switch (keyboardInput.scancode)
                {
                case SdlKeys.SDL_SCANCODE_ESCAPE:
                {
                    mode = MODE_NONE;
                    done = true;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_RETURN:
                {
                    done = true;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_UP:
                {
                    if (mode == MODE_FIRST)
                    {
                        if (config.allow_custom == true)
                            mode = MODE_LAST;
                        else
                            mode = MODE_LAST - 1;
                    }
                    else
                    {
                        mode--;
                    }
                    break;
                }
                case SdlKeys.SDL_SCANCODE_DOWN:
                {
                    if (mode >= MODE_LAST - 1)
                    {
                        if (config.allow_custom == true && mode == MODE_LAST - 1)
                            mode++;
                        else
                            mode = MODE_FIRST;
                    }
                    else
                    {
                        mode++;
                    }
                    break;
                }
                default:
                    break;
                }
            }

            if (done)
                break;
        }

        Palette.fade_black(15);
        ScreenCopy(Video.VGAScreen2, Video.VGAScreen);
        Video.JE_showVGA();
        return mode;
    }

    private static void JE_generateTerrain()
    {
        world.mapFlags = MAP_NORMAL;

        if (MtRand.mt_rand() % 2 == 0)
            world.mapFlags |= MAP_WALLS;
        if (MtRand.mt_rand() % 4 == 0)
            world.mapFlags |= MAP_HOLES;
        switch (MtRand.mt_rand() % 4)
        {
        case 0:
            world.mapFlags |= MAP_FUZZY;
            break;

        case 1:
            world.mapFlags |= MAP_TALL;
            break;

        case 2:
            world.mapFlags |= MAP_RINGS;
            break;
        }

        Loudness.play_song((uint)(goodsel[MtRand.mt_rand() % 14] - 1));

        DE_generateBaseTerrain(world.mapFlags, world.baseMap);
        DE_generateUnits(world.baseMap);
        DE_generateWalls(world);
        DE_drawBaseTerrain(world.baseMap);

        if ((world.mapFlags & MAP_RINGS) != 0)
            DE_generateRings(world.VGAScreen, PIXEL_DIRT);
        if ((world.mapFlags & MAP_HOLES) != 0)
            DE_generateRings(world.VGAScreen, PIXEL_BLACK);

        JE_aliasDirt(world.VGAScreen);
        Video.JE_showVGA();

        ScreenCopy(Video.VGAScreen, destructTempScreen);
    }

    private static void DE_generateBaseTerrain(uint mapFlags, uint[] baseWorld)
    {
        int i;
        uint newheight;
        int HeightMul;
        float sinewave, sinewave2, cosinewave, cosinewave2;

        /* 範圍約在 .01 到 0.07283... 之間 */
        sinewave = (float)(MtRand.mt_rand_lt1() * Opentyr.M_PI / 50 + 0.01f);
        sinewave2 = (float)(MtRand.mt_rand_lt1() * Opentyr.M_PI / 50 + 0.01f);
        cosinewave = (float)(MtRand.mt_rand_lt1() * Opentyr.M_PI / 50 + 0.01f);
        cosinewave2 = (float)(MtRand.mt_rand_lt1() * Opentyr.M_PI / 50 + 0.01f);
        HeightMul = 20;

        if ((mapFlags & MAP_FUZZY) != 0)
        {
            sinewave = (float)(Opentyr.M_PI - MtRand.mt_rand_lt1() * 0.3f);
            sinewave2 = (float)(Opentyr.M_PI - MtRand.mt_rand_lt1() * 0.3f);
        }
        if ((mapFlags & MAP_TALL) != 0)
        {
            HeightMul = 100;
        }

        for (i = 1; i <= 318; i++)
        {
            newheight = (uint)(roundf(MathF.Sin(sinewave * i) * HeightMul + MathF.Sin(sinewave2 * i) * 15 +
                                      MathF.Cos(cosinewave * i) * 10 + MathF.Sin(cosinewave2 * i) * 15) + 130);

            /* 限制範圍 */
            if (newheight < 40)
                newheight = 40;
            else if (newheight > 195)
                newheight = 195;
            baseWorld[i] = newheight;
        }
    }

    private static void DE_drawBaseTerrain(uint[] baseWorld)
    {
        int i;

        for (i = 1; i <= 318; i++)
        {
            Vga256d.JE_rectangle(Video.VGAScreen, i, (int)baseWorld[i], i, 199, PIXEL_DIRT);
        }
    }

    private static void DE_generateUnits(uint[] baseWorld)
    {
        int i, j;
        uint numSatellites;

        for (i = 0; i < MAX_PLAYERS; i++)
        {
            numSatellites = 0;
            destruct_player[i].unitsRemaining = 0;

            for (j = 0; j < basetypes[baseLookup[i, world.destructMode], 0]; j++)
            {
                destruct_unit_s u = destruct_player[i].unit[j];

                /* 玩家左右兩側生成位置不同 */
                if (i == PLAYER_LEFT)
                {
                    u.unitX = (MtRand.mt_rand() % 120) + 10;
                }
                else
                {
                    u.unitX = 320 - ((MtRand.mt_rand() % 120) + 22);
                }

                u.unitY = JE_placementPosition(u.unitX - 1, 14, baseWorld);
                u.unitType = basetypes[baseLookup[i, world.destructMode], (MtRand.mt_rand() % 10) + 1];

                /* 衛星是特例：無用、不算現役單位、不能整隊都是衛星 */
                if (u.unitType == UNIT_SATELLITE)
                {
                    if (numSatellites == basetypes[baseLookup[i, world.destructMode], 0])
                    {
                        u.unitType = UNIT_TANK;
                        destruct_player[i].unitsRemaining++;
                    }
                    else
                    {
                        u.unitY = 30 + (MtRand.mt_rand() % 40);
                        numSatellites++;
                    }
                }
                else
                {
                    destruct_player[i].unitsRemaining++;
                }

                /* 填入單位其餘數值 */
                u.lastMove = 0;
                u.unitYMov = 0;
                u.isYInAir = false;
                u.angle = 0;
                u.power = (u.unitType == UNIT_LASER) ? 6 : 3;
                u.shotType = defaultWeapon[u.unitType];
                u.health = baseDamage[u.unitType];
                u.ani_frame = 0;
            }
        }
    }

    private static void DE_generateWalls(destruct_world_s gameWorld)
    {
        int i, j;
        uint wallX;
        uint wallHeight, remainWalls;
        uint tries;
        bool isGood;

        if ((world.mapFlags & MAP_WALLS) == 0)
        {
            /* 全部清掉 */
            for (i = 0; i < config.max_walls; i++)
            {
                gameWorld.mapWalls[i].wallExist = false;
            }
            return;
        }

        remainWalls = (uint)((c_rand() % (config.max_walls - config.min_walls + 1)) + config.min_walls);

        do
        {
            /* 建一面牆，決定高度 */
            wallHeight = (MtRand.mt_rand() % 5) + 1;
            if (wallHeight > remainWalls)
            {
                wallHeight = remainWalls;
            }

            /* 找一個好位置放牆 */
            tries = 0;
            do
            {
                isGood = true;
                wallX = (MtRand.mt_rand() % 300) + 10;

                for (i = 0; i < MAX_PLAYERS; i++)
                {
                    for (j = 0; j < config.max_installations; j++)
                    {
                        if ((wallX > destruct_player[i].unit[j].unitX - 12) &&
                            (wallX < destruct_player[i].unit[j].unitX + 13))
                        {
                            isGood = false;
                            goto label_outer_break;
                        }
                    }
                }

label_outer_break:
                tries++;

            } while (isGood == false && tries < 5);

            /* 有了有效的 X，建立牆。 */
            for (i = 1; i <= wallHeight; i++)
            {
                gameWorld.mapWalls[remainWalls - i].wallExist = true;
                gameWorld.mapWalls[remainWalls - i].wallX = wallX;
                gameWorld.mapWalls[remainWalls - i].wallY = JE_placementPosition(wallX, 12, gameWorld.baseMap) - (uint)(14 * i);
            }

            remainWalls -= wallHeight;

        } while (remainWalls != 0);
    }

    private static void DE_generateRings(SDL_Surface screen, byte pixel)
    {
        int i, j;
        uint tempSize, rings;
        int tempPosX1, tempPosY1, tempPosX2, tempPosY2;
        float tempRadian;

        rings = MtRand.mt_rand() % 6 + 1;
        for (i = 1; i <= rings; i++)
        {
            tempPosX1 = (int)(MtRand.mt_rand() % 320);
            tempPosY1 = (int)(MtRand.mt_rand() % 160) + 20;
            tempSize = (MtRand.mt_rand() % 40) + 10;  /*Size*/

            for (j = 1; j <= tempSize * tempSize * 2; j++)
            {
                tempRadian = (float)(MtRand.mt_rand_lt1() * (2 * Opentyr.M_PI));
                tempPosY2 = tempPosY1 + (int)roundf(MathF.Cos(tempRadian) * (MtRand.mt_rand_lt1() * 0.1f + 0.9f) * tempSize);
                tempPosX2 = tempPosX1 + (int)roundf(MathF.Sin(tempRadian) * (MtRand.mt_rand_lt1() * 0.1f + 0.9f) * tempSize);
                if ((tempPosY2 > 12) && (tempPosY2 < 200) &&
                    (tempPosX2 > 0) && (tempPosX2 < 319))
                {
                    screen.pixels[tempPosX2 + tempPosY2 * screen.pitch] = pixel;
                }
            }
        }
    }

    private static uint aliasDirtPixel(SDL_Surface screen, int x, int y, byte* s)
    {
        uint newColor = PIXEL_BLACK;

        if ((y > 0) && (*(s - screen.pitch) == PIXEL_DIRT)) // look up
            newColor += 1;
        if ((y < screen.h - 1) && (*(s + screen.pitch) == PIXEL_DIRT)) // look down
            newColor += 3;
        if ((x > 0) && (*(s - 1) == PIXEL_DIRT)) // look left
            newColor += 2;
        if ((x < screen.pitch - 1) && (*(s + 1) == PIXEL_DIRT)) // look right
            newColor += 2;
        if (newColor != PIXEL_BLACK)
            return newColor + 16; // 16 是 brown pixels 的起點

        return PIXEL_BLACK;
    }

    private static void JE_aliasDirt(SDL_Surface screen)
    {
        int x, y;

        byte* s = screen.pixels;
        s += 12 * screen.pitch;

        for (y = 12; y < screen.h; y++)
        {
            for (x = 0; x < screen.pitch; x++)
            {
                if (*s == PIXEL_BLACK)
                    *s = (byte)aliasDirtPixel(screen, x, y, s);

                s++;
            }
        }
    }

    private static uint JE_placementPosition(uint passed_x, uint width, uint[] world)
    {
        uint i, new_y;

        new_y = 0;
        for (i = passed_x; i <= passed_x + width - 1; i++)
        {
            if (new_y < world[i])
                new_y = world[i];
        }

        for (i = passed_x; i <= passed_x + width - 1; i++)
        {
            world[i] = new_y;
        }

        return new_y;
    }

    private static bool JE_stabilityCheck(uint x, uint y)
    {
        int i;
        uint numDirtPixels;
        byte* s;

        numDirtPixels = 0;
        s = destructTempScreen.pixels;
        s += x + (y * (uint)destructTempScreen.pitch) - 1;

        /* 檢查物件底邊的 12 個像素 */
        for (i = 0; i < 12; i++)
        {
            if (*s == PIXEL_DIRT)
                numDirtPixels++;

            s++;
        }

        /* 少於 10 個棕色像素就不算是穩固地基 */
        return (numDirtPixels < 10);
    }

    private static void JE_tempScreenChecking() /*and copy to vgascreen*/
    {
        byte* s = Video.VGAScreen.pixels;
        s += 12 * Video.VGAScreen.pitch;

        byte* temps = destructTempScreen.pixels;
        temps += 12 * destructTempScreen.pitch;

        for (int y = 12; y < Video.VGAScreen.h; y++)
        {
            for (int x = 0; x < Video.VGAScreen.pitch; x++)
            {
                // 淡出爆炸。241..255 的調色盤由極暗紅淡至極亮黃。
                if (*temps >= 241)
                {
                    if (*temps == 241)
                        *temps = PIXEL_BLACK;
                    else
                        (*temps)--;
                }

                // 反鋸齒泥土
                if (config.alwaysalias == true && *temps == PIXEL_BLACK)
                    *temps = (byte)aliasDirtPixel(Video.VGAScreen, x, y, temps);

                /* 從 temp screen 複製到 VGAScreen */
                *s = *temps;

                s++;
                temps++;
            }
        }
    }

    private static void JE_makeExplosion(uint tempPosX, uint tempPosY, int shottype)
    {
        uint i, tempExploSize;

        /* 先找一個可用的爆炸槽，找不到就 return */
        for (i = 0; i < config.max_explosions; i++)
            if (exploRec[i].isAvailable == true)
                break;
        if (i == config.max_explosions) /* 沒有空槽 */
            return;

        exploRec[i].isAvailable = false;
        exploRec[i].x = tempPosX;
        exploRec[i].y = tempPosY;
        exploRec[i].explowidth = 2;

        if (shottype != SHOT_INVALID)
        {
            tempExploSize = (uint)exploSize[shottype];
            if (tempExploSize < 5)
                JE_eSound(3);
            else if (tempExploSize < 15)
                JE_eSound(4);
            else if (tempExploSize < 20)
                JE_eSound(12);
            else if (tempExploSize < 40)
                JE_eSound(11);
            else
            {
                JE_eSound(12);
                JE_eSound(11);
            }

            exploRec[i].explomax = tempExploSize;
            exploRec[i].explofill = (uint)exploDensity[shottype];
            exploRec[i].exploType = shotDirt[shottype];
        }
        else
        {
            JE_eSound(4);
            exploRec[i].explomax = (MtRand.mt_rand() % 40) + 10;
            exploRec[i].explofill = (MtRand.mt_rand() % 60) + 20;
            exploRec[i].exploType = EXPL_NORMAL;
        }
    }

    private static void JE_eSound(uint sound)
    {
        if (++exploSoundChannel > 5)
            exploSoundChannel = 1;

        Varz.soundQueue[exploSoundChannel] = (byte)sound;
    }

    private static readonly uint[,] superPixel_starPattern =
    {
        { 0, 0, 246, 0, 0 },
        { 0, 247, 249, 247, 0 },
        { 246, 249, 252, 249, 246 },
        { 0, 247, 249, 247, 0 },
        { 0, 0, 246, 0, 0 }
    };
    private static readonly uint[,] superPixel_starIntensity =
    {
        { 0, 0, 1, 0, 0 },
        { 0, 1, 2, 1, 0 },
        { 1, 2, 4, 2, 1 },
        { 0, 1, 2, 1, 0 },
        { 0, 0, 1, 0, 0 }
    };

    private static void JE_superPixel(uint tempPosX, uint tempPosY)
    {
        int x, y, maxX, maxY;
        int rowLen;
        byte* s;

        maxX = destructTempScreen.pitch;
        maxY = destructTempScreen.h;

        rowLen = destructTempScreen.pitch;
        s = destructTempScreen.pixels;
        s += (rowLen * ((int)tempPosY - 2)) + ((int)tempPosX - 2);

        for (y = 0; y < 5; y++, s += rowLen - 5)
        {
            if ((int)tempPosY + y - 2 < 0 ||    /* 超出邊界 */
                (int)tempPosY + y - 2 >= maxY)
            {
                continue;
            }

            for (x = 0; x < 5; x++, s++)
            {
                if ((int)tempPosX + x - 2 < 0 ||
                    (int)tempPosX + x - 2 >= maxX)
                {
                    continue;
                }

                if (superPixel_starPattern[y, x] == 0)
                    continue;  /* 加速用 */

                if (*s < superPixel_starPattern[y, x])
                    *s = (byte)superPixel_starPattern[y, x];
                else if (*s + superPixel_starIntensity[y, x] > 255)
                    *s = 255;
                else
                    *s += (byte)superPixel_starIntensity[y, x];
            }
        }
    }

    private static void JE_helpScreen()
    {
        int i, j;

        Palette.fade_black(15);
        ScreenCopy(Video.VGAScreen, Video.VGAScreen2);
        Video.JE_clr256(Video.VGAScreen);

        for (i = 0; i < 2; i++)
        {
            Fonthand.JE_outText(Video.VGAScreen, 100, 5 + i * 90, Helptext.destructHelp[i * 12 + 0], 2, 4);
            Fonthand.JE_outText(Video.VGAScreen, 100, 15 + i * 90, Helptext.destructHelp[i * 12 + 1], 2, 1);
            for (j = 3; j <= 12; j++)
                Fonthand.JE_outText(Video.VGAScreen, ((j - 1) % 2) * 160 + 10, 15 + ((j - 1) / 2) * 12 + i * 90, Helptext.destructHelp[i * 12 + j - 1], 1, 3);
        }
        Fonthand.JE_outText(Video.VGAScreen, 30, 190, Helptext.destructHelp[24], 3, 4);
        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 15, 0, 255);

        while (true)
        {
            Nortsong.setFrameCount(1);

            Nortsong.delayUntilElapsed();

            Keyboard.handleSdlEvents();

            if (Keyboard.keyboardGetInput(out _))
                break;
        }

        Palette.fade_black(15);
        ScreenCopy(Video.VGAScreen2, Video.VGAScreen);
        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 15, 0, 255);
    }

    private static void JE_pauseScreen()
    {
        Loudness.set_volume((byte)(Nortsong.tyrMusicVolume / 2), (byte)Nortsong.fxVolume);

        /* 暫存目前畫面/遊戲世界，暫停時別搞壞它。 */
        ScreenCopy(Video.VGAScreen, Video.VGAScreen2);
        Fonthand.JE_outText(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.miscText[22], (uint)Sprites.TINY_FONT), 90, Helptext.miscText[22], 12, 5);
        Video.JE_showVGA();

        while (true)
        {
            Nortsong.setFrameCount(1);

            Nortsong.delayUntilElapsed();

            Keyboard.handleSdlEvents();

            if (Keyboard.keyboardGetInput(out _))
                break;
        }

        /* 還原畫面與音量 */
        ScreenCopy(Video.VGAScreen2, Video.VGAScreen);
        Video.JE_showVGA();

        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
    }

    /* DE_ResetX: 清除其負責對象的狀態。 */
    private static void DE_ResetUnits()
    {
        int p, u;

        for (p = 0; p < MAX_PLAYERS; ++p)
            for (u = 0; u < config.max_installations; ++u)
                destruct_player[p].unit[u].health = 0;
    }

    private static void DE_ResetPlayers()
    {
        int i, k, o;

        for (i = 0; i < MAX_PLAYERS; ++i)
        {
            destruct_player[i].is_cpu = false;
            destruct_player[i].unitSelected = 0;
            destruct_player[i].shotDelay = 0;
            destruct_player[i].score = 0;
            destruct_player[i].aiMemory.c_Angle = 0;
            destruct_player[i].aiMemory.c_Power = 0;
            destruct_player[i].aiMemory.c_Fire = 0;
            destruct_player[i].aiMemory.c_noDown = 0;
            for (k = 0; k < MAX_KEY; ++k)
                for (o = 0; o < MAX_KEY_OPTIONS; ++o)
                    destruct_player[i].keys.Config[k, o] = defaultKeyConfig[i, k, o];
        }
    }

    private static void DE_ResetWeapons()
    {
        int i;

        for (i = 0; i < config.max_shots; i++)
            shotRec[i].isAvailable = true;

        for (i = 0; i < config.max_explosions; i++)
            exploRec[i].isAvailable = true;
    }

    private static void DE_ResetLevel()
    {
        /* 準備競技場 */

        DE_ResetWeapons();

        JE_generateTerrain();
        DE_ResetAI();
    }

    private static void DE_ResetAI()
    {
        int i, j;

        for (i = PLAYER_LEFT; i < MAX_PLAYERS; i++)
        {
            if (destruct_player[i].is_cpu == false)
                continue;

            for (j = 0; j < config.max_installations; j++)
            {
                destruct_unit_s ptr = destruct_player[i].unit[j];
                if (DE_isValidUnit(ptr) == false)
                    continue;

                if (systemAngle[ptr.unitType] != 0 || ptr.unitType == UNIT_HELI)
                    ptr.angle = (float)Opentyr.M_PI_4;
                else
                    ptr.angle = 0;

                ptr.power = (ptr.unitType == UNIT_LASER) ? 6 : 4;

                if ((world.mapFlags & MAP_WALLS) != 0)
                    ptr.shotType = defaultCpuWeaponB[ptr.unitType];
                else
                    ptr.shotType = defaultCpuWeapon[ptr.unitType];
            }
        }
    }

    private static void DE_ResetActions()
    {
        int i, k;

        for (i = 0; i < MAX_PLAYERS; i++)
        {
            for (k = 0; k < MAX_MOVE; k++)
                destruct_player[i].moves.actions[k] = false;
        }
    }

    /* DE_RunTick: 執行一個 tick。回傳遊戲狀態。 */
    private static int DE_RunTick()
    {
        Nortsong.setFrameCount(1);

        Array.Clear(Varz.soundQueue, 0, Varz.soundQueue.Length);
        JE_tempScreenChecking();

        DE_ResetActions();
        DE_RunTickCycleDeadUnits();

        DE_RunTickGravity();
        DE_RunTickAnimate();
        DE_RunTickDrawWalls();
        DE_RunTickExplosions();
        DE_RunTickShots();
        DE_RunTickAI();
        DE_RunTickDrawCrosshairs();
        DE_RunTickDrawHUD();
        Video.JE_showVGA();

        if (destructFirstTime)
        {
            Palette.fade_palette(Palette.colors, 25, 0, 255);
            destructFirstTime = false;
            runTick_endDelay = 0;
        }

        DE_RunTickGetInput();
        DE_ProcessInput();

        if (runTick_endDelay > 0)
        {
            if (--runTick_endDelay == 0)
                return STATE_RELOAD;
        }
        else if (DE_RunTickCheckEndgame() == true)
        {
            runTick_endDelay = 80;
        }

        DE_RunTickPlaySounds();

        Nortsong.delayUntilElapsed();

        Keyboard.keyboardClearInput();

        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F10])
        {
            destruct_player[PLAYER_LEFT].is_cpu = !destruct_player[PLAYER_LEFT].is_cpu;
            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F10] = false;
        }
        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F11])
        {
            destruct_player[PLAYER_RIGHT].is_cpu = !destruct_player[PLAYER_RIGHT].is_cpu;
            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F11] = false;
        }
        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_P])
        {
            JE_pauseScreen();
            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_P] = false;
        }

        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F1])
        {
            JE_helpScreen();
            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F1] = false;
        }

        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_ESCAPE])
        {
            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_ESCAPE] = false;
            return STATE_INIT; /* 回到模式選擇 */
        }

        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_BACKSPACE])
        {
            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_BACKSPACE] = false;
            return STATE_RELOAD; /* 建立新地圖 */
        }

        return STATE_CONTINUE;
    }

    private static void DE_RunTickCycleDeadUnits()
    {
        int i;

        /* 若現役單位被摧毀就自動切換，並跳過無用的衛星 */
        for (i = 0; i < MAX_PLAYERS; i++)
        {
            if (destruct_player[i].unitsRemaining == 0)
                continue;

            destruct_unit_s unit = destruct_player[i].unit[destruct_player[i].unitSelected];
            while (DE_isValidUnit(unit) == false ||
                   unit.shotType == SHOT_INVALID)
            {
                destruct_player[i].unitSelected++;
                if (destruct_player[i].unitSelected >= config.max_installations)
                {
                    destruct_player[i].unitSelected = 0;
                }
                unit = destruct_player[i].unit[destruct_player[i].unitSelected];
            }
        }
    }

    private static void DE_RunTickGravity()
    {
        int i, j;

        for (i = 0; i < MAX_PLAYERS; i++)
        {
            for (j = 0; j < config.max_installations; j++)
            {
                destruct_unit_s unit = destruct_player[i].unit[j];
                if (DE_isValidUnit(unit) == false)
                    continue;

                switch (unit.unitType)
                {
                case UNIT_SATELLITE: /* 衛星不會掉落 */
                    break;

                case UNIT_HELI:
                case UNIT_JUMPER:
                    if (unit.isYInAir == true) /* 理論上正在落下 */
                    {
                        DE_GravityFlyUnit(unit);
                        break;
                    }
                    /* 否則當作一般單位 / fall through */
                    DE_GravityLowerUnit(unit);
                    break;
                default:
                    DE_GravityLowerUnit(unit);
                    break;
                }

                /* 畫出單位。 */
                DE_GravityDrawUnit(i, unit);
            }
        }
    }

    private static void DE_GravityDrawUnit(int team, destruct_unit_s unit)
    {
        uint anim_index;

        anim_index = (uint)(GraphicBase[team, unit.unitType] + unit.ani_frame);
        if (unit.unitType == UNIT_HELI)
        {
            /* 依左右移動調整動畫索引。 */
            if (unit.lastMove < -2)
                anim_index += 5;
            else if (unit.lastMove > 2)
                anim_index += 10;
        }
        else /* 砲管類 */
        {
            anim_index += (uint)MathF.Floor((float)(unit.angle * 9.99f / Opentyr.M_PI));
        }

        Sprites.blit_sprite2(Video.VGAScreen, (int)unit.unitX, (int)roundf(unit.unitY) - 13, Sprites.destructSpriteSheet, anim_index);
    }

    private static void DE_GravityLowerUnit(destruct_unit_s unit)
    {
        if (unit.unitY < 199)  /* 在底部就不檢查 */
        {
            if (JE_stabilityCheck(unit.unitX, (uint)roundf(unit.unitY)))
            {
                switch (unit.unitType)
                {
                case UNIT_HELI:
                    unit.unitYMov = 1.5f;
                    unit.unitY += 0.2f;
                    break;

                default:
                    unit.unitY += 1;
                    break;
                }

                if (unit.unitY > 199)
                    unit.unitY = 199;
            }
        }
    }

    private static void DE_GravityFlyUnit(destruct_unit_s unit)
    {
        if (unit.unitY + unit.unitYMov > 199) /* 會撞到螢幕底部 */
        {
            unit.unitY = 199;
            unit.unitYMov = 0;
            unit.isYInAir = false;
            return;
        }

        /* 移動單位並調整加速度 */
        unit.unitY += unit.unitYMov;
        if (unit.unitY < 24) /* 防止單位跑到螢幕上方 */
        {
            unit.unitYMov = 0;
            unit.unitY = 24;
        }

        if (unit.unitType == UNIT_HELI) /* 直升機掉得較慢 */
            unit.unitYMov += 0.0001f;
        else
            unit.unitYMov += 0.03f;

        if (!JE_stabilityCheck(unit.unitX, (uint)roundf(unit.unitY)))
        {
            unit.unitYMov = 0;
            unit.isYInAir = false;
        }
    }

    private static void DE_RunTickAnimate()
    {
        int p, u;

        for (p = 0; p < MAX_PLAYERS; ++p)
        {
            for (u = 0; u < config.max_installations; ++u)
            {
                destruct_unit_s ptr = destruct_player[p].unit[u];
                /* 不要動到未配置、或不會動畫且 frame 為 0 的單位 */
                if (DE_isValidUnit(ptr) == false)
                    continue;
                if (systemAni[ptr.unitType] == 0 && ptr.ani_frame == 0)
                    continue;

                if (++(ptr.ani_frame) > 3)
                    ptr.ani_frame = 0;
            }
        }
    }

    private static void DE_RunTickDrawWalls()
    {
        int i;

        for (i = 0; i < config.max_walls; i++)
            if (world.mapWalls[i].wallExist)
                Sprites.blit_sprite2(Video.VGAScreen, (int)world.mapWalls[i].wallX, (int)world.mapWalls[i].wallY, Sprites.destructSpriteSheet, 42);
    }

    private static void DE_RunTickExplosions()
    {
        int i, j;
        int tempPosX, tempPosY;
        float tempRadian;

        /* 處理所有未排序的開放爆炸 */
        for (i = 0; i < config.max_explosions; i++)
        {
            if (exploRec[i].isAvailable == true)
                continue;  /* 沒事可做 */

            for (j = 0; j < exploRec[i].explofill; j++)
            {
                /* 一個爆炸由多道四散的火花組成，算出火花落點 */
                tempRadian = (float)(MtRand.mt_rand_lt1() * (2 * Opentyr.M_PI));
                tempPosY = (int)exploRec[i].y + (int)roundf(MathF.Cos(tempRadian) * MtRand.mt_rand_lt1() * exploRec[i].explowidth);
                tempPosX = (int)exploRec[i].x + (int)roundf(MathF.Sin(tempRadian) * MtRand.mt_rand_lt1() * exploRec[i].explowidth);

                /* 允許爆炸繞回（原是 bug，因好玩而保留），但避免越界陣列。 */
                while (tempPosX < 0)
                    tempPosX += 320;
                while (tempPosX > 320)
                    tempPosX -= 320;

                /* 垂直越界就不畫 */
                if (tempPosY >= 200 || tempPosY <= 15)
                    continue;

                switch (exploRec[i].exploType)
                {
                    case EXPL_DIRT:
                        destructTempScreen.pixels[tempPosX + tempPosY * destructTempScreen.pitch] = PIXEL_DIRT;
                        break;

                    case EXPL_NORMAL:
                        JE_superPixel((uint)tempPosX, (uint)tempPosY);
                        DE_TestExplosionCollision((uint)tempPosX, (uint)tempPosY);
                        break;

                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }
            }

            /* 擴大爆炸，必要時刪除。 */
            exploRec[i].explowidth++;
            if (exploRec[i].explowidth == exploRec[i].explomax)
            {
                exploRec[i].isAvailable = true;
            }
        }
    }

    private static void DE_TestExplosionCollision(uint PosX, uint PosY)
    {
        int i, j;

        for (i = PLAYER_LEFT; i < MAX_PLAYERS; i++)
        {
            for (j = 0; j < config.max_installations; j++)
            {
                destruct_unit_s unit = destruct_player[i].unit[j];
                if (DE_isValidUnit(unit) == true &&
                    PosX > unit.unitX && PosX < unit.unitX + 11 &&
                    PosY < unit.unitY && PosY > unit.unitY - 11)
                {
                    unit.health--;
                    if (unit.health <= 0)
                        DE_DestroyUnit(i, unit);
                }
            }
        }
    }

    private static void DE_DestroyUnit(int playerID, destruct_unit_s unit)
    {
        /* 直升機像 small shot 爆炸；Invalid 是特例。 */
        JE_makeExplosion(unit.unitX + 5, (uint)roundf(unit.unitY) - 5, (unit.unitType == UNIT_HELI) ? SHOT_SMALL : SHOT_INVALID);

        if (unit.unitType != UNIT_SATELLITE) /* 增加分數 */
        {
            destruct_player[playerID].unitsRemaining--;
            destruct_player[((playerID == PLAYER_LEFT) ? PLAYER_RIGHT : PLAYER_LEFT)].score++;
        }
    }

    private static void DE_RunTickShots()
    {
        int i, j, k;
        uint tempTrails;
        int tempPosX, tempPosY;

        for (i = 0; i < config.max_shots; i++)
        {
            if (shotRec[i].isAvailable == true)
                continue;  /* 沒事可做 */

            /* 移動子彈，單純位移 */
            shotRec[i].x += shotRec[i].xmov;
            shotRec[i].y += shotRec[i].ymov;

            /* 若可在地圖上反彈，就反彈 */
            if (shotBounce[shotRec[i].shottype])
            {
                if (shotRec[i].y > 199 || shotRec[i].y < 14)
                {
                    shotRec[i].y -= shotRec[i].ymov;
                    shotRec[i].ymov = -shotRec[i].ymov;
                }
                if (shotRec[i].x < 1 || shotRec[i].x > 318)
                {
                    shotRec[i].x -= shotRec[i].xmov;
                    shotRec[i].xmov = -shotRec[i].xmov;
                }
            }
            else /* 否則套用一般物理 */
            {
                shotRec[i].ymov += 0.05f; /* 加上重力 */

                if (shotRec[i].y > 199) /* 撞到地面 */
                {
                    shotRec[i].y -= shotRec[i].ymov;
                    shotRec[i].ymov = -shotRec[i].ymov * 0.8f; /* 以較低速反彈 */

                    /* 別讓反彈子彈垂直上下彈 */
                    if (shotRec[i].xmov == 0)
                        shotRec[i].xmov += MtRand.mt_rand_lt1() - 0.5f;
                }
            }

            /* 子彈越界，消滅它。 */
            if (shotRec[i].x > 318 || shotRec[i].x < 1)
            {
                shotRec[i].isAvailable = true;
                continue;
            }

            /* 檢查碰撞。 */

            /* 別檢查地圖上方的碰撞 :) */
            if (shotRec[i].y <= 14)
                continue;

            tempPosX = (int)roundf(shotRec[i].x);
            tempPosY = (int)roundf(shotRec[i].y);

            /*檢查建物命中*/
            for (j = 0; j < MAX_PLAYERS; j++)
            {
                for (k = 0; k < config.max_installations; k++)
                {
                    destruct_unit_s unit = destruct_player[j].unit[k];
                    if (DE_isValidUnit(unit) == false)
                        continue;

                    if (tempPosX > unit.unitX && tempPosX < unit.unitX + 11 &&
                        tempPosY < unit.unitY && tempPosY > unit.unitY - 13)
                    {
                        shotRec[i].isAvailable = true;
                        JE_makeExplosion((uint)tempPosX, (uint)tempPosY, (int)shotRec[i].shottype);
                    }
                }
            }

            tempTrails = (uint)((shotColor[shotRec[i].shottype] << 4) - 3);
            JE_pixCool((uint)tempPosX, (uint)tempPosY, (byte)tempTrails);

            /*畫出子彈軌跡（若有）*/
            switch (shotTrail[shotRec[i].shottype])
            {
            case TRAILS_NONE:
                break;
            case TRAILS_NORMAL:
                DE_DrawTrails(shotRec[i], 2, 4, tempTrails - 3);
                break;
            case TRAILS_FULL:
                DE_DrawTrails(shotRec[i], 4, 3, tempTrails - 1);
                break;
            }

            /* 反彈或摧毀牆 */
            for (j = 0; j < config.max_walls; j++)
            {
                if (world.mapWalls[j].wallExist == true &&
                    tempPosX >= world.mapWalls[j].wallX && tempPosX <= world.mapWalls[j].wallX + 11 &&
                    tempPosY >= world.mapWalls[j].wallY && tempPosY <= world.mapWalls[j].wallY + 14)
                {
                    if (demolish[shotRec[i].shottype])
                    {
                        /* 炸掉牆並移除子彈。 */
                        world.mapWalls[j].wallExist = false;
                        shotRec[i].isAvailable = true;
                        JE_makeExplosion((uint)tempPosX, (uint)tempPosY, (int)shotRec[i].shottype);
                        continue;
                    }
                    else
                    {
                        /* 否則反彈。 */
                        if (shotRec[i].x - shotRec[i].xmov < world.mapWalls[j].wallX ||
                            shotRec[i].x - shotRec[i].xmov > world.mapWalls[j].wallX + 11)
                        {
                            shotRec[i].xmov = -shotRec[i].xmov;
                        }
                        if (shotRec[i].y - shotRec[i].ymov < world.mapWalls[j].wallY ||
                            shotRec[i].y - shotRec[i].ymov > world.mapWalls[j].wallY + 14)
                        {
                            if (shotRec[i].ymov < 0)
                                shotRec[i].ymov = -shotRec[i].ymov;
                            else
                                shotRec[i].ymov = -shotRec[i].ymov * 0.8f;
                        }

                        tempPosX = (int)roundf(shotRec[i].x);
                        tempPosY = (int)roundf(shotRec[i].y);
                    }
                }
            }

            /* 最後的碰撞檢查：撞到泥土。 */
            if ((destructTempScreen.pixels[tempPosX + tempPosY * destructTempScreen.pitch]) == PIXEL_DIRT)
            {
                shotRec[i].isAvailable = true;
                JE_makeExplosion((uint)tempPosX, (uint)tempPosY, (int)shotRec[i].shottype);
                continue;
            }
        }
    }

    private static void DE_DrawTrails(destruct_shot_s shot, int count, uint decay, uint startColor)
    {
        int i;

        for (i = count - 1; i >= 0; i--) /* 反向很重要，影響繪製方式 */
        {
            if (shot.trailc[i] > 0 && shot.traily[i] > 12) /* 存在且未越界就畫 */
            {
                JE_pixCool(shot.trailx[i], shot.traily[i], (byte)shot.trailc[i]);
            }

            if (i == 0) /* 我們建立的第一段軌跡。 */
            {
                shot.trailx[i] = (uint)roundf(shot.x);
                shot.traily[i] = (uint)roundf(shot.y);
                shot.trailc[i] = startColor;
            }
            else /* 較新的軌跡衰減成較舊的軌跡。 */
            {
                shot.trailx[i] = shot.trailx[i - 1];
                shot.traily[i] = shot.traily[i - 1];
                if (shot.trailc[i - 1] > 0)
                {
                    shot.trailc[i] = shot.trailc[i - 1] - decay;
                }
            }
        }
    }

    private static void DE_RunTickAI()
    {
        int i, j;

        for (i = 0; i < MAX_PLAYERS; i++)
        {
            destruct_player_s ptrPlayer = destruct_player[i];
            if (ptrPlayer.is_cpu == false)
                continue;

            j = i + 1;
            if (j >= MAX_PLAYERS)
                j = 0;

            destruct_player_s ptrTarget = destruct_player[j];
            destruct_unit_s ptrCurUnit = ptrPlayer.unit[ptrPlayer.unitSelected];

            /* 原始 AI 開始。 */

            if (ptrPlayer.aiMemory.c_noDown > 0)
                ptrPlayer.aiMemory.c_noDown--;

            if (MtRand.mt_rand() % 100 > 80)
            {
                ptrPlayer.aiMemory.c_Angle += (int)(MtRand.mt_rand() % 3) - 1;

                if (ptrPlayer.aiMemory.c_Angle > 1)
                    ptrPlayer.aiMemory.c_Angle = 1;
                else if (ptrPlayer.aiMemory.c_Angle < -1)
                    ptrPlayer.aiMemory.c_Angle = -1;
            }
            if (MtRand.mt_rand() % 100 > 90)
            {
                if (ptrPlayer.aiMemory.c_Angle > 0 && ptrCurUnit.angle > (Opentyr.M_PI_2) - (Opentyr.M_PI / 9))
                    ptrPlayer.aiMemory.c_Angle = 0;
                else if (ptrPlayer.aiMemory.c_Angle < 0 && ptrCurUnit.angle < Opentyr.M_PI / 8)
                    ptrPlayer.aiMemory.c_Angle = 0;
            }

            if (MtRand.mt_rand() % 100 > 93)
            {
                ptrPlayer.aiMemory.c_Power += (int)(MtRand.mt_rand() % 3) - 1;

                if (ptrPlayer.aiMemory.c_Power > 1)
                    ptrPlayer.aiMemory.c_Power = 1;
                else if (ptrPlayer.aiMemory.c_Power < -1)
                    ptrPlayer.aiMemory.c_Power = -1;
            }
            if (MtRand.mt_rand() % 100 > 90)
            {
                if (ptrPlayer.aiMemory.c_Power > 0 && ptrCurUnit.power > 4)
                    ptrPlayer.aiMemory.c_Power = 0;
                else if (ptrPlayer.aiMemory.c_Power < 0 && ptrCurUnit.power < 3)
                    ptrPlayer.aiMemory.c_Power = 0;
                else if (ptrCurUnit.power < 2)
                    ptrPlayer.aiMemory.c_Power = 1;
            }

            // 偏好直升機
            for (j = 0; j < config.max_installations; j++)
            {
                destruct_unit_s ptrUnit = ptrPlayer.unit[j];
                if (DE_isValidUnit(ptrUnit) && ptrUnit.unitType == UNIT_HELI)
                {
                    ptrPlayer.unitSelected = (uint)j;
                    break;
                }
            }

            if (ptrCurUnit.unitType == UNIT_HELI)
            {
                if (ptrCurUnit.isYInAir == false)
                {
                    ptrPlayer.aiMemory.c_Power = 1;
                }
                if (MtRand.mt_rand() % ptrCurUnit.unitX > 100)
                {
                    ptrPlayer.aiMemory.c_Power = 1;
                }
                if (MtRand.mt_rand() % 240 > ptrCurUnit.unitX)
                {
                    ptrPlayer.moves.actions[MOVE_RIGHT] = true;
                }
                else if ((MtRand.mt_rand() % 20) + 300 < ptrCurUnit.unitX)
                {
                    ptrPlayer.moves.actions[MOVE_LEFT] = true;
                }
                else if (MtRand.mt_rand() % 30 == 1)
                {
                    ptrPlayer.aiMemory.c_Angle = (int)(MtRand.mt_rand() % 3) - 1;
                }
                if (ptrCurUnit.unitX > 295 && ptrCurUnit.lastMove > 1)
                {
                    ptrPlayer.moves.actions[MOVE_LEFT] = true;
                    ptrPlayer.moves.actions[MOVE_RIGHT] = false;
                }
                if (ptrCurUnit.unitType != UNIT_HELI || ptrCurUnit.lastMove > 3 || (ptrCurUnit.unitX > 160 && ptrCurUnit.lastMove > -3))
                {
                    if (MtRand.mt_rand() % (uint)(int)roundf(ptrCurUnit.unitY) < 150 && ptrCurUnit.unitYMov < 0.01f && (ptrCurUnit.unitX < 160 || ptrCurUnit.lastMove < 2))
                        ptrPlayer.moves.actions[MOVE_FIRE] = true;
                    ptrPlayer.aiMemory.c_noDown = (uint)((5 - Math.Abs(ptrCurUnit.lastMove)) * (5 - Math.Abs(ptrCurUnit.lastMove)) + 3);
                    ptrPlayer.aiMemory.c_Power = 1;
                }
                else
                {
                    ptrPlayer.moves.actions[MOVE_FIRE] = false;
                }

                for (j = 0; j < config.max_installations; j++)
                {
                    destruct_unit_s ptrUnit = ptrTarget.unit[j];
                    if (Math.Abs((int)ptrUnit.unitX - (int)ptrCurUnit.unitX) < 8)
                    {
                        /* 讓直升機懸停在敵人上方。 */
                        if (ptrUnit.unitType == UNIT_SATELLITE)
                        {
                            ptrPlayer.moves.actions[MOVE_FIRE] = false;
                        }
                        else
                        {
                            ptrPlayer.moves.actions[MOVE_LEFT] = false;
                            ptrPlayer.moves.actions[MOVE_RIGHT] = false;
                            if (ptrCurUnit.lastMove < -1)
                                ptrCurUnit.lastMove++;
                            else if (ptrCurUnit.lastMove > 1)
                                ptrCurUnit.lastMove--;
                        }
                    }
                }
            }
            else
            {
                ptrPlayer.moves.actions[MOVE_FIRE] = true;
            }

            if (MtRand.mt_rand() % 200 > 198)
            {
                ptrPlayer.moves.actions[MOVE_CHANGE] = true;
                ptrPlayer.aiMemory.c_Angle = 0;
                ptrPlayer.aiMemory.c_Power = 0;
                ptrPlayer.aiMemory.c_Fire = 0;
            }

            if (MtRand.mt_rand() % 100 > 98 || ptrCurUnit.shotType == SHOT_TRACER)
            {
                ptrPlayer.moves.actions[MOVE_CYDN] = true;
            }
            if (ptrPlayer.aiMemory.c_Angle > 0)
            {
                ptrPlayer.moves.actions[MOVE_LEFT] = true;
            }
            if (ptrPlayer.aiMemory.c_Angle < 0)
            {
                ptrPlayer.moves.actions[MOVE_RIGHT] = true;
            }
            if (ptrPlayer.aiMemory.c_Power > 0)
            {
                ptrPlayer.moves.actions[MOVE_UP] = true;
            }
            if (ptrPlayer.aiMemory.c_Power < 0 && ptrPlayer.aiMemory.c_noDown == 0)
            {
                ptrPlayer.moves.actions[MOVE_DOWN] = true;
            }
            if (ptrPlayer.aiMemory.c_Fire > 0)
            {
                ptrPlayer.moves.actions[MOVE_FIRE] = true;
            }

            if (ptrCurUnit.unitYMov < -0.1f && ptrCurUnit.unitType == UNIT_HELI)
            {
                ptrPlayer.moves.actions[MOVE_FIRE] = false;
            }

            if (ptrCurUnit.unitType == UNIT_LASER || ptrCurUnit.isYInAir == true)
                ptrPlayer.aiMemory.c_Power = 0;
        }
    }

    private static void DE_RunTickDrawCrosshairs()
    {
        int i;
        int tempPosX, tempPosY;
        int direction;

        /* 畫準星。多數載具瞄左或右；直升機兩邊都可，需特別處理。 */
        for (i = 0; i < MAX_PLAYERS; i++)
        {
            direction = (i == PLAYER_LEFT) ? -1 : 1;
            destruct_unit_s curUnit = destruct_player[i].unit[destruct_player[i].unitSelected];

            if (curUnit.unitType == UNIT_HELI)
            {
                tempPosX = (int)curUnit.unitX + (int)roundf(0.1f * curUnit.lastMove * curUnit.lastMove * curUnit.lastMove) + 5;
                tempPosY = (int)roundf(curUnit.unitY) + 1;
            }
            else
            {
                tempPosX = (int)roundf(curUnit.unitX + 6 - MathF.Cos(curUnit.angle) * (curUnit.power * 8 + 7) * direction);
                tempPosY = (int)roundf(curUnit.unitY - 7 - MathF.Sin(curUnit.angle) * (curUnit.power * 8 + 7));
            }

            /* 畫出來，但避開 HUD 區。 */
            if (tempPosY > 9)
            {
                if (tempPosY > 11)
                {
                    if (tempPosY > 13)
                    {
                        /* 頂端像素 */
                        Vga256d.JE_pix(Video.VGAScreen, tempPosX, tempPosY - 2, 3);
                    }
                    /* 中間三個像素 */
                    Vga256d.JE_pix(Video.VGAScreen, tempPosX + 3, tempPosY, 3);
                    Vga256d.JE_pix(Video.VGAScreen, tempPosX, tempPosY, 14);
                    Vga256d.JE_pix(Video.VGAScreen, tempPosX - 3, tempPosY, 3);
                }
                /* 底端像素 */
                Vga256d.JE_pix(Video.VGAScreen, tempPosX, tempPosY + 2, 3);
            }
        }
    }

    private static void DE_RunTickDrawHUD()
    {
        int i;
        int startX;
        string tempstr;

        for (i = 0; i < MAX_PLAYERS; i++)
        {
            destruct_unit_s curUnit = destruct_player[i].unit[destruct_player[i].unitSelected];
            startX = ((i == PLAYER_LEFT) ? 0 : 320 - 150);

            Vga256d.fill_rectangle_xy(Video.VGAScreen, startX + 5, 3, startX + 14, 8, 241);
            Vga256d.JE_rectangle(Video.VGAScreen, startX + 4, 2, startX + 15, 9, 242);
            Vga256d.JE_rectangle(Video.VGAScreen, startX + 3, 1, startX + 16, 10, 240);
            Vga256d.fill_rectangle_xy(Video.VGAScreen, startX + 18, 3, startX + 140, 8, 241);
            Vga256d.JE_rectangle(Video.VGAScreen, startX + 17, 2, startX + 143, 9, 242);
            Vga256d.JE_rectangle(Video.VGAScreen, startX + 16, 1, startX + 144, 10, 240);

            Sprites.blit_sprite2(Video.VGAScreen, startX + 4, 0, Sprites.destructSpriteSheet, (uint)(191 + curUnit.shotType));

            Fonthand.JE_outText(Video.VGAScreen, startX + 20, 3, Helptext.weaponNames[curUnit.shotType], 15, 2);
            tempstr = $"dmg~{curUnit.health}~";
            Fonthand.JE_outText(Video.VGAScreen, startX + 75, 3, tempstr, 15, 0);
            tempstr = $"pts~{destruct_player[i].score}~";
            Fonthand.JE_outText(Video.VGAScreen, startX + 110, 3, tempstr, 15, 0);
        }
    }

    private static void DE_RunTickGetInput()
    {
        int player_index, key_index, slot_index;
        int key;

        Keyboard.handleSdlEvents();

        for (player_index = 0; player_index < MAX_PLAYERS; player_index++)
        {
            for (key_index = 0; key_index < MAX_KEY; key_index++)
            {
                for (slot_index = 0; slot_index < MAX_KEY_OPTIONS; slot_index++)
                {
                    key = destruct_player[player_index].keys.Config[key_index, slot_index];
                    if (key == 0 /* SDL_SCANCODE_UNKNOWN */)
                        break;
                    if (Keyboard.keysactive[key] == true)
                    {
                        destruct_player[player_index].moves.actions[key_index] = true;

                        /* 有些鍵之後要切回（toggle） */
                        if (key_index == KEY_CHANGE ||
                            key_index == KEY_CYUP ||
                            key_index == KEY_CYDN)
                        {
                            Keyboard.keysactive[key] = false;
                        }
                        break;
                    }
                }
            }
        }
    }

    private static void DE_ProcessInput()
    {
        int direction;

        int player_index;

        for (player_index = 0; player_index < MAX_PLAYERS; player_index++)
        {
            if (destruct_player[player_index].unitsRemaining <= 0)
                continue;

            direction = (player_index == PLAYER_LEFT) ? -1 : 1;
            destruct_unit_s curUnit = destruct_player[player_index].unit[destruct_player[player_index].unitSelected];

            if (systemAngle[curUnit.unitType] != 0) /* 選中的單位可改變射擊角度 */
            {
                if (destruct_player[player_index].moves.actions[MOVE_LEFT] == true)
                {
                    if (player_index == PLAYER_LEFT)
                        DE_RaiseAngle(curUnit);
                    else
                        DE_LowerAngle(curUnit);
                }
                if (destruct_player[player_index].moves.actions[MOVE_RIGHT] == true)
                {
                    if (player_index == PLAYER_LEFT)
                        DE_LowerAngle(curUnit);
                    else
                        DE_RaiseAngle(curUnit);
                }
            }
            else if (curUnit.unitType == UNIT_HELI)
            {
                if (destruct_player[player_index].moves.actions[MOVE_LEFT] == true && curUnit.unitX > 5)
                {
                    if (JE_stabilityCheck(curUnit.unitX - 5, (uint)roundf(curUnit.unitY)))
                    {
                        if (curUnit.lastMove > -5)
                            curUnit.lastMove--;
                        curUnit.unitX--;
                        if (JE_stabilityCheck(curUnit.unitX, (uint)roundf(curUnit.unitY)))
                            curUnit.isYInAir = true;
                    }
                }
                if (destruct_player[player_index].moves.actions[MOVE_RIGHT] == true && curUnit.unitX < 305)
                {
                    if (JE_stabilityCheck(curUnit.unitX + 5, (uint)roundf(curUnit.unitY)))
                    {
                        if (curUnit.lastMove < 5)
                            curUnit.lastMove++;
                        curUnit.unitX++;
                        if (JE_stabilityCheck(curUnit.unitX, (uint)roundf(curUnit.unitY)))
                            curUnit.isYInAir = true;
                    }
                }
            }

            if (curUnit.unitType != UNIT_LASER)
            {	/*increasepower*/
                if (destruct_player[player_index].moves.actions[MOVE_UP] == true)
                {
                    if (curUnit.unitType == UNIT_HELI)
                    {
                        curUnit.isYInAir = true;
                        curUnit.unitYMov -= 0.1f;
                    }
                    else if (curUnit.unitType == UNIT_JUMPER &&
                             curUnit.isYInAir == false)
                    {
                        curUnit.unitYMov = -3;
                        curUnit.isYInAir = true;
                    }
                    else
                    {
                        DE_RaisePower(curUnit);
                    }
                }
                /*decreasepower*/
                if (destruct_player[player_index].moves.actions[MOVE_DOWN] == true)
                {
                    if (curUnit.unitType == UNIT_HELI && curUnit.isYInAir == true)
                    {
                        curUnit.unitYMov += 0.1f;
                    }
                    else
                    {
                        DE_LowerPower(curUnit);
                    }
                }
            }

            /*up/down weapon. 一直循環直到找到有效武器 */
            if (destruct_player[player_index].moves.actions[MOVE_CYUP] == true)
                DE_CycleWeaponUp(curUnit);
            if (destruct_player[player_index].moves.actions[MOVE_CYDN] == true)
                DE_CycleWeaponDown(curUnit);

            /* Change：會改變 curUnit 指標，放最後做。有效性檢查在 tick 開頭做。 */
            if (destruct_player[player_index].moves.actions[MOVE_CHANGE] == true)
            {
                destruct_player[player_index].unitSelected++;
                if (destruct_player[player_index].unitSelected >= config.max_installations)
                    destruct_player[player_index].unitSelected = 0;
            }

            /*Newshot*/
            if (destruct_player[player_index].shotDelay > 0)
                destruct_player[player_index].shotDelay--;
            if (destruct_player[player_index].moves.actions[MOVE_FIRE] == true &&
                destruct_player[player_index].shotDelay == 0)
            {
                destruct_player[player_index].shotDelay = (uint)shotDelay[curUnit.shotType];

                switch (shotDirt[curUnit.shotType])
                {
                    case EXPL_NONE:
                        break;

                    case EXPL_MAGNET:
                        DE_RunMagnet(player_index, curUnit);
                        break;

                    case EXPL_DIRT:
                    case EXPL_NORMAL:
                        DE_MakeShot(player_index, curUnit, direction);
                        break;

                    default:
                        System.Diagnostics.Debug.Assert(false);
                        break;
                }
            }
        }
    }

    private static void DE_CycleWeaponUp(destruct_unit_s unit)
    {
        do
        {
            unit.shotType++;
            if (unit.shotType > SHOT_LAST)
                unit.shotType = SHOT_FIRST;
        } while (weaponSystems[unit.unitType, unit.shotType] == false);
    }

    private static void DE_CycleWeaponDown(destruct_unit_s unit)
    {
        do
        {
            unit.shotType--;
            if (unit.shotType < SHOT_FIRST)
                unit.shotType = SHOT_LAST;
        } while (weaponSystems[unit.unitType, unit.shotType] == false);
    }

    private static void DE_MakeShot(int curPlayer, destruct_unit_s curUnit, int direction)
    {
        int i;
        int shotIndex = -1;

        /* 先找一個可用的 shot struct */
        for (i = 0; ; i++)
        {
            if (i >= config.max_shots)
                return;  /* 沒有空槽，什麼都不做。 */

            if (shotRec[i].isAvailable)
            {
                shotIndex = i;
                break;
            }
        }

        /* 直升機在地面時不能開火。 */
        if (curUnit.unitType == UNIT_HELI && curUnit.isYInAir == false)
            return;

        /* 播放開火音效 */
        Varz.soundQueue[curPlayer] = (byte)shotSound[curUnit.shotType];

        /* 建立子彈，某些單位邏輯不同 */
        switch (curUnit.unitType)
        {
            case UNIT_HELI:

                shotRec[shotIndex].x = curUnit.unitX + curUnit.lastMove * 2 + 5;
                shotRec[shotIndex].xmov = 0.02f * curUnit.lastMove * curUnit.lastMove * curUnit.lastMove;

                /* 若正徒勞地往螢幕上方移動，行為不同。*/
                if (destruct_player[curPlayer].moves.actions[MOVE_UP] && curUnit.unitY < 30)
                {
                    shotRec[shotIndex].y = curUnit.unitY;
                    shotRec[shotIndex].ymov = 0.1f;

                    if (shotRec[shotIndex].xmov < 0)
                        shotRec[shotIndex].xmov += 0.1f;
                    else if (shotRec[shotIndex].xmov > 0)
                        shotRec[shotIndex].xmov -= 0.1f;
                }
                else
                {
                    shotRec[shotIndex].y = curUnit.unitY + 1;
                    shotRec[shotIndex].ymov = 0.5f + curUnit.unitYMov * 0.1f;
                }
                break;

            case UNIT_JUMPER: /* Jumper 通常只對左方玩家特別。Bug？還是 feature？ */

                if (config.jumper_straight[curPlayer])
                {
                    shotRec[shotIndex].x = curUnit.unitX + 6 - MathF.Cos(curUnit.angle) * 10 * direction;
                    shotRec[shotIndex].y = curUnit.unitY - 7 - MathF.Sin(curUnit.angle) * 10;
                    shotRec[shotIndex].xmov = -MathF.Cos(curUnit.angle) * curUnit.power * direction;
                    shotRec[shotIndex].ymov = -MathF.Sin(curUnit.angle) * curUnit.power;
                }
                else
                {
                    shotRec[shotIndex].x = curUnit.unitX + 2;
                    shotRec[shotIndex].xmov = -MathF.Cos(curUnit.angle) * curUnit.power * direction;

                    if (curUnit.isYInAir == true)
                    {
                        shotRec[shotIndex].ymov = 1;
                        shotRec[shotIndex].y = curUnit.unitY + 2;
                    }
                    else
                    {
                        shotRec[shotIndex].ymov = -2;
                        shotRec[shotIndex].y = curUnit.unitY - 12;
                    }
                }
                break;

            default:

                shotRec[shotIndex].x = curUnit.unitX + 6 - MathF.Cos(curUnit.angle) * 10 * direction;
                shotRec[shotIndex].y = curUnit.unitY - 7 - MathF.Sin(curUnit.angle) * 10;
                shotRec[shotIndex].xmov = -MathF.Cos(curUnit.angle) * curUnit.power * direction;
                shotRec[shotIndex].ymov = -MathF.Sin(curUnit.angle) * curUnit.power;
                break;
        }

        /* 設定/清除最後幾個細節。 */
        shotRec[shotIndex].isAvailable = false;

        shotRec[shotIndex].shottype = (uint)curUnit.shotType;

        shotRec[shotIndex].trailc[0] = 0;
        shotRec[shotIndex].trailc[1] = 0;
        shotRec[shotIndex].trailc[2] = 0;
        shotRec[shotIndex].trailc[3] = 0;
    }

    private static void DE_RunMagnet(int curPlayer, destruct_unit_s magnet)
    {
        int i;
        int curEnemy;
        int direction;

        curEnemy = (curPlayer == PLAYER_LEFT) ? PLAYER_RIGHT : PLAYER_LEFT;
        direction = (curPlayer == PLAYER_LEFT) ? -1 : 1;

        /* 推動所有在磁鐵前方的子彈 */
        for (i = 0; i < config.max_shots; i++)
        {
            if (shotRec[i].isAvailable == false)
            {
                if ((curPlayer == PLAYER_LEFT && shotRec[i].x > magnet.unitX) ||
                    (curPlayer == PLAYER_RIGHT && shotRec[i].x < magnet.unitX))
                {
                    shotRec[i].xmov += magnet.power * 0.1f * -direction;
                }
            }
        }

        for (i = 0; i < config.max_installations; i++) /* 磁鐵推動直升機 */
        {
            destruct_unit_s enemyUnit = destruct_player[curEnemy].unit[i];
            if (DE_isValidUnit(enemyUnit) &&
                enemyUnit.unitType == UNIT_HELI &&
                enemyUnit.isYInAir == true)
            {
                if ((curEnemy == PLAYER_RIGHT && destruct_player[curEnemy].unit[i].unitX + 11 < 318) ||
                    (curEnemy == PLAYER_LEFT && destruct_player[curEnemy].unit[i].unitX > 1))
                {
                    enemyUnit.unitX = (uint)(enemyUnit.unitX - 2 * direction);
                }
            }
        }
        magnet.ani_frame = 1;
    }

    private static void DE_RaiseAngle(destruct_unit_s unit)
    {
        unit.angle += 0.01f;
        if (unit.angle > Opentyr.M_PI_2 - 0.01f)
            unit.angle = (float)(Opentyr.M_PI_2 - 0.01f);
    }

    private static void DE_LowerAngle(destruct_unit_s unit)
    {
        unit.angle -= 0.01f;
        if (unit.angle < 0)
            unit.angle = 0;
    }

    private static void DE_RaisePower(destruct_unit_s unit)
    {
        unit.power += 0.05f;
        if (unit.power > 5)
            unit.power = 5;
    }

    private static void DE_LowerPower(destruct_unit_s unit)
    {
        unit.power -= 0.05f;
        if (unit.power < 1)
            unit.power = 1;
    }

    /* DE_isValidUnit: health > 0 時回傳 true。 */
    private static bool DE_isValidUnit(destruct_unit_s unit)
    {
        return unit.health > 0;
    }

    private static bool DE_RunTickCheckEndgame()
    {
        if (destruct_player[PLAYER_LEFT].unitsRemaining == 0)
        {
            destruct_player[PLAYER_RIGHT].score += (uint)ModeScore[PLAYER_LEFT, world.destructMode];
            Varz.soundQueue[7] = (byte)Sndmast.V_CLEARED_PLATFORM;
            return true;
        }
        if (destruct_player[PLAYER_RIGHT].unitsRemaining == 0)
        {
            destruct_player[PLAYER_LEFT].score += (uint)ModeScore[PLAYER_RIGHT, world.destructMode];
            Varz.soundQueue[7] = (byte)Sndmast.V_CLEARED_PLATFORM;
            return true;
        }
        return false;
    }

    private static void DE_RunTickPlaySounds()
    {
        int i;
        uint tempSampleIndex, tempVolume;

        for (i = 0; i < Varz.soundQueue.Length; i++)
        {
            if (Varz.soundQueue[i] != Sndmast.S_NONE)
            {
                tempSampleIndex = Varz.soundQueue[i];
                if (i == 7)
                    tempVolume = Nortsong.fxPlayVol;
                else
                    tempVolume = Nortsong.fxPlayVol / 2u;

                Loudness.multiSamplePlay(Nortsong.soundSamples[tempSampleIndex - 1], Nortsong.soundSampleCount[tempSampleIndex - 1], (byte)i, (byte)tempVolume);
                Varz.soundQueue[i] = (byte)Sndmast.S_NONE;
            }
        }
    }

    private static void JE_pixCool(uint x, uint y, byte c)
    {
        Vga256d.JE_pix(Video.VGAScreen, (int)x, (int)y, c);
        Vga256d.JE_pix(Video.VGAScreen, (int)(x - 1), (int)y, (byte)(c - 2));
        Vga256d.JE_pix(Video.VGAScreen, (int)(x + 1), (int)y, (byte)(c - 2));
        Vga256d.JE_pix(Video.VGAScreen, (int)x, (int)(y - 1), (byte)(c - 2));
        Vga256d.JE_pix(Video.VGAScreen, (int)x, (int)(y + 1), (byte)(c - 2));
    }

    /* 全螢幕複製（對應原版 memcpy(dst->pixels, src->pixels, h*pitch)）。 */
    private static void ScreenCopy(SDL_Surface src, SDL_Surface dst)
    {
        long n = (long)dst.h * dst.pitch;
        Buffer.MemoryCopy(src.pixels, dst.pixels, n, n);
    }
}
