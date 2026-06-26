using System.Runtime.CompilerServices;

// 玩家狀態欄位由尚未移植的遊戲邏輯（mainint/tyrian2/shots…）指派；此處為型別/全域定義。
#pragma warning disable CS0649

namespace AprCSTyrian.Core;

/// <summary>對應 player.h:PlayerItems.weapon 元素。</summary>
internal struct PlayerWeapon
{
    public byte id;
    public byte power;
}

[InlineArray(2)]
internal struct PlayerWeapon2 { private PlayerWeapon _e; }

[InlineArray(2)]
internal struct Byte2 { private byte _e; }

/// <summary>對應 player.h:PlayerItems（玩家裝備，value 語意以供 items/last_items 複製與存檔）。</summary>
internal struct PlayerItems
{
    public byte ship;
    public byte generator;
    public byte shield;
    public PlayerWeapon2 weapon;   // [2]
    public Byte2 sidekick;         // [2]
    public byte special;

    // Dragonwing only
    public byte sidekick_series;
    public byte sidekick_level;

    // Single-player only
    public byte super_arcade_mode; // stored as an item for compatibility
}

/// <summary>對應 player.h:Player.sidekick 子結構。</summary>
internal struct PlayerSidekick
{
    // calculatable
    public int ammo_max;
    public uint ammo_refill_ticks_max;
    public uint style;

    // state
    public int x, y;
    public int ammo;
    public uint ammo_refill_ticks;

    public bool animation_enabled;
    public uint animation_frame;

    public uint charge;
    public uint charge_ticks;
}

/// <summary>
/// 對應 player.h:Player。以 class（參考型別）對應 C 的 Player*（程式碼以指標傳遞並就地修改）。
/// </summary>
internal sealed unsafe class Player
{
    public uint cash;

    public PlayerItems items, last_items;

    public bool is_dragonwing; // i.e., is player 2
    // C 原為 `byte* lives = &items.weapon[p].power`（殘機別名到武器 power 欄位）。
    // C# 無法取結構欄位穩定指標，改存「別名的武器 port 索引」+ 屬性存取。
    public byte livesPort;
    public byte Lives
    {
        get => items.weapon[livesPort].power;
        set => items.weapon[livesPort].power = value;
    }

    // calculatable
    public uint shield_max;
    public uint initial_armor;
    public uint shot_hit_area_x, shot_hit_area_y;

    // state
    public bool is_alive;
    public uint invulnerable_ticks;
    public uint exploding_ticks;
    public uint shield;
    public uint armor;
    public uint weapon_mode;
    public uint superbombs;
    public uint purple_balls_needed;

    public ushort mouseX;
    public ushort mouseY;

    public int x, y;
    public readonly int[] old_x = new int[20];
    public readonly int[] old_y = new int[20];

    public int x_velocity, y_velocity;
    public uint x_friction_ticks, y_friction_ticks;

    public int delta_x_shot_move, delta_y_shot_move;

    public int last_x_shot_move, last_y_shot_move;
    public int last_x_explosion_follow, last_y_explosion_follow;

    public readonly PlayerSidekick[] sidekick = new PlayerSidekick[2];
}

/// <summary>player.h 的列舉與全域 player[2] 與輔助函式。</summary>
internal static class Players
{
    public const int FRONT_WEAPON = 0;
    public const int REAR_WEAPON = 1;

    public const int LEFT_SIDEKICK = 0;
    public const int RIGHT_SIDEKICK = 1;

    public static readonly Player[] player = { new Player(), new Player() };

    public static bool all_players_dead() =>
        !player[0].is_alive && (!Config.twoPlayerMode || !player[1].is_alive);

    public static bool all_players_alive() =>
        player[0].is_alive && (!Config.twoPlayerMode || player[1].is_alive);

    private static readonly uint[] purple_balls_required = { 1, 1, 2, 4, 8, 12, 16, 20, 25, 30, 40, 50 };

    /// <summary>對應 player.c:calc_purple_balls_needed。</summary>
    public static void calc_purple_balls_needed(Player this_player)
    {
        this_player.purple_balls_needed = purple_balls_required[this_player.Lives];
    }

    /// <summary>對應 player.c:power_up_weapon。</summary>
    public static bool power_up_weapon(Player this_player, uint port)
    {
        bool can_power_up = this_player.items.weapon[(int)port].id != 0 &&  // not None
                            this_player.items.weapon[(int)port].power < 11; // not at max power
        if (can_power_up)
        {
            ++this_player.items.weapon[(int)port].power;
            Config.shotMultiPos[(int)port] = 0; // TODO: should be part of Player structure

            calc_purple_balls_needed(this_player);
        }
        else  // cash consolation prize
        {
            this_player.cash += 1000;
        }

        return can_power_up;
    }

    /// <summary>對應 player.c:handle_got_purple_ball。</summary>
    public static void handle_got_purple_ball(Player this_player)
    {
        if (this_player.purple_balls_needed > 1)
            --this_player.purple_balls_needed;
        else
            power_up_weapon(this_player, (uint)(this_player.is_dragonwing ? REAR_WEAPON : FRONT_WEAPON));
    }
}
