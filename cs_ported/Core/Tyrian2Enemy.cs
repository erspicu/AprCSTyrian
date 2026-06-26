namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c 的敵人工廠/生成：JE_makeEnemy / JE_createNewEventEnemy / JE_eventJump。
/// JE_makeEnemy 以「敵人索引」操作 Varz.enemy[en]（C 原為指標）；sprite2s/enemydatofs 改值/索引。
/// </summary>
internal static unsafe partial class Tyrian2
{
    /// <summary>對應 JE_makeEnemy：由 enemyDat 初始化一個敵人。回傳要設給 enemyAvail 的值。</summary>
    public static uint JE_makeEnemy(int en, ushort eDatI, short uniqueShapeTableI)
    {
        uint avail;
        byte shapeTableI;

        if (Config.superArcadeMode != VarzConst.SA_NONE && eDatI == 534)
            eDatI = 533;
        int d = eDatI;

        shapeTableI = uniqueShapeTableI > 0 ? (byte)uniqueShapeTableI : Episodes.enemyDat[d].shapebank;

        // 未找到 sprite 表時沿用敵人槽舊值（同 C）
        if (shapeTableI == 21)
        {
            Varz.enemy[en].sprite2s = Sprites.spriteSheet11; // Coins&Gems
        }
        else if (shapeTableI == 26)
        {
            Varz.enemy[en].sprite2s = Sprites.spriteSheet10; // Two-Player Stuff
        }
        else
        {
            for (int i = 0; i < Sprites.enemySpriteSheetIds.Length; ++i)
                if (shapeTableI == Sprites.enemySpriteSheetIds[i])
                {
                    Varz.enemy[en].sprite2s = Sprites.enemySpriteSheets[i];
                   
                }
        }
        // 若未找到：沿用敵人槽先前的 sprite2s（與 C 同：保留舊值）

        Varz.enemy[en].enemydatofs = d;
        Varz.enemy[en].mapoffset = 0;

        for (int i = 0; i < 3; ++i)
            Varz.enemy[en].eshotmultipos[i] = 0;

        Varz.enemy[en].enemyground = (Episodes.enemyDat[d].explosiontype & 1) == 0;
        Varz.enemy[en].explonum = (byte)(Episodes.enemyDat[d].explosiontype >> 1);

        Varz.enemy[en].launchfreq = Episodes.enemyDat[d].elaunchfreq;
        Varz.enemy[en].launchwait = Episodes.enemyDat[d].elaunchfreq;
        Varz.enemy[en].launchtype = (ushort)(Episodes.enemyDat[d].elaunchtype % 1000);
        Varz.enemy[en].launchspecial = (byte)(Episodes.enemyDat[d].elaunchtype / 1000);

        Varz.enemy[en].xaccel = (byte)Episodes.enemyDat[d].xaccel;
        Varz.enemy[en].yaccel = (byte)Episodes.enemyDat[d].yaccel;

        Varz.enemy[en].xminbounce = -10000;
        Varz.enemy[en].xmaxbounce = 10000;
        Varz.enemy[en].yminbounce = -10000;
        Varz.enemy[en].ymaxbounce = 10000;

        for (int i = 0; i < 3; ++i)
            Varz.enemy[en].tur[i] = Episodes.enemyDat[d].tur[i];

        Varz.enemy[en].ani = Episodes.enemyDat[d].ani;
        Varz.enemy[en].animin = 1;

        switch (Episodes.enemyDat[d].animate)
        {
            case 0:
                Varz.enemy[en].enemycycle = 1; Varz.enemy[en].aniactive = 0;
                Varz.enemy[en].animax = 0; Varz.enemy[en].aniwhenfire = 0;
                break;
            case 1:
                Varz.enemy[en].enemycycle = 0; Varz.enemy[en].aniactive = 1;
                Varz.enemy[en].animax = 0; Varz.enemy[en].aniwhenfire = 0;
                break;
            case 2:
                Varz.enemy[en].enemycycle = 1; Varz.enemy[en].aniactive = 2;
                Varz.enemy[en].animax = Varz.enemy[en].ani; Varz.enemy[en].aniwhenfire = 2;
                break;
        }

        if (Episodes.enemyDat[d].startxc != 0)
            Varz.enemy[en].ex = (short)(Episodes.enemyDat[d].startx + (MtRand.mt_rand() % (Episodes.enemyDat[d].startxc * 2)) - Episodes.enemyDat[d].startxc + 1);
        else
            Varz.enemy[en].ex = (short)(Episodes.enemyDat[d].startx + 1);

        if (Episodes.enemyDat[d].startyc != 0)
            Varz.enemy[en].ey = (short)(Episodes.enemyDat[d].starty + (MtRand.mt_rand() % (Episodes.enemyDat[d].startyc * 2)) - Episodes.enemyDat[d].startyc + 1);
        else
            Varz.enemy[en].ey = (short)(Episodes.enemyDat[d].starty + 1);

        Varz.enemy[en].exc = Episodes.enemyDat[d].xmove;
        Varz.enemy[en].eyc = Episodes.enemyDat[d].ymove;
        Varz.enemy[en].excc = Episodes.enemyDat[d].xcaccel;
        Varz.enemy[en].eycc = Episodes.enemyDat[d].ycaccel;
        Varz.enemy[en].exccw = (sbyte)Math.Abs(Varz.enemy[en].excc);
        Varz.enemy[en].exccwmax = (byte)Varz.enemy[en].exccw;
        Varz.enemy[en].eyccw = (sbyte)Math.Abs(Varz.enemy[en].eycc);
        Varz.enemy[en].eyccwmax = (byte)Varz.enemy[en].eyccw;
        Varz.enemy[en].exccadd = (short)(Varz.enemy[en].excc > 0 ? 1 : -1);
        Varz.enemy[en].eyccadd = (short)(Varz.enemy[en].eycc > 0 ? 1 : -1);
        Varz.enemy[en].special = false;
        Varz.enemy[en].iced = 0;

        if (Episodes.enemyDat[d].xrev == 0) Varz.enemy[en].exrev = 100;
        else if (Episodes.enemyDat[d].xrev == -99) Varz.enemy[en].exrev = 0;
        else Varz.enemy[en].exrev = Episodes.enemyDat[d].xrev;

        if (Episodes.enemyDat[d].yrev == 0) Varz.enemy[en].eyrev = 100;
        else if (Episodes.enemyDat[d].yrev == -99) Varz.enemy[en].eyrev = 0;
        else Varz.enemy[en].eyrev = Episodes.enemyDat[d].yrev;

        Varz.enemy[en].exca = (sbyte)(Varz.enemy[en].xaccel > 0 ? 1 : -1);
        Varz.enemy[en].eyca = (sbyte)(Varz.enemy[en].yaccel > 0 ? 1 : -1);

        Varz.enemy[en].enemytype = eDatI;

        for (int i = 0; i < 3; ++i)
        {
            if (Varz.enemy[en].tur[i] == 252) Varz.enemy[en].eshotwait[i] = 1;
            else if (Varz.enemy[en].tur[i] > 0) Varz.enemy[en].eshotwait[i] = 20;
            else Varz.enemy[en].eshotwait[i] = 255;
        }

        for (int i = 0; i < 20; ++i)
            Varz.enemy[en].egr[i] = Episodes.enemyDat[d].egraphic[i];
        Varz.enemy[en].size = Episodes.enemyDat[d].esize;
        Varz.enemy[en].linknum = 0;
        Varz.enemy[en].edamaged = Episodes.enemyDat[d].dani < 0;
        Varz.enemy[en].enemydie = Episodes.enemyDat[d].eenemydie;

        for (int i = 0; i < 3; ++i)
            Varz.enemy[en].freq[i] = Episodes.enemyDat[d].freq[i];

        Varz.enemy[en].edani = Episodes.enemyDat[d].dani;
        Varz.enemy[en].edgr = Episodes.enemyDat[d].dgr;
        Varz.enemy[en].edlevel = Episodes.enemyDat[d].dlevel;

        Varz.enemy[en].fixedmovey = 0;
        Varz.enemy[en].filter = 0x00;

        short val = Episodes.enemyDat[d].value;
        int tempValue = 0;
        if (val > 1 && val < 10000)
        {
            tempValue = (int)difficultyValue(val);
            if (tempValue > 10000) tempValue = 10000;
            Varz.enemy[en].evalue = (short)tempValue;
        }
        else
        {
            Varz.enemy[en].evalue = val;
        }

        int tempArmor = 1;
        byte armor = Episodes.enemyDat[d].armor;
        if (armor > 0)
        {
            tempArmor = armor != 255 ? difficultyArmor(armor) : 255;
            if (armor != 255 && tempArmor > 254) tempArmor = 254;
            Varz.enemy[en].armorleft = (byte)tempArmor;
            avail = 0;
            Varz.enemy[en].scoreitem = false;
        }
        else
        {
            avail = 2;
            Varz.enemy[en].armorleft = 255;
            Varz.enemy[en].scoreitem = Varz.enemy[en].evalue != 0;
        }

        if (!Varz.enemy[en].scoreitem)
            totalEnemy++;

        return avail;
    }

