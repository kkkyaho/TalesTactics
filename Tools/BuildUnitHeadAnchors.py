"""Measure directional HUD head anchors offline; requires Pillow and NumPy.

Writes reviewable JSON and provenance into --output only. It never alters art,
Unity import settings, or Content. Review weapon/tail exclusion windows when art
changes, then copy UnitHeadAnchors.json into Resources/TalesTactics explicitly.
"""
import re,json,hashlib,argparse
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
FRAME=re.compile(r'      name: ([^\r\n]+)\s+rect:\s+serializedVersion: \d+\s+x: ([\d.eE+-]+)\s+y: ([\d.eE+-]+)\s+width: ([\d.eE+-]+)\s+height: ([\d.eE+-]+)\s+alignment: \d+\s+pivot: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+)\}.*?      internalID: (-?\d+)',re.S)
REF=re.compile(r'\{fileID: (-?\d+), guid: ([0-9a-f]+), type: 3\}')
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def runs(row):
 d=np.diff(np.r_[False,row,False].astype(np.int8));return list(zip(np.where(d==1)[0],np.where(d==-1)[0]))
def anchor(mask):
 h,w=mask.shape; yy,xx=np.where(mask)
 if len(xx)<20:raise ValueError('Empty body silhouette')
 ymin,ymax=int(yy.min()),int(yy.max());bh=ymax-ymin+1
 middle=mask[ymin+int(bh*.28):ymin+int(bh*.72)]
 weights=middle.sum(axis=0);cx=float(np.sum(weights*np.arange(w))/max(1,weights.sum()))
 row_width=mask.sum(axis=1);threshold=max(8,float(np.percentile(row_width[row_width>0],85))*.20)
 selected=None
 for y in range(ymin,min(h,ymin+int(bh*.65))):
  choices=[(lo,hi) for lo,hi in runs(mask[y]) if hi-lo>=threshold and abs((lo+hi-1)*.5-cx)<=max(w*.28,bh*.12)]
  if choices:
   lo,hi=min(choices,key=lambda p:abs((p[0]+p[1]-1)*.5-cx));selected=(y,lo,hi);break
 if selected is None:raise ValueError('No broad central head region')
 y,lo,hi=selected
 # A top-only connected component removes a separated spear/staff while retaining
 # the actual hair/helmet crown above the first sufficiently broad row.
 bottom=min(h,y+max(8,int(bh*.10)));region=mask[:bottom]
 visited=np.zeros_like(region,dtype=np.bool_);todo=[(y,int((lo+hi-1)//2))];pixels=[]
 while todo:
  py,px=todo.pop()
  if py<0 or py>=bottom or px<0 or px>=w or visited[py,px] or not region[py,px]:continue
  visited[py,px]=True;pixels.append((py,px));todo.extend(((py-1,px),(py+1,px),(py,px-1),(py,px+1)))
 if not pixels:raise ValueError('Empty crown component')
 crown_top=min(p[0] for p in pixels)
 # Use the crown mass below its tip to avoid anchoring to one stray hair pixel.
 crown=[p[1] for p in pixels if crown_top+max(2,int(bh*.015))<=p[0]<=y+max(3,int(bh*.055))]
 headx=float(np.median(crown if crown else [p[1] for p in pixels]))+.5
 return headx/w,(h-crown_top)/h,{'bodyCenterX':round(cx,3),'firstBroadRow':y,'crownTop':crown_top,'threshold':round(threshold,3),'bbox':[int(xx.min()),ymin,int(xx.max()),ymax]}

HEAD_WINDOWS={
 'mage:Right':(.38,.62,.02,.15),
 'jade:Front':(.35,.62,.15,.29),'jade:Back':(.31,.56,.14,.27),
 'jade:Right':(.38,.64,.14,.27),'jade:Left':(.38,.63,.15,.29),
 'balseph:Front':(.53,.73,.10,.27),'balseph:Right':(.57,.79,.13,.29),'balseph:Left':(.18,.40,.18,.34),
 'tear:Front':(.32,.61,.04,.18),'tear:Left':(.43,.67,.06,.19),
 'executor:Front':(.37,.58,.10,.23),'executor:Back':(.37,.58,.09,.24),'executor:Left':(.36,.56,.12,.29),
 'maxwell:Front':(.32,.56,.09,.24),'maxwell:Right':(.33,.58,.10,.27),
 'polwigle:Front':(.57,.64,.38,.56),'polwigle:Back':(.65,.72,.35,.57),
 'polwigle:Right':(.55,.80,.20,.40),'polwigle:Left':(.20,.45,.22,.42)
}

def reviewed_anchor(mask,characters,u,v,details):
 match=next((c for c in characters if c in HEAD_WINDOWS),None)
 if not match:return u,v,details
 h,w=mask.shape;x0,x1,y0,y1=HEAD_WINDOWS[match]
 lo,hi,top,bottom=int(x0*w),int(x1*w),int(y0*h),int(y1*h)
 yy,xx=np.where(mask[top:bottom,lo:hi])
 if not len(yy):raise ValueError('Reviewed head window is empty: '+match)
 crown=int(yy.min())+top
 band=mask[crown:min(h,crown+max(3,int(h*.02))),lo:hi];_,bx=np.where(band)
 u=(float(np.median(bx))+lo+.5)/w
 if match.startswith('polwigle:'):u={'Front':.50,'Back':.50,'Right':.67,'Left':.34}[match.split(':')[1]]
 details['reviewedHeadWindow']=list(HEAD_WINDOWS[match]);details['crownTop']=crown
 return u,(h-crown)/h,details

def main(out):
 out.mkdir(parents=True,exist_ok=True)
 refs={};character_refs={}
 for asset in sorted((ROOT/'Assets/TalesTactics/Content').rglob('*.asset')):
  text=asset.read_text(encoding='utf-8-sig')
  if 'TalesTactics.CharacterData' not in text:continue
  block=re.search(r'^  Sprites:\s*\n(.*?)(?=^  \w|\Z)',text,re.M|re.S)
  if not block:continue
  char=re.search(r'^  Id: (.+)$',text,re.M)[1]
  for direction,line in re.findall(r'^    (Front|Back|Left|Right): (.+)$',block[1],re.M):
   match=REF.search(line)
   if not match:continue
   fid,guid=match.groups();key=(guid,fid);refs[key]=refs.get(key,[])+[char+':'+direction];character_refs.setdefault(char,{})[direction]=key
 # Include the corresponding equipped idle frames: weapons never change the chosen head.
 for asset in sorted((ROOT/'Assets/TalesTactics/Content').rglob('*.asset')):
  text=asset.read_text(encoding='utf-8-sig')
  if 'TalesTactics.EquipmentAppearance' not in text:continue
  for pair in re.findall(r'Original: (\{[^\n]+\})\s+Equipped: (\{[^\n]+\})',text):
   orig,eq=REF.search(pair[0]),REF.search(pair[1])
   if orig and eq and (orig[2],orig[1]) in refs:refs[(eq[2],eq[1])]=refs[(orig[2],orig[1])]+['equipped']
 metas={}
 for path in sorted((ROOT/'Assets/TalesTactics/Art').rglob('*.png.meta')):
  if '/SdCharacters/' in path.as_posix():continue
  txt=path.read_text(encoding='utf-8-sig');g=re.search(r'^guid: ([a-f0-9]+)',txt,re.M)
  if g and g[1] in {k[0] for k in refs}:metas[g[1]]=(path,txt)
 entries=[];evidence=[];resolved=set()
 for guid,(meta,text) in sorted(metas.items()):
  path=Path(str(meta)[:-5]);image=np.asarray(Image.open(path).convert('RGBA'));ih,iw=image.shape[:2]
  for f in FRAME.finditer(text):
   key=(guid,f[8])
   if key not in refs:continue
   name=f[1];x,y,w,h=[int(float(v)) for v in f.groups()[1:5]];mask=image[ih-y-h:ih-y,x:x+w,3]>128
   u,v,details=reviewed_anchor(mask,refs[key],*anchor(mask))
   entry={'texture':path.stem,'sprite':name,'u':round(u,7),'v':round(v,7),'width':w,'height':h};entries.append(entry);resolved.add(key)
   evidence.append(dict(entry,source=str(path.relative_to(ROOT)).replace('\\','/'),spriteId=f[8],sourceSha256=sha(path),metaSha256=sha(meta),characters=sorted(set(refs[key])),rect=[x,y,w,h],pivot=[float(f[6]),float(f[7])],analysis=details))
 missing=[{'guid':k[0],'fileId':k[1],'characters':v} for k,v in refs.items() if k not in resolved]
 if missing:raise ValueError('Unresolved directional frames: '+json.dumps(missing))
 assert len({(e['sprite'],e['width'],e['height']) for e in entries})==len(entries),'Ambiguous runtime head anchor keys'
 entries.sort(key=lambda e:(e['texture'],e['sprite'],e['width'],e['height']))
 (out/'UnitHeadAnchors.json').write_text(json.dumps({'schemaVersion':1,'sprites':entries},ensure_ascii=False,separators=(',',':')),encoding='utf-8')
 (out/'head-anchor-provenance.json').write_text(json.dumps({'schemaVersion':1,'algorithm':'alpha>128; broad central run; connected top crown component; head center median; offline only; artist-reviewed weapon/tail exclusion windows','spriteCount':len(entries),'sources':evidence},ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps({'sprites':len(entries),'uniqueCharacters':len(character_refs),'bytes':(out/'UnitHeadAnchors.json').stat().st_size,'output':str(out)}))
if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('--project',type=Path,default=ROOT);ap.add_argument('--output',type=Path,required=True)
 args=ap.parse_args();ROOT=args.project.resolve();main(args.output)
