# 초반 1~3화 조작과 이동 흐름

2026-11-03 방어 취소, 이전 대상 오발, 배속 보행과 타일 경계의 이동 지연을 수정했다. 실제 Unity 전체 **EditMode 469/469**, **PlayMode 287/287**, 일반·검수 Windows 빌드 및 **1~3화 수정 전후 Windows 실행**을 통과했다. 같은 자동 명령 조건에서 행동·성장·보상 결과가 일치했고, 두 실행 모두 사용자 저장 트리를 보존했다. 실제 화면은 아래에 보관한 정적 표본 3장만 확인했다.

## 적용한 수정

| 문제 | 적용한 동작 |
|---|---|
| 방어 버튼을 누르는 순간 행동권·이동 취소권·되감기 기록과 방어 효과가 확정되어, 방향 선택을 취소해도 상태가 남음 | 방향을 확정할 때 방어를 한 번 적용한다. 방향 선택 취소에서는 이동·행동 상태를 보존하고, 이후 대기에 취소한 방어가 섞이지 않도록 했다. |
| 유효 대상을 가리킨 뒤 빈 칸으로 옮겨도 이전 대상과 피해 예측이 남아 Enter로 공격할 수 있음 | 무효 칸으로 옮기면 대상과 예측을 비우고 기본 기술 범위를 복원한다. 다시 유효 대상을 고르기 전에는 이전 대상을 실행하지 않는다. |
| 적 이동 2·4배속이 보행 원화 재생에 전달되지 않음 | 실제 이동과 보행 프레임에 같은 명시적 배속을 적용한다. |
| 타일마다 이동 시간을 새로 재면서 초과한 프레임 시간을 버려 경로가 길수록 대기가 누적됨 | 전체 경로의 누적 시간으로 위치를 계산한다. 타일 경계를 넘은 시간을 다음 구간에 이어 쓰고, 마지막 프레임에서만 도착 시간을 반올림한다. |

기존 공격·기술의 준비·발동·복귀 곡선과 성장 수치·보상표는 유지했다. 새 표정 원화, 보상 다양화, 오디오 마무리와 장시간 성능 검수는 이번 완료 범위에 포함하지 않는다.

## 완료한 검증

| 검증 | 실제 결과 | 근거 |
|---|---|---|
| 전체 Unity EditMode | **469/469**, 23.36초, 실패·건너뜀·판정 불가 0 | [원문](Validation/full-editmode.json) |
| 전체 Unity PlayMode | **287/287**, 712.43초, 실패·건너뜀·판정 불가 0 | [원문](Validation/full-playmode.json) |
| 이동 관련 PlayMode 별도 실행 | **2/2**, 35.60초. 1·2·4배 보행 프레임, 타일 경계 시간, 도착 위치·대기 복귀와 논리 상태 보존 확인 | [원문](Validation/motion-after-result.json) |
| 일반 Windows 빌드 | **Succeeded**, 98.8486772초, 오류 0·경고 7 | [빌드 결과](Validation/windows-build-normal.json) |
| 검수 Windows 빌드 | **Succeeded**, 73.5150718초, 오류 0·경고 6, 보호 파일 불변 검사 통과 | [빌드 결과](Validation/windows-build-review.json) |
| Windows 수정 전·후 실행 | **각각 통과**, exit 0, 3/3화, 12/12 PNG, 영구 상태 JSON 6개, 저장 재로드 검사 25회, 강제 종료 없음 | [수정 전](Validation/windows-before.json) · [수정 후](Validation/windows-after.json) |
| 일반·검수 DLL 분리 | 일반 DLL의 검수 타입·참조 제외, 검수 DLL의 초기화·저장 격리·3화 선택 경로 확인 | [메타데이터/IL 검사](Validation/player-type-isolation.json) |
| wrapper 시간 처리 오프라인 검사 | Windows PowerShell **5.1: 14/14**, PowerShell **7.6.5: 14/14** | [PS5 원문](Validation/wrapper-timestamps-ps5.json) · [PS7 원문](Validation/wrapper-timestamps-ps7.json) |
| wrapper 프로세스 보호 오프라인 검사 | Windows PowerShell **5.1: 17/17**, PowerShell **7.6.5: 17/17** | [PS5 원문](Validation/wrapper-process-ps5.json) · [PS7 원문](Validation/wrapper-process-ps7.json) |

이동 관련 두 검사는 뒤의 전체 PlayMode 287개에도 포함된다. 별도 실행 횟수를 전체 테스트 수에 더하지 않는다. wrapper 검사는 모의 입력을 사용하는 PowerShell 회귀이며 Unity 테스트나 게임 실행 결과가 아니다.