    /// <summary>對應 JE_createNewEventEnemy：在 [enemyOffset, +25) 找空槽生成事件敵人。</summary>
    public static void JE_createNewEventEnemy(byte enemyTypeOfs, ushort enemyOffset, short uniqueShapeTableI)
    {
        Varz.b = 0;
        for (int i = enemyOffset; i < enemyOffset + 25; i++)
            if (Varz.enemyAvail[i] == 1) { Varz.b = i + 1; break; }

        if (Varz.b == 0)
            return;

        int en = Varz.b - 1;
        Varz.tempW = (ushort)(eventRec[eventLoc - 1].eventdat + enemyTypeOfs);
        Varz.enemyAvail[en] = (byte)JE_makeEnemy(en, Varz.tempW, uniqueShapeTableI);

        if (eventRec[eventLoc - 1].eventdat2 != -99)
        {
            int ex2 = eventRec[eventLoc - 1].eventdat2;
            switch (enemyOffset)
            {
                case 0:
                    Varz.enemy[en].ex = (short)(ex2 - (Backgrnd.mapX - 1) * 24);
                    Varz.enemy[en].ey -= (short)Backgrnd.backMove2;
                    break;
                case 25:
                case 75:
                    Varz.enemy[en].ex = (short)(ex2 - (Backgrnd.mapX - 1) * 24 - 12);
                    Varz.enemy[en].ey -= (short)Backgrnd.backMove;
                    break;
                case 50:
                    if (background3x1)
                        Varz.enemy[en].ex = (short)(ex2 - (Backgrnd.mapX - 1) * 24 - 12);
                    else
                        Varz.enemy[en].ex = (short)(ex2 - Backgrnd.mapX3 * 24 - 24 * 2 + 6);
                    Varz.enemy[en].ey -= (short)Backgrnd.backMove3;
                    if (background3x1b)
                        Varz.enemy[en].ex -= 6;
                    break;
            }
            Varz.enemy[en].ey = -28;
            if (background3x1b && enemyOffset == 50)
                Varz.enemy[en].ey += 4;
        }

        if (smallEnemyAdjust && Varz.enemy[en].size == 0)
        {
            Varz.enemy[en].ex -= 10;
            Varz.enemy[en].ey -= 7;
        }

        Varz.enemy[en].ey += eventRec[eventLoc - 1].eventdat5;
        Varz.enemy[en].eyc += (sbyte)eventRec[eventLoc - 1].eventdat3;
        Varz.enemy[en].linknum = eventRec[eventLoc - 1].eventdat4;
        Varz.enemy[en].fixedmovey = eventRec[eventLoc - 1].eventdat6;
    }

