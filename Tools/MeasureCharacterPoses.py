"""Read-only PNG analysis; writes Unity sprite layout/outline CSV, never edits pixels.
Requires Pillow and NumPy. Review generated boundaries before running the Editor import.
"""
from pathlib import Path
import csv
import numpy as np
from PIL import Image


def simplify(points, tolerance=0.65):
    if len(points) <= 2:
        return points
    v = points[-1] - points[0]
    d = np.linalg.norm(v)
    distances = (np.abs(v[0]*(points[:, 1]-points[0, 1])-v[1]*(points[:, 0]-points[0, 0]))/d
                 if d else np.linalg.norm(points-points[0], axis=1))
    i = int(np.argmax(distances))
    if distances[i] <= tolerance:
        return points[[0, -1]]
    return np.concatenate((simplify(points[:i+1])[:-1], simplify(points[i:])))


def contour(mask):
    # Include the source's antialiased edge without including distant neighbouring art.
    p = np.pad(mask, 1)
    m = np.logical_or.reduce([p[dy:dy+mask.shape[0], dx:dx+mask.shape[1]]
                              for dy in range(3) for dx in range(3)])
    p = np.pad(m, 1)
    edges = {}
    for neighbour, start, end in [
        (p[:-2, 1:-1], (0, 0), (1, 0)), (p[1:-1, 2:], (1, 0), (1, 1)),
        (p[2:, 1:-1], (1, 1), (0, 1)), (p[1:-1, :-2], (0, 1), (0, 0))]:
        for y, x in zip(*np.where(m & ~neighbour)):
            a=(int(x)+start[0], int(y)+start[1]); b=(int(x)+end[0], int(y)+end[1])
            edges.setdefault(a, []).append(b)
    loops=[]
    while edges:
        first=next(iter(edges)); current=first; loop=[]
        while current in edges:
            loop.append(current); choices=edges[current]; nxt=choices.pop()
            if not choices: del edges[current]
            current=nxt
            if current==first: break
        if current==first: loops.append(np.array(loop, dtype=float))
    def area(p): return abs(np.sum(p[:,0]*np.roll(p[:,1],1)-p[:,1]*np.roll(p[:,0],1)))
    outer=max(loops,key=area)
    return simplify(np.concatenate((outer,outer[:1])))[:-1]


def measure_actions():
    layouts=[]; outlines=[]
    for path in sorted(Path('Assets/TalesTactics/Art/Characters').glob('*-poses.png')):
        alpha=np.array(Image.open(path))[:,:,3]; available=alpha>32; height,width=alpha.shape
        components=[]
        for y,x in zip(*np.where(available)):
            if not available[y,x]:continue
            available[y,x]=False; todo=[(int(x),int(y))]; pixels=[]
            while todo:
                xx,yy=todo.pop();pixels.append((xx,yy))
                for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)):
                    nx,ny=xx+dx,yy+dy
                    if 0<=nx<width and 0<=ny<height and available[ny,nx]:
                        available[ny,nx]=False;todo.append((nx,ny))
            if len(pixels)>1500:components.append(np.array(pixels))
        assert len(components)==16, f'{path}: expected 16 main figures'
        components.sort(key=lambda p:(round(p[:,1].min()/300),p[:,0].min()))
        ppu=max(p[:,1].max()-p[:,1].min()+1 for p in components[4:8])/1.28
        for i,pixels in enumerate(components):
            left,top=np.maximum(pixels.min(axis=0)-2,0);right,bottom=np.minimum(pixels.max(axis=0)+3,[width,height])
            w,h=right-left,bottom-top
            feet=pixels[pixels[:,1]>=bottom-h*0.2]
            foot=(feet[:,0].min()+feet[:,0].max())/2
            name=[path.stem.removesuffix('-poses'),['Attack','Cast','Guard','Damage'][i//4],['Front','Back','Right','Left'][i%4]]
            layouts.append(name+[left,top,w,h,round((foot-left)/w,6),round(ppu,4),width,height])
            mask=np.zeros((h,w),dtype=bool);mask[pixels[:,1]-top,pixels[:,0]-left]=True
            poly=contour(mask)
            outlines.append(['_'.join(name)]+[f'{x-w/2:.3f}:{h/2-y:.3f}' for x,y in poly])
        print(path.stem, '16 measured rectangles and isolated render outlines',flush=True)
    with Path('Tools/character-pose-layout.csv').open('w',newline='') as f:
        wr=csv.writer(f);wr.writerow(['id','pose','facing','left','top','width','height','pivotX','ppu','sourceWidth','sourceHeight']);wr.writerows(layouts)
    with Path('Tools/character-pose-outlines.csv').open('w',newline='') as f:
        csv.writer(f).writerows(outlines)


if __name__ == '__main__':
    measure_actions()
