# 移植進度 (PORTING)

OpenTyrian (C, `../sources/`) → .NET 10 / C# 移植追蹤。

## 架構
- `Core/`（class lib，零 SDL）：Ports 介面 + 遊戲邏輯
- `App/`（WinExe，參考 Core）：SDL Adapters + `Program.cs` 組合根
- 依賴單向 `App → Core`；換平台只改 Adapter。
- SDL 繫結：`Sayers.SDL2.Core`（SDL2-CS 的 NuGet 包裝，內含原生 SDL2.dll）。

## 已完成
- [x] **版控**：git 初始化 + 推送至 GitHub `erspicu/AprCSTyrian`（private）；`.gitignore` 排除
      `Build`/`private`/`temp`/`bin`/`obj` 與 VS C# 慣例檔；`sources/` 以對照參考提交（移除巢狀 .git）
- [x] **Phase A 基礎**：`CTypes`(JE_* 別名)、`CMem`(malloc/calloc/realloc/free + DEBUG leak 追蹤)、
      `Opentyr`(常數/巨集/版本)、`MtRand`(mtrand.c 指標式 MT，非託管 state)、
      `Globals`(平台/路徑橋接)、`TyrianHaltException`+`Varz.JE_tyrianHalt`
- [x] **Phase B 部分**：`CFile`(file.c：data_dir/dir_fopen/fread・fwrite_die/typed・scalar 讀寫，FILE*→Stream)、
      `MemIO`(memreader.c/memwriter.c：MemReader/MemWriter 指標式 LE)
- [x] **Phase C 影像/調色盤**：`SdlShim`(SDL_Color/Rect/Surface) + `Vga256d`(繪圖原語) +
      `Video`(video.c：VGAScreen surfaces/init_video/JE_showVGA→IVideoBackend) +
      `Palette`(palette.c：JE_loadPals/set_palette/fade*) +
      `Nortsong`(計時) + `Keyboard`/`Mouse`(最小橋接)
- [x] **可視里程碑**：ported 管線實機跑通（VGAScreen 非託管 → JE_showVGA → IVideoBackend，無 crash）
- [x] **圖片載入**：`Pcxload`(pcxload.c) + `Picload`(picload.c) + `Pcxmast`(pcxmast.c)
- [x] **真實資料里程碑**：Tyrian 2.1 資料已置於 `Build/data`（gitignore），實機輪播真實 `tyrian.pic` 圖片無 crash
- [x] **Sprites/Fonts**：`Sprites`(sprite.c：Sprite/Sprite2 結構 + 全 blit 變體 + shape table 載入)、
      `FontDraw`(font.c)、`Fonthand`(fonthand.c：fontMap/JE_outText/dString/textShade…)、`Sndmast`(音效常數)
- [x] **里程碑**：真實 `tyrian.shp` 字型疊在 `tyrian.pic` 圖上顯示文字（"APRCSTYRIAN" 等），無 crash
- [x] **背景星空**：`Starlib`(starlib.c) 3D 星空 → 標題星空背景（互動鍵暫簡化）
- [x] **varz 結構**：`VarzConst`(常數/enum) + 資料結構（JE_SingleEnemyType/EnemyShotType/Explosion/
      rep_explosion_type/superpixel_type/JE_EventRecType）+ `Varz` 全域陣列（enemy/enemyShot/explosions…）
- [x] **player/episodes 結構**：`Lvlmast`(維度常數)、`Player`(PlayerItems/Player/PlayerSidekick + player[2] +
      all_players_dead/alive，InlineArray 保 value 語意)、`Episodes`(JE_WeaponType/Port/Power/Special/Option/
      Shield/Ship/EnemyDat 結構 + weaponPort/weapons/ships/options/… 全域)、`Config`(最小占位 twoPlayerMode/galagaMode)
- [x] **varz 函式（自足子集）**：const 資料表(SAWeapon/optionSelect/keyboardCombos/shipCombos…) +
      JE_setupExplosion/setupExplosionLarge/doSP/drawSP/portConfigs/wipeShieldArmorBars/drawOptions/drawOptionLevel