    /// <summary>
    /// 敵人受擊後跨越 edlevel 門檻時的傷害態轉換（受損圖/受損動畫/或死亡）。
    /// 對應 tyrian2.c 1574-1612 的單一敵人分支（連動敵人群組待後續）。
    /// </summary>
    public static void JE_enemyDamageTransform(int b)
    {
        Varz.enemy[b].enemycycle = 1;
        Varz.enemy[b].edamaged = !Varz.enemy[b].edamaged;

        if (Varz.enemy[b].edani != 0)
        {
            Varz.enemy[b].ani = (byte)Math.Abs(Varz.enemy[b].edani);
            Varz.enemy[b].aniactive = 1;
            Varz.enemy[b].animax = 0;
            Varz.enemy[b].animin = (byte)Varz.enemy[b].edgr;
            Varz.enemy[b].enemycycle = (byte)(Varz.enemy[b].animin - 1);
        }
        else if (Varz.enemy[b].edgr > 0)
        {
            Varz.enemy[b].egr[0] = Varz.enemy[b].edgr;
            Varz.enemy[b].ani = 1;
            Varz.enemy[b].aniactive = 0;
            Varz.enemy[b].animax = 0;
            Varz.enemy[b].animin = 1;
        }
        else
        {
            Varz.enemyAvail[b] = 1; // 無受損圖 → 死亡
        }

        Varz.enemy[b].aniwhenfire = 0;

        if (Varz.enemy[b].armorleft > Varz.enemy[b].edlevel)
            Varz.enemy[b].armorleft = (byte)Varz.enemy[b].edlevel;

        int tempX = Varz.enemy[b].ex + Varz.enemy[b].mapoffset, tempY = Varz.enemy[b].ey;
        if (Episodes.enemyDat[Varz.enemy[b].enemytype].esize != 1)
            Varz.JE_setupExplosion(tempX, tempY - 6, 0, 1, false, false);
        else
            Varz.JE_setupExplosionLarge(Varz.enemy[b].enemyground, (byte)(Varz.enemy[b].explonum / 2), tempX, tempY);
    }

    private static void JE_barX(int x1, int y1, int x2, int y2, int col)
    {
        Vga256d.fill_rectangle_xy(Video.VGAScreen, x1, y1, x2, y1, (byte)(col + 1));
        Vga256d.fill_rectangle_xy(Video.VGAScreen, x1, y1 + 1, x2, y2 - 1, (byte)col);
        Vga256d.fill_rectangle_xy(Video.VGAScreen, x1, y2, x2, y2, (byte)(col - 1));
    }

    /// <summary>boss 血條繪製。對應 tyrian2.c draw_boss_bar。</summary>
    public static void draw_boss_bar()
    {
        for (int b = 0; b < 2; b++)
        {
            if (Varz.boss_bar[b].link_num == 0)
                continue;

            uint armor = 256;
            for (int e = 0; e < 100; e++)
                if (Varz.enemyAvail[e] != 1 && Varz.enemy[e].linknum == Varz.boss_bar[b].link_num)
                    if (Varz.enemy[e].armorleft < armor)
                        armor = Varz.enemy[e].armorleft;

            if (armor > 255 || armor == 0)
                Varz.boss_bar[b].link_num = 0;
            else
                Varz.boss_bar[b].armor = (byte)((armor == 255) ? 254 : armor);
        }

        int bars = (Varz.boss_bar[0].link_num != 0 ? 1 : 0) + (Varz.boss_bar[1].link_num != 0 ? 1 : 0);

        if (bars == 1 && Varz.boss_bar[0].link_num == 0)
        {
            Varz.boss_bar[0] = Varz.boss_bar[1];
            Varz.boss_bar[1].link_num = 0;
        }

        for (int b = 0; b < bars; b++)
        {
            int x = (bars == 2) ? ((b == 0) ? 125 : 185) : (Varz.levelTimer ? 250 : 155);
            JE_barX(x - 25, 7, x + 25, 12, 115);
            JE_barX(x - (Varz.boss_bar[b].armor / 10), 7, x + (Varz.boss_bar[b].armor + 5) / 10, 12, 118 + Varz.boss_bar[b].color);
            if (Varz.boss_bar[b].color > 0)
                Varz.boss_bar[b].color--;
        }
    }

