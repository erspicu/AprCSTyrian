namespace AprCSTyrian.Core;

/// <summary>對應 shots.h:PlayerShotDataType。</summary>
internal struct PlayerShotDataType
{
    public short shotX, shotY, shotXM, shotYM, shotXC, shotYC;
    public bool shotComplicated;
    public short shotDevX, shotDirX, shotDevY, shotDirY, shotCirSizeX, shotCirSizeY;
    public byte shotTrail;
    public ushort shotGr, shotAni, shotAniMax;
    public byte shotDmg;
    public byte shotBlastFilter, chainReaction, playerNumber, aimAtEnemy, aimDelay, aimDelayMax;
}

/// <summary>
/// 移植 sources/src/shots.c —— 玩家射擊建立/移動/繪製/方向。
/// C 的輸出指標參數改為 C# out 參數。
/// </summary>
internal static unsafe class Shots
{
    public const int MAX_PWEAPON = 81;

    public static readonly PlayerShotDataType[] playerShotData = new PlayerShotDataType[MAX_PWEAPON + 1];
    public static readonly byte[] shotAvail = new byte[MAX_PWEAPON]; // 0:Avail 1-255:Duration left

    public static void simulate_player_shots()
    {
        var player = Players.player;
        for (int z = 0; z < MAX_PWEAPON; z++)
        {
            if (shotAvail[z] != 0)
            {
                shotAvail[z]--;
                if (z != MAX_PWEAPON - 1)
                {
                    ref PlayerShotDataType shot = ref playerShotData[z];

                    shot.shotXM += shot.shotXC;
                    if (shot.shotXM <= 100)
                        shot.shotX += shot.shotXM;

                    shot.shotYM += shot.shotYC;
                    shot.shotY += shot.shotYM;

                    if (shot.shotYM > 100)
                    {
                        shot.shotY -= 120;
                        shot.shotY += (short)player[0].delta_y_shot_move;
                    }

                    if (shot.shotComplicated)
                    {
                        shot.shotDevX += shot.shotDirX;
                        shot.shotX += shot.shotDevX;
                        if (Math.Abs(shot.shotDevX) == shot.shotCirSizeX)
                            shot.shotDirX = (short)-shot.shotDirX;

                        shot.shotDevY += shot.shotDirY;
                        shot.shotY += shot.shotDevY;
                        if (Math.Abs(shot.shotDevY) == shot.shotCirSizeY)
                            shot.shotDirY = (short)-shot.shotDirY;
                    }

                    int tempShotX = shot.shotX;
                    int tempShotY = shot.shotY;

                    if (shot.shotX < 0 || shot.shotX > 140 ||
                        shot.shotY < 0 || shot.shotY > 170)
                    {
                        shotAvail[z] = 0;
                        continue;
                    }

                    ushort anim_frame = (ushort)(shot.shotGr + shot.shotAni);
                    if (++shot.shotAni == shot.shotAniMax)
                        shot.shotAni = 0;

                    if (anim_frame < 60000)
                    {
                        if (anim_frame > 1000)
                            anim_frame = (ushort)(anim_frame % 1000);
                        if (anim_frame > 500)
                            Sprites.blit_sprite2(Video.VGAScreen, tempShotX + 1, tempShotY, Sprites.spriteSheet12, (uint)(anim_frame - 500));
                        else
                            Sprites.blit_sprite2(Video.VGAScreen, tempShotX + 1, tempShotY, Sprites.spriteSheet8, anim_frame);
                    }
                }
            }
        }
    }

    private static readonly ushort[] linkMultiGr = { 77,221,183,301,1,282,164,202,58,201,163,281,39,300,182,220,77 };
    private static readonly ushort[] linkSonicGr = { 85,242,131,303,47,284,150,223,66,224,149,283,9,302,130,243,85 };
    private static readonly ushort[] linkMult2Gr = { 78,299,295,297,2,278,276,280,59,279,275,277,40,296,294,298,78 };

