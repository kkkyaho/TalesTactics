# 보상 다양화 Windows 검증 도구

이 폴더의 도구는 로컬 검증용이며 원격 업로드나 릴리즈를 수행하지 않는다. 실제 Windows 실행·Unity 테스트·빌드 결과는 각각 별도 영수증으로 판정한다.

## 빌드

`Build-RewardVariety.cs`는 Assets 밖에서 Pipeline `run_script`로 실행하는 임시 소스다. 진입점은 `RewardVarietyBuild.Run`, 인자는 아래 경로 중 하나다.

- `D:/TalesTacticsBuildArchive/RewardVarietyWindows20261106`
- `D:/TalesTacticsBuildArchive/RewardVarietyReview20261106`

이미 존재하는 출력 폴더는 거부한다. 기존 프로젝트의 StandaloneWindows64 대상, clean 씬, 중지/컴파일 완료 상태만 사용한다. REVIEW는 빌드의 `extraScriptingDefines`만 사용하고 프로젝트 define·패키지·설정·Content를 변경하지 않는다. `Scheduled` 응답은 성공 판정이 아니다. 이후 `Logs/RewardVariety20261106/Build-<GUID>/build-result.json`의 실제 BuildReport, 오류/경고와 보호 파일 전후 SHA를 확인한다.

## 전후 캠페인

`Validate-RewardCampaignWindows.ps1`은 기존 OpeningPolish의 사용자 트리, PID/시작 시간/EXE 신원, UTC 파싱, PNG 검사를 유지한다. `--campaign-flow-review <GUID>`로 기존 20화 기본 프로필(timeScale 4)을 실행한다. 별도 사용자 저장이나 일반 실행본은 시작하지 않는다. `-Mode Inspect`는 DLL 메타데이터/IL만 확인하며 캠페인을 실행하지 않는다.

사전 실행 예:

```powershell
& 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe' -NoProfile -ExecutionPolicy Bypass -File 'C:/dev/TalesTactics/Docs/RewardVariety/Validation/Validate-RewardCampaignWindows.ps1' -Mode Review -NormalBuild 'D:/TalesTacticsBuildArchive/OpeningPolishWindows20261103' -ReviewBuild 'D:/TalesTacticsBuildArchive/OpeningPolishReview20261103' -Phase Before -SourceLabel 'OpeningPolish 73a0557 existing binary; source mapping recorded separately' -SourceManifestPath 'C:/dev/TalesTactics/Docs/RewardVariety/Validation/baseline-binary-source.json'
```

사후 실행은 NormalBuild/ReviewBuild를 새 RewardVariety 폴더로 바꾸고, `-Phase After -BaselineValidation '<위 사전 실행의 절대 validation.json 경로>'`를 지정한다. SourceLabel/SourceManifestPath는 실제 새 소스/빌드 증거를 가리켜야 한다. 기본 창은 Hidden이며 900초 제한을 넘으면 실패다. 기존 플레이어가 실행 중이면 기본적으로 시작하지 않는다. 명시적으로 `-AllowExistingPlayers`를 지정한 경우에도 기존 프로세스 신원과 사용자 파일은 계속 검사하고 건드리지 않는다.

필수 성공 조건은 20화 완주·48장 PNG·40개 저장 전후 JSON·각 화 저장 재로드·오류0·정상 종료0·사용자 트리 불변이다. 사용자 트리에서 제외하는 것은 최상위 `Player.log`, `Player-prev.log` 두 이름뿐이다. `Reward-CampaignContract.ps1`은 별도로 다음을 검사한다.

- 각 화 확정 보상 1개: Before는 메달, After는 1~6화 메달 및 7~20화 후보 A.
- 편성 1~6명과 Player 행 1~6개의 중복 없는 ID가 정확히 일치해야 한다. 이 전체 Player 목록의 성장을 검사한다.
- 초기 300G, 매 화 `120 + 50 × 화수` 골드, 각 동료 행동 EXP에 35 임무 EXP를 더한 저장 성장.
- 동일 seed17의 추가 드롭, 실제 구매 가격/잔액, 이전 재고+구매+보상의 수량, 저장 재고·골드·장착 수량.
- 실행 조건·구매 정책·편성의 동일성과 변경하지 않은 첫 6화 결과의 동일성.

7화 이후에는 새 보상을 자동 장착하거나 구매를 대체할 수 있으므로 행동·성장·소지금의 동일성을 강제하지 않는다. 전후 구매 기록·전투 지표·장비·성장·재고 차이를 보존한다. 보상을 받을 때 이미 보유한 사본 수, 누적 구매 사본 수, 미장착 사본 수와 산술 매각 가치도 기록한다. 실제 매각은 수행하지 않는다. 판매가 감소의 플레이 체감이나 난이도 승인은 자동 검증 범위가 아니다.