    /// <summary>對應 JE_newEnemy：在 [enemyOffset, +25) 找空槽生成敵人，回傳 slot+1（0=無空槽）。</summary>
    public static int JE_newEnemy(int enemyOffset, ushort eDatI, short uniqueShapeTableI)
    {
        for (int i = enemyOffset; i < enemyOffset + 25; ++i)
        {
            if (Varz.enemyAvail[i] == 1)
            {
                Varz.enemyAvail[i] = (byte)JE_makeEnemy(i, eDatI, uniqueShapeTableI);
                return i + 1;
            }
        }
        return 0;
    }

    public static void JE_eventJump(ushort jump)
    {
        if (jump == 65535)
        {
            curLoc = returnLoc;
        }
        else
        {
            returnLoc = (ushort)(curLoc + 1);
            curLoc = jump;
        }
        ushort t = 0;
        do { t++; } while (!(eventRec[t - 1].eventtime >= curLoc));
        eventLoc = (ushort)(t - 1);
    }

    private static void blit_enemy(int i, int xofs, int yofs, int spriteOfs)
    {
        if (Varz.enemy[i].sprite2s.data == null)
            return;
        int x = Varz.enemy[i].ex + xofs + Backgrnd.tempMapXOfs;
        int y = Varz.enemy[i].ey + yofs;
        int index = Varz.enemy[i].egr[Varz.enemy[i].enemycycle - 1] + spriteOfs;
        if (Varz.enemy[i].filter != 0)
            Sprites.blit_sprite2_filter(Video.VGAScreen, x, y, Varz.enemy[i].sprite2s, (uint)index, Varz.enemy[i].filter);
        else
            Sprites.blit_sprite2(Video.VGAScreen, x, y, Varz.enemy[i].sprite2s, (uint)index);
    }

