namespace AprCSTyrian.Core;

// 設定全域多由遊戲邏輯指派；此檔集中宣告。
#pragma warning disable CS0649

/// <summary>對應 config.h:JE_SaveFileType（單一存檔/高分項）。</summary>
internal sealed class JE_SaveFileType
{
    public ushort encode;
    public ushort level;
    public readonly byte[] items = new byte[12];   // JE_PItemsType
    public int score;
    public int score2;
    public readonly byte[] levelName = new byte[11];
    public readonly byte[] name = new byte[15];
    public byte cubes;
    public readonly byte[] power = new byte[2];
    public byte episode;
    public readonly byte[] lastItems = new byte[12];
    public byte difficulty;
    public byte secretHint;
    public byte input1;
    public byte input2;
    public bool gameHasRepeated;
    public byte initialDifficulty;
    public int highScore1;
    public int highScore2;
    public readonly byte[] highScoreName = new byte[30];
    public byte highScoreDiff;
}

/// <summary>
/// 移植 sources/src/config.c —— 二進位設定 (tyrian.cfg)、存檔/高分 (tyrian.sav，含加解密)、
/// JE_saveGame/JE_loadGame、處理器/速度設定。opentyrian.cfg (INI, config_file.c) 與 episode
/// 資料載入暫為最小占位。
/// </summary>
internal static unsafe partial class Config
{
    public const int SAVE_FILES_NUM = 11 * 2;
    private const int SAVE_FILES_SIZE = 109 * SAVE_FILES_NUM;
    private const int SAVE_FILE_SIZE = SAVE_FILES_SIZE + 100;

    // 難度
    public const int DIFFICULTY_WIMP = 0, DIFFICULTY_EASY = 1, DIFFICULTY_NORMAL = 2, DIFFICULTY_HARD = 3,
        DIFFICULTY_IMPOSSIBLE = 4, DIFFICULTY_INSANITY = 5, DIFFICULTY_SUICIDE = 6, DIFFICULTY_MANIACAL = 7,
        DIFFICULTY_ZINGLON = 8, DIFFICULTY_NORTANEOUS = 9, DIFFICULTY_10 = 10;

    // 按鍵設定索引
    public const int KEY_SETTING_UP = 0, KEY_SETTING_DOWN = 1, KEY_SETTING_LEFT = 2, KEY_SETTING_RIGHT = 3,
        KEY_SETTING_FIRE = 4, KEY_SETTING_CHANGE_FIRE = 5, KEY_SETTING_LEFT_SIDEKICK = 6, KEY_SETTING_RIGHT_SIDEKICK = 7;

    // SHOT_* 索引（shotRepeat/shotMultiPos 用）
    public const int SHOT_FRONT = 0, SHOT_REAR = 1, SHOT_LEFT_SIDEKICK = 2, SHOT_RIGHT_SIDEKICK = 3,
        SHOT_MISC = 4, SHOT_P2_CHARGE = 5, SHOT_P1_SUPERBOMB = 6, SHOT_P2_SUPERBOMB = 7, SHOT_SPECIAL = 8,
        SHOT_NORTSPARKS = 9, SHOT_SPECIAL2 = 10;

    private static readonly byte[] cryptKey = { 15, 50, 89, 240, 147, 34, 86, 9, 32, 208 };

    public static readonly int[] defaultKeySettings =
    {
        SdlKeys.SDL_SCANCODE_UP, SdlKeys.SDL_SCANCODE_DOWN, SdlKeys.SDL_SCANCODE_LEFT, SdlKeys.SDL_SCANCODE_RIGHT,
        SdlKeys.SDL_SCANCODE_SPACE, SdlKeys.SDL_SCANCODE_RETURN, SdlKeys.SDL_SCANCODE_LCTRL, SdlKeys.SDL_SCANCODE_LALT,
    };

    private static readonly string[] defaultHighScoreNames =
    {
        "The Prime Chair", "Transon Lohk", "Javi Onukala", "Mantori", "Nortaneous", "Dougan", "Reid",
        "General Zinglon", "Late Gyges Phildren", "Vykromod", "Beppo", "Borogar", "ShipMaster Carlos",
        "Jill", "Darcy", "Jake Stone", "Malvineous Havershim", "Marta Louise Velasquez",
        "Jazz Jackrabbit", "Eva Earlong", "Devan Shell",
        "Crystal Devroe", "Steffan Tommas", "Milano Angston", "Christian", "Shirro", "Jean-Paul",
        "Ibrahim Hothe", "Angel", "Cossette Akira", "Raven", "Hans Kreissack",
        "Tyler", "Rennis the Rat Guard",
    };

