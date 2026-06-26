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
- [x] **player.lives 別名**：C `byte* lives=&weapon[p].power` → livesPort 索引 + Lives 屬性; 街機殘機顯示接上; 驗證雙向別名
- [x] **過關摘要 JE_endLevelAni** + adjust_difficulty + cube 收集動畫(JE_drawCube)(完成關卡名/現金/摧毀率/難度調整/ShipEdit 權限/資料方塊收集; glow 文字; 接入過關流程)
- [x] **連動敵人群組死亡**：JE_killEnemyGroup(被擊毀敵人 + linknum 編組一起死亡，各自 enemydie/計分/爆炸; edlevel==-1 score item 轉換/globalFlags/254 事件跳轉)
- [x] **enemyOnScreen 計數**：畫面內非受損敵人計數；砲塔發射改僅限畫面內敵人(對應 JE_drawEnemy 339-351)
- [x] **edlevel 傷害態**：JE_enemyDamageTransform(受擊跨越 edlevel→切換受損圖 edgr/受損動畫 edani/或死亡)
- [x] **boss 血條**：draw_boss_bar+JE_barX(找連動敵人最低 armor 畫血條)；事件 79 設定 + JE_main setup 清空
- [x] **後繼敵人(enemydie)生成**：JE_newEnemy；死亡時生成 enemydie 後繼(多階段敵人)+ esize-based 爆炸/音效 + 計分 cubeMax/cash；驗證 type91→type502
- [x] **特殊砲塔型 251-255**：Suck-O-Magnet(吸引)/ShortRange Magnet(推±2)/Magneto RePulse(排斥)/Savara DualMissile
- [x] 敵人**受擊閃白**：命中時 enemy.filter=blast_filter，下一幀 blit_enemy 變色繪製
- [x] **JE_updateEnemies(敵人移動/繪製核心)**：homing + 動畫 + size 多格(2x2)繪製 + 立方加速(curved) + 彈跳(xmin/maxbounce) + score item 邊界 + tempBackMove + 砲塔發射; blit_enemy(含 filter)
- [x] 敵人 **homing AI**：xaccel/yaccel 朝玩家加速(JE_drawEnemy 188-214)，含 89 基準/強度封頂；驗證 exc/eyc 朝玩家
- [x] **關卡完成流程(簡化)**：JE_main goto 式關卡迴圈；事件處理完 + 無敵人/敵彈 → 倒數過關 → mainLevel=nextLevel 載入下一關(章節結束回標題)
- [x] **爆炸繪製**：`Tyrian2.JE_drawExplosions`(序列爆炸 rep_explosions + 一般 explosions 更新/繪製，explosionSpriteSheet 載入)；接入 JE_main
- [x] **敵人發射敵彈 + 敵彈↔玩家碰撞**：enemyTurretFire(砲塔→weapons 建敵彈+瞄準玩家) + simulateEnemyShots(移動/homing/繪製/擊中) + JE_playerDamage(護盾→裝甲→死亡爆炸)；實機驗證 shield 20→15
- [x] **HUD**：`Mainint.JE_inGameDisplays`(分數cash/特殊武器圖示/超級炸彈) + 護盾/裝甲 bar(JE_drawShield/Armor)；街機 lives 待 lives 指標
- [x] **子彈↔敵人碰撞**：JE_main player_shot_move_and_draw + box collision(同原版 25/29/13/15) → armorleft 扣血/死亡爆炸/計分(cash+evalue)/回收；dmg 99(冰)/250+(無限) 處理
- [x] **JE_makeEnemy 敵人工廠**：由 enemyDat 初始化敵人(60+ 欄位)；sprite2s 改值複製/enemydatofs 改索引(C 指標→C# 託管陣列)
      實機驗證: 5 種敵人 evalue/armor/起始座標數值正確
- [x] **JE_main 骨架（進入關卡）**：`Tyrian2.JE_main`(tyrian2.c)：忠實關卡 setup(JE_loadMap+map位置+玩家+背景旗標) +
      最小主迴圈(三層捲動背景)；TyrianGame 標題迴圈接 JE_main(opentyr main 流程)。實機驗證進入 ep1lv1 捲動無 crash
      完整遊戲邏輯(事件系統/敵人 AI/玩家移動/射擊/碰撞/HUD)待續
- [x] **背景繪製 backgrnd.c**：`Backgrnd`(draw_background_1/2/3 + blit_background_row/blend + JE_darkenBackground)
      flat-index 走訪 megaData.mainmap(C 的 byte** → nint[] 索引)；實機驗證 draw_background_1=56448/64000 非零像素
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
- [x] **game_menu.c 逐步移植**：JE_drawItem + 選單狀態機(MENU enum/menuChoices/curSel/cube) + JE_drawMenuHeader/Choices + 經濟 helper(playeritem_map ref-別名/JE_getCost/weapon_upgrade_cost/JE_cashLeft/JE_drawScore)
- [x] **game_menu.c 商店子選單**：`GameMenu.JE_genItemMenu`(建立升級子選單物品清單；驗證武器名 Pulse/Multi/Vulcan Cannon + DONE)
- [x] **game_menu.c JE_scaleBitmap**(最近鄰縮放 leaf；ship specs/scaleInPicture 用)
- [x] **game_menu.c 星圖導航繪製**：JE_drawDots/JE_drawPlanet + 星圖狀態(planetX/Y/planetDots/navX/tempNav/planetAni)
- [x] **game_menu.c JE_computeDots/JE_partWay**(計算星圖路線導航點，sqrt(sqrt(dist²)) 點數 + 線性插值)
- [x] **game_menu.c JE_updateNavScreen**(星圖導航畫面總繪製：網格/星球/虛線 + 平滑捲動 + 動畫)
- [x] **game_menu.c 船艦規格屏**：JE_drawShipSpecs(綠化技術屏)+JE_scaleInPicture(放大進場)+JE_doShipSpecs
- [x] **game_menu.c 武器預覽**：JE_initWeaponView/weaponViewFrame/weaponSimUpdate(武器射擊模擬+power bar+升降級成本) + 遊戲內星空
- [x] **game_menu.c JE_itemScreen 商店主迴圈完整移植**：JE_itemScreen + JE_menuFunction + draw_ship_illustration + load_cubes/load_cube + JE_drawMainMenuHelpText + JE_quitRequest;接入 JE_loadMap。對照驗證:商店畫面(frame 224-276)與原版逐像素吻合(原為黑屏)
- [x] **JE_main HUD/介面合成**：JE_starShowVGA(game_screen playfield 寬264偏移+24 合成到 VGAScreenSeg) + JE_main 載入介面圖(JE_loadPic 3)/JE_drawOptions/關卡名 + repoint VGAScreen↔game_screen + 護盾裝甲畫面板。實機:介面面板(FRONT/REAR GUN/MODE/TYRIAN/shield)正確顯示(原為無面板)
- [x] **game_menu.c JE_drawLines/JE_drawNavLines**(網格背景線 + 星圖導航網格)
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

## 各原始 C 模組移植狀態（依實際進度）
- [x] 資料檔載入：`file.c`(CFile) / `lvllib.c`(Lvllib) / `pcxload.c`(Pcxload) / `picload.c`(Picload)
- [x] 調色盤與繪圖：`palette.c`(Palette) / `vga256d.c`(Vga256d) / `video.c`(Video) / `sprite.c`(Sprite) /
      `font.c`(Font) / `fonthand.c`(Fonthand)
- [x] 亂數：`mtrand.c`(MtRand，指標式 MT 與原版序列一致)
- [x] 設定/存檔：`config.c`(Config，含加解密/存檔/高分)　／　[ ] `config_file.c`(opentyrian.cfg INI，未移植)
- [x] 音訊：`loudness.c`(混音) / `opl.c`(OPL2 FM) / `lds_play.c`(Lds) / `nortsong.c`(Nortsong)　／　[ ] `jukebox.c`(未移植)
- [x] 輸入：`keyboard.c`(Keyboard，中性事件佇列) / `mouse.c`(Mouse) / `joystick.c`(Joystick stub)
- [x] 遊戲核心：`shots.c`(Shots) / `backgrnd.c`(Backgrnd) / `starlib.c`(Starlib)
- [x] **`tyrian2.c`**：titleScreen / JE_loadMap / JE_main 主迴圈 / JE_eventSystem / 完整敵人系統
      (makeEnemy/updateEnemies/turret/碰撞/群組死亡/edlevel/boss)　／　intro_logos / newGame
- [x] **`mainint.c`**：JE_initPlayerData / highScore / helpSystem / loadScreen / inGameDisplays /
      playerDamage / endLevelAni / adjust_difficulty / JE_getCost / drawCube　(JE_playerMovement 完整版未移植，主迴圈用簡化版)
- [~] **`menus.c`**(Menus，開新遊戲選單 ✅) ／ **`opentyr.c`**(main 流程在 TyrianGame ✅，setupMenu 未移植)
- [~] **`game_menu.c`**(GameMenu)：drawItem / 選單狀態機 / drawMenuHeader·Choices / 經濟層(playeritem_map/
      getCost/cashLeft/drawScore) / genItemMenu / scaleBitmap / 星圖(computeDots/drawDots/drawPlanet/drawLines/NavLines) ✅
      ／ **待**：updateNavScreen / drawShipSpecs / 武器預覽 / drawMainMenuHelpText / menuFunction / **JE_itemScreen 主迴圈**
- [ ] `player.c`(玩家移動/碰撞，主迴圈用簡化版) ／ `destruct.c`(毀滅模式，未移植)
- [~] 網路 `network.c`：**不移植**（決議跳過）

> **總結**：核心遊戲（開機→選單→多關卡戰鬥→過關→循環）已完整可玩且忠實移植。
> 唯一未完成的主要功能是 `game_menu.c` 的**商店 `JE_itemScreen` 主迴圈**（helper 已大致湊齊，待組裝）；
> 次要未移植：config_file.c(INI)、jukebox.c、destruct.c(毀滅模式)、setupMenu、JE_playerMovement 完整版。

## 備註
- 遊戲需 Tyrian 2.1 freeware 資料檔：https://camanis.net/tyrian/tyrian21.zip
  （放入 `Build/data/` 或以執行參數指定資料根目錄）
- 建置指令：`dotnet build App/App.csproj -c Release -o C:\ai_project\AprCSTyrian\Build`
  （建置 App 即會帶入 Core 與原生 DLL；勿對 solution 用 `-o`，會有 NETSDK1194 警告）
