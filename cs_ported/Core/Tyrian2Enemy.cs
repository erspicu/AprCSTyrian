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

    /// <summary>
    /// 連動敵人群組死亡（對應 tyrian2.c 1620-1718）：被擊毀敵人 b 及其 linknum 相符的編組敵人
    /// 一起死亡，各自生成 enemydie 後繼、計分、爆炸。temp=被擊敵人 linknum（0→255）。
    /// </summary>
    public static void JE_killEnemyGroup(int b, byte temp, byte playerNum)
    {
        var player = Players.player;

        if (temp == 254 && superEnemy254Jump > 0)
            JE_eventJump(superEnemy254Jump);

        for (int temp2 = 0; temp2 < 100; temp2++)
        {
            if (Varz.enemyAvail[temp2] == 1)
                continue;

            int temp3 = Varz.enemy[temp2].linknum;
            bool match = (temp2 == b) || (temp == 254) ||
                ((temp != 255) && ((temp == temp3) || (temp - 100 == temp3) ||
                                   ((temp3 > 40) && (temp3 / 20 == temp / 20) && (temp3 <= temp))));
            if (!match)
                continue;

            int enemyScreenX = Varz.enemy[temp2].ex + Varz.enemy[temp2].mapoffset;

            if (Varz.enemy[temp2].special)
                globalFlags[Varz.enemy[temp2].flagnum - 1] = Varz.enemy[temp2].setto;

            // 後繼敵人 enemydie
            ushort edie = Varz.enemy[temp2].enemydie;
            if (edie > 0 && !(Config.superArcadeMode != VarzConst.SA_NONE && Episodes.enemyDat[edie].value == 30000))
            {
                int offset = temp2 - (temp2 % 25);
                if (Episodes.enemyDat[edie].value > 30000)
                    offset = 0;
                int nb = JE_newEnemy(offset, edie, 0);
                if (nb != 0)
                {
                    Varz.enemy[nb - 1].scoreitem = Varz.enemy[nb - 1].evalue != 0;
                    Varz.enemy[nb - 1].ex = Varz.enemy[temp2].ex;
                    Varz.enemy[nb - 1].ey = Varz.enemy[temp2].ey;
                }
            }

            // 計分
            if (Varz.enemy[temp2].evalue > 0 && Varz.enemy[temp2].evalue < 10000)
            {
                if (Varz.enemy[temp2].evalue == 1)
                    Config.cubeMax++;
                else
                    player[Config.galagaMode ? 0 : playerNum - 1].cash += (uint)Varz.enemy[temp2].evalue;
            }

            if (Varz.enemy[temp2].edlevel == -1 && temp == temp3)
            {
                Varz.enemy[temp2].edlevel = 0;
                Varz.enemyAvail[temp2] = 2;
                Varz.enemy[temp2].egr[0] = Varz.enemy[temp2].edgr;
                Varz.enemy[temp2].ani = 1;
                Varz.enemy[temp2].aniactive = 0;
                Varz.enemy[temp2].animax = 0;
                Varz.enemy[temp2].animin = 1;
                Varz.enemy[temp2].edamaged = true;
                Varz.enemy[temp2].enemycycle = 1;
            }
            else
            {
                Varz.enemyAvail[temp2] = 1;
                enemyKilled++;
            }

            if (Episodes.enemyDat[Varz.enemy[temp2].enemytype].esize == 1)
            {
                Varz.JE_setupExplosionLarge(Varz.enemy[temp2].enemyground, Varz.enemy[temp2].explonum, enemyScreenX, Varz.enemy[temp2].ey);
                Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_9;
            }
            else
            {
                Varz.JE_setupExplosion(enemyScreenX, Varz.enemy[temp2].ey, 0, 1, false, false);
                Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_8;
            }
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

    /// <summary>對應 tyrian2.c:JE_searchFor —— 尋找指定 linknum 的存活敵人。</summary>
    public static bool JE_searchFor(byte PLType) => JE_searchFor(PLType, out _);

    public static bool JE_searchFor(byte PLType, out byte out_index)
    {
        int found_id = -1;

        for (int i = 0; i < 100; i++)
        {
            if (Varz.enemyAvail[i] == 0 && Varz.enemy[i].linknum == PLType)
            {
                found_id = i;
                if (Config.galagaMode)
                    Varz.enemy[i].evalue += Varz.enemy[i].evalue;
            }
        }

        if (found_id != -1)
        {
            out_index = (byte)found_id;
            return true;
        }
        else
        {
            out_index = 0;
            return false;
        }
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
    /// 忠實移植 tyrian2.c:JE_drawEnemy(int enemyOffset)（行 178-622）。
    /// 處理 band [enemyOffset-25, enemyOffset) 的 25 個敵人：homing AI、動畫、size 多格繪製、
    /// 立方加速(curved)、移動/彈跳、score item 邊界、iced 凍結、enemyOnScreen 計數、
    /// 砲塔發射(含 galaga 射速/雙人選活玩家瞄準/特殊磁鐵飛彈 251-255)、Enemy Launch Routine。
    /// </summary>
    public static void JE_drawEnemy(int enemyOffset)
    {
        var player = Players.player;

        player[0].x -= 25;

        for (int i = enemyOffset - 25; i < enemyOffset; i++)
        {
            if (Varz.enemyAvail[i] != 1)
            {
                Varz.enemy[i].mapoffset = Backgrnd.tempMapXOfs;

                if (Varz.enemy[i].xaccel != 0 && (uint)Varz.enemy[i].xaccel - 89u > MtRand.mt_rand() % 11)
                {
                    if (player[0].x > Varz.enemy[i].ex)
                    {
                        if (Varz.enemy[i].exc < Varz.enemy[i].xaccel - 89)
                            Varz.enemy[i].exc++;
                    }
                    else
                    {
                        if (Varz.enemy[i].exc >= 0 || -Varz.enemy[i].exc < Varz.enemy[i].xaccel - 89)
                            Varz.enemy[i].exc--;
                    }
                }

                if (Varz.enemy[i].yaccel != 0 && (uint)Varz.enemy[i].yaccel - 89u > MtRand.mt_rand() % 11)
                {
                    if (player[0].y > Varz.enemy[i].ey)
                    {
                        if (Varz.enemy[i].eyc < Varz.enemy[i].yaccel - 89)
                            Varz.enemy[i].eyc++;
                    }
                    else
                    {
                        if (Varz.enemy[i].eyc >= 0 || -Varz.enemy[i].eyc < Varz.enemy[i].yaccel - 89)
                            Varz.enemy[i].eyc--;
                    }
                }

                if (Varz.enemy[i].ex + Backgrnd.tempMapXOfs > -29 && Varz.enemy[i].ex + Backgrnd.tempMapXOfs < 300)
                {
                    if (Varz.enemy[i].aniactive == 1)
                    {
                        Varz.enemy[i].enemycycle++;

                        if (Varz.enemy[i].enemycycle == Varz.enemy[i].animax)
                            Varz.enemy[i].aniactive = Varz.enemy[i].aniwhenfire;
                        else if (Varz.enemy[i].enemycycle > Varz.enemy[i].ani)
                            Varz.enemy[i].enemycycle = Varz.enemy[i].animin;
                    }

                    if (Varz.enemy[i].egr[Varz.enemy[i].enemycycle - 1] == 999)
                        goto enemy_gone;

                    if (Varz.enemy[i].size == 1) // 2x2 enemy
                    {
                        if (Varz.enemy[i].ey > -13)
                        {
                            blit_enemy(i, -6, -7, 0);
                            blit_enemy(i,  6, -7, 1);
                        }
                        if (Varz.enemy[i].ey > -26 && Varz.enemy[i].ey < 182)
                        {
                            blit_enemy(i, -6,  7, 19);
                            blit_enemy(i,  6,  7, 20);
                        }
                    }
                    else
                    {
                        if (Varz.enemy[i].ey > -13)
                            blit_enemy(i, 0, 0, 0);
                    }

                    Varz.enemy[i].filter = 0;
                }

                if (Varz.enemy[i].excc != 0)
                {
                    if (--Varz.enemy[i].exccw <= 0)
                    {
                        if (Varz.enemy[i].exc == Varz.enemy[i].exrev)
                        {
                            Varz.enemy[i].excc = (sbyte)-Varz.enemy[i].excc;
                            Varz.enemy[i].exrev = (sbyte)-Varz.enemy[i].exrev;
                            Varz.enemy[i].exccadd = (short)-Varz.enemy[i].exccadd;
                        }
                        else
                        {
                            Varz.enemy[i].exc += (sbyte)Varz.enemy[i].exccadd;
                            Varz.enemy[i].exccw = (sbyte)Varz.enemy[i].exccwmax;
                            if (Varz.enemy[i].exc == Varz.enemy[i].exrev)
                            {
                                Varz.enemy[i].excc = (sbyte)-Varz.enemy[i].excc;
                                Varz.enemy[i].exrev = (sbyte)-Varz.enemy[i].exrev;
                                Varz.enemy[i].exccadd = (short)-Varz.enemy[i].exccadd;
                            }
                        }
                    }
                }

                if (Varz.enemy[i].eycc != 0)
                {
                    if (--Varz.enemy[i].eyccw <= 0)
                    {
                        if (Varz.enemy[i].eyc == Varz.enemy[i].eyrev)
                        {
                            Varz.enemy[i].eycc = (sbyte)-Varz.enemy[i].eycc;
                            Varz.enemy[i].eyrev = (sbyte)-Varz.enemy[i].eyrev;
                            Varz.enemy[i].eyccadd = (short)-Varz.enemy[i].eyccadd;
                        }
                        else
                        {
                            Varz.enemy[i].eyc += (sbyte)Varz.enemy[i].eyccadd;
                            Varz.enemy[i].eyccw = (sbyte)Varz.enemy[i].eyccwmax;
                            if (Varz.enemy[i].eyc == Varz.enemy[i].eyrev)
                            {
                                Varz.enemy[i].eycc = (sbyte)-Varz.enemy[i].eycc;
                                Varz.enemy[i].eyrev = (sbyte)-Varz.enemy[i].eyrev;
                                Varz.enemy[i].eyccadd = (short)-Varz.enemy[i].eyccadd;
                            }
                        }
                    }
                }

                Varz.enemy[i].ey += Varz.enemy[i].fixedmovey;

                Varz.enemy[i].ex += Varz.enemy[i].exc;
                if (Varz.enemy[i].ex < -80 || Varz.enemy[i].ex > 340)
                    goto enemy_gone;

                Varz.enemy[i].ey += Varz.enemy[i].eyc;
                if (Varz.enemy[i].ey < -112 || Varz.enemy[i].ey > 190)
                    goto enemy_gone;

                goto enemy_still_exists;

enemy_gone:
                /* enemy[i].egr[10] &= 0x00ff; <MXD> madness? */
                Varz.enemyAvail[i] = 1;
                goto draw_enemy_end;

enemy_still_exists:

                /*X bounce*/
                if (Varz.enemy[i].ex <= Varz.enemy[i].xminbounce || Varz.enemy[i].ex >= Varz.enemy[i].xmaxbounce)
                    Varz.enemy[i].exc = (sbyte)-Varz.enemy[i].exc;

                /*Y bounce*/
                if (Varz.enemy[i].ey <= Varz.enemy[i].yminbounce || Varz.enemy[i].ey >= Varz.enemy[i].ymaxbounce)
                    Varz.enemy[i].eyc = (sbyte)-Varz.enemy[i].eyc;

                /* Evalue != 0 - score item at boundary */
                if (Varz.enemy[i].scoreitem)
                {
                    if (Varz.enemy[i].ex < -5)
                        Varz.enemy[i].ex++;
                    if (Varz.enemy[i].ex > 245)
                        Varz.enemy[i].ex--;
                }

                Varz.enemy[i].ey += (short)Backgrnd.tempBackMove;

                if (Varz.enemy[i].ex <= -24 || Varz.enemy[i].ex >= 296)
                    goto draw_enemy_end;

                int tempX = Varz.enemy[i].ex;
                int tempY = Varz.enemy[i].ey;

                Varz.temp = (byte)Varz.enemy[i].enemytype;

                /* Enemy Shots */
                if (Varz.enemy[i].edamaged)
                    goto draw_enemy_end;

                enemyOnScreen++;

                if (Varz.enemy[i].iced != 0)
                {
                    Varz.enemy[i].iced--;
                    if (Varz.enemy[i].enemyground)
                    {
                        Varz.enemy[i].filter = 0x09;
                    }
                    goto draw_enemy_end;
                }

                for (int j = 3; j > 0; j--)
                {
                    if (Varz.enemy[i].freq[j-1] != 0)
                    {
                        Varz.temp3 = Varz.enemy[i].tur[j-1];

                        if (--Varz.enemy[i].eshotwait[j-1] == 0 && Varz.temp3 != 0)
                        {
                            Varz.enemy[i].eshotwait[j-1] = Varz.enemy[i].freq[j-1];
                            if (Config.difficultyLevel > Config.DIFFICULTY_NORMAL)
                            {
                                Varz.enemy[i].eshotwait[j-1] = (byte)((Varz.enemy[i].eshotwait[j-1] / 2) + 1);
                                if (Config.difficultyLevel > Config.DIFFICULTY_MANIACAL)
                                    Varz.enemy[i].eshotwait[j-1] = (byte)((Varz.enemy[i].eshotwait[j-1] / 2) + 1);
                            }

                            if (Config.galagaMode && (Varz.enemy[i].eyc == 0 || (MtRand.mt_rand() % 400) >= galagaShotFreq))
                                goto draw_enemy_end;

                            switch (Varz.temp3)
                            {
                            case 252: /* Savara Boss DualMissile */
                                if (Varz.enemy[i].ey > 20)
                                {
                                    Varz.JE_setupExplosion(tempX - 8 + Backgrnd.tempMapXOfs, tempY - 20 - Backgrnd.backMove * 8, -2, 6, false, false);
                                    Varz.JE_setupExplosion(tempX + 4 + Backgrnd.tempMapXOfs, tempY - 20 - Backgrnd.backMove * 8, -2, 6, false, false);
                                }
                                break;
                            case 251: /* Suck-O-Magnet */
                                {
                                    int attraction = 4 - (Math.Abs(player[0].x - tempX) + Math.Abs(player[0].y - tempY)) / 100;
                                    if (attraction > 0)
                                        player[0].x_velocity += (player[0].x > tempX) ? -attraction : attraction;
                                }
                                break;
                            case 253: /* Left ShortRange Magnet */
                                if (Math.Abs(player[0].x + 25 - 14 - tempX) < 24 && Math.Abs(player[0].y - tempY) < 28)
                                {
                                    player[0].x_velocity += 2;
                                }
                                if (Config.twoPlayerMode &&
                                   (Math.Abs(player[1].x - 14 - tempX) < 24 && Math.Abs(player[1].y - tempY) < 28))
                                {
                                    player[1].x_velocity += 2;
                                }
                                break;
                            case 254: /* Left ShortRange Magnet */
                                if (Math.Abs(player[0].x + 25 - 14 - tempX) < 24 && Math.Abs(player[0].y - tempY) < 28)
                                {
                                    player[0].x_velocity -= 2;
                                }
                                if (Config.twoPlayerMode &&
                                   (Math.Abs(player[1].x - 14 - tempX) < 24 && Math.Abs(player[1].y - tempY) < 28))
                                {
                                    player[1].x_velocity -= 2;
                                }
                                break;
                            case 255: /* Magneto RePulse!! */
                                if (Config.difficultyLevel != Config.DIFFICULTY_EASY) /*DIF*/
                                {
                                    if (j == 3)
                                    {
                                        Varz.enemy[i].filter = 0x70;
                                    }
                                    else
                                    {
                                        int repulsion = 4 - (Math.Abs(player[0].x - tempX) + Math.Abs(player[0].y - tempY)) / 20;
                                        if (repulsion > 0)
                                            player[0].x_velocity += (player[0].x > tempX) ? repulsion : -repulsion;
                                    }
                                }
                                break;
                            default:
                            /*Rot*/
                                for (int tempCount = Episodes.weapons[Varz.temp3].multi; tempCount > 0; tempCount--)
                                {
                                    for (Varz.b = 0; Varz.b < VarzConst.ENEMY_SHOT_MAX; Varz.b++)
                                    {
                                        if (Varz.enemyShotAvail[Varz.b])
                                            break;
                                    }
                                    if (Varz.b == VarzConst.ENEMY_SHOT_MAX)
                                        goto draw_enemy_end;

                                    Varz.enemyShotAvail[Varz.b] = !Varz.enemyShotAvail[Varz.b];

                                    if (Episodes.weapons[Varz.temp3].sound > 0)
                                    {
                                        do
                                        {
                                            Varz.temp = (byte)(MtRand.mt_rand() % 8);
                                        } while (Varz.temp == 3);
                                        Varz.soundQueue[Varz.temp] = Episodes.weapons[Varz.temp3].sound;
                                    }

                                    if (Varz.enemy[i].aniactive == 2)
                                        Varz.enemy[i].aniactive = 1;

                                    if (++Varz.enemy[i].eshotmultipos[j-1] > Episodes.weapons[Varz.temp3].max)
                                        Varz.enemy[i].eshotmultipos[j-1] = 1;

                                    int tempPos = Varz.enemy[i].eshotmultipos[j-1] - 1;

                                    if (j == 1)
                                        Varz.temp2 = 4;

                                    Varz.enemyShot[Varz.b].sx = (short)(tempX + Episodes.weapons[Varz.temp3].bx[tempPos] + Backgrnd.tempMapXOfs);
                                    Varz.enemyShot[Varz.b].sy = (short)(tempY + Episodes.weapons[Varz.temp3].by[tempPos]);
                                    Varz.enemyShot[Varz.b].sdmg = Episodes.weapons[Varz.temp3].attack[tempPos];
                                    Varz.enemyShot[Varz.b].tx = Episodes.weapons[Varz.temp3].tx;
                                    Varz.enemyShot[Varz.b].ty = Episodes.weapons[Varz.temp3].ty;
                                    Varz.enemyShot[Varz.b].duration = Episodes.weapons[Varz.temp3].del[tempPos];
                                    Varz.enemyShot[Varz.b].animate = 0;
                                    Varz.enemyShot[Varz.b].animax = Episodes.weapons[Varz.temp3].weapani;

                                    Varz.enemyShot[Varz.b].sgr = Episodes.weapons[Varz.temp3].sg[tempPos];
                                    switch (j)
                                    {
                                    case 1:
                                        Varz.enemyShot[Varz.b].syc = Episodes.weapons[Varz.temp3].acceleration;
                                        Varz.enemyShot[Varz.b].sxc = Episodes.weapons[Varz.temp3].accelerationx;

                                        Varz.enemyShot[Varz.b].sxm = Episodes.weapons[Varz.temp3].sx[tempPos];
                                        Varz.enemyShot[Varz.b].sym = Episodes.weapons[Varz.temp3].sy[tempPos];
                                        break;
                                    case 3:
                                        Varz.enemyShot[Varz.b].sxc = (sbyte)-Episodes.weapons[Varz.temp3].acceleration;
                                        Varz.enemyShot[Varz.b].syc = Episodes.weapons[Varz.temp3].accelerationx;

                                        Varz.enemyShot[Varz.b].sxm = (short)-Episodes.weapons[Varz.temp3].sy[tempPos];
                                        Varz.enemyShot[Varz.b].sym = (short)-Episodes.weapons[Varz.temp3].sx[tempPos];
                                        break;
                                    case 2:
                                        Varz.enemyShot[Varz.b].sxc = Episodes.weapons[Varz.temp3].acceleration;
                                        Varz.enemyShot[Varz.b].syc = (sbyte)-Episodes.weapons[Varz.temp3].acceleration;

                                        Varz.enemyShot[Varz.b].sxm = Episodes.weapons[Varz.temp3].sy[tempPos];
                                        Varz.enemyShot[Varz.b].sym = (short)-Episodes.weapons[Varz.temp3].sx[tempPos];
                                        break;
                                    }

                                    if (Episodes.weapons[Varz.temp3].aim > 0)
                                    {
                                        int aim = Episodes.weapons[Varz.temp3].aim;

                                        /*DIF*/
                                        if (Config.difficultyLevel > Config.DIFFICULTY_NORMAL)
                                            aim += Config.difficultyLevel - 2;

                                        int targetX = player[0].x;
                                        int targetY = player[0].y;

                                        if (Config.twoPlayerMode)
                                        {
                                            // fire at live player(s)
                                            if (player[0].is_alive && !player[1].is_alive)
                                                Varz.temp = 0;
                                            else if (player[1].is_alive && !player[0].is_alive)
                                                Varz.temp = 1;
                                            else
                                                Varz.temp = (byte)(MtRand.mt_rand() % 2);

                                            if (Varz.temp == 1)
                                            {
                                                targetX = player[1].x - 25;
                                                targetY = player[1].y;
                                            }
                                        }

                                        int aimX = (targetX + 25) - tempX - Backgrnd.tempMapXOfs - 4;
                                        if (aimX == 0)
                                            aimX = 1;
                                        int aimY = targetY - tempY;
                                        if (aimY == 0)
                                            aimY = 1;
                                        int maxMagAim = Math.Max(Math.Abs(aimX), Math.Abs(aimY));
                                        Varz.enemyShot[Varz.b].sxm = (short)MathF.Round((float)aimX / maxMagAim * aim);
                                        Varz.enemyShot[Varz.b].sym = (short)MathF.Round((float)aimY / maxMagAim * aim);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                /* Enemy Launch Routine */
                if (Varz.enemy[i].launchfreq != 0)
                {
                    if (--Varz.enemy[i].launchwait == 0)
                    {
                        Varz.enemy[i].launchwait = Varz.enemy[i].launchfreq;

                        if (Varz.enemy[i].launchspecial != 0)
                        {
                            /*Type  1 : Must be inline with player*/
                            if (Math.Abs(Varz.enemy[i].ey - player[0].y) > 5)
                                goto draw_enemy_end;
                        }

                        if (Varz.enemy[i].aniactive == 2)
                        {
                            Varz.enemy[i].aniactive = 1;
                        }

                        if (Varz.enemy[i].launchtype == 0)
                            goto draw_enemy_end;

                        Varz.tempW = Varz.enemy[i].launchtype;
                        Varz.b = JE_newEnemy(enemyOffset == 50 ? 75 : enemyOffset - 25, Varz.tempW, 0);

                        /*Launch Enemy Placement*/
                        if (Varz.b > 0)
                        {
                            int e = Varz.b - 1;

                            Varz.enemy[e].ex = (short)tempX;
                            Varz.enemy[e].ey = (short)(tempY + Episodes.enemyDat[Varz.enemy[e].enemytype].startyc);
                            if (Varz.enemy[e].size == 0)
                                Varz.enemy[e].ey -= 7;

                            if (Varz.enemy[e].launchtype > 0 && Varz.enemy[e].launchfreq == 0)
                            {
                                if (Varz.enemy[e].launchtype > 90)
                                {
                                    int lt = Varz.enemy[e].launchtype;
                                    Varz.enemy[e].ex = (short)(Varz.enemy[e].ex + ((int)(MtRand.mt_rand() % (uint)((lt - 90) * 4)) - (lt - 90) * 2));
                                }
                                else
                                {
                                    int aimX = (player[0].x + 25) - tempX - Backgrnd.tempMapXOfs - 4;
                                    if (aimX == 0)
                                        aimX = 1;
                                    int aimY = player[0].y - tempY;
                                    if (aimY == 0)
                                        aimY = 1;
                                    int maxMagAim = Math.Max(Math.Abs(aimX), Math.Abs(aimY));
                                    Varz.enemy[e].exc = (sbyte)MathF.Round((float)aimX / maxMagAim * Varz.enemy[e].launchtype);
                                    Varz.enemy[e].eyc = (sbyte)MathF.Round((float)aimY / maxMagAim * Varz.enemy[e].launchtype);
                                }
                            }

                            do
                            {
                                Varz.temp = (byte)(MtRand.mt_rand() % 8);
                            } while (Varz.temp == 3);
                            Varz.soundQueue[Varz.temp] = Varz.randomEnemyLaunchSounds[(MtRand.mt_rand() % 3)];

                            if (Varz.enemy[i].launchspecial == 1 &&
                                Varz.enemy[i].linknum < 100)
                            {
                                Varz.enemy[e].linknum = Varz.enemy[i].linknum;
                            }
                        }
                    }
                }
            }
draw_enemy_end:
            ;
        }

        player[0].x += 25;
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
