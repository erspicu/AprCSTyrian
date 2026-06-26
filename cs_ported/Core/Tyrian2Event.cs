namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c 的 JE_eventSystem —— 關卡事件分派器（依 curLoc 觸發 eventRec）。
/// **逐步移植中**：目前處理背景/星空/特殊武器等較單純事件型別；敵人生成(5/6/7…)與其餘
/// 型別待 JE_makeEnemy/敵人系統移植，先比照原版 default（略過並前進 eventLoc）。
/// </summary>
internal static unsafe partial class Tyrian2
{
    public static void JE_eventSystem()
    {
        ref JE_EventRecType ev = ref eventRec[eventLoc - 1];

        switch (ev.eventtype)
        {
            case 1:
                Backgrnd.starfield_speed = ev.eventdat;
                break;

            case 2:
                Backgrnd.map1YDelay = 1; Backgrnd.map1YDelayMax = 1;
                Backgrnd.map2YDelay = 1; Backgrnd.map2YDelayMax = 1;
                Backgrnd.backMove = (ushort)ev.eventdat;
                Backgrnd.backMove2 = (ushort)ev.eventdat2;
                explodeMove = Backgrnd.backMove2 > 0 ? Backgrnd.backMove2 : Backgrnd.backMove;
                Backgrnd.backMove3 = (ushort)ev.eventdat3;
                if (Backgrnd.backMove > 0)
                    stopBackgroundNum = 0;
                break;

            case 3:
                Backgrnd.backMove = 1; Backgrnd.map1YDelay = 3; Backgrnd.map1YDelayMax = 3;
                Backgrnd.backMove2 = 1; Backgrnd.map2YDelay = 2; Backgrnd.map2YDelayMax = 2;
                Backgrnd.backMove3 = 1;
                break;

            case 4:
                stopBackgrounds = true;
                switch (ev.eventdat)
                {
                    case 0:
                    case 1: stopBackgroundNum = 1; break;
                    case 2: stopBackgroundNum = 2; break;
                    case 3: stopBackgroundNum = 3; break;
                }
                break;

            case 8:
                Config.starActive = false;
                break;

            case 82: // Give SPECIAL WEAPON
                Players.player[0].items.special = (byte)ev.eventdat;
                Config.shotMultiPos[Config.SHOT_SPECIAL] = 0;
                Config.shotRepeat[Config.SHOT_SPECIAL] = 0;
                Config.shotMultiPos[Config.SHOT_SPECIAL2] = 0;
                Config.shotRepeat[Config.SHOT_SPECIAL2] = 0;
                break;

            default:
                // TODO: 其餘事件型別（敵人生成 5/6/7、地面敵人、boss、背景 wrap…）待敵人系統移植
                break;
        }

        eventLoc++;
    }
}