    private static readonly string[] defaultTeamNames =
    {
        "Jackrabbits", "Team Tyrian", "The Elam Brothers", "Dare to Dream Team", "Pinball Freaks",
        "Extreme Pinball Freaks", "Team Vykromod", "Epic All-Stars", "Hans Keissack's WARriors", "Team Overkill",
        "Pied Pipers", "Gencore Growlers", "Microsol Masters", "Beta Warriors", "Team Loco", "The Shellians",
        "Jungle Jills", "Murderous Malvineous", "The Traffic Department", "Clan Mikal", "Clan Patrok", "Carlos' Crawlers",
    };

    private static readonly byte[] initialEditorItemAvail =
    {
        1,1,1,0,0,1,1,0,1,1,1,1,1,0,1,0,1,1,1,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,1,0,0,0,1,1,0,1,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,
    };

    // === 全域 ===
    public static readonly bool[] smoothies = new bool[9];
    public static byte starShowVGASpecialCode;
    public static ushort lastCubeMax, cubeMax;
    public static readonly ushort[] cubeList = new ushort[4];
    public static bool gameHasRepeated;
    public static sbyte difficultyLevel, oldDifficultyLevel, initialDifficulty;
    public static uint power, lastPower, powerAdd;
    public static byte shieldWait, shieldT;

    public static readonly byte[] shotRepeat = new byte[11];
    public static readonly byte[] shotMultiPos = new byte[11];
    public static bool portConfigChange, portConfigDone;

    public static readonly byte[] lastLevelName = new byte[11];
    public static readonly byte[] levelName = new byte[11];
    public static byte mainLevel, nextLevel, saveLevel;

    public static readonly int[] keySettings = new int[8];

    public static sbyte levelFilter, levelFilterNew, levelBrightness, levelBrightnessChg;
    public static bool filtrationAvail, filterActive, filterFade, filterFadeStart;
    public static bool gameJustLoaded;
    public static bool galagaMode;
    public static bool extraGame;
    public static bool twoPlayerMode, twoPlayerLinked, onePlayerAction, superTyrian;
    public const bool isNetworkGame = false; // network.c 不移植 → 恆 false
    public static bool trentWin = false;
    public static byte superArcadeMode;
    public static byte superArcadePowerUp;
    public static float linkGunDirec;
    public static readonly byte[] inputDevice = { 1, 2 };
    public static byte secretHint;
    public static byte background3over;
    public static byte background2over;
    public static byte gammaCorrection;
    public static bool superPause = false;
    public static bool explosionTransparent, youAreCheating, displayScore, background2, smoothScroll, wild, superWild,
        starActive, topEnemyOver, skyEnemyOverAll, background2notTransparent;
    public static byte fastPlay;
    public static bool pentiumMode;
    public static byte gameSpeed;
    public static byte processorType;
    public static readonly JE_SaveFileType[] saveFiles = NewSaveFiles();
    public static readonly byte[] editorItemAvail = new byte[100];
    public static ushort editorLevel;

    // TYRIAN.CFG 中為相容性保留的欄位
    private static byte inputDevice_ = 0, jConfigure = 0, midiPort = 0, soundEffects = 0, versionNum;
    private static readonly byte[] defaultJoyButtonAssign = { 1, 4, 5, 5 };
    private static readonly byte[] joyButtonAssign = new byte[4];
    private static byte inputDevice1 = 0, inputDevice2 = 0;
    private static readonly byte[] defaultDosKeySettings = { 72, 80, 75, 77, 57, 28, 29, 56 };
    private static readonly byte[] dosKeySettings = new byte[8];

    private static JE_SaveFileType[] NewSaveFiles()
    {
        var a = new JE_SaveFileType[SAVE_FILES_NUM];
        for (int i = 0; i < a.Length; ++i) a[i] = new JE_SaveFileType();
        return a;
    }

