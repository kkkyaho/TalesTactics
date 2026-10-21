"""Audit and measure SD replacement sheets. Never changes PNGs or Unity .meta files.
The production manifest enumerates existing sprite identities before replacements.
"""
from pathlib import Path
import argparse, hashlib, json, re, sys
import numpy as np
from PIL import Image
from MeasureCharacterPoses import contour

PROJECT = Path(__file__).resolve().parents[1]
ART = PROJECT / 'Assets/TalesTactics/Art'
DOCS = PROJECT / 'Docs/SdCharacters'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def read_sprites(path):
    text = path.with_suffix(path.suffix + '.meta').read_text(encoding='utf-8-sig')
    guid = re.search(r'^guid: ([a-f0-9]{32})', text, re.M).group(1)
    ppu = float(re.search(r'^  spritePixelsToUnits: ([\d.]+)', text, re.M).group(1))
    records = []
    for block in re.split(r'\n    - serializedVersion: \d+\n', text)[1:]:
        name = re.search(r'^      name: (.+)$', block, re.M)
        if not name:
            continue
        rect = re.search(r'      rect:\n        serializedVersion: \d+\n        x: ([-\d.]+)\n        y: ([-\d.]+)\n        width: ([\d.]+)\n        height: ([\d.]+)', block)
        pivot = re.search(r'      pivot: \{x: ([-\d.]+), y: ([-\d.]+)\}', block)
        sprite_id = re.search(r'^      spriteID: (\w+)', block, re.M)
        internal_id = re.search(r'^      internalID: (-?\d+)', block, re.M)
        if rect and pivot and sprite_id and internal_id:
            records.append(dict(name=name.group(1), rect=[float(v) for v in rect.groups()], pivot=[float(v) for v in pivot.groups()], spriteId=sprite_id.group(1), internalId=internal_id.group(1)))
    return guid, ppu, records

