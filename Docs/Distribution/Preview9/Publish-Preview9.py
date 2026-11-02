"""Prepare/publish only kkkyaho/TalesTactics v0.1.0-preview.9 (Python 3.10+).

This file does nothing when imported. Do not run until release authorization,
package validation, and the exact remote tag are ready. All modes require the
same local ZIP, its .zip.sha256 sidecar, full source SHA, and pinned ZIP SHA256.

Modes:
  draft-upload: create a draft and stream two assets; never publishes.
  publish: require --release-id, revalidate, then publish as a prerelease.
  verify: anonymous, read-only REST metadata/digest verification of public assets.

Use a NEW --evidence .json path on every invocation. To resume a failed upload,
inspect the preserved draft first and pass its --release-id to draft-upload.
Matching uploaded assets are reused; conflicting/starter assets are never
deleted. There are no mutation retries, tag writes, git pushes or cleanup calls.
verify does not follow CDN redirects or download the remote ZIP/checksum bytes.

API references (checked 2026-11-03):
https://docs.github.com/en/rest/releases/releases
https://docs.github.com/en/rest/releases/assets
https://docs.github.com/en/rest/git/refs
https://docs.github.com/en/rest/users/users
"""

import argparse
import hashlib
import http.client
import json
import os
import re
import ssl
import subprocess
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import quote, urlsplit


OWNER = 'kkkyaho'
REPOSITORY = 'kkkyaho/TalesTactics'
VERSION = '0.1.0-preview.9'
TAG = 'v' + VERSION
PREFIX = '/repos/' + REPOSITORY
TITLE = 'TalesTactics ' + VERSION
ZIP_NAME = 'TalesTactics-' + VERSION + '-windows-x64.zip'
API_VERSION = '2026-03-10'
ROOT = Path(__file__).resolve().parents[3]
CHUNK = 1024 * 1024
JSON_LIMIT = 8 * CHUNK
REQUIRED_FILES = {'TalesTactics.exe', 'UnityPlayer.dll',
                  'TalesTactics_Data/Managed/TalesTactics.Runtime.dll'}


class SafetyError(Exception):
    """Only deliberately selected, credential-free text may enter this exception."""


def require(condition, message):
    if not condition:
        raise SafetyError(message)


def now():
    return datetime.now(timezone.utc).isoformat()


def sha_stream(stream):
    result = hashlib.sha256()
    for block in iter(lambda: stream.read(CHUNK), b''):
        result.update(block)
    return result.hexdigest()


def identity(path):
    s = path.stat()
    return (s.st_dev, s.st_ino, s.st_size, s.st_mtime_ns)


def local_file(path):
    require(path.is_file() and not path.is_symlink(), 'Expected a regular local file.')
    before = identity(path)
    with path.open('rb') as stream:
        digest = sha_stream(stream)
    require(identity(path) == before, 'Local file changed while hashing.')
    return {'path': path, 'name': path.name, 'size': before[2],
            'sha256': digest, 'identity': before}


def safe_name(name):
    require(isinstance(name, str) and name and not re.search(r'[\\:<>"|?*]', name)
            and all(p not in ('', '.', '..') and not p.endswith((' ', '.'))
                    for p in name.split('/')), 'Unsafe archive member name.')
    return name