    // === C 字串 helper ===
    private static int CStrLen(byte[] s)
    {
        int i = 0;
        while (i < s.Length && s[i] != 0) i++;
        return i;
    }
    private static void StrCpy(byte[] dst, string src)
    {
        int n = Math.Min(src.Length, dst.Length - 1);
        for (int i = 0; i < n; ++i) dst[i] = (byte)src[i];
        dst[n] = 0;
    }
    private static bool StrEq(byte[] s, string lit)
    {
        int n = CStrLen(s);
        if (n != lit.Length) return false;
        for (int i = 0; i < n; ++i) if (s[i] != (byte)lit[i]) return false;
        return true;
    }

    private static void ReadBytes(ref MemReader r, byte[] dst, int count)
    {
        fixed (byte* p = dst) MemIO.memReadU8Array(ref r, p, (nuint)count);
    }
    private static void WriteBytes(ref MemWriter w, byte[] src, int count)
    {
        fixed (byte* p = src) MemIO.memWriteU8Array(ref w, p, (nuint)count);
    }

    public static string get_user_directory() => Globals.UserDir;

    private static void playeritems_to_pitems(byte[] pItems, ref PlayerItems items, byte initial_episode_num)
    {
        pItems[0] = items.weapon[Players.FRONT_WEAPON].id;
        pItems[1] = items.weapon[Players.REAR_WEAPON].id;
        pItems[2] = items.super_arcade_mode;
        pItems[3] = items.sidekick[Players.LEFT_SIDEKICK];
        pItems[4] = items.sidekick[Players.RIGHT_SIDEKICK];
        pItems[5] = items.generator;
        pItems[6] = items.sidekick_level;
        pItems[7] = items.sidekick_series;
        pItems[8] = initial_episode_num;
        pItems[9] = items.shield;
        pItems[10] = items.special;
        pItems[11] = items.ship;
    }

    private static void pitems_to_playeritems(ref PlayerItems items, byte[] pItems, bool storeEpisode)
    {
        items.weapon[Players.FRONT_WEAPON].id = pItems[0];
        items.weapon[Players.REAR_WEAPON].id = pItems[1];
        items.super_arcade_mode = pItems[2];
        items.sidekick[Players.LEFT_SIDEKICK] = pItems[3];
        items.sidekick[Players.RIGHT_SIDEKICK] = pItems[4];
        items.generator = pItems[5];
        items.sidekick_level = pItems[6];
        items.sidekick_series = pItems[7];
        if (storeEpisode)
            Episodes.initial_episode_num = pItems[8];
        items.shield = pItems[9];
        items.special = pItems[10];
        items.ship = pItems[11];
    }

    public static void JE_saveGame(byte slot, string name)
    {
        var sf = saveFiles[slot - 1];
        var player = Players.player;

        sf.initialDifficulty = (byte)initialDifficulty;
        sf.gameHasRepeated = gameHasRepeated;
        sf.level = saveLevel;

        if (superTyrian)
            player[0].items.super_arcade_mode = VarzConst.SA_SUPERTYRIAN;
        else if (superArcadeMode == VarzConst.SA_NONE && onePlayerAction)
            player[0].items.super_arcade_mode = VarzConst.SA_ARCADE;
        else
            player[0].items.super_arcade_mode = superArcadeMode;

        playeritems_to_pitems(sf.items, ref player[0].items, Episodes.initial_episode_num);

        if (twoPlayerMode)
            playeritems_to_pitems(sf.lastItems, ref player[1].items, 0);
        else
            playeritems_to_pitems(sf.lastItems, ref player[0].last_items, 0);

        sf.score = (int)player[0].cash;
        sf.score2 = (int)player[1].cash;

        Array.Copy(lastLevelName, sf.levelName, lastLevelName.Length);
        sf.cubes = (byte)lastCubeMax;

        if (StrEq(lastLevelName, "Completed"))
        {
            Varz.temp = (byte)(Episodes.episodeNum - 1);
            if (Varz.temp < 1)
                Varz.temp = Episodes.EPISODE_AVAILABLE;
            sf.episode = Varz.temp;
        }
        else
        {
            sf.episode = Episodes.episodeNum;
        }

        sf.difficulty = (byte)difficultyLevel;
        sf.secretHint = secretHint;
        sf.input1 = inputDevice[0];
        sf.input2 = inputDevice[1];

        StrCpy(sf.name, name);

        for (int port = 0; port < 2; ++port)
            sf.power[port] = player[twoPlayerMode ? port : 0].items.weapon[port].power;

        saveSaves();
    }

