[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$BeforeResult,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$AfterResult,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$OutputHtml
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The page references original PNGs. It never resizes, composites, interpolates or rewrites them.
function Get-AbsolutePath([string]$Path, [string]$Label) {
    if (-not [IO.Path]::IsPathRooted($Path) -or $Path.StartsWith('\\')) {
        throw "$Label must be an absolute local path."
    }
    return [IO.Path]::GetFullPath($Path)
}
function Test-Within([string]$Path, [string]$Directory) {
    $prefix = $Directory.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    return $Path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}
function Get-RequiredMember($Value, [string]$Name) {
    if ($null -eq $Value) { throw "Missing object for $Name." }
    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property) { throw "Missing capture member: $Name." }
    return $property.Value
}
function Get-RelativeUri([string]$File, [string]$Directory) {
    if (-not [IO.Path]::GetPathRoot($File).Equals([IO.Path]::GetPathRoot($Directory), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Images, result files and the HTML must be on the same drive for relative links.'
    }
    $baseUri = [Uri]($Directory.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar)
    $fileUri = [Uri]$File
    $relative = $baseUri.MakeRelativeUri($fileUri).ToString()
    if ([Uri]::IsWellFormedUriString($relative, [UriKind]::Absolute)) { throw 'An image link unexpectedly became absolute.' }
    return $relative
}

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$logsRoot = Join-Path $projectRoot 'Logs'
$outputPath = Get-AbsolutePath $OutputHtml 'OutputHtml'
if (-not (Test-Within $outputPath $logsRoot) -or [IO.Path]::GetExtension($outputPath) -ne '.html') {
    throw 'OutputHtml must be a new .html file below this project''s Logs directory.'
}
if (Test-Path -LiteralPath $outputPath) { throw 'The output already exists. Existing evidence is never overwritten.' }
$outputDirectory = [IO.Path]::GetDirectoryName($outputPath)

function Read-Capture([string]$Path, [bool]$ExpectedPilot) {
    $fullPath = Get-AbsolutePath $Path 'Result JSON'
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { throw "Capture result does not exist: $fullPath" }
    $result = [IO.File]::ReadAllText($fullPath) | ConvertFrom-Json
    if ([int](Get-RequiredMember $result 'schema') -ne 1) { throw 'Unsupported capture schema.' }
    if ((Get-RequiredMember $result 'pilot') -isnot [bool] -or [bool]$result.pilot -ne $ExpectedPilot) {
        throw 'BeforeResult must use pilot=false and AfterResult must use pilot=true.'
    }
    if (-not [bool](Get-RequiredMember $result 'complete') -or -not [bool](Get-RequiredMember $result 'passed')) {
        throw 'Only completed, passed captures can produce the comparison page. Preserve failed results separately.'
    }
    $captureRoot = [IO.Path]::GetDirectoryName($fullPath)
    $converted = New-Object 'System.Collections.Generic.List[object]'
    foreach ($frame in @(Get-RequiredMember $result 'frames')) {
        $name = [string](Get-RequiredMember $frame 'file')
        if ([string]::IsNullOrWhiteSpace($name) -or [IO.Path]::IsPathRooted($name) -or $name.Contains(':')) {
            throw "Capture image must use a relative local path: $name"
        }
        $imagePath = [IO.Path]::GetFullPath((Join-Path $captureRoot $name))
        if (-not (Test-Within $imagePath $captureRoot) -or [IO.Path]::GetExtension($imagePath) -ne '.png' -or
            -not (Test-Path -LiteralPath $imagePath -PathType Leaf)) {
            throw "Missing or out-of-folder original PNG: $name"
        }
        $elapsed = [double](Get-RequiredMember $frame 'elapsed')
        if ([double]::IsNaN($elapsed) -or [double]::IsInfinity($elapsed) -or $elapsed -lt 0) {
            throw "Invalid elapsed timestamp: $name"
        }
        $converted.Add([ordered]@{
            file = $name
            src = Get-RelativeUri $imagePath $outputDirectory
            phase = [string](Get-RequiredMember $frame 'phase')
            facing = [string](Get-RequiredMember $frame 'viewFacing')
            elapsed = $elapsed
            frameCount = [int](Get-RequiredMember $frame 'frame')
            time = [double](Get-RequiredMember $frame 'time')
            sprite = [string](Get-RequiredMember $frame 'sprite')
            fullBoard = [bool](Get-RequiredMember $frame 'fullBoard')
            supplement = [bool](Get-RequiredMember $frame 'blinkSupplement')
        })
    }
    foreach ($direction in @('Front', 'Back', 'Left', 'Right')) {
        $idle = @($converted | Where-Object { $_.phase -eq 'idle' -and $_.facing -eq $direction -and -not $_.fullBoard -and -not $_.supplement -and $_.elapsed -lt 6 })
        if ($idle.Count -eq 0) { throw "Missing continuous idle capture direction: $direction" }
    }
    foreach ($kind in @('attack', 'damage')) {
        $action = @($converted | Where-Object { $_.phase.StartsWith($kind, [StringComparison]::Ordinal) -and $_.facing -eq 'Front' -and -not $_.fullBoard })
        if ($action.Count -eq 0) { throw "Missing action captures: $kind" }
    }
    return [ordered]@{
        result = Get-RelativeUri $fullPath $outputDirectory
        unityVersion = [string](Get-RequiredMember $result 'unityVersion')
        pilot = $ExpectedPilot
        frames = @($converted.ToArray())
    }
}

$before = Read-Capture $BeforeResult $false
$after = Read-Capture $AfterResult $true
$payload = ConvertTo-Json -InputObject ([ordered]@{ before = $before; after = $after }) -Depth 8 -Compress
# JSON remains data even if a local filename contains an HTML closing tag.
$payload = $payload.Replace('<', '\u003c').Replace('>', '\u003e').Replace('&', '\u0026').Replace([string][char]0x2028, '\u2028').Replace([string][char]0x2029, '\u2029')

$template = @'
<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src 'self' file:; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'none'; base-uri 'none'; form-action 'none'">
<title>알펜 움직임 전후 비교</title>
<style>
:root{color-scheme:dark;font-family:system-ui,-apple-system,"Malgun Gothic",sans-serif;background:#101924;color:#e9f1f5}
*{box-sizing:border-box}body{margin:0}main{max-width:1440px;margin:auto;padding:28px 22px 40px}
h1{font-size:clamp(24px,3vw,34px);margin:0 0 10px;font-weight:750;letter-spacing:-.04em}
p{line-height:1.65;color:#aebfc9;margin:6px 0}.intro{max-width:1000px}.controls{margin:24px 0 16px;display:flex;gap:12px;align-items:end;flex-wrap:wrap}
label{display:grid;gap:7px;font-size:13px;color:#b7cad3}select,button{font:inherit;background:#1d2b39;color:#f1f6f8;border:1px solid #425466;border-radius:9px;min-height:43px;padding:9px 14px}
button{cursor:pointer;min-width:92px;background:#27666a;border-color:#3c9295;font-weight:650}button:hover{background:#317a7f}select:disabled{opacity:.45}
.scrub{display:flex;align-items:center;gap:15px;margin-bottom:20px}.scrub input{accent-color:#76cbc9;flex:1;cursor:pointer}.scrub output{font-variant-numeric:tabular-nums;font-size:13px;min-width:120px;color:#b9cdd6}
.comparison{display:grid;grid-template-columns:1fr 1fr;gap:18px}.panel{background:#14212d;border:1px solid #2e414f;border-radius:14px;overflow:hidden}.panel h2{font-size:17px;margin:0;padding:15px 18px;border-bottom:1px solid #2e414f}.panel.after h2{color:#a9e5df}
.viewport{aspect-ratio:16/9;background:#080e16;display:flex;align-items:center;justify-content:center;position:relative}.viewport img{display:block;max-width:100%;max-height:100%;width:100%;height:100%;object-fit:contain}
.frame-note{font-variant-numeric:tabular-nums;font-size:12px;color:#a9bdc9;padding:10px 16px;min-height:38px}.empty{padding:25px;text-align:center;color:#eac389}
.status{margin-top:12px;min-height:24px;color:#dfb977;font-size:13px}.footnote{font-size:13px;margin-top:12px}.footnote strong{color:#cee4e9;font-weight:600}
details{margin-top:24px;border-top:1px solid #304451;padding-top:18px;color:#b6c7d0}summary{cursor:pointer;font-size:14px}a{color:#91d6dc;text-underline-offset:3px}.metadata{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:15px;font-size:12px;line-height:1.6}.metadata pre{white-space:pre-wrap;word-break:break-word;background:#0c151f;padding:15px;border-radius:8px;margin:8px 0}
.extra{font-size:13px;margin-top:14px}.extra ul{margin:8px 0;padding-left:20px}.extra li{margin:4px 0}.hint{font-size:12px}
@media(max-width:800px){main{padding:22px 14px}.comparison,.metadata{grid-template-columns:1fr}.controls{gap:9px}select,button{padding:9px 11px}.scrub{gap:8px}}
</style>
</head>
<body>
<main>
<h1>알펜 움직임 전후 비교</h1>
<p class="intro">실제 Unity 실행 중 저장한 원본 화면을 나란히 재생합니다. 같은 방향과 동작을 선택해 자세, 눈 깜빡임, 망토와 검의 움직임을 비교하세요.</p>
<div class="controls">
<label>동작<select id="kind"><option value="idle">대기 · 6초 반복</option><option value="attack">기본 공격</option><option value="damage">피격</option></select></label>
<label>방향<select id="direction"><option value="Front">앞</option><option value="Back">뒤</option><option value="Left">왼쪽</option><option value="Right">오른쪽</option></select></label>
<label>재생 속도<select id="speed"><option value="1">1배</option><option value="0.5">0.5배</option><option value="0.25">0.25배</option></select></label>
<button id="play" type="button">일시 정지</button><button id="restart" type="button">처음부터</button>
</div>
<div class="scrub"><input id="timeline" type="range" min="0" max="6000" value="0" step="1" aria-label="재생 위치"><output id="clock">0.00 / 6.00초</output></div>
<div class="comparison">
<section class="panel"><h2>개선 전 · 기존 모션</h2><div class="viewport"><img id="before-image" alt="개선 전 실제 Unity 프레임"></div><div id="before-note" class="frame-note"></div></section>
<section class="panel after"><h2>개선 후 · 새 원화 모션</h2><div class="viewport"><img id="after-image" alt="개선 후 실제 Unity 프레임"></div><div id="after-note" class="frame-note"></div></section>
</div>
<div id="status" class="status" role="status"></div>
<p class="footnote"><strong>실제 캡처 간격 그대로:</strong> 각 결과의 경과 시각에 맞춰 원본 PNG를 보여주며, 캡처 사이에는 마지막 관측 화면을 유지합니다. 중간 그림을 만들거나 원본 이미지를 편집하지 않았습니다.</p>
<p class="footnote">캡처 저장과 Editor 처리 시간이 포함되므로 성능 측정 자료가 아닙니다. 배경은 각 캡처 당시의 상태이며, 전체 전장 개선은 별도 전장 캡처로 확인해야 합니다.</p>
<details>
<summary>현재 프레임 · 원본 자료 · 보충 캡처 확인</summary>
<div class="metadata"><div><a id="before-result">개선 전 결과 JSON</a><pre id="before-meta"></pre></div><div><a id="after-result">개선 후 결과 JSON</a><pre id="after-meta"></pre></div></div>
<div class="extra"><strong>눈 깜빡임 보충 캡처</strong><p class="hint">연속 캡처에서 눈 감음을 놓친 경우, 다음 실제 주기에서 관측한 원본입니다. 시각이 다른 보충 이미지는 위 6초 재생에 삽입하지 않습니다.</p><ul id="supplements"></ul></div>
<div class="extra"><strong>전장 전체와 HUD</strong><ul id="fullboards"></ul></div>
</details>
</main>
<script type="application/json" id="capture-data">__CAPTURE_DATA__</script>
<script>
(function () {
  'use strict';
  const data = JSON.parse(document.getElementById('capture-data').textContent);
  const names = { Front:'앞', Back:'뒤', Left:'왼쪽', Right:'오른쪽' };
  const controls = Object.fromEntries(['kind','direction','speed','play','restart','timeline','clock','status'].map(id => [id,document.getElementById(id)]));
  const sides = ['before','after'];
  const elements = {};
  const imageCache = new Map();
  const failures = new Set();
  let tracks = {}, current = {}, position = 0, duration = 6, playing = true, lastNow = null;
  let preparing = false, preparation = 0;

  function imageFor(src) {
    if (!imageCache.has(src)) {
      const image = new Image();
      const promise = new Promise(resolve => {
        image.onload = () => resolve(true);
        image.onerror = () => { failures.add(src); resolve(false); };
      });
      image.src = src;
      imageCache.set(src,{image,promise});
    }
    return imageCache.get(src).promise;
  }
  sides.forEach(side => {
    elements[side] = {image:document.getElementById(side+'-image'),note:document.getElementById(side+'-note'),meta:document.getElementById(side+'-meta')};
    const link = document.getElementById(side+'-result');
    link.href = data[side].result;
    link.target = '_blank'; link.rel = 'noopener';
  });
  function pick(list,t) {
    let lo = 0, hi = list.length-1;
    while (lo < hi) { const mid = Math.ceil((lo+hi)/2); if (list[mid].elapsed <= t) lo=mid; else hi=mid-1; }
    return list[lo];
  }
  function render() {
    sides.forEach(side => {
      const frame = pick(tracks[side],position);
      if (!frame || current[side] === frame) return;
      current[side] = frame;
      elements[side].image.src = frame.src;
      elements[side].note.textContent = '관측 '+frame.elapsed.toFixed(3)+'초 · 프레임 '+frame.frameCount;
      elements[side].meta.textContent = '원본: '+frame.file+'\nSprite: '+frame.sprite+'\nUnity frameCount: '+frame.frameCount+'\nUnity Time.time: '+frame.time.toFixed(6)+'초\n경과 시각: '+frame.elapsed.toFixed(6)+'초\n방향: '+(names[frame.facing]||frame.facing)+'\nUnity: '+data[side].unityVersion;
    });
    controls.timeline.value = String(Math.round(position*1000));
    controls.clock.textContent = position.toFixed(2)+' / '+duration.toFixed(2)+'초';
    controls.play.textContent = playing ? '일시 정지' : '재생';
  }
  function addLink(list,frame,side,detail) {
    const li = document.createElement('li'), link = document.createElement('a');
    link.href=frame.src;link.target='_blank';link.rel='noopener';
    link.textContent=(side==='before'?'개선 전':'개선 후')+' · '+names[frame.facing]+' · '+detail+' · '+frame.elapsed.toFixed(3)+'초';
    li.append(link);list.append(li);
  }
  const supplements=document.getElementById('supplements'), fullboards=document.getElementById('fullboards');
  sides.forEach(side => data[side].frames.forEach(frame => {
    if(frame.supplement) addLink(supplements,frame,side,'눈 감음');
    if(frame.fullBoard) addLink(fullboards,frame,side,frame.file);
  }));
  if(!supplements.children.length) {
    const li=document.createElement('li');li.textContent='별도 보충 캡처가 없습니다.';supplements.append(li);
  }
  async function selectTrack() {
    const ticket=++preparation;
    preparing=true;lastNow=null;position=0;current={};imageCache.clear();failures.clear();
    const kind=controls.kind.value, direction=kind==='idle'?controls.direction.value:'Front';
    controls.direction.disabled=kind!=='idle';
    sides.forEach(side => {
      tracks[side]=data[side].frames.filter(frame => !frame.fullBoard && !frame.supplement &&
        frame.facing===direction && (kind==='idle' ? frame.phase==='idle' && frame.elapsed<6 : frame.phase.startsWith(kind)))
        .sort((a,b)=>a.elapsed-b.elapsed || a.frameCount-b.frameCount);
    });
    if(sides.some(side=>!tracks[side].length)) {
      controls.status.textContent='이 방향과 동작의 실제 캡처가 없습니다.';playing=false;preparing=false;return;
    }
    duration=kind==='idle'?6:Math.max(.05,...sides.flatMap(side=>tracks[side].map(frame=>frame.elapsed)));
    controls.timeline.max=String(Math.ceil(duration*1000));
    controls.status.textContent='원본 프레임을 불러오는 중입니다…';
    render();
    const sources=[...new Set(sides.flatMap(side=>tracks[side].map(frame=>frame.src)))];
    await Promise.all(sources.map(imageFor));
    if(ticket!==preparation)return;
    preparing=false;lastNow=null;
    const missing=sources.filter(src=>failures.has(src)).length;
    controls.status.textContent=missing ? '원본 PNG '+missing+'개를 열지 못했습니다. HTML과 캡처 폴더의 위치를 유지해 주세요.' : '';
    if(missing)playing=false;
    render();
  }
  controls.kind.addEventListener('change',selectTrack);
  controls.direction.addEventListener('change',selectTrack);
  controls.speed.addEventListener('change',()=>{lastNow=null;});
  controls.play.addEventListener('click',()=>{playing=!playing;lastNow=null;render();});
  controls.restart.addEventListener('click',()=>{position=0;lastNow=null;render();});
  controls.timeline.addEventListener('input',()=>{position=Math.min(duration,Number(controls.timeline.value)/1000);lastNow=null;render();});
  document.addEventListener('visibilitychange',()=>{if(document.hidden){playing=false;lastNow=null;render();}});
  function tick(now) {
    if(lastNow!==null && playing && !preparing) {
      position=(position+(now-lastNow)/1000*Number(controls.speed.value))%duration;
      render();
    }
    lastNow=now;requestAnimationFrame(tick);
  }
  selectTrack();requestAnimationFrame(tick);
})();
</script>
</body>
</html>
'@

$html = $template.Replace('__CAPTURE_DATA__', $payload)
[void][IO.Directory]::CreateDirectory($outputDirectory)
# Exclusive creation prevents a concurrent run from overwriting an existing comparison.
$stream = [IO.File]::Open($outputPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
try {
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes($html)
    $stream.Write($bytes, 0, $bytes.Length)
}
finally { $stream.Dispose() }
[pscustomobject]@{
    Html = $outputPath
    BeforeFrames = @($before.frames).Count
    AfterFrames = @($after.frames).Count
    OriginalImagesModified = $false
    NetworkRequired = $false
} | ConvertTo-Json -Compress
