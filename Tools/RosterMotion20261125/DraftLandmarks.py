"""Draft native-pixel landmarks and an HTML overlay; never alters PNG pixels."""
import json,sys,importlib.util
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('old_head',ROOT/'Tools/BuildUnitHeadAnchors.py')
old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
spec2=importlib.util.spec_from_file_location('measure',ROOT/'Tools/RosterMotion20261125/MeasureRosterArt.py')
measure=importlib.util.module_from_spec(spec2);spec2.loader.exec_module(measure)
report=json.loads((ROOT/'Logs/RosterMotion20261125/art-measurements.json').read_text(encoding='utf-8'))
out=ROOT/'Logs/RosterMotion20261125/LandmarkReview';out.mkdir(exist_ok=True)
landmarks=[]
for s in report['sheets']:
 if s['characterId'] not in ('cless','farah','jade','kisara','velvet'):continue
 im=np.asarray(Image.open(ROOT/s['path']).convert('RGBA'));marks=[]
 for f in s['frames']:
  b=f['alpha64BBoxTopLeft'];x,y,w,h=(b[k] for k in ('x','y','width','height'))
  mask=im[y:y+h,x:x+w,3]>=128
  rgb=im[y:y+h,x:x+w,:3].astype(float);r,g,b=rgb[:,:,0],rgb[:,:,1],rgb[:,:,2]
  cid=s['characterId']
  if cid=='farah': hair=(g>r*1.15)&(g>b*1.02)&(g>35)
  elif cid=='jade':hair=(r>g*1.12)&(g>b*1.05)&(r>45)&(r<245)
  elif cid=='velvet':hair=(b>r*.9)&(r>g*1.05)&(rgb.max(axis=2)<190)
  else:hair=(r>100)&(g>60)&(r>g*1.05)&(g>b*1.25)&(r-b>40)
  hair &= mask
  hair[int(h*.6):]=False
  comps=measure.components(hair)
  if not comps:raise ValueError('No hair: '+f['name'])
  haircomp=comps[0];hairtop=haircomp['y0'];band=[(a,c,yy) for a,c,yy in haircomp['runs'] if yy<=hairtop+max(8,int(h*.07))]
  hx=np.median([xx for a,c,yy in band for xx in range(a,c)])+.5
  if cid=='jade':
   center=792.0-x if f['name']=='jade-roster-attack-back-2' else float(hx)
   restricted=hair.copy();restricted[:,:max(0,int(center-30))]=False;restricted[:,min(w,int(center+31)):]=False
   hc=measure.components(restricted)[0];hairtop=hc['y0']
   band=[(a,c,yy) for a,c,yy in hc['runs'] if yy<=hairtop+12]
   hx=np.median([xx for a,c,yy in band for xx in range(a,c)])+.5
  head={'x':round(x+float(hx),3),'y':y+hairtop}
  overrides={'kisara-roster-attack-right-0':(181,650),'kisara-roster-attack-left-0':(131,960)}
  if f['name'] in overrides:head=dict(zip(('x','y'),overrides[f['name']]))
  if cid=='velvet':
   u,v,_=old.anchor(mask);head={'x':round(x+u*w,3),'y':round(y+(1-v)*h,3)}
  dark=mask&(rgb.max(axis=2)<150)&((rgb.max(axis=2)-rgb.min(axis=2))<95)
  dark[:int(h*.80)]=False
  possible=[yy for yy in range(h) if any(hi-lo>=8 for lo,hi in old.runs(dark[yy]))]
  if not possible:raise ValueError('No sole: '+f['name'])
  fy=max(possible)
  strip=dark[max(0,fy-max(8,int(h*.12))):fy+1]
  groups=[(a,c) for a,c in old.runs(strip.any(axis=0)) if c-a>=10 and strip[:,a:c].sum()>=20]
  if not groups:raise ValueError('No boot groups: '+f['name'])
  foot={'x':round(x+(groups[0][0]+groups[-1][1])/2,3),'y':y+fy+1}
  lm={'name':f['name'],'head':head,'foot':foot,'source':s['path'],'note':'Draft broad crown and lower support groups; review before approval'}
  landmarks.append(lm)
  marks.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="none" stroke="#ffffff55"/><circle cx="{head["x"]}" cy="{head["y"]}" r="5" fill="red" stroke="white"/><path d="M{foot["x"]-10},{foot["y"]}h20 M{foot["x"]},{foot["y"]-8}v16" stroke="#00ffff" stroke-width="2"/>')
 body=f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {s["width"]} {s["height"]}" width="900" height="900"><rect width="100%" height="100%" fill="#444a52"/><image href="{(ROOT/s["path"]).as_uri()}" width="{s["width"]}" height="{s["height"]}"/>{"".join(marks)}</svg>'
 (out/(s['characterId']+'-'+s['kind']+'.html')).write_text('<!doctype html><html><meta charset="utf-8"><style>body{margin:0;background:#222}svg{display:block}</style>'+body,encoding='utf-8')
(out/'draft-five.json').write_text(json.dumps({'frames':landmarks},indent=2),encoding='utf-8')
print(json.dumps({'frames':len(landmarks),'overlayPages':10,'output':str(out)}))