    public static void JE_loadGame(byte slot)
    {
        var sf = saveFiles[slot - 1];
        var player = Players.player;

        superTyrian = false;
        onePlayerAction = false;
        twoPlayerMode = false;
        extraGame = false;
        galagaMode = false;

        initialDifficulty = (sbyte)sf.initialDifficulty;
        gameHasRepeated = sf.gameHasRepeated;
        twoPlayerMode = (slot - 1) > 10;
        difficultyLevel = (sbyte)sf.difficulty;

        pitems_to_playeritems(ref player[0].items, sf.items, true);

        superArcadeMode = player[0].items.super_arcade_mode;

        if (superArcadeMode == VarzConst.SA_SUPERTYRIAN) superTyrian = true;
        if (superArcadeMode != VarzConst.SA_NONE) onePlayerAction = true;
        if (superArcadeMode > VarzConst.SA_NORTSHIPZ) superArcadeMode = VarzConst.SA_NONE;

        if (twoPlayerMode)
        {
            onePlayerAction = false;
            pitems_to_playeritems(ref player[1].items, sf.lastItems, false);
        }
        else
        {
            pitems_to_playeritems(ref player[0].last_items, sf.lastItems, false);
        }

        if (player[1].items.sidekick_level < 101)
        {
            player[1].items.sidekick_level = 101;
            player[1].items.sidekick_series = player[1].items.sidekick[Players.LEFT_SIDEKICK];
        }

        player[0].cash = (uint)sf.score;
        player[1].cash = (uint)sf.score2;

        mainLevel = (byte)sf.level;
        cubeMax = sf.cubes;
        lastCubeMax = cubeMax;

        secretHint = sf.secretHint;
        inputDevice[0] = sf.input1;
        inputDevice[1] = sf.input2;

        for (int port = 0; port < 2; ++port)
            player[twoPlayerMode ? port : 0].items.weapon[port].power = sf.power[port];

        int episode = sf.episode;

        Array.Copy(sf.levelName, levelName, levelName.Length);

        if (StrEq(levelName, "Completed"))
        {
            if (episode == Episodes.EPISODE_AVAILABLE) episode = 1;
            else if (episode < Episodes.EPISODE_AVAILABLE) episode++;
        }

        Episodes.JE_initEpisode(episode);
        saveLevel = mainLevel;
        Array.Copy(levelName, lastLevelName, levelName.Length);
    }

    public static void JE_initProcessorType()
    {
        wild = false;
        superWild = false;
        smoothScroll = true;
        explosionTransparent = true;
        filtrationAvail = false;
        background2 = true;
        displayScore = true;

        switch (processorType)
        {
            case 1: background2 = false; displayScore = false; explosionTransparent = false; break;
            case 2: break;
            case 3: smoothScroll = false; break;
            case 4: wild = true; filtrationAvail = true; break;
            case 5: smoothScroll = false; break;
            case 6: wild = true; superWild = true; filtrationAvail = true; break;
        }

        switch (gameSpeed)
        {
            case 1: fastPlay = 3; break;
            case 2: fastPlay = 4; break;
            case 3: fastPlay = 5; break;
            case 4: fastPlay = 0; break;
            case 5: fastPlay = 1; break;
        }
    }

    public static void JE_setNewGameSpeed()
    {
        pentiumMode = false;
        ushort speed;
        switch (fastPlay)
        {
            default:
            case 0: speed = 0x4300; smoothScroll = true; Nortsong.frameCountMax = 2; break;
            case 1: speed = 0x3000; smoothScroll = true; Nortsong.frameCountMax = 2; break;
            case 2: speed = 0x2000; smoothScroll = false; Nortsong.frameCountMax = 2; break;
            case 3: speed = 0x5300; smoothScroll = true; Nortsong.frameCountMax = 4; break;
            case 4: speed = 0x4300; smoothScroll = true; Nortsong.frameCountMax = 3; break;
            case 5: speed = 0x4300; smoothScroll = true; Nortsong.frameCountMax = 2; pentiumMode = true; break;
        }
        Nortsong.setFrameSpeed(speed);
        Nortsong.setFrameCount(Nortsong.frameCountMax);
    }

    // opentyrian.cfg (INI) 暫為最小占位：套用預設按鍵；INI 解析待 config_file.c 移植。
    private static bool load_opentyrian_config()
    {
        Video.fullscreen_display = -1;
        Array.Copy(defaultKeySettings, keySettings, keySettings.Length);
        return false;
    }
    private static void save_opentyrian_config() { /* INI 寫出待 config_file.c */ }