일반 빌드의 경고 7개는 Pipeline 설정 1개, 기존 SdMotionDirectionReview obsolete 경고 4개, VfxUpgradeReview directive 경고 2개다. 경고 원문은 빌드 결과에 보존했다.

일반 실행본은 로컬의 D:/TalesTacticsBuildArchive/OpeningPolishWindows20261103에 있다. 일반 실행본의 확인 범위는 빌드와 DLL 분리 검사이며, 이번 자동 3화 실행은 검수 실행본으로 진행했다. [변경 소스 해시](Validation/source-manifest.json)를 별도로 보존한다.

## 1~3화 Windows 전후 결과

CampaignFlowReview의 선택형 --campaign-opening-profile을 기존 --campaign-flow-review GUID와 함께 사용했다. 메인 1~3화를 timeScale = 1로 진행하고, 3화 후일담·타이틀 저장 재로드를 거쳐 4화 준비 화면에서 끝냈다. 기본 20화·배율 4 검수 경로는 유지했다.

수정 전후 실행본을 같은 PC에서 순차 실행했으며, 새 GUID 저장 폴더·seed 17·편성·구매·자동 명령 정책을 맞췄다. 최종 실행 ID는 수정 전 c921967e47df482e8c83af71d547fa57, 수정 후 146a3e17a2a748c5ba103214987aa405다. 둘 다 정상 종료했고 wrapper 오류는 0개다. [수정 전 캠페인](Validation/campaign-before.json) · [수정 후 캠페인](Validation/campaign-after.json)

전후 실행 조건과 결정적 결과 비교가 모두 일치했다(conditionsEqual = true, deterministicOutcomesEqual = true). 출전·합류 동료, 행동 순서·횟수, HP/MP·레벨, 구매·보상 및 저장 해시가 같았다. 유닛별 공격·회복·지원 기술, 방어·대기·이동 집계와 아군 이동·기술·적 처리 시간은 [전후 비교 원문](Validation/windows-comparison.json)에 보존했다.

| 전투 | 수정 전 실측 | 수정 후 실측 | 차이 |
|---|---:|---:|---:|
| 1화 | 77.346초 | 77.239초 | -0.107초 |
| 2화 | 52.739초 | 52.680초 | -0.059초 |
| 3화 | 58.871초 | 58.788초 | -0.083초 |

이는 버전별 **1회, 배율 1의 자동 실행**이다. 차이가 작고 반복 측정도 하지 않았으므로 일반적인 성능 향상이나 사람이 체감하는 플레이 시간 단축으로 판단하지 않는다. 배속 보행과 타일 경계 수정은 별도 PlayMode 검사로 확인했다.

최종 수정 후 실행의 원본 PNG 3장을 보존하고 시각적으로 확인했다. 3화 후일담 표본에서는 한글 대사·파라 초상·다음/대화기록/자동 버튼의 깨짐이나 잘림이 보이지 않았다. 이 표본 검토를 전체 전투 수동 검수나 이동 모션 영상 검수로 확대해 표현하지 않는다.

- [1화 전투 시작 원본](Evidence/episode01-battle-start.png)
- [3화 전투 시작 원본](Evidence/episode03-battle-start.png)
- [3화 후일담 원본](Evidence/episode03-after-story.png)

[세 표본의 원본 경로·SHA와 확인 범위](Evidence/manifest.json).

## 저장과 기존 파일 보존

Unity 테스트를 포함한 전체 작업의 사용자 저장 트리는 4,960파일에서 **5,014파일·2,412디렉터리**로 바뀌었다. 기존 파일 중 TestResults.xml 1개가 변경됐고, StorageTests GUID 아래 검사용 파일 54개가 추가됐으며 기존 파일 삭제는 0개다. 실제 게임 저장과 설정은 변경되지 않았다. 따라서 **전체 작업의 사용자 트리가 불변이었다고 표현하지 않는다.** [전체 작업 트리 차이](Validation/whole-task-user-tree.json)

최종 Windows 두 실행은 각각 실행 직전의 사용자 전체 트리를 새 기준으로 검사했다. **각 실행에서 5,014파일·2,412디렉터리의 파일 내용과 디렉터리 목록이 유지**됐다. 사용 중인 최상위 Player.log, Player-prev.log만 해시에서 제외했으며, 검수 저장은 실행별 GUID 폴더로 격리했다. 기존 플레이어 검사와 자신이 시작한 플레이어 제어는 PID·시작 시각·실행 경로를 함께 확인한다. 두 실행 모두 강제 종료 없이 종료했다.