def inventory():
    DOCS.mkdir(parents=True, exist_ok=True)
    reference_text = '\n'.join(p.read_text(encoding='utf-8-sig') for root in (PROJECT/'Assets/TalesTactics/Content', ART/'SignatureWeapons') for p in root.rglob('*.asset'))
    references = set(re.findall(r'fileID: (-?\d+), guid: ([a-f0-9]{32})', reference_text))
    entries = []
    for folder in ('PixelCampaign', 'MotionPremium', 'Crossover', 'SignatureWeapons'):
        for path in sorted((ART/folder).glob('*.png')):
            guid, ppu, sprites = read_sprites(path)
            active = [s for s in sprites if (s['internalId'], guid) in references]
            if not active:
                continue
            rel = path.relative_to(PROJECT).as_posix()
            entries.append(dict(source=rel, sourceGuid=guid, sourceSha256=digest(path), category=folder, spriteCount=len(sprites), activeSpriteCount=len(active), spriteNames=[s['name'] for s in sprites], activeSpriteNames=[s['name'] for s in active], candidate='', measurement='', status='pending', visualApproved=False))
    document = dict(schemaVersion=1, description='All currently referenced character and signature art; no claim of completed conversion.', applyPolicy='Only a complete visually approved manifest may replace production textures. Preserve every existing sprite name/ID and gameplay clip.', totals=dict(sheets=len(entries), spriteNames=sum(e['spriteCount'] for e in entries), activeSpriteNames=sum(e['activeSpriteCount'] for e in entries)), jobs=entries)
    path = DOCS/'production-manifest.json'
    if path.exists():
        old = {j['source']:j for j in json.loads(path.read_text(encoding='utf-8'))['jobs']}
        for entry in entries:
            if entry['source'] in old:
                for key in ('candidate', 'measurement', 'status', 'visualApproved'):
                    entry[key] = old[entry['source']].get(key, entry[key])
    path.write_text(json.dumps(document, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(document['totals']))

def components(mask):
    available = mask.copy()
    result = []
    height, width = mask.shape
    for y,x in zip(*np.where(available)):
        if not available[y,x]:
            continue
        available[y,x] = False
        todo=[(int(x),int(y))]
        pixels=[]
        while todo:
            xx,yy=todo.pop()
            pixels.append((xx,yy))
            for nx,ny in ((xx-1,yy),(xx+1,yy),(xx,yy-1),(xx,yy+1)):
                if 0<=nx<width and 0<=ny<height and available[ny,nx]:
                    available[ny,nx]=False
                    todo.append((nx,ny))
        if len(pixels)>=8:
            result.append(np.array(pixels,dtype=np.int32))
    return sorted(result,key=len,reverse=True)

def measure(source, candidate, output):
    source = PROJECT/source
    candidate = PROJECT/candidate
    guid, old_ppu, old = read_sprites(source)
    # The source importer order is authoritative, including corrected direction strips and icons.
    original_image = Image.open(source)
    image = Image.open(candidate)
    assert image.mode=='RGBA', 'A real alpha channel is required'
    alpha=np.array(image)[:,:,3]
    mask=alpha>128
    height,width=mask.shape
    assert 0.02<float(mask.mean())<0.75 and float(np.mean(alpha==0))>.15, 'Missing transparent background'
    sx=width/original_image.width;sy=height/original_image.height
    assert abs(sx-sy)<.03, 'Canvas aspect changed'
    parts=components(mask)
    majors=[part for part in parts if len(part)>1000]
    expected=np.array([[(s['rect'][0]+s['rect'][2]/2)*sx,(original_image.height-s['rect'][1]-s['rect'][3]/2)*sy] for s in old])
    # Some authored sheets retain a fourth direction which the Unity importer
    # intentionally excludes in favor of a corrected strip. Keep those drawings,
    # but do not assign their pixels to an adjacent imported sprite.
    unused=[]
    for source_part in components(np.array(original_image)[:,:,3]>128):
        if len(source_part)<=1000:
            continue
        center=(source_part.min(axis=0)+source_part.max(axis=0))/2
        contained=any(s['rect'][0]-4<=center[0]<=s['rect'][0]+s['rect'][2]+4
            and original_image.height-s['rect'][1]-s['rect'][3]-4<=center[1]<=original_image.height-s['rect'][1]+4 for s in old)
        if not contained:
            unused.append(center*np.array([sx,sy]))
    all_expected=np.concatenate([expected,np.array(unused)]) if unused else expected
    ignored_major_counts=[0 for _ in unused]
    # An authored frame may contain two disconnected gloves, a bowstring or a dropped
    # sword. Source sprite rectangles, rather than component count, define frames.
    assigned=[[] for _ in old]
    major_counts=[0 for _ in old]
    for part in parts:
        center=(part.min(axis=0)+part.max(axis=0))/2
        distance=((all_expected-center)**2).sum(axis=1)
        order=np.argsort(distance)
        index=int(order[0])
        if len(part)>1000:
            if len(order)>1 and distance[order[1]]-distance[index]<25:
                raise AssertionError('Ambiguous large component between source frames')
            if index>=len(old):
                ignored_major_counts[index-len(old)]+=1
            else:
                major_counts[index]+=1
        if index<len(old):
            assigned[index].append(part)
    assert all(ignored_major_counts), 'An unimported source figure was removed or merged'
    assert all(major_counts), f'Missing or touching source frame(s): {[old[i]["name"] for i,n in enumerate(major_counts) if n==0]}'
    assigned=[np.concatenate(group) for group in assigned]
    for record,part in zip(old,assigned):
        span=part.max(axis=0)-part.min(axis=0)+1
        assert span[0]<record['rect'][2]*sx*1.65 and span[1]<record['rect'][3]*sy*1.65, 'Possible extra/merged figure: '+record['name']
    standing=[i for i,s in enumerate(old) if len(s['name'].rsplit('_',2))==3 and s['name'].rsplit('_',2)[-2]=='0'] or list(range(len(old)))
    scale=np.median([(assigned[i][:,1].max()-assigned[i][:,1].min()+1)/old[i]['rect'][3] for i in standing])
    ppu=old_ppu*float(scale)
    frames=[]
    problems=[]
    for record,part,expected_center in zip(old,assigned,expected):
        l,t=part.min(axis=0);r,b=part.max(axis=0)+1
        l=max(0,int(l)-2);t=max(0,int(t)-2);r=min(width,int(r)+2);b=min(height,int(b)+2)
        if l==0 or t==0 or r==width or b==height:
            problems.append(record['name']+': canvas edge')
        w,h=r-l,b-t
        selection=np.zeros((h,w),dtype=bool)
        selection[part[:,1]-t,part[:,0]-l]=True
        feet=part[part[:,1]>b-h*.18]
        foot_x=float(np.median(feet[:,0])) if len(feet) else (l+r)/2
        source_anchor_x=(record['rect'][0]+record['rect'][2]*record['pivot'][0])*sx
        pivot_x=(source_anchor_x-l)/w
        is_character=bool(re.search(r'_\d+_\d+$', record['name']))
        source_anchor_y=(original_image.height-record['rect'][1]-record['rect'][3]*record['pivot'][1])*sy
        pivot_y=.01 if is_character else (b-source_anchor_y)/h
        polys=contour(selection,all_parts=True)
        assert polys, 'Missing outline'
        outlines=[dict(points=[dict(x=round(float(x-w/2),3),y=round(float(h/2-y),3)) for x,y in poly]) for poly in polys]
        frame=dict(name=record['name'], originalSpriteId=record['spriteId'], originalInternalId=record['internalId'], x=l,y=height-b,width=w,height=h,pivotX=round(pivot_x,6),pivotY=pivot_y,outlines=outlines,centerDriftPixels=round(float(np.linalg.norm((part.min(axis=0)+part.max(axis=0))/2-expected_center)),3),footAnchorDeltaPixels=round(foot_x-(record['rect'][0]+record['rect'][2]*record['pivot'][0])*sx,3))
        frames.append(frame)
    assert not problems, problems
    data=dict(schemaVersion=1,source=source.relative_to(PROJECT).as_posix(),candidate=candidate.relative_to(PROJECT).as_posix(),sourceGuid=guid,sourceSha256=digest(source),candidateSha256=digest(candidate),width=width,height=height,pixelsPerUnit=round(ppu,6),alphaZeroFraction=round(float(np.mean(alpha==0)),6),solidFraction=round(float(mask.mean()),6),frameCount=len(frames),majorComponentCount=len(majors),majorComponentsPerFrame=major_counts,unusedSourceFigureCount=len(unused),unusedSourceFigureCenters=[p.tolist() for p in unused],ignoredCandidateMajorCounts=ignored_major_counts,directionOrder=['front','back','right','left'],visualApproved=False,frames=frames)
    output=PROJECT/output
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:data[k] for k in ('frameCount','width','height','pixelsPerUnit','alphaZeroFraction')}))
    print(json.dumps(dict(maxCenterDrift=max(f['centerDriftPixels'] for f in frames),maxFootDrift=max(abs(f['footAnchorDeltaPixels']) for f in frames),outlineCount=sum(len(f['outlines']) for f in frames))))

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    sub=parser.add_subparsers(dest='command',required=True)
    sub.add_parser('inventory')
    a=sub.add_parser('approve');a.add_argument('measurement');a.add_argument('note')
    m=sub.add_parser('measure');m.add_argument('source');m.add_argument('candidate');m.add_argument('output')
    args=parser.parse_args()
    if args.command=='inventory':inventory()
    elif args.command=='approve':
        path=PROJECT/args.measurement
        data=json.loads(path.read_text(encoding='utf-8'))
        assert digest(PROJECT/data['candidate'])==data['candidateSha256']
        assert len(args.note)>20, 'Record concrete visual comparison evidence'
        data['visualApproved']=True;data['visualReview']=args.note
        path.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
        print('Reviewed '+data['candidate'])
    else:measure(args.source,args.candidate,args.output)