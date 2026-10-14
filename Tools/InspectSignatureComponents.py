"""Read-only connected-component audit of generated sprite alpha."""
import json,csv
from pathlib import Path
from PIL import Image
import numpy as np
from scipy import ndimage
manifest=json.loads(Path('Tools/signature-art-generation.json').read_text())
reports=[]
for job in manifest['jobs']:
    mask=np.array(Image.open(job['destination']))[:,:,3]>160
    labels,n=ndimage.label(mask);sizes=np.bincount(labels.ravel());boxes=ndimage.find_objects(labels)
    parts=[dict(index=i,size=int(sizes[i]),x=int(boxes[i-1][1].start),y=int(boxes[i-1][0].start),w=int(boxes[i-1][1].stop-boxes[i-1][1].start),h=int(boxes[i-1][0].stop-boxes[i-1][0].start)) for i in range(1,n+1) if sizes[i]>500]
    reports.append(dict(id=job['id'],kind=job['kind'],parts=len(parts),edge=[p for p in parts if p['x']==0 or p['y']==0 or p['x']+p['w']==mask.shape[1] or p['y']+p['h']==mask.shape[0]],boxes=parts))
Path('Docs/SignatureWeapons/components.json').write_text(json.dumps(reports,indent=2))
print(json.dumps([{k:v for k,v in r.items() if k!='boxes'} for r in reports]))