    public static void loadConfiguration()
    {
        bool invalid = false;

        Stream? f = CFile.dir_fopen_warn(get_user_directory(), "tyrian.cfg", "rb");
        if (f == null)
        {
            invalid = true;
        }
        else
        {
            byte[] data = new byte[28];
            int size = 0;
            int r;
            while (size < data.Length && (r = f.Read(data, size, data.Length - size)) > 0) size += r;
            invalid |= f.Length != data.Length;
            CFile.fclose(f);

            fixed (byte* dp = data)
            {
                MemReader reader = new() { data = dp, size = (nuint)size, error = false };

                background2 = MemIO.memReadBool(ref reader);
                gameSpeed = MemIO.memReadU8(ref reader);
                inputDevice_ = MemIO.memReadU8(ref reader);
                jConfigure = MemIO.memReadU8(ref reader);
                versionNum = MemIO.memReadU8(ref reader);
                processorType = MemIO.memReadU8(ref reader);
                midiPort = MemIO.memReadU8(ref reader);
                soundEffects = MemIO.memReadU8(ref reader);
                gammaCorrection = MemIO.memReadU8(ref reader);
                difficultyLevel = MemIO.memReadS8(ref reader);
                ReadBytes(ref reader, joyButtonAssign, joyButtonAssign.Length);
                Nortsong.tyrMusicVolume = MemIO.memReadU16LE(ref reader);
                Nortsong.fxVolume = MemIO.memReadU16LE(ref reader);
                inputDevice1 = MemIO.memReadU8(ref reader);
                inputDevice2 = MemIO.memReadU8(ref reader);
                ReadBytes(ref reader, dosKeySettings, dosKeySettings.Length);

                invalid |= reader.error;
            }

            inputDevice_ = 0;
            if (jConfigure == 0) jConfigure = 1;
            versionNum = 2;
            if (Nortsong.tyrMusicVolume > 255) Nortsong.tyrMusicVolume = 255;
            if (Nortsong.fxVolume > 255) Nortsong.fxVolume = 255;
        }

        if (invalid)
        {
            Console.WriteLine("\nInvalid or missing TYRIAN.CFG! Continuing using defaults.\n");
            background2 = true;
            gameSpeed = 4;
            inputDevice_ = 0;
            jConfigure = 0;
            versionNum = 2;
            processorType = 3;
            midiPort = 1;
            soundEffects = 1;
            gammaCorrection = 0;
            difficultyLevel = 0;
            Array.Copy(defaultJoyButtonAssign, joyButtonAssign, joyButtonAssign.Length);
            Nortsong.tyrMusicVolume = 223;
            Nortsong.fxVolume = 223;
            inputDevice1 = 0;
            inputDevice2 = 0;
            Array.Copy(defaultDosKeySettings, dosKeySettings, dosKeySettings.Length);
        }

        load_opentyrian_config();
        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
        JE_initProcessorType();
    }

    public static void saveConfiguration()
    {
        byte[] data = new byte[28];
        fixed (byte* dp = data)
        {
            MemWriter writer = new() { data = dp, size = (nuint)data.Length, error = false };

            MemIO.memWriteBool(ref writer, background2);
            MemIO.memWriteU8(ref writer, gameSpeed);
            MemIO.memWriteU8(ref writer, inputDevice_);
            MemIO.memWriteU8(ref writer, jConfigure);
            MemIO.memWriteU8(ref writer, versionNum);
            MemIO.memWriteU8(ref writer, processorType);
            MemIO.memWriteU8(ref writer, midiPort);
            MemIO.memWriteU8(ref writer, soundEffects);
            MemIO.memWriteU8(ref writer, gammaCorrection);
            MemIO.memWriteS8(ref writer, difficultyLevel);
            WriteBytes(ref writer, joyButtonAssign, joyButtonAssign.Length);
            MemIO.memWriteU16LE(ref writer, Nortsong.tyrMusicVolume);
            MemIO.memWriteU16LE(ref writer, Nortsong.fxVolume);
            MemIO.memWriteU8(ref writer, inputDevice1);
            MemIO.memWriteU8(ref writer, inputDevice2);
            WriteBytes(ref writer, dosKeySettings, dosKeySettings.Length);
        }

        Stream? f = CFile.dir_fopen_warn(get_user_directory(), "tyrian.cfg", "wb");
        if (f != null)
        {
            f.Write(data, 0, data.Length);
            CFile.fclose(f);
        }

        save_opentyrian_config();
    }

