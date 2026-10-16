# VFX 실제 화면 비교 절차

이 문서는 검수 실행 방법이다. 현재 실행 결과와 한계는 [README.md](README.md), 이미지 제작 출처는 [ART_PROVENANCE.md](ART_PROVENANCE.md)를 따른다. [실제 전후 비교 페이지](review.html)와 [Actual](Actual/)에 실제 화면을 보관하며 시안 이미지를 실제 캡처로 사용하지 않는다.

## 대표 기술

|분류|기존 기술 ID|Content 에셋|
|---|---|---|
|검기|cless.0 / 마신검|Assets/TalesTactics/Content/Skills/cless.0.asset|
|화염·유성|jade.6 / 메테오 스톰|Assets/TalesTactics/Content/Skills/jade.6.asset|
|얼음|jade.5 / 앱솔루트|Assets/TalesTactics/Content/Skills/jade.5.asset|
|번개|jade.3 / 썬더 블레이드|Assets/TalesTactics/Content/Skills/jade.3.asset|
|회복|mint.0 / 퍼스트 에이드|Assets/TalesTactics/Content/Skills/mint.0.asset|
|궁극기|cless.ultimate / 명공참상검|Assets/TalesTactics/Content/Skills/cless.ultimate.asset|

## Editor

TestBattle을 Play Mode로 시작한 뒤 기존 Unity 실행 도구로 다음 API를 호출한다. output은 기존 result.json이 없는 **새 절대 경로**를 사용한다.

```csharp
TalesTactics.VfxUpgradeReview.BeginEditor(output, "before", false);
// 변경 후 동일 조건으로 다른 output에 재실행:
TalesTactics.VfxUpgradeReview.BeginEditor(output, "after", false);
```

`SessionState` 키 `TalesTactics.VfxUpgradeReviewExit`는 실행 중 -1, 완료 성공0/실패1이다. 실제 출력 경로는 `TalesTactics.VfxUpgradeReviewOutput`이다. 종료 시 Play Mode를 끝내며 추가한1920×1080 Game View 크기·기존 선택 크기·captureFramerate·runInBackground를 복원한다.

## 개발 Windows 실행본

`UNITY_EDITOR || DEVELOPMENT_BUILD || TALESTACTICS_REVIEW`로만 컴파일한다. 일반 배포 빌드에는 검수 타입을 포함하지 않는다. 기존 검수 빌드 도구에서 TALESTACTICS_REVIEW를 사용하는 빌드에 포함할 수 있다.

```text
TalesTactics.exe --vfx-upgrade-review <32자리 GUID> --vfx-label before
TalesTactics.exe --vfx-upgrade-review <다른 GUID> --vfx-label after
```

출력은 실행본 옆 `VfxUpgrade/<GUID>`이다. `--vfx-frames`를 추가하면 30fps PNG 전체 프레임을 저장한다. 기본은 기술별 windup/contact/follow/clear 4장으로 저장 공간을 제한한다. Editor의 세 번째 인자 true도 같은 기능이다. PNG는 검수 플레이어의 실제 렌더 프레임을 `ScreenCapture.CaptureScreenshotAsTexture`로 기록한 것이며, 일반 실행본의 숨김 시작·로그 검사와 구분한다.

Windows에서는 화면 크기 변경 후 2초간 초기화가 안정되기를 기다린 다음 격리 저장 기준 스냅샷을 기록한다. 초기 창 크기 자동 저장을 기술 실행 중 저장 변경으로 오인하지 않기 위한 순서다. 사용자 파일의 기준 스냅샷은 이 초기화 전부터 유지한다. 검수 완료 후 숨김 창에서도 종료 요청을 처리할 수 있도록 `Application.Quit`까지 `runInBackground`를 유지한다.

## 검수 범위와 한계