    public static void player_shot_set_direction(int shot_id, uint weapon_id, float direction)
    {
        ref PlayerShotDataType shot = ref playerShotData[shot_id];

        shot.shotXM = (short)-(int)MathF.Round(MathF.Sin(direction) * shot.shotYM, MidpointRounding.AwayFromZero);
        shot.shotYM = (short)-(int)MathF.Round(MathF.Cos(direction) * shot.shotYM, MidpointRounding.AwayFromZero);

        int rounded_dir;
        switch (weapon_id)
        {
        case 27:
        case 32:
        case 10:
            rounded_dir = (int)MathF.Round((float)(direction * (16 / (2 * Opentyr.M_PI))), MidpointRounding.AwayFromZero);
            shot.shotGr = linkMultiGr[rounded_dir];
            break;
        case 28:
        case 33:
        case 11:
            rounded_dir = (int)MathF.Round((float)(direction * (16 / (2 * Opentyr.M_PI))), MidpointRounding.AwayFromZero);
            shot.shotGr = linkSonicGr[rounded_dir];
            break;
        case 30:
        case 35:
        case 14:
            if (direction > Opentyr.M_PI_2 && direction < Opentyr.M_PI + Opentyr.M_PI_2)
                shot.shotYC = 1;
            break;
        case 38:
        case 22:
            rounded_dir = (int)MathF.Round((float)(direction * (16 / (2 * Opentyr.M_PI))), MidpointRounding.AwayFromZero);
            shot.shotGr = linkMult2Gr[rounded_dir];
            break;
        }
    }

    public static bool player_shot_move_and_draw(
        int shot_id, out bool out_is_special,
        out int out_shotx, out int out_shoty,
        out short out_shot_damage, out byte out_blast_filter,
        out byte out_chain, out byte out_playerNum,
        out ushort out_special_radiusw, out ushort out_special_radiush)
    {
        out_is_special = false;
        out_shotx = 0; out_shoty = 0;
        out_shot_damage = 0; out_blast_filter = 0; out_chain = 0; out_playerNum = 0;
        out_special_radiusw = 0; out_special_radiush = 0;

        var player = Players.player;
        ref PlayerShotDataType shot = ref playerShotData[shot_id];

        shotAvail[shot_id]--;
        if (shot_id != MAX_PWEAPON - 1)
        {
            shot.shotXM += shot.shotXC;
            shot.shotX += shot.shotXM;
            short tmp_shotXM = shot.shotXM;

            if (shot.shotXM > 100)
            {
                if (shot.shotXM == 101)
                {
                    shot.shotX -= 101;
                    shot.shotX += (short)player[shot.playerNumber - 1].delta_x_shot_move;
                    shot.shotY += (short)player[shot.playerNumber - 1].delta_y_shot_move;
                }
                else
                {
                    shot.shotX -= 120;
                    shot.shotX += (short)player[shot.playerNumber - 1].delta_x_shot_move;
                }
            }

            shot.shotYM += shot.shotYC;
            shot.shotY += shot.shotYM;

            if (shot.shotYM > 100)
            {
                shot.shotY -= 120;
                shot.shotY += (short)player[shot.playerNumber - 1].delta_y_shot_move;
            }

            if (shot.shotComplicated)
            {
                shot.shotDevX += shot.shotDirX;
                shot.shotX += shot.shotDevX;
                if (Math.Abs(shot.shotDevX) == shot.shotCirSizeX)
                    shot.shotDirX = (short)-shot.shotDirX;

                shot.shotDevY += shot.shotDirY;
                shot.shotY += shot.shotDevY;
                if (Math.Abs(shot.shotDevY) == shot.shotCirSizeY)
                    shot.shotDirY = (short)-shot.shotDirY;
            }

            out_shotx = shot.shotX;
            out_shoty = shot.shotY;

            if (shot.shotX < -34 || shot.shotX > 290 ||
                shot.shotY < -15 || shot.shotY > 190)
            {
                shotAvail[shot_id] = 0;
                return false;
            }

            if (shot.shotTrail != 255)
            {
                if (shot.shotTrail == 98)
                    Varz.JE_setupExplosion(shot.shotX - shot.shotXM, shot.shotY - shot.shotYM, 0, shot.shotTrail, false, false);
                else
                    Varz.JE_setupExplosion(shot.shotX, shot.shotY, 0, shot.shotTrail, false, false);
            }

            if (shot.aimAtEnemy != 0)
            {
                if (--shot.aimDelay == 0)
                {
                    shot.aimDelay = shot.aimDelayMax;

                    if (Varz.enemyAvail[shot.aimAtEnemy - 1] != 1)
                    {
                        if (shot.shotX < Varz.enemy[shot.aimAtEnemy - 1].ex)
                            shot.shotXM++;
                        else
                            shot.shotXM--;

                        if (shot.shotY < Varz.enemy[shot.aimAtEnemy - 1].ey)
                            shot.shotYM++;
                        else
                            shot.shotYM--;
                    }
                    else
                    {
                        if (shot.shotXM > 0)
                            shot.shotXM++;
                        else
                            shot.shotXM--;
                    }
                }
            }

            ushort sprite_frame = (ushort)(shot.shotGr + shot.shotAni);
            if (++shot.shotAni == shot.shotAniMax)
                shot.shotAni = 0;

            out_shot_damage = shot.shotDmg;
            out_blast_filter = shot.shotBlastFilter;
            out_chain = shot.chainReaction;
            out_playerNum = shot.playerNumber;

            out_is_special = sprite_frame > 60000;

            if (out_is_special)
            {
                Sprites.blit_sprite_blend(Video.VGAScreen, out_shotx + 1, out_shoty, Sprites.OPTION_SHAPES, (uint)(sprite_frame - 60001));

                out_special_radiusw = (ushort)(Sprites.sprite(Sprites.OPTION_SHAPES, (uint)(sprite_frame - 60001)).width / 2);
                out_special_radiush = (ushort)(Sprites.sprite(Sprites.OPTION_SHAPES, (uint)(sprite_frame - 60001)).height / 2);
            }
            else
            {
                if (sprite_frame > 1000)
                {
                    Varz.JE_doSP((ushort)(out_shotx + 1 + 6), (ushort)(out_shoty + 6), 5, 3, (byte)((sprite_frame / 1000) << 4));
                    sprite_frame = (ushort)(sprite_frame % 1000);
                }
                if (sprite_frame > 500)
                {
                    if (Config.background2 && out_shoty + Varz.shadowYDist < 190 && tmp_shotXM < 100)
                        Sprites.blit_sprite2_darken(Video.VGAScreen, out_shotx + 1, out_shoty + (int)Varz.shadowYDist, Sprites.spriteSheet12, (uint)(sprite_frame - 500));
                    Sprites.blit_sprite2(Video.VGAScreen, out_shotx + 1, out_shoty, Sprites.spriteSheet12, (uint)(sprite_frame - 500));
                }
                else
                {
                    if (Config.background2 && out_shoty + Varz.shadowYDist < 190 && tmp_shotXM < 100)
                        Sprites.blit_sprite2_darken(Video.VGAScreen, out_shotx + 1, out_shoty + (int)Varz.shadowYDist, Sprites.spriteSheet8, sprite_frame);
                    Sprites.blit_sprite2(Video.VGAScreen, out_shotx + 1, out_shoty, Sprites.spriteSheet8, sprite_frame);
                }
            }
        }

        return true;
    }