- [x] **shots 武器邏輯**：`Shots`(shots.c)：`PlayerShotDataType` + `playerShotData`/`shotAvail` +
      `simulate_player_shots`/`player_shot_set_direction`/`player_shot_move_and_draw`(out 參數)/`player_shot_create`
      （補 config 全域 power/shotRepeat/shotMultiPos/background2、varz soundQueue/x/y）
- [x] **音訊完整（音樂 + 音效）**：`Loudness`(loudness.c 混音器→IAudioSource)、
      `Nortsong`(loadSndFile/JE_playSampleNum/JE_changeVolume)、
      **`Opl`(opl.c DOSBox OPL2 FM 模擬器)**、**`Lds`(lds_play.c .lds 音樂播放器，驅動 OPL)**
      → OPL FM 音樂 + 取樣音效皆可發聲；IAudioBackend Lock/Unlock，SdlAudio mono 44100
- [x] **完整輸入（中性事件佇列）**：`Keyboard`(keyboard.c)、`Mouse`(mouse.c)、`Joystick`(stub)、
      `SdlKeys`(SDL scancode/keymod/KEY_COMBO)、`PlatformEvent`/`IInputBackend.PollEvent`(App SdlInput 翻譯)
      → Core 保持 SDL-free；IVideoBackend 加 MapWindowToScreen/ToggleFullscreen；移除 GameKey
- [x] **config 序列化**：`Config`(config.c)：二進位設定 tyrian.cfg + 存檔/高分 tyrian.sav
      (loadConfiguration/saveConfiguration/loadSaves/saveSaves/JE_saveGame/JE_loadGame + 加解密 + 預設高分榜
      + JE_initProcessorType/setNewGameSpeed)，用已移植 MemIO；TyrianGame 啟動載入
- [x] **物品資料庫載入**：`Episodes.JE_loadItemDat`(tyrian.hdt → weapons/weaponPort/special/powerSys/ships/
      options/shields/enemyDat 全部解析)、JE_initEpisode/scanForEpisodes/findNextEpisode；`Lvllib` 最小占位
      （已驗證：船艦名稱如 "USP Talon"/"Gencore Phoenix" 正確解析）
- [x] **editship + 船艦資訊**：`Editship`(editship.c：JE_loadExtraShapes/JE_decryptShips，額外船艦圖)、
      `Varz.JE_getShipInfo`/`JE_SGr`(解先前延後；shipGrPtr 改 Sprite2_array 值複製)
- [x] **關卡偏移分析**：`Lvllib.JE_analyzeLevel`(lvllib.c：讀 tyrian{N}.lvl 取 lvlNum/lvlPos)
- [x] **選單/說明文字**：`Helptext`(helptext.c：JE_loadHelpText 解密 tyrian.hdt Pascal 字串→全部文字表
      +JE_helpBox/HBox)；`Menus` 文字占位（已驗證："Episode 1: Escape"/"Start New Game" 正確）
- [x] **命令列參數**：`ArgParse`(arg_parse.c getopt 重實作)+`Params`(params.c JE_paramCheck)+`Xmas`(占位)；
      網路選項視為不支援；TyrianGame/Program 串接 argv
- [x] **JE_loadMap 關卡載入**：`Tyrian2.JE_loadMap`(tyrian2.c)：episode 檔 ]X 命令解析(過場/設定/跳關/galaga/engage) +
      Part2 讀 LEVELS.DAT(mapX/敵人/event)/shapes?.dat→megaData(shape blob+mainmap 指標)。實機驗證 ep1lv1 正確
      (maxEvent=1009/levelEnemyMax=7)。JE_itemScreen/JE_nextEpisode/JE_displayText/平移過場 暫 stub/簡化
- [x] **animlib**：`Animlib.playAnim`(animlib.c：.ANM 動畫 Run/Skip/Dump 解碼播放；JE_loadMap ]A 命令所需)
- [x] **megaData 地圖結構**：`JE_MegaData`(varz.h JE_MegaDataType1/2/3)：mainmap=nint[](byte* 指標)、
      shapes=非託管連續 672-byte blob(stable ptr)、megaData1/2/3 全域 + alloc/free(無 leak)；解鎖 JE_loadMap/背景
