"""Derive Unity sprite rects/pivots/outlines without changing source image pixels."""
from pathlib import Path
import csv,json
import numpy as np
from PIL import Image
from MeasureCharacterPoses import contour
root=Path('Assets/TalesTactics/Art/Crossover')
roster=json.loads(Path('Docs/BossConcepts/roster.json').read_text(encoding='utf-8-sig'))
audit={r['id']:r for r in json.loads(Path('Docs/CrossoverStatus/art-alpha-audit.json').read_text())}
selected={'demitel':'demitel-v2'}
layouts=[];outlines=[]
sources=[]
overrides={('dhaos',3):'dhaos-left',('shigure',1):'shigure-back',('vholran',1):'vholran-back-v2'}
for entry in roster['bosses']+[{'id':'boundary-device'},{'id':'hyades-human'}]:
    identity=entry['id'].replace('guardian','golem')
    if identity=='artorius':sources.extend((identity,'artorius-'+d,index) for index,d in enumerate(('front','back','right','left-v2')))
    else:sources.append((identity,selected.get(identity,identity),None))
sources.extend((identity,source,direction) for (identity,direction),source in overrides.items())
for identity,source,fixed_direction in sources:
    report=audit[source]
    if not report['passed']:raise ValueError(f'{source}: alpha audit not passed')
    mask=np.asarray(Image.open(root/(source+'.png')).convert('RGBA'))[:,:,3]>160
    height,width=mask.shape
    boxes=sorted(report['boxes'],key=lambda b:(b[1]+b[3])/2)
    ordered=[]
    columns=4 if identity=='boundary-device' else 8 if fixed_direction is None else 6;rows=3 if identity=='boundary-device' else 9 if fixed_direction is None else 3
    for row in range(rows):ordered.extend(sorted(boxes[row*columns:row*columns+columns],key=lambda b:(b[0]+b[2])/2))
    ppu=float(np.median([b[3]-b[1] for b in ordered[:columns]]))/1.22
    for i,(x,y,r,b,_) in enumerate(ordered):
        row,col=divmod(i,columns);pose=row if identity=='boundary-device' else row*2+col//4 if fixed_direction is None else i;direction=col%4 if fixed_direction is None else fixed_direction
        if identity=='jahmir':pose=row*2+col%2;direction=(0,1,3,2)[col//2]
        if identity=='ganabelt':direction=(3,0,2,1)[col%4]
        if fixed_direction is None and (identity,direction) in overrides:continue
        # Bounds come from an isolated connected silhouette, never a crop of the concept board.
        l=max(0,x-2);t=max(0,y-2);right=min(width,r+2);bottom=min(height,b+2);w=right-l;h=bottom-t
        part=mask[t:bottom,l:right]
        yy,xx=np.where(part);feet=xx[yy>=h*.86];pivot=float(np.median(feet))/w
        name=f'{identity}_{pose}_{direction}'
        layouts.append([identity,source,pose,direction,l,t,w,h,round(pivot,6),round(ppu,4),width,height])
        for poly in contour(part,all_parts=True):outlines.append([name]+[f'{px-w/2:.3f}:{h/2-py:.3f}' for px,py in poly])
death_groups=[['dhaos','demitel','jahmir','shizel'],['hyades','rassius','van','asch'],['legretta','artorius','shigure','oscar'],['vholran','balseph','ganabelt','ordo'],['executor','golem','origin','maxwell'],['hyades-human']]
for index,identities in enumerate(death_groups):
    source=f'death-{index}';report=audit[source]
    if not report['passed']:raise ValueError(f'{source}: alpha audit not passed')
    mask=np.asarray(Image.open(root/(source+'.png')).convert('RGBA'))[:,:,3]>160
    height,width=mask.shape;boxes=sorted(report['boxes'],key=lambda b:(b[1]+b[3])/2)
    ordered=[]
    for row in range(len(identities)):ordered.extend(sorted(boxes[row*8:row*8+8],key=lambda b:(b[0]+b[2])/2))
    # Kneeling figures are about three quarters of their standing height. One PPU per source.
    ppu=float(np.median([b[3]-b[1] for i,b in enumerate(ordered) if i%8<4]))/.9
    for i,(x,y,r,b,_) in enumerate(ordered):
        row,col=divmod(i,8);identity=identities[row];pose=18+col//4;direction=col%4
        l=max(0,x-2);t=max(0,y-2);right=min(width,r+2);bottom=min(height,b+2);w=right-l;h=bottom-t
        part=mask[t:bottom,l:right];name=f'{identity}_{pose}_{direction}'
        layouts.append([identity,source,pose,direction,l,t,w,h,.5,round(ppu,4),width,height])
        for poly in contour(part,all_parts=True):outlines.append([name]+[f'{px-w/2:.3f}:{h/2-py:.3f}' for px,py in poly])
source='civilians';report=audit[source]
if not report['passed']:raise ValueError('Civilian alpha audit not passed')
mask=np.asarray(Image.open(root/(source+'.png')).convert('RGBA'))[:,:,3]>160
height,width=mask.shape;boxes=sorted(report['boxes'],key=lambda b:(b[1]+b[3])/2);ordered=[]
for row in range(2):ordered.extend(sorted(boxes[row*8:row*8+8],key=lambda b:(b[0]+b[2])/2))
ppu=float(np.median([b[3]-b[1] for b in ordered]))/1.15
for i,(x,y,r,b,_) in enumerate(ordered):
    identity=f'civilian-{i//4}';direction=i%4;l=max(0,x-2);t=max(0,y-2);right=min(width,r+2);bottom=min(height,b+2);w=right-l;h=bottom-t
    part=mask[t:bottom,l:right];name=f'{identity}_0_{direction}'
    layouts.append([identity,source,0,direction,l,t,w,h,.5,round(ppu,4),width,height])
    for poly in contour(part,all_parts=True):outlines.append([name]+[f'{px-w/2:.3f}:{h/2-py:.3f}' for px,py in poly])
with open('Tools/crossover-layout.csv','w',newline='') as f:
    w=csv.writer(f);w.writerow(['id','source','pose','direction','left','top','width','height','pivotX','ppu','sourceWidth','sourceHeight']);w.writerows(layouts)
with open('Tools/crossover-outlines.csv','w',newline='') as f:csv.writer(f).writerows(outlines)
print(f'{len(roster["bosses"])} atlases, {len(layouts)} rects, {len(outlines)} polygons; pixels unchanged')
