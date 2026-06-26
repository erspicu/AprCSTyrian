namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/mainint.c 的玩家更新三函式：
/// JE_playerMovement / JE_mainGamePlayerFunctions / JE_playerCollide。
/// 逐行對照原始 C；網路(#ifdef WITH_NETWORK)略過，特殊武器/Street-Fighter/demo 子系統以 TODO 空殼保留。
/// </summary>
internal static unsafe partial class Mainint
{
    // === 網路/輸入相關全域（network.c 不移植；非網路預設值） ===
    public static byte thisPlayerNum = 1;          // network.c: 非網路恆 1
    public static bool moveOk;                      // network.c: JE_boolean moveOk
#pragma warning disable CS0649 // 待 joystick/pause 子系統移植後指派
    public static bool ingamemenu_pressed, pause_pressed; // mainint.c
#pragma warning restore CS0649
    public static string network_player_name = "", network_opponent_name = "";

    private static string NameStr(byte* p)
    {
        int n = 0;
        while (n < 30 && p[n] != 0) n++;
        var c = new char[n];
        for (int i = 0; i < n; ++i) c[i] = (char)p[i];
        return new string(c);
    }

    /// <summary>對應 mainint.c:JE_getName。</summary>
    public static string JE_getName(byte pnum)
    {
        if (pnum == thisPlayerNum && network_player_name.Length > 0)
            return network_player_name;
        else if (network_opponent_name.Length > 0)
            return network_opponent_name;

        return Helptext.miscText[47 + pnum];
    }

    /// <summary>
    /// 對應 mainint.c:replay_demo_keys（2279-2315）—— 每幀讀 demo 按鍵並套用至 player[0]/button[]，EOF 回 false。
    /// </summary>
    public static bool replay_demo_keys()
    {
        Stream demo_file = Varz.demo_file!;

        byte* temp2 = stackalloc byte[2];

        while (Varz.demo_keys_wait == 0)
        {
            Varz.demo_keys = 0;
            byte k = 0;
            nuint got1 = CFile.fread_u8(&k, 1, demo_file);
            Varz.demo_keys = k;

            temp2[0] = 0; temp2[1] = 0;
            nuint got2 = CFile.fread_u8(temp2, 2, demo_file);
            Varz.demo_keys_wait = (ushort)((temp2[0] << 8) | temp2[1]);

            if (got1 < 1 || got2 < 2)  // feof(demo_file)
            {
                // no more keys
                return false;
            }
        }

        Varz.demo_keys_wait--;

        Player p0 = Players.player[0];

        if ((Varz.demo_keys & (1 << 0)) != 0)
            p0.y -= VarzConst.CURRENT_KEY_SPEED;
        if ((Varz.demo_keys & (1 << 1)) != 0)
            p0.y += VarzConst.CURRENT_KEY_SPEED;

        if ((Varz.demo_keys & (1 << 2)) != 0)
            p0.x -= VarzConst.CURRENT_KEY_SPEED;
        if ((Varz.demo_keys & (1 << 3)) != 0)
            p0.x += VarzConst.CURRENT_KEY_SPEED;

        button[0] = (Varz.demo_keys & (1 << 4)) != 0;
        button[3] = (Varz.demo_keys & (1 << 5)) != 0;
        button[1] = (Varz.demo_keys & (1 << 6)) != 0;
        button[2] = (Varz.demo_keys & (1 << 7)) != 0;

        return true;
    }

    /// <summary>對應 mainint.c:JE_SFCodes —— Street Fighter 式方向連續輸入解碼以觸發特殊武器。</summary>
    public static void JE_SFCodes(byte playerNum_, int PX_, int PY_, int mouseX_, int mouseY_)
    {
        byte temp, temp2, temp3, temp4, temp5;

        uint ship = Players.player[playerNum_ - 1].items.ship;

        /*Get direction*/
        if (playerNum_ == 2 && ship < 15)
        {
            ship = 0;
        }

        if (ship < 15)
        {
            temp2 = (byte)(((mouseY_ > PY_) ? 1 : 0) +    /*UP*/
                           ((mouseY_ < PY_) ? 1 : 0) +    /*DOWN*/
                           ((PX_ < mouseX_) ? 1 : 0) +    /*LEFT*/
                           ((PX_ > mouseX_) ? 1 : 0));    /*RIGHT*/
            temp = (byte)(((mouseY_ > PY_) ? 1 : 0) * 1 + /*UP*/
                          ((mouseY_ < PY_) ? 1 : 0) * 2 + /*DOWN*/
                          ((PX_ < mouseX_) ? 1 : 0) * 3 + /*LEFT*/
                          ((PX_ > mouseX_) ? 1 : 0) * 4); /*RIGHT*/

            if (temp == 0) // no direction being pressed
            {
                if (!Mainint.button[0]) // if fire button is released
                {
                    temp = 9;
                    temp2 = 1;
                }
                else
                {
                    temp2 = 0;
                    temp = 99;
                }
            }

            if (temp2 == 1) // if exactly one direction pressed or fire button is released
            {
                temp += (byte)((Mainint.button[0] ? 1 : 0) * 4);

                temp3 = (byte)(Config.superTyrian ? 21 : 3);
                for (temp2 = 0; temp2 < temp3; temp2++)
                {
                    /*Use SuperTyrian ShipCombos or not?*/
                    temp5 = Config.superTyrian ? Varz.shipCombosB[temp2] : Varz.shipCombos[ship, temp2];

                    // temp5 == selected combo in ship
                    if (temp5 == 0) /* combo doesn't exists */
                    {
                        // mark twiddles as cancelled/finished
                        Varz.SFCurrentCode[playerNum_ - 1, temp2] = 0;
                    }
                    else
                    {
                        // get next combo key
                        temp4 = Varz.keyboardCombos[temp5 - 1, Varz.SFCurrentCode[playerNum_ - 1, temp2]];

                        // correct key
                        if (temp4 == temp)
                        {
                            Varz.SFCurrentCode[playerNum_ - 1, temp2]++;

                            temp4 = Varz.keyboardCombos[temp5 - 1, Varz.SFCurrentCode[playerNum_ - 1, temp2]];
                            if (temp4 > 100 && temp4 <= 100 + Lvlmast.SPECIAL_NUM)
                            {
                                Varz.SFCurrentCode[playerNum_ - 1, temp2] = 0;
                                Varz.SFExecuted[playerNum_ - 1] = (byte)(temp4 - 100);
                            }
                        }
                        else
                        {
                            if ((temp != 9) &&
                                (temp4 - 1) % 4 != (temp - 1) % 4 &&
                                (Varz.SFCurrentCode[playerNum_ - 1, temp2] == 0 ||
                                 Varz.keyboardCombos[temp5 - 1, Varz.SFCurrentCode[playerNum_ - 1, temp2] - 1] != temp))
                            {
                                Varz.SFCurrentCode[playerNum_ - 1, temp2] = 0;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void JE_playerMovement(Player this_player,
                                         byte inputDevice,
                                         byte playerNum_,
                                         ushort shipGr_,
                                         Sprite2_array shipGrPtr_,
                                         ref ushort mouseX_, ref ushort mouseY_)
    {
        var player = Players.player;

        int mouseXC, mouseYC;
        int accelXC, accelYC;

        if (playerNum_ == 2 || !Config.twoPlayerMode)
        {
            Varz.tempW = Episodes.weaponPort[this_player.items.weapon[Players.REAR_WEAPON].id].opnum;

            if (this_player.weapon_mode > Varz.tempW)
                this_player.weapon_mode = 1;
        }

    redo:

        if (Config.isNetworkGame)
        {
            inputDevice = 0;
        }

        mouseXC = 0;
        mouseYC = 0;
        accelXC = 0;
        accelYC = 0;

        bool link_gun_analog = false;
        float link_gun_angle = 0;

        /* Draw Player */
        if (!this_player.is_alive)
        {
            if (this_player.exploding_ticks > 0)
            {
                --this_player.exploding_ticks;

                if (Varz.levelEndFxWait > 0)
                {
                    Varz.levelEndFxWait--;
                }
                else
                {
                    Varz.levelEndFxWait = (ushort)((MtRand.mt_rand() % 6) + 3);
                    if ((MtRand.mt_rand() % 3) == 1)
                        Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_9;
                    else
                        Varz.soundQueue[5] = (byte)Sndmast.S_EXPLOSION_11;
                }

                int explosion_x = this_player.x + (int)(MtRand.mt_rand() % 32) - 16;
                int explosion_y = this_player.y + (int)(MtRand.mt_rand() % 32) - 16;
                Varz.JE_setupExplosionLarge(false, 0, explosion_x, explosion_y + 7);
                Varz.JE_setupExplosionLarge(false, 0, this_player.x, this_player.y + 7);

                if (Varz.levelEnd > 0)
                    Varz.levelEnd--;
            }
            else
            {
                if (Config.twoPlayerMode || Config.onePlayerAction)  // if arcade mode
                {
                    if (this_player.Lives > 1)  // respawn if any extra lives
                    {
                        --this_player.Lives;

                        Tyrian2.reallyEndLevel = false;
                        Config.shotMultiPos[playerNum_ - 1] = 0;
                        Players.calc_purple_balls_needed(this_player);
                        Config.twoPlayerLinked = false;
                        if (Config.galagaMode)
                            Config.twoPlayerMode = false;
                        this_player.y = 160;
                        this_player.invulnerable_ticks = 100;
                        this_player.is_alive = true;
                        Tyrian2.endLevel = false;

                        if (Config.galagaMode || Episodes.episodeNum == 4)
                            this_player.armor = this_player.initial_armor;
                        else
                            this_player.armor = this_player.initial_armor / 2;

                        if (Config.galagaMode)
                            this_player.shield = 0;
                        else
                            this_player.shield = this_player.shield_max / 2;

                        Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */
                        Varz.JE_drawArmor();
                        Varz.JE_drawShield();
                        Video.VGAScreen = Video.game_screen; /* side-effect of game_screen */
                        goto redo;
                    }
                    else
                    {
                        if (Config.galagaMode)
                            Config.twoPlayerMode = false;
                        if (Tyrian2.allPlayersGone && Config.isNetworkGame)
                            Tyrian2.reallyEndLevel = true;
                    }

                }
            }
        }
        else if (Params.constantDie)
        {
            // finished exploding?  start dying again
            if (this_player.exploding_ticks == 0)
            {
                this_player.shield = 0;

                if (this_player.armor > 0)
                {
                    --this_player.armor;
                }
                else
                {
                    this_player.is_alive = false;
                    this_player.exploding_ticks = 60;
                    Varz.levelEnd = 40;
                }

                Varz.JE_wipeShieldArmorBars();
                Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */
                Varz.JE_drawArmor();
                Video.VGAScreen = Video.game_screen; /* side-effect of game_screen */

                // as if instant death weren't enough, player also gets infinite lives in order to enjoy an infinite number of deaths -_-
                if (player[0].Lives < 11)
                    ++player[0].Lives;
            }
        }

        if (!this_player.is_alive)
        {
            Tyrian2.explosionFollowAmountX = Tyrian2.explosionFollowAmountY = 0;
            return;
        }

        if (!Tyrian2.endLevel)
        {
            mouseX_ = (ushort)this_player.x;
            mouseY_ = (ushort)this_player.y;
            button[1 - 1] = false;
            button[2 - 1] = false;
            button[3 - 1] = false;
            button[4 - 1] = false;

            /* --- Movement Routine Beginning --- */

            if (!Config.isNetworkGame || playerNum_ == thisPlayerNum)
            {
                if (Tyrian2.endLevel)
                {
                    this_player.y -= 2;
                }
                else
                {
                    if (Varz.record_demo || Varz.play_demo)
                        inputDevice = 1;  // keyboard is required device for demo recording

                    // demo playback input
                    if (Varz.play_demo)
                    {
                        if (!replay_demo_keys())
                        {
                            Tyrian2.endLevel = true;
                            Varz.levelEnd = 40;
                        }
                    }

                    /* joystick input */
                    if ((inputDevice == 0 || inputDevice >= 3) && Joystick.joysticks > 0)
                    {
                        // TODO: 待移植 joystick 輸入（poll_joystick/joystick_axis_reduce/joystick_analog_angle
                        //       及 joystick[].x/.y/.action/.action_pressed 未移植；目前 joysticks==0，此區不執行）
                    }

                    /* mouse input */
                    if ((inputDevice == 0 || inputDevice == 2) && Mouse.has_mouse)
                    {
                        button[0] |= (Keyboard.mouseButtonsDown & SdlKeys.SDL_BUTTON(SdlKeys.SDL_BUTTON_LEFT)) != 0;
                        button[1] |= (Keyboard.mouseButtonsDown & SdlKeys.SDL_BUTTON(SdlKeys.SDL_BUTTON_RIGHT)) != 0;
                        button[2] |= (Keyboard.mouseButtonsDown & SdlKeys.SDL_BUTTON(Mouse.mouse_has_three_buttons ? SdlKeys.SDL_BUTTON_MIDDLE : SdlKeys.SDL_BUTTON_RIGHT)) != 0;

                        Keyboard.mouseGetRelativePosition(out int mouseXR, out int mouseYR);
                        mouseXC += mouseXR;
                        mouseYC += mouseYR;
                    }

                    /* keyboard input */
                    if ((inputDevice == 0 || inputDevice == 1) && !Varz.play_demo)
                    {
                        if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_UP]])
                            this_player.y -= VarzConst.CURRENT_KEY_SPEED;
                        if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_DOWN]])
                            this_player.y += VarzConst.CURRENT_KEY_SPEED;

                        if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_LEFT]])
                            this_player.x -= VarzConst.CURRENT_KEY_SPEED;
                        if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_RIGHT]])
                            this_player.x += VarzConst.CURRENT_KEY_SPEED;

