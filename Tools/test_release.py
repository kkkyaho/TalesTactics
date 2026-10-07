"""Distribution tool tests; these are not Unity tests."""
import json
import subprocess
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

import PackageRelease as release


class DistributionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.build = self.root/'build'
        self.evidence = self.root/'evidence'
        self.evidence.mkdir()
        for name in release.REQUIRED:
            path = self.build/name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b'fixture-content')
        report = {'result': 'Succeeded', 'totalErrors': 0, 'platform': 'StandaloneWindows64',
                  'outputPath': str(self.build/'TalesTactics.exe'),
                  'files': [{'path': str(self.build/p), 'sizeBytes': (self.build/p).stat().st_size}
                            for p in release.REQUIRED]}
        (self.evidence/'windows-build.json').write_text(json.dumps({'data': {'result': json.dumps(report)}}))
        hashes = [{'Path': str(self.build/p), 'Hash': release.digest(self.build/p)} for p in (
            'TalesTactics.exe', 'TalesTactics_Data/Managed/TalesTactics.Runtime.dll')]
        (self.evidence/'build-hashes.json').write_text(json.dumps(hashes))
        self.notes = self.root/'notes.txt'
        self.notes.write_text('test notes')

    def pack(self, version='0.1.0-test.1'):
        with patch.object(release.subprocess, 'check_output', side_effect=['a'*40, '']):
            return release.package(self.build, self.evidence, version, self.root/'out', self.notes)

    def ps(self, script, *args, success=True):
        proc = subprocess.run(['powershell.exe', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File',
                               str(release.ROOT/'Tools/Distribution'/script), *map(str, args)],
                              capture_output=True, text=True, errors='replace')
        self.assertEqual(proc.returncode == 0, success, proc.stdout+proc.stderr)
        return proc.stdout.strip()

    def rewrite_zip(self, source, transform):
        dest = self.root/'changed.zip'
        with zipfile.ZipFile(source) as z:
            contents = {i.filename: z.read(i) for i in z.infolist()}
        transform(contents)
        with zipfile.ZipFile(dest, 'w') as z:
            for name, data in contents.items():
                z.writestr(name, data)
        return dest

    def test_pack_roundtrip_excludes_debug_artifacts_and_verifies_on_windows_powershell(self):
        private = self.build/'TalesTactics_BackUpThisFolder_ButDontShipItWithYourGame'
        private.mkdir()
        (private/'private.txt').write_text('must not ship')
        (self.build/'symbols.pdb').write_text('symbols')
        result = self.pack()
        self.assertTrue(result['passed'])
        self.assertEqual(result['excluded'], 2)
        with zipfile.ZipFile(result['archive']) as z:
            z.extractall(self.root/'installed')
            self.assertNotIn('symbols.pdb', z.namelist())
        self.ps('Verify-Package.ps1', '-Directory', self.root/'installed')

    def test_changed_archive_fails_hash(self):
        original = self.pack()['archive']
        changed = self.rewrite_zip(original, lambda c: c.update({'TalesTactics.exe': b'tampered'}))
        with self.assertRaises(ValueError):
            release.verify(changed)

    def test_missing_required_and_unlisted_files_fail(self):
        original = self.pack()['archive']
        for mutate in (lambda c: c.pop('UnityPlayer.dll'), lambda c: c.update({'unlisted.txt': b'x'})):
            with self.subTest(mutate=mutate), self.assertRaises((KeyError, ValueError)):
                release.verify(self.rewrite_zip(original, mutate))

    def test_unsafe_and_case_duplicate_paths_fail(self):
        original = self.pack()['archive']
        for name in ('../outside', 'C:/outside', '/outside', 'TalesTactics.EXE'):
            with self.subTest(name=name), self.assertRaises(ValueError):
                release.verify(self.rewrite_zip(original, lambda c: c.update({name: b'x'})))

    def test_version_and_overwrite_protection(self):
        with self.assertRaises(ValueError):
            self.pack('../release')
        result = self.pack()
        before = release.digest(result['archive'])
        with self.assertRaises(FileExistsError):
            self.pack()
        self.assertEqual(before, release.digest(result['archive']))

    def test_update_uses_separate_folder_without_stale_assets_or_changing_previous_release(self):
        old_asset = self.build/'obsolete.dat'
        old_asset.write_text('old asset')
        old = self.pack('0.1.0-test.1')
        old_asset.unlink()
        (self.build/'new.dat').write_text('new asset')
        new = self.pack('0.1.0-test.2')
        for label, result in (('old', old), ('new', new)):
            with zipfile.ZipFile(result['archive']) as z:
                z.extractall(self.root/label)
            self.ps('Verify-Package.ps1', '-Directory', self.root/label)
        self.assertTrue((self.root/'old/obsolete.dat').exists())
        self.assertFalse((self.root/'new/obsolete.dat').exists())
        self.assertTrue((self.root/'new/new.dat').exists())
        self.assertEqual(old['sha256'], release.digest(old['archive']))

    def test_unverified_runtime_and_user_save_rejected(self):
        exe = self.build/'TalesTactics.exe'
        exe.write_bytes(b'changed')
        with self.assertRaises(ValueError):
            self.pack()
        exe.write_bytes(b'fixture-content')
        (self.build/'campaign.json').write_text('{}')
        with self.assertRaises(ValueError):
            self.pack()

    def test_missing_required_build_file_rejected(self):
        (self.build/'UnityPlayer.dll').unlink()
        with self.assertRaises(ValueError):
            self.pack()

    def test_development_review_assembly_rejected(self):
        dll = self.build/'TalesTactics_Data/Managed/TalesTactics.Runtime.dll'
        dll.write_bytes(b'CampaignPlayerReview')
        hashes = release.read_json(self.evidence/'build-hashes.json')
        hashes[1]['Hash'] = release.digest(dll)
        (self.evidence/'build-hashes.json').write_text(json.dumps(hashes))
        report = json.loads(release.read_json(self.evidence/'windows-build.json')['data']['result'])
        for f in report['files']:
            if Path(f['path']) == dll:
                f['sizeBytes'] = dll.stat().st_size
        (self.evidence/'windows-build.json').write_text(json.dumps({'data': {'result': json.dumps(report)}}))
        with self.assertRaisesRegex(ValueError, 'Development'):
            self.pack()

    def test_build_report_detects_missing_nonmandatory_runtime_dependency(self):
        report = json.loads(release.read_json(self.evidence/'windows-build.json')['data']['result'])
        report['files'].append({'path': str(self.build/'MonoBleedingEdge/runtime.dll'), 'sizeBytes': 100})
        (self.evidence/'windows-build.json').write_text(json.dumps({'data': {'result': json.dumps(report)}}))
        with self.assertRaisesRegex(ValueError, 'Build report file missing'):
            self.pack()

    def test_backup_preserves_all_slots_settings_and_history_without_touching_source(self):
        saves = self.root/'saves'
        saves.mkdir()
        names = ['campaign.json', 'campaign.json.bak', 'campaign.json.v1.bak',
                 'campaign-slot2.json', 'campaign-slot3.json', 'settings.json',
                 'settings.json.bak', 'campaign.json.corrupt-aabb']
        for name in names:
            (saves/name).write_text('payload '+name)
        (saves/'Player.log').write_text('exclude log')
        (saves/'Reviews').mkdir()
        (saves/'Reviews/private.txt').write_text('exclude reviews')
        before = {p.name: release.digest(p) for p in saves.iterdir() if p.is_file()}
        destination = Path(self.ps('Backup-Saves.ps1', '-SaveDirectory', saves,
                                   '-BackupDirectory', self.root/'backups'))
        self.ps('Verify-Package.ps1', '-Directory', destination)
        manifest = release.read_json(destination/'manifest.json')
        self.assertEqual(set(names), {f['path'] for f in manifest['files']})
        self.assertEqual(before, {p.name: release.digest(p) for p in saves.iterdir() if p.is_file()})
        (destination/'campaign.json').write_text('corrupt')
        self.ps('Verify-Package.ps1', '-Directory', destination, success=False)

    def test_backup_rejects_nested_destination_and_empty_source(self):
        saves = self.root/'saves'
        saves.mkdir()
        self.ps('Backup-Saves.ps1', '-SaveDirectory', saves, '-BackupDirectory', saves/'nested', success=False)
        self.ps('Backup-Saves.ps1', '-SaveDirectory', saves, '-BackupDirectory', self.root/'backups', success=False)

    def test_installed_missing_extra_and_escaping_manifest_fail(self):
        result = self.pack()
        install = self.root/'installed'
        with zipfile.ZipFile(result['archive']) as z:
            z.extractall(install)
        (install/'extra.txt').write_text('extra')
        self.ps('Verify-Package.ps1', '-Directory', install, success=False)
        (install/'extra.txt').unlink()
        (install/'UnityPlayer.dll').unlink()
        self.ps('Verify-Package.ps1', '-Directory', install, success=False)
        manifest = release.read_json(install/'manifest.json')
        manifest['files'][0]['path'] = '../outside.txt'
        (install/'manifest.json').write_text(json.dumps(manifest))
        self.ps('Verify-Package.ps1', '-Directory', install, success=False)


if __name__ == '__main__':
    unittest.main(verbosity=2)
