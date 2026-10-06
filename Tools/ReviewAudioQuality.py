"""Read-only PCM audit of shipped music and effects, not a listening assessment."""
from array import array
import hashlib
import json
import math
from pathlib import Path
import sys
import wave

root = Path(__file__).resolve().parents[1]
rows = []
for path in sorted((root / 'Assets/TalesTactics/Audio').rglob('*.wav')):
    with wave.open(str(path), 'rb') as source:
        channels, width, rate = source.getnchannels(), source.getsampwidth(), source.getframerate()
        if width != 2:
            raise ValueError(f'Expected signed 16-bit PCM: {path}')
        pcm = array('h', source.readframes(source.getnframes()))
        if sys.byteorder != 'little':
            pcm.byteswap()
    if not pcm:
        raise ValueError(f'Empty audio: {path}')
    rows.append(dict(file=path.relative_to(root).as_posix(), channels=channels, rate=rate,
                     seconds=len(pcm)/channels/rate, peak=max(abs(x) for x in pcm)/32768,
                     rms=math.sqrt(sum(x*x for x in pcm)/len(pcm))/32768,
                     clipped_samples=sum(abs(x) >= 32767 for x in pcm),
                     boundary_step=max(abs(pcm[c]-pcm[-channels+c]) for c in range(channels))/32768,
                     sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
issues = [r['file'] for r in rows if r['clipped_samples'] or r['rms'] == 0]
report = dict(files=rows, issues=issues,
              limitation='Source PCM only. Does not measure mixed output peaks, device output or subjective audio quality.')
output = root / 'Docs/PresentationQuality/audio-audit.json'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
print(f'{len(rows)} WAV files; {len(issues)} empty/clipped/silent issues; {output}')
if issues:
    raise SystemExit(1)