    private static readonly byte[] soundChannel = { 0, 2, 4, 4, 2, 2, 5, 5, 1, 4, 1 };

    public static int player_shot_create(ushort portNum, uint bay_i, ushort PX, ushort PY, ushort mouseX, ushort mouseY, ushort wpNum, byte playerNum)
    {
        // Bounds check
        if (portNum > Lvlmast.PORT_NUM || wpNum <= 0 || wpNum > Lvlmast.WEAP_NUM)
            return MAX_PWEAPON;

        JE_WeaponType weapon = Episodes.weapons[wpNum];

        if (Config.power < Episodes.weaponPort[portNum].poweruse)
            return MAX_PWEAPON;
        Config.power -= Episodes.weaponPort[portNum].poweruse;

        if (weapon.sound > 0)
            Varz.soundQueue[soundChannel[bay_i]] = weapon.sound;

        var player = Players.player;
        int shot_id = MAX_PWEAPON;
        for (int multi_i = 1; multi_i <= weapon.multi; multi_i++)
        {
            for (shot_id = 0; shot_id < MAX_PWEAPON; shot_id++)
                if (shotAvail[shot_id] == 0)
                    break;
            if (shot_id == MAX_PWEAPON)
                return MAX_PWEAPON;

            if (Config.shotMultiPos[bay_i] == weapon.max || Config.shotMultiPos[bay_i] > 8)
                Config.shotMultiPos[bay_i] = 1;
            else
                Config.shotMultiPos[bay_i]++;

            int mp = Config.shotMultiPos[bay_i] - 1;

            ref PlayerShotDataType shot = ref playerShotData[shot_id];
            shot.chainReaction = 0;
            shot.playerNumber = playerNum;
            shot.shotAni = 0;
            shot.shotComplicated = weapon.circlesize != 0;

            if (weapon.circlesize == 0)
            {
                shot.shotDevX = 0;
                shot.shotDirX = 0;
                shot.shotDevY = 0;
                shot.shotDirY = 0;
                shot.shotCirSizeX = 0;
                shot.shotCirSizeY = 0;
            }
            else
            {
                byte circsize = weapon.circlesize;
                if (circsize > 19)
                {
                    byte circsize_mod20 = (byte)(circsize % 20);
                    shot.shotCirSizeX = circsize_mod20;
                    shot.shotDevX = (short)(circsize_mod20 >> 1);

                    circsize = (byte)(circsize / 20);
                    shot.shotCirSizeY = circsize;
                    shot.shotDevY = (short)(circsize >> 1);
                }
                else
                {
                    shot.shotCirSizeX = circsize;
                    shot.shotCirSizeY = circsize;
                    shot.shotDevX = (short)(circsize >> 1);
                    shot.shotDevY = (short)(circsize >> 1);
                }
                shot.shotDirX = 1;
                shot.shotDirY = -1;
            }

            shot.shotTrail = weapon.trail;

            if (weapon.attack[mp] > 99 && weapon.attack[mp] < 250)
            {
                shot.chainReaction = (byte)(weapon.attack[mp] - 100);
                shot.shotDmg = 1;
            }
            else
            {
                shot.shotDmg = weapon.attack[mp];
            }

            shot.shotBlastFilter = weapon.shipblastfilter;

            int tmp_by = weapon.by[mp];

            shot.shotX = (short)(PX + weapon.bx[mp]);
            shot.shotY = (short)(PY + tmp_by);
            shot.shotYC = (short)-weapon.acceleration;
            shot.shotXC = weapon.accelerationx;
            shot.shotXM = weapon.sx[mp];

            byte del = weapon.del[mp];
            if (del == 121)
            {
                shot.shotTrail = 0;
                del = 255;
            }

            shot.shotGr = weapon.sg[mp];
            if (shot.shotGr == 0)
                shotAvail[shot_id] = 0;
            else
                shotAvail[shot_id] = del;

            if (del > 100 && del < 120)
                shot.shotAniMax = (ushort)(del - 100 + 1);
            else
                shot.shotAniMax = (ushort)(weapon.weapani + 1);

            if (del == 99 || del == 98)
            {
                tmp_by = PX - mouseX;
                if (tmp_by < -5)
                    tmp_by = -5;
                else if (tmp_by > 5)
                    tmp_by = 5;
                shot.shotXM += (short)tmp_by;
            }

            if (del == 99 || del == 100)
            {
                tmp_by = PY - mouseY - weapon.sy[mp];
                if (tmp_by < -4)
                    tmp_by = -4;
                else if (tmp_by > 4)
                    tmp_by = 4;
                shot.shotYM = (short)tmp_by;
            }
            else if (weapon.sy[mp] == 98)
            {
                shot.shotYM = 0;
                shot.shotYC = -1;
            }
            else if (weapon.sy[mp] > 100)
            {
                shot.shotYM = weapon.sy[mp];
                shot.shotY -= (short)player[shot.playerNumber - 1].delta_y_shot_move;
            }
            else
            {
                shot.shotYM = (short)-weapon.sy[mp];
            }

            if (weapon.sx[mp] > 100)
            {
                shot.shotXM = weapon.sx[mp];
                shot.shotX -= (short)player[shot.playerNumber - 1].delta_x_shot_move;
                if (shot.shotXM == 101)
                    shot.shotY -= (short)player[shot.playerNumber - 1].delta_y_shot_move;
            }

            if (weapon.aim > 5) // Guided Shot
            {
                uint best_dist = 65000;
                byte closest_enemy = 0;
                for (Varz.x = 0; Varz.x < 100; Varz.x++)
                {
                    if (Varz.enemyAvail[Varz.x] != 1 && !Varz.enemy[Varz.x].scoreitem)
                    {
                        Varz.y = (ushort)(Math.Abs(Varz.enemy[Varz.x].ex - shot.shotX) + Math.Abs(Varz.enemy[Varz.x].ey - shot.shotY));
                        if (Varz.y < best_dist)
                        {
                            best_dist = Varz.y;
                            closest_enemy = (byte)(Varz.x + 1);
                        }
                    }
                }
                shot.aimAtEnemy = closest_enemy;
                shot.aimDelay = 5;
                shot.aimDelayMax = (byte)(weapon.aim - 5);
            }
            else
            {
                shot.aimAtEnemy = 0;
            }

            Config.shotRepeat[bay_i] = weapon.shotrepeat;
        }

        return shot_id;
    }
}
