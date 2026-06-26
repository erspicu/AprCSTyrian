namespace AprCSTyrian.Core;

/// <summary>移植 sources/src/varz.c 的 JE_playerDamage —— 玩家受傷（護盾→裝甲→死亡）。</summary>
internal static unsafe partial class Varz
{
    public static byte JE_playerDamage(byte temp, int pi)
    {
        var player = Players.player;
        int playerDamage = 0;
        soundQueue[7] = (byte)Sndmast.S_SHIELD_HIT;

        if (player[pi].shield < temp)
        {
            playerDamage = temp;
            temp -= (byte)player[pi].shield;
            player[pi].shield = 0;

            if (temp > 0)
            {
                // 穿透護盾 → 裝甲
                if (player[pi].armor < temp)
                {
                    temp -= (byte)player[pi].armor;
                    player[pi].armor = 0;

                    if (player[pi].is_alive && !Config.youAreCheating)
                    {
                        levelTimer = false;
                        player[pi].is_alive = false;
                        player[pi].exploding_ticks = 60;
                        levelEnd = 40;
                        Nortsong.tempVolume = Nortsong.tyrMusicVolume;
                        soundQueue[1] = (byte)Sndmast.S_EXPLOSION_22;
                    }
                }
                else
                {
                    player[pi].armor -= temp;
                    soundQueue[7] = (byte)Sndmast.S_HULL_HIT;
                }
            }
        }
        else
        {
            player[pi].shield -= temp;

            int x = player[pi].x, y = player[pi].y;
            bool fp = !Config.twoPlayerMode;
            JE_setupExplosion(x - 17, y - 12, 0, 14, false, fp);
            JE_setupExplosion(x - 5, y - 12, 0, 15, false, fp);
            JE_setupExplosion(x + 7, y - 12, 0, 16, false, fp);
            JE_setupExplosion(x + 19, y - 12, 0, 17, false, fp);
            JE_setupExplosion(x - 17, y + 2, 0, 18, false, fp);
            JE_setupExplosion(x + 19, y + 2, 0, 19, false, fp);
            JE_setupExplosion(x - 17, y + 16, 0, 20, false, fp);
            JE_setupExplosion(x - 5, y + 16, 0, 21, false, fp);
            JE_setupExplosion(x + 7, y + 16, 0, 22, false, fp);
        }

        JE_wipeShieldArmorBars();
        JE_drawShield();
        JE_drawArmor();

        return (byte)playerDamage;
    }
}