`equipment-prices.json`은 변경 전 Content 장비 27종의 가격·파일 SHA를 고정한 소스 자료다. 실행 시 원본 파일 SHA를 다시 확인한다. 런타임 카탈로그 추출을 뜻하지 않으며, 실제 바이너리와 소스의 대응은 별도 빌드 증거가 뒷받침해야 한다. 루트의 `catalog-source.json`은 보상·추가드롭 대상 14종의 카탈로그 참조/아이콘/슬롯을 보충한다.

## 도구 자체 검사

`Test-RewardWrapperContract.ps1`은 게임을 실행하지 않고 기존 증거를 읽고 새 GUID 폴더에 합성 보고서/저장을 만들어 검사한다. PS7.6.9와 Windows PS5.1에서 각각 26/26 통과했다([PS7](wrapper-contract-ps7.json), [PS5](wrapper-contract-ps5.json)). 보호 함수 13개는 개행 정규화 후 기존 검증 코드와 일치한다. 합성 후보 A의 매각가 합계는 1~20화 3850G, 7~20화 2410G이며 종전보다 950G 작다.

첫 probe는 26개 중 3개에서 실패했다. CRLF/LF를 문자 그대로 비교한 보호 함수 검사와 첫 6화의 합성 재고 배열을 정렬해 원본 순서가 달라진 fixture 문제였다. probe만 고치고 새 GUID로 두 셸에서 다시 실행했다. [첫 실패](wrapper-contract-attempt1.json)를 보존하며 실패한 실행을 통과로 바꾸지 않는다. 이 검사는 실제 Unity 테스트나 Windows 후보 A 플레이 결과가 아니다.

## 계약 v2 보강

읽기 전용 감사에서 Player 행이 누락된 보고서가 성장 검사를 건너뛸 수 있음을 발견했다. 실제 Before 실행이 끝난 뒤 계약을 보강해 편성과 Player의 인원 1~6명, 빈 ID/중복 없음, 대소문자를 구분하는 ID 집합의 정확한 일치를 강제했다. 빈 Player 목록, 일부 누락, 중복/추가/교체 Player, 중복/빈/7인 편성, 빈 ID와 대소문자 불일치의 실패 사례 10개를 추가했다. PS7.6.9와 Windows PS5.1에서 각각 **36/36** 통과했다([v2 PS7](wrapper-contract-v2-ps7.json), [v2 PS5](wrapper-contract-v2-ps5.json)). 이전 26개 결과를 덮어쓰거나 당시 검증 범위를 소급 확장하지 않았다.

실제 Before GUID `b4c6ce58d32a4107a82a4250dddddf7e`는 새 게임을 실행하지 않고 `Test-RewardBeforeContractV2.ps1`로 보강 검사했다. 원래 성공 validation/report/source mapping SHA, 저장 JSON 40개와 기존 Before 산출물 불변을 확인했고, 20화의 편성·전체 Player 성장·보상·거래 검사를 통과했다. [추가 검사 영수증](windows-before-contract-v2.json). 원본 Before validation/report는 그대로 보존하며 PNG·프로세스·사용자 트리 증거는 원래 실행 결과를 따른다. 이후 After 실행은 새 계약 v2를 사용한다.

## 최종 실제 실행 — 2026-11-10 KST

일반/REVIEW 빌드를 만들고 [빌드 입력·BuildReport·바이너리 연결](after-binary-source.json)을 새로 기록했다. SourceManifest 인자는 파일의 존재와 SHA를 보존할 뿐 그 내용의 정확성을 자동 검증하지 않으므로, 최종 입력2,320개와 실제 실행본을 별도로 대조했다. [최종 보존 대조](final-state.json).

After GUID `3db2b87c5ea449029bedbd714ca6a406`는 계약v2로 **20/20화·26,713검사·176재로드·48PNG·40저장 JSON·정상 종료0·오류0·사용자 트리 불변**을 통과했다. [After 출처](windows-after-summary.json) · [경제](windows-after-economy.json) · [전후 비교](windows-comparison.json). Before 원본을 다시 실행하거나 v2 보강 검사를 새 게임 실행으로 세지 않았다. 첫6화와 조건·추가 드롭이 동일하다. 실제 구매비는7290→7300G, 최종 골드5910→5900G였다. 매각·사람의 체감 평가·일반 플레이어 시작 검사는 수행하지 않았다.
