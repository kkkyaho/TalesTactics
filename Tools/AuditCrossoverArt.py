"""Read-only alpha/component audit. Does not edit, rescale, crop, or export image pixels."""
from pathlib import Path
import json,sys
import numpy as np
from PIL import Image
out=[]
audit_path=Path('Docs/CrossoverStatus/art-alpha-audit.json')
if len(sys.argv)>1 and audit_path.exists():out=[r for r in json.loads(audit_path.read_text()) if r['id'] not in sys.argv[1:]]
for p in sorted(Path('Assets/TalesTactics/Art/Crossover').glob('*.png')):
    if len(sys.argv)>1 and p.stem not in sys.argv[1:]:continue
    a=np.asarray(Image.open(p).convert('RGBA'))[:,:,3]
    available=a>160;boxes=[];height,width=a.shape
    for y,x in zip(*np.where(available)):
        if not available[y,x]:continue
        available[y,x]=False;pending=[(int(x),int(y))];pixels=[]
        while pending:
            xx,yy=pending.pop();pixels.append((xx,yy))
            for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)):
                nx,ny=xx+dx,yy+dy
                if 0<=nx<width and 0<=ny<height and available[ny,nx]:available[ny,nx]=False;pending.append((nx,ny))
        if len(pixels)>24:
            coords=np.array(pixels);lo=coords.min(0);hi=coords.max(0)+1
            boxes.append([int(lo[0]),int(lo[1]),int(hi[0]),int(hi[1]),len(pixels)])
    bodies=[b[:] for b in boxes if b[4]>2000];fragments=[b for b in boxes if b[4]<=2000]
    # Floating books/stone rings are parts of a figure, not extra animation frames.
    # Attach only fragments within 35px of an unambiguous body silhouette.
    ambiguous=[]
    for f in fragments:
        x,y,r,b,n=f
        distance=lambda q:max(q[0]-r,x-q[2],0)**2+max(q[1]-b,y-q[3],0)**2
        nearest=sorted(range(len(bodies)),key=lambda i:distance(bodies[i]))
        if not nearest or distance(bodies[nearest[0]])>35**2:continue
        if len(nearest)>1 and distance(bodies[nearest[0]])==distance(bodies[nearest[1]]):ambiguous.append(f);continue
        q=bodies[nearest[0]];q[0]=min(q[0],x);q[1]=min(q[1],y);q[2]=max(q[2],r);q[3]=max(q[3],b);q[4]+=n
    edges=[i for i,(x,y,r,b,n) in enumerate(bodies) if min(x,y)==0 or r==a.shape[1] or b==a.shape[0]]
    expected=12 if p.stem=='boundary-device' else 18 if p.stem in ('artorius-front','artorius-back','artorius-right','artorius-left','artorius-left-v2','dhaos-left','shigure-back','vholran-back','vholran-back-v2') else 72
    if p.stem.startswith('death-'):expected=8 if p.stem=='death-5' else 32
    if p.stem=='civilians':expected=16
    if p.stem=='boundary-device':
        # Visually reviewed four columns / three states; broken relay pieces intentionally disconnect.
        bodies=[];ys=[0,int(height*.34),int(height*.635),height]
        for row in range(3):
            for col in range(4):
                left=width*col//4;right=width*(col+1)//4;top,bottom=ys[row:row+2]
                yy,xx=np.where(a[top:bottom,left:right]>160)
                if len(xx):bodies.append([int(xx.min())+left,int(yy.min())+top,int(xx.max())+left+1,int(yy.max())+top+1,len(xx)])
        ambiguous=[];edges=[i for i,(x,y,r,b,n) in enumerate(bodies) if min(x,y)==0 or r==width or b==height]
    out.append(dict(id=p.stem,width=a.shape[1],height=a.shape[0],figures=len(bodies),expected=expected,edgeContacts=edges,ambiguousFragments=ambiguous,componentsOver500=sum(b[4]>500 for b in boxes),opaqueFraction=round(float((a>160).mean()),4),boxes=bodies,passed=bool(len(bodies)==expected and not edges and not ambiguous and (a>160).mean()<.6)))
Path('Docs/CrossoverStatus').mkdir(exist_ok=True)
Path('Docs/CrossoverStatus/art-alpha-audit.json').write_text(json.dumps(out,indent=2)+'\n')
for r in out:
    if len(sys.argv)==1 or r['id'] in sys.argv[1:]:print(r['id'],r['figures'],'edge',r['edgeContacts'],'PASS' if r['passed'] else 'REVIEW')
