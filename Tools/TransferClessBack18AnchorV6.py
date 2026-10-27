"""Explicit v5 -> v6 Back18 correction transfer; new JSON outputs only.

Usage: python Tools/TransferClessBack18AnchorV6.py OUTPUT.json REPORT.json
This fixed-evidence utility is intentionally not reusable for an unreviewed atlas.
"""
import argparse
import copy
import hashlib
import json

import numpy as np
from PIL import Image

from MeasureSdCharacterArt import digest
from RefineSdMotionAnchors import project_path, require

ROOT = 'Docs/SdCharacters/2026-10-24/'
FILES = {
    ROOT + 'cless-native-v5-calibrated220.json': '02e7482b5595ba718f866c47b193b02cbecbfc246a3aa48c3a12466d6704ca94',
    ROOT + 'cless-native-v5-back18-report.json': 'a9d84da51504ececc6a21e65df7c0345bb43a01e2a165bfa4259122791bfa86e',
    ROOT + 'cless-native-v6-measurement.json': 'e6970dd47368d468c78af41669461772da7d2c095a5e3590e404fff61dea62e5',
    ROOT + 'cless-native-v6-calibrated220.json': '1aaf7eb515a29fec930ebec5e572884dd1fbf7d0d1fc3608e2d123f06add6a4f',
    'Assets/TalesTactics/Art/SdCharacters/cless-native-v5.png': '0ffd2c058365aa5fb3d2659a837053c950d84083704973baf94ba16d1a221d86',
    'Assets/TalesTactics/Art/SdCharacters/cless-native-v6.png': 'c6bd896f69d75e1f9df5d8d22af72bb265693777181b3e94c081e5f902c2be44',
}


