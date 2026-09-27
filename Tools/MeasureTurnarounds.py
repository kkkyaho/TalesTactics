"""Read source alpha only; emit precise idle sprite bounds/pivots/outlines, never edit PNGs."""
from pathlib import Path
import csv
import numpy as np
from PIL import Image
from MeasureCharacterPoses import contour

ids=['cless','mint','velvet','farah','tear','jade','natalia','alphen','shionne','kisara']
layouts=[];outlines=[]
for id in ids:
    a=np.array(Image.open(f'Assets/TalesTactics/Art/Characters/{id}-turnaround.png'))[:,:,3]
    height,width=a.shape
    figures=[]
    for column in range(4):
        x0=column*width//4;x1=(column+1)*width//4
        available=a[:,x0:x1]>32
        components=[]
        for y,x in zip(*np.where(available)):
            if not available[y,x]:continue
            available[y,x]=False;todo=[(int(x),int(y))];pixels=[]
            while todo:
                xx,yy=todo.pop();pixels.append((xx+x0,yy))
                for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)):
                    nx,ny=xx+dx,yy+dy
                    if 0<=nx<x1-x0 and 0<=ny<height and available[ny,nx]:
                        available[ny,nx]=False;todo.append((nx,ny))
            if len(pixels)>1000:components.append(np.array(pixels))
        assert len(components)==1,(id,column,[len(c) for c in components])
        figures.append(components[0])
    ppu=max(p[:,1].max()-p[:,1].min()+1 for p in figures)/1.28
    for i,p in enumerate(figures):
        left,top=np.maximum(p.min(axis=0)-2,0);right,bottom=np.minimum(p.max(axis=0)+3,[width,height]);w,h=right-left,bottom-top
        feet=p[p[:,1]>=bottom-h*.15];center=(feet[:,0].min()+feet[:,0].max())/2
        name=id+'_'+['Front','Back','Right','Left'][i]
        layouts.append([id,name,left,top,w,h,round((center-left)/w,6),round(ppu,4),width,height])
        mask=np.zeros((h,w),dtype=bool);mask[p[:,1]-top,p[:,0]-left]=True
        outlines.append([name]+[f'{x-w/2:.3f}:{h/2-y:.3f}' for x,y in contour(mask)])
    print(id,'4 complete figures',flush=True)
with Path('Tools/turnaround-layout.csv').open('w',newline='') as f:
    writer=csv.writer(f);writer.writerow(['id','name','left','top','width','height','pivotX','ppu','sourceWidth','sourceHeight']);writer.writerows(layouts)
with Path('Tools/turnaround-outlines.csv').open('w',newline='') as f:csv.writer(f).writerows(outlines)
