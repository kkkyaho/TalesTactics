"""Read-only alpha measurement for supplemental animation sheets; never alters PNG pixels."""
from pathlib import Path
import csv
import json
import numpy as np
from PIL import Image
from MeasurePixelCampaign import bands, SCALE
from MeasureCharacterPoses import contour

root = Path('Assets/TalesTactics/Art/PixelCampaign')
layouts, outlines, reports, failures = [], [], [], []
for path in sorted(root.glob('*-polish.png')):
    alpha = np.array(Image.open(path).convert('RGBA'))[:, :, 3]
    mask = alpha > 128
    height, width = mask.shape
    assert mask.mean() < .8, f'{path}: opaque background'
    ys = bands(mask.sum(axis=1), 4, .44)
    items = []
    for row in range(4):
        top, bottom = ys[row:row+2]
        xs = bands(mask[top:bottom].sum(axis=0), 4)
        for col in range(4):
            left, right = xs[col:col+2]
            yy, xx = np.where(mask[top:bottom, left:right])
            assert len(xx) > 100, f'{path}: empty {row},{col}'
            l, t, r, b = int(xx.min()+left), int(yy.min()+top), int(xx.max()+left+1), int(yy.max()+top+1)
            margin = min(l-left, right-r, t-top, bottom-b)
            if margin < 1:
                failures.append(f'{path.stem}: clipped/touching cell {row},{col}, margin={margin}')
            items.append((row,col,l-1,t-1,r+1,b+1,margin))
    identity = path.stem.removesuffix('-polish')
    # Recovery is upright with lowered weapons; one PPU across poses preserves body size.
    baseline_row = 0 if identity == 'slime' else 3
    ppu = float(np.median([b-t for row,col,l,t,r,b,m in items if row==baseline_row]))/SCALE.get(identity,1.22)
    for row,col,l,t,r,b,margin in items:
        w,h = r-l,b-t
        part = mask[t:b,l:r]
        yy,xx = np.where(part & (np.indices(part.shape)[0] > h*.86))
        pivot = float(np.median(xx))/w if len(xx) else .5
        name = f'{path.stem}_{row}_{col}'
        layouts.append([path.stem,row,col,l,t,w,h,round(pivot,6),round(ppu,4),width,height])
        for poly in contour(part,all_parts=True):
            outlines.append([name]+[f'{x-w/2:.3f}:{h/2-y:.3f}' for x,y in poly])
    reports.append(dict(id=identity,sprites=16,ppu=round(ppu,4),minimumGutter=min(x[-1] for x in items),opaqueFraction=round(float(mask.mean()),4)))
if failures:
    raise ValueError('\n'.join(failures))
with Path('Tools/animation-polish-layout.csv').open('w',newline='') as f:
    writer=csv.writer(f)
    writer.writerow(['id','row','column','left','top','width','height','pivotX','ppu','sourceWidth','sourceHeight'])
    writer.writerows(layouts)
with Path('Tools/animation-polish-outlines.csv').open('w',newline='') as f:
    csv.writer(f).writerows(outlines)
Path('Docs/AnimationPolish/alpha-audit.json').write_text(json.dumps(reports,indent=2)+'\n')
print(json.dumps(reports,indent=2))
