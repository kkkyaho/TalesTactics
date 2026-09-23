"""Read-only analysis of walk/dead PNGs. Writes Unity layout/outline CSV only."""
from pathlib import Path
import csv
import numpy as np
from PIL import Image
from MeasureCharacterPoses import contour

layouts=[]; outlines=[]
for character in ('farah','natalia'):
    for action,rows in (('walk',4),('dead',3)):
        suffix='-v2' if character=='natalia' and action=='walk' else ''
        path=Path(f'Assets/TalesTactics/Art/Characters/{character}-{action}{suffix}.png')
        alpha=np.array(Image.open(path))[:,:,3];available=alpha>32;height,width=alpha.shape
        components=[]
        for y,x in zip(*np.where(available)):
            if not available[y,x]:continue
            available[y,x]=False;todo=[(int(x),int(y))];pixels=[]
            while todo:
                xx,yy=todo.pop();pixels.append((xx,yy))
                for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)):
                    nx,ny=xx+dx,yy+dy
                    if 0<=nx<width and 0<=ny<height and available[ny,nx]:
                        available[ny,nx]=False;todo.append((nx,ny))
            if len(pixels)>1500:components.append(np.array(pixels))
        assert len(components)==rows*4,f'{path}: expected {rows*4} isolated figures, got {len(components)}'
        components.sort(key=lambda p:p[:,1].min())
        components=[p for row in range(rows) for p in sorted(components[row*4:row*4+4],key=lambda p:p[:,0].min())]
        ppu=max(p[:,1].max()-p[:,1].min()+1 for p in components[:4])/(1.28 if action=='walk' else 1.18)
        for i,pixels in enumerate(components):
            left,top=np.maximum(pixels.min(axis=0)-2,0);right,bottom=np.minimum(pixels.max(axis=0)+3,[width,height]);w,h=right-left,bottom-top
            # Walk frames anchor the torso horizontally, avoiding alternating-foot pivot jitter.
            # Collapse frames anchor their ground footprint, including the final prone pose.
            anchor=pixels[pixels[:,1]<=top+h*0.3] if action=='walk' else pixels[pixels[:,1]>=bottom-h*0.2]
            center=(anchor[:,0].min()+anchor[:,0].max())/2
            name=[character,action.capitalize(),str(i//4),['Front','Back','Right','Left'][i%4]]
            layouts.append(name+[left,top,w,h,round((center-left)/w,6),round(ppu,4),width,height])
            mask=np.zeros((h,w),dtype=bool);mask[pixels[:,1]-top,pixels[:,0]-left]=True
            outlines.append(['_'.join(name)]+[f'{x-w/2:.3f}:{h/2-y:.3f}' for x,y in contour(mask)])
        print(path.stem,len(components),'frames',flush=True)
with Path('Tools/locomotion-layout.csv').open('w',newline='') as f:
    wr=csv.writer(f);wr.writerow(['id','action','frame','facing','left','top','width','height','pivotX','ppu','sourceWidth','sourceHeight']);wr.writerows(layouts)
with Path('Tools/locomotion-outlines.csv').open('w',newline='') as f:
    csv.writer(f).writerows(outlines)