    /// <summary>
    /// 敵人更新/繪製（對應 JE_drawEnemy 的核心：homing + 動畫 + size 多格繪製 + 立方加速 + 移動 + 彈跳 + 砲塔發射）。
    /// 簡化省略：傷害閃白 filter 動畫、特殊砲塔型、boss、enemyOnScreen 計數。
    /// </summary>
    public static void JE_updateEnemies()
    {
        var player = Players.player;
        Backgrnd.tempBackMove = Backgrnd.backMove;
        enemyOnScreen = 0;

        for (int z = 0; z < 100; z++)
        {
            if (Varz.enemyAvail[z] == 1)
                continue;

            Varz.enemy[z].mapoffset = Backgrnd.tempMapXOfs;

            // homing 加速
            if (Varz.enemy[z].xaccel != 0 && (uint)Varz.enemy[z].xaccel - 89u > MtRand.mt_rand() % 11)
            {
                if (player[0].x > Varz.enemy[z].ex)
                {
                    if (Varz.enemy[z].exc < Varz.enemy[z].xaccel - 89) Varz.enemy[z].exc++;
                }
                else if (Varz.enemy[z].exc >= 0 || -Varz.enemy[z].exc < Varz.enemy[z].xaccel - 89)
                    Varz.enemy[z].exc--;
            }
            if (Varz.enemy[z].yaccel != 0 && (uint)Varz.enemy[z].yaccel - 89u > MtRand.mt_rand() % 11)
            {
                if (player[0].y > Varz.enemy[z].ey)
                {
                    if (Varz.enemy[z].eyc < Varz.enemy[z].yaccel - 89) Varz.enemy[z].eyc++;
                }
                else if (Varz.enemy[z].eyc >= 0 || -Varz.enemy[z].eyc < Varz.enemy[z].yaccel - 89)
                    Varz.enemy[z].eyc--;
            }

            bool gone = false;

            int exAbs = Varz.enemy[z].ex + Backgrnd.tempMapXOfs;
            if (exAbs > -29 && exAbs < 300)
            {
                if (Varz.enemy[z].aniactive == 1)
                {
                    Varz.enemy[z].enemycycle++;
                    if (Varz.enemy[z].enemycycle == Varz.enemy[z].animax)
                        Varz.enemy[z].aniactive = Varz.enemy[z].aniwhenfire;
                    else if (Varz.enemy[z].enemycycle > Varz.enemy[z].ani)
                        Varz.enemy[z].enemycycle = Varz.enemy[z].animin;
                }
                if (Varz.enemy[z].enemycycle < 1)
                    Varz.enemy[z].enemycycle = 1;

                if (Varz.enemy[z].egr[Varz.enemy[z].enemycycle - 1] == 999)
                {
                    gone = true;
                }
                else
                {
                    if (Varz.enemy[z].size == 1) // 2x2 敵人
                    {
                        if (Varz.enemy[z].ey > -13)
                        {
                            blit_enemy(z, -6, -7, 0);
                            blit_enemy(z, 6, -7, 1);
                        }
                        if (Varz.enemy[z].ey > -26 && Varz.enemy[z].ey < 182)
                        {
                            blit_enemy(z, -6, 7, 19);
                            blit_enemy(z, 6, 7, 20);
                        }
                    }
                    else
                    {
                        if (Varz.enemy[z].ey > -13)
                            blit_enemy(z, 0, 0, 0);
                    }
                    Varz.enemy[z].filter = 0;
                }
            }

            if (!gone)
            {
                // 立方加速（curved movement）
                if (Varz.enemy[z].excc != 0 && --Varz.enemy[z].exccw <= 0)
                {
                    if (Varz.enemy[z].exc == Varz.enemy[z].exrev)
                    {
                        Varz.enemy[z].excc = (sbyte)-Varz.enemy[z].excc;
                        Varz.enemy[z].exrev = (sbyte)-Varz.enemy[z].exrev;
                        Varz.enemy[z].exccadd = (short)-Varz.enemy[z].exccadd;
                    }
                    else
                    {
                        Varz.enemy[z].exc += (sbyte)Varz.enemy[z].exccadd;
                        Varz.enemy[z].exccw = (sbyte)Varz.enemy[z].exccwmax;
                        if (Varz.enemy[z].exc == Varz.enemy[z].exrev)
                        {
                            Varz.enemy[z].excc = (sbyte)-Varz.enemy[z].excc;
                            Varz.enemy[z].exrev = (sbyte)-Varz.enemy[z].exrev;
                            Varz.enemy[z].exccadd = (short)-Varz.enemy[z].exccadd;
                        }
                    }
                }
                if (Varz.enemy[z].eycc != 0 && --Varz.enemy[z].eyccw <= 0)
                {
                    if (Varz.enemy[z].eyc == Varz.enemy[z].eyrev)
                    {
                        Varz.enemy[z].eycc = (sbyte)-Varz.enemy[z].eycc;
                        Varz.enemy[z].eyrev = (sbyte)-Varz.enemy[z].eyrev;
                        Varz.enemy[z].eyccadd = (short)-Varz.enemy[z].eyccadd;
                    }
                    else
                    {
                        Varz.enemy[z].eyc += (sbyte)Varz.enemy[z].eyccadd;
                        Varz.enemy[z].eyccw = (sbyte)Varz.enemy[z].eyccwmax;
                        if (Varz.enemy[z].eyc == Varz.enemy[z].eyrev)
                        {
                            Varz.enemy[z].eycc = (sbyte)-Varz.enemy[z].eycc;
                            Varz.enemy[z].eyrev = (sbyte)-Varz.enemy[z].eyrev;
                            Varz.enemy[z].eyccadd = (short)-Varz.enemy[z].eyccadd;
                        }
                    }
                }

                Varz.enemy[z].ey += Varz.enemy[z].fixedmovey;
                Varz.enemy[z].ex += Varz.enemy[z].exc;
                if (Varz.enemy[z].ex < -80 || Varz.enemy[z].ex > 340)
                    gone = true;
                else
                {
                    Varz.enemy[z].ey += Varz.enemy[z].eyc;
                    if (Varz.enemy[z].ey < -112 || Varz.enemy[z].ey > 190)
                        gone = true;
                    else
                    {
                        if (Varz.enemy[z].ex <= Varz.enemy[z].xminbounce || Varz.enemy[z].ex >= Varz.enemy[z].xmaxbounce)
                            Varz.enemy[z].exc = (sbyte)-Varz.enemy[z].exc;
                        if (Varz.enemy[z].ey <= Varz.enemy[z].yminbounce || Varz.enemy[z].ey >= Varz.enemy[z].ymaxbounce)
                            Varz.enemy[z].eyc = (sbyte)-Varz.enemy[z].eyc;

                        if (Varz.enemy[z].scoreitem)
                        {
                            if (Varz.enemy[z].ex < -5) Varz.enemy[z].ex++;
                            if (Varz.enemy[z].ex > 245) Varz.enemy[z].ex--;
                        }
                        Varz.enemy[z].ey += (short)Backgrnd.tempBackMove;
                    }
                }
            }

            if (gone)
            {
                Varz.enemyAvail[z] = 1;
                continue;
            }

            // 僅在畫面內且非受損態才計數並發射（對應 JE_drawEnemy 339-351）
            if (Varz.enemy[z].ex > -24 && Varz.enemy[z].ex < 296 && !Varz.enemy[z].edamaged)
            {
                enemyOnScreen++;
                enemyTurretFire(z);
            }
        }
    }

