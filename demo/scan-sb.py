# 扫描时间面板截图里的滚动条（灰条）：宽度 + 颜色。用法：
#   python scan-sb.py sbm-tp-before.png sbm-tp-after.png ...
# 输出每个「竖条」的 x 范围、设备像素宽、CSS 宽（dsf=2）与拇指色。
import sys, pathlib
from PIL import Image

DSF = 2
ROOT = pathlib.Path(__file__).parent / 'shots'


def is_grey(p, lo=170, hi=248, sat=16):
    r, g, b = p[0], p[1], p[2]
    return lo <= max(r, g, b) <= hi and (max(r, g, b) - min(r, g, b)) <= sat


def scan(name, minrun=60):
    im = Image.open(ROOT / name).convert('RGB')
    w, h = im.size
    px = im.load()
    runs = []           # (x, runlen, y0)
    for x in range(w):
        best = cur = 0
        by = 0
        for y in range(h):
            if is_grey(px[x, y]):
                cur += 1
                if cur > best:
                    best, by = cur, y
            else:
                cur = 0
        runs.append((x, best, by - best + 1))
    bars, s = [], -1
    for x in range(w + 1):
        on = x < w and runs[x][1] >= minrun
        if on and s < 0:
            s = x
        if not on and s >= 0:
            if x - s >= 2:
                seg = runs[s:x]
                bx, blen, by0 = max(seg, key=lambda t: t[1])
                mid = by0 + blen // 2
                # 取该条中部 3 像素的平均色（避开抗锯齿边）
                cs = [px[bx, mid], px[bx, min(mid + 1, h - 1)], px[bx, max(mid - 1, 0)]]
                avg = tuple(sum(c[i] for c in cs) // len(cs) for i in range(3))
                bars.append((s, x - 1, x - s, round((x - s) / DSF, 2), avg, blen))
            s = -1
    return im.size, bars


for n in sys.argv[1:]:
    size, bars = scan(n)
    print(f'== {n}  {size[0]}x{size[1]} (dsf={DSF})')
    if not bars:
        print('   （无灰色竖条）')
    for x0, x1, dw, cw, avg, blen in bars:
        print(f'   x={x0:>3}..{x1:<3} 设备宽={dw}px  CSS宽={cw}px  拇指长={blen // DSF}px  色={avg}')
