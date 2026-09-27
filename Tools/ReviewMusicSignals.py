"""Read-only PCM checks; these measurements do not represent listening approval."""
from array import array
import hashlib
import json
import math
from pathlib import Path
import sys
import wave

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'Docs/original-music-manifest.json').read_text(encoding='utf-8-sig'))
rows = []
for entry in manifest:
    path = root / entry['file']
    with wave.open(str(path), 'rb') as source:
        channels, width, rate = source.getnchannels(), source.getsampwidth(), source.getframerate()
        if channels != 2 or width != 2 or rate != 44100:
            raise ValueError(f'Unexpected PCM format: {path.name}')
        samples = array('h', source.readframes(source.getnframes()))
        if sys.byteorder != 'little':
            samples.byteswap()
    if not samples:
        raise ValueError(f'Empty audio: {path.name}')
    peak = max(abs(value) for value in samples) / 32768
    rms = math.sqrt(sum(value * value for value in samples) / len(samples)) / 32768
    seam = max(abs(samples[c] - samples[-channels + c]) for c in range(channels)) / 32768
    dc = [sum(samples[c::channels]) / (len(samples) // channels) / 32768 for c in range(channels)]
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    rows.append(dict(id=entry['id'], seconds=len(samples)/channels/rate, peak=peak, rms=rms,
                     boundary_step=seam, dc=dc, clipped_samples=sum(abs(v) >= 32767 for v in samples),
                     original_unchanged=digest.lower() == entry['sha256'].lower()))
if len(rows) != 14 or any(r['clipped_samples'] or not r['original_unchanged'] for r in rows):
    raise ValueError('Music integrity check failed')
output = root / 'Docs/music-signal-review.json'
output.write_text(json.dumps(rows, indent=2) + '\n', encoding='utf-8')
print(f'{len(rows)} tracks: unchanged, no clipped PCM samples; report: {output}')