- [x] **護盾/裝甲 bar**：`Nortvars`(nortvars.c：JE_dBar3/JE_barDrawShadow) +
      `Varz.JE_drawShield`/`JE_drawArmor`(解先前延後)
- [x] **mainint 起手**：`Mainint.JE_initPlayerData`(新遊戲玩家初始化)/`JE_drawPortConfigButtons`
- [x] **真實標題畫面/主選單**：`Tyrian2.titleScreen`(tyrian2.c)：tyrian.pic 標題圖 + Tyrian logo +
      選單(鍵盤/滑鼠導航) + SONG_TITLE 配樂 + 淡入 + 特殊碼；Quit/ESC 結束。TyrianGame 主迴圈改走 titleScreen
      （取代星空 demo）。開機序列: intro_logos→title→menu。「High Scores」已接 JE_highScoreScreen；「Instructions」已接 JE_helpSystem；「Load Game」已接 JE_loadScreen；僅 Setup(game_menu.c) 暫 stub
- [x] **New Game 選單流程**：`Menus.gameplaySelect`/`episodeSelect`/`difficultySelect`(menus.c，含隱藏難度解鎖) +
      `Tyrian2.newGame`(1P/2P/arcade 模式設定、cash/ship)；「Start New Game」→ 真實 gameplay→episode→difficulty 導航
- 註：config_file.c(opentyrian.cfg INI)、關卡地圖載入(megaData)、varz 其餘重度函式、
      主遊戲邏輯(mainint titleScreen/JE_main、tyrian2、game_menu、opentyr 進入點) 待後續
- [x] solution + 雙專案骨架（`AprCSTyrian.slnx` / `Core` / `App`）
- [x] Core Ports：`IVideoBackend` `IAudioBackend` `IInputBackend` `IClock` `IFileSystem` `IGamePlatform`
- [x] Core 型別：`Color`(8-bit RGB, `FromVga`) / `GameKey` / `VgaScreen`(320×200)
- [x] SDL Adapters：`SdlVideo`(window+renderer+streaming texture，邏輯尺寸整數縮放) /
      `SdlInput`(scancode→GameKey、按住+邊緣) / `SdlClock` / `SdlAudio`(callback 模式) /
      `PhysicalFileSystem` / `SdlPlatform`(SDL 生命週期聚合)
- [x] `Program.cs` 組合根（WinExe 無 console，crash 寫 `crash.log`）
- [x] **里程碑 0**：`TyrianGame` 顯示動態測試畫面，視窗開啟、迴圈執行、ESC/X 關閉，建置→`Build/` 並實機跑通

## 待移植（依原始 C 模組，順序待定）
- [ ] 資料檔載入：`file.c` / `lvllib.c` / `pcxload.c` / `picload.c`（讀 Tyrian 2.1 資料）
- [ ] 調色盤與繪圖：`palette.c` / `vga256d.c` / `video.c` / `sprite.c` / `font.c` `fonthand.c`
- [ ] 亂數：`mtrand.c`（需與原版序列一致）
- [ ] 設定/存檔：`config.c` / `config_file.c`（二進位，注意型別寬度與 little-endian）
- [ ] 音訊：`loudness.c`(混音) / `opl.c`(FM 模擬) / `lds_play.c` / `nortsong.c` / `jukebox.c`
- [ ] 選單與主流程：`opentyr.c`(進入點) / `setup`/`menus.c` / `game_menu.c` / `mainint.c`
- [ ] 遊戲核心：`tyrian2.c` / `player.c` / `shots.c` / `destruct.c` / `backgrnd.c` / `starlib.c`
- [ ] 輸入：`keyboard.c` / `joystick.c` / `mouse.c`
- [~] 網路 `network.c`：**不移植**（決議跳過）

## 備註
- 遊戲需 Tyrian 2.1 freeware 資料檔：https://camanis.net/tyrian/tyrian21.zip
  （放入 `Build/data/` 或以執行參數指定資料根目錄）
- 建置指令：`dotnet build App/App.csproj -c Release -o C:\ai_project\AprCSTyrian\Build`
  （建置 App 即會帶入 Core 與原生 DLL；勿對 solution 用 `-o`，會有 NETSDK1194 警告）
