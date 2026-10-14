"""Read-only PNG analysis; writes sprite rectangles/outlines, never PNG pixels.
Requires Pillow, NumPy and SciPy. Source foot anchors and PPU are preserved.
"""
from pathlib import Path
import csv,json
import numpy as np
from PIL import Image
from scipy import ndimage
from scipy.optimize import linear_sum_assignment
from MeasureCharacterPoses import contour
from MeasurePixelCampaign import bands
root=Path('Assets/TalesTactics/Art/SignatureWeapons')
manifest=json.loads(Path('Tools/signature-art-generation.json').read_text(encoding='utf-8'))
original={}
for file in ['pixel-campaign-layout.csv','animation-polish-layout.csv','motion-premium-layout.csv']:
    with Path('Tools',file).open() as f:
        for v in csv.reader(f):
            if v[0]!='id':original[(file,v[0],int(v[1]),int(v[2]))]=v
layouts=[];outlines=[];reports=[]
for job in manifest['jobs']:
    path=Path(job['destination']);source=Path(job['source']);premium=job['kind']=='premium'
    table='motion-premium-layout.csv' if premium else 'animation-polish-layout.csv' if job['kind']=='polish' else 'pixel-campaign-layout.csv'
    records=[v for (t,s,r,c),v in original.items() if t==table and s==source.stem]
    records.sort(key=lambda v:(int(v[1]),int(v[2])))
    image=Image.open(path);mask=np.array(image.convert('RGBA'))[:,:,3]>160;height,width=mask.shape
    assert image.mode=='RGBA' and .02<float(mask.mean())<.72, f'{path}: missing transparency'
    sx=width/float(records[0][9]);sy=height/float(records[0][10]);assert abs(sx-sy)<.025,f'{path}: distorted canvas'
    labels,n=ndimage.label(mask);sizes=np.bincount(labels.ravel());boxes=ndimage.find_objects(labels)
    major=sorted(range(1,n+1),key=lambda i:sizes[i],reverse=True)[:len(records)]
    assert len(major)==len(records) and min(sizes[i] for i in major)>1000,f'{path}: incomplete or touching figures'
    centers=np.array([[(boxes[i-1][1].start+boxes[i-1][1].stop)/2,(boxes[i-1][0].start+boxes[i-1][0].stop)/2] for i in major])
    expected=np.array([[(float(v[3])+float(v[5])/2)*sx,(float(v[4])+float(v[6])/2)*sy] for v in records])
    cost=((expected[:,None,:]-centers[None,:,:])**2).sum(axis=2)
    rr,cc=linear_sum_assignment(cost);assigned={int(row):[major[col]] for row,col in zip(rr,cc)}
    major_set=set(major)
    # Detached bowstrings / blade highlights join the nearest complete figure. The raster is untouched.
    for i in range(1,n+1):
        if i in major_set or sizes[i]<4:continue
        box=boxes[i-1];cx=(box[1].start+box[1].stop)/2;cy=(box[0].start+box[0].stop)/2
        distance=[]
        for row in range(len(records)):
            b=boxes[assigned[row][0]-1]
            dx=max(b[1].start-cx,0,cx-b[1].stop);dy=max(b[0].start-cy,0,cy-b[0].stop)
            distance.append(dx*dx+dy*dy+.001*((expected[row][0]-cx)**2+(expected[row][1]-cy)**2))
        assigned[int(np.argmin(distance))].append(i)
    edge=[]
    for row,old in enumerate(records):
        selection=np.isin(labels,assigned[row]);yy,xx=np.where(selection)
        l,t,r,b=int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)
        pose,direction=int(old[1]),int(old[2]);name=f'{source.stem}_{pose}_{direction}';key=f'{path.stem}_{pose}_{direction}'
        if l==0 or t==0 or r==width or b==height:edge.append(name)
        l=max(0,l-2);t=max(0,t-2);r=min(width,r+2);b=min(height,b+2)
        anchor_x=(float(old[3])+float(old[5])*float(old[7]))*sx
        anchor_y=(float(old[4])+float(old[6])*.99)*sy
        layouts.append([path.as_posix(),source.as_posix(),name,key,l,height-b,r-l,b-t,round((anchor_x-l)/(r-l),7),round((b-anchor_y)/(b-t),7),round(float(old[8])*sy,5)])
        for poly in contour(selection[t:b,l:r],all_parts=True):outlines.append([key]+[f'{x-(r-l)/2:.3f}:{(b-t)/2-y:.3f}' for x,y in poly])
    reports.append(dict(id=job['id'],kind=job['kind'],frames=len(records),size=[width,height],alphaFraction=round(float(mask.mean()),4),edgeCells=edge))
icons=root/'icons.png'
if icons.exists():
    ids=['eternal-sword','bc-rod','god-weapon','unicorn-horn','blood-pain','holy-quelqatl','unnamed-blade','nebilim','damonisch-core','os-rex']
    im=Image.open(icons);mask=np.array(im.convert('RGBA'))[:,:,3]>160;h,w=mask.shape
    assert im.mode=='RGBA' and .02<float(mask.mean())<.72, 'Icons require true transparency'
    ys=bands(mask.sum(axis=1),2,.4)
    for row in range(2):
        top,bottom=ys[row:row+2];xs=bands(mask[top:bottom].sum(axis=0),5,.4)
        for col in range(5):
            left,right=xs[col:col+2];yy,xx=np.where(mask[top:bottom,left:right])
            l=max(left,int(xx.min()+left)-2);t=max(top,int(yy.min()+top)-2);r=min(right,int(xx.max()+left)+3);b=min(bottom,int(yy.max()+top)+3);key=ids[row*5+col]
            layouts.append([icons.as_posix(),'','',key,l,h-b,r-l,b-t,.5,.5,100])
            for poly in contour(mask[t:b,l:r],all_parts=True):outlines.append([key]+[f'{x-(r-l)/2:.3f}:{(b-t)/2-y:.3f}' for x,y in poly])
with Path('Tools/signature-art-layout.csv').open('w',newline='') as f:
    w=csv.writer(f);w.writerow(['target','source','original','name','x','y','width','height','pivotX','pivotY','ppu']);w.writerows(layouts)
with Path('Tools/signature-art-outlines.csv').open('w',newline='') as f:csv.writer(f).writerows(outlines)
Path('Docs/SignatureWeapons').mkdir(parents=True,exist_ok=True)
Path('Docs/SignatureWeapons/alpha-audit.json').write_text(json.dumps(reports,indent=2)+'\n')
print(json.dumps(dict(sheets=len(reports),characterFrames=sum(r['frames'] for r in reports),icons=10,edgeCells=sum(len(r['edgeCells']) for r in reports))))