def local_inputs(args):
    archive = Path(args.zip).resolve(strict=True)
    require(archive.name == ZIP_NAME, 'ZIP filename must match Preview9 exactly.')
    checksum = Path(str(archive) + '.sha256')
    files = [local_file(archive), local_file(checksum)]
    require(files[0]['sha256'] == args.expected_zip_sha256,
            'ZIP SHA256 differs from the explicitly pinned value.')
    require(files[1]['size'] <= 1024, 'Checksum sidecar is unexpectedly large.')
    line = checksum.read_text(encoding='ascii').strip()
    require(re.fullmatch(r'[0-9a-fA-F]{64}  ' + re.escape(ZIP_NAME), line)
            and line[:64].lower() == args.expected_zip_sha256,
            'Checksum sidecar does not identify this ZIP and SHA256.')
    with zipfile.ZipFile(archive) as package:
        entries = package.infolist()
        names = [safe_name(e.filename) for e in entries]
        require(len({n.casefold() for n in names}) == len(names),
                'Duplicate or case-colliding ZIP members.')
        require(all(not e.is_dir() and ((e.external_attr >> 16) & 0o170000) != 0o120000
                    for e in entries), 'Directory/link ZIP members are not allowed.')
        require('manifest.json' in names and package.getinfo('manifest.json').file_size <= 2 * CHUNK,
                'Release manifest missing or unexpectedly large.')
        manifest = json.loads(package.read('manifest.json').decode('utf-8-sig'))
        require(manifest.get('schema') == 1 and manifest.get('kind') == 'release'
                and manifest.get('version') == VERSION
                and manifest.get('platform') == 'windows-x64'
                and manifest.get('sourceCommit') == args.expected_source,
                'ZIP manifest version/platform/sourceCommit does not match.')
        described = {'manifest.json'}
        for entry in manifest['files']:
            name = safe_name(entry['path'])
            require(name.casefold() not in {n.casefold() for n in described},
                    'Duplicate manifest member.')
            require(re.fullmatch(r'[0-9a-fA-F]{64}', entry['sha256']) is not None
                    and package.getinfo(name).file_size == entry['bytes'],
                    'Invalid manifest member digest or size.')
            described.add(name)
        require(described == set(names) and REQUIRED_FILES.issubset(described),
                'ZIP inventory differs from its manifest or lacks runtime files.')
    require(identity(archive) == files[0]['identity'], 'ZIP changed during inspection.')
    notes_path = Path(args.notes).resolve(strict=True)
    require(notes_path.stat().st_size <= 100 * 1024, 'Release notes are unexpectedly large.')
    notes = notes_path.read_text(encoding='utf-8-sig')
    require(bool(notes.strip()), 'Release notes are empty.')
    return files, notes, manifest


