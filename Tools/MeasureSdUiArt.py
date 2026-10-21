"""Measure UI alpha islands and update atlas rectangles; PNG pixels are never changed."""
import json, pathlib, re, sys, hashlib
import numpy as np
from PIL import Image
from MeasureSdCharacterArt import components
ROOT=pathlib.Path(__file__).resolve().parents[1]
RES=ROOT/'Assets/TalesTactics/Resources/TalesTactics'
report=[]
def measure(name, regions):
 path=RES/(name+'.png'); image=Image.open(path).convert('RGBA'); a=np.asarray(image)[:,:,3]
 assert float(np.mean(a==0))>.25,(name,'background not transparent')
 assert not any(np.any(line>24) for line in (a[0],a[-1],a[:,0],a[:,-1])),(name,'canvas edge clipping')
 centers=np.array([[r['x']+r['width']/2,r['y']+r['height']/2] for r in regions])
 groups=[[] for _ in regions]
 for part in components(a>12):
  center=(part.min(0)+part.max(0))/2
  i=int(np.argmin(np.sum((centers-center)**2,axis=1)))
  groups[i].append(part)
 revised=[]
 for r,parts in zip(regions,groups):
  assert parts,(name,r['key'],'empty')
  p=np.concatenate(parts);lo=np.maximum(0,p.min(0)-3);hi=np.minimum([image.width,image.height],p.max(0)+4)
  n=dict(r,x=int(lo[0]),y=int(lo[1]),width=int(hi[0]-lo[0]),height=int(hi[1]-lo[1]))
  assert n['width']*n['height']<image.width*image.height*.25,(name,r['key'],'merged region')
  revised.append(n)
 report.append(dict(name=name,width=image.width,height=image.height,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),alphaZero=float(np.mean(a==0)),regions=revised))
 return revised
catalog_path=RES/'Ornate/IconRegions.json'
backup=ROOT/'Temp/SdArtDirection/UiOriginals/IconRegions.json'
backup.parent.mkdir(parents=True,exist_ok=True)
if not backup.exists():backup.write_bytes(catalog_path.read_bytes())
catalog=json.loads(backup.read_text(encoding='utf-8-sig'))
for name in ['CommonIcons','FieldIcons','CompactIcons']:
 old=[r for r in catalog['regions'] if r['atlas']==name]
 new=measure('Ornate/'+name,old)
 mapping={r['key']:r for r in new}
 catalog['regions']=[mapping.get(r['key'],r) for r in catalog['regions']]
catalog_path.write_text(json.dumps(catalog,indent=2)+'\n',encoding='utf-8')
original=[(44,18,384,410),(510,15,365,415),(942,45,330,385),(1365,20,400,410),(35,450,380,400),(455,450,415,400),(875,450,445,405),(1390,450,330,400)]
regions=[dict(key=str(i),x=x,y=y,width=w,height=h) for i,(x,y,w,h) in enumerate(original)]
new=measure('Ornate/ClassIcons',regions)
code_path=ROOT/'Assets/TalesTactics/Runtime/Presentation/BattleClassArt.cs'
code=code_path.read_text(encoding='utf-8-sig')
rects=',\n            '.join('new Rect({x},{y},{width},{height})'.format(**r) for r in new)
code=re.sub(r'static readonly Rect\[\] classArtRegions =\s*\{.*?\};','static readonly Rect[] classArtRegions =\n        {\n            '+rects+'\n        };',code,flags=re.S)
code_path.write_text(code,encoding='utf-8')
for name,cols,rows in [('Ornate/MenuIcons',3,2),('EquipmentIcons',3,3)]:
 image=Image.open(RES/(name+'.png'))
 regions=[dict(key=str(i),x=(i%cols)*image.width/cols,y=(i//cols)*image.height/rows,width=image.width/cols,height=image.height/rows) for i in range(cols*rows)]
 measure(name,regions)
(ROOT/'Docs/SdUiTheme/icon-regions-measured.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps([dict(name=r['name'],regions=len(r['regions']),size=[r['width'],r['height']]) for r in report]))
