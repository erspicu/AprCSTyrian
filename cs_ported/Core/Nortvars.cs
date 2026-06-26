namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/nortvars.c —— 護盾/裝甲 bar 繪製 helper。
/// </summary>
internal static class Nortvars
{
    public static void JE_dBar3(SDL_Surface surface, int x, int y, int num, int col)
    {
        int zWait = 2;
        col += 2;

        for (int z = 0; z <= num; z++)
        {
            Vga256d.JE_rectangle(surface, x, y - 1, x + 8, y, col); /* <MXD> SEGa000 */
            if (zWait > 0)
            {
                zWait--;
            }
            else
            {
                col++;
                zWait = 1;
            }
            y -= 2;
        }
    }

    public static void JE_barDrawShadow(SDL_Surface surface, int x, int y, int res, int col, int amt, int xsize, int ysize)
    {
        xsize--;
        ysize--;

        for (int z = 1; z <= amt / res; z++)
        {
            Vga256d.JE_barShade(surface, x + 2, y + 2, x + xsize + 2, y + ysize + 2);
            Vga256d.fill_rectangle_xy(surface, x, y, x + xsize, y + ysize, (byte)(col + 12));
            Vga256d.fill_rectangle_xy(surface, x, y, x + xsize, y, (byte)(col + 13));
            Vga256d.JE_pix(surface, x, y, (byte)(col + 15));
            Vga256d.fill_rectangle_xy(surface, x, y + ysize, x + xsize, y + ysize, (byte)(col + 11));
            x += xsize + 2;
        }

        amt %= res;
        if (amt > 0)
        {
            Vga256d.JE_barShade(surface, x + 2, y + 2, x + xsize + 2, y + ysize + 2);
            Vga256d.fill_rectangle_xy(surface, x, y, x + xsize, y + ysize, (byte)(col + (12 / res * amt)));
        }
    }
}