    public static void loadSaves()
    {
        bool invalid = false;

        Stream? f = CFile.dir_fopen_warn(get_user_directory(), "tyrian.sav", "rb");
        if (f == null)
        {
            invalid = true;
        }
        else
        {
            byte[] data = new byte[SAVE_FILE_SIZE + 4];
            int size = 0;
            int r;
            while (size < data.Length && (r = f.Read(data, size, data.Length - size)) > 0) size += r;
            CFile.fclose(f);

            invalid = !decryptSaveData(data);

            fixed (byte* dp = data)
            {
                MemReader reader = new() { data = dp, size = (nuint)size, error = false };

                for (int i = 0; i < saveFiles.Length; ++i)
                {
                    var sf = saveFiles[i];
                    sf.encode = MemIO.memReadU16LE(ref reader);
                    sf.level = MemIO.memReadU16LE(ref reader);
                    ReadBytes(ref reader, sf.items, sf.items.Length);
                    sf.score = (int)MemIO.memReadU32LE(ref reader);
                    sf.score2 = (int)MemIO.memReadU32LE(ref reader);
                    byte levelNameLen = MemIO.memReadU8(ref reader);
                    ReadBytes(ref reader, sf.levelName, 9);
                    sf.levelName[Math.Min(levelNameLen, (byte)9)] = 0;
                    ReadBytes(ref reader, sf.name, 14);
                    sf.name[14] = 0;
                    sf.cubes = MemIO.memReadU8(ref reader);
                    ReadBytes(ref reader, sf.power, sf.power.Length);
                    sf.episode = MemIO.memReadU8(ref reader);
                    ReadBytes(ref reader, sf.lastItems, sf.lastItems.Length);
                    sf.difficulty = MemIO.memReadU8(ref reader);
                    sf.secretHint = MemIO.memReadU8(ref reader);
                    sf.input1 = MemIO.memReadU8(ref reader);
                    sf.input2 = MemIO.memReadU8(ref reader);
                    sf.gameHasRepeated = MemIO.memReadBool(ref reader);
                    sf.initialDifficulty = MemIO.memReadU8(ref reader);
                    sf.highScore1 = MemIO.memReadS32LE(ref reader);
                    sf.highScore2 = MemIO.memReadS32LE(ref reader);
                    byte highScoreNameLen = MemIO.memReadU8(ref reader);
                    ReadBytes(ref reader, sf.highScoreName, 29);
                    sf.highScoreName[Math.Min(highScoreNameLen, (byte)29)] = 0;
                    sf.highScoreDiff = MemIO.memReadU8(ref reader);
                }

                ReadBytes(ref reader, editorItemAvail, editorItemAvail.Length);
                editorLevel = (ushort)((editorItemAvail[98] << 8) | editorItemAvail[99]);

                invalid |= reader.error;
            }
        }

        if (invalid)
        {
            foreach (var sf in saveFiles) ClearSave(sf);

            for (int i = 0; i < SAVE_FILES_NUM; ++i)
            {
                var sf = saveFiles[i];
                sf.level = 0;
                for (int j = 0; j < 14; ++j) sf.name[j] = (byte)' ';
                sf.name[14] = 0;

                sf.highScore1 = (int)((MtRand.mt_rand() % 20 + 1) * 1000);
                if (i % 6 < 3)
                {
                    sf.highScore2 = 0;
                    StrCpy(sf.highScoreName, defaultHighScoreNames[MtRand.mt_rand() % (uint)defaultHighScoreNames.Length]);
                }
                else
                {
                    sf.highScore2 = (int)((MtRand.mt_rand() % 20 + 1) * 1000);
                    StrCpy(sf.highScoreName, defaultTeamNames[MtRand.mt_rand() % (uint)defaultTeamNames.Length]);
                }
                sf.highScoreDiff = 0;
            }

            Array.Copy(initialEditorItemAvail, editorItemAvail, editorItemAvail.Length);
            editorLevel = 800;
        }
    }

