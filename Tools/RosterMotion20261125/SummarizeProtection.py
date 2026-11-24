"""Publish count-only preservation and nonprivate Editor state from strict local proofs."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path('C:/dev/TalesTactics')
LOCAL = ROOT / 'Logs/RosterMotion20261125'
PUBLIC = ROOT / 'Docs/RosterMotion20261125/Validation'
BASELINE_SHA = 'cb2d688f2e12323568986d884a584c459dd5acd9ef9aba74999402772e7f955c'
FREEZE_SHA = 'bd115915a0a6dc064591de2b0ebd455e0ab95dcdba6e97731795d52d62887214'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def require(condition, message):
    if not condition:
        raise ValueError(message)

def evidence(path):
    require(path.resolve().is_relative_to(LOCAL.resolve()), 'Local evidence scope required')
    return {'file': path.relative_to(LOCAL).as_posix(), 'sha256': sha(path)}

def write_new(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, ensure_ascii=False, indent=2)
        stream.write('\n')

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--protection', type=Path, required=True)
    parser.add_argument('--editor', type=Path, required=True)
    args = parser.parse_args()
    protection = read(args.protection)
    editor_envelope = read(args.editor)
    editor = editor_envelope['result']
    while isinstance(editor, dict) and 'result' in editor:
        require(editor.get('success', True) is True, 'Editor eval failed')
        editor = editor['result']
    freeze_path = LOCAL / 'validation-product-freeze.json'
    restore_path = LOCAL / 'BeforeBuildRegressionRestore/result.json'
    baseline_path = LOCAL / 'Baseline-bdadaad52bb0415ea7e0fe660d597031/baseline.json'
    restore = read(restore_path)
    require(sha(baseline_path) == BASELINE_SHA and sha(freeze_path) == FREEZE_SHA, 'Baseline or product freeze changed')
    require(protection['passed'] is True and not protection['failures'], 'Strict preservation failed')
    require(len(protection['checks']) == 13 and all(c['passed'] is True for c in protection['checks']), 'All thirteen preservation checks including both builds required')
    require(len(protection['frozenProducts']) == 166 and all(p['match'] is True for p in protection['frozenProducts']), '83 product files in both checkouts required')
    require(len(protection['protected']) == 4181, 'Protected original count differs')
    require(len(protection['originalModifications']) == 35 and all(p['match'] is True for p in protection['originalModifications']), 'Original dirty file bytes differ')
    require(len(protection['originalArt']) == 946 and all(p['match'] is True for p in protection['originalArt']), 'Original artwork differs')
    require(len(protection['existingApks']) == 6 and all(p['match'] is True for p in protection['existingApks']), 'Preexisting APKs differ')
    tree = protection['userTree']
    require(tree['beforeCount'] == tree['currentCount'] == 5597, 'Original user-tree file count differs')
    require(all(not tree[name] for name in ['addedFiles','changedFiles','deletedFiles','newDirectories','deletedDirectories']), 'User tree is not exactly restored')
    require(restore['passed'] is True and restore['applied'] is True and restore['archivedNewFileCount'] == 65 and restore['archivedNewDirectoryCount'] == 35 and restore['existingFilesDeleted'] == 0 and restore['restoredValidationCsvCount'] == 2 and restore['restoredPriorXML'] is True, 'Regression restoration evidence differs')
    require(editor_envelope['success'] is True and editor_envelope['cliExitCode'] == 0 and editor['passed'] is True and len(editor['checks']) == 19 and all(editor['checks']), 'All nineteen Editor state checks required')
    require(not editor['temporaryObjects'] and not editor['activeSceneDirectors'], 'Temporary verification scene remains')
    report = {
        'schema': 1, 'passed': True, 'scope': 'Prepublication check against the immutable 83-file product freeze, after both builds and restoration. A later local final check covers the full selected publication manifest before commit.',
        'checkedUtc': protection['checkedUtc'], 'baselineSha256': BASELINE_SHA, 'sourceFreezeSha256': FREEZE_SHA,
        'checkCount': 13, 'failedCheckCount': 0, 'sourceFileCount': 83, 'sourceCheckoutComparisons': 166,
        'protectedOriginalFileCopies': 4181, 'originalRootDirtyFileCount': 12, 'originalValidationDirtyFileCount': 23,
        'originalArtworkFileCount': 946, 'preexistingApkCount': 6, 'actualUserSaveFileCount': 9,
        'wholeUserTreeUnchanged': True, 'userTreeBeforeFileCount': 5597, 'userTreeFinalFileCount': 5597,
        'userTreeExcludedRootFiles': ['Player.log','Player-prev.log'], 'excludeScope': 'Only the two player execution logs at the root of the user-data directory are excluded; all 5597 baseline files and all subdirectories are compared.',
        'finalAddedFiles': 0, 'finalChangedFiles': 0, 'finalDeletedFiles': 0, 'finalAddedDirectories': 0, 'finalDeletedDirectories': 0,
        'regressionArtifacts': {'archivedGeneratedFiles':65,'archivedGeneratedDirectories':35,'existingFilesDeleted':0,'restoredValidationCsvFiles':2,'priorDiagnosticXmlRestored':True,'scope':'Only new StorageTests artifacts from this validation run were archived and removed from the user tree. Original user files were retained.'},
        'checkNames': [c['name'] for c in protection['checks']], 'evidence': [evidence(args.protection),evidence(restore_path)],
        'privacy':'Only counts, nonprivate state and evidence hashes are published. User paths, file lists, save content and supplied-music hashes remain local.'
    }
    fields = ['checkedUtc','play','paused','background','playerBackground','development','activeBuildTarget','optionsEnabled','options','scene','sceneName','sceneRootCount','dirty','pipeline','autoTickSessionFallback','autoTickIntervalSessionFallback','actualAutoTick','actualAutoTickInterval','temporaryObjects','activeSceneDirectors','ownedRefreshLock','androidTextureRecoveryPending']
    editor_report = {'schema':1,'passed':True,'checkCount':19,'failedCheckCount':0,'scope':'Validation checkout returned to its initial stopped PC Editor state; the original ROOT Editor was not opened. No standalone player or physical phone was exercised.','details':{key:editor[key] for key in fields},'checks':editor['checks'],'evidence':evidence(args.editor)}
    outputs = [PUBLIC/'protection-summary.json',PUBLIC/'editor-state.json']
    require(all(not path.exists() for path in outputs), 'Refusing to overwrite a published summary')
    write_new(outputs[0], report)
    write_new(outputs[1], editor_report)
    print(json.dumps({'passed':True,'outputs':[{'file':p.relative_to(ROOT).as_posix(),'sha256':sha(p)} for p in outputs]}))

if __name__ == '__main__':
    main()