    /// <summary>敵人砲塔發射敵彈（簡化：default 類型，跳過特殊磁鐵/飛彈 251-255）。對應 JE_drawEnemy 363-545。</summary>
    public static void enemyTurretFire(int z)
    {
        int tempX = Varz.enemy[z].ex, tempY = Varz.enemy[z].ey, ofs = Backgrnd.tempMapXOfs;

        for (int j = 3; j > 0; j--)
        {
            if (Varz.enemy[z].freq[j - 1] == 0)
                continue;
            int tur = Varz.enemy[z].tur[j - 1];
            if (--Varz.enemy[z].eshotwait[j - 1] != 0 || tur == 0)
                continue;

            Varz.enemy[z].eshotwait[j - 1] = Varz.enemy[z].freq[j - 1];
            if (Config.difficultyLevel > Config.DIFFICULTY_NORMAL)
            {
                Varz.enemy[z].eshotwait[j - 1] = (byte)(Varz.enemy[z].eshotwait[j - 1] / 2 + 1);
                if (Config.difficultyLevel > Config.DIFFICULTY_MANIACAL)
                    Varz.enemy[z].eshotwait[j - 1] = (byte)(Varz.enemy[z].eshotwait[j - 1] / 2 + 1);
            }

            // 特殊砲塔型（磁鐵/飛彈/排斥），處理後跳過一般子彈建立
            if (tur >= 251)
            {
                var pl = Players.player;
                switch (tur)
                {
                    case 252: // Savara Boss DualMissile
                        if (Varz.enemy[z].ey > 20)
                        {
                            Varz.JE_setupExplosion(tempX - 8 + ofs, tempY - 20 - Backgrnd.backMove * 8, -2, 6, false, false);
                            Varz.JE_setupExplosion(tempX + 4 + ofs, tempY - 20 - Backgrnd.backMove * 8, -2, 6, false, false);
                        }
                        break;
                    case 251: // Suck-O-Magnet（吸引玩家）
                        {
                            int attraction = 4 - (Math.Abs(pl[0].x - tempX) + Math.Abs(pl[0].y - tempY)) / 100;
                            if (attraction > 0)
                                pl[0].x_velocity += (pl[0].x > tempX) ? -attraction : attraction;
                        }
                        break;
                    case 253: // ShortRange Magnet（推 +2）
                        if (Math.Abs(pl[0].x + 25 - 14 - tempX) < 24 && Math.Abs(pl[0].y - tempY) < 28)
                            pl[0].x_velocity += 2;
                        if (Config.twoPlayerMode && Math.Abs(pl[1].x - 14 - tempX) < 24 && Math.Abs(pl[1].y - tempY) < 28)
                            pl[1].x_velocity += 2;
                        break;
                    case 254: // ShortRange Magnet（推 -2）
                        if (Math.Abs(pl[0].x + 25 - 14 - tempX) < 24 && Math.Abs(pl[0].y - tempY) < 28)
                            pl[0].x_velocity -= 2;
                        if (Config.twoPlayerMode && Math.Abs(pl[1].x - 14 - tempX) < 24 && Math.Abs(pl[1].y - tempY) < 28)
                            pl[1].x_velocity -= 2;
                        break;
                    case 255: // Magneto RePulse（排斥玩家）
                        if (Config.difficultyLevel != Config.DIFFICULTY_EASY)
                        {
                            if (j == 3)
                                Varz.enemy[z].filter = 0x70;
                            else
                            {
                                int repulsion = 4 - (Math.Abs(pl[0].x - tempX) + Math.Abs(pl[0].y - tempY)) / 20;
                                if (repulsion > 0)
                                    pl[0].x_velocity += (pl[0].x > tempX) ? repulsion : -repulsion;
                            }
                        }
                        break;
                }
                continue; // 特殊型不建立一般子彈
            }

            for (int tempCount = Episodes.weapons[tur].multi; tempCount > 0; tempCount--)
            {
                int b2 = -1;
                for (int bb = 0; bb < VarzConst.ENEMY_SHOT_MAX; bb++)
                    if (Varz.enemyShotAvail[bb]) { b2 = bb; break; }
                if (b2 < 0)
                    return;

                Varz.enemyShotAvail[b2] = false;

                if (Episodes.weapons[tur].sound > 0)
                {
                    int si;
                    do { si = (int)(MtRand.mt_rand() % 8); } while (si == 3);
                    Varz.soundQueue[si] = Episodes.weapons[tur].sound;
                }

                if (Varz.enemy[z].aniactive == 2)
                    Varz.enemy[z].aniactive = 1;

                Varz.enemy[z].eshotmultipos[j - 1]++;
                if (Varz.enemy[z].eshotmultipos[j - 1] > Episodes.weapons[tur].max)
                    Varz.enemy[z].eshotmultipos[j - 1] = 1;
                int tp = Varz.enemy[z].eshotmultipos[j - 1] - 1;

                Varz.enemyShot[b2].sx = (short)(tempX + Episodes.weapons[tur].bx[tp] + ofs);
                Varz.enemyShot[b2].sy = (short)(tempY + Episodes.weapons[tur].by[tp]);
                Varz.enemyShot[b2].sdmg = Episodes.weapons[tur].attack[tp];
                Varz.enemyShot[b2].tx = Episodes.weapons[tur].tx;
                Varz.enemyShot[b2].ty = Episodes.weapons[tur].ty;
                Varz.enemyShot[b2].duration = Episodes.weapons[tur].del[tp];
                Varz.enemyShot[b2].animate = 0;
                Varz.enemyShot[b2].animax = Episodes.weapons[tur].weapani;
                Varz.enemyShot[b2].sgr = Episodes.weapons[tur].sg[tp];

                sbyte acc = Episodes.weapons[tur].acceleration, accx = Episodes.weapons[tur].accelerationx;
                sbyte wsx = Episodes.weapons[tur].sx[tp], wsy = Episodes.weapons[tur].sy[tp];
                switch (j)
                {
                    case 1:
                        Varz.enemyShot[b2].syc = acc; Varz.enemyShot[b2].sxc = accx;
                        Varz.enemyShot[b2].sxm = wsx; Varz.enemyShot[b2].sym = wsy;
                        break;
                    case 3:
                        Varz.enemyShot[b2].sxc = (sbyte)-acc; Varz.enemyShot[b2].syc = accx;
                        Varz.enemyShot[b2].sxm = (short)-wsy; Varz.enemyShot[b2].sym = (short)-wsx;
                        break;
                    case 2:
                        Varz.enemyShot[b2].sxc = acc; Varz.enemyShot[b2].syc = (sbyte)-acc;
                        Varz.enemyShot[b2].sxm = wsy; Varz.enemyShot[b2].sym = (short)-wsx;
                        break;
                }

                if (Episodes.weapons[tur].aim > 0)
                {
                    int aim = Episodes.weapons[tur].aim;
                    if (Config.difficultyLevel > Config.DIFFICULTY_NORMAL)
                        aim += Config.difficultyLevel - 2;

                    int aimX = (Players.player[0].x + 25) - tempX - ofs - 4;
                    if (aimX == 0) aimX = 1;
                    int aimY = Players.player[0].y - tempY;
                    if (aimY == 0) aimY = 1;
                    int maxMag = Math.Max(Math.Abs(aimX), Math.Abs(aimY));
                    Varz.enemyShot[b2].sxm = (short)MathF.Round((float)aimX / maxMag * aim);
                    Varz.enemyShot[b2].sym = (short)MathF.Round((float)aimY / maxMag * aim);
                }
            }
        }
    }