작업 시작 때 보호한 12개 파일은 모두 초기 SHA256과 같다. 그중 검증으로 바뀐 폰트, enemy-speed, tactical-scenarios 생성본 3개만 별도 보존한 뒤 초기 바이트로 복원했다. [보호 파일 복원 기록](Validation/protected-restoration.json) · [빌드 입력·기존 수정·기록한 Editor 상태 최종 대조](Validation/final-state.json)

## 초기 실패와 재검사

**수정 전 입력 검사:** 최초 3개 실행은 0/3, 100.40초였다. 이 중 하나는 Pipeline /api/exec 타임아웃 로그로 SetUp이 실패한 환경 오류이고, 두 개는 방어 취소 상태 잔류와 빈 칸에서 이전 대상 잔류를 검출한 실제 assertion 실패다. 세 개 모두를 제품 결함 재현으로 세지 않는다. 이후 수정한 소스의 전체 PlayMode 287/287에는 이 입력 검사도 포함된다. [최초 원문](Validation/controls-before-result.json)

**테스트 선택 0개:** category·이름 필터와 직접 호출에서 completed, 총 0개를 반환한 실행이 있었다. 목록 조회와 진단에서는 신규 이동 테스트 2개가 발견됐다. 재컴파일과 실제 스크립트 재로드 뒤 이동 검사 및 전체 PlayMode를 실행했으며, 0개 결과는 통과에 포함하지 않는다. 캐시·도메인 전환 등 정확한 원인은 확정하지 않았다. [0개 결과 예시](Validation/motion-before-script-result.json)

**첫 번째 Windows wrapper 실패:** 실제 게임은 3/3화와 12PNG 수집, exit 0, 사용자 파일 불변을 기록했지만 wrapper 보고서 시각 검사에서 실패했다. PowerShell 7의 ConvertFrom-Json이 만든 DateTime을 문자열로 다시 파싱하면서 UTC 정보가 손실된 문제였다. 문자열·DateTime·DateTimeOffset을 각각 UTC로 정규화하도록 고쳤고, 시간대 누락·손상값 및 기존 시작/종료 시각 범위 거부 조건을 유지했다.

**두 번째 Windows wrapper 실패:** 실행 직후 프로세스의 MainModule.FileName 조회에서 실패했다. 나중에 같은 PID·시작 시각·경로가 정상 확인되어 일시적인 모듈 정보 미준비 가능성에 부합하지만, 실패 당시 null 값을 직접 포착한 것은 아니다. 이후 게임의 3/3화 완료와 프로세스 종료는 별도로 관측했으나 exit code를 회수하지 못했으므로 성공 실행으로 세지 않는다. 모듈 정보가 없으면 최대 5초 재조회하고, PID·시작 시각·경로 불일치는 즉시 거부하도록 보완했다. 신원 확인 전 프로세스 신호를 보내지 않으며 실제 종료는 정상 처리한다.

두 실패의 passed = false 원문과 해시를 [실패 기록](Validation/windows-before-failures.json)에 보존했다. 이를 덮어쓰거나 사후 관측으로 성공 처리하지 않았다. 위 최종 Before/After는 수정한 wrapper로 새 GUID에서 실행한 별도 통과 결과다.

## 재현 도구와 해석 범위

- [로컬 빌드 도구](Validation/Build-OpeningPolish.cs)
- [Windows 3화 검수 도구](Validation/Validate-OpeningCampaignWindows.ps1)
- [시간 처리 오프라인 검사](Validation/Test-OpeningWrapperTimestamps.ps1)
- [프로세스 보호 오프라인 검사](Validation/Test-OpeningWrapperProcesses.ps1)

자동 대화와 자동 명령을 사용하므로 측정 시간은 사람의 플레이 시간·조작 발견성·체감 재미·전체 밸런스 평가가 아니다. 실측에는 프레임·CPU·저장 처리 대기가 포함되고, 적 처리 시간은 프레임마다 관측한 값이다. 공격·회복·지원 기술 횟수는 기술 효과 종류에 따른 집계여서 겹칠 수 있으며 실제 회복 HP 총량을 뜻하지 않는다. PNG 자동 유효성 검사와 앞서 명시한 정적 3장 시각 검토는 별개의 확인이다.

검수 도구의 소스 라벨은 실행자가 제공한다. 실행본의 DLL 해시, 빌드 소스 영수증과 원문 검증 결과 해시를 함께 보존한다. 검증을 마친 이번 변경은 develop의 로컬 커밋으로 보관한다. 사용자 제공 음원과 이를 포함한 실행본·ZIP은 **로컬에만 보관**하고, 이번 작업에서 원격 업로드나 공개 릴리즈는 하지 않는다.
