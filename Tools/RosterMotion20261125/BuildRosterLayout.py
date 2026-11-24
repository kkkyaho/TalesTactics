"""Build reviewed sprite metadata only; native PNG bytes remain unchanged."""
import json,hashlib
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
docs=ROOT/'Docs/RosterMotion20261125'
measure=json.loads((ROOT/'Logs/RosterMotion20261125/art-measurements.json').read_text(encoding='utf-8'))
five=json.loads((docs/'landmarks-reviewed-five.json').read_text(encoding='utf-8'))
assert five['reviewed'] and len(five['frames'])==160
four=json.loads((docs/'landmarks-reviewed-four.json').read_text(encoding='utf-8'))
land={f['name']:f for f in five['frames']+four['frames']}
assert len(land)==288
refs={r['characterId']:r for r in measure['references']}
manifest={'schemaVersion':1,'characters':[]};heads=[];scale=[]
for cid,ref in refs.items():
 ch={'characterId':cid,'pixelsPerUnit':0,'sheets':[]}
 for s in [s for s in measure['sheets'] if s['characterId']==cid]:
  path=ROOT/s['path'];raw=path.read_bytes();assert hashlib.sha256(raw).hexdigest()==s['sha256']
  rgba=np.asarray(Image.open(path).convert('RGBA'));h,w=rgba.shape[:2]
  standing=land[f"{cid}-roster-{'idle-front-0' if s['kind']=='idle' else 'attack-front-2'}"]
  body=standing['foot']['y']-standing['head']['y']
  target=ref['existingHeadAnchorToFootWorldUnitsBeforeCharacterScale']
  ppu=round(body/target,5)
  if s['kind']=='idle':ch['pixelsPerUnit']=ppu
  sheet={k:s[k] for k in ('path','sha256','width','height')};sheet.update(pixelsPerUnit=ppu,frames=[])
  for f in s['frames']:
   b=f['alpha64BBoxTopLeft'];l=land[f['name']]
   x0=max(0,b['x']-4);top=max(0,b['y']-4)
   x1=min(w,b['x']+b['width']+4);bottom=min(h,b['y']+b['height']+4)
   rw,rh=x1-x0,bottom-top
   crop=rgba[top:bottom,x0:x1,3]>=64
   assert 0<=int(crop.sum())-f['alpha64AreaPixels']<=1,f['name']+' unexpected detached alpha pixels'
   for neighbor in s['frames']:
    if neighbor['name']==f['name']:continue
    q=neighbor['alpha64BBoxTopLeft']
    assert x1<=q['x'] or x0>=q['x']+q['width'] or bottom<=q['y'] or top>=q['y']+q['height'],f['name']+' neighboring pose bounds overlap'
   px=(l['foot']['x']-x0)/rw;py=(bottom-l['foot']['y'])/rh
   hu=(l['head']['x']-x0)/rw;hv=(bottom-l['head']['y'])/rh
   assert all(0<=v<=1 for v in (px,py,hu,hv)),f['name']
   sheet['frames'].append(dict(name=f['name'],x=x0,y=h-bottom,width=rw,height=rh,pivot=dict(x=round(px,8),y=round(py,8))))
   heads.append(dict(texture=Path(s['path']).stem,sprite=f['name'],u=round(hu,8),v=round(hv,8),width=rw,height=rh))
  scale.append(dict(characterId=cid,kind=s['kind'],pixelsPerUnit=ppu,sourcePose=standing['name'],headToSolePixels=body,targetHeadToSoleWorld=target,formula='new standing head-to-sole pixels / original head-to-pivot world units (before existing character scale)'))
  ch['sheets'].append(sheet)
 manifest['characters'].append(ch)
assert len(heads)==288
out=ROOT/'Assets/TalesTactics/Resources/TalesTactics/UnitHeadAnchors.json'
existing=json.loads(out.read_text(encoding='utf-8-sig'))
old=[e for e in existing['sprites'] if '-roster-' not in e['sprite']]
assert len(old)==200
result={'schemaVersion':existing['schemaVersion'],'sprites':old+heads}
assert len({(e['texture'],e['sprite'],e['width'],e['height']) for e in result['sprites']})==488
(docs/'art-layout.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
(docs/'scale-calibration.json').write_text(json.dumps({'schemaVersion':1,'nativePixelsUnmodified':True,'scales':scale},indent=2)+'\n',encoding='utf-8')
out.write_text(json.dumps(result,separators=(',',':'))+'\n',encoding='utf-8')
print(json.dumps({'characters':9,'sheets':18,'newSprites':288,'headAnchors':488,'nativePngPreserved':True,'scales':scale},indent=2))
