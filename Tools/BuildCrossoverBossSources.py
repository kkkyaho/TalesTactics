"""Copy approved source names/notes to C#; this does not modify Unity assets."""
import json
from pathlib import Path
r=json.loads(Path('Docs/BossConcepts/roster.json').read_text(encoding='utf-8-sig'))
q=lambda s:json.dumps(s,ensure_ascii=False)
lines=['// Generated from Docs/BossConcepts/roster.json by Tools/BuildCrossoverBossSources.py.', 'namespace TalesTactics { public static class CrossoverBossSources {', 'public static readonly string[][] Entries = {']
for b in r['bosses']:
    fields=[b['id'].replace('guardian','golem'),b['name'],b['group'],b['visual']]
    for skill in b['skills']:fields.extend([skill[0],skill[1]+' 기술명 · 전술 효과는 본작 재설계: '+skill[2]])
    fields.extend([' · '.join(b['links']),b['counter']])
    lines.append('new[]{'+','.join(map(q,fields))+'},')
lines.append('}; } }')
Path('Assets/TalesTactics/Runtime/Core/CrossoverBossSources.Generated.cs').write_text('\n'.join(lines)+'\n',encoding='utf-8')
