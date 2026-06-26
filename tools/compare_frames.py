#!/usr/bin/env python3
"""逐幀比對原版 vs C# 移植版截圖，找出畫面分歧（= 尚未正確移植之處）。

用法:
    python tools/compare_frames.py [orig_dir] [cs_dir] [--out diff_out] [--top N]

預設（相對專案根 AprCSTyrian/，與 cwd 無關）:
    orig_dir = temp/orig   (原版 run_keylog.bat 產出)
    cs_dir   = temp/cs     (C# run_replay.bat 產出)
    out      = temp/diff

對每個兩邊都有的 frame_<N>.bmp 計算差異百分比（不同像素 / 總像素），
列出最分歧的前 N 幀；並把這些幀輸出 orig|cs|diff 並排圖到 diff_out/。
需要 Pillow：  pip install pillow
"""
import os, re, sys, argparse
from PIL import Image, ImageChops

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # tools/ 的上一層 = 專案根

def load_frames(d):
    out = {}
    if not os.path.isdir(d): return out
    for fn in os.listdir(d):
        m = re.match(r'frame_(\d+)\.bmp$', fn)
        if m: out[int(m.group(1))] = os.path.join(d, fn)
    return out

def diff_pct(a, b):
    ia = Image.open(a).convert('RGB')
    ib = Image.open(b).convert('RGB')
    if ia.size != ib.size:
        return 100.0, ia, ib, None
    dc = ImageChops.difference(ia, ib)
    # 不同像素數（任一通道差 > 8 視為不同，容忍微小色差）。用 histogram 計數，免逐像素迴圈。
    bands = dc.split()
    mask = None
    for band in bands:
        m = band.point(lambda v: 255 if v > 8 else 0)
        mask = m if mask is None else ImageChops.lighter(mask, m)
    diff_pixels = mask.histogram()[255]  # bin 255 = 不同像素數
    total = ia.size[0] * ia.size[1]
    return 100.0 * diff_pixels / total, ia, ib, dc

def side_by_side(ia, ib, dc, path):
    w, h = ia.size
    canvas = Image.new('RGB', (w*3 + 8, h), (40, 40, 40))
    canvas.paste(ia, (0, 0))
    canvas.paste(ib, (w+4, 0))
    if dc is not None:
        canvas.paste(dc, (w*2+8, 0))
    canvas.save(path)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('orig', nargs='?', default=os.path.join(ROOT, 'temp', 'orig'))
    ap.add_argument('cs', nargs='?', default=os.path.join(ROOT, 'temp', 'cs'))
    ap.add_argument('--out', default=os.path.join(ROOT, 'temp', 'diff'))
    ap.add_argument('--top', type=int, default=20)
    a = ap.parse_args()

    fo, fc = load_frames(a.orig), load_frames(a.cs)
    common = sorted(set(fo) & set(fc))
    only_o = sorted(set(fo) - set(fc))
    only_c = sorted(set(fc) - set(fo))

    print(f"orig frames={len(fo)}  cs frames={len(fc)}  common={len(common)}")
    if only_o: print(f"  只在原版的 frame（C# 未到達/未截）: {only_o[:15]}{'...' if len(only_o)>15 else ''}")
    if only_c: print(f"  只在 C# 的 frame: {only_c[:15]}{'...' if len(only_c)>15 else ''}")
    if not common:
        print("沒有共同 frame 可比對。請先在原版與 C# 各跑一次（同一份 keylog）。")
        return

    results = []
    for n in common:
        pct, ia, ib, dc = diff_pct(fo[n], fc[n])
        results.append((pct, n))
    results.sort(reverse=True)

    avg = sum(p for p, _ in results) / len(results)
    print(f"\n平均差異={avg:.2f}%   完全相同(<0.5%)幀數={sum(1 for p,_ in results if p<0.5)}/{len(results)}")
    print(f"\n最分歧的前 {a.top} 幀（差異% / frame）—— 這些就是移植還沒對上的地方:")
    os.makedirs(a.out, exist_ok=True)
    for pct, n in results[:a.top]:
        print(f"  {pct:6.2f}%   frame {n}")
        _, ia, ib, dc = diff_pct(fo[n], fc[n])
        side_by_side(ia, ib, dc, os.path.join(a.out, f"diff_{n:08d}_{pct:05.1f}pct.png"))
    print(f"\n並排對照圖(orig|cs|diff)已輸出到: {a.out}/")

if __name__ == '__main__':
    main()
