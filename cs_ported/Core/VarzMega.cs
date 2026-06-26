namespace AprCSTyrian.Core;

/// <summary>
/// 對應 varz.h 的 megaData 地圖結構（JE_MegaDataType1/2/3）。
/// C 原版 mainmap 是 byte* 的二維陣列，指向同結構內 shapes[].sh（JE_DanCShape，672 bytes）。
/// 此處將 shape 資料放在「非託管連續區塊」(stable pointer)，mainmap 以 nint(byte*) 陣列表示。
/// nothing[3]/fill 為未使用 padding（C 僅引用 .sh），故每個 shape 只存 672 bytes。
/// </summary>
internal sealed unsafe class JE_MegaData
{
    public const int DAN_C_SHAPE = 24 * 28; // 672

    public readonly int rows, cols, shapeCount;
    public readonly nint[] mainmap;          // [rows*cols] of byte* (null = 無 shape)
    public readonly byte[] fill;             // [shapeCount] 每個 shape 是否全不透明（megaData2/3 用）
    public byte* shapesData;                 // 非託管：shapeCount * 672
#pragma warning disable CS0649 // 由尚未移植的 JE_loadMap 指派
    public byte tempdat;
#pragma warning restore CS0649

    public JE_MegaData(int rows, int cols, int shapeCount)
    {
        this.rows = rows;
        this.cols = cols;
        this.shapeCount = shapeCount;
        mainmap = new nint[rows * cols];
        fill = new byte[shapeCount];
    }

    public void Alloc()
    {
        if (shapesData == null)
            shapesData = (byte*)CMem.calloc((nuint)shapeCount, (nuint)DAN_C_SHAPE);
    }

    public void Free()
    {
        if (shapesData != null)
        {
            CMem.free(shapesData);
            shapesData = null;
        }
        Array.Clear(mainmap);
    }

    /// <summary>第 k 個 shape 的 sh 資料指標（672 bytes）。</summary>
    public byte* Shape(int k) => shapesData + k * DAN_C_SHAPE;

    public byte* Map(int x, int y) => (byte*)mainmap[x * cols + y];
    public void SetMap(int x, int y, byte* p) => mainmap[x * cols + y] = (nint)p;
}

internal static unsafe partial class Varz
{
    // 對應 varz.c 的 megaData1/2/3（背景三層地圖）
    public static readonly JE_MegaData megaData1 = new(300, 14, 72);
    public static readonly JE_MegaData megaData2 = new(600, 14, 71);
    public static readonly JE_MegaData megaData3 = new(600, 15, 70);

    public static void allocMegaData()
    {
        megaData1.Alloc();
        megaData2.Alloc();
        megaData3.Alloc();
    }

    public static void freeMegaData()
    {
        megaData1.Free();
        megaData2.Free();
        megaData3.Free();
    }
}