- 아군6명(크레스·민트·제이드·파라·티아·알펜), 기존5화 전장, 기본 줌,1920×1080에서 기존 `BattleDirector.Execute`를 실제 실행한다. 일반/축소 효과를 각각6회 연속 실행한다.
- 전투 시작 전에 타이틀을 닫고 각 촬영 직전에 `Hud.TitleOpen`이 false인지 검사한다. 전체 결과와 각 시퀀스의 `titleClosed`가 true인지 확인한다. 변경 전 기준 자료는 수정 검수기로 재촬영한 `D:/TalesTacticsHelpGuide/Logs/VfxUpgrade/before-visible`이며, 타이틀이 겹친 최초 촬영은 사용하지 않는다.
- 보이는 위치에서 기술을 사용할 수 있도록 런타임 유닛 배치·레벨·체력·MP·게이지를 준비한다. 지형/Content 에셋은 수정하지 않는다. 반격·임무 이벤트는 이 비교에서 끈다. 자동 시나리오 완주나 일반 전투 밸런스 검증은 아니다.
- 효과의 최대 활성 수, 풀 인스턴스 수, 실제 HP 변화, 격자 위치 유지, 카메라 줌, 시퀀스 전후의 실제 아군/모델6개, 각 시퀀스의 효과 종료, 마지막 Restart 뒤 소유 메시·재질 해제를 기록한다. 정상/축소 효과의 HP 결과가 같은지도 확인한다. contact/follow 샘플은 HP 변화 또는 CombatEffect 생성으로 감지한 실제 접촉 프레임을 기준으로 저장하므로 투사체 이동 시간이 추가돼도 준비 시간만으로 접촉을 추정하지 않는다.
- 각 실행은 새 GUID 하위 저장 경로를 만들고 초기 저장을 1회 생성한다. 화면 초기화가 안정된 뒤 격리 저장 기준을 기록하고, 사용자 파일은 초기화 전 기준으로 비교한다. 캡처 중 사용자가 별도 게임에서 저장하면 해시 검사는 실패하므로 결과 해석 시 구분해야 한다.
- before/after는 동일한 도구·기술 ID·해상도·기본 줌·배치를 사용해야 한다. 이미 생산 코드를 바꾼 상태에서 만든 화면을 before라고 부르지 않는다. 이미지 확대나 미술적 품질을 자동 통과 기준으로 대신하지 않는다.
- 30fps는 `Time.captureFramerate=30`으로 정한 캡처 표본 간격이다. 실제 기기 성능이나 유지 프레임률을 측정한 결과가 아니다.
- PlayMode `RepresentativeVfxPoolReuseReducedEffectsAndOwnedResourcesStayIsolated`는6종×일반/축소 순차 풀 재사용, 유효 메시 좌표, 종료, 전투 상태/RNG/timeScale 불변, 소유 리소스 정리를 검사한다. 실제 실행 결과는 별도 XML 증거가 있어야 통과로 기록한다.

## 현재 기준 자료

원본은 `D:/TalesTacticsHelpGuide/Logs/VfxUpgrade/before-visible`과 `after-final`이며, 결과 사본은 [before-result.json](Actual/before-result.json)과 [after-result.json](Actual/after-result.json)이다. 두 실행 모두 일반/축소 합계 12개 시퀀스, 오류 0개, 사용자/격리 저장 불변, `titleClosed=true`, 아군/모델 6개 유지와 정리를 기록한다. 일반/축소 HP 변화는 대표 기술 6종 모두 일치한다. 비교 페이지의 after 영상과 PNG는 최종 수정이 반영된 `after-final`에서 가져왔으며 이전 `after-visible` 자료를 사용하지 않는다.

전체 테스트와 집중 재검사는 서로 다른 실행으로 기록한다. EditMode는 358/358 통과했고, 2차 전체 PlayMode는 184/185 통과 후 실패한 궁극기 구분 검사를 수정해 별도 1/1 재검사를 통과했다. 단일 전체 실행의 185/185 통과로 합산하지 않는다.

Windows 최종 검수 실행 `c07bbc17a23941188f5ca72ab24be170`은 12개 시퀀스, 오류 0개와 실제 PNG 48개를 기록했고 사용자/격리 저장 불변까지 확인했다. 일반 실행본은 87.20초 숨김 시작·응답·로그 및 사용자 파일 8개 불변 검사만 수행했으며 타이틀 화면 수동 검수는 하지 못했다. 빌드 시간·경고 수·최초 검수의 초기화 저장 실패 이력과 결과 경로는 [README.md](README.md)의 검증 표를 따른다.

[manifest.json](Actual/manifest.json)은 소스 경로·결과 해시·촬영 조건을, `media-verification.json`, `png-verification.json`, `page-verification.json`은 변환된 영상과 복사한 PNG 및 비교 페이지의 검증 근거를 보관한다. 영상에는 오디오 트랙이 없으므로 효과음 청취 결과로 사용하지 않는다.