    private static void ClearSave(JE_SaveFileType sf)
    {
        sf.encode = 0; sf.level = 0; Array.Clear(sf.items); sf.score = sf.score2 = 0;
        Array.Clear(sf.levelName); Array.Clear(sf.name); sf.cubes = 0; Array.Clear(sf.power);
        sf.episode = 0; Array.Clear(sf.lastItems); sf.difficulty = sf.secretHint = sf.input1 = sf.input2 = 0;
        sf.gameHasRepeated = false; sf.initialDifficulty = 0; sf.highScore1 = sf.highScore2 = 0;
        Array.Clear(sf.highScoreName); sf.highScoreDiff = 0;
    }

    public static void saveSaves()
    {
        byte[] data = new byte[SAVE_FILE_SIZE + 4];
        fixed (byte* dp = data)
        {
            MemWriter writer = new() { data = dp, size = (nuint)data.Length, error = false };

            for (int i = 0; i < saveFiles.Length; ++i)
            {
                var sf = saveFiles[i];
                MemIO.memWriteU16LE(ref writer, sf.encode);
                MemIO.memWriteU16LE(ref writer, sf.level);
                WriteBytes(ref writer, sf.items, sf.items.Length);
                MemIO.memWriteU32LE(ref writer, (uint)sf.score);
                MemIO.memWriteU32LE(ref writer, (uint)sf.score2);
                MemIO.memWriteU8(ref writer, (byte)CStrLen(sf.levelName));
                WriteBytes(ref writer, sf.levelName, 9);
                WriteBytes(ref writer, sf.name, 14);
                MemIO.memWriteU8(ref writer, sf.cubes);
                WriteBytes(ref writer, sf.power, sf.power.Length);
                MemIO.memWriteU8(ref writer, sf.episode);
                WriteBytes(ref writer, sf.lastItems, sf.lastItems.Length);
                MemIO.memWriteU8(ref writer, sf.difficulty);
                MemIO.memWriteU8(ref writer, sf.secretHint);
                MemIO.memWriteU8(ref writer, sf.input1);
                MemIO.memWriteU8(ref writer, sf.input2);
                MemIO.memWriteBool(ref writer, sf.gameHasRepeated);
                MemIO.memWriteU8(ref writer, sf.initialDifficulty);
                MemIO.memWriteS32LE(ref writer, sf.highScore1);
                MemIO.memWriteS32LE(ref writer, sf.highScore2);
                MemIO.memWriteU8(ref writer, (byte)CStrLen(sf.highScoreName));
                WriteBytes(ref writer, sf.highScoreName, 29);
                MemIO.memWriteU8(ref writer, sf.highScoreDiff);
            }

            editorItemAvail[98] = (byte)(editorLevel >> 8);
            editorItemAvail[99] = (byte)editorLevel;
            WriteBytes(ref writer, editorItemAvail, editorItemAvail.Length);
        }

        encryptSaveData(data);

        Stream? f = CFile.dir_fopen_warn(get_user_directory(), "tyrian.sav", "wb");
        if (f != null)
        {
            f.Write(data, 0, data.Length);
            CFile.fclose(f);
        }
    }

    private static void encryptSaveData(byte[] data)
    {
        byte y;
        y = 0;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y += data[i];
        data[SAVE_FILE_SIZE] = y;
        y = 0;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y -= data[i];
        data[SAVE_FILE_SIZE + 1] = y;
        y = 1;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y = (byte)((y * data[i]) + 1);
        data[SAVE_FILE_SIZE + 2] = y;
        y = 0;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y ^= data[i];
        data[SAVE_FILE_SIZE + 3] = y;

        for (int i = 0; i < SAVE_FILE_SIZE; ++i)
        {
            data[i] ^= cryptKey[(i + 1) % 10];
            if (i > 0) data[i] ^= data[i - 1];
        }
    }

    private static bool decryptSaveData(byte[] data)
    {
        for (int i = SAVE_FILE_SIZE - 1; ; --i)
        {
            data[i] ^= cryptKey[(i + 1) % 10];
            if (i > 0) data[i] ^= data[i - 1];
            else break;
        }

        byte y;
        y = 0;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y += data[i];
        if (data[SAVE_FILE_SIZE] != y) return false;
        y = 0;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y -= data[i];
        if (data[SAVE_FILE_SIZE + 1] != y) return false;
        y = 1;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y = (byte)((y * data[i]) + 1);
        if (data[SAVE_FILE_SIZE + 2] != y) return false;
        y = 0;
        for (int i = 0; i < SAVE_FILE_SIZE; ++i) y ^= data[i];
        if (data[SAVE_FILE_SIZE + 3] != y) return false;

        return true;
    }
}