    /// <summary>敵彈移動/繪製 + 擊中玩家碰撞。對應 tyrian2.c 1769-1858。</summary>
    public static void simulateEnemyShots()
    {
        var player = Players.player;
        for (int z = 0; z < VarzConst.ENEMY_SHOT_MAX; z++)
        {
            if (Varz.enemyShotAvail[z])
                continue; // true = 空閒

            Varz.enemyShot[z].sxm += Varz.enemyShot[z].sxc;
            Varz.enemyShot[z].sx += Varz.enemyShot[z].sxm;
            if (Varz.enemyShot[z].tx != 0)
            {
                if (Varz.enemyShot[z].sx > player[0].x)
                {
                    if (Varz.enemyShot[z].sxm > -Varz.enemyShot[z].tx) Varz.enemyShot[z].sxm--;
                }
                else if (Varz.enemyShot[z].sxm < Varz.enemyShot[z].tx) Varz.enemyShot[z].sxm++;
            }

            Varz.enemyShot[z].sym += Varz.enemyShot[z].syc;
            Varz.enemyShot[z].sy += Varz.enemyShot[z].sym;
            if (Varz.enemyShot[z].ty != 0)
            {
                if (Varz.enemyShot[z].sy > player[0].y)
                {
                    if (Varz.enemyShot[z].sym > -Varz.enemyShot[z].ty) Varz.enemyShot[z].sym--;
                }
                else if (Varz.enemyShot[z].sym < Varz.enemyShot[z].ty) Varz.enemyShot[z].sym++;
            }

            if (Varz.enemyShot[z].duration-- == 0 || Varz.enemyShot[z].sy > 190 || Varz.enemyShot[z].sy <= -14 || Varz.enemyShot[z].sx > 275 || Varz.enemyShot[z].sx <= 0)
            {
                Varz.enemyShotAvail[z] = true;
            }
            else
            {
                for (int i = 0; i < (Config.twoPlayerMode ? 2 : 1); ++i)
                {
                    if (player[i].is_alive &&
                        Varz.enemyShot[z].sx > player[i].x - (int)player[i].shot_hit_area_x &&
                        Varz.enemyShot[z].sx < player[i].x + (int)player[i].shot_hit_area_x &&
                        Varz.enemyShot[z].sy > player[i].y - (int)player[i].shot_hit_area_y &&
                        Varz.enemyShot[z].sy < player[i].y + (int)player[i].shot_hit_area_y)
                    {
                        int tempX = Varz.enemyShot[z].sx, tempY = Varz.enemyShot[z].sy;
                        byte dmg = Varz.enemyShot[z].sdmg;
                        Varz.enemyShotAvail[z] = true;
                        Varz.JE_setupExplosion(tempX, tempY, 0, 0, false, false);

                        if (player[i].invulnerable_ticks == 0)
                        {
                            byte through = Varz.JE_playerDamage(dmg, i);
                            if (through > 0)
                            {
                                player[i].x_velocity += (Varz.enemyShot[z].sxm * through) / 2;
                                player[i].y_velocity += (Varz.enemyShot[z].sym * through) / 2;
                            }
                        }
                        break;
                    }
                }

                if (!Varz.enemyShotAvail[z])
                {
                    if (Varz.enemyShot[z].animax != 0)
                    {
                        if (++Varz.enemyShot[z].animate >= Varz.enemyShot[z].animax)
                            Varz.enemyShot[z].animate = 0;
                    }
                    if (Varz.enemyShot[z].sgr >= 500)
                        Sprites.blit_sprite2(Video.VGAScreen, Varz.enemyShot[z].sx, Varz.enemyShot[z].sy, Sprites.spriteSheet12, (uint)(Varz.enemyShot[z].sgr + Varz.enemyShot[z].animate - 500));
                    else
                        Sprites.blit_sprite2(Video.VGAScreen, Varz.enemyShot[z].sx, Varz.enemyShot[z].sy, Sprites.spriteSheet8, (uint)(Varz.enemyShot[z].sgr + Varz.enemyShot[z].animate));
                }
            }
        }
    }

