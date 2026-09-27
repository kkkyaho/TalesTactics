"""Create the explicit presentation table; reads assets, never edits Unity YAML."""
from pathlib import Path
import re
import csv

# Ordered by each character's existing numbered skill IDs.
patterns={
 'cless':'Wave Rising Thrust Rising Wave Slash Wave',
 'mint':'Heal Heal Heal Burst Cleanse Revive Revive',
 'velvet':'Claw Rising Claw Claw',
 'farah':'Wave Burst Rising Burst Rising Wave Burst',
 'tear':'Heal Sleep Heal Barrier Pillar Revive Rain',
 'jade':'Pillar Burst Wave Lightning Rain Ice Meteor Lightning',
 'natalia':'Thrust Wave Heal Rain Rain Heal Revive',
 'alphen':'Wave Rising Thrust Slash Rising Wave Wave',
 'shionne':'Thrust Heal Burst Wave Heal Gravity Revive',
 'kisara':'Barrier Rising Thrust Burst Meteor Burst Ice'
}
finales=dict(zip(patterns,'SwordFinale TimeStop ClawFinale LionFinale Radiance Cage AstralRain FlameFinale Wildfire ShieldFinale'.split()))
rows=[]
for path in sorted(Path('Assets/TalesTactics/Content/Skills').glob('*.asset')):
    text=path.read_text(encoding='utf-8')
    identity=re.search(r'^  Id: (.+)$',text,re.M)[1].strip()
    owner,index=identity.split('.')
    basic=index=='attack';ultimate=index=='ultimate'
    if basic: pattern='Thrust' if owner in ('jade','natalia','shionne','enemy') else 'Slash'
    elif ultimate: pattern=finales[owner]
    else: pattern=patterns[owner].split()[int(index)]
    animation=int(re.search(r'^  Animation: (\d+)',text,re.M)[1])
    prepare=0.65 if ultimate else 0.12 if basic else 0.32 if animation==4 else 0.22
    recover=0.7 if ultimate else 0.3 if basic else 0.48
    pulses=3 if pattern in ('Rain','Meteor','Claw','Burst') else 2 if pattern in ('Wave','Lightning','Slash') else 1
    if ultimate: pulses=1 if pattern in ('TimeStop','Cage') else 3
    size=1.4 if ultimate else 0.7 if basic else 1
    rows.append([identity,pattern,pulses,prepare,recover,size])
assert len(rows)==89, 'Review new catalog entries before changing the presentation table'
with Path('Tools/skill-presentation.csv').open('w',newline='',encoding='utf-8') as f:
    writer=csv.writer(f);writer.writerow(['id','pattern','pulses','windup','recovery','size']);writer.writerows(rows)
print('89 explicit presentation profiles')
