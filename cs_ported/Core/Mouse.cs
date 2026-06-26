namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/mouse.c —— 滑鼠游標 sprite 的繪製/抓背景/還原。
/// </summary>
internal static unsafe class Mouse
{
    public static bool has_mouse = true;
    public static bool mouse_has_three_buttons = true;

    public static bool mouseInactive = true;
    public static byte mouseCursor;

    private static ushort mouseGrabX, mouseGrabY;
    private static readonly byte[] mouseGrabShape = new byte[24 * 28];

    private struct MousePointerSpriteInfo
    {
        public ushort index;
        public byte x, y, w, h, fx, fy;
        public MousePointerSpriteInfo(ushort index, byte x, byte y, byte w, byte h, byte fx, byte fy)
        { this.index = index; this.x = x; this.y = y; this.w = w; this.h = h; this.fx = fx; this.fy = fy; }
    }

    private static readonly MousePointerSpriteInfo[] mousePointerSprites =
    {
        new(273, 0, 0, 11, 16, 0, 0),
        new(275, 0, 0, 21, 16, 10, 8),
        new(277, 0, 0, 21, 16, 10, 7),
        new(279, 0, 0, 16, 21, 8, 10),
        new(281, 8, 0, 16, 21, 7, 10),
    };

    private static void JE_drawShapeTypeOne(int x, int y, byte[] shape)
    {
        var screen = Video.VGAScreen;
        byte* s = screen.pixels + y * screen.pitch + x;
        byte* s_limit = screen.pixels + screen.h * screen.pitch;

        int pi = 0;
        for (int yloop = 0; yloop < 28; yloop++)
        {
            for (int xloop = 0; xloop < 24; xloop++)
            {
                if (s >= s_limit) return;
                *s = shape[pi];
                s++; pi++;
            }
            s -= 24;
            s += screen.pitch;
        }
    }

    private static void JE_grabShapeTypeOne(int x, int y, byte[] shape)
    {
        var screen = Video.VGAScreen;
        byte* s = screen.pixels + y * screen.pitch + x;
        byte* s_limit = screen.pixels + screen.h * screen.pitch;

        int pi = 0;
        for (int yloop = 0; yloop < 28; yloop++)
        {
            for (int xloop = 0; xloop < 24; xloop++)
            {
                if (s >= s_limit) return;
                shape[pi] = *s;
                s++; pi++;
            }
            s -= 24;
            s += screen.pitch;
        }
    }

    public static void JE_mouseStart()
    {
        if (!has_mouse) return;

        ref MousePointerSpriteInfo spriteInfo = ref mousePointerSprites[mouseCursor];

        mouseGrabX = (ushort)(Opentyr.MIN(Opentyr.MAX(spriteInfo.fx, Keyboard.mouseX), 320 - (spriteInfo.w - spriteInfo.fx)) - spriteInfo.fx);
        mouseGrabY = (ushort)(Opentyr.MIN(Opentyr.MAX(spriteInfo.fy, Keyboard.mouseY), 200 - (spriteInfo.h - spriteInfo.fy)) - spriteInfo.fy);

        JE_grabShapeTypeOne(mouseGrabX, mouseGrabY, mouseGrabShape);

        if (!mouseInactive)
        {
            int x = Keyboard.mouseX - spriteInfo.x - spriteInfo.fx;
            int y = Keyboard.mouseY - spriteInfo.y - spriteInfo.fy;
            Sprites.blit_sprite2x2_clip(Video.VGAScreen, x, y, Sprites.shopSpriteSheet, spriteInfo.index);
        }
    }

    public static void JE_mouseStartFilter(byte filter)
    {
        if (!has_mouse) return;

        ref MousePointerSpriteInfo spriteInfo = ref mousePointerSprites[mouseCursor];

        mouseGrabX = (ushort)(Opentyr.MIN(Opentyr.MAX(spriteInfo.fx, Keyboard.mouseX), 320 - (spriteInfo.w - spriteInfo.fx)) - spriteInfo.fx);
        mouseGrabY = (ushort)(Opentyr.MIN(Opentyr.MAX(spriteInfo.fy, Keyboard.mouseY), 200 - (spriteInfo.h - spriteInfo.fy)) - spriteInfo.fy);

        JE_grabShapeTypeOne(mouseGrabX, mouseGrabY, mouseGrabShape);

        if (!mouseInactive)
        {
            int x = Keyboard.mouseX - spriteInfo.x - spriteInfo.fx;
            int y = Keyboard.mouseY - spriteInfo.y - spriteInfo.fy;
            Sprites.blit_sprite2x2_filter_clip(Video.VGAScreen, x, y, Sprites.shopSpriteSheet, spriteInfo.index, filter);
        }
    }

    public static void JE_mouseReplace()
    {
        if (!has_mouse) return;
        JE_drawShapeTypeOne(mouseGrabX, mouseGrabY, mouseGrabShape);
    }

    /// <summary>對應 keyboard.c 透過 mouse 清除輸入（轉呼叫 Keyboard）。</summary>
    public static void mouseClearInput() => Keyboard.mouseClearInput();
}
