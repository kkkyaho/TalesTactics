"""Read-only PNG analysis for authored pixel sheets. Writes slice metadata, never image pixels."""
from pathlib import Path
import csv
import numpy as np
from PIL import Image
from MeasureCharacterPoses import contour

ROOT=Path('Assets/TalesTactics/Art/PixelCampaign')
ENEMIES={'wolf','slime','golem','sentinel','archer','mage','polwigle','eggbear','penguinist'}
SCALE={'wolf':.85,'slime':.66,'golem':1.5,'polwigle':.72,'eggbear':1.32,'penguinist':.84,'dhaos':1.5}

def bands(projection, count, span_fraction=.24):
    # Find the deepest whitespace gutter around each nominal row boundary.
    n=len(projection); cuts=[0]
    for i in range(1,count):
        mid=n*i/count; span=n/count*span_fraction
        lo,hi=int(mid-span),int(mid+span)
        cost=np.convolve(projection.astype(float),np.ones(7)/7,mode='same')
        candidates=np.arange(lo,hi)
        best=candidates[np.argmin(cost[lo:hi]+np.abs(candidates-mid)*.0001)]
        cuts.append(int(best))
    return cuts+[n]

def measure():
    layouts=[];outlines=[]
    scales={}
    paths=sorted(ROOT.glob('*-hq.png'))+sorted(ROOT.glob('*-motion.png'))+sorted(ROOT.glob('*-extra.png'))+sorted(p for p in ROOT.glob('*.png') if p.stem in ENEMIES)
    paths+=sorted(ROOT.glob('*-right.png'))+[Path('Assets/TalesTactics/Resources/TalesTactics/PixelProps.png')]
    for path in paths:
        if not path.exists():continue
        alpha=np.array(Image.open(path).convert('RGBA'))[:,:,3];height,width=alpha.shape
        mask=alpha>128;rows=1 if path.stem.endswith('-right') else 5 if path.stem in ENEMIES else 4
        if np.mean(mask)>.8:raise ValueError(f'{path}: opaque backdrop')
        ys=bands(mask.sum(axis=1),rows,.44);items=[]
        for row in range(rows):
            top,bottom=ys[row:row+2];rowmask=mask[top:bottom]
            xs=bands(rowmask.sum(axis=0),4)
            for col in range(4):
                left,right=xs[col:col+2];part=rowmask[:,left:right]
                yy,xx=np.where(part)
                if len(xx)<100:raise ValueError(f'{path} empty cell {row},{col}')
                l,t,r,b=int(xx.min()+left),int(yy.min()+top),int(xx.max()+left+1),int(yy.max()+top+1)
                l=max(left,l-1);r=min(right,r+1);t=max(top,t-1);b=min(bottom,b+1)
                items.append((row,col,l,t,r,b))
        identity=path.stem.removesuffix('-hq').removesuffix('-motion').removesuffix('-right').removesuffix('-extra')
        standing=items[:2] if path.stem.endswith('-right') else items[:4]
        ppu=np.median([b-t for row,col,l,t,r,b in standing])/SCALE.get(identity,1.22)
        if path.stem.endswith('-hq'):scales[identity]=ppu
        # Each authored sheet has a different canvas density. Normalize its standing row.
        if identity=='PixelProps':ppu=220
        for row,col,l,t,r,b in items:
            w,h=r-l,b-t;part=mask[t:b,l:r]
            # Geometry outline excludes low-alpha generation residue without changing source PNG.
            polys=contour(part,all_parts=True)
            yy,xx=np.where(part & (np.indices(part.shape)[0]>h*.82))
            pivot=(float(np.median(xx)) if len(xx) else w*.5)/w
            if (path.stem.endswith('-motion') and row==3) or (identity in ENEMIES and row==4):pivot=.5
            name=f'{path.stem}_{row}_{col}'
            layouts.append([path.stem,row,col,l,t,w,h,round(pivot,6),round(ppu,4),width,height])
            for poly in polys:outlines.append([name]+[f'{x-w/2:.3f}:{h/2-y:.3f}' for x,y in poly])
        print(path.stem,rows*4,'sprites', 'PPU',round(ppu,2),flush=True)
    with Path('Tools/pixel-campaign-layout.csv').open('w',newline='') as f:
        wr=csv.writer(f);wr.writerow(['id','row','column','left','top','width','height','pivotX','ppu','sourceWidth','sourceHeight']);wr.writerows(layouts)
    with Path('Tools/pixel-campaign-outlines.csv').open('w',newline='') as f:csv.writer(f).writerows(outlines)
if __name__=='__main__':measure()