                        button[0] |= Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_FIRE]];
                        button[3] |= Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_CHANGE_FIRE]];
                        button[1] |= Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_LEFT_SIDEKICK]];
                        button[2] |= Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_RIGHT_SIDEKICK]];

                        if (Params.constantPlay)
                        {
                            for (uint i = 0; i < 4; i++)
                                button[i] = true;

                            ++this_player.y;
                            this_player.x += Params.constantLastX;
                        }

                        if (Varz.record_demo)
                        {
                            bool new_input = false;

                            for (int i = 0; i < 8; i++)
                            {
                                bool temp = (Varz.demo_keys & (1 << i)) != 0;
                                if (temp != Keyboard.keysactive[Config.keySettings[i]])
                                    new_input = true;
                            }

                            Varz.demo_keys_wait++;

                            if (new_input)
                            {
                                CFile.write_u8(Varz.demo_file!, (byte)(Varz.demo_keys_wait >> 8));
                                CFile.write_u8(Varz.demo_file!, (byte)Varz.demo_keys_wait);

                                Varz.demo_keys = 0;
                                for (int i = 0; i < 8; i++)
                                    Varz.demo_keys = (byte)(Varz.demo_keys | (Keyboard.keysactive[Config.keySettings[i]] ? (1 << i) : 0));

                                CFile.write_u8(Varz.demo_file!, Varz.demo_keys);

                                Varz.demo_keys_wait = 0;
                            }
                        }
                    }

                    if (Config.smoothies[9 - 1])
                    {
                        mouseY_ = (ushort)(this_player.y - (mouseY_ - this_player.y));
                        mouseYC = -mouseYC;
                    }

                    accelXC += this_player.x - mouseX_;
                    accelYC += this_player.y - mouseY_;

                    if (mouseXC > 30)
                        mouseXC = 30;
                    else if (mouseXC < -30)
                        mouseXC = -30;
                    if (mouseYC > 30)
                        mouseYC = 30;
                    else if (mouseYC < -30)
                        mouseYC = -30;

                    if (mouseXC > 0)
                        this_player.x += (mouseXC + 3) / 4;
                    else if (mouseXC < 0)
                        this_player.x += (mouseXC - 3) / 4;
                    if (mouseYC > 0)
                        this_player.y += (mouseYC + 3) / 4;
                    else if (mouseYC < 0)
                        this_player.y += (mouseYC - 3) / 4;

                    if (mouseXC > 3)
                        accelXC++;
                    else if (mouseXC < -2)
                        accelXC--;
                    if (mouseYC > 2)
                        accelYC++;
                    else if (mouseYC < -2)
                        accelYC--;

                }   /*endLevel*/
            }  /*isNetworkGame*/

            /* --- Movement Routine Ending --- */

            moveOk = true;

            /*Street-Fighter codes*/
            JE_SFCodes(playerNum_, this_player.x, this_player.y, mouseX_, mouseY_);

            if (moveOk)
            {
                /* END OF MOVEMENT ROUTINES */

                /*Linking Routines*/

                if (Config.twoPlayerMode && !Config.twoPlayerLinked && this_player.x == mouseX_ && this_player.y == mouseY_ &&
                    Math.Abs(player[0].x - player[1].x) < 8 && Math.Abs(player[0].y - player[1].y) < 8 &&
                    player[0].is_alive && player[1].is_alive && !Config.galagaMode)
                {
                    Config.twoPlayerLinked = true;
                }

                if (playerNum_ == 1 && (button[3 - 1] || button[2 - 1]) && !Config.galagaMode)
                    Config.twoPlayerLinked = false;

                if (Config.twoPlayerMode && Config.twoPlayerLinked && playerNum_ == 2 &&
                    (this_player.x != mouseX_ || this_player.y != mouseY_))
                {
                    if (button[0])
                    {
                        if (link_gun_analog)
                        {
                            Config.linkGunDirec = link_gun_angle;
                        }
                        else
                        {
                            float tempR;

                            if (Math.Abs(this_player.x - mouseX_) > Math.Abs(this_player.y - mouseY_))
                                tempR = (this_player.x - mouseX_ > 0) ? (float)Opentyr.M_PI_2 : (float)(Opentyr.M_PI + Opentyr.M_PI_2);
                            else
                                tempR = (this_player.y - mouseY_ > 0) ? 0 : (float)Opentyr.M_PI;

                            if (MathF.Abs(Config.linkGunDirec - tempR) < 0.3f)
                                Config.linkGunDirec = tempR;
                            else if (Config.linkGunDirec < tempR && Config.linkGunDirec - tempR > -3.24f)
                                Config.linkGunDirec += 0.2f;
                            else if (Config.linkGunDirec - tempR < (float)Opentyr.M_PI)
                                Config.linkGunDirec -= 0.2f;
                            else
                                Config.linkGunDirec += 0.2f;
                        }

                        if (Config.linkGunDirec >= (2 * (float)Opentyr.M_PI))
                            Config.linkGunDirec -= (2 * (float)Opentyr.M_PI);
                        else if (Config.linkGunDirec < 0)
                            Config.linkGunDirec += (2 * (float)Opentyr.M_PI);
                    }
                    else if (!Config.galagaMode)
                    {
                        Config.twoPlayerLinked = false;
                    }
                }
            }
        }

        if (Varz.levelEnd > 0 && Players.all_players_dead())
            Tyrian2.reallyEndLevel = true;

        /* End Level Fade-Out */
        if (this_player.is_alive && Tyrian2.endLevel)
        {
            if (Varz.levelEnd == 0)
            {
                Tyrian2.reallyEndLevel = true;
            }
            else
            {
                this_player.y -= Varz.levelEndWarp;
                if (this_player.y < -200)
                    Tyrian2.reallyEndLevel = true;

                int trail_spacing = 1;
                int trail_y = this_player.y;
                int num_trails = Math.Abs(41 - Varz.levelEnd);
                if (num_trails > 20)
                    num_trails = 20;

                for (int i = 0; i < num_trails; i++)
                {
                    trail_y += trail_spacing;
                    trail_spacing++;
                }

                for (int i = 1; i < num_trails; i++)
                {
                    trail_y -= trail_spacing;
                    trail_spacing--;

                    if (trail_y > 0 && trail_y < 170)
                    {
                        if (shipGr_ == 0)
                        {
                            Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x - 17, trail_y - 7, shipGrPtr_, 13);
                            Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x + 7, trail_y - 7, shipGrPtr_, 51);
                        }
                        else if (shipGr_ == 1)
                        {
                            Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x - 17, trail_y - 7, shipGrPtr_, 220);
                            Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x + 7, trail_y - 7, shipGrPtr_, 222);
                        }
                        else
                        {
                            Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x - 5, trail_y - 7, shipGrPtr_, shipGr_);
                        }
                    }
                }
            }
        }

        if (Varz.play_demo)
            Fonthand.JE_dString(Video.VGAScreen, 115, 10, Helptext.miscText[7], (uint)Sprites.SMALL_FONT_SHAPES); // insert coin

        if (this_player.is_alive && !Tyrian2.endLevel)
        {
            if (!Config.twoPlayerLinked || playerNum_ < 2)
            {
                if (!Config.twoPlayerMode || Varz.shipGr2 != 0)  // if not dragonwing
                {
                    if (this_player.sidekick[Players.LEFT_SIDEKICK].style == 0)
                    {
                        this_player.sidekick[Players.LEFT_SIDEKICK].x = mouseX_ - 14;
                        this_player.sidekick[Players.LEFT_SIDEKICK].y = mouseY_;
                    }

                    if (this_player.sidekick[Players.RIGHT_SIDEKICK].style == 0)
                    {
                        this_player.sidekick[Players.RIGHT_SIDEKICK].x = mouseX_ + 16;
                        this_player.sidekick[Players.RIGHT_SIDEKICK].y = mouseY_;
                    }
                }

                if (this_player.x_friction_ticks > 0)
                {
                    --this_player.x_friction_ticks;
                }
                else
                {
                    this_player.x_friction_ticks = 1;

                    if (this_player.x_velocity < 0)
                        ++this_player.x_velocity;
                    else if (this_player.x_velocity > 0)
                        --this_player.x_velocity;
                }

                if (this_player.y_friction_ticks > 0)
                {
                    --this_player.y_friction_ticks;
                }
                else
                {
                    this_player.y_friction_ticks = 2;

                    if (this_player.y_velocity < 0)
                        ++this_player.y_velocity;
                    else if (this_player.y_velocity > 0)
                        --this_player.y_velocity;
                }

                this_player.x_velocity += accelXC;
                this_player.y_velocity += accelYC;

                this_player.x_velocity = Math.Min(Math.Max(-4, this_player.x_velocity), 4);
                this_player.y_velocity = Math.Min(Math.Max(-4, this_player.y_velocity), 4);

                this_player.x += this_player.x_velocity;
                this_player.y += this_player.y_velocity;

                // if player moved, add new ship x, y history entry
                if (this_player.x - mouseX_ != 0 || this_player.y - mouseY_ != 0)
                {
                    for (uint i = 1; i < this_player.old_x.Length; ++i)
                    {
                        this_player.old_x[i - 1] = this_player.old_x[i];
                        this_player.old_y[i - 1] = this_player.old_y[i];
                    }
                    this_player.old_x[this_player.old_x.Length - 1] = this_player.x;
                    this_player.old_y[this_player.old_x.Length - 1] = this_player.y;
                }
            }
            else  /*twoPlayerLinked*/
            {
                if (shipGr_ == 0)
                    this_player.x = player[0].x - 1;
                else
                    this_player.x = player[0].x;
                this_player.y = player[0].y + 8;

                this_player.x_velocity = player[0].x_velocity;
                this_player.y_velocity = 4;

                // turret direction marker/shield
                Config.shotMultiPos[Config.SHOT_MISC] = 0;
                Varz.b = Shots.player_shot_create(0, (uint)Config.SHOT_MISC, (ushort)(this_player.x + 1 + MathF.Round(MathF.Sin(Config.linkGunDirec + 0.2f) * 26)), (ushort)(this_player.y + MathF.Round(MathF.Cos(Config.linkGunDirec + 0.2f) * 26)), mouseX_, mouseY_, 148, playerNum_);
                Config.shotMultiPos[Config.SHOT_MISC] = 0;
                Varz.b = Shots.player_shot_create(0, (uint)Config.SHOT_MISC, (ushort)(this_player.x + 1 + MathF.Round(MathF.Sin(Config.linkGunDirec - 0.2f) * 26)), (ushort)(this_player.y + MathF.Round(MathF.Cos(Config.linkGunDirec - 0.2f) * 26)), mouseX_, mouseY_, 148, playerNum_);
                Config.shotMultiPos[Config.SHOT_MISC] = 0;
                Varz.b = Shots.player_shot_create(0, (uint)Config.SHOT_MISC, (ushort)(this_player.x + 1 + MathF.Round(MathF.Sin(Config.linkGunDirec) * 26)), (ushort)(this_player.y + MathF.Round(MathF.Cos(Config.linkGunDirec) * 26)), mouseX_, mouseY_, 147, playerNum_);

                if (Config.shotRepeat[Config.SHOT_REAR] > 0)
                {
                    --Config.shotRepeat[Config.SHOT_REAR];
                }
                else if (button[1 - 1])
                {
                    Config.shotMultiPos[Config.SHOT_REAR] = 0;
                    Varz.b = Shots.player_shot_create(0, (uint)Config.SHOT_REAR, (ushort)(this_player.x + 1 + MathF.Round(MathF.Sin(Config.linkGunDirec) * 20)), (ushort)(this_player.y + MathF.Round(MathF.Cos(Config.linkGunDirec) * 20)), mouseX_, mouseY_, Varz.linkGunWeapons[this_player.items.weapon[Players.REAR_WEAPON].id - 1], playerNum_);
                    Shots.player_shot_set_direction(Varz.b, this_player.items.weapon[Players.REAR_WEAPON].id, Config.linkGunDirec);
                }
            }
        }

        if (!Tyrian2.endLevel)
        {
            if (this_player.x > 256)
            {
                this_player.x = 256;
                Params.constantLastX = (sbyte)(-Params.constantLastX);
            }
            if (this_player.x < 40)
            {
                this_player.x = 40;
                Params.constantLastX = (sbyte)(-Params.constantLastX);
            }

            if (Config.isNetworkGame && playerNum_ == 1)
            {
                if (this_player.y > 154)
                    this_player.y = 154;
            }
            else
            {
                if (this_player.y > 160)
                    this_player.y = 160;
            }

            if (this_player.y < 10)
                this_player.y = 10;

            // Determines the ship banking sprite to display, depending on horizontal velocity and acceleration
            int ship_banking = this_player.x_velocity / 2 + (this_player.x - mouseX_) / 6;
            ship_banking = Math.Max(-2, Math.Min(ship_banking, 2));

            int ship_sprite = ship_banking * 2 + shipGr_;

            Tyrian2.explosionFollowAmountX = this_player.x - this_player.last_x_explosion_follow;
            Tyrian2.explosionFollowAmountY = this_player.y - this_player.last_y_explosion_follow;

            if (Tyrian2.explosionFollowAmountY < 0)
                Tyrian2.explosionFollowAmountY = 0;

            this_player.last_x_explosion_follow = this_player.x;
            this_player.last_y_explosion_follow = this_player.y;

            if (shipGr_ == 0)
            {
                if (Config.background2)
                {
                    Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x - 17 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, (uint)(ship_sprite + 13));
                    Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x + 7 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, (uint)(ship_sprite + 51));
                    if (Config.superWild)
                    {
                        Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x - 16 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, (uint)(ship_sprite + 13));
                        Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x + 6 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, (uint)(ship_sprite + 51));
                    }
                }
            }
            else if (shipGr_ == 1)
            {
                if (Config.background2)
                {
                    Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x - 17 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, 220);
                    Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x + 7 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, 222);
                }
            }
            else
            {
                if (Config.background2)
                {
                    Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x - 5 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, (uint)ship_sprite);
                    if (Config.superWild)
                    {
                        Sprites.blit_sprite2x2_darken(Video.VGAScreen, this_player.x - 4 - Backgrnd.mapX2Ofs + 30, this_player.y - 7 + (int)Varz.shadowYDist, shipGrPtr_, (uint)ship_sprite);
                    }
                }
            }

            if (this_player.invulnerable_ticks > 0)
            {
                --this_player.invulnerable_ticks;

                if (shipGr_ == 0)
                {
                    Sprites.blit_sprite2x2_blend(Video.VGAScreen, this_player.x - 17, this_player.y - 7, shipGrPtr_, (uint)(ship_sprite + 13));
                    Sprites.blit_sprite2x2_blend(Video.VGAScreen, this_player.x + 7, this_player.y - 7, shipGrPtr_, (uint)(ship_sprite + 51));
                }
                else if (shipGr_ == 1)
                {
                    Sprites.blit_sprite2x2_blend(Video.VGAScreen, this_player.x - 17, this_player.y - 7, shipGrPtr_, 220);
                    Sprites.blit_sprite2x2_blend(Video.VGAScreen, this_player.x + 7, this_player.y - 7, shipGrPtr_, 222);
                }
                else
                    Sprites.blit_sprite2x2_blend(Video.VGAScreen, this_player.x - 5, this_player.y - 7, shipGrPtr_, (uint)ship_sprite);
            }
            else
            {
                if (shipGr_ == 0)
                {
                    Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x - 17, this_player.y - 7, shipGrPtr_, (uint)(ship_sprite + 13));
                    Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x + 7, this_player.y - 7, shipGrPtr_, (uint)(ship_sprite + 51));
                }
                else if (shipGr_ == 1)
                {
                    Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x - 17, this_player.y - 7, shipGrPtr_, 220);
                    Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x + 7, this_player.y - 7, shipGrPtr_, 222);

                    int ship_banking_n = 0;
                    switch (ship_sprite)
                    {
                    case 5:
                        Sprites.blit_sprite2(Video.VGAScreen, this_player.x - 17, this_player.y + 7, shipGrPtr_, 40);
                        Varz.tempW = (ushort)(this_player.x - 7);
                        ship_banking_n = -2;
                        break;
                    case 3:
                        Sprites.blit_sprite2(Video.VGAScreen, this_player.x - 17, this_player.y + 7, shipGrPtr_, 39);
                        Varz.tempW = (ushort)(this_player.x - 7);
                        ship_banking_n = -1;
                        break;
                    case 1:
                        ship_banking_n = 0;
                        break;
                    case -1:
                        Sprites.blit_sprite2(Video.VGAScreen, this_player.x + 19, this_player.y + 7, shipGrPtr_, 58);
                        Varz.tempW = (ushort)(this_player.x + 9);
                        ship_banking_n = 1;
                        break;
                    case -3:
                        Sprites.blit_sprite2(Video.VGAScreen, this_player.x + 19, this_player.y + 7, shipGrPtr_, 59);
                        Varz.tempW = (ushort)(this_player.x + 9);
                        ship_banking_n = 2;
                        break;
                    }
                    if (ship_banking_n != 0)  // NortSparks
                    {
                        if (Config.shotRepeat[Config.SHOT_NORTSPARKS] > 0)
                        {
                            --Config.shotRepeat[Config.SHOT_NORTSPARKS];
                        }
                        else
                        {
                            Varz.b = Shots.player_shot_create(0, (uint)Config.SHOT_NORTSPARKS, (ushort)(Varz.tempW + (int)(MtRand.mt_rand() % 8) - 4), (ushort)(this_player.y + (int)(MtRand.mt_rand() % 8) - 4), mouseX_, mouseY_, 671, 1);
                            Config.shotRepeat[Config.SHOT_NORTSPARKS] = (byte)(Math.Abs(ship_banking_n) - 1);
                        }
                    }
                }
                else
                {
                    Sprites.blit_sprite2x2(Video.VGAScreen, this_player.x - 5, this_player.y - 7, shipGrPtr_, (uint)ship_sprite);
                }
            }

            /*Options Location*/
            if (playerNum_ == 2 && shipGr_ == 0)  // if dragonwing
            {
                if (this_player.sidekick[Players.LEFT_SIDEKICK].style == 0)
                {
                    this_player.sidekick[Players.LEFT_SIDEKICK].x = this_player.x - 14 + ship_banking * 2;
                    this_player.sidekick[Players.LEFT_SIDEKICK].y = this_player.y;
                }

                if (this_player.sidekick[Players.RIGHT_SIDEKICK].style == 0)
                {
                    this_player.sidekick[Players.RIGHT_SIDEKICK].x = this_player.x + 17 + ship_banking * 2;
                    this_player.sidekick[Players.RIGHT_SIDEKICK].y = this_player.y;
                }
            }
        }  // !endLevel

        if (moveOk)
        {
            if (this_player.is_alive)
            {
                if (!Tyrian2.endLevel)
                {
                    this_player.delta_x_shot_move = this_player.x - this_player.last_x_shot_move;
                    this_player.delta_y_shot_move = this_player.y - this_player.last_y_shot_move;

                    /* PLAYER SHOT Change */
                    if (button[4 - 1])
                    {
                        Config.portConfigChange = true;
                        if (Config.portConfigDone)
                        {
                            Config.shotMultiPos[Config.SHOT_REAR] = 0;

                            if (Config.superArcadeMode != VarzConst.SA_NONE && Config.superArcadeMode <= VarzConst.SA_NORTSHIPZ)
                            {
                                Config.shotMultiPos[Config.SHOT_SPECIAL] = 0;
                                Config.shotMultiPos[Config.SHOT_SPECIAL2] = 0;
                                if (player[0].items.special == Varz.SASpecialWeapon[Config.superArcadeMode - 1])
                                {
                                    player[0].items.special = (byte)Varz.SASpecialWeaponB[Config.superArcadeMode - 1];
                                    this_player.weapon_mode = 2;
                                }
                                else
                                {
                                    player[0].items.special = (byte)Varz.SASpecialWeapon[Config.superArcadeMode - 1];
                                    this_player.weapon_mode = 1;
                                }
                            }
                            else if (++this_player.weapon_mode > Varz.JE_portConfigs())
                                this_player.weapon_mode = 1;

                            JE_drawPortConfigButtons();
                            Config.portConfigDone = false;
                        }
                    }

                    /* PLAYER SHOT Creation */

                    /*SpecialShot*/
                    if (!Config.galagaMode)
                        Varz.JE_doSpecialShot(playerNum_, ref this_player.armor, ref this_player.shield);

                    /*Normal Main Weapons*/
                    if (!(Config.twoPlayerLinked && playerNum_ == 2))
                    {
                        int min, max;

                        if (!Config.twoPlayerMode)
                        { min = 1; max = 2; }
                        else
                            min = max = playerNum_;

                        for (Varz.temp = (byte)(min - 1); Varz.temp < max; Varz.temp++)
                        {
                            uint item = this_player.items.weapon[Varz.temp].id;

                            if (item > 0)
                            {
                                if (Config.shotRepeat[Varz.temp] > 0)
                                {
                                    --Config.shotRepeat[Varz.temp];
                                }
                                else if (button[1 - 1])
                                {
                                    uint item_power = Config.galagaMode ? 0 : (uint)(this_player.items.weapon[Varz.temp].power - 1);
                                    uint item_mode = (Varz.temp == Players.REAR_WEAPON) ? this_player.weapon_mode - 1 : 0;

                                    Varz.b = Shots.player_shot_create((ushort)item, Varz.temp, (ushort)this_player.x, (ushort)this_player.y, mouseX_, mouseY_, Episodes.weaponPort[(int)item].op[(int)(item_mode * 11 + item_power)], playerNum_);
                                }
                            }
                        }
                    }

                    /*Super Charge Weapons*/
                    if (playerNum_ == 2)
                    {

                        if (!Config.twoPlayerLinked)
                            Sprites.blit_sprite2(Video.VGAScreen, this_player.x + (shipGr_ == 0 ? 1 : 0) + 1, this_player.y - 13, Sprites.spriteSheet10, (uint)(77 + Varz.chargeLevel + Varz.chargeGr * 19));

                        if (Varz.chargeGrWait > 0)
                        {
                            Varz.chargeGrWait--;
                        }
                        else
                        {
                            Varz.chargeGr++;
                            if (Varz.chargeGr == 4)
                                Varz.chargeGr = 0;
                            Varz.chargeGrWait = 3;
                        }

                        if (Varz.chargeLevel > 0)
                        {
                            Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 269, 107 + (Varz.chargeLevel - 1) * 3, 275, 108 + (Varz.chargeLevel - 1) * 3, 193);
                        }

                        if (Varz.chargeWait > 0)
                        {
                            Varz.chargeWait--;
                        }
                        else
                        {
                            if (Varz.chargeLevel < Varz.chargeMax)
                                Varz.chargeLevel++;

                            Varz.chargeWait = (byte)(28 - this_player.items.weapon[Players.REAR_WEAPON].power * 2);
                            if (Config.difficultyLevel > Config.DIFFICULTY_HARD)
                                Varz.chargeWait -= 5;
                        }

                        if (Varz.chargeLevel > 0)
                            Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 269, 107 + (Varz.chargeLevel - 1) * 3, 275, 108 + (Varz.chargeLevel - 1) * 3, 204);

                        if (Config.shotRepeat[Config.SHOT_P2_CHARGE] > 0)
                        {
                            --Config.shotRepeat[Config.SHOT_P2_CHARGE];
                        }
                        else if (button[1 - 1] && (!Config.twoPlayerLinked || Varz.chargeLevel > 0))
                        {
                            Config.shotMultiPos[Config.SHOT_P2_CHARGE] = 0;
                            Varz.b = Shots.player_shot_create(16, (uint)Config.SHOT_P2_CHARGE, (ushort)this_player.x, (ushort)this_player.y, mouseX_, mouseY_, (ushort)(Varz.chargeGunWeapons[player[1].items.weapon[Players.REAR_WEAPON].id - 1] + Varz.chargeLevel), playerNum_);

                            if (Varz.chargeLevel > 0)
                                Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 269, 107 + (Varz.chargeLevel - 1) * 3, 275, 108 + (Varz.chargeLevel - 1) * 3, 193);

                            Varz.chargeLevel = 0;
                            Varz.chargeWait = (byte)(30 - this_player.items.weapon[Players.REAR_WEAPON].power * 2);
                        }
                    }

                    /*SUPER BOMB*/
                    Varz.temp = playerNum_;
                    if (Varz.temp == 0)
                        Varz.temp = 1;  /*Get whether player 1 or 2*/

                    if (player[Varz.temp - 1].superbombs > 0)
                    {
                        if (Config.shotRepeat[Config.SHOT_P1_SUPERBOMB + Varz.temp - 1] > 0)
                        {
                            --Config.shotRepeat[Config.SHOT_P1_SUPERBOMB + Varz.temp - 1];
                        }
                        else if (button[3 - 1] || button[2 - 1])
                        {
                            --player[Varz.temp - 1].superbombs;
                            Config.shotMultiPos[Config.SHOT_P1_SUPERBOMB + Varz.temp - 1] = 0;
                            Varz.b = Shots.player_shot_create(16, (uint)(Config.SHOT_P1_SUPERBOMB + Varz.temp - 1), (ushort)this_player.x, (ushort)this_player.y, mouseX_, mouseY_, 535, playerNum_);
                        }
                    }

                    // sidekicks

                    if (this_player.sidekick[Players.LEFT_SIDEKICK].style == 4 && this_player.sidekick[Players.RIGHT_SIDEKICK].style == 4)
                        Varz.optionSatelliteRotate += 0.2f;
                    else if (this_player.sidekick[Players.LEFT_SIDEKICK].style == 4 || this_player.sidekick[Players.RIGHT_SIDEKICK].style == 4)
                        Varz.optionSatelliteRotate += 0.15f;

                    switch (this_player.sidekick[Players.LEFT_SIDEKICK].style)
                    {
                    case 1:  // trailing
                    case 3:
                        this_player.sidekick[Players.LEFT_SIDEKICK].x = this_player.old_x[this_player.old_x.Length / 2 - 1];
                        this_player.sidekick[Players.LEFT_SIDEKICK].y = this_player.old_y[this_player.old_x.Length / 2 - 1];
                        break;
                    case 2:  // front-mounted
                        this_player.sidekick[Players.LEFT_SIDEKICK].x = this_player.x;
                        this_player.sidekick[Players.LEFT_SIDEKICK].y = Math.Max(10, this_player.y - 20);
                        break;
                    case 4:  // orbiting
                        this_player.sidekick[Players.LEFT_SIDEKICK].x = this_player.x + (int)MathF.Round(MathF.Sin(Varz.optionSatelliteRotate) * 20);
                        this_player.sidekick[Players.LEFT_SIDEKICK].y = this_player.y + (int)MathF.Round(MathF.Cos(Varz.optionSatelliteRotate) * 20);
                        break;
                    }

                    switch (this_player.sidekick[Players.RIGHT_SIDEKICK].style)
                    {
                    case 4:  // orbiting
                        this_player.sidekick[Players.RIGHT_SIDEKICK].x = this_player.x - (int)MathF.Round(MathF.Sin(Varz.optionSatelliteRotate) * 20);
                        this_player.sidekick[Players.RIGHT_SIDEKICK].y = this_player.y - (int)MathF.Round(MathF.Cos(Varz.optionSatelliteRotate) * 20);
                        break;
                    case 1:  // trailing
                    case 3:
                        this_player.sidekick[Players.RIGHT_SIDEKICK].x = this_player.old_x[0];
                        this_player.sidekick[Players.RIGHT_SIDEKICK].y = this_player.old_y[0];
                        break;
                    case 2:  // front-mounted
                        if (!Varz.optionAttachmentLinked)
                        {
                            this_player.sidekick[Players.RIGHT_SIDEKICK].y += Varz.optionAttachmentMove / 2;
                            if (Varz.optionAttachmentMove >= -2)
                            {
                                if (Varz.optionAttachmentReturn)
                                    Varz.temp = 2;
                                else
                                    Varz.temp = 0;

                                if (this_player.sidekick[Players.RIGHT_SIDEKICK].y > (this_player.y - 20) + 5)
                                {
                                    Varz.temp = 2;
                                    Varz.optionAttachmentMove -= (short)(1 + (Varz.optionAttachmentReturn ? 1 : 0));
                                }
                                else if (this_player.sidekick[Players.RIGHT_SIDEKICK].y > (this_player.y - 20) - 0)
                                {
                                    Varz.temp = 3;
                                    if (Varz.optionAttachmentMove > 0)
                                        Varz.optionAttachmentMove--;
                                    else
                                        Varz.optionAttachmentMove++;
                                }
                                else if (this_player.sidekick[Players.RIGHT_SIDEKICK].y > (this_player.y - 20) - 5)
                                {
                                    Varz.temp = 2;
                                    Varz.optionAttachmentMove++;
                                }
                                else if (Varz.optionAttachmentMove < 2 + (Varz.optionAttachmentReturn ? 1 : 0) * 4)
                                {
                                    Varz.optionAttachmentMove += (short)(1 + (Varz.optionAttachmentReturn ? 1 : 0));
                                }

                                if (Varz.optionAttachmentReturn)
                                    Varz.temp = (byte)(Varz.temp * 2);
                                if (Math.Abs(this_player.sidekick[Players.RIGHT_SIDEKICK].x - this_player.x) < Varz.temp)
                                    Varz.temp = 1;

                                if (this_player.sidekick[Players.RIGHT_SIDEKICK].x > this_player.x)
                                    this_player.sidekick[Players.RIGHT_SIDEKICK].x -= Varz.temp;
                                else if (this_player.sidekick[Players.RIGHT_SIDEKICK].x < this_player.x)
                                    this_player.sidekick[Players.RIGHT_SIDEKICK].x += Varz.temp;

                                if (Math.Abs(this_player.sidekick[Players.RIGHT_SIDEKICK].y - (this_player.y - 20)) + Math.Abs(this_player.sidekick[Players.RIGHT_SIDEKICK].x - this_player.x) < 8)
                                {
                                    Varz.optionAttachmentLinked = true;
                                    Varz.soundQueue[2] = (byte)Sndmast.S_CLINK;
                                }

                                if (button[3 - 1])
                                    Varz.optionAttachmentReturn = true;
                            }
                            else  // sidekick needs to catch up to player
                            {
                                Varz.optionAttachmentMove += (short)(1 + (Varz.optionAttachmentReturn ? 1 : 0));
                                Varz.JE_setupExplosion(this_player.sidekick[Players.RIGHT_SIDEKICK].x + 1, this_player.sidekick[Players.RIGHT_SIDEKICK].y + 10, 0, 0, false, false);
                            }
                        }
                        else
                        {
                            this_player.sidekick[Players.RIGHT_SIDEKICK].x = this_player.x;
                            this_player.sidekick[Players.RIGHT_SIDEKICK].y = this_player.y - 20;
                            if (button[3 - 1])
                            {
                                Varz.optionAttachmentLinked = false;
                                Varz.optionAttachmentReturn = false;
                                Varz.optionAttachmentMove = -20;
                                Varz.soundQueue[3] = (byte)Sndmast.S_WEAPON_26;
                            }
                        }

                        if (this_player.sidekick[Players.RIGHT_SIDEKICK].y < 10)
                            this_player.sidekick[Players.RIGHT_SIDEKICK].y = 10;
                        break;
                    }

                    if (playerNum_ == 2 || !Config.twoPlayerMode)  // if player has sidekicks
                    {
                        for (uint i = 0; i < 2; ++i)
                        {
                            uint shot_i = (i == 0) ? (uint)Config.SHOT_LEFT_SIDEKICK : (uint)Config.SHOT_RIGHT_SIDEKICK;

                            ref JE_OptionType this_option = ref Episodes.options[this_player.items.sidekick[(int)i]];

                            // fire/refill sidekick
                            if (this_option.wport > 0)
                            {
                                if (Config.shotRepeat[shot_i] > 0)
                                {
                                    --Config.shotRepeat[shot_i];
                                }
                                else
                                {
                                    int ammo_max = this_player.sidekick[(int)i].ammo_max;

                                    if (ammo_max > 0)  // sidekick has limited ammo
                                    {
                                        if (this_player.sidekick[(int)i].ammo_refill_ticks > 0)
                                        {
                                            --this_player.sidekick[(int)i].ammo_refill_ticks;
                                        }
                                        else  // refill one ammo
                                        {
                                            this_player.sidekick[(int)i].ammo_refill_ticks = this_player.sidekick[(int)i].ammo_refill_ticks_max;

                                            if (this_player.sidekick[(int)i].ammo < ammo_max)
                                                ++this_player.sidekick[(int)i].ammo;

                                            // draw sidekick refill ammo gauge
                                            int y = Varz.hud_sidekick_y[Config.twoPlayerMode ? 1 : 0, (int)i] + 13;
                                            Vga256d.draw_segmented_gauge(Video.VGAScreenSeg, 284, y, 112, 2, 2, (uint)Math.Max(1, ammo_max / 10), (uint)this_player.sidekick[(int)i].ammo);
                                        }

                                        if (button[1 + i] && this_player.sidekick[(int)i].ammo > 0)
                                        {
                                            Varz.b = Shots.player_shot_create(this_option.wport, shot_i, (ushort)this_player.sidekick[(int)i].x, (ushort)this_player.sidekick[(int)i].y, mouseX_, mouseY_, (ushort)(this_option.wpnum + this_player.sidekick[(int)i].charge), playerNum_);

                                            --this_player.sidekick[(int)i].ammo;
                                            if (this_player.sidekick[(int)i].charge > 0)
                                            {
                                                Config.shotMultiPos[shot_i] = 0;
                                                this_player.sidekick[(int)i].charge = 0;
                                            }
                                            this_player.sidekick[(int)i].charge_ticks = 20;
                                            this_player.sidekick[(int)i].animation_enabled = true;

                                            // draw sidekick discharge ammo gauge
                                            int y = Varz.hud_sidekick_y[Config.twoPlayerMode ? 1 : 0, (int)i] + 13;
                                            Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 284, y, 312, y + 2, 0);
                                            Vga256d.draw_segmented_gauge(Video.VGAScreenSeg, 284, y, 112, 2, 2, (uint)Math.Max(1, ammo_max / 10), (uint)this_player.sidekick[(int)i].ammo);
                                        }
                                    }
                                    else  // has infinite ammo
                                    {
                                        if (button[0] || button[1 + i])
                                        {
                                            Varz.b = Shots.player_shot_create(this_option.wport, shot_i, (ushort)this_player.sidekick[(int)i].x, (ushort)this_player.sidekick[(int)i].y, mouseX_, mouseY_, (ushort)(this_option.wpnum + this_player.sidekick[(int)i].charge), playerNum_);

                                            if (this_player.sidekick[(int)i].charge > 0)
                                            {
                                                Config.shotMultiPos[shot_i] = 0;
                                                this_player.sidekick[(int)i].charge = 0;
                                            }
                                            this_player.sidekick[(int)i].charge_ticks = 20;
                                            this_player.sidekick[(int)i].animation_enabled = true;
                                        }
                                    }
                                }
                            }
                        }
                    }  // end of if player has sidekicks
                }  // !endLevel
            } // this_player->is_alive
        } // moveOK

        // draw sidekicks
        if ((playerNum_ == 2 || !Config.twoPlayerMode) && !Tyrian2.endLevel)
        {
            for (uint i = 0; i < 2; ++i)
            {
                ref JE_OptionType this_option = ref Episodes.options[this_player.items.sidekick[(int)i]];

                if (this_option.option > 0)
                {
                    if (this_player.sidekick[(int)i].animation_enabled)
                    {
                        if (++this_player.sidekick[(int)i].animation_frame >= this_option.ani)
                        {
                            this_player.sidekick[(int)i].animation_frame = 0;
                            this_player.sidekick[(int)i].animation_enabled = (this_option.option == 1);
                        }
                    }

                    int x = this_player.sidekick[(int)i].x,
                        y = this_player.sidekick[(int)i].y;
                    uint sprite = (uint)(this_option.gr[this_player.sidekick[(int)i].animation_frame] + this_player.sidekick[(int)i].charge);

                    if (this_player.sidekick[(int)i].style == 1 || this_player.sidekick[(int)i].style == 2)
                        Sprites.blit_sprite2x2(Video.VGAScreen, x - 6, y, Sprites.spriteSheet10, sprite);
                    else
                        Sprites.blit_sprite2(Video.VGAScreen, x, y, Sprites.spriteSheet9, sprite);
                }

                if (--this_player.sidekick[(int)i].charge_ticks == 0)
                {
                    if (this_player.sidekick[(int)i].charge < this_option.pwr)
                        ++this_player.sidekick[(int)i].charge;
                    this_player.sidekick[(int)i].charge_ticks = 20;
                }
            }
        }
    }

    public static void JE_mainGamePlayerFunctions()
    {
        var player = Players.player;

        /*PLAYER MOVEMENT/MOUSE ROUTINES*/

        if (Tyrian2.endLevel && Varz.levelEnd > 0)
        {
            Varz.levelEnd--;
            Varz.levelEndWarp++;
        }

        /*Reset Street-Fighter commands*/
        Array.Clear(Varz.SFExecuted);

        Config.portConfigChange = false;

        if (Config.twoPlayerMode)
        {
            JE_playerMovement(player[0],
                              !Config.galagaMode ? Config.inputDevice[0] : (byte)0, 1, Varz.shipGr, Varz.shipGrPtr,
                              ref player[0].mouseX, ref player[0].mouseY);
            JE_playerMovement(player[1],
                              !Config.galagaMode ? Config.inputDevice[1] : (byte)0, 2, Varz.shipGr2, Varz.shipGr2ptr,
                              ref player[1].mouseX, ref player[1].mouseY);
        }
        else
        {
            JE_playerMovement(player[0],
                              0, 1, Varz.shipGr, Varz.shipGrPtr,
                              ref player[0].mouseX, ref player[0].mouseY);
        }

        /* == Parallax Map Scrolling == */
        ushort tempX;
        if (Config.twoPlayerMode)
            tempX = (ushort)((player[0].x + player[1].x) / 2);
        else
            tempX = (ushort)player[0].x;

        Varz.tempW = (ushort)MathF.Floor((float)(260 - (tempX - 36)) / (260 - 36) * (24 * 3) - 1);
        Backgrnd.mapX3Ofs = Varz.tempW;
        Backgrnd.mapX3Pos = (ushort)(Backgrnd.mapX3Ofs % 24);
        Backgrnd.mapX3bpPos = 1 - (Backgrnd.mapX3Ofs / 24);

        Backgrnd.mapX2Ofs = (ushort)((Varz.tempW * 2) / 3);
        Backgrnd.mapX2Pos = (ushort)(Backgrnd.mapX2Ofs % 24);
        Backgrnd.mapX2bpPos = 1 - (Backgrnd.mapX2Ofs / 24);

        Backgrnd.oldMapXOfs = Backgrnd.mapXOfs;
        Backgrnd.mapXOfs = (ushort)(Backgrnd.mapX2Ofs / 2);
        Backgrnd.mapXPos = (ushort)(Backgrnd.mapXOfs % 24);
        Backgrnd.mapXbpPos = 1 - (Backgrnd.mapXOfs / 24);

        if (Tyrian2.background3x1)
        {
            Backgrnd.mapX3Ofs = Backgrnd.mapXOfs;
            Backgrnd.mapX3Pos = Backgrnd.mapXPos;
            Backgrnd.mapX3bpPos = Backgrnd.mapXbpPos - 1;
        }
    }

    public static void JE_playerCollide(Player this_player, byte playerNum_)
    {
        var player = Players.player;
        string tempStr;

        for (int z = 0; z < 100; z++)
        {
            if (Varz.enemyAvail[z] != 1)
            {
                int enemy_screen_x = Varz.enemy[z].ex + Varz.enemy[z].mapoffset;

                if (Math.Abs(this_player.x - enemy_screen_x) < 12 && Math.Abs(this_player.y - Varz.enemy[z].ey) < 14)
                {   /*Collide*/
                    int evalue = Varz.enemy[z].evalue;
                    if (evalue > 29999)
                    {
                        if (evalue == 30000)  // spawn dragonwing in galaga mode, otherwise just a purple ball
                        {
                            this_player.cash += 100;

                            if (!Config.galagaMode)
                            {
                                Players.handle_got_purple_ball(this_player);
                            }
                            else
                            {
                                // spawn the dragonwing?
                                if (Config.twoPlayerMode)
                                    this_player.cash += 2400;
                                Config.twoPlayerMode = true;
                                Config.twoPlayerLinked = true;
                                player[1].items.weapon[Players.REAR_WEAPON].power = 1;
                                player[1].armor = 10;
                                player[1].is_alive = true;
                            }
                            Varz.enemyAvail[z] = 1;
                            Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                        }
                        else if (Config.superArcadeMode != VarzConst.SA_NONE && evalue > 30000)
                        {
                            Config.shotMultiPos[Config.SHOT_FRONT] = 0;
                            Config.shotRepeat[Config.SHOT_FRONT] = 10;

                            Varz.tempW = Varz.SAWeapon[Config.superArcadeMode - 1, evalue - 30000 - 1];

                            // if picked up already-owned weapon, power weapon up
                            if (Varz.tempW == player[0].items.weapon[Players.FRONT_WEAPON].id)
                            {
                                this_player.cash += 1000;
                                Players.power_up_weapon(this_player, Players.FRONT_WEAPON);
                            }
                            // else weapon also gives purple ball
                            else
                            {
                                Players.handle_got_purple_ball(this_player);
                            }

                            player[0].items.weapon[Players.FRONT_WEAPON].id = (byte)Varz.tempW;
                            this_player.cash += 200;
                            Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                            Varz.enemyAvail[z] = 1;
                        }
                        else if (evalue > 32100)
                        {
                            if (playerNum_ == 1)
                            {
                                this_player.cash += 250;
                                player[0].items.special = (byte)(evalue - 32100);
                                Config.shotMultiPos[Config.SHOT_SPECIAL] = 0;
                                Config.shotRepeat[Config.SHOT_SPECIAL] = 10;
                                Config.shotMultiPos[Config.SHOT_SPECIAL2] = 0;
                                Config.shotRepeat[Config.SHOT_SPECIAL2] = 0;

                                string specialName; fixed (byte* p = Episodes.special[evalue - 32100].name) specialName = NameStr(p);
                                if (Config.isNetworkGame)
                                    tempStr = $"{JE_getName(1)} {Helptext.miscTextB[4 - 1]} {specialName}";
                                else if (Config.twoPlayerMode)
                                    tempStr = $"{Helptext.miscText[43 - 1]} {specialName}";
                                else
                                    tempStr = $"{Helptext.miscText[64 - 1]} {specialName}";
                                JE_drawTextWindow(tempStr);
                                Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                                Varz.enemyAvail[z] = 1;
                            }
                        }
                        else if (evalue > 32000)
                        {
                            if (playerNum_ == 2)
                            {
                                Varz.enemyAvail[z] = 1;
                                string optName; fixed (byte* p = Episodes.options[evalue - 32000].name) optName = NameStr(p);
                                if (Config.isNetworkGame)
                                    tempStr = $"{JE_getName(2)} {Helptext.miscTextB[4 - 1]} {optName}";
                                else
                                    tempStr = $"{Helptext.miscText[44 - 1]} {optName}";
                                JE_drawTextWindow(tempStr);

                                // if picked up a different sidekick than player already has, then reset sidekicks to least powerful, else power them up
                                if ((uint)(evalue - 32000) != player[1].items.sidekick_series)
                                {
                                    player[1].items.sidekick_series = (byte)(evalue - 32000);
                                    player[1].items.sidekick_level = 101;
                                }
                                else if (player[1].items.sidekick_level < 103)
                                {
                                    ++player[1].items.sidekick_level;
                                }

                                uint temp = (uint)(player[1].items.sidekick_level - 100 - 1);
                                for (uint i = 0; i < 2; ++i)
                                    player[1].items.sidekick[(int)i] = Varz.optionSelect[player[1].items.sidekick_series, (int)temp, (int)i];

                                Config.shotMultiPos[Config.SHOT_LEFT_SIDEKICK] = 0;
                                Config.shotMultiPos[Config.SHOT_RIGHT_SIDEKICK] = 0;
                                Varz.JE_drawOptions();
                                Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                            }
                            else if (Config.onePlayerAction)
                            {
                                Varz.enemyAvail[z] = 1;
                                string optName; fixed (byte* p = Episodes.options[evalue - 32000].name) optName = NameStr(p);
                                tempStr = $"{Helptext.miscText[64 - 1]} {optName}";
                                JE_drawTextWindow(tempStr);

                                for (uint i = 0; i < 2; ++i)
                                    player[0].items.sidekick[(int)i] = (byte)(evalue - 32000);
                                Config.shotMultiPos[Config.SHOT_LEFT_SIDEKICK] = 0;
                                Config.shotMultiPos[Config.SHOT_RIGHT_SIDEKICK] = 0;

                                Varz.JE_drawOptions();
                                Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                            }
                            if (Varz.enemyAvail[z] == 1)
                                this_player.cash += 250;
                        }
                        else if (evalue > 31000)
                        {
                            this_player.cash += 250;
                            if (playerNum_ == 2)
                            {
                                string wpName; fixed (byte* p = Episodes.weaponPort[evalue - 31000].name) wpName = NameStr(p);
                                if (Config.isNetworkGame)
                                    tempStr = $"{JE_getName(2)} {Helptext.miscTextB[4 - 1]} {wpName}";
                                else
                                    tempStr = $"{Helptext.miscText[44 - 1]} {wpName}";
                                JE_drawTextWindow(tempStr);
                                player[1].items.weapon[Players.REAR_WEAPON].id = (byte)(evalue - 31000);
                                Config.shotMultiPos[Config.SHOT_REAR] = 0;
                                Varz.enemyAvail[z] = 1;
                                Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                            }
                            else if (Config.onePlayerAction)
                            {
                                string wpName; fixed (byte* p = Episodes.weaponPort[evalue - 31000].name) wpName = NameStr(p);
                                tempStr = $"{Helptext.miscText[64 - 1]} {wpName}";
                                JE_drawTextWindow(tempStr);
                                player[0].items.weapon[Players.REAR_WEAPON].id = (byte)(evalue - 31000);
                                Config.shotMultiPos[Config.SHOT_REAR] = 0;
                                Varz.enemyAvail[z] = 1;
                                Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;

                                if (player[0].items.weapon[Players.REAR_WEAPON].power == 0)  // does this ever happen?
                                    player[0].items.weapon[Players.REAR_WEAPON].power = 1;
                            }
                        }
                        else if (evalue > 30000)
                        {
                            if (playerNum_ == 1 && Config.twoPlayerMode)
                            {
                                string wpName; fixed (byte* p = Episodes.weaponPort[evalue - 30000].name) wpName = NameStr(p);
                                if (Config.isNetworkGame)
                                    tempStr = $"{JE_getName(1)} {Helptext.miscTextB[4 - 1]} {wpName}";
                                else
                                    tempStr = $"{Helptext.miscText[43 - 1]} {wpName}";
                                JE_drawTextWindow(tempStr);
                                player[0].items.weapon[Players.FRONT_WEAPON].id = (byte)(evalue - 30000);
                                Config.shotMultiPos[Config.SHOT_FRONT] = 0;
                                Varz.enemyAvail[z] = 1;
                                Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                            }
                            else if (Config.onePlayerAction)
                            {
                                string wpName; fixed (byte* p = Episodes.weaponPort[evalue - 30000].name) wpName = NameStr(p);
                                tempStr = $"{Helptext.miscText[64 - 1]} {wpName}";
                                JE_drawTextWindow(tempStr);
                                player[0].items.weapon[Players.FRONT_WEAPON].id = (byte)(evalue - 30000);
                                Config.shotMultiPos[Config.SHOT_FRONT] = 0;
                                Varz.enemyAvail[z] = 1;
                                Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                            }

                            if (Varz.enemyAvail[z] == 1)
                            {
                                player[0].items.special = Varz.specialArcadeWeapon[evalue - 30000 - 1];
                                if (player[0].items.special > 0)
                                {
                                    Config.shotMultiPos[Config.SHOT_SPECIAL] = 0;
                                    Config.shotRepeat[Config.SHOT_SPECIAL] = 0;
                                    Config.shotMultiPos[Config.SHOT_SPECIAL2] = 0;
                                    Config.shotRepeat[Config.SHOT_SPECIAL2] = 0;
                                }
                                this_player.cash += 250;
                            }

                        }
                    }
                    else if (evalue > 20000)
                    {
                        if (Config.twoPlayerLinked)
                        {
                            // share the armor evenly between linked players
                            for (uint i = 0; i < 2; ++i)
                            {
                                player[i].armor += (uint)((evalue - 20000) / 2);
                                if (player[i].armor > 28)
                                    player[i].armor = 28;
                            }
                        }
                        else
                        {
                            this_player.armor += (uint)(evalue - 20000);
                            if (this_player.armor > 28)
                                this_player.armor = 28;
                        }
                        Varz.enemyAvail[z] = 1;
                        Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */
                        Varz.JE_drawArmor();
                        Video.VGAScreen = Video.game_screen; /* side-effect of game_screen */
                        Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                    }
                    else if (evalue > 10000 && Varz.enemyAvail[z] == 2)
                    {
                        if (!Episodes.bonusLevel)
                        {
                            Loudness.play_song(30);  /*Zanac*/
                            Episodes.bonusLevel = true;
                            Config.nextLevel = (byte)(evalue - 10000);
                            Varz.enemyAvail[z] = 1;
                            Varz.displayTime = 150;
                        }
                    }
                    else if (Varz.enemy[z].scoreitem)
                    {
                        Varz.enemyAvail[z] = 1;
                        Varz.soundQueue[7] = (byte)Sndmast.S_ITEM;
                        if (evalue == 1)
                        {
                            Config.cubeMax++;
                            Varz.soundQueue[3] = (byte)Sndmast.V_DATA_CUBE;
                        }
                        else if (evalue == -1)  // got front weapon powerup
                        {
                            if (Config.isNetworkGame)
                                tempStr = $"{JE_getName(1)} {Helptext.miscTextB[4 - 1]} {Helptext.miscText[45 - 1]}";
                            else if (Config.twoPlayerMode)
                                tempStr = $"{Helptext.miscText[43 - 1]} {Helptext.miscText[45 - 1]}";
                            else
                                tempStr = Helptext.miscText[45 - 1];
                            JE_drawTextWindow(tempStr);

                            Players.power_up_weapon(player[0], Players.FRONT_WEAPON);
                            Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                        }
                        else if (evalue == -2)  // got rear weapon powerup
                        {
                            if (Config.isNetworkGame)
                                tempStr = $"{JE_getName(2)} {Helptext.miscTextB[4 - 1]} {Helptext.miscText[46 - 1]}";
                            else if (Config.twoPlayerMode)
                                tempStr = $"{Helptext.miscText[44 - 1]} {Helptext.miscText[46 - 1]}";
                            else
                                tempStr = Helptext.miscText[46 - 1];
                            JE_drawTextWindow(tempStr);

                            Players.power_up_weapon(Config.twoPlayerMode ? player[1] : player[0], Players.REAR_WEAPON);
                            Varz.soundQueue[7] = (byte)Sndmast.S_POWERUP;
                        }
                        else if (evalue == -3)
                        {
                            // picked up orbiting asteroid killer
                            Config.shotMultiPos[Config.SHOT_MISC] = 0;
                            Varz.b = Shots.player_shot_create(0, (uint)Config.SHOT_MISC, (ushort)this_player.x, (ushort)this_player.y, player[0].mouseX, player[0].mouseY, 104, playerNum_);
                            Shots.shotAvail[z] = 0; // NOTE: 原版 C 以 z(敵人索引,0..99) 索引 shotAvail[MAX_PWEAPON=81]，為原始碼既有越界寫入；此處忠實保留
                        }
                        else if (evalue == -4)
                        {
                            if (player[playerNum_ - 1].superbombs < 10)
                                ++player[playerNum_ - 1].superbombs;
                        }
                        else if (evalue == -5)
                        {
                            player[0].items.weapon[Players.FRONT_WEAPON].id = 25;  // HOT DOG!
                            player[0].items.weapon[Players.REAR_WEAPON].id = 26;
                            player[1].items.weapon[Players.REAR_WEAPON].id = 26;

                            player[0].last_items = player[0].items;

                            for (uint i = 0; i < 2; ++i)
                                player[i].weapon_mode = 1;

                            Array.Clear(Config.shotMultiPos);
                        }
                        else if (Config.twoPlayerLinked)
                        {
                            // players get equal share of pick-up cash when linked
                            for (uint i = 0; i < 2; ++i)
                                player[i].cash += (uint)(evalue / 2);
                        }
                        else
                        {
                            this_player.cash += (uint)evalue;
                        }
                        Varz.JE_setupExplosion(enemy_screen_x, Varz.enemy[z].ey, 0, Episodes.enemyDat[Varz.enemy[z].enemytype].explosiontype, true, false);
                    }
                    else if (this_player.invulnerable_ticks == 0 && Varz.enemyAvail[z] == 0 &&
                             (Episodes.enemyDat[Varz.enemy[z].enemytype].explosiontype & 1) == 0) // explosiontype & 1 == 0: not ground enemy
                    {
                        int armorleft = Varz.enemy[z].armorleft;
                        if (armorleft > Tyrian2.damageRate)
                            armorleft = Tyrian2.damageRate;

                        Varz.JE_playerDamage((byte)armorleft, playerNum_ - 1);

                        // player ship gets push-back from collision
                        if (Varz.enemy[z].armorleft > 0)
                        {
                            this_player.x_velocity += (Varz.enemy[z].exc * Varz.enemy[z].armorleft) / 2;
                            this_player.y_velocity += (Varz.enemy[z].eyc * Varz.enemy[z].armorleft) / 2;
                        }

                        int armorleft2 = Varz.enemy[z].armorleft;
                        if (armorleft2 == 255)
                            armorleft2 = 30000;

                        Varz.temp = Varz.enemy[z].linknum;
                        if (Varz.temp == 0)
                            Varz.temp = 255;

                        Varz.b = z;

                        if (armorleft2 > armorleft)
                        {
                            // damage enemy
                            if (Varz.enemy[z].armorleft != 255)
                                Varz.enemy[z].armorleft -= (byte)armorleft;
                            Varz.soundQueue[5] = (byte)Sndmast.S_ENEMY_HIT;
                        }
                        else
                        {
                            // kill enemy
                            for (Varz.temp2 = 0; Varz.temp2 < 100; Varz.temp2++)
                            {
                                if (Varz.enemyAvail[Varz.temp2] != 1)
                                {
                                    Varz.temp3 = Varz.enemy[Varz.temp2].linknum;
                                    if (Varz.temp2 == Varz.b ||
                                        (Varz.temp != 255 &&
                                         (Varz.temp == Varz.temp3 || Varz.temp - 100 == Varz.temp3 ||
                                          (Varz.temp3 > 40 && Varz.temp3 / 20 == Varz.temp / 20 && Varz.temp3 <= Varz.temp))))
                                    {
                                        int enemy_screen_x2 = Varz.enemy[Varz.temp2].ex + Varz.enemy[Varz.temp2].mapoffset;

                                        Varz.enemy[Varz.temp2].linknum = 0;

                                        Varz.enemyAvail[Varz.temp2] = 1;

                                        if (Episodes.enemyDat[Varz.enemy[Varz.temp2].enemytype].esize == 1)
                                        {
                                            Varz.JE_setupExplosionLarge(Varz.enemy[Varz.temp2].enemyground, Varz.enemy[Varz.temp2].explonum, enemy_screen_x2, Varz.enemy[Varz.temp2].ey);
                                            Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_9;
                                        }
                                        else
                                        {
                                            Varz.JE_setupExplosion(enemy_screen_x2, Varz.enemy[Varz.temp2].ey, 0, 1, false, false);
                                            Varz.soundQueue[5] = (byte)Sndmast.S_EXPLOSION_4;
                                        }
                                    }
                                }
                            }
                            Varz.enemyAvail[z] = 1;
                        }
                    }
                }

            }
        }
    }
}