def credential():
    # The standard helper is consulted only on a requested mutation mode.
    # Capture both streams; never log stdout, stderr, headers or subprocess errors.
    env = os.environ.copy()
    env.update({'GIT_TERMINAL_PROMPT': '0', 'GCM_INTERACTIVE': 'Never'})
    for name in ('GIT_TRACE', 'GIT_TRACE2', 'GIT_TRACE2_EVENT', 'GIT_TRACE2_PERF',
                 'GIT_TRACE_CURL', 'GIT_CURL_VERBOSE', 'GIT_TRACE_PACKET',
                 'GCM_TRACE', 'GCM_TRACE_SECRETS'):
        env[name] = '0'
    result = subprocess.run(
        ['git', '-c', 'credential.interactive=false', 'credential', 'fill'],
        input='protocol=https\nhost=github.com\npath=kkkyaho/TalesTactics\nusername=kkkyaho\n\n',
        cwd=ROOT, env=env, text=True, encoding='utf-8', errors='replace',
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=30,
        creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    require(result.returncode == 0, 'Git credential helper failed; output was withheld.')
    fields = dict(line.split('=', 1) for line in result.stdout.splitlines() if '=' in line)
    token = fields.get('password', '')
    require(token and '\r' not in token and '\n' not in token,
            'Git credential helper returned no usable credential.')
    return token


class Api:
    def __init__(self, token=None):
        self.token = token

    def request(self, path, method='GET', payload=None, upload=None, missing=False):
        # Host and path are constructed here, never accepted from redirects.
        host = 'uploads.github.com' if upload is not None else 'api.github.com'
        require(path.startswith(PREFIX + '/') or path == PREFIX or path == '/user',
                'Unexpected API path.')
        require(method in ('GET', 'POST', 'PATCH'), 'Unsupported HTTP method.')
        require(method == 'GET' or self.token is not None, 'Mutation requires authentication.')
        body = json.dumps(payload, ensure_ascii=False).encode('utf-8') if payload is not None else None
        headers = {'User-Agent': 'TalesTactics-Preview9-release',
                   'Accept': 'application/vnd.github+json', 'X-GitHub-Api-Version': API_VERSION}
        if self.token is not None:
            headers['Authorization'] = 'Bearer ' + self.token
        connection = http.client.HTTPSConnection(host, timeout=300, context=ssl.create_default_context())
        try:
            if upload is None:
                if body is not None:
                    headers['Content-Type'] = 'application/json'
                connection.request(method, path, body=body, headers=headers)
            else:
                require(method == 'POST' and identity(upload['path']) == upload['identity'],
                        'Upload input changed after validation.')
                headers['Content-Type'] = 'application/zip' if upload['name'].endswith('.zip') else 'text/plain'
                headers['Content-Length'] = str(upload['size'])
                connection.putrequest('POST', path)
                for key, value in headers.items():
                    connection.putheader(key, value)
                connection.endheaders()
                sent, digest = 0, hashlib.sha256()
                with upload['path'].open('rb') as stream:
                    for block in iter(lambda: stream.read(CHUNK), b''):
                        connection.send(block)
                        sent += len(block)
                        digest.update(block)
                require(sent == upload['size'] and digest.hexdigest() == upload['sha256']
                        and identity(upload['path']) == upload['identity'],
                        'File changed while uploading; draft preserved for inspection.')
            response = connection.getresponse()
            if response.status == 404 and missing:
                return None
            expected = 201 if method == 'POST' else 200
            require(response.status == expected,
                    'GitHub request returned HTTP ' + str(response.status) +
                    '; no automatic retry or deletion. Inspect any existing draft before retrying.')
            raw = response.read(JSON_LIMIT + 1)
            require(len(raw) <= JSON_LIMIT, 'GitHub JSON response exceeds the bounded limit.')
            return json.loads(raw.decode('utf-8'))
        finally:
            connection.close()


def check_repository(api, authenticated):
    repo = api.request(PREFIX)
    require(repo.get('full_name', '').lower() == REPOSITORY.lower()
            and repo.get('owner', {}).get('login', '').lower() == OWNER
            and repo.get('private') is False and not repo.get('archived'),
            'Repository owner/name/public state does not match the release target.')
    result = {'repository': REPOSITORY, 'owner': OWNER, 'public': True}
    if authenticated:
        user = api.request('/user')
        require(user.get('login', '').lower() == OWNER
                and repo.get('permissions', {}).get('push') is True,
                'Authenticated account must be the repository owner with push permission.')
        result['authenticatedLogin'] = OWNER
    return result


def check_tag(api, expected):
    ref = api.request(PREFIX + '/git/ref/tags/' + TAG)
    require(ref.get('ref') == 'refs/tags/' + TAG, 'Unexpected tag reference.')
    item, seen = ref['object'], set()
    for _ in range(8):
        sha = item.get('sha', '')
        require(re.fullmatch(r'[0-9a-f]{40}', sha) is not None and sha not in seen,
                'Invalid or cyclic remote tag object.')
        seen.add(sha)
        if item.get('type') == 'commit':
            require(sha == expected, 'Remote tag commit differs from the expected source SHA.')
            return {'ref': ref['ref'], 'objectSHA': ref['object']['sha'], 'commitSHA': sha}
        require(item.get('type') == 'tag', 'Remote tag does not resolve to a commit.')
        item = api.request(PREFIX + '/git/tags/' + sha)['object']
    raise SafetyError('Remote annotated tag nesting exceeds the bounded limit.')


def all_pages(api, path):
    items = []
    for page in range(1, 51):
        batch = api.request(path + '?per_page=100&page=' + str(page))
        require(isinstance(batch, list), 'Expected a paginated GitHub list.')
        items.extend(batch)
        if len(batch) < 100:
            return items
    raise SafetyError('Pagination limit reached; no mutation allowed from a partial list.')


def public_url(value):
    p = urlsplit(value or '')
    if p.scheme == 'https' and p.netloc == 'github.com' and p.path.startswith('/' + REPOSITORY + '/releases/'):
        return 'https://github.com' + p.path
    return None


def release_snapshot(release, assets=()):
    return {'id': release['id'], 'tag': release.get('tag_name'),
            'targetCommitish': release.get('target_commitish'), 'draft': release.get('draft'),
            'prerelease': release.get('prerelease'), 'url': public_url(release.get('html_url')),
            'publishedAt': release.get('published_at'), 'assets': [
                {'id': a['id'], 'name': a['name'], 'bytes': a['size'], 'serverDigest': a.get('digest'),
                 'state': a.get('state'), 'url': public_url(a.get('browser_download_url'))} for a in assets]}


def check_release(release, source, notes):
    require(isinstance(release.get('id'), int) and release['id'] > 0
            and release.get('tag_name') == TAG and release.get('target_commitish') == source
            and release.get('prerelease') is True and release.get('name') == TITLE
            and release.get('body') == notes,
            'Release identity/source/title/body/prerelease state differs from the reviewed input.')


def check_assets(api, release_id, files, complete):
    assets = all_pages(api, PREFIX + '/releases/' + str(release_id) + '/assets')
    by_name = {a['name']: a for a in assets}
    expected = {f['name']: f for f in files}
    require(len(by_name) == len(assets) and set(by_name).issubset(expected),
            'Unexpected or duplicate release assets; nothing will be deleted.')
    if complete:
        require(set(by_name) == set(expected), 'Both release assets must be present before publication.')
    for name, asset in by_name.items():
        local = expected[name]
        require(asset.get('state') == 'uploaded' and asset.get('size') == local['size']
                and asset.get('digest') == 'sha256:' + local['sha256'],
                'Existing release asset has different bytes/digest or is incomplete; draft preserved.')
    return assets


def execute(args, evidence, save):
    files, notes, manifest = local_inputs(args)
    evidence.update({'phase': 'local-verified', 'sourceCommit': manifest['sourceCommit'],
                     'notesSHA256': hashlib.sha256(notes.encode('utf-8')).hexdigest(),
                     'files': [{'name': f['name'], 'bytes': f['size'], 'sha256': f['sha256']} for f in files],
                     'zipMemberHashesRecomputed': False})
    save()
    api = Api(None if args.mode == 'verify' else credential())
    evidence['identity'] = check_repository(api, args.mode != 'verify')
    evidence['tag'] = check_tag(api, args.expected_source)
    evidence['phase'] = 'remote-verified'
    save()
    if args.mode == 'draft-upload':
        matches = [r for r in all_pages(api, PREFIX + '/releases') if r.get('tag_name') == TAG]
        require(len(matches) <= 1, 'Multiple releases use this tag; inspect manually.')
        if matches:
            require(args.release_id == matches[0]['id'],
                    'Release already exists. Inspect it, then explicitly supply its --release-id to resume.')
            release = api.request(PREFIX + '/releases/' + str(args.release_id))
            check_release(release, args.expected_source, notes)
            require(release.get('draft') is True, 'Existing release is public; draft-upload cannot modify it.')
        else:
            require(args.release_id is None, 'Requested draft ID was not found under this exact tag.')
            evidence['phase'] = 'create-draft-requested'
            evidence['mutationAttempted'] = True
            save()
            release = api.request(PREFIX + '/releases', 'POST', {
                'tag_name': TAG, 'target_commitish': args.expected_source, 'name': TITLE,
                'body': notes, 'draft': True, 'prerelease': True, 'make_latest': 'false',
                'generate_release_notes': False})
            check_release(release, args.expected_source, notes)
            require(release.get('draft') is True, 'Create response was not a draft.')
        evidence['release'] = release_snapshot(release)
        evidence['phase'] = 'draft-present'
        save()
        existing = check_assets(api, release['id'], files, complete=False)
        upload = urlsplit(release.get('upload_url', '').split('{')[0])
        upload_path = PREFIX + '/releases/' + str(release['id']) + '/assets'
        require(upload.scheme == 'https' and upload.netloc == 'uploads.github.com'
                and upload.path == upload_path and not upload.query and not upload.fragment,
                'Unexpected upload host/path; no credential was sent there.')
        for local in files:
            if any(a['name'] == local['name'] for a in existing):
                continue
            evidence['phase'] = 'upload-requested:' + local['name']
            evidence['mutationAttempted'] = True
            save()
            api.request(upload_path + '?name=' + quote(local['name'], safe=''), 'POST', upload=local)
            existing = check_assets(api, release['id'], files, complete=False)
            evidence['release'] = release_snapshot(release, existing)
            save()
        release = api.request(PREFIX + '/releases/' + str(release['id']))
        check_release(release, args.expected_source, notes)
        require(release.get('draft') is True, 'Draft changed during upload; inspect remote state.')
    else:
        if args.mode == 'publish':
            require(args.release_id is not None, 'publish requires the inspected --release-id.')
            release = api.request(PREFIX + '/releases/' + str(args.release_id))
        else:
            release = api.request(PREFIX + '/releases/tags/' + TAG)
        check_release(release, args.expected_source, notes)
        check_assets(api, release['id'], files, complete=True)
        if args.mode == 'publish' and release.get('draft') is True:
            evidence['tag'] = check_tag(api, args.expected_source)
            require(all(identity(f['path']) == f['identity'] for f in files),
                    'Local package changed before publication.')
            evidence['release'] = release_snapshot(release)
            evidence['phase'] = 'publish-requested'
            evidence['mutationAttempted'] = True
            save()
            release = api.request(PREFIX + '/releases/' + str(release['id']), 'PATCH',
                                  {'draft': False, 'prerelease': True, 'make_latest': 'false'})
            check_release(release, args.expected_source, notes)
        require(release.get('draft') is False and release.get('published_at'),
                'Public prerelease confirmation is absent.')
    assets = check_assets(api, release['id'], files, complete=True)
    evidence['tag'] = check_tag(api, args.expected_source)
    evidence.update({'release': release_snapshot(release, assets), 'phase': 'complete',
                     'passed': True, 'finishedUtc': now(), 'anonymous': args.mode == 'verify',
                     'remoteZipDownloaded': False, 'publicAttachmentBytesDownloaded': False})
    save()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mode', required=True, choices=('draft-upload', 'publish', 'verify'))
    parser.add_argument('--zip', required=True, help='Exact Preview9 ZIP; adjacent .zip.sha256 is required.')
    parser.add_argument('--expected-source', required=True, help='Full 40-character manifest and remote-tag commit SHA.')
    parser.add_argument('--expected-zip-sha256', required=True, help='Independently verified 64-character ZIP SHA256.')
    parser.add_argument('--release-id', type=int, help='Required for publish or explicit resume of an existing draft.')
    parser.add_argument('--notes', default=str(Path(__file__).with_name('release-notes.md')))
    parser.add_argument('--evidence', required=True, help='NEW JSON file; existing evidence is never overwritten.')
    args = parser.parse_args()
    args.expected_source = args.expected_source.lower()
    args.expected_zip_sha256 = args.expected_zip_sha256.lower()
    if not re.fullmatch(r'[0-9a-f]{40}', args.expected_source) or not re.fullmatch(r'[0-9a-f]{64}', args.expected_zip_sha256):
        parser.error('A full source SHA and ZIP SHA256 are required.')
    if args.release_id is not None and args.release_id <= 0:
        parser.error('--release-id must be positive.')
    evidence_path = Path(args.evidence).resolve()
    if evidence_path.suffix.lower() != '.json' or not evidence_path.parent.is_dir():
        parser.error('--evidence requires a new .json path in an existing directory.')
    evidence = {'schema': 1, 'mode': args.mode, 'repository': REPOSITORY, 'tag': TAG,
                'startedUtc': now(), 'phase': 'started', 'passed': False, 'mutationAttempted': False,
                'scope': 'Pinned local package + exact remote tag; API owner/user checks for mutation modes; '
                         'two streamed assets matched by server digest/size; no CDN downloads, tag writes, '
                         'automatic retries, asset/release deletions, or remote ZIP full-download verification.'}
    try:
        # Hold one exclusively created handle: failures update only this invocation's receipt.
        with evidence_path.open('x', encoding='utf-8', newline='\n') as receipt:
            def save():
                receipt.seek(0)
                receipt.write(json.dumps(evidence, ensure_ascii=False, indent=2) + '\n')
                receipt.truncate()
                receipt.flush()
                os.fsync(receipt.fileno())
            save()
            try:
                execute(args, evidence, save)
            except BaseException as error:
                evidence.update({'passed': False, 'finishedUtc': now(),
                                 'failure': str(error) if isinstance(error, SafetyError) else
                                 'Operation stopped (' + type(error).__name__ + '); raw details withheld.',
                                 'remoteState': 'No rollback/deletion attempted. A requested mutation may have '
                                                'completed remotely; inspect the existing release before retrying.'})
                save()
                print(json.dumps({'passed': False, 'phase': evidence['phase'], 'failure': evidence['failure']}))
                return 1
        print(json.dumps({'passed': True, 'mode': args.mode, 'release': evidence['release']}, ensure_ascii=False))
        return 0
    except Exception:
        # Never print subprocess output, HTTP bodies, headers or exception locals.
        print('FAILED: evidence could not be exclusively created or updated; inspect existing receipts.')
        return 1


if __name__ == '__main__':
    sys.exit(main())