    /// <summary>序列爆炸 + 一般爆炸的更新/繪製。對應 tyrian2.c 1888-1961。</summary>
    public static void JE_drawExplosions()
    {
        Varz.enemyStillExploding = false;
        for (int i = 0; i < VarzConst.MAX_REPEATING_EXPLOSIONS; i++)
        {
            if (Varz.rep_explosions[i].ttl != 0)
            {
                Varz.enemyStillExploding = true;
                if (Varz.rep_explosions[i].delay > 0)
                {
                    Varz.rep_explosions[i].delay--;
                    continue;
                }

                Varz.rep_explosions[i].y += (uint)(Backgrnd.backMove2 + 1);
                int tempX = (int)Varz.rep_explosions[i].x + (int)(MtRand.mt_rand() % 24) - 12;
                int tempY = (int)Varz.rep_explosions[i].y + (int)(MtRand.mt_rand() % 27) - 24;

                if (Varz.rep_explosions[i].big)
                {
                    Varz.JE_setupExplosionLarge(false, 2, tempX, tempY);
                    if (Varz.rep_explosions[i].ttl == 1 || MtRand.mt_rand() % 5 == 1)
                        Varz.soundQueue[7] = (byte)Sndmast.S_EXPLOSION_11;
                    else
                        Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_9;
                    Varz.rep_explosions[i].delay = (uint)(4 + (MtRand.mt_rand() % 3));
                }
                else
                {
                    Varz.JE_setupExplosion(tempX, tempY, 0, 1, false, false);
                    Varz.soundQueue[5] = (byte)Sndmast.S_EXPLOSION_4;
                    Varz.rep_explosions[i].delay = 3;
                }

                Varz.rep_explosions[i].ttl--;
            }
        }

        for (int j = 0; j < VarzConst.MAX_EXPLOSIONS; j++)
        {
            if (Varz.explosions[j].ttl != 0)
            {
                if (!Varz.explosions[j].fixedPosition)
                {
                    Varz.explosions[j].sprite++;
                    Varz.explosions[j].y += (short)Tyrian2.explodeMove;
                }
                else if (Varz.explosions[j].followPlayer)
                {
                    Varz.explosions[j].x += (short)Tyrian2.explosionFollowAmountX;
                    Varz.explosions[j].y += (short)Tyrian2.explosionFollowAmountY;
                }
                Varz.explosions[j].y += Varz.explosions[j].deltaY;

                if (Varz.explosions[j].y > 200 - 14)
                {
                    Varz.explosions[j].ttl = 0;
                }
                else
                {
                    if (Config.explosionTransparent)
                        Sprites.blit_sprite2_blend(Video.VGAScreen, Varz.explosions[j].x, Varz.explosions[j].y, Sprites.explosionSpriteSheet, (uint)(Varz.explosions[j].sprite + 1));
                    else
                        Sprites.blit_sprite2(Video.VGAScreen, Varz.explosions[j].x, Varz.explosions[j].y, Sprites.explosionSpriteSheet, (uint)(Varz.explosions[j].sprite + 1));
                    Varz.explosions[j].ttl--;
                }
            }
        }
    }

    private static float difficultyValue(short value)
    {
        switch ((int)Config.difficultyLevel)
        {
            case -1:
            case Config.DIFFICULTY_WIMP: return value * 0.75f;
            case Config.DIFFICULTY_EASY:
            case Config.DIFFICULTY_NORMAL: return value;
            case Config.DIFFICULTY_HARD: return value * 1.125f;
            case Config.DIFFICULTY_IMPOSSIBLE: return value * 1.5f;
            case Config.DIFFICULTY_INSANITY: return value * 2;
            case Config.DIFFICULTY_SUICIDE: return value * 2.5f;
            case Config.DIFFICULTY_MANIACAL:
            case Config.DIFFICULTY_ZINGLON: return value * 4;
            case Config.DIFFICULTY_NORTANEOUS:
            case Config.DIFFICULTY_10: return value * 8;
            default: return 0;
        }
    }

    private static int difficultyArmor(byte armor)
    {
        switch ((int)Config.difficultyLevel)
        {
            case -1:
            case Config.DIFFICULTY_WIMP: return (int)(armor * 0.5f + 1);
            case Config.DIFFICULTY_EASY: return (int)(armor * 0.75f + 1);
            case Config.DIFFICULTY_NORMAL: return armor;
            case Config.DIFFICULTY_HARD: return (int)(armor * 1.2f);
            case Config.DIFFICULTY_IMPOSSIBLE: return (int)(armor * 1.5f);
            case Config.DIFFICULTY_INSANITY: return (int)(armor * 1.8f);
            case Config.DIFFICULTY_SUICIDE: return armor * 2;
            case Config.DIFFICULTY_MANIACAL: return armor * 3;
            case Config.DIFFICULTY_ZINGLON: return armor * 4;
            case Config.DIFFICULTY_NORTANEOUS:
            case Config.DIFFICULTY_10: return armor * 8;
            default: return 1;
        }
    }
}