def transfer():
    for name, expected in FILES.items():
        require(digest(project_path(name)) == expected, 'Fixed evidence hash differs: ' + name)
    def read(name):
        return json.loads(project_path(ROOT + name).read_text(encoding='utf-8-sig'))
    v5, v6 = read('cless-native-v5-calibrated220.json'), read('cless-native-v6-calibrated220.json')
    proof = read('cless-native-v5-back18-report.json')
    require('poseAnchorCorrection' not in v6 and v5['pixelsPerUnit'] == v6['pixelsPerUnit'] == 220 and
            v5['frameCount'] == v6['frameCount'] == len(v5['frames']) == len(v6['frames']) == 80 and
            v5['sourceGuid'] == v6['sourceGuid'] and v5['sourceSha256'] == v6['sourceSha256'],
            'Wrong atlas identity, scale, count or repeated correction')
    arrays = []
    for data in (v5, v6):
        require(data['candidateSha256'] == FILES[data['candidate']], 'Measurement candidate differs')
        with Image.open(project_path(data['candidate'])) as image:
            require(image.mode == 'RGBA' and image.size == (4096, 4096), 'Unexpected canvas')
            arrays.append(np.array(image))
    rows = []
    for old, new in zip(v5['frames'], v6['frames']):
        keys = ('name', 'originalSpriteId', 'originalInternalId')
        require(all(old[k] == new[k] for k in keys), 'Frame names/IDs/order changed')
        if old['name'] == 'cless_19_2':
            continue  # Root-reviewed one-frame art revision; no Back18 evidence transfer from it.
        require(old == new, 'One of the other 79 frame records changed: ' + old['name'])
        crops = []
        for frame, array in zip((old, new), arrays):
            x, y, w, h = [frame[k] for k in ('x', 'y', 'width', 'height')]
            crops.append(array[4096-y-h:4096-y, x:x+w])
        require(np.array_equal(*crops), 'Unchanged frame RGBA differs: ' + old['name'])
        yy, xx = np.nonzero(crops[0][:, :, 3] > 128)
        bbox = [int(xx.min()), int(yy.min()), int(xx.max()) + 1, int(yy.max()) + 1]
        rows.append(dict(name=old['name'], allFrameFieldsIdentical=True, rgbaIdentical=True,
                         rect=[old[k] for k in ('x', 'y', 'width', 'height')],
                         translationPixels=[0, 0], alpha128BboxInRectTopOrigin=bbox,
                         rectRgbaSha256=hashlib.sha256(crops[0].tobytes()).hexdigest()))
    require(len(rows) == 79, 'Expected exactly 79 identical frames')
    for name in ('cless_17_1', 'cless_18_1', 'cless_19_1'):
        reference = next(r for r in proof['atlasFrameEvidence'] if r['name'] == name)
        current = next(r for r in rows if r['name'] == name)
        require(all(reference[k] == current[k] for k in ('rect', 'rectRgbaSha256', 'alpha128BboxInRectTopOrigin')),
                'Back evidence does not match the reviewed v5 report')
    require(proof['targetSprite'] == 'cless_18_1' and proof['appliedAnchorDeltaPixels'] == 67.501504,
            'Unexpected explicit correction evidence')
    output = copy.deepcopy(v6)
    frame = next(f for f in output['frames'] if f['name'] == 'cless_18_1')
    previous = copy.deepcopy(frame)
    frame['pivotX'] = round(frame['pivotX'] + proof['appliedAnchorDeltaPixels'] / frame['width'], 6)
    require(0 <= frame['pivotX'] <= 1 and frame['pivotX'] == proof['newPivotX'], 'Invalid transferred pivot')
    applied = (frame['pivotX'] - previous['pivotX']) * frame['width']
    frame['footAnchorDeltaPixels'] = round(frame['footAnchorDeltaPixels'] - applied, 3)
    for before, after in zip(v6['frames'], output['frames']):
        for key in before:
            if before['name'] == 'cless_18_1' and key in ('pivotX', 'footAnchorDeltaPixels'):
                continue
            require(before[key] == after[key], 'Unintended metadata change')
    provenance = dict(method='cless-v6-exact-content-back18-transfer-v1', targetSprite='cless_18_1',
                      evidenceHashes=FILES, comparedUnchangedFrameCount=79,
                      previousPivotX=previous['pivotX'], pivotX=frame['pivotX'], appliedAnchorDeltaPixels=applied,
                      v5CorrectionReport=ROOT + 'cless-native-v5-back18-report.json',
                      inputMeasurement=ROOT + 'cless-native-v6-calibrated220.json',
                      supersedes='Old bbox-relative world-X preservation only for cless_18_1; the reviewed body anchor replaces it.',
                      unchanged='All v6 PNG pixels, PPU220, 80 IDs/names, rects, pivotY and outline coordinates; 79 entire v6 frame records.',
                      limitation='Exact-content transfer of prior actual-capture evidence. This is not a new rendered continuity pass; actual v6 playback must be reviewed.')
    output['poseAnchorCorrection'] = provenance
    output['visualApproved'] = False
    output.pop('visualReview', None)
    report = dict(schemaVersion=1, **provenance, comparisons=rows,
                  v5PredictedTransitions=proof['transitions'], requiresActualMotionReview=True)
    for name, expected in FILES.items():
        require(digest(project_path(name)) == expected, 'Evidence changed during transfer: ' + name)
    return output, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output'); parser.add_argument('report')
    args = parser.parse_args()
    paths = [project_path(args.output), project_path(args.report)]
    require(paths[0] != paths[1] and all(p.suffix.lower() == '.json' and not p.exists() for p in paths),
            'Output/report must be distinct new JSON files')
    output, report = transfer()
    created = []
    try:
        for path, data in zip(paths, (output, report)):
            path.parent.mkdir(parents=True, exist_ok=True)
            with path.open('x', encoding='utf-8', newline='\n') as stream:
                created.append(path); json.dump(data, stream, indent=2, allow_nan=False); stream.write('\n')
    except BaseException:
        for path in created:
            path.unlink()
        raise
    print(json.dumps({k: report[k] for k in ('comparedUnchangedFrameCount', 'previousPivotX', 'pivotX', 'appliedAnchorDeltaPixels')}, indent=2))


if __name__ == '__main__':
    main()
