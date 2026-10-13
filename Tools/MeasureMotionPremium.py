"""Read-only connected-component slicing for generated atlases; never modifies PNG pixels."""
from pathlib import Path
import numpy as np
from PIL import Image
from MeasureCharacterPoses import contour
import csv,json
reports=[];layouts=[];outlines=[]
for path in sorted(Path("Assets/TalesTactics/Art/MotionPremium").glob("*.png")):
 alpha=np.array(Image.open(path).convert("RGBA"))[:,:,3];mask=alpha>160;available=mask.copy();height,width=mask.shape
 if mask.mean()>.6:raise ValueError(f"Opaque background: {path}")
 pieces=[]
 for y,x in zip(*np.where(available)):
  if not available[y,x]:continue
  available[y,x]=False;todo=[(int(x),int(y))];pixels=[]
  while todo:
   xx,yy=todo.pop();pixels.append((xx,yy))
   for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)):
    nx,ny=xx+dx,yy+dy
    if 0<=nx<width and 0<=ny<height and available[ny,nx]:available[ny,nx]=False;todo.append((nx,ny))
  if len(pixels)>500:pieces.append(np.array(pixels))
 if len(pieces) not in (72,80):raise ValueError(f"{path.stem}: {len(pieces)} complete figures; expected 72 or 80")
 pieces.sort(key=lambda p:float(np.median(p[:,1])))
 ordered=[]
 for row in range(len(pieces)//8):
  group=pieces[row*8:row*8+8];group.sort(key=lambda p:float(np.median(p[:,0])))
  ordered.extend(group)
 ppu=float(np.median([p[:,1].max()-p[:,1].min()+1 for p in ordered[:16]]))/1.22
 edge=[];hashes=set()
 for index,pixels in enumerate(ordered):
  lo=pixels.min(0);hi=pixels.max(0)+1
  if lo.min()==0 or hi[0]==width or hi[1]==height:edge.append(index)
  l,t=np.maximum(lo-2,0);r,b=np.minimum(hi+2,[width,height]);w,h=int(r-l),int(b-t)
  part=np.zeros((h,w),dtype=bool);part[pixels[:,1]-t,pixels[:,0]-l]=True
  feet=pixels[pixels[:,1]>=b-h*.14];pivot=(float(np.median(feet[:,0]))-l)/w
  row,col=divmod(index,8);pose=row*2+col//4;direction=col%4;name=f"{path.stem}_{pose}_{direction}"
  layouts.append([path.stem,pose,direction,int(l),int(t),w,h,round(pivot,6),round(ppu,4),width,height])
  for poly in contour(part,all_parts=True):outlines.append([name]+[f"{x-w/2:.3f}:{h/2-y:.3f}" for x,y in poly])
  hashes.add(hash(part.tobytes()))
 if edge:raise ValueError(f"{path.stem}: image-edge clipping in figures {edge}")
 reports.append(dict(id=path.stem,sprites=len(ordered),width=width,height=height,ppu=round(ppu,3),uniqueMasks=len(hashes),imageEdgeContacts=edge,opaqueFraction=round(float(mask.mean()),3)))
with open("Tools/motion-premium-layout.csv","w",newline="") as f:
 writer=csv.writer(f);writer.writerow(["id","pose","direction","left","top","width","height","pivotX","ppu","sourceWidth","sourceHeight"]);writer.writerows(layouts)
with open("Tools/motion-premium-outlines.csv","w",newline="") as f:csv.writer(f).writerows(outlines)
Path("Docs/MotionStatus/alpha-audit.json").write_text(json.dumps(reports,indent=2)+"\n")
print(json.dumps(reports,indent=2))
