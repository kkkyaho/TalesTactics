# 검증 기록 — 2026-09-22

## 2026-09-27 — 6번 입력·배포

- GamepadPointer가 왼쪽 스틱 화면 커서, 방향키 활성 메뉴 순환, A 선택/타이밍, B 취소, LB/RB 회전, 오른쪽 스틱 확대/축소·누름 초기화를 제공한다. 기존 UI 클릭/전투 상태 경로를 사용하고 가상 Mouse나 OS 포인터 조작은 하지 않는다. 기본 UI submit과 중복되지 않도록 게임패드 사용 중 navigation 이벤트를 제어하고 마우스 전환/장치 해제 시 복원한다.
- Noto Sans CJK KR Regular 2.004 원본 OTF, 정적 TMP 문자563개/2048 아틀라스1개 및 동적1024 fallback을 포함했다. fallback의 Clear Dynamic Data On Build=1을 재조회했다. Resources 폰트가 사용자 미지정 HUD의 기본이며, 원본 폰트 저작권·OFL 전문은 StreamingAssets/Licenses에 포함한다. 파일 해시와 출처는 INPUT_DISTRIBUTION.md에 기록했다.
- 실제 Unity EditMode **90/90** 통과(0.45초, unity-input-editmode-results.json), PlayMode **47/47** 통과(60.29초, unity-input-playmode-results.json). 초기 실행도47/47이었다(61.01초, unity-input-playmode-initial.json). 신규 검사는 InputSystem 가상 Gamepad 이벤트로 A 중복 실행 방지, 메뉴 순환/스틱, 연결 해제/재연결, 전투 시작·이동 타일 선택·B 취소·카메라·타이밍을 확인한다. 별도 폰트 검사는 Resources 선택, 정적/동적 구성 및 카탈로그 한글/한자 표시를 확인했다.
- 실제 Unity API DLL 참조 Runtime/Editor/EditMode/PlayMode 4개 컴파일 통과. 엔진 독립 규칙을 바꾸지 않아 ManagedChecks는 재실행하지 않았다. 과거 ManagedChecks 결과를 Unity 검사로 재표기하지 않는다.
- 1920×1080 Game View에서 출전/전투 한국어와 게임패드 커서를 확인했다(input-deployment-preview.png, input-battle-preview.png). 폰트 변경 후 출전 인원 제목이 두 줄로 내려가는 것을 발견해 짧은 문구로 수정하고 재촬영했다. 이 문구 수정은 전체 테스트 후 화면 검수와 아래 최종 빌드에서 확인했다. 임시 가상 장치와 Play Mode는 종료했고 Scene/ProjectSettings를 변경하지 않았다.
- Windows 개발 빌드 **25.613초/오류0/경고8**, 일반 빌드 **20.785초/오류0/경고6** 성공(unity-input-development-build.json, unity-input-windows-build.json). 기존 Pipeline/개발 검수 API/직렬화/셰이더 경고가 남아 있다. 이후 개발 전용 검수 도구의 포커스 처리를 바꾸었으며 일반 빌드에는 해당 코드가 포함되지 않는다.
- 초기 개발 빌드26.755초/오류0/경고8은 성공했으나 숨겨진 플레이어의 상점 진입 입력 검사에서 실패했다(PlayerReviews/442a4ca75e9a47a98aee5fb387903349-summary.json). 검수 도구에 초기 화면 안정화 대기와 일시적 IgnoreFocus를 추가하고 종료 시 원래 정책을 복구했다. 일반 게임의 입력 정책을 바꾸지 않았다. 포커스와 초기 화면 배치 중 단일 원인을 분리해 확정한 것은 아니다.
- 최종 플레이어 **3회 실행 통과**: 1장27턴/16공격, 2장27턴/17공격, 재실행5턴/0공격. 각 실행에서 포함 폰트 선택/한글·한자/라이선스 파일, 가상 패드로 상점 진입·B 복귀·장치 제거, 음악14곡 출력, 정상 승패·재출전·중복 보상 방지를 확인했다. 사용자 저장/백업 불변. 증거: PlayerReviews/3f47658fcdbc40f38c7e4d8c7fd1348c-summary.json.
- 한계: 실제 USB/Bluetooth 기기·드라이버별 검수, 사람이 게임패드로 캠페인 완주한 결과는 아니다. 숨겨진 플레이어의 검은 캡처는 시각 증거로 사용하지 않는다. 7번의 수동 완주·장시간 성능 검수는 남아 있다.

## 통과한 검증

- Unity 6000.6.0f1, 실제 TestBattle/Content/TMP 에셋 import 및 생성 확인.
- 실제 Unity EditMode 34/34 통과: unity-test-run.json.
- 실제 Unity PlayMode 5/5 통과: unity-playmode-results.json.
- 전장 카메라 수정 후 PlayMode 5/5 재실행 통과: unity-playmode-viewport.xml. 출전 버튼, 참조/폰트, 전체 타일 viewport 포함, 이동/취소, 공격 미리보기/실행/턴 종료, 승리/패배/재시작 검증.
- Windows x64 플레이어 빌드 성공. 카메라 수정 후 재빌드 성공: windows-build-results.json에 Unity 빌드 결과와 실행 파일/런타임 DLL SHA256 보관.
- Computer Use로 Windows 실행 파일 검수: 출전, Lv25 훈련 전투 시작, 한글과 파라 궁극기의 일본어 한자, 전장 전체 표시, 실제 타일 클릭 이동, 이동 취소, 스킬 목록과 취소, 방향 선택과 다음 유닛 턴 전환, Restart 출전 화면 복귀.
- 엔진 독립 규칙 테스트 34/34 통과: managed-test-results.txt. 이는 Unity 실행과 별개의 .NET 대체 API 검사다.
- 실제 프로젝트 Library DLL 참조 API 컴파일: Runtime/Editor/EditMode/PlayMode 4개 통과 (api-compile-results.txt).

## 이번에 수정한 문제

- 출전 전 Catalog, Rules, roster, 공격 및 스킬 참조를 검증하고 누락 시 원인이 드러나는 오류 화면을 표시한다.
- TMP Essentials 비동기 import가 끝난 뒤 Scene을 생성한다. 미완성 Scene으로 빌드하지 않는다.
- Windows 맑은 고딕과 Yu Gothic 대체 폰트를 사용하고 소유한 동적 폰트 리소스를 정리한다.
- 최초 PlayMode 검증에서 fallbackFontAssetTable null로 5개 모두 초기화 실패했다. 목록 초기화 후 모두 통과했다. 실패 근거는 unity-playmode-initial-failure.json.
- 좁은 창에서 UI가 전장을 가리는 현상을 확인했다. UI 바깥 viewport에 카메라를 배치하고 전장 bounds 전체를 맞추도록 수정했다. 수정본에서 클릭 이동과 취소를 다시 확인했다.
- Unity가 Scene에 추가한 URP 카메라/조명 컴포넌트와 빌드 시 직렬화한 URP 설정을 보존했다.

## 한계 및 후속 검수

- 완성 게임이 아닌 임시 아트 전투 프로토타입이다. 최종 캐릭터 아트/애니메이션/음원은 포함하지 않는다.
- 실행 파일에서 전투를 끝까지 수동 플레이한 승리/패배 검수는 아직 없다. 승패와 재시작은 실제 Unity PlayMode 테스트에서 검증했다.
- 세로 창 HUD 재배치와 확대 조작은 아래 후속 검증에서 완료했다.
- 장시간 성능/메모리/저장 마이그레이션, 10명 전체 기술의 수동 검수는 미완료다.
- 빌드에는 Pipeline RuntimePipelineConfig 미지정, URP 디버그 셰이더 stripping, TMP 셰이더 pragma, Unity UAC1001 직렬화 분석기 경고가 있었다. 빌드는 성공했으며 런타임 Pipeline 원격 제어는 이번 검수에 사용하지 않았다.

## 작업 도구

CLI는 codexsandboxonline 계정에서 Pipeline 연결 파일을 읽지 못한다. 파일 읽기 승인만으로 Windows ACL이 바뀌지는 않았다. Computer Use 플러그인의 node_repl + @oai/sky를 통해 Unity 메뉴, Test Runner, 빌드 및 플레이어를 직접 제어할 수 있다. 브라우저 cua 도구의 native 비활성화는 이 별도 경로의 비활성화를 의미하지 않는다.

실행 파일은 Builds/Windows/TalesTactics.exe. Builds, Library, Logs 등은 Git에서 제외한다. 배포는 폴더 전체가 필요하며 exe 하나만 복사하지 않는다.

## 후속 단계 — 반응형 전투 HUD와 카메라

- 세로 창(화면 비율 1.2 미만)에서 유닛 정보와 명령을 전장 아래 2열로 배치했다. 넓은 화면에서는 기존 양쪽 패널을 유지한다.
- 회전/확대/축소/초기화 버튼 및 전장 위 휠 확대를 추가했다. 전투 시작과 재시작 시 기본 구도로 복원한다.
- 실제 Unity PlayMode 6/6 통과: unity-playmode-camera-buttons.xml. 카메라 버튼으로 네 방향 회전 후 전체 타일 표시, 확대/초기화, 전투 상태 보존과 재시작을 확인한다. 중간 결과는 unity-playmode-camera.xml.
- Runtime/Editor/EditMode/PlayMode 실제 API 컴파일 4개 통과. 최종 Windows 빌드 성공(9.102초); windows-build-results.json 갱신.
- Windows 실행 파일 604×932 창에서 하단 HUD, 전체 전장, 스킬 목록/취소 표시, 우회전 버튼, 회전한 전장 타일 클릭 이동/취소, 확대 버튼 및 초기화 복원을 직접 확인했다. 전장 위 휠 확대도 확인했다.
- Q/E/Home 단축키 코드도 포함했지만 Computer Use의 문자 키 입력에서는 회전 반응을 확인하지 못했다. 물리 키보드 입력 검증은 남아 있으며, 마우스 버튼으로 모든 카메라 기능을 사용할 수 있다.
- 최종 방향별 아트의 카메라 각도별 선택, 카메라 이동(pan), 매우 작은 창/초광폭의 모든 조합 검수는 후속 작업이다.

## 후속 단계 — 스킬 상세와 사용 조건

- 스킬 목록에서 사용 불가 스킬도 조회할 수 있다. MP/HP/게이지 비용, 사거리/범위, 대상, 해금 레벨, 재사용 대기, 효과와 현재 사용 불가 사유를 표시한다. 사용 가능한 스킬만 목표 선택으로 진행한다.
- 복합 효과 미리보기에 시전자 적용 여부와 부활/해제/이동/게이지 등 누락된 효과를 표시했다. 피해·회복은 계산값이며 회복 상한/복합 효과 순차 적용까지 예측하는 전체 시뮬레이터는 아니다.
- 엔진 독립 테스트 36/36 및 실제 Unity API 어셈블리 4개 컴파일 통과.
- 실제 Unity EditMode 36/36: unity-editmode-skill-details.xml. 실제 PlayMode 7/7: unity-playmode-skill-details.xml.
- 새 테스트는 실제 HP 비용 표시, 복합 효과 시전자 표기와 자원 불변, MP 부족 스킬 조회 및 목표 선택 비활성, 자원 복구 후 목표 선택 진입을 확인한다.
- Windows 빌드 성공(9.878초). 604×932 실행 창에서 Lv1 파라의 잠긴 삼산화 상세/해금 Lv4/사용 불가 사유, 목록 복귀, 장저파 상세/사용 가능/목표 선택/사거리 표시를 직접 검수했다. 조회 후 HP190/MP85와 행동 가능 상태가 유지됐다.
- 상세 UI에서 10명 전체 스킬의 긴 설명 조합, 다수 AoE 대상의 미리보기 줄바꿈은 추가 검수 대상이다.

## 후속 단계 — 출전 전 장비 관리

- 무기/방어구/장신구 초안, 변경 전후 능력치, 적용·저장과 미적용 취소를 구현했다. 호환되는 무기 종류와 슬롯만 허용하고 저장 실패 시 기존 장비를 유지한다.
- 저장된 장비를 캠페인 및 Lv25 훈련 전투에 적용한다. 훈련의 성장 저장 규칙은 유지한다.
- 엔진 독립 규칙 39/39, 실제 Unity API 어셈블리 4개 컴파일 통과.
- 실제 Unity EditMode 39/39: unity-editmode-equipment.xml. PlayMode 8/8: unity-playmode-equipment.xml.
- 새 규칙 검사는 잘못된 슬롯/무기, 저장 실패 복원, 초안 분리, 알 수 없는 ID와 짧은 구형 배열을 검증한다. PlayMode는 UI 장비 선택/취소와 저장된 장비의 훈련 반영을 검증한다.
- Windows 빌드 성공(10.833초). 604×932 실행 창에서 크레스 청동검 선택 시 STR 42→46, 적용·저장 성공 및 종료/재실행 후 청동검과 STR46 유지를 직접 확인했다.
- 검수용 로컬 캠페인에는 크레스의 청동검 장착을 남겼다. UI에서 무기 슬롯을 다시 누르고 저장하면 해제된다.
- 현재 카탈로그에는 청동검 한 개만 있다. 방어구/장신구 슬롯은 교체할 데이터가 없어 비활성화된다. 수량 제한·획득·상점은 아직 구현하지 않았다.

## 후속 단계 — 저장 데이터 보호와 복구

- Version 1의 누락 컬렉션/장비 배열, 레벨·경험치 범위를 보정한다. 빈/중복 캐릭터 ID는 거부한다. 다른 스키마 버전으로의 변환은 이번 범위에 포함하지 않는다.
- 손상 또는 누락된 기본 파일은 유효한 .bak에서 읽는다. 복구 후 저장 시 손상 원본을 .corrupt-GUID로 보존하고 정상 백업을 덮어쓰지 않는다.
- 복구 불가 또는 미래 버전 기본 파일은 자동 저장을 차단하고 출전 화면에 안내한다. 차단 세션의 진행은 저장되지 않는다. 원본 복원 또는 호환 버전 사용 후 재실행이 필요하다.
- 실제 Unity EditMode 45/45 통과: unity-editmode-storage.xml. PlayMode 회귀 검사 8/8 통과: unity-playmode-storage.xml. 별도 엔진 독립 45개 및 실제 Unity API 4개 컴파일 통과.
- 추가 검사는 성장/승급/장비 왕복 저장, 이전 백업, 누락 필드, 손상 복구 및 원본 보존, 미래 버전/복구 불가 덮어쓰기 차단, 중복 ID 백업 복구를 다룬다. 각 검사는 GUID 테스트 디렉터리에서 실행하며 실제 campaign.json을 수정하지 않는다.
- 여러 게임 프로세스의 동시 저장, 저장 중 전원 차단, 디스크 고갈의 강제 실패 검수는 미완료다.
- Windows 빌드 성공(10.050초). 실행 파일에서 기존 캠페인의 크레스 청동검/STR46을 정상 로드하고 장비 저장 성공 메시지를 확인했다. 복구·차단 안내의 실제 화면 검수는 미완료이며 해당 파일 동작은 Unity EditMode로 검증했다.

## 후속 단계 — 성장·승급 화면

- 저장된 레벨/EXP와 장비 포함 능력치의 승급 전후 비교, 고정 직업/승급 직업, 레벨 및 스토리 조건을 표시한다. 훈련의 Lv25는 승급 판정에 사용하지 않는다.
- 조건을 모두 만족한 캠페인 캐릭터만 1회 승급한다. 저장 실패 시 Promoted를 복원하고 재시도할 수 있다. 저장 보호/훈련 모드에서 UI 승급을 막는다. 전투 HUD도 승급 직업명을 표시한다.
- 엔진 독립 48/48 및 실제 Unity API 4개 컴파일 통과. 실제 Unity EditMode 48/48: unity-editmode-promotion.xml. PlayMode 9/9: unity-playmode-promotion.xml.
- 새 테스트는 조건 미달/스토리 미달, 1회 저장, 실패 후 재시도와 EXP/장비 유지, 미설정 승급 데이터 차단을 검증한다. PlayMode는 조건 UI, 미리보기 불변, 완료 버튼 비활성 및 캠페인 전투 보너스/HP 반영을 확인한다. 성공 승급은 실제 사용자 파일 대신 격리된 저장 콜백을 사용했다.
- chapter2는 화면에서 '2장 완료'로 표시한다. 스토리 이벤트가 아직 없으므로 신규 캠페인에서 정상 플레이로 이 조건을 획득하는 경로는 후속 단계다. 테스트 외 기존 캠페인에 조건/레벨/승급을 강제로 부여하지 않았다.
- Windows 빌드 성공(9.969초). 604×932 창에서 성장 메뉴→크레스 선택, Lv1/EXP0/100, 청동검 포함 STR46→54, HP190→230, Lv20/2장 완료 미충족 및 승급 버튼 비활성을 직접 확인했다. 조건 충족 상태의 실제 파일 저장 클릭 및 10명 전체 화면은 추가 수동 검수 대상이다.

## 후속 단계 — 캠페인 순차 해금

- 1장/2장 선택, 이전 장 완료 전 잠금, 캠페인 승리 시 chapter1/chapter2 저장 및 승급 조건 연결. 두 장은 테스트 맵을 재사용하며 적 레벨은 각각 1/3이다. 반복 승리는 EXP120을 지급하되 완료 플래그는 중복하지 않는다.
- 저장 실패 시 성장/스토리 기록을 복원하고 결과 화면에서 재시도한다. 성공 후 중복 지급하지 않으며 실패 상태에서 출전 화면으로 복귀하면 보상을 포기한다.
- 실제 Unity EditMode 50/50: unity-editmode-campaign.xml. 실제 PlayMode 10/10: unity-playmode-campaign.xml. 별도 엔진 독립 50개와 Unity API 참조 컴파일 4개도 통과.
- 새 테스트는 순차 해금/반복 승리/저장 왕복/실패 복원, 잠긴 장 진입 차단, 승리 저장 실패→재시도→다음 장 클리어 및 패배 시 저장 미호출을 확인한다. PlayMode는 사용자 파일을 쓰지 않는 저장 콜백을 사용한다.
- Windows 빌드 성공(14.399초). 604×932 창에서 두 장 버튼 전체 표시, 잠긴 2장 클릭 시 1장 선택 유지, 캠페인 1장 전투 시작, 중도 복귀 후 2장 잠금 유지를 확인했다. 16:9 화면에서 장 버튼이 하단에 걸쳐 배치를 위로 보정하고 PlayMode를 재실행했다.
- 실제 파일로 두 장 연속 승리 후 재실행하는 수동 검수, 전용 맵·대사·서사 및 장/성장 밸런스는 후속 작업이다. 캠페인 데이터에 완료 플래그를 강제로 추가하지 않았다.
- 화면 접근 오류로 중단됐다가 Windows 화면 제어 복구 후 검증을 완료했다.

## 후속 단계 — Windows 실제 파일 캠페인 자동 검수

- 2026-09-22, 개발 빌드의 별도 Windows 플레이어 프로세스를 세 번 실행했다. 정상 이동/일반 공격/UI submit 이벤트로 1장과 2장에 승리하고 실제 `CampaignFile` 저장을 다음 프로세스에서 읽었다. 강제 피해·승리/완료 플래그 주입은 사용하지 않았다.
- 출전 인원은 크레스·벨벳·파라·나탈리아·알펜·키사라 6명이다. 1장 플레이어 26턴/공격 16회, 2장 30턴/공격 16회. 출전 전원 Lv2/EXP140과 chapter1/chapter2 기록, 중복 보상 방지를 확인했다.
- 세 번째 실행에서 완료 UI/저장 로드를 확인한 뒤 민트 1명으로 대기하여 적의 정상 공격으로 패배했다. 패배·재출전 후 파일 바이트 불변, 재출전 전원 HP 회복도 통과했다.
- GUID로 격리한 저장 슬롯을 사용했다. 실제 사용자 `campaign.json`과 `.bak`는 실행 전후 해시가 동일하거나 양쪽 모두 없는 상태임을 확인했다. 일반 저장을 읽는 시작 코드는 유지하며, 검수 시작 후 별도 CampaignFile과 보상 저장 콜백으로 교체한다.
- 최종 근거: `PlayerReviews/e35af45cccba4f84925c1741b7ca5edb-summary.json`. 각 단계 보고서와 실행한 플레이어/Runtime DLL SHA256 포함. 최초 실행의 `fef810a4eed14afea819066a7483a2f2` 보고서는 캡처 경고 처리 추가 전 결과다.
- 실제 Unity PlayMode 회귀 검사 10/10 통과: `PlayerReviews/unity-playmode-review.json`. 이번 작업에서 EditMode 50개를 새로 실행한 것은 아니다.
- 최종 개발 빌드 10.879초/경고 5개/오류 0개, 일반 Windows 빌드 20.300초/경고 6개/오류 0개. `PlayerReviews/unity-development-build.json`, `unity-release-build.json`에 기록했다.
- `PlayerReviews/build-code-isolation.json`: DLL 타입 메타데이터로 CampaignPlayerReview가 개발 빌드에 존재하고 일반 배포 빌드에는 없음을 확인했다. `Builds/Windows/TalesTactics.exe`를 갱신했다.
- **한계:** 4배 시간 배율·자동 submit/타일 상태 호출이며 물리 마우스 수동 검수가 아니다. 숨김 실행의 캡처는 검은 화면이라 시각 검수 증거로 사용할 수 없다. 최종 도구는 이를 `captureWarnings`로 기록하고 해당 이미지를 저장하지 않는다. 전체 스킬, 장시간 성능/메모리, 수동 두 장 완주 검수는 여전히 남아 있다.
- 재실행: `Tools/RunCampaignReview.ps1`. 빌드/검수 상세는 `PLAYER_REVIEW.md` 참조.

## 작업 도구 연결 상태 갱신

- GitHub 플러그인의 TalesTactics 저장소 접근과 로컬 Git 원격 접근 확인 완료.
- Unity CLI 연결은 복구됐다. `Library/Pipeline/.unity-pipeline-port` 자리에 생긴 빈 디렉터리를 백업 이름으로 옮기고 사용자가 Window → Pipeline → Start Server를 실행한 뒤 ready 상태가 됐다. 이번 테스트/빌드는 실제 연결된 Unity CLI로 수행했다.
- 이전 ACL 연결 실패 기록은 과거 상태다. 현재 셸/이미지/Computer Use 도구의 샌드박스 초기화 오류는 별개이며, 이번 작업은 승인된 셸로 실행했다.

## 후속 단계 — 장비 재고·골드·상점

- Version 2 저장에 소지금과 장비별 보유 수량을 추가했다. 신규/Version 1 이관 시 300G·최소 청동검 1개를 제공하며 기존 캐릭터 장착분을 모두 보존한다. 로드만으로 파일을 바꾸지 않고 첫 저장 시 원본 .v1.bak를 남긴다.
- 상점 구매와 보유 수량 내 장착, 구매 실패/예외/재시도의 소지금·수량 복원, 여러 캐릭터 초안 사이 장착 경쟁 재검사를 구현했다. 상점은 청동검100G·가죽갑옷150G·생명의부적100G. 기존 청동검 능력치를 유지하고 새 에셋은 Editor API로 생성했다.
- 캠페인 1장120G/2장180G를 EXP·완료 플래그와 함께 저장한다. 실패 시 전부 복원하고 성공 이후 중복 지급을 막는다. 훈련에서는 구매와 보상을 막고 저장된 장비 사용은 유지한다.
- 실제 Unity EditMode **57/57**, PlayMode **12/12** 통과: unity-editmode-inventory.json, unity-playmode-inventory.json. 새 검사는 구버전 장비 보존·원본 백업, 구매 실패/예외·수량/가격 제한, 초안 경쟁·해제 실패, 실제 파일 구매/장착 왕복, 손상 재고 차단, 골드 보상 복원/상한을 확인한다. PlayMode는 실제 상점 버튼을 통한 실패/재시도, 장착/타 캐릭터 차단 및 전투 능력치를 확인한다. 저장은 격리 경로를 사용했다.
- 별도 .NET 엔진 독립 규칙 검사 **57/57**도 통과했다. Unity 테스트와 다른 실행이다. ManagedChecks/Run.ps1은 분리된 테스트 소스 파일도 컴파일하도록 확장했다.
- Unity Play 모드 1920×1080 실제 GameView 캡처 shop-editor.png에서 한글·가격·잔액·장비 능력치·수량·구매/돌아가기 버튼을 확인했다. 임시 캠페인과 저장 실패 콜백을 써 사용자 저장을 쓰지 않았다. 세로/초소형 창 상점의 추가 수동 검수는 남아 있다.
- 최종 개발 Windows 빌드 **20.317초·오류0·경고8**, 일반 Windows 빌드 **16.473초·오류0·경고6**. unity-inventory-development-build.json, unity-inventory-release-build.json에 근거를 보관했다. Builds/Windows/TalesTactics.exe 갱신 완료.
- 실제 Windows 플레이어를 세 번 실행한 상점/캠페인 검수 통과: PlayerReviews/cba78b9147494d6cb61352fb89c90759-summary.json. UI 구매·장착 → 정상 전투 1·2장 승리 → 프로세스 재실행 후 가죽갑옷 1개/크레스 장착/450G/출전6명 Lv2·EXP140 유지 확인. 패배 후 저장 불변·재출전 HP 회복 확인. 사용자 campaign.json/.bak는 실행 전후 동일함을 검사했다.
- 한계: 상점 수치는 초기값이며 매각·랜덤 드롭·장별 판매 목록은 아직 없다. Windows 검수는 개발 빌드의 자동 이벤트와 4배 시간 배율이며 물리 마우스 수동 검수가 아니다. 숨김 플레이어의 검은 캡처 경고는 별도 기록하고 시각 검수 통과로 간주하지 않는다. 일반 배포 빌드에는 자동 검수 코드가 포함되지 않는다.

## 후속 단계 — 미장착 장비 매각

- 장비 상점 → 장비 매각에서 보유 장비를 조회하고 미장착분만 1개씩 매각한다. 구매가의 절반(소수점 버림)을 지급하며, 매각가 0/장착분만 남은 경우/골드 상한 초과/훈련/저장 보호 상태를 차단한다. 기존 Version 2 저장 형식을 유지한다.
- 저장 실패 또는 예외 시 골드·수량·재고 순서를 복원한다. 마지막 수량 매각 시 빈 재고 항목을 제거하며 장착 슬롯은 변경하지 않는다.
- 실제 Unity EditMode 59/59, PlayMode 13/13 통과: unity-editmode-sale.json, unity-playmode-sale.json. 신규 검사는 장착분 보호, 골드 상한, 소수점 버림, 실패/예외 복원, 실제 UI 실패→재시도→격리 파일 재로드, 훈련 차단, 빈 목록을 확인한다. 사용자 저장 대신 임시 GUID 경로를 사용했다.
- 별도 엔진 독립 규칙 검사 59/59 통과. 이는 Unity 테스트와 별개다.
- Windows 배포 빌드 성공: 12.585초/오류0/경고3. unity-sale-release-build.json에 결과 보관. Builds/Windows/TalesTactics.exe 갱신. 경고는 Pipeline 런타임 설정 누락, 기존 DEVELOPMENT_BUILD 지시자 및 nullable Target 직렬화 안내다.
- 한계: 이번 매각 화면의 실제 마우스/스크린샷 시각 검수와 Windows 플레이어 매각 재실행 검수는 수행하지 않았다. 실제 버튼 submit과 파일 재로드는 Unity PlayMode에서 검증했다. 드롭·상위 장비·장별 판매 목록 및 경제 밸런스는 후속 작업이다.

## 후속 단계 — 장별 상점 해금과 상위 장비

- 1장 완료 기록으로 철검(240G/STR+8), 강화 갑옷(280G/HP+35/DEF+6)을 해금한다. 기존 3개 상품과 사용자 에셋 수치는 유지한다. ChapterShopContent는 새 에셋만 생성하고 카탈로그에 추가하며 기존 에셋은 덮어쓰지 않는다.
- 잠긴 상품도 상세 조회가 가능하며 목록의 잠김 표기/상세의 해금 조건/구매 비활성을 제공한다. 실제 구매 함수에서도 완료 기록을 재검사한다. 기존 보유 장비의 매각/장착은 해금과 독립적이다. 저장 Version 2 유지.
- 실제 Unity EditMode 61/61 및 PlayMode 14/14 통과: unity-editmode-chapter-shop.json, unity-playmode-chapter-shop.json. 보상 저장 실패 시 잠금 유지, 성공 후 해금, 격리 파일 구매/재로드, 미지정 조건과 알 수 없는 조건, 실제 UI 잠금→해금→구매 및 장착 후 전투 STR을 검증했다. 사용자 캠페인을 수정하지 않았다.
- 별도 .NET 엔진 독립 규칙 61/61 통과. 이는 실제 Unity 실행 결과와 별개다.
- 한계: 해금 UI는 PlayMode 자동 submit으로 확인했으며 이번 변경의 수동 마우스/시각 검수와 Windows 실행 파일 완주 검수는 수행하지 않았다. 랜덤 드롭과 경제 수치 조정은 후속 작업이다.
- Windows 배포 빌드 성공: 12.568초/오류0/경고6. 결과는 unity-chapter-shop-release-build.json에 보관했고 Builds/Windows/TalesTactics.exe를 갱신했다.

## 후속 단계 — 전투 승리 장비 보상

- 1장 생명의 부적 1개/2장 철검 1개를 매 승리 확정 지급한다. EXP·골드·완료 기록과 한 번에 저장하며 실패/예외 시 장비 수량도 복원한다. 동일 전투의 성공 이후 재호출은 지급하지 않고, 훈련·패배에서는 지급하지 않는다.
- 장비별 99개 상한일 때 해당 장비만 건너뛰며 결과 메시지에 미지급 이유를 표시한다. 다른 승리 보상은 정상 지급한다. 기존 Version 2 형식과 사용자 에셋을 유지한다. 확률 추첨/적별 드롭 테이블은 이번 범위에 포함하지 않는다.
- 실제 Unity EditMode 63/63, PlayMode 16/16 통과: unity-editmode-loot.json, unity-playmode-loot.json. 실패/예외 복원·재시도·반복 승리·파일 재로드·잠긴 장 차단·보유 상한·결과 메시지·훈련/패배 미지급을 검증했다. 별도 .NET 엔진 독립 검사도 63/63 통과했으며 Unity 실행과는 구분한다.
- PlayMode에서는 일부 승패 상태를 강제하여 보상 분기를 검사했다. 정상 전투 완주 근거는 별도 Windows 검수 결과를 따른다. 새 결과 메시지의 수동 시각 검수는 수행하지 않았다.
- 실제 Windows 개발 플레이어 3회 실행 검수 통과: PlayerReviews/c8b79d54287c45189a6751b16b22e02d-summary.json. 정상 이동/일반 공격으로 두 장 승리 후 별도 프로세스에서 생명의 부적1개·철검1개, 가죽 갑옷 장착·450G·Lv2/EXP140 유지 확인. 정상 적 공격에 의한 패배 후 저장 불변도 확인했다. 기존 사용자 저장/백업 해시는 실행 전후 동일했다.
- 개발 빌드 20.888초/오류0/경고8, 일반 배포 빌드 16.010초/오류0/경고6 성공. unity-loot-development-build.json 및 unity-loot-release-build.json 보관, Builds/Windows/TalesTactics.exe 갱신. Windows 검수는 자동 이벤트·4배 시간 배율이며 수동 마우스 검수와 다르다.

## 후속 단계 — 두 장 전용 맵과 이야기

- CampaignContent로 1장 11×9 외곽 물길, 2장 12×10 심층 계단/제단과 장별 6명/4명 출전 좌표를 구현했다. 기존 TestBattle 씬과 캐릭터 에셋을 재생성하지 않았다. 훈련은 원래 10×9 맵을 유지한다.
- 창작 시나리오 '경계의 빛': 1장 전5/후4, 2장 전5/후5, 총19개 대사. 시작 전 자동 표시, 다음/마치기/건너뛰기, 출전 화면 다시 읽기, 승리 저장 성공 이후 후일담 버튼을 제공한다. 대화 진행 자체는 저장을 호출하지 않는다.
- 실제 Unity EditMode65/65, PlayMode18/18 통과. unity-editmode-story.json, unity-playmode-story.json. 전 맵 이동 연결성/10개 출전 좌표, 장별 대사, 잠금/보상 실패, 이야기 재생·건너뛰기·재조회, 장별 맵 선택/카메라 범위를 검증했다. 별도 .NET 엔진 독립 검사65/65도 통과했다.
- 1920×1080 GameView에서 story-intro.png, chapter1-map.png, chapter2-map.png를 확인했다. 처음 확인한 이야기 잔상은 전체 화면 배경 카메라와 불투명 이야기 패널로 수정했으며 수정 후 PlayMode18개 재통과/이미지 재확인했다. 검수는 임시 캠페인/저장 실패 콜백으로 사용자 저장을 쓰지 않았다.
- 한계: 최종 배경 아트·초상화·음성은 포함하지 않는다. 대사 위치/전투 중간 상태는 저장하지 않는다. 새 이야기의 세로·초소형 창 시각 검수 및 물리 마우스 완주는 미완료다. 이번 스토리는 분기 없는 두 장 완결이며 추가 장은 포함하지 않는다.
- Windows 개발 플레이어 3회 실행 검수 통과: PlayerReviews/1ef829e115064004a97d8216707a4635-summary.json. 새 전용 맵에서 정상 전투로 1장27턴/공격15회, 2장24턴/공격16회 승리, 도입·후일담 진행, 후일담 조회 전후 저장 바이트 불변, 프로세스 재실행 후 성장/보상/완료 기록 유지 및 정상 패배 후 저장 불변을 확인했다. 사용자 저장/백업 해시 동일. 자동 이벤트/4배 시간 배율 검수이며 수동 마우스 완주와 구분한다.
- 개발 Windows 빌드20.852초/경고8/오류0, 일반 Windows 빌드14.736초/경고6/오류0 성공. unity-story-development-build.json, unity-story-release-build.json 보관. Builds/Windows/TalesTactics.exe 갱신.

## 후속 단계 — 스킬 범위·높이 차·시야

- SkillGeometry로 기존 마름모와 새 직선 관통/부채꼴을 구현했다. 스킬별 최대 높이 차(-1이면 무제한), 장애물/높은 중간 지형 시야 차단을 지정할 수 있다. 기본값은 기존 동작을 유지한다. 대각 모서리는 양 옆칸을 검사하며 유닛은 시야를 막지 않는다.
- 나탈리아 피어싱 라인(직선/사거리4/높이차2), 파라 쌍장저파(부채꼴/사거리3/높이차1), 나탈리아/시온 기본 공격(기존 사거리/높이차2)에 시야 필요를 적용했다. 비용/위력/해금 레벨은 유지했다. 전 에셋 재생성 없이 Unity Editor API로 해당 4개만 수정했다.
- Targets/Execute/노란 영향 타일은 AreaTiles를 공유한다. 원거리 기본 공격 AI도 InRange에서 같은 시야/높이 규칙을 따른다. 잘못된 목표 선택 시 이전 목표를 비워 실수로 기존 목표를 실행하지 않게 했다.
- 실제 Unity EditMode69/69, PlayMode19/19 통과: unity-editmode-geometry.json, unity-playmode-geometry.json. 직선 복수 대상/부채꼴 회전/각도·사거리 경계/모서리·높은 지형 차단/기본 호환/불가 공격 자원 불변, 실제 씬의 직선 미리보기·장애물 제외·복수 피해를 확인했다. 별도 .NET 엔진 독립 검사69/69 통과.
- PlayMode 결과 파일이 도메인 전환 중 잠시 없었으나 이후 completed/19 passed 결과를 확인하고 보관했다. 이번 대표 기술의 물리 마우스/시각 검수, 전체 스킬별 튜닝 및 속성 저항은 후속 작업이다.
- Windows 개발 플레이어 3회 실행 통과: PlayerReviews/76b85784f8d54f8e995d81b11ad7d8dd-summary.json. 새 원거리 시야 제한으로 1장27턴/공격16회, 2장22턴/공격17회 정상 승리, 후일담/재실행 저장 유지 및 패배 시 저장 불변을 확인했다. 사용자 저장/백업 해시 동일. 자동 이벤트·4배속 검수다.
- 검수 후 방향형 스킬 상세의 '범위 반경' 표기를 '방향 선택 · 사거리 끝까지'로 정정했다. 최종 컴파일 통과. 개발 플레이어 검수와 테스트는 이 설명 문구 정정 전 버전이며 전투 규칙은 동일하다.
- 개발 Windows 빌드20.157초/경고8/오류0, 일반 배포 빌드16.458초/경고6/오류0 성공. unity-geometry-development-build.json, unity-geometry-release-build.json 보관. Builds/Windows/TalesTactics.exe 갱신.

## 2번 완료 — 전체 기술 기하·속성·고저차·초기 밸런스

- 전체 89개 기술(일반68/궁극10/기본11)에 개별 기하·높이·시야·속성을 적용했다. 기존 에셋을 Editor API로 수정했으며 비용·위력·해금 레벨·게이트는 보존했다. 상세 목록은 COMBAT_BALANCE.md.
- 물리 높이 피해는 한 단계당 ±10%, 최대 ±20%. 활/총 물리 기술은 높이 차에 따라 사거리 최대 ±2칸. 마법/회복에는 물리 높이 피해를 적용하지 않는다. 캐릭터별 속성 피해 배율은 미지정/무속성 1, 기본 저항0.75/약점1.25이며 API는 무효0도 지원한다. 회복/상태확률에는 저항을 적용하지 않는다. 미리보기·실제 실행·AI가 같은 계산을 사용한다.
- 실제 Unity EditMode **73/73**, PlayMode **21/21** 통과. unity-editmode-complete-combat.json, unity-playmode-complete-combat.json. 별도 .NET 엔진 독립 검사 **73/73** 통과(실제 Unity 결과와 구분).
- 전체 89개 기술을 해금 레벨과 Lv50에서 총178회 실행했다. 게이트 충족 상태에서 사용 가능/유효 대상/MP 차감/확정 피해 미리보기 일치를 검사했다. combat-skill-matrix.csv. 모든 지형·확률 분포·스킬 조합을 전수 검사한 것은 아니다.
- 두 장 × 세 파티의 기본 공격 자동 전투 결과는 combat-party-matrix.csv. 6인 물리 파티는 1장39턴/5명 생존, 2장32턴/6명 생존; 6인 지원·원거리 파티는 1장51턴/4명, 2장42턴/5명으로 승리했다. 무장비 3인(크레스/민트/파라)은 두 장 모두 패배했다. 최초 테스트의 모든 파티 승리 가정은 이를 발견하고, 소수 인원은 결과 기록·정상 종료 검증으로 수정했다. 6인 파티의 승리 조건은 유지했다. 출전 화면에 캠페인 6인 권장을 표시한다.
- 이는 프로젝트용 초기 밸런스 기준선이다. 원작의 공식 수치나 모든 파티 승리 보장이 아니며, 사람 플레이 기반 난이도·장기 성장·소수 인원 조정은 후속 검수다. 적 AI는 기존 기본 공격 계획기이며 Utility AI 확장은 별도 항목이다.
- Windows 개발 빌드20.353초/오류0/경고8, 일반 배포 빌드15.861초/오류0/경고6 성공. unity-complete-combat-development-build.json, unity-complete-combat-release-build.json. Builds/Windows/TalesTactics.exe 갱신.
- Windows 개발 플레이어를 세 번 실행해 1장27턴/공격16회, 2장27턴/공격17회 승리와 후일담·재실행 저장 유지·정상 패배·중복 보상 방지·사용자 저장/백업 해시 불변을 확인했다. PlayerReviews/d9f22427982b4efcbaf46fa558673c1a-summary.json. 자동 이벤트와 4배속 검수이며 수동 완주를 뜻하지 않는다.
- 실제 Editor GameView 1920×1080에서 피어싱 라인의 형태·높이 제한·시야·높이 사거리·물리 보정·비용 및 목표 선택 버튼을 확인했다(combat-details-editor.png). 임시 캠페인/저장 실패 콜백을 사용하여 사용자 저장을 쓰지 않았다. 모든 스킬 상세와 모든 창 비율의 시각 전수 검수는 수행하지 않았다.

## 3번 완료 — CT·Utility AI·다양한 전투 목표

- CTTurnScheduler, UtilityPlanner, 보스/도착/호위/생존 목표와 출전 전 설정 UI를 구현했다. 기본 SPD/기본 AI 및 캠페인 전멸 목표를 보존하며 새 목표는 훈련에서 선택한다. 정확한 조건과 제한은 TACTICAL_SYSTEMS.md 참조. 기존 에셋·씬과 저장 버전은 변경하지 않았다.
- 실제 Unity EditMode **83/83**, PlayMode **25/25** 통과: unity-editmode-tactical-systems.json, unity-playmode-tactical-systems.json. 별도 .NET 엔진 독립 검사 **83/83** 통과(실제 Unity 실행과 구분).
- CT 미리보기 비변경/실제 순서 일치/속도별 빈도/속도 변경/KO/부활/0속도, AI 복수 대상/회복/부활/점유 복원/비용/게이트/시야/대기 가드, 보스 격파/호위 KO/도착/생존 패배 우선순위와 종료 중복 집계를 검사했다. 실제 씬에서 설정 버튼으로 CT/Utility/보스 훈련을 시작하고 저장 미호출, 정상 이동 코루틴의 도착 승리, 호위 KO 실패, 적 코루틴의 회복 기술/MP 소비를 확인했다.
- tactical-scenarios.csv: CT+Utility, 아군6명 Lv25/적 Lv10으로 강제 피해 없이 전멸4턴/보스4턴/도착16턴/호위20턴/생존12턴에 완료했다. 턴 수는 양 팀 행동 합계이며, 생존 목표 집계는 아군 턴 종료다. 레벨 우위의 기능 검증이며 동레벨/모든 파티 밸런스 증거가 아니다.
- Windows CT+Utility 캠페인: PlayerReviews/0263642c29ca4a6380bef5975ba62406-summary.json. 1장12회 아군 턴·기술7회, 2장9회·기술7회로 정상 승리했다. 세 번의 별도 프로세스로 보상·성장·장비·후일담·재실행 저장 유지·정상 패배와 사용자 저장/백업 불변을 확인했다.
- Windows 기본 SPD/기본 AI 회귀: PlayerReviews/2ac080b012524ebe9444f1c9b6bbccc8-summary.json. 1장27회 아군 턴·공격16회, 2장27회·공격17회 및 별도 재실행 검사 통과. 두 실행 모두 자동 이벤트/4배속이며 물리 마우스 수동 완주를 뜻하지 않는다.
- 개발 Windows 빌드19.290초/오류0/경고8, 일반 배포 빌드15.150초/오류0/경고6 성공. unity-tactical-development-build.json, unity-tactical-release-build.json 보관. Builds/Windows/TalesTactics.exe 갱신.
- 실제 Editor GameView 1920×1080에서 tactical-options-editor.png의 설정·설명·버튼과 tactical-escort-editor.png의 금색 도착 타일·호위 대상 막대·목표/CT/AI 안내를 확인했다. 임시 캠페인과 저장 실패 콜백으로 사용자 저장을 쓰지 않았다. 모든 해상도 및 장시간 성능 검수는 미수행이다.
- Utility는 단일 행동 점수 비교이고 추가 연계/다중 턴 최적화는 하지 않는다. 호위는 첫 출전 캐릭터를 직접 조작하는 방식이다. 특수 목표의 캠페인 장 편성·전용 보스 아트/패턴·행동별 CT 지연은 후속 확장이다.
- 사용자 지속 지침(완료 및 관련 검증 통과 후 별도 확인 없이 커밋/푸시)을 AGENTS.md에 기록했다.

## 4번 완료 — 확률 드롭·경제·성장 기준선

- 기존 확정 장비를 유지하고 승리당 추가 드롭 1회(청동검25%, 장별 방어구15%, 없음60%)를 추가했다. 1장 방어구는 가죽 갑옷, 2장은 강화 갑옷. 출전 화면에 확률·최초/반복 보상, 결과 화면에 추가 장비/미지급 사유를 표시한다.
- 최초 EXP/G는 1장120/120·2장180/180, 반복은60/60·90/90. 두 장 최초 완료 시 Lv3/EXP0. 레벨당 EXP 요구량·상한50·기존 저장은 보존한다. 강화 갑옷 가격280→300(매각150), 다른 네 장비 가격·능력치·해금은 유지했다. 기존280 값만 Editor API로 갱신하며 에셋 재생성은 하지 않았다.
- CampaignReward로 추첨과 최초/반복 수치를 승리 때 고정했다. 저장 실패/예외 후 같은 결과로 재시도하고 재고 순서·객체·수량·성장·골드·플래그를 복원한다. 성공한 보상 객체와 UI는 중복 지급을 차단한다. 장비99개 상한은 해당 장비만 건너뛰고 재추첨하지 않는다. 훈련·패배는 추첨/저장을 하지 않는다.
- 실제 Unity EditMode **88/88**, PlayMode **27/27** 통과(unity-editmode-economy.json, unity-playmode-economy.json). 별도 .NET 엔진 독립 검사 **88/88** 통과. 10,000개 추첨 구간을 각 장에서 전수 검사해25%/15%/60%를 확인했다. 원본 RNG의 통계 품질을 측정한 것은 아니다.
- 실제 씬은 저장 예외→실패→성공 세 번 시도에서 추첨1회 유지, 추가 장비 실제 파일 재로드, 성공 후 미중복, 반복 EXP60/골드60, 훈련·패배 무추첨을 검증했다. 임시 GUID 파일을 사용했다. 전투 종료 분기는 강제 KO이며 정상 완주 근거는 아래 Windows 검사다.
- Windows 기본 모드 PlayerReviews/edc7ea1cb1db403887e842619541f89c-summary.json: 1장27턴/16행동, 2장27턴/17행동. CT·Utility 모드 PlayerReviews/e82ea66e37f24d4ab412e2286a83fdd6-summary.json: 1장12턴/7행동, 2장9턴/7행동. 각각 세 번의 별도 프로세스로 두 장 승리·성장 Lv3/EXP0·450G·가죽 갑옷2개/기존 장착·강화 갑옷1개·확정 장비 유지·후일담·정상 패배 저장 불변을 확인했다. 사용자 저장/백업 해시 동일.
- Windows 검수에는 추첨값2500을 주입해 방어구 드롭을 재현했으며 기본 게임 RNG는 변경하지 않았다. 자동 이벤트·4배속 검사이며 수동 마우스 완주나 장시간 밸런스 검수를 뜻하지 않는다.
- 개발 빌드27.222초/오류0/경고8, 배포 빌드15.279초/오류0/경고6 성공. unity-economy-development-build.json, unity-economy-release-build.json. Builds/Windows/TalesTactics.exe 갱신.
- Editor GameView 1920×1080에서 economy-deployment-editor.png의 보상/확률, economy-reward-editor.png의 확정·추가 장비 안내를 확인했다. 메모리 전용 임시 캠페인과 파일을 쓰지 않는 성공 콜백을 사용했다. 다른 모든 창 크기를 전수 검사하지는 않았다.
- ECONOMY_BALANCE.md에 가격·매각·기대 가치와 조정 이유, economy-progression.csv에20회 성장 곡선을 보관했다. 경제 초기 기준선 완료이며 장기간 사람 플레이 기반 최종 튜닝은 후속이다. 미저장 보상은 출전 복귀/앱 종료 시 포기되고 전투/보상 대기 상태 복구는 제공하지 않는다.
# 2026-09-23 — 5번 아트·연출 중간 구현

5번 전체 완료가 아니다. 구현 범위와 남은 최종 아트/개별 포즈 프레임/전용 VFX/제공 음원은 [ART_PRESENTATION.md](ART_PRESENTATION.md)를 따른다.

- 실제 Unity EditMode 88/88, PlayMode 30/30 통과. 증거: unity-art-editmode-results.json, unity-art-playmode-results.json.
- ManagedChecks 88/88은 별도 엔진 독립 검사다.
- Windows Development 빌드 성공: 오류 0, 경고 8. 일반 빌드 성공: 오류 0, 경고 6. 증거: unity-art-development-build.json, unity-art-windows-build.json. 기존 shader stripping/DEVELOPMENT_BUILD/nullable 관련 경고 포함.
- Windows 실제 플레이어 3회 실행: 1장 27턴/16공격, 2장 27턴/17공격, 재실행·패배/재출전·보상 중복 방지 통과. 기존 사용자 저장/백업 불변. 증거: PlayerReviews/6615117d84a24aeb88ba48e0e01c2188-summary.json.
- Unity Game View에서 6명 스프라이트 표시 확인. HP 바가 얼굴을 가리는 것을 발견해 카메라 기준 머리 위로 수정했다.
- 위 검증은 최종 원작 외형 일치, 정교한 프레임 작화, 기술별 전용 연출, 실제 BGM 재생, 사람의 캠페인 수동 완주 또는 장시간 성능 검수를 증명하지 않는다.

## 5번 후속 — 캐릭터별 VFX 및 대상별 피드백

- 10명에 서로 다른 기본 VFX 유형을 연결했다. CharacterData/SkillData.VisualStyle로 기본값·재정의를 지정하며 전투 규칙/비용/위력/저장 형식은 변경하지 않았다.
- 판정 직전 대상 목록으로 범위 공격의 KO 대상까지 개별 연출한다. HP 비용은 피격과 분리하고, 회복·부활·보조 기술은 다른 도형/색으로 표현한다. 같은 기술 연속 동작 재생과 타이밍 갱신 중 동작 리셋도 수정했다.
- 실제 Unity EditMode 88/88, PlayMode 34/34 통과(unity-vfx-editmode-results.json, unity-vfx-playmode-results.json). 별도 엔진 독립 ManagedChecks 88/88 통과.
- 추가 검사는 10명 유형/도형 생성과 소멸, 4명 동시 KO 후 대상 보존, 회복·부활·HP 비용과 공격 동작 유지, 연속 동작 및 타이밍 갱신을 확인한다.
- Game View 갤러리 검수에서 효과가 흰색으로 나오는 재질 문제를 발견해 Sprite 재질로 수정했다. 수정 후 청색 정점 색·형태를 확인했다(vfx-gallery-preview.png). 임시 검수 화면은 저장하지 않았다.
- 5번 전체는 여전히 진행 중이다. 최종 아트 보정·개별 포즈 프레임·89개 기술별 상세 연출·제공 BGM은 완료되지 않았다.
- 첫 개발 빌드는 Bee의 Unity.Pipeline ExtractUsedFeatures 단계에서 상세 메시지 없이 실패했다(unity-vfx-development-build-initial-failure.json). 소스·설정 변경 없는 재시도가 성공했다. 최종 개발 빌드 6.700초/오류0/경고1, 일반 빌드 16.657초/오류0/경고6(unity-vfx-development-build.json, unity-vfx-windows-build.json). 일회성 실패의 근본 원인을 확정한 것은 아니다.
- 실제 Windows 플레이어 3회 실행: 1장27턴/16공격, 2장27턴/17공격, 재실행·정상 패배·재출전·중복 보상 방지 모두 통과. 사용자 저장/백업 불변. 증거: PlayerReviews/100d320ccf4d488c86833f7892d5a47d-summary.json. 수동 플레이 또는 장시간 성능 검증을 뜻하지 않는다.

## 5번 후속 — 160개 행동 포즈

- 10명 × 앞/뒤/좌/우 × 공격/시전/방어/피격 포즈를 연결했다. 일반 기술은 공격, 궁극기는 시전 포즈를 재사용한다. 공격 준비/복귀, 방어 유지, 피격 후 복귀, 포즈 누락 시 기존 그림 사용을 구현했다. 기존 Animator 우선 동작과 전투 수치를 보존한다.
- 실제 Unity EditMode 88/88 통과(unity-poses-editmode-results.json). 별도 ManagedChecks 88/88은 엔진 독립 검사다.
- 최초 PlayMode 36/36 통과(unity-poses-playmode-initial-results.json). 새 검사는 160개 개별 Sprite/크기, 실제 SpriteRenderer의 카메라 4방향 선택, 공격 복귀·방어 유지·레거시 데이터 대체를 다룬다.
- Game View 1920×1080에서 앞/뒤/좌/우 갤러리 4개로 전체 포즈를 검수했다(poses-*-preview.png). 사각 분할에 이웃 행의 머리/발이 섞이는 문제를 발견해 개별 알파 기반 렌더링 윤곽으로 제외했다. 발 pivot도 보정했다. 원본 PNG 픽셀은 변경하지 않았다.
- 임포트 중 CLI의 main-thread 응답 시간이 초과됐지만 에디터에서 10명 마지막 에셋까지 윤곽/참조 적용을 확인했다. 응답 시간 초과를 전체 적용 실패나 성공으로 단정하지 않는다.
- 수동 Play Mode 검수 후 재검사가 0개를 반환했다(unity-poses-playmode-empty-run.json). 이는 통과 증거가 아니다. 테스트 목록에는 36개가 나타나 스크립트 캐시를 재빌드한 뒤 재검사했다.
- 최종 윤곽 에셋으로 실제 PlayMode 36/36 재통과(unity-poses-playmode-results.json). 캐시 재빌드로 실행이 복구됐으며 0개 보고의 근본 원인을 확정한 것은 아니다.
- 최종 Windows 개발 빌드 5.454초/오류0/경고1, 일반 빌드 22.640초/오류0/경고6 성공(unity-poses-development-build.json, unity-poses-windows-build.json). 첫 개발 빌드는 Succeeded이지만 빌드 중 별도 CLI 상태 조회의 main-thread 시간 초과가 오류1개로 집계돼 초기 보고서를 보관하고 재빌드했다(unity-poses-development-build-initial.json). 최종 빌드 중에는 해당 조회를 실행하지 않았다.
- 실제 Windows 플레이어 3회 실행: 1장27턴/16공격, 2장27턴/17공격, 재실행·정상 패배·재출전·중복 보상 방지 통과. 기존 사용자 저장/백업 불변. 증거: PlayerReviews/ba00f74860894b488a721abb8692741e-summary.json. 자동 실행 검사이며 수동 완주와 장시간 성능 검증은 아니다.
- 이 검수는 최종 원작 외형 일치, 걷기/쓰러짐의 연속 프레임, 기술별 독립 포즈, BGM, 사람의 캠페인 수동 완주를 증명하지 않는다. 5번 전체는 진행 중이다.

## 5번 후속 — 파라·나탈리아 보행/쓰러짐 프레임

- 2명 × 4방향 × 보행4/쓰러짐3 = 56개 Sprite를 연결했다. 보행 8fps 반복, 쓰러짐 6fps 마지막 프레임 유지, 부활 후 기본 자세 복구를 구현했다. 나머지 8명은 기존 Transform 동작을 사용하며 5번 전체 완료가 아니다.
- 실제 Unity EditMode 90/90·PlayMode 38/38 통과(unity-locomotion-editmode-results.json, unity-locomotion-playmode-results.json). 별도 엔진 독립 ManagedChecks 90/90 통과.
- 새 EditMode 검사는 프레임 경계·반복·종료 유지·빈 배열·비정상 시간/FPS를 확인한다. 새 PlayMode 검사는 두 캐릭터의 카메라 4방향에서 모든 보행 프레임 표시, Idle 복귀, KO 3단계 진행, 동일 KO 갱신 시 재시작 방지, 부활 시 스프라이트/크기 복구를 확인한다.
- Game View 1920×1080에서 전체 56개를 검수했다(locomotion-frames-preview.png). 초기 나탈리아 보행 시트의 행간 접촉을 발견해 여백을 넓힌 v2로 교체했다. 분석 도구는 원본 알파를 읽어 사각 경계·발/몸 중심 pivot·렌더링 윤곽 CSV만 작성한다. PNG 픽셀을 코드로 편집하지 않았다.
- 기존 행동 포즈 임포트가 새 Walk/Dead 필드를 보존하도록 수정했다. DemoContent 재생성, 능력치 변경, 원작 음원 다운로드는 하지 않았다.
- 방향별 장비·체형 일관성 및 더 많은 중간 프레임은 최종 아트 보정 대상이다. 임시 갤러리는 Play Mode를 종료해 폐기했으며 장면 에셋에 저장하지 않았다.
- Windows 개발 빌드 21.491초/오류0/경고8, 일반 빌드 15.693초/오류0/경고6 성공(unity-locomotion-development-build.json, unity-locomotion-windows-build.json). 기존 Pipeline/셰이더 관련 경고는 유지된다.
- 실제 Windows 플레이어 3회 실행: 1장27턴/16공격, 2장27턴/17공격, 재실행·정상 패배·재출전·중복 보상 방지 통과. 사용자 저장/백업 불변. 증거: PlayerReviews/8106f621368046a7984dabc4180f5a3a-summary.json. 자동 회귀 검사이며 수동 완주·장시간 성능 검수는 아니다.

## 5번 후속 — 전체 10명 보행/쓰러짐 연결

- 크레스·민트·벨벳·티아·제이드·알펜·시온·키사라의 224개 프레임을 추가했다. 전체 10명의 4방향 보행4/쓰러짐3, 총 280개다. 기존 재생기를 재사용하며 런타임 전투 규칙은 변경하지 않았다.
- 실제 Unity EditMode 90/90·PlayMode 38/38 통과(unity-locomotion-roster-editmode-results.json, unity-locomotion-roster-playmode-results.json). PlayMode 대상은 기존 2명에서 전체 10명으로 확대했다. 보행 4방향 전체 프레임 표시/Idle 복귀와, 5명씩 두 번 출전시킨 KO 3단계/최종 프레임 유지/부활 복구를 확인했다. 별도 ManagedChecks 90/90도 통과했다.
- Game View 1920×1080에서 새 224개 프레임의 잘림·이웃 그림 혼입·투명도를 검수했다. 증거: locomotion-cless-mint-preview.png, locomotion-velvet-tear-preview.png, locomotion-jade-alphen-preview.png, locomotion-shionne-kisara-preview.png. 임시 갤러리는 저장하지 않고 Play Mode 종료로 폐기했다.
- 원본 PNG 픽셀은 편집하지 않았다. 알파 분석은 CSV만 생성하며 Sprite Editor API가 경계/pivot/렌더링 윤곽/안정된 Sprite ID를 적용했다. Git 기준 비교로 새 8명 데이터의 Walk/Dead 외 필드가 모두 보존됐고 파라·나탈리아 및 ProjectSettings 변경이 없음을 확인했다.
- Windows 개발 빌드 21.668초/오류0/경고5, 일반 빌드 14.852초/오류0/경고4 성공(unity-locomotion-roster-development-build.json, unity-locomotion-roster-windows-build.json). Pipeline 런타임 설정 없음/셰이더 관련 경고가 있다. 개발 빌드에는 미컴파일 코드 변경 경고가 추가로 기록됐다. 이 단계에서 런타임·Editor 후처리 코드를 바꾸지 않았으며 이후 recompile_status는 completed/failed=false/compilationFailed=false였다. 일반 빌드에는 해당 경고가 없다.
- 실제 Windows 플레이어 3회 실행: 1장27턴/16공격, 2장27턴/17공격, 재실행·정상 패배·재출전·중복 보상 방지 통과. 기존 사용자 저장/백업 불변. 증거: PlayerReviews/68ca64ec4d8a4864bd1f3c54142dc776-summary.json.
- 이 검증은 최종 원작 외형 일치, 방향별 장비·체형 일관성, 부드러운 중간 작화, 기술/궁극기 전용 연출, 제공 BGM, 수동 캠페인 완주 또는 장시간 성능을 증명하지 않는다. 5번 전체는 진행 중이다.

## 2026-09-27 — 5번 기술·궁극기 전용 프레임과 단계 연출

- 10명×4방향×Skill/Ultimate 준비·발동 160개 Sprite와 89개 연출 프로필을 연결했다. 준비 중 고리/기술명, 실제 판정 시 발동 전환, 10명별 궁극기 도형, 음악 테마 복귀를 추가했다. 기존 비용·위력·해금·기하·보유 포즈는 보존했다.
- 실제 Unity EditMode **90/90** 통과(0.69초, unity-special-editmode-results.json). 실제 PlayMode **42/42** 통과(78.0초, unity-special-playmode-results.json). 기존 38개 회귀에 4개 검사를 추가했다. 별도 ManagedChecks 90/90은 Unity 실행 결과가 아니다. 실제 Unity API DLL 참조 Runtime/Editor/EditMode/PlayMode 컴파일도 통과했다.
- 새 PlayMode 검사는 전체 10명×4카메라 방향에서 기술/궁극기 준비·발동·Idle 복귀, 아군 88개 프로필과 궁극기 10개 기하 구분, 준비 중 HP/MP 불변·피해/비용 1회·시각 반복의 무해성, Restart 고리/이름 정리, 테마 누락/재생 위치 복귀/보스 선택/정지 후 재개 방지를 확인했다. 음악 검사는 임시 무음 AudioClip을 사용했으며 실제 BGM 청음 검증이 아니다.
- Game View 1920×1080 갤러리 검수: special-front-preview.png, special-back-preview.png, special-right-preview.png, special-left-preview.png. 총 160개 포즈의 경계·투명도·이웃 그림 혼입을 확인했다. special-finales-preview.png는 동일 색상의 궁극기 기하 비교다. 갤러리는 Play Mode 종료로 폐기했고 시간 배율은 1로 복구했다.
- Windows 개발 빌드 **33.133초/오류0/경고8**, 일반 빌드 **20.360초/오류0/경고6** 성공(unity-special-development-build.json, unity-special-windows-build.json). 경고는 기존 Pipeline 런타임 설정 없음, 개발 검수 코드의 deprecated API/DEVELOPMENT_BUILD, nullable Vector2Int 직렬화, URP 디버그 셰이더 stripping, TMP pragma다. 이번 빌드에 미컴파일 변경 경고는 없다.
- 실제 Windows 플레이어 3회 실행: 1장 27턴/16공격, 2장 27턴/17공격 승리; 재실행 5턴/0공격으로 정상 적 공격에 의한 패배·재출전·중복 보상 방지 통과. 기존 사용자 저장/백업 SHA256 불변. 증거: PlayerReviews/8fde9a7b98ae496c8cd266ad21b52ff6-summary.json. 자동 캠페인 회귀이며 수동 마우스 완주 또는 모든 기술의 시각 검수는 아니다.
- 99개 캐릭터/기술 에셋의 기존 필드가 보존된 것을 Git HEAD와 읽기 비교했다. Sprite 편집은 Editor API로만 수행했고 원본 PNG를 코드로 수정하지 않았다. ProjectSettings/Scene 변경이 없다.
- 현재 한계: 일반 기술은 캐릭터별 2개 단계 포즈와 공통 VFX 패턴을 공유한다. 고밀도 애니메이션, 원작 외형의 최종 정확성·장비/체형 일관성, 제공 BGM 연결/청음은 미완료다. 5번 전체 완료나 최종 아트 승인으로 해석하지 않는다.

## 2026-09-27 — 5번 프로토타입 통합 마감

- 10명 기본 그림 40개의 경계·발 pivot·렌더링 윤곽을 Sprite Editor API로 보정했다. 10개 PNG와 Sprite ID 및 CharacterData 참조는 보존했다. 파라/나탈리아 PNG 재작화 시도는 확실한 품질 개선이 없어 채택하지 않았다. 강한 적색 주변 점을 읽기 분석한 결과 대부분 alpha 1–3/255였으며 렌더링 윤곽은 alpha>32의 본체를 기준으로 했다.
- 카메라 pitch 때문에 45도 isometric 시점에서 회전 4방향이 2방향으로 합쳐지는 문제를 새 검사에서 발견했다. 최초 43개 중 42개 통과/1개 실패(unity-polish-playmode-initial.json). yaw만 사용한 4분면 선택으로 수정했다. 파라 타이밍 연출은 실제 그림 방향을 두 바퀴 회전시키고 성공 구간을 별도 고리로 표시하며 전투 Facing은 유지한다.
- HP 숫자에 공통 한국어 폰트를 지정했다. 기본 공격 11개의 공통 Slash/Thrust 재정의를 CharacterStyle로 바꿔 주먹·클로·노래·창·활·화염검·총·방패 등 기존 캐릭터 표현을 복구했다. 변경은 연출 패턴/반복 필드에 한정하며 전투 수치는 유지했다.
- 오리지널 BGM/캐릭터 테마 14곡을 신규 제작·연결했다. 원작 음원/외부 샘플 다운로드가 없고 제작 음표/합성 코드는 Tools/ComposeOriginalMusic.py에 보존한다. 44.1kHz 스테레오 WAV 총 약 28.8MB. 각 파일의 비무음·피크<0.9·RMS 범위와 SHA256은 original-music-manifest.json에 기록했다.
- Unity import 재조회(unity-original-music-import.json): 14개 모두 stereo/44.1kHz/Vorbis quality0.85, 3개 긴 BGM Streaming·11개 테마/승리 CompressedInMemory. 런타임 Windows 출력은 48kHz, listener1·믹서0, 음악 음량0.28/효과음0.45. 다른 플랫폼의 메모리/CPU 최적화 수치는 측정하지 않았다.
- Game View 1920×1080에서 10명×9상태×4방향 360칸 비교(polish-front/back/right/left-preview.png). 전체 89개 VFX 프로필을 4페이지로 표시해 검수(polish-skills-0/1/2/3-preview.png). 고밀도 중간 프레임이나 원작 작화의 완전한 일치를 증명하는 검사가 아니다. 임시 갤러리/시간 배율은 복구했고 Scene은 저장하지 않았다.
- 첫 Windows 오디오 검수는 최초 GetOutputData 직후 빈 버퍼를 읽어 실패했다(PlayerReviews/072a90d3ad6c4b069ff7827bc07c7c4d-summary.json). 검수 도구를 선행 버퍼 조회 후 최대2초 신호 대기로 수정했으며 통과 기준(재생 중·샘플 진행·RMS>0.00001)은 유지했다. 재실행 d07e7a20da694b28b8d269b32bb29cf4에서 14곡 신호와 캠페인 회귀를 확인했다. 이는 사람의 청음 평가가 아니다.
- 5번의 완료 상태는 ART_ACCEPTANCE.md의 프로토타입 구현/통합 검수 기준이다. 원작 세부 외형의 전문가 감수, 고밀도 추가 작화, 제공 원작 음원, 주관적인 음악 평가, 수동 캠페인 완주·장시간 FPS/메모리 검수는 이 증거에 포함하지 않는다.
- 실제 Unity EditMode **90/90** 통과(0.45초, unity-polish-editmode-results.json), 최종 기본 공격 표현 수정 후 PlayMode **44/44** 통과(79.85초, unity-polish-playmode-results.json). 14곡 재생과 회전 방향/전투 Facing 보존 검사를 추가했다. 별도 ManagedChecks를 이번 단계에서 재실행하지 않았으며 이전 90/90 결과는 엔진 독립 검사다.
- 최종 Windows 개발 빌드 **16.402초/오류0/경고8**, 일반 빌드 **20.329초/오류0/경고6** 성공(unity-polish-development-build.json, unity-polish-windows-build.json). 기존 Pipeline/개발 검수 API/직렬화/셰이더 관련 경고가 남아 있다.
- 최종 실행 파일로 Windows 플레이어 3회 실행 통과: 1장27턴/16공격, 2장27턴/17공격, 재실행5턴/0공격으로 정상 패배·재출전·중복 보상 방지 확인. 각 실행에서 14곡의 재생/샘플 진행/출력 RMS 기준을 통과했고 사용자 저장/백업 SHA256은 불변이다. 증거: PlayerReviews/74c693e4c79145d59022b0f1749a85c3-summary.json. 숨겨진 플레이어의 검은 캡처는 시각 검수 증거로 사용하지 않으며 위 Editor 갤러리와 구분한다.

## 2026-09-27 — 7번 추가 검수

- 실제 Unity PlayMode **48/48** 통과(66.10초). 전체89개 기술 상세의 글자 누락0, 영역 넘침0, 열람 중 HP/MP 불변을 추가 검증했다. `unity-final-review-playmode-results.json`, `skill-panel-review.csv`.
- Windows 개발 플레이어를 화면 기반으로 조작해 6인 캠페인1·2장 전원 생존 승리, 전후 대사·상점 구매/매각·장비 적용·성장 조건·보상/해금을 확인했다. 나탈리아1인 정상 적 공격 패배, Restart 재출전 회복, 프로세스 재실행의400G/Lv3/장비/두 장 완료 복원도 확인했다. 저장 데이터나 HP를 주입하지 않았다.
- 첫 세션4038.73초에서 초기 대기 등을 제외한 약53분의 분당 FPS992.30–1203.63, p95 1.528–1.951ms, Unity 할당190.26–191.75MiB, 예약544.375MiB 고정을 관찰했다. 별도 Windows 측정120개/약30분의 작업 집합290.48–371.95MiB, 전용 메모리924.22–962.64MiB. 혼합 전투/메뉴이며 다른 기기 성능이나 누수 부재를 보장하지 않는다.
- Mono WorkingSet64=0은 유효한 측정값으로 쓰지 않고 외부 측정으로 대체했다. 조회 불가를 빈칸으로 기록하도록 수정하고 재실행 CSV에서 검증했다. 수집기에 런타임 Error/Exception/Assert가 없었다.
- 최종 Windows 개발 빌드 **8.267초/오류0/경고1**, 일반 빌드 **3.495초/오류0/경고1**. 경고는 Pipeline RuntimePipelineConfig 부재(플레이어에서 Pipeline 비활성)다. 개발 최초 시도는 Burst 컴파일러 실행 실패, 일반 최초 시도는 내용 없는 빌드 오류1개로 실패했으며 설정/코드 변경 없는 각각의 재시도가 성공했다. 근본 원인 해결로 주장하지 않는다. `unity-final-review-*-initial-failure.json`과 최종 `*-build.json`에 보존했다.
- 일반 사용자 저장/백업 SHA256 불변, 검수 저장도 패배 전/재실행 후 동일하다. `ManualReview/save-integrity.json` 참조. 폰트 동적 캐시 등 검수 부산물은 복원했다.
- 실제 Gamepad 장치는0개였으므로 물리 기기별 검수는 미완료다. 전체89개 수동 발동이나 사람의 난이도·장기 성장 평가, 전문가의 원작 외형/청음 승인도 별도다. 7번 완료 범위는 `FINAL_REVIEW.md`의 전수 자동 검사와 대표 기술 화면 검수다.


## 2026-09-28 — 장편 첫 확장 / 3장 협곡의 봉화

- 13×10 협곡 맵, 두 돌다리·물길·동쪽 2단 능선, 전후 대사 각5개, chapter2 완료 시 순차 해금을 추가했다. 적 Lv4, 최초 EXP300/240G, 반복 EXP150/120G, 확정 강화 갑옷1개. 기존 장 보상·성장 곡선·승급 조건·V2 저장 스키마는 유지한다.
- 실제 Unity EditMode **91/91** 통과(0.64초): unity-chapter3-editmode-results.json. 전 장 스폰/지형 연결성·추첨1만 구간 및 기존2장 저장 로드→3장 보상 실패 복원→재시도→중복 방지→반복 보상→재로드를 검사했다.
- 실제 Unity PlayMode **49/49** 통과(61.48초): unity-chapter3-playmode-results.json. 신규3장 잠금·버튼 선택·대사·적 레벨·130타일 카메라 포함·보상·후일담 다시 읽기를 검사했다. 해당 UI 흐름 테스트의 승리는 강제 피해를 사용하며 정상 전투 검증은 아래 Windows 실행 결과로 구분한다.
- combat-party-matrix.csv: 3장 Lv3 기본 공격 AI 시뮬레이션에서 6인 두 편성 모두 승리(42/43턴, 각각5명 생존), 기본3인은47턴/1명 HP8로 승리. 모든 편성의 승률이나 사람이 느끼는 난이도 보장은 아니다.
- 엔진 독립 ManagedChecks **91/91** 통과(chapter3-managed-results.txt). Unity 실행 결과가 아니다. Runtime/Editor/EditMode/PlayMode 실제 Unity API DLL 참조 컴파일도 통과했다.
- Windows 개발 빌드 **33.152초/오류0/경고9**, 일반 빌드 **18.192초/오류0/경고7** 성공. unity-chapter3-development-build.json, unity-chapter3-windows-build.json. 기존 Pipeline 설정 미지정·deprecated API/매크로·직렬화 분석기·디버그 셰이더/TMP pragma 경고를 보존했다.
- 개발 플레이어 **4회 독립 실행 통과**: 1장27턴/16공격, 2장27턴/17공격, 3장26턴/16공격, 재실행 패배5턴. 정상 이동/일반 공격으로 각 장 승리, Lv4/EXP0·690G·강화 갑옷3개(확정1+2/3장 추첨각1) 재로드, 중복 지급 방지, 전후 이야기, 패배 후 저장 불변 및 재출전 회복을 확인했다. PlayerReviews/a3a3b174685e4ae085c0d842ece71390-summary.json 참조. 사용자 원본 저장/백업 SHA256 불변.
- 최초 직접 검수는 Windows 잠금 화면 때문에 중단했다. 후속 세션에서 잠금 해제 상태를 확인하고 Computer Use로 검수를 마쳤다. 일반 배포 빌드1280×800의 새 캠페인에서 3장 잠금 표시와 메뉴 배치를 확인했다. 사용자 저장을 유지하기 위해 개발 빌드의 격리 슬롯에 위 자동 정상 완주 저장을 복사하여 3장 완료 버튼 선택, 반복 EXP150/120G·강화 갑옷 문구, 전후 대사 첫 화면·건너뛰기, 전장 진입 및 우회전을 직접 확인했다. 메뉴·대사 글자 잘림과 전장 UI 가림은 발견하지 못했다. Chapter3Review/*.png 참조.
- 사용자 원본/백업 및 격리 저장 SHA256 불변, 검수 런타임 오류0: Chapter3Review/save-integrity.json. 이번 직접 검수는 3장 수동 전투 완주나 대사10개 전체 시각 검수가 아니다. 정상 승리는 위 자동 플레이어 결과이며, 일반 배포 빌드에서 직접3장 전투 완주를 했다고 주장하지 않는다. 물리 패드·장기 사람 플레이 검수는 기존 후속 범위다.


## 2026-09-28 — 4~6장 일괄 확장

- 사용자 확정 범위는6장까지다. 관측소(12×12)·수문(14×10)·중계핵(13×13)의 맵/스폰/전후 대사30개를 추가하고 첫 여정의 결말을 연결했다. 장 선택은3개씩 페이지로 탐색하며, 목록만 넘기면 실제 선택 장을 변경하지 않는다.
- 최초 EXP600/900/1500, 골드320/420/560; 반복은 각각 절반. 기존1~3장 수치와 V2 저장 형식을 유지했다. 동일 출전자가1~6장 초회 진행 시 Lv9/EXP0이다. 새 메달(HP50/MDF8,480G)과 갑옷(HP70/DEF10,650G)을 Editor API로 생성하고 카탈로그에 추가했다. 기존 Content는 재생성하지 않았다.
- 실제 Unity EditMode **92/92**(0.70초), PlayMode **51/51**(62.57초) 통과. unity-expansion-editmode-results.json, unity-expansion-playmode-results.json. 모든6장 지형/스폰 연결성, 정상 편성 자동 전투, 장 선택/잠금·카메라·새 대사30개 글자/넘침, 상점 구매·장비 능력치·파일 재로드 및 보상 실패 복원/중복 방지를 검사했다. UI 흐름 검사에서 승리는 강제 피해이며 실제 정상 전투는 별도 시뮬레이션/아래 플레이어 검사로 구분한다.
- 초기 PlayMode **50/51**(65.50초)은 대사 패널 뒤에 가려진 기존 출전 제목까지 넘침 검사에 포함해 실패했다. 실제 대사 패널의 자식 레이블만 검사하도록 범위를 수정하고 전체51개를 다시 통과했다. 초기 증거 unity-expansion-playmode-initial.json을 보존했다. 테스트 기준은 새 대사/버튼의 누락 글자0·영역 넘침0이다.
- 엔진 독립 ManagedChecks **92/92** 통과(expansion-managed-results.txt), 실제 Unity API DLL 참조4개 어셈블리 컴파일 통과(expansion-api-compile-results.txt). ManagedChecks는 Unity 실행 결과가 아니다.
- combat-party-matrix.csv: 두 가지6인 편성이4~6장 모두 승리. 4장37/43턴·각5명 생존,5장55/54턴·5/4명 생존,6장30/38턴·6/5명 생존. 기본3인 편성은 해당 세 장 모두 패배했다. 기본 공격 AI의 초기 기준선이며 모든 조합/사람 체감 난이도 보장이 아니다.
- Windows 개발 빌드 **29.105초/오류0/경고9**, 일반 빌드 **21.380초/오류0/경고7** 성공(unity-expansion-development-build.json, unity-expansion-windows-build.json). 기존 Pipeline runtime 설정·deprecated API/매크로·직렬화·디버그 셰이더/TMP 경고가 남는다.
- 기본 SPD/AI Windows 플레이어7회 독립 실행 통과:1~6장 정상 전투 승리(플레이어 턴27/27/26/25/33/22), 마지막 재실행에서6장 완료·Lv9/EXP0·1990G·수호의 메달2개/단련 갑옷1개·강화 갑옷6개 확인. 1인 수동 대기 패배 후 저장 불변·재출전 회복·중복 보상 방지도 통과했다. PlayerReviews/42cf78ed00a247658e63ac865633fdec-summary.json. 각 프로세스의14곡 신호·가상 패드 상점 진입도 통과했으며 물리 기기나 청음 평가가 아니다.
- CT+Utility Windows 플레이어도7회 독립 실행 통과:1~6장 플레이어 턴12/9/15/10/19/10, 마지막 정상 패배1턴. 같은 성장/재고/완료 플래그·저장 불변 검사를 통과했다. PlayerReviews/904b60d2f9c94d6ca566823382e3af7e-summary.json. 두 실행 모두 원본 저장/백업 SHA256 불변.
- Computer Use로1280×800 격리 개발 플레이어에서 장 목록2페이지,4~6장 선택·반복 보상·도입 대사·각 전장·Restart,6장 후일담5개 전부와 종료 복귀, 수호의 메달 가격/능력치/보유 수량을 직접 확인했다. 사용한 저장은 기본AI 정상6장 완주 결과를 새 ManualReviews 슬롯으로 복사한 것이다. 캡처는 ExpansionReview 폴더, 저장 불변/런타임 오류0은 save-integrity.json. 새 세 장의 수동 전투 완주라고 주장하지 않는다.
- 직접 검수에서 발견한 상점의1·2장 한정 보상 안내를 장별 출전 화면 안내로 수정했다. 이후 변경은 이 문자열1개뿐이며 전투/저장/보상 로직은 동일하다. 전체 테스트 및 두 플레이어 완주 보고서는 문자열 수정 전 빌드 기준이다. 최종 빌드/상점 화면 재확인은 아래 기록을 따른다.
- 상점 안내 수정 후 최종 개발 빌드28.007초/오류0/경고9 성공. 일반 빌드 첫 시도3.542초/오류2/경고4는 빈 오류와 스크립트 컴파일 오류 메시지로 실패했다. 에디터 scriptCompilationFailed=false/isCompiling=false를 확인한 뒤 코드 변경 없이 재시도하여19.663초/오류0/경고4로 성공했다. 근본 원인 해결로 주장하지 않는다. unity-expansion-final-windows-initial-failure.json 및 final-*-build.json에 기록했다.
- 최종 일반 배포 플레이어에서 수정한 상점 안내와 신규2종 가격/잠금 표시를 직접 재확인했다(ExpansionReview/release-final-shop.png). 최종 빌드 해시는final-build-hashes.json에 보관했다. 검수 후 원본/백업 저장 해시도 동일하다. 6장까지 구현·자동 검증·대표 화면 검수가 완료되었으며 물리 게임패드/사람 체감 밸런스/전문가 아트·청음 승인 및7장 이후는 이번 확장 완료 범위에 포함하지 않는다.

## 2026-09-29 — 6장 픽셀 아트·적 편성 개편

- 기존 영웅10명 전원을 특징을 유지한3등신 고밀도 픽셀 아트로 교체했다. 각 기본/동작/보충48개 그림, 시온 오른쪽4개 보정, 일반 적9종×20개·다오스32개, 재질16종·장식16종·6장별 원경을 제작/연결했다. 채택 PNG45개 약79.9MB, 프롬프트/원본 이력은 Tools/pixel-final-generation.json. 개별 일반 기술은 시전/공격 그림과 기존VFX를 공유한다. 자세한 범위는 PixelCampaign/README.md를 따른다.
- 신규 적 편성과 창작 전후 대사를6장까지 연결했다. 다오스는 기본AI에서도 레이저와 범위 블래스트를 선택한다. 기존 영웅10명 및 레거시 파수병의 Animator 이후 능력치/기술/성장 필드는 기준커밋2999c64와 일치한다(파수병의 기본값VisualStyle=0 직렬화 추가 제외). 지형/스폰/장별 보상/V2저장은 유지했고 DemoContent.Create를 실행하지 않았다.
- 실제 Unity EditMode **93/93**,0.96초(unity-pixel-editmode-results.json). 실제 Unity PlayMode **52/52**,65.29초(unity-pixel-playmode-results.json). 20개 캐릭터의 아트 참조/크기/import,6장별 적ID·장식·전6장4회전의 모든 보행 타일 선택, 기존 기술89개·보행/궁극기·UI/저장 검사를 통과했다.
- 초기 PlayMode49/52,34.63초(unity-pixel-playmode-initial.json)는 파라 피격 행 잘림, 보행의서로다른그림 부족, 일반기술/궁극기 그림 공유로 실패했다. 행 경계 탐색범위를 넓히고,10명 보충160개 그림을 제작해4개 보행/궁극기 전용2개를 연결한 뒤 기존 기준을 바꾸지 않고52개를 모두 통과했다.
- 엔진 독립 ManagedChecks **93/93**(pixel-managed-results.txt)은 Unity 실행 결과가 아니다. 실제 Unity DLL 참조 Runtime/Editor/EditMode/PlayMode API 컴파일4개도 통과(pixel-api-compile-results.txt). 마지막 테스트 코드 변경은 지면 probe 설명 주석1줄뿐이다.
- 최종 GameView1920×1080에서6개 전장(PixelCampaign/chapter1-final.png~chapter6-final.png)과10명×12그림×4방향480칸(heroes-front/back/right/left-final.png)을 확인했다. 검수용 메모리 캠페인에 해금 플래그를 주고 PersistCampaign을 비활성화했으므로 정상 완주 증거가 아니다. 임시갤러리/캡처용Assets/Docs는 제거하고 Scene은 저장하지 않았다. Native Computer Use의창활성화가 실패하여 Unity의capture_game_view screen 경로를 사용했다. 이번 새아트 수동Windows마우스 검수나 원작 세부 외형 전문가 승인을 주장하지 않는다.
- Windows 개발 빌드 **46.578초/오류0/경고9**, 일반 빌드 **31.688초/오류0/경고7** 성공(unity-pixel-development-build.json, unity-pixel-windows-build.json). 기존 Pipeline Runtime 설정 부재·deprecated API/전처리기·직렬화 분석기·URP 디버그셰이더/TMP pragma 경고가 남는다.
- 기본 SPD/AI 개발 플레이어7회 독립 실행 통과:1~6장 정상 전투 승리27/20/34/22/33/22플레이어턴, 재실행의 정상 패배2턴·재출전·중복 보상 방지 통과. 각실행의14곡출력신호/가상패드상점검사도 통과. PlayerReviews/72b5027d48df4bc498b12c2204857869-summary.json. 숨긴 플레이어의 검은 캡처는 시각 증거로 쓰지 않는다.
- combat-party-matrix.csv의 두6인 편성이 전6장 승리(6장37/41턴·각6명생존), 기본3인 도전은2/4/6장 패배다. 초기AI기준선으로 사람의 체감난이도나 모든조합승률을 보장하지 않는다.
- 3D 회전 카메라와 비정수 확대에서 완전한 pixel-perfect를 보장하지 않는다. 각 일반 기술의 고유 전신 작화, 실물 게임패드·사람 장기 난이도·전문가 원작외형/음악청음 승인은 별도다. 기존2026-09-27 시각검수 수치/이미지는 이전아트 이력으로 보존한다.
- CT+Utility 개발 플레이어도7회 독립 실행 통과:1~6장15/12/17/14/17/9플레이어턴, 재실행 정상 패배3턴·재출전·저장 복원/중복 보상 방지 통과. PlayerReviews/0c0fa920a15c45e09355f16b3876531b-summary.json. 두 검수 모두 원본/백업 저장 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변. 최종 빌드/저장 해시는 PixelCampaign/final-hashes.json에 보관했다.

## 2026-09-29 — 픽셀 전장 조작 가독성·재출전 검증

- 6장 중계핵의 이동 범위가 어두운 바닥 무늬에 묻히는 것을 GameView에서 확인했다(PixelInteraction/move-before.png). 이동/공격 범위에 밝은 청색/적색 선, 선택 효과 범위에 굵은 금색 선을 추가했다. 경로 선에도 URP Unlit 색상을 MaterialPropertyBlock으로 지정했다. 범위 판정·HP·이동·저장 로직은 바꾸지 않았다.
- 타일별 선은 같은 전장에서 재사용하며 취소 시 비활성화, Restart 시 전장과 함께 제거한다. 새 collider나 개별 material 복제를 만들지 않는다. 수정 후 실제 화면은 PixelInteraction/move-after.png, attack-after.png, area-after.png다. 메모리 캠페인/저장 비활성 상태로 촬영했고 효과 범위는 표시 함수를 직접 호출했다.
- 실제 Unity PlayMode **53/53**,65.50초 통과(unity-pixel-interaction-playmode-results.json). 신규 검사에서6장×2회 전장 생성/재출전의 UI Submit 이동·취소·공격·4회전·확대/초기화, 실제 합법 범위와 선 표시 일치, 선 재사용·충돌체 없음·지면 위 높이·밝은 소재 색상, 위치/HP 불변, 참조된 장식 소재/선 삭제를 확인했다. 장시간 FPS/메모리 프로파일링이나 모든 자원 누수 부재 증명은 아니다.
- Runtime/Editor/EditMode/PlayMode 실제 API 컴파일4개 통과(pixel-interaction-api-results.txt). 전투 규칙 변경이 없어 EditMode/ManagedChecks는 다시 실행하지 않았고 이전93/93 결과를 유지한다.
- Windows Computer Use로 배포 플레이어를 실행했지만 화면이 검게 캡처됐고, 대상 창 재조회 후에도 `failed to activate captured window`로 활성화가 실패했다. 수동 Windows 마우스 검수는 보류다. 화면 검토는 Editor GameView, 입력 검증은 실제 PlayMode UI Submit과 구분한다. 검수용으로 시작한 플레이어 두 개는 종료했다.
- 사용자 campaign.json 및 .bak SHA256은 모두324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1로 이전과 동일하다. 임시 Assets/Docs는 Editor API로 삭제했으며 Scene을 저장하지 않았다.
- Windows 일반 빌드14.224초/오류0/경고4, 개발 빌드26.048초/오류0/경고9 성공(unity-pixel-interaction-windows-build.json, unity-pixel-interaction-development-build.json). 기존 Pipeline/사용 중단 API·전처리기/직렬화·셰이더 관련 경고는 유지된다.
- 수정 후 Windows 개발 플레이어7회 독립 실행 통과:1~6장 정상 승리27/20/34/22/33/22플레이어턴, 재실행 정상 패배2턴 및 재출전·중복 보상 방지·저장 복원 통과. 사용자 저장/백업 불변. PlayerReviews/3739dcc79fc54d92b0b9f8feca3c8587-summary.json. CT 모드 Windows 완주를 이번 표시 변경 후 별도로 반복하지 않았으며 이전 개편 결과와 구분한다. 빌드/저장 해시는 PixelInteraction 폴더에 기록했다.

## 2026-09-30 — 새 아트 Windows 화면·마우스 검수 완료

- 이전에 보류했던 Windows 창 제어가 이번 세션에서 정상 작동했다. Computer Use의 node_repl + @oai/sky로 실제1280×800 게임 창을 관찰하고 마우스로 조작했다. 도구/게임 코드 변경 없이 재시도한 결과이며 이전 활성화 오류의 근본 원인 해결로 주장하지 않는다.
- 일반 배포 실행본에서 출전 준비·1장 도입 건너뛰기·전장 진입, 청색 이동 범위·타일 클릭 이동/보행·Undo 원위치 복원·카메라 우회전을 확인했다. 격리 개발 실행본에서는2~6장 선택·도입 첫 대사/건너뛰기·전장·Restart와 장 목록2페이지를 확인했다. 새 배경/장식/영웅/적 그림 누락, 메뉴 글자 잘림, 기본 카메라에서 HUD가 전장을 가리는 문제는 발견하지 못했다.
- 6장에서 파라를 다오스 인접 타일로 이동하고 Attack의 적색 사거리와 다오스 클릭의 금색 선택 범위를 확인했다. 피해43/100%/무속성×1 미리보기와 실행 버튼 활성화, 확대·우회전·초기화 중 대상 유지, 취소 시 테두리 해제, Restart 복귀를 확인했다. 표시 함수를 직접 호출하지 않았다. 공격 실행/승리까지 진행한 검사는 아니다.
- 격리 슬롯7d58cb747397401587b342fd7d2e02b8에는 기존 정상6장 자동 완주3739dcc79fc54d92b0b9f8feca3c8587의 저장을 복사했다. 별도 값/HP/해금 편집 없이1990G/Lv9 상태를 사용했다. 사용자 원본·백업 SHA256은324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1, 격리 저장은82B5F74995EEEF4E79F3BE6AD934A1B5F715E01D843B2B355743CA2405B82181로 검수 전후 동일하다.
- 개발 수집기 errors.txt 없음, player.log의 Error/Exception/Assert 검색0건, 플레이어 종료 확인. 두 exe/두 Runtime DLL 해시가 기존 PixelInteraction/build-hashes.json과 일치한다. PixelWindowsReview/save-integrity.json 및 원본 창 캡처21장, events.txt/hardware.txt/performance.csv에 기록했다. 약442.6초 혼합 구간의 성능 수집은 장시간 안정성/벤치마크가 아니다.
- 이번 변경은 문서·증거뿐이며 코드/에셋/빌드 및 Scene 변경이 없다. Unity EditMode93/93·PlayMode53/53과 빌드/자동 완주 결과는 이전 기록을 유지하며 이번에 재실행했다고 표현하지 않는다. 새 아트 Windows 대표 화면·마우스 검수 보류 항목은 완료했으며 전6장 수동 완주·전체 기술 수동 발동·사람 난이도/전문가 아트·실물 패드 검수는 별도다. 상세 확인 흐름: PixelWindowsReview/README.md.

## 2026-10-01 — 새 아트 대표 전투 연출 후속 검수

- 기존 Windows 개발 빌드1280×800을 Computer Use로 조작했다. 격리 슬롯4974b814b4554128a3d15cf451038bba에는 이전 정상6장 자동 완주3739dcc79fc54d92b0b9f8feca3c8587 저장을 복사했다. 기본3인 편성은 크레스·파라Lv9, 민트Lv1이다. 이전 PixelWindowsReview의 기본3인 전원Lv9 표기를 동일 원본 저장과 실제 민트 턴 화면을 근거로 정정했다. 원본 완료 저장에 민트 성장 항목이 없어 기본Lv1로 시작한다.
- 파라의 합법 이동→일반 공격과 다오스 피격, 크레스의 마신검 상세→직선 목표→시전/복귀를 확인했다. 피해 미리보기43+58=101과 다음 다오스 턴 HP315→214가 일치했다. 크레스MP109→103, 공격자 게이지0→20, 행동 사용 뒤 Undo 비활성화를 확인했다.
- 민트 퍼스트 에이드의 기술명/시전과MP120→114, 재사용 시 녹색 회복 효과/+60 표시·MP114→108을 확인했다. 미리보기 회복77은 상한 적용 전 값이다. 첫 회복의 직전 대상 HP 숫자는 확보하지 않아 첫 실행의 실제 회복량은 주장하지 않는다.
- 다오스의 정상 AI 블래스트에서 기술명/시전 자세, 두 아군 각각30 피해 숫자/피격 표시, 다음 적 턴과 결과 문구를 기록했다. 단일 방향의 짧은 캡처이며 모든 프레임이나 레이저 시각 검수는 아니다. 파라가 정상 적 공격으로 HP246→141→42 이후 쓰러지고 턴 목록에서 빠지는 것을 관찰했다. HP0 숫자 캡처 자체는 없다. Restart 후 파라 생존·HP246/246·MP109/109·게이지0·위치/명령 복원을 확인했다.
- 사용자 원본/백업 SHA256은324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1, 격리 저장은82B5F74995EEEF4E79F3BE6AD934A1B5F715E01D843B2B355743CA2405B82181로 보존됐다. 수집기errors.txt 없음, player.log Error/Exception/Assert 검색0건, 플레이어 종료 확인. 두exe/두Runtime DLL도 기존 PixelInteraction 빌드 해시와 일치한다.
- PixelCombatReview에 캡처22장·events/hardware/performance 및 save-integrity.json을 보존했다. 약570초 혼합 구간은 장시간 성능 시험이 아니다. 코드/에셋 변경이나 Unity 테스트/빌드 재실행은 없고 기존93/53 통과 기록을 유지한다. 대표 연출 검수 완료이며 전6장 수동 완주·보스 격파·전체 기술/방향 검수·사람 난이도/실물 패드 평가를 뜻하지 않는다.

## 2026-10-04 — 전체20종 중간 동작/외형 보강(1번)

- 영웅10명·일반 적9종·다오스에 20개 보충 시트/320개 그림을 추가하고 공격4·일반 기술5·궁극기4프레임을 연결했다. 기존 핵심 타격/궁극기 그림과 준비/복귀 공유를 포함한 수치다. ReleaseFrame으로 준비/발동 구간을 나누고 기존 ReleaseSkill 판정 시점과 회복 시간에 맞춰 재생한다. 전투 규칙/능력치/비용/피해/저장 스키마는 변경하지 않았다.
- 실제 Unity EditMode **95/95**(0.69초), PlayMode **54/54**(102.77초) 통과. AnimationPolish/editmode-results.json, playmode-results.json. 새 테스트는 단계 범위/레거시 클립과20종의 실제 준비 대기·발동·후속·복귀·쓰러짐/대기 중단을 확인한다. 엔진 독립 ManagedChecks는 이번에 재실행하지 않았다.
- Sprite Editor API로 경계/윤곽/발 피벗/PPU를 연결했다. 채택320칸은 alpha 분석에서 빈 칸·불투명 배경·경계 잘림 검사를 통과했다. 실제 Unity 렌더링 영웅/적 ×4방향8장으로 체형·장비 식별·크기·잘림을 비교했다. 시온2개 행의 좌우 참조를 보정했다. 임시 Play Mode 갤러리와 Assets 아래 임시 캡처는 제거했고 Scene/ProjectSettings 변경은 없다. Application.runInBackground도 검수 전 false로 복원했다. 상세 화면/한계: AnimationPolish/README.md.
- Windows 일반 빌드 **32.812초/오류0/경고7**, 개발 빌드 **27.000초/오류0/경고9** 성공. 기존 Pipeline 비활성 안내·개발 검수의 사용 중단 API/전처리기·직렬화·셰이더 경고가 남아 있다. AnimationPolish/windows-build.json, development-build.json, build-hashes.json에 결과와 해시를 보관했다.
- 새 개발 플레이어7회 독립 실행 통과: 1~6장 정상 승리27/20/34/22/33/22플레이어턴, 재실행 정상 패배2턴과 재출전·중복 보상 방지·저장 복원 통과. 사용자 원본/백업 불변, 실행 로그 Exception/Assertion/Error 검색0건. PlayerReviews/fd83d49d46c040e486b3a8b7c21a83c5-summary.json 및 장별 보고서. 기본 SPD/AI 자동 검증이며 CT 완주를 새로 반복하지 않았다. 숨겨진 플레이어의 검은 캡처는 시각 증거로 쓰지 않는다.
- 요청한 중간 프레임 및 자세별 외형 일관성 개선은 완료했다. 전6장 사람 수동 완주·전체 기술별 독립 작화·외부 전문가 원작 외형 승인·사람 난이도/실물 패드 검수는 이번 완료 범위에 포함하지 않는다.

## 2026-10-04 — 슈로대 참고 소형 캐릭터 HUD

- 캐릭터 정보를 좌하단344×174 Canvas 단위의 요약창으로 줄였다. 기본1440×900 기준 기존268×656 대비 면적 약66% 감소. 이름/Lv·기존 캐릭터 그림·HP/MP/SP·이동/행동 상태를 표시하고 직업/세부 능력치/상태는 능력 버튼의 상세창에 분리했다.
- 명령은 캐릭터 바깥쪽, 기술 목록/상세는 오른쪽, 대상 실행/취소는 하단, 카메라는 상단으로 배치했다. 상세창 중 배경 UI/전장 입력 차단, 닫기/Esc/우클릭/패드B 복귀를 구현했다. Scene/콘텐츠/전투 규칙은 변경하지 않았다.
- 실제 Unity EditMode95/95, PlayMode55/55(102.62초) 통과. CompactHud/editmode-results.json, playmode-results.json. 새 검사는 요약창 면적·전장 폭·명령 버튼 경계·상세창 단독 입력·패드 취소 후 State/HP/MP 보존·출전 화면 복원을 확인한다. 전체89개 기술 설명 크기/폰트 검사도 통과했으며 새 결과를 CompactHud/skill-panel-review.csv에 보관했다. ManagedChecks는 재실행하지 않았다.
- 1920×1080 및1366×768 Game View에서 요약/명령·상세·기술·대상 선택을 확인했다. 최종 원본1366×768 캡처4장은 CompactHud/에 있다. 게임 프레임 증가와 상태를 확인했으며, Game View의 갱신이 늦은 캡처는 Repaint 후 다시 취득했다. 검수 후 해상도 선택과 runInBackground=false를 복원하고 Assets 임시 캡처를 제거했다. 세로 창의 별도 배치는 구현했으나 이번 시각 검수는 가로 창 기준이다.
- 기존 Builds/Windows 출력의 첫 빌드는 실행 중인 사용자 플레이어가 lib_burst_generated.dll을 점유하여 실패했다(25.570초/오류1, windows-build-locked.json). 플레이어를 강제 종료하지 않고 별도 Builds/WindowsCompactHud/TalesTactics.exe로 빌드하여 성공했다(4.569초/오류0/경고1). 현재 새 UI 실행 경로는 WindowsCompactHud이며 기존 Windows 폴더를 최신 검증본으로 사용하지 않는다.
- 개발 Builds/CampaignReview 빌드25.335초/오류0/경고9 성공. 최종 일반 빌드는 Pipeline 비활성 안내1건, 개발 빌드는 기존 사용 중단 API/전처리기·직렬화·셰이더 경고 포함9건이다. CompactHud/windows-build.json, development-build.json, build-hashes.json 참조.
- 새 개발 플레이어7회 독립 실행 통과:1~6장 정상 승리27/20/34/22/33/22플레이어턴, 재실행 정상 패배2턴과 재출전·중복 보상 방지·저장 복원. 사용자 원본/백업 불변. PlayerReviews/d216ec73315643378d3e07d433b72264-summary.json. 실행 로그 Exception/Assertion/Error 검색0건. 숨겨진 플레이어 캡처는 시각 검수로 사용하지 않는다. 기본 SPD/AI 자동 검증이며 사람의 전6장 수동 완주나 실물 패드 검수가 아니다.

## 2026-10-04 — 판매 준비 개선안 1–2: 전장 가독성·편성/장비/상점

- 실제 타일의 투영 경계로 전체 카메라를 맞추고 현재 유닛/선택 대상 집중 보기, 흰 행동자·금색 대상·청록 목적지 테두리, 이전/다음 대상 버튼·Tab/Shift+Tab·LT/RT를 추가했다. 대상 순환과 실행 확정을 분리하며 기존 Resolver로 유효성을 판단한다.
- 출전 준비를 장 선택/초상화 편성/선택 유닛 정보의 3열과 하단 고정 출전 버튼으로 구성했다. 카드 선택과 편성 변경을 분리했다. 장비 직접 후보 선택과 능력치 증감 비교, 구매/매각 공통 화면·종류 필터·거래 후 페이지 유지·구매 직후 장비 비교 동선, 별도 훈련/전투 설정을 연결했다. 저장 스키마/전투 규칙은 변경하지 않았다.
- 실제 Unity EditMode **95/95**(0.38초), PlayMode **58/58**(104.98초) 통과. ReleaseUi/editmode-results.json, playmode-results.json. 신규 3개 검사는 6장×4회전/집중 보기·복귀/목적지·재출전, 편성 선택 분리/구매→비교→저장, 대상 순환/능력창 입력 보호를 확인한다. 기존 테스트는 변경된 명시적 장비 후보 선택 동선을 따른다. 엔진 독립 ManagedChecks는 재실행하지 않았다.
- 최초 PlayMode 38/55 통과·17실패에서 전투 종료 후 지연 파괴 중 카메라 null 참조와 빈 매각 안내 차이를 찾았다. ResetBoard가 즉시 런타임 참조를 비우고 LateUpdate가 Session을 확인하도록 수정했다. 신규 테스트의 NUnit 구문 컴파일 오류를 수정한 뒤 테스트 도구의 중단/0개 검색 상태가 발생해 스크립트 재로드로 복구했다. 0개 결과는 검증으로 인정하지 않고 최종 58개 실제 실행 결과로 교체했다. 초기 실패 증거는 ReleaseUi/playmode-initial.json에 남겼다.
- Game View 1920×1080에서 편성/전체 전장/현재 유닛 확대, 1366×768에서 편성/상점 목록·상세/장비 증감/설정/전장/대상 선택 화면을 직접 확인했다. 화면별 Repaint와 증가한 frameCount를 확인하고 격리 CampaignSave/저장 콜백으로 구매→장착 비교를 조작했다. 캡처는 ReleaseUi/*.png. 임시 Assets 캡처를 제거했고 Game View 기본 Full HD·runInBackground=false로 복원했다. Scene은 dirty=false이며 ProjectSettings 변경은 없다.
- Windows 일반 빌드 **26.508초/오류0/경고7**, 개발 빌드 **27.902초/오류0/경고9** 성공. 기존 Pipeline·검수 API·직렬화·셰이더 경고가 남아 있다. ReleaseUi/windows-build.json, development-build.json 및 build-hashes.json. 최신 일반 실행 파일은 **Builds/WindowsReleaseUi/TalesTactics.exe**다. 기존 실행 중인 Builds/Windows 플레이어를 종료하지 않고 별도 위치에 빌드했다.
- 새 개발 플레이어 **7회 독립 실행 모두 통과**: 1~6장 승리 27/20/34/22/33/22 플레이어턴, 재실행 정상 패배 2턴과 재출전/저장 복원/중복 보상 보호. 구매 가격·보유 수량·새 UI 장비 장착 저장, 가상 패드 상점 진입/취소도 통과했다. PlayerReviews/85f1af87e4634056a8f36f59db07d54c-summary.json 및 장별 보고서. 사용자 원본/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변, 실행 로그 Exception/Assertion/Error 검색 0건(ReleaseUi/save-and-log-check.json).
- 범위/한계: 이번 요청의 1–2번 UI 개선 완료이며 전체 판매 준비 완료를 뜻하지 않는다. 기본 SPD/AI 자동 캠페인 검수이며 사람이 전6장을 수동 완주하거나 실물 게임패드·세로 화면·전체 모니터 비율을 검수한 결과는 아니다. 숨겨진 플레이어의 검은 캡처는 시각 증거로 사용하지 않는다. 전체 UI 화면과 조작은 ReleaseUi/README.md 참고.

## 2026-10-05 — 판매 준비 개선안 3: 입문 연습·도움말

- 출전 준비의 처음 플레이 · 도움말에서 Lv1 크레스 이동→공격→민트 회복→대기/방향을 직접 수행하는 연습을 추가했다. 단계별 명령/대상을 제한하고 실제 이동·SkillResolver·턴 시작/종료를 사용한다. 연습용 부상을 명시하고 적은 기다린다. 재시작/중단/완료에서 성장·편성·장비·골드·기존 설정·저장을 보존한다.
- 준비/전투 도움말 및 F1로 여는 8페이지 가이드에 이동/공격/회복/대기, 고저차/방향, 상태, 10명 궁극기 조건, 편성/저장, 입력을 설명한다. 수치/궁극기 조건은 현재 Catalog에서 읽는다. 모달 동안 배경 메뉴와 전장 입력을 차단하고 닫기/F1/Esc/우클릭/패드B로 기존 선택을 유지한다.
- 실제 Unity EditMode **95/95**(1.01초), PlayMode **61/61**(107.35초) 통과. TutorialHelp/editmode-results.json, playmode-results.json. 신규3개 검사는 실제 행동·취소·잘못된 대상·피해 미리보기·MP 비용·턴 종료 상태 지속시간·캠페인 불변, 도움말8페이지 글자/범위/단독 입력, 연습 중단 후 정상 캠페인을 확인한다. ManagedChecks는 이번에 실행하지 않았다.
- 최초 PlayMode60/61에서 테스트의 Campaign JSON 기준을 준비 화면의 기본 캐릭터 초기화 전에 수집한 문제를 수정했다. 준비 화면 표시 후 기준을 잡아61/61을 확인했다. 실제 화면에서 가려진 피해 미리보기와 과도한 모달 외곽 효과를 수정했다. 후속 검토에서 연습의 직접 Active 교체를 PracticeTurns+Session.Advance로 바꾸어 정상 EndTurn 경로를 보장했고 최종 전체 검사를 다시 통과했다. 초기 결과는 playmode-initial.json.
- 1366×768 실제 Game View에서 도움말·궁극기 조건, 이동·공격 미리보기·회복·방향·완료를 확인했다. 버튼 콜백과 타일 선택으로 정상 연출을 진행해 크레스HP145→190, 민트MP120→114를 확인했다. TutorialHelp/*.png에 보존했다. 검수 후 Game View Full HD/runInBackground=false 복원, 임시 Assets 캡처 제거, 테스트가 변경한 폰트 캐시 복원. Scene/ProjectSettings 변경 없음.
- Windows 일반 빌드 **33.388초/오류0/경고7**, 개발 빌드 **28.010초/오류0/경고9** 성공. 기존 Pipeline·검수 API·직렬화·셰이더 경고가 남아 있다. TutorialHelp/windows-build.json, development-build.json, build-hashes.json. 최신 일반 실행 파일은 **Builds/WindowsTutorial/TalesTactics.exe**. 기존 실행 중인 플레이어는 종료하지 않았다.
- 새 개발 플레이어 **7회 독립 실행 모두 통과**: 입문 연습 정상 완료/무저장 확인 후 1~6장 승리27/20/34/22/33/22플레이어턴, 재실행 정상 패배2턴과 재출전·저장 복원·중복 보상 방지. PlayerReviews/161b177e75074f2b8eda8a36df6ee532-summary.json 및 장별 보고서. 사용자 원본/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변, 실행 로그 Exception/Assertion/Error0건(TutorialHelp/save-and-log-check.json).
- 범위/한계: 3번 입문 연습·도움말 구현/검증 완료. 기본 SPD/AI 자동 캠페인 검수이며 사람의 전6장 수동 완주·초보자 학습성·실물 패드 검수는 별도다. 숨겨진 플레이어 캡처는 시각 증거로 사용하지 않는다. 다음 항목은 4번 저장·이어하기·설정이다.

## 2026-10-05 — 판매 준비 개선안 4: 저장·이어하기·설정

- 기존 campaign.json을 슬롯1로 보존하면서 슬롯2/3, 마지막 슬롯 기억, 저장 시각/진행 요약/중단 기록 표시, 현재 편성·선택 장 저장을 추가했다. 기존 보상·거래·장비·승급 자동 저장에 성공 시각/슬롯 안내를 연결했다. V1 이관·V2 호환·백업 복구·미래 버전 덮어쓰기 보호를 유지한다.
- 캠페인 아군 명령 대기에서 전투 중단 저장/이어하기를 추가했다. HP/MP/SP·위치·상태·쿨다운·장비·이동/행동/취소/고유 플래그·SPD 대기열/CT 누적·난수 상태를 보존한다. 복원 검증 및 중단 기록 소비 저장이 성공한 뒤 재개한다. 실패 시 현재 전투/원본을 유지한다. 훈련·연습·연출·적 턴·결과 중에는 중단할 수 없다. 재개 후 강제 종료는 복원하지 않으며 다시 중단 저장해야 한다.
- 창/전체화면·3개 해상도·음악/효과음·패드 커서 속도·자동 타이밍·기본 턴 순서/AI를 별도 설정 파일에 저장한다. 저장 후에도 초안을 분리해 미적용 변경이 현재 설정을 바꾸지 않게 했다. 미지원/손상 설정은 원본 보호 상태가 된다. 키 전체 재지정이나 임의 프레임 자동 저장을 구현한 것은 아니다.
- 실제 Unity EditMode **98/98**(4.39초), PlayMode **64/64**(109.67초) 통과. Persistence/editmode-results.json, playmode-results.json. 신규 검사는 슬롯 분리/기존 저장/미래 버전, 설정 재읽기/초안 취소, 실제 SPD/CT 이동·상태·KO·행동·난수 복원과 이후20턴 일치, 중단 실패/손상 기록 원본 보존 및 모달 입력 보호를 확인한다. 기존 PlayMode 테스트도 임시 저장 루트로 격리했다. ManagedChecks는 실행하지 않았다.
- 최초 PlayMode63/64에서 Unity JsonUtility가 null인 인라인 중단 객체를 다시 생성하는 문제를 발견했다. HasSuspendedBattle 플래그를 추가하고 소비 후 파일 재읽기까지 통과했다. 초기 결과는 Persistence/playmode-initial.json에 남겼다.
- 실제1366×768 Game View에서 슬롯·설정·중단 안내·저장 시각·이어하기·재개된 전장을 확인했다. 헤더 메뉴의 줄바꿈을 수정하고 최종 화면을 다시 확인했다. 프레임 갱신/Repaint와 격리 파일을 사용했으며 검수 후 기본 Full HD·runInBackground=false·Scene dirty=false 복원, Assets 임시 캡처를 제거했다. 이미지/조작/제한은 Persistence/README.md 참조.
- Windows 일반 빌드 **39.471초/오류0/경고8**, 개발 빌드 **24.945초/오류0/경고10** 성공. 기존 Pipeline·사용 중단 API/DEVELOPMENT_BUILD 전처리기·직렬화·셰이더 경고와 같은 종류이며 새 개발 검수 클래스의 전처리기 경고1건이 추가됐다. Persistence/windows-build.json, development-build.json 및 build-hashes.json. 최신 일반 실행 파일은 **Builds/WindowsPersistence/TalesTactics.exe**다. 기존 실행 중인 사용자 플레이어는 종료하지 않았다.
- 새 Windows 플레이어 **4회 독립 프로세스 저장/재실행 검증 통과**: SPD/CT 각각 이동·상태·게이지를 저장하고 종료한 뒤 같은 상태 복원, 디스크의 중단 기록 소비, 이후20턴 순서 일치, 다른 슬롯의 골드123 보존, 설정 재읽기·1366×768 실제 화면 적용 확인. Persistence/47fad1e280e3403f9d00007259b152e4/summary.json 및 격리 저장 원본. 실행 로그 원본은 PersistentDataPath/PersistenceReviews의 같은 GUID 폴더에 보존한다. 사용자 campaign.json/.bak 해시 불변, settings.json/슬롯2/3은 생성하지 않음, 실행 로그 오류0건.
- 새 개발 플레이어 캠페인 **7회 독립 실행 모두 통과**: 입문 연습·상점·장비 후 1~6장 승리27/20/34/22/33/22플레이어턴, 재실행 정상 패배2턴과 재출전·저장 복원·중복 보상 방지. PlayerReviews/8503653e73c04252aad78ac7f7d108bc-summary.json 및 장별 보고서. 사용자 원본/백업 해시 불변, 실행 로그 오류0건(Persistence/save-and-log-check.json).
- 범위/한계: 4번 저장·이어하기·설정 구현/검증 완료. 캠페인 완주는 기본 SPD/AI 자동 검수이며 CT는 별도 저장·복원/20턴 일치를 검수했다. 사람의 전6장 수동 완주, 실물 패드, 모든 모니터/전체화면 전환 조합 검수는 별도다. 숨겨진 플레이어 캡처는 시각 증거로 사용하지 않는다. 다음 항목은 5번 전투 진행 속도/결과 흐름이다.

## 2026-10-05 — 판매 준비 개선안 5: 전투 진행 속도·결과 흐름

- 캐릭터별 마지막 기술을 기억하고 목표 취소→상세→목록으로 복귀한다. 목록 강조/메뉴 포커스를 복원하며 취소로 MP/행동을 소비하지 않는다. 일반 공격 취소는 기존 명령으로 복귀한다.
- F5 시스템의 전투 진행 탭에 적 행동 1/2/4배와 간략 연출을 추가하고 설정 파일에 저장한다. 적 이동 보간·준비/복귀 대기만 조정하며 전역 Time.timeScale, 아군 연출, 파라 입력 시간은 유지한다. 간략 모드도 실제 기술 처리/턴 종료를 실행한다.
- 결과 전용 패널에 저장 상태·실제 골드/장비 증가량·6인 Lv/EXP와 새 기술 수를 표시한다. 실패 시 성장 행을 숨기고 같은 추첨으로 저장 재시도하며 보상 포기는 두 번 눌러 확정한다. 저장된 승리에서 다음 장 준비로 이동하고 패배/훈련/최종 장에서 이야기 없이 재도전한다.
- 실제 Unity EditMode **98/98**(3.96초), PlayMode **67/67**(112.17초) 통과. BattleFlow/editmode-results.json, playmode-results.json. 신규3개 검사는 기술 복귀/자원 보존, 실제 적 이동·공격의 모드별 피해/비용/상태/난수/다음 턴 일치, 저장 실패→재시도/중복 지급 방지·성장 표시/글자 넘침·모달 차단/다음 장을 확인한다. 엔진 독립 ManagedChecks는 실행하지 않았다.
- 첫67개 실행은 결과 제목 높이 부족으로66/67이었다. 제목 높이를 수정했다. 다음 실행은 적 행동 검사의300프레임 대기가 고FPS에서 실제 행동보다 먼저 끝나66/67이었다. 실제10초 제한으로 수정한 뒤67/67을 확인했다. 두 실패는 playmode-initial.json, playmode-frame-timeout.json에 남겼다. 최종 단일 장면 측정은1배1.102초/2배0.562초/4배0.286초/간략0.293초(enemy-speed.csv). 전체 성능 벤치마크는 아니다.
- 실제1366×768 Game View에서 적 설정·6인 승리 성장·저장 실패·패배 화면을 확인했다. 결과 화면은 격리 저장에 승패를 구성한 시각 검수이며 수동 완주를 뜻하지 않는다. 패배 재도전의 CommandState/Ongoing 진입도 확인했다. Game View Full HD/runInBackground=false·Scene dirty=false로 복원하고 임시 Assets 캡처를 제거했다. BattleFlow/*.png 참조.
- Windows 일반 빌드 **24.552초/오류0/경고8**, 개발 빌드 **20.920초/오류0/경고10** 성공. 기존 Pipeline·사용 중단 API/전처리기·직렬화·셰이더 경고가 남아 있다. BattleFlow/windows-build.json, development-build.json, build-hashes.json. 최신 일반 실행 파일은 **Builds/WindowsBattleFlow/TalesTactics.exe**이며 실행 중인 기존 Builds/Windows 플레이어는 유지했다.
- 새 개발 플레이어 **7회 독립 실행 모두 통과**: 1~6장 정상 승리27/20/34/22/33/22플레이어턴, 재실행 정상 패배2턴·재출전·저장 복원·중복 보상 보호. 1장1배/2장2배/3장4배/4·5장간략/6장1배 설정을 저장하고 실제 EnemyTurn 경로로 실행했다. 검수 도구 자체의 기존 Time.timeScale=4 가속이 있으므로 실행 시간은 일반 플레이 속도 측정값이 아니다. PlayerReviews/22a4ecc31a8a4239b8c8ab8eed375034-summary.json 및 장별 보고서. 사용자 원본/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변, 실행 로그 오류0건(BattleFlow/save-and-log-check.json).
- 새 개발 플레이어 **4회 별도 저장/재개 실행도 통과**: SPD/CT 각각 중단 저장 후 프로세스 재실행·상태/난수/이후20턴 일치·설정 재읽기·다른 슬롯 보존. Persistence/8536d230797e4400acb6213f5b642b39/summary.json. 사용자 저장/설정 불변, 실행 로그 오류0건. 동적 폰트 캐시와 기존 검사 CSV는 복원했다.
- 범위/한계: 5번 구현/검증 완료. 캠페인은 기본 SPD/AI 자동 검수이며 사람이 전6장을 수동 완주하거나 실물 패드·모든 화면 비율을 확인한 결과는 아니다. 숨겨진 플레이어 캡처는 시각 증거로 사용하지 않는다. 다음은 6번 목표 전달 및 적 역할 차별화다.

## 2026-10-05 — 판매 준비 개선안 6: 목표 전달·적 역할

- 준비/전투의 임무 · 적 정보에서 승리·패배 조건, 진행 상태, 장별 접근 방법과 실제 적4명의 역할·기본 기술/사거리·대응법을 표시한다. 전투 중 HP/격파 상태를 읽고 하단에 남은 적 수를 상시 표시한다. 캠페인 목표는6장 모두 전멸로 유지하며 다오스만 격파하면 끝난다는 잘못된 안내를 하지 않는다. 훈련 보스/도착/호위/생존 조건도 구분한다.
- 사격/마법형은 사거리 안에서 근접 위험을 피하고 돌격형은 공격 가능한 부상자에 추가 선호를 준다. 기본/Utility AI가 같은 보정을 사용하며 훈련·아군에는 적용하지 않는다. 전열의 기존 근접 행동과 다오스의 Utility 기술 선택을 유지했다. 수치·Content 에셋·보상·저장 스키마를 바꾸지 않았다. 행동 예정 확정 예고나 신규 기술 추가는 아니다.
- 실제 Unity EditMode **102/102**(4.23초), PlayMode **69/69**(112.89초) 통과. MissionRoles/editmode-results.json, playmode-results.json. 첫 PlayMode도69/69(113.50초, playmode-initial.json)이었다. 이후 설명/상단 배치를 정리하고 목표 선택 상태의 패드B 복귀 검사를 보강해 최종 전체 검사를 다시 통과했다. 실패 결과는 없었다. ManagedChecks는 이번에 실행하지 않았다.
- 신규 핵심 검사는 기본/Utility의 원거리 사거리3 유지·속박 이동 금지·판단 전후 위치/점유/자원/난수 불변, 돌격 대상 선호/적용 범위, 목표별 안내를 확인한다. UI 검사는6장 출전 전 적 구성·승패 문구/글자 넘침/배경 입력 차단·저장 불변, 전투 대상 선택 보존/패드B 취소·시스템 메뉴 전환·적 잔존 갱신/Restart를 확인한다.
- 기존6장×3편성 자동 전투18행은15승/3패로 기존 승패 구성이 유지됐다. 일부 턴 수/생존 HP 변화는 Docs/combat-party-matrix.csv와 MissionRoles/combat-party-matrix.csv에 새 기준선으로 기록했다. 사람의 체감 난이도 평가나 모든 편성의 균형 보장을 뜻하지 않는다.
- 실제1366×768 Game View에서6장 출전 전 임무·전투 HUD·격파/HP 갱신·호위 훈련 조건을 확인했다. 프레임 진행/Repaint를 확인하고 사용자 저장과 분리했다. 격파 화면은 적1명을 KO 처리한 표시 검수이며 수동 전투 완주 증거가 아니다. MissionRoles/briefing.png, battle.png, progress.png, escort.png. Game View Full HD/runInBackground=false·Scene dirty=false 복원 및 임시 Assets 캡처 제거 완료.
- Windows 일반 빌드 **24.086초/오류0/경고8**, 개발 빌드 **27.180초/오류0/경고10** 성공. 기존 Pipeline·사용 중단 API/전처리기·직렬화·셰이더 경고가 남아 있다. MissionRoles/windows-build.json, development-build.json, build-hashes.json. 최신 실행 파일은 **Builds/WindowsMissionRoles/TalesTactics.exe**이며 기존 실행 중인 사용자 플레이어를 유지했다.
- 새 개발 플레이어 **14회 독립 실행 모두 통과**. 기본 SPD/AI: 1~6장 승리27/23/35/24/29/25플레이어턴, 재실행 정상 패배2턴. CT/Utility: 1~6장 승리15/13/17/14/17/9플레이어턴, 재실행 정상 패배3턴. 상점·장비·입문 연습·설정·저장된 성장/보상·재출전·중복 보상 보호도 통과했다. PlayerReviews/331d12e51dfd4d7fb681a8a843c4e42b-summary.json 및 54383b23417641fbb0afff5ae7cca4ba-summary.json. 도구의 기존4배 시간 가속 및 장별 적 속도 설정으로 수행한 자동 검수이며 사람의 전투 시간 측정이 아니다.
- 사용자 원본/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변. 사용자 settings.json/슬롯2/3은 생성하지 않았다. 두 실행의 로그 Exception/Assertion/Error 검색0건(MissionRoles/save-and-log-check.json). 동적 폰트와 기존 시간/레이아웃 검사 CSV를 복원했고 새 검사 사본은 MissionRoles에 보존했다. 실제 전투 결과가 바뀐 combat-party-matrix.csv만 새 기준선으로 갱신했다.
- 범위/한계: 6번 구현·자동/시각 검증 완료. 실물 게임패드, 모든 화면 비율, 사람의 전6장 수동 완주·적 역할 인지성·체감 난이도 평가는 별도다. 숨겨진 플레이어 캡처는 시각 증거로 사용하지 않는다. 다음 항목은 7번 사람 플레이 기반 난이도·보상 균형 검수다.

## 2026-10-05 — 판매 준비 개선안 7 준비 (진행 중)

- 기존18개 자동 편성 결과·6장 경제를 검토하고 사람 검수 우선 항목을 정리했다: 기본3인과 권장6인의 난이도 차이, 후반 대기 캐릭터의 레벨 차이, 장비 구매/반복 사냥 필요성, 확정 장비 매각까지 포함한 반복 보상 체감. 이는 가설/검수 대상이며 새 사람 평가 결과가 아니다.
- Tools/StartBalancePlaytest.ps1은 기존 개발 빌드의 --manual-review를 사용한다. 새 GUID별 저장/설정·피드백 CSV·실행 manifest/로그를 분리하고 같은 GUID 재실행 시 이전 performance/hardware 기록을 보존한다. 같은 테스트 저장 중복 실행을 막고32자리GUID만 허용한다. PrepareOnly는 게임을 실행하지 않는다.
- 첫 숨김 실행에서 ManualPlayerReview.Start가 runInBackground 설정 전에 yield하여 기록 초기화가 멈췄다. 활성화 순서를 yield 앞으로 옮기고 미사용 CampaignFile 변수를 제거했다. 첫 실행 증거는 BalancePlaytest/launcher-initial.json. 개발 전용 검수 도구 변경이며 일반 게임 전투/보상 수치에는 변경이 없다.
- 개발 Windows 빌드18.517초/오류0/경고10 성공(BalancePlaytest/development-build.json). 수정 후 숨김 초기화·프레임 진행·동일 GUID 재실행2회, 중복 실행/잘못된GUID 차단, 피드백 보존 및 이전 기록 백업을 확인했다(launcher-check.json). 두 검수 프로세스는 확인 후 종료했으며 기존 사용자 플레이어는 유지했다. 숨김 실행의 극단적으로 높은FPS 값은 렌더링 성능이나 사람 플레이 속도 증거로 사용하지 않는다.
- 사용자 원본/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변. settings.json/슬롯2/3 미생성, 두 실행 로그 오류0건. 동적 폰트 캐시 복원. 현재 턴에는 EditMode/PlayMode 전체 검사를 재실행하지 않았다. 최신102/69 통과는 직전6번의 기록이다.
- 사람의 난이도·보상 의견은 아직 제공되지 않았다. 따라서7번은 미완료이며 수치 조정/완료 처리/커밋·푸시를 하지 않았다. BalancePlaytest/README.md와 prepared-session.json에 직접 플레이 절차와 격리 세션을 준비했다. 기존 성장/보상/가격은 유지한다.

## 2026-10-07 — 사용자 테스트 확인 및8번 전투 피드백·효과음 보강

- 사용자가 테스트 진행을 알리고 다음 단계 진행을 요청했다. 준비한 격리 세션0f88102af8fa4bc7a63025060109e25d의 기록에서6인 편성(크레스·민트·파라·벨벳·시온·티아), chapter1 완료·420G 저장, Victory 후 출전 준비 복귀를 확인했다. 실행 로그 오류 검색0건. feedback.csv의6행은 평점/의견이 비어 있다. 사용자1장 테스트 확인이며2~6장 수동 완주나 난이도·보상 만족 승인으로 확대하지 않는다. BalancePlaytest/user-test-summary.json 참조. 사용자 요청에 따라 다음 보강으로 진행하며7번 전체 균형 평가는 남겨 둔다.
- 피해·회복·HP 비용 숫자에 반투명 어두운 배경과 확대 글자, 초반0.38초 선명 유지/0.65초 제거를 적용했다. 카메라 회전 시 현재 화면 방향을 따라 머리 위 표시를 유지한다. 효과음은 동일ID45ms 중복 억제·동시4개 상한·신규 음성1/√n 게인으로 중첩을 완화한다. unscaledTime 수명 추적·음소거 시 예약 방지·StopAll 초기화를 적용하며 음악/테마 복귀와 전투 수치·난수·저장 형식은 유지한다. 마스터 리미터나 최대 음량 보장은 아니다.
- 실제 Unity EditMode **102/102**(2.94초), 최종 PlayMode **71/71**(112.93초) 통과. PresentationPolish/editmode-results.json, playmode-results.json. 새2개 검사는 효과음 중복/상한/게인/음소거/정지/일시정지 중 만료와 숫자 배경/대비/카메라 회전/HP 불변/재출전 정리를 확인한다. 기존14곡 재생·테마 위치 복귀 검사도 통과했다. ManagedChecks는 이번에 실행하지 않았다.
- 첫 PlayMode71/71(113.65초) 이후 화면 검수에서 회전 시 숫자 위치가 밀리는 문제를 발견해 수정했다. 다음 실행은 기존 SpecialPosesPrepareReleaseAndRecoverForEntireRoster 검사에서 프레임 지연으로 시작 프레임을 지나친 뒤 비교하여70/71이었다. 시작 프레임 검사 구간만 시간을 멈추고 실제 시간의 복귀 검사를 유지하도록 수정한 후 최종71/71 통과했다. 두 이전 결과는 playmode-before-rotation.json, playmode-pose-timing-failure.json에 보존했다.
- 실제1366×768·1920×1080 Game View에서 피해/회복 대비와 회전 후 위치를 확인했다. 격리 훈련 저장에 HP 변화량을 구성하고 캡처 동안 시간을 멈춘 표시 검수다. 자연 전투 완주/실제 표시 지속 시간 증거는 아니다. 프레임 진행은1539→3694→3904로 확인했다. Full HD/runInBackground=false·Time.timeScale=1·Scene dirty=false로 복원하고 임시 Assets 캡처를 제거했다. feedback.png, feedback-rotated.png 참조.
- Unity 시작의 Scene Backup Detected 숨김 창은 Computer Use 활성화가 실패했다. 이전 장면 백업을 LocalReview/SceneBackup-20261007에 보존하고 이번에 시작한 에디터만 재시작해 정상 연결했다. 백업은 로컬 보관하며 Git에 포함하지 않는다. 에셋 생성기/장면 재생성은 실행하지 않았다.
- Windows 일반 빌드 **27.601초/오류0/경고8**, 개발 빌드 **20.595초/오류0/경고10** 성공. 기존 Pipeline·전처리기/사용 중단 API·직렬화·셰이더 경고가 남아 있다. PresentationPolish/windows-build.json, development-build.json, build-hashes.json. 최신 일반 실행 파일은 **Builds/WindowsPresentationPolish/TalesTactics.exe**다.
- 새 개발 플레이어7회 독립 실행 모두 통과: 기본 SPD/AI의1~6장 승리27/23/35/24/29/25플레이어턴, 재실행 정상 패배2턴과 재출전/저장 복원/중복 보상 방지. PlayerReviews/512cb66074294c8cbd2aada5c4f96f9f-summary.json 참조. 검수 도구의4배 시간 가속과 장별 적 속도 설정으로 수행한 자동 검사이며 사람 플레이 시간/청음 검수는 아니다. 숨김 플레이어 캡처는 시각 증거로 사용하지 않았다.
- 사용자 원본/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변. settings.json/슬롯2/3 미생성. 실행 로그7개의 Exception/Assertion/Error 검색0건(PresentationPolish/save-and-log-check.json). 동적 폰트 캐시와 기존 시간/레이아웃 검사 CSV는 복원하고 새 검사 사본만 PresentationPolish에 보존했다.
- 범위: 이번8번은 전투 표시·효과음 중첩의 구현 보강이다. 신규 원화/음원 제작이나 작화·스피커/헤드폰별 최종 청음 승인이 아니다. 전체8번 최종 품질 평가와7번 나머지 사람 균형 평가는 미완료로 유지한다. 완료한 보강과7번 실행 도구만 검증 후 반영한다.

## 2026-10-07 — 8번 후속 에셋 감사·기술명 가독성

- 기술명이 속성색으로 지형 위에 직접 표시되던 부분에 밝은 글자/반투명 어두운 배경을 적용했다. 일반 글자 크기2.8→3.4, 생성 시 preferred width를 한 번 측정해 긴 이름의 한 줄 표시를 보장한다. AutoSize를 표시 중 반복하지 않는다. 속성색은 준비 고리에 유지하고 카메라 회전/발동/재출전 정리를 기존 경로에 연결했다. 전투 수치·비용·판정·아트/음원 에셋은 변경하지 않았다.
- 현재20종의 스프라이트1012개 참조(캐릭터별 중복 제거)에서 기본4방향 누락·텍스처 밖 rect·비정상 PPU0건, 오디오18개 참조 모두 존재. Tools/ReviewPresentationAssets.cs.txt 및 PresentationQuality/asset-inventory.json. 보충 동작20시트/320칸 알파 측정에서 빈 칸·셀 경계 접촉/잘림0건(alpha-audit.json). 이는 추가 시트의 알파/경계 검사이며 모든 작화 세부 품질 보장은 아니다.
- 기존 AnimationPolish 갤러리8장(영웅/적×4방향)을 다시 읽고 머리/무기/망토 경계와 캐릭터별 식별을 확인했다. 새 갤러리 렌더링은 아니며 원화/스프라이트 참조는 그대로다. 음악14곡은 기존 manifest SHA256와 일치한다. 새 Tools/ReviewAudioQuality.py는 음악14+효과음4 WAV를 읽어 빈 데이터/완전 무음/PCM 클리핑0건을 기록했다(audio-audit.json). 혼합 출력의 최대 피크나 실제 장치 청음 평가는 아니다.
- 실제 Unity EditMode **102/102**(3.91초), PlayMode **72/72**(113.23초) 통과. 신규 검사는 긴 한글 기술명의 한 줄/넘침 없음·배경 폭·밝은 글자·AutoSize 비활성·회전 추적·Restart 제거를 확인한다. 기존 준비/판정/비용1회 처리 및 전체 연출/음악 검사가 함께 통과했다. 이번 실행 실패 없음. ManagedChecks는 실행하지 않았다.
- 1366×768 Game View의1~6장에 가장 긴 카탈로그 기술명 `미스틱 케이지 / Mystic Cage`를 구성하여 밝은/어두운 바닥 및 장별 원경에서 대비를 확인했다. chapter1~6.png. 격리 저장의 메모리 해금과 크레스 위 타 캐릭터 기술명 표시, Time.timeScale=0으로 캡처한 표시 검수이며 정상 습득/전투 완주 증거는 아니다. 프레임7558→9601 진행 확인. 종료 후Time.timeScale=1/runInBackground=false/Full HD·Scene dirty=false 복원, 임시 Assets 캡처 제거 완료.
- Windows 일반 빌드 **21.640초/오류0/경고8**, 개발 빌드 **21.479초/오류0/경고10** 성공. 기존 Pipeline·전처리기/사용 중단 API·직렬화·셰이더 경고 유지. PresentationQuality/windows-build.json, development-build.json, build-hashes.json. 최신 일반 실행 파일은 **Builds/WindowsPresentationQuality/TalesTactics.exe**다.
- 새 개발 플레이어7회 독립 실행 통과:1~6장 승리27/23/35/24/29/25플레이어턴, 재실행 정상 패배2턴·재출전/저장 복원/중복 보상 보호. PlayerReviews/029aceb694f24f07829e1a74d207ad5e-summary.json. 기본 SPD/AI 및 기존4배 시간 가속의 자동 검수이며 사람의 전투 시간/청음 증거가 아니다. 숨김 플레이어 캡처는 시각 증거로 사용하지 않았다. 실행 로그7개 오류 검색0건, 사용자 저장/백업 해시 불변, settings/슬롯2/3 미생성. PresentationQuality/save-and-log-check.json, user-files-after.json. 동적 폰트 캐시와 기존 검사 CSV를 복원했다.
- 이번8번의 기술적 에셋 감사·확인된 가독성 보완은 완료했다. 작화 선호/실제 장치 청음 최종 평가는 별도로 남기며 다음 구현 순서는9번이다. 신규 원화·음원 제작이나 전체8번의 사람 최종 승인을 선언하지 않는다.

## 2026-10-07 — 9번 화면 비율·메뉴 키보드 보강

- 2560×1080에서 준비 화면의5번째 편성 카드 행과 상점 버튼이 하단 메시지/출전 영역을 침범하는 문제를 재현했다. ScaleWithScreenSize CanvasScaler를 Expand로 전환해1440×900 논리 영역을 확보한다. ConstantPixelSize 설정과 전투 수치/콘텐츠 에셋은 변경하지 않았다. Accessibility/ultrawide-before.png, ultrawide-after.png.
- 메뉴/모달/이야기/결과에서 Tab/Shift+Tab으로 활성·사용 가능한 버튼을 순환하며 선택 Outline을 금색으로 표시한다. 키보드 입력 시 패드 커서를 비활성화하고 기본 EventSystem Submit을 복원한다. 기존 전투 대상 Tab 및 패드 조작을 유지한다. 새 PlayMode 검사는 패드→키보드 전환·모달 뒤 버튼 제외·역순 순환·Enter1회 실행/닫기를 확인한다.
- 실제 Unity EditMode **102/102**(3.83초), PlayMode **73/73**(113.74초) 통과. 단독 키보드 검사1/1(1.4초). 초기 단독 검사는 완료 보고가 갱신되지 않아 취소/재시도했다. EditMode 뒤 PlayMode0개로 종료한 시도2회는 무효 처리하고 스크립트 재로드/73개 발견 후 전체 실행했다. 테스트 실패0건, ManagedChecks는 실행하지 않았다. Accessibility/editmode-results.json, playmode-results.json, keyboard-test.json.
- 실제 Game View1024×768/1280×800/1366×768/1920×1080/2560×1080의 준비·설정·기술 목록15화면 검수. 활성 버튼 경계 이탈0건, 편성/상점/하단 영역 겹침 해소 확인. 격리 저장·훈련 Lv25·1장, eval로 메뉴를 열었으며 프레임5245→23281 진행을 확인했다. 파라 기술 목록 대표 검수이며 모든 장/팝업 조합은 아니다. Keyboard Tab 이벤트 주입 후 금색 테두리 화면도 확인했다. Accessibility/aspect-matrix.json 및 README.md. 작은 화면의 사용자 체감 글자 가독성은 별도다.
- Windows 일반 빌드 **22.870초/오류0/경고8**, 개발 빌드 **19.395초/오류0/경고10** 성공. 기존 Pipeline·전처리기/사용 중단 API·직렬화·셰이더 경고 유지. Accessibility/windows-build.json, development-build.json, build-hashes.json. 최신 일반 실행 파일 **Builds/WindowsAccessibility/TalesTactics.exe**.
- 현재 물리 장치는 Keyboard/Mouse만 열거되었다. 가상 Gamepad 테스트는 실물 USB/Bluetooth·기종별 드라이버 검수가 아니다. 실제 모니터 DPI/전체화면·저시력/색각 사용자 평가는 남긴다. 키보드만의 전장 타일 조작·키 재설정·글자 배율/색각 옵션은 이번 변경에 포함하지 않았다. 완료한 화면/메뉴 개선만 반영하며9번 전체 완료로 표시하지 않는다.
- 새 개발 플레이어7회 독립 실행 모두 통과:1~6장 승리27/23/35/24/29/25플레이어턴, 재실행 정상 패배2턴·재출전/저장 복원/중복 보상 보호. PlayerReviews/5ebe2f6eaad245ea819fae38dadc97e1-summary.json. 기존4배 시간 가속의 자동 검사이며 사람 플레이 시간이나 물리 입력 검수가 아니다. 로그7개 Exception/Assertion/Error 검색0건.
- 사용자 campaign.json/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변, settings/슬롯2/3 미생성. Accessibility/user-files-before.json, user-files-after.json, save-and-log-check.json. 동적 폰트/기존CSV·TimeManager 직렬화 변경을 복원하고 검사 사본만 보존했다. 임시 Assets 캡처와 검수용 Game View 항목을 제거하고 Full HD·Time.timeScale=1/runInBackground=false·Scene dirty=false 복원 확인(editor-restored.json).

## 2026-10-07 — 10번 로컬 배포·전체 패키지 업데이트

- Tools/PackageRelease.py와 배포용 Verify-Package.ps1/Backup-Saves.ps1, 플레이어 시작/업데이트/복귀/제보 안내 및 변경 내역을 추가했다. 새 버전 ZIP·전체 파일 manifest·SHA256 체크섬을 만들고 기존 버전을 덮어쓰지 않는다. 게임 소스 변경 상태/잘못된 빌드 증거/실행 파일·Runtime DLL 해시 불일치/개발 검수 코드/누락 파일/사용자 저장 포함을 거부한다. 원격 업로드·온라인 업데이트·자동 저장 복원은 없다.
- 도구 **13/13 검사 통과(3.464초)**. 실제 Windows PowerShell로 정상 패키지/백업을 확인하고 변조·누락·추가·경로 탈출/대소문자 중복·같은 버전 덮어쓰기·개발 빌드/사용자 저장 포함 차단,3슬롯/설정/백업 이력 보존 및 별도 폴더 업데이트의 이전 버전/파일 분리를 검사했다. Distribution/tool-tests.txt. 첫 두 실행은 스크립트 실행 정책 및 이 환경의 Get-FileHash 모듈 검색 문제로 각각2개 실패했다. 프로세스 한정 실행 옵션과 .NET SHA256 계산으로 보완했으며 이전 실패 로그도 보존했다. Unity 검사와 합산하지 않는다.
- 실제 배포 ZIP **0.1.0-preview.1**,199,546,142바이트,202개 파일+manifest 생성. SHA256 **10e792b1f683da7ece10d6d7f69267fbc3aba487cad56e2e737591575e739a11**. Builds/Releases/TalesTactics-0.1.0-preview.1-windows-x64.zip. 게임 소스 커밋20b8504, 제품 버전0.1.0/Unity6000.6.0f1/저장V2 유지. 새 게임 빌드가 아니라 직전 WindowsAccessibility 일반 빌드 포장이며 이전 검증의 EXE/Runtime DLL 해시와 일치한다. Unity 배포 제외 폴더의1개 파일을 제외하고 폰트 고지를 포함했다. Distribution/package-result.json, package-manifest.json.
- ZIP을 Builds/ReleaseSmoke/0.1.0-preview.1의 새 폴더에 풀고 Windows PowerShell로202개 파일 검사 통과. 실제 사용자 원본/백업2개를 Builds/SaveBackups/20261007-203657-ae6ed3b42c63467f97ae4a34df49f97d에 복사하고 백업 해시 검사 통과. 사용자 원본/백업 SHA256 324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1 불변, settings/슬롯2/3 미생성. 백업 내용과 바이너리는 Git에서 제외한다. Distribution/user-backup-check.json, user-files-before.json, user-files-after.json.
- 압축 해제한 일반 플레이어를 숨김 실행하여10초 후 프로세스 유지(작업 메모리296,050,688바이트), Mono/PhysX/입력 초기화와 로그 오류0건을 확인했다. 숨김 창 핸들0으로 CloseMainWindow가 불가능해 이 검수 프로세스PID39348만 종료했다. 정상 종료·화면/프레임 진행·사람 플레이 또는 성능 기준으로 계산하지 않는다. Distribution/release-startup.json. 실행 후 파일 무결성 검사도 통과했다.
- 이번에는 게임 코드/콘텐츠를 바꾸지 않아 Unity 테스트·빌드/캠페인 전체 회귀를 반복하지 않았다. 직전 실제 Unity102Edit/73Play 및7회 플레이어 검증은 동일 EXE/Runtime DLL의 Accessibility 기록이다. 10번의 로컬 배포/수동 전체 패키지 업데이트 체계는 완료하며 온라인 자동 업데이트·설치/서명·스토어 공개는 배포 채널 결정 후 별도 범위다. 7~9번의 남은 사람/장치 검수는 유지한다.
- 최종 패키징 점검에서 빌드 보고서의 전체 파일 목록도 사전 검사하도록 보강했다. 실제 보고서199개 중 배포 제외1개를 뺀198개 파일의 존재/크기 일치 확인(Distribution/build-file-check.json). 필수 이름 목록 밖의 런타임 의존 파일 누락을 차단하는 검사를 포함해 최종13/13 통과했다. 패키지에 포함된 두 도구와 시작 안내/변경 내역은 현재 소스 해시와 일치한다.

## 2026-10-07 — 승인 시안 기반 아이콘 전투 HUD

- 2×2 명령 아이콘, 보조 명령, 4장 단위 기술 카드/페이지 전환, 별도 기술 상세, 작은 캐릭터 요약, 상단 행동 순서 초상, 대상 가까이의 결과 패널을 적용했다. 전투 배경은 전체 화면에 렌더링하며 타일은 HUD 안전 영역 안에 맞춘다. 패널 후보 위치를 주변 생존 유닛의 투영 영역과 비교한다. 전투 데이터/저장 형식은 유지했다.
- 실제 Unity **EditMode 102/102**, **PlayMode 75/75(115.74초)** 통과. 신규 2개 검사는 전체 기술/궁극기 페이지 접근·아이콘 렌더러·상세 모달 차단·비용 보존과 카메라 회전/기술 화면에서 타일의 안전 영역 유지를 확인한다. 기존 89개 기술 설명은 상세 모달에서 높이/글리프 검사를 통과했다. IconHud/editmode-results.json, playmode-results.json, skill-detail-review.csv.
- 첫 전체 실행은70/75였다. UI 변경에서 입문 연습 피해 미리보기가 빠진 문제를 복구했다. 다른3개 실패는 Awake에서 읽은 사용자6인 편성이 테스트용 저장 경로로 바꾼 뒤에도 남아 발생해, 테스트 준비에서 기본3인 편성을 명시적으로 분리했다. 걷기 프레임 관측1개는 다음 전체 실행에서 정상 통과했고 애니메이션 코드는 바꾸지 않았다. 실행기0개 결과는 통과로 계산하지 않았다. 첫 실패는 playmode-first-results.json에 남겼다.
- 전체 회귀 이후 패널의 유닛 가림을 줄이는 위치 선택을 보완하고 관련 요약/입력 검사 **1/1(1.07초)**를 통과했다. 최종 1024×768/1280×800/1366×768/1920×1080/2560×1080 × 명령/기술/대상/이동/방향 **25조합**에서 활성 버튼 화면 이탈0건. 그중3해상도의 명령/기술/대상9장 캡처를 보존했다. 저해상도 대상 가림을 시각 확인 후 보완했다. 긴 궁극기 이름/사용 불가 표시의 최종 텍스트 넘침0건, 목표 선택 비활성도 확인했다. IconHud/aspect-matrix.json, placement-test-results.json, ultimate-layout-check.json.
- 일반 Windows 빌드 **32.627초, 오류0/경고8**, Builds/WindowsIconHud/TalesTactics.exe. 기존 Pipeline 비활성, 개발 검사 전처리기, nullable 직렬화, 디버그 셰이더/TMP 경고다. IconHud/build-results.json. 실행 파일과 같은 폴더의 Data/DLL 전체가 필요하다.
- 일반 플레이어 숨김 시작 검수에서10초 후 프로세스 유지, 작업 메모리283,447,296바이트, 시작 로그 오류/예외0건을 확인했다. 숨김 창을 정상 종료할 수 없어 이 검수 프로세스만 종료했다. 이 결과는 사람 플레이/프레임 성능 검사가 아니다. IconHud/player-startup.json, player-startup.log.
- 사용자 원본/백업 해시는 이번 확인 전후 동일했다. 원본15B253B1C0AC332C33F0BFA73C5203B765553B8499E337FF881A59099AFB0F3E, 백업324E361730B35C466BEBE77A4D9E4730ED9BB8764BB8298FCE26807E6C7081C1. 원본 수정 시각21:36:55는 이번 HUD 검수 전이며 이전 배포 검수 시점 해시와 구분한다. 자동 전투/캡처는 임시 저장 경로를 사용했다.
- 실제 패드와 사용자 체감 검수는 남는다. 원화풍 개별 기술 그림을 추가한 것이 아니라 해상도에 독립적인 도형 아이콘으로 조작 체계를 바꿨다. 게시된preview.1 릴리즈는 변경하지 않았다.

## 2026-10-07 — 간결한 명령·방향 화살표·아군 통과

- 명령창을104×142 논리 좌표의5줄(이동·공격·기술·방어·대기)로 축소했다. 이동 취소 가능 시에만6줄/높이168로 늘어난다. 출전 복귀는 기존 상단 시스템 메뉴에서 제공한다. 이동 안내180×78, 실행/취소168×112로 축소했다.
- 방향 선택은20×20 그래픽 영역의 얇은 꺾쇠4개와40×40 투명 클릭 영역이다. 캐릭터 주위를 따라가며 카메라 회전별 화면 방향과 Facing을 대응시킨다. 현재 방향은 금색이다. 클릭은 기존 ChooseFacing으로 턴을 종료한다.
- Dijkstra 탐색은 같은 팀 점유 칸을 통과하지만 해당 칸을 도착 후보에서 제외한다. 경로의 부모 연결은 보존한다. 적/벽/고저차/이동 비용/Root/Cage와 최종 배치·강제 이동의 점유 제약은 유지한다.
- 실제 Unity EditMode **109/109**, PlayMode **77/77**(117.33초) 통과. 신규7개 규칙 사례는 양 팀 통과/적 차단, 점유 도착 금지, 비용/고저차/벽/이동 불가를 확인한다. 신규 플레이2개는4회 카메라 회전×4방향의 UI 포인터 클릭, 아군 통과 애니메이션 중 점유 유지, 도착과 이동 취소를 확인한다. 기존 튜토리얼·저장·기술·전투 회귀도 통과했다. 증거: SimpleHud/editmode-results.json, playmode-results.json. ManagedChecks를 Unity 검사로 표기하지 않았으며 이번에는 별도로 재실행하지 않았다.
- Game View1024×768,1280×800,1366×768,1920×1080,2560×1080에서 명령·기술·대상·이동·방향25개 상태의 활성 버튼 화면 경계를 확인했다. 대표 명령/방향/이동 화면을 직접 검수했다. SimpleHud/aspect-matrix.json 및 README.md의 PNG 참조. facing-final은 실제 이동→대기 흐름의 화면이다.
- Windows 일반 빌드 **23.804초/오류0/경고8**, 개발 빌드 **28.128초/오류0/경고10** 성공. 경고는 기존 Pipeline 설정, 개발 검수 코드의 폐기 API/컴파일 지시어, nullable 직렬화, URP/TMP 셰이더 관련이다. SimpleHud/windows-build.json, development-build.json에 전체 기록을 보존했다. 일반 빌드10초 시작 검사에서 프로세스 정상 유지 및 로그 예외 없음(startup-check.json, windows-startup.log).
- 개발 플레이어를 별도 저장 공간으로7회 실행해1~6장 승리와 재실행을 통과했다. 턴 수는25/20/26/20/27/21 및 재실행2. 보상/장 해금/중복 지급 방지/저장 재실행 확인은 PlayerReviews/17118daf764c47928b433bc1d5a8a391-summary.json과 각 장 보고서 참조. 숨겨진 플레이어의 검은 캡처는 시각 증거로 사용하지 않았다.
- 작업 전후 사용자 campaign.json 및 백업 SHA256 불변(SimpleHud/user-save-before.json, user-save-after.json). 임시 Game View 해상도와 캡처 에셋을 제거하고 Play Mode를 종료했으며 runInBackground=false로 복구했다. 사용자 Content/Scene 에셋은 변경하지 않았다.
- 최신 일반 실행 파일: Builds/WindowsSimpleHud/TalesTactics.exe. 폴더 전체가 필요하다. 실제 하드웨어 게임패드 수동 검수 및 사람이6장을 완주한 결과는 아니다. 기존 릴리즈와 main은 변경하지 않는다.

## 2026-10-07 — 기술 목록 축소와 중간 단계 제거

- 기술 UI를 폭300, 최대4행의 세로 목록으로 줄였다. 이름/MP와 가리킨 기술의 사거리·속성을 표시한다. 목록 길이에 맞춰 높이를 줄이며 여러 페이지일 때만 이전/다음이 나온다. CanUse 및 연습 조건으로 비활성 기술을 숨긴다. 빈 목록에는 안내 한 줄과 취소만 표시한다.
- 기술 클릭은 SelectSkill→TargetSelectionState로 바로 이어진다. 중간 ‘목표 선택’ 버튼을 제거했고 대상 확인 후 ‘실행’은 유지한다. 취소는 목록으로 바로 복귀하며 마지막 선택을 복원하고 사거리 표시를 지운다. 다른 기술 선택 시 이전 Target을 초기화한다. 도움말/입문 회복/개발 플레이어 검수 경로도 새 절차에 맞췄다.
- 실제 Unity 전체 PlayMode **78/78** 통과(116.19초). 초기76/78의 실패2개는 구 UI 너비400 이상 요구와300의 부동소수점 정확 비교였다. 새 요구와 허용 오차에 맞춰 수정했다. 이후 시각 검수에서 취소 후 사거리 표시 정리와 빈 목록 문구를 보완하고 관련 **9/9** 재통과(최종7.84초). CompactSkills/playmode-initial.json, playmode-results.json, skill-regression-results.json 참조.
- 직접 선택/취소/기술별 기억, MP 부족 시 숨김 및 회복 후 재표시, 빈 목록·페이지 보정, 선택/상세 보기 비용 유지, 대상 초기화, 실행 시 한 번만 MP 차감과 피해 적용을 확인했다. 실제 포인터 Submit도 대상 선택으로 진입했고 취소 후 범위 제거 및 MP 불변을 확인했다(pointer-review.json). EditMode109개는 직전 통과 기록이며 이번 UI 변경에서는 재실행하지 않았다. ManagedChecks도 재실행하지 않았다.
- 1024×768,1280×800,1366×768,1920×1080,2560×1080의 목록/대상10개 상태에서 활성 버튼 화면 경계 이탈0건. 대표 화면을 직접 검수했고 마지막 사거리/빈 목록 정리 후1920×1080 재촬영했다. CompactSkills/aspect-matrix.json 및 README.md/PNG 참조. 별도 임시 저장 경로를 사용했으며 사용자 원본/백업 SHA256 불변.
- 최종 Windows 일반 빌드 **11.944초/오류0/경고5** 성공(CompactSkills/windows-build.json). 기존 Pipeline 설정/개발 컴파일 지시어/nullable 직렬화 경고가 남아 있다.10초 숨김 실행에서 프로세스 유지와 로그 예외0건(startup-check.json, windows-startup.log). 이전 UI 실행 중인 사용자 게임은 종료하지 않았다.
- 최신 실행 파일: Builds/WindowsCompactSkills/TalesTactics.exe. 폴더 전체가 필요하다. 실제 물리 패드 검수 및 새6장 수동 완주는 포함하지 않는다. 사용자 Content/Scene/저장 형식은 유지하고 임시 캡처 에셋·해상도와 Play Mode를 정리했다.

## 2026-10-07 — 캐릭터 기준 기술 창과 바깥 클릭 취소

- 기술 목록과 대상 피해/실행 창을 시전자 옆에 배치하고 카메라 회전에 맞춰 추적한다. 이전/다음 대상 창은 제거하고 Tab/Shift+Tab 및 패드 대상 전환은 유지한다. 대상 미리보기는 최대2명과 추가 인원 수를 표시한다.
- 컨텍스트 밖 클릭은 한 단계 취소한다. 이동 직후 명령 상태에서는 되돌릴 수 있는 이동만 취소한다. 유효한 이동 칸/대상 선택을 우선하고 다른 UI, 모달, 이야기, 행동 연출, 결과 상태는 보호한다. 마우스와 패드 커서 Submit이 같은 처리 경로를 사용한다.
- 실제 Unity 전체 PlayMode **81/81(117.08초)** 통과. 최종 배치 보정 후 관련 **3/3(1.71초)** 재통과. 실제 마우스 이벤트, 기술 행 클릭, Tab/Shift+Tab, 유효 칸 우선, 취소 단계, 이동 되돌리기, 모달 보호, 카메라4방향 추적을 확인했다. ContextHud/playmode-results.json, context-final-tests.json 참조. EditMode109개는 직전 통과 기록이며 이번에는 재실행하지 않았다. ManagedChecks도 재실행하지 않았다.
- 최종1024×768/1280×800/1366×768/1920×1080/2560×1080의 기술/대상10조합에서 활성 버튼 화면 이탈0건. 대표3해상도6장 캡처를 저장하고 직접 확인했다. 해상도 전환 직후 측정의 일시적 헤더 이탈은 Canvas 갱신과 안정화 후 재측정하여 해소했다. ContextHud/aspect-matrix.json 및 README.md/PNG 참조.
- Windows 일반 빌드 **18.712초, 오류0/경고5** 성공(ContextHud/windows-build.json). 기존 Pipeline 설정/개발 컴파일 지시어/nullable 직렬화 경고가 남는다. 최신 실행 파일은 Builds/WindowsContextHud/TalesTactics.exe이며 폴더 전체가 필요하다.
- 10초 숨김 시작에서 프로세스 유지 및 로그 예외0건을 확인했다(ContextHud/startup-check.json, windows-startup.log). 기존 사용자 게임은 종료하지 않았다. 사용자 원본/백업 저장 SHA256은 작업 전후 동일하며 검수에는 임시 저장 경로를 사용했다.
- 임시 캡처 에셋과 Game View 해상도를 정리하고 Play Mode 종료/runInBackground=false 복구를 완료했다. 실제 물리 패드 수동 검수 및6장 수동 완주는 이번 검증에 포함하지 않는다. Content/Scene/저장 형식과 게시 릴리즈는 변경하지 않았다.

## 2026-10-08 — 대상 클릭 즉시 사용

- 기술 선택 후 유효한 전장 대상을 클릭하면 별도 실행 버튼 없이 발동한다. 마우스 이동은 피해/회복 미리보기만 표시하고 Tab/Shift+Tab 전환과 Enter/숫자패드 Enter 사용을 제공한다. 바깥 클릭 취소·모달 보호·행동 중 중복 입력 차단을 유지한다.
- 최종 실제 Unity 전체 PlayMode **82/82(121.10초)** 통과(DirectCast/final-playmode-results.json). 실제 마우스 호버의 비용/피해 불변, 클릭 발동, 반복 클릭/확인 시 MP 한 번 차감, Tab/Enter 경로와 기존 입문 공격·회복 흐름을 포함한다. EditMode와 ManagedChecks는 이번 UI 변경에서 재실행하지 않았다.
- 앞선81/81 및 호버 추가1/1은 공유 작업폴더의 전술 변경이 있던 중간 검증이다. 다른 작업자가 자신의 변경을 백업·분리한 뒤 이번 UI 변경만 있는 상태에서 최종82개를 다시 검증했다. 전술 변경은 이번 커밋/빌드에 포함하지 않는다.
- 1920×1080 실제 대상 미리보기 화면에서 실행 버튼 제거와 캐릭터 옆 배치를 확인했다(DirectCast/target.png). 임시 저장 경로를 사용하고 Play Mode 및 runInBackground를 복구했다. 실제 하드웨어 패드·6장 수동 완주 검수는 포함하지 않는다.
- 최종 Windows 일반 빌드 **28.564초/오류0/경고8** 성공(DirectCast/windows-build.json). 기존 Pipeline/개발 빌드 검사/nullable 직렬화/셰이더·TMP 경고가 남는다. Builds/WindowsDirectCast/TalesTactics.exe가 최신 실행 파일이며 폴더 전체가 필요하다.
- 해당 빌드10초 숨김 시작에서 프로세스 유지 및 로그 예외0건(DirectCast/startup-check.json, windows-startup.log). 기존 실행 중인 사용자 게임은 종료하지 않았다.

## 2026-10-08 — 시스템·UI 전술 개선

- 적 선택/전체 위험 범위, 고정 공격 예고, 이동 위험, HP·비용·상태의 전체 예측, 턴 순서 연결·방향·상태 지속, 키보드 타일 조작, 100/115/130% 글자 최대 크기를 구현했다. 실제 판정과 같은 Resolver를 독립 복제본에서 실행하며 확률 효과는 따로 표시한다. 기존 대상 클릭 즉시 사용을 유지한다. 역할별 적 기술 4개, 보호/예고/휴식, 선택 특성 5종, 장신구 3개, 장별 목표, 추천 6인·합류 훈련·기술 숙련·6장 궁극기 체험을 추가했다. 구체적인 규칙은 [SystemUi/README.md](SystemUi/README.md).
- 실제 Unity EditMode **125/125(0.72초)**, 전체 PlayMode **87/87(117.26초)** 통과. 예측 시 상태/점유/난수 불변, 실제 결과 일치, 비용·회복·보호, 예고 회피, V1 중단 호환/V2 임무 진행, 저장 실패 복원, 실제 UI 저장·키보드 이동/취소·글자 크기를 포함한다. 기존 18개 전투 시나리오도 통과했다. 초기 실행의 구 임무/버튼명/높이 기대를 새 동작에 맞췄다. 보행과 연출 소멸의 고정 대기 테스트 2개는 최대 1.5초 동안 실제 프레임/파괴 완료를 기다리게 했으며 동일한 최종 단언을 유지했다. 테스트 0개로 끝난 필터 시도는 통과 수에 포함하지 않는다. SystemUi/editmode.json, playmode.json. ManagedChecks는 이번에 재실행하지 않았다.
- 최종 런타임·에디터·EditMode/PlayMode C#의 실제 Unity API DLL 컴파일도 모두 통과했다(SystemUi/api-check.log). 동시 진행된 개인 BGM 작업은 별도 커밋33b2b35로 포함되며 해당 작업의 관련 PlayMode3개/로컬 로딩 증거는 [PersonalMusic/README.md](PersonalMusic/README.md)를 따른다. 사용자 제공 MP3는 Assets·Git·빌드에 포함하지 않는다.
- 실제 Game View **1024×768 / 1280×800 / 1366×768 / 1920×1080 / 2560×1080**, 기술 목록/대상 예측 10조합에서 활성 버튼 화면 경계 이탈0건. 대표3개 해상도 PNG를 직접 확인했다. 프레임9988→23187 진행을 기록했고 130% 글자 메뉴와 실제 EnemyTurn으로 발생한 다오스 예고를 추가 캡처했다. 격리 저장·6장 Lv7/기술 숙련·근접 배치로 검수했다. 모든 화면 조합 또는 실제 저시력 사용자 평가를 뜻하지 않는다. SystemUi/aspect-matrix.json 및 PNG.
- Windows 일반 빌드 **38.877초/오류0/경고10**, 개발 빌드 **43.486초/오류0/경고12** 성공. 기존 Pipeline 설정/개발 전처리기/폐기 API/셰이더 경고와 런타임 전용 선택 유닛 필드가 직렬화되지 않는다는 경고가 있다. SystemUi/windows-build.json, development-build.json, build-hashes.json. 일반 빌드는142초 동안 프로세스 정상 유지·시작 로그 예외0건(startup-check.json, windows-startup.log).
- 새 개발 플레이어7회 독립 실행 모두 통과: **1~6장 승리22/9/19/12/26/4플레이어턴**, 재실행 정상 패배2턴·재출전/저장 복원/중복 보상 보호. PlayerReviews/d1ec10ec56e940158c198e2a33b66327-summary.json 및 각 장 보고서. 기본 SPD+역할 적 AI, 기존4배 시간 배율이며 비전멸 임무의 검수용 아군은 목표를 고려하는 기술 선택을 사용한다. 사람의 전투 시간·난이도 판단이 아니다. 숨김 캡처는 시각 증거로 사용하지 않았고 로그7개 예외/오류0건(player-log-check.json).
- 사용자 campaign.json/.bak/.v1.bak의 SHA256은 작업 전후 불변이며 설정·슬롯2/3 파일을 생성하지 않았다(SystemUi/user-save-check.json). 검사 캡처 에셋은 Temp로 옮기고 동적 폰트 캐시·TimeManager 직렬화 변경을 복원했다. Editor Play 종료, Full HD, runInBackground=false, Scene dirty=false를 확인했다(editor-restored.json). 작업에서 실행한 일반 플레이어만 종료했다.
- **최신 실행 파일: Builds/WindowsSystemUi/TalesTactics.exe**. 폴더 전체가 필요하다. Computer Use의 일반 Windows 창 캡처는 검은 화면이며 창 활성화도 복구 재시도에서 실패했다. 따라서 이번 시각 검수는 Editor Game View 범위이고 일반 플레이어의 물리 입력/화면 검수는 성공으로 표시하지 않는다. 물리 게임패드·사람의6장 완주/장기 밸런스도 미검증이다. 새 공개 Release 업로드는 이번 변경에 포함하지 않는다.

## 2026-10-08 — 0.1.0-preview.2 공개 배포

- 사용자의 일괄 배포 요청으로 검증된 WindowsSystemUi 일반 빌드를 공개 GitHub prerelease **[v0.1.0-preview.2](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.2)**에 게시했다. 게임 소스/태그5696f6e, 기존preview.1은 유지한다. ZIP199,339,510바이트·202개 파일+manifest, SHA256 `b34addd6fd52238d3dcde21c4fb6f1cd506b9dbd85ea994fafcb254bf0746fa7`. ZIP과 체크섬2개 첨부, draft=false/prerelease=true. 개인 음원·사용자 저장·개발 검수 코드는 포함하지 않는다.
- 게임 코드·콘텐츠는 변경하지 않았고 기존 실제 Unity125/87 및 Windows6장·재실행 검증을 사용했다. 배포 도구 **13/13(4.406초)** 재통과. 기존 EXE/Runtime DLL 해시·빌드 보고서 파일 목록과 크기를 대조하고 ZIP 내부 해시 및 Windows PowerShell5.1 압축 해제본202개 파일 검사를 통과했다. 기존 보고서 형식 변환은 동일 값을 감싼 것이며 새 빌드로 계산하지 않는다.
- 압축 해제 실행 파일109초 유지·로그 오류/예외0건. 일부 사용하지 않는 URP 후처리 셰이더 제거 경고는 남는다. Computer Use 캡처/창 활성화 복구 실패로 일반 Windows 화면/물리 입력 검수는 완료로 계산하지 않았다. 검사에서 실행한 프로세스만 종료하고 실행 뒤에도 배포 파일 무결성을 재확인했다.
- 사용자 저장3개를 Builds/SaveBackups의 새 폴더에 백업하여 무결성을 확인하고 실행 전후 원본 SHA256 불변을 확인했다. 저장 내용은 Git/Release에 올리지 않았다. 게시 후 **인증 없이 ZIP 전체·체크섬을 다운로드하여 HTTP200·크기·SHA256 일치**, 공개 태그의 소스 커밋 일치를 확인했다.
- 세부 증거와 재현 범위는 [Distribution/Preview2/README.md](Distribution/Preview2/README.md). 사람의 난이도·실물 패드·다른 PC/DPI/전체화면·최종 작화/청음은 미검증으로 유지한다. 설치 프로그램·서명·온라인 업데이트·스토어 등록은 별도다. 공개 테스트 배포 완료가 미검증 항목의 완료를 뜻하지 않는다.

## 2026-10-08 — 전신 모델 출전 편성

- 승인된2안의 5×2 전신 모델 선택과 남색·황동·청록 색상,10인 초상화 아틀라스·유적 배경을 적용했다. 모델 조회/출전 토글, 금색 현재 선택/청록 출전 표시, 병과 필터·정렬·빈 목록·추천/해제·장비 슬롯·능력 모달·성장·임무 선택을 기존 시스템에 연결했다. 캐릭터 Content/Scene/전투 규칙/저장 형식은 유지한다. 조회 시 없는 성장 기록을 생성하지 않는다.
- 실제 Unity 전체 PlayMode **90/90(184.15초)**, 최종 레이아웃 보정 후 관련 **3/3(4.04초)** 통과. 첫88/90에서 발견한 구 준비 패널 높이 기대와 조회 시 성장 기록 추가를 수정했고, 빈 저장 조회 불변 단언을 추가했다. 실제 포인터 Raycast/Submit,6인 제한,정렬 뒤 초상화 ID 일치,장비·특성 수치,모달 차단,기존 관리/출전을 확인했다. ModelFormation/playmode-results.json 및 final-formation-tests.json. EditMode125는 직전 통과 기록이며 이번에는 재실행하지 않았다. ManagedChecks도 실행하지 않았다.
- Runtime/Editor/EditMode/PlayMode의 실제 Unity API DLL 컴파일 통과. 최종 Game View1024×768/1280×800/1366×768/1920×1080/2560×1080과 글자100/130%의10조합에서 각40개 활성 버튼 화면 이탈0건·활성 문구 넘침0건. 초기1280×800의 이름/레벨 높이는 고정 하단 영역으로 수정했다. 프레임2293→18539 진행 및 대표3해상도 PNG를 직접 확인했다. ModelFormation/aspect-matrix.json, api-check.log와 PNG.
- Windows 일반 빌드 **30.078초/오류0/경고10** 성공. 기존 Pipeline·개발 전처리기·직렬화·셰이더/TMP 경고가 남는다. 새 실행 파일12초 숨김 시작에서 프로세스 정상 유지·로그 오류/예외0건. ModelFormation/windows-build.json, startup-check.json, windows-startup.log, build-hashes.json.
- 사용자 저장3개 SHA256은 전후 불변이며 추가 슬롯/설정 파일을 생성하지 않았다. 검수는 별도 임시 저장 경로를 사용했고 Play 종료,Full HD,runInBackground=false,Scene dirty=false를 복구했다. 캡처 에셋·동적 폰트 캐시·자동 검사 CSV 임시 변경을 정리했다.
- **최신 실행 파일: Builds/WindowsModelFormation/TalesTactics.exe**. 폴더 전체가 필요하다. 시각 증거는 Editor Game View이고 Windows 일반 플레이어는 시작 로그 검사다. 실물 패드·다른PC/DPI·사람의6장 완주/최종 작화 감수는 미검증으로 유지한다. 새 공개 Release 업로드는 포함하지 않는다. 상세 조작/증거는 [ModelFormation/README.md](ModelFormation/README.md).

## 2026-10-08 — 인터미션 메뉴 개편

- 승인 시안의 장비 관리·상점·성장/승급 및 구매 후 장착 대상 화면을 같은 남색·청록·황동 uGUI로 적용했다. ID 기반 장비 아이콘 아틀라스,공통 메뉴/초상,장비9개 수치 비교,6개 상품 카드/페이지,수량·거래 후 잔액,실제 승급 직업·조건·비교,특성 선택 후 적용/저장을 연결했다. 기존 Content/Scene/저장 형식과 거래·승급 규칙을 유지한다. 단순 조회는 성장 기록을 만들지 않으며,특성 저장 실패 시 이번에 추가한 새 기록도 되돌린다.
- 실제 Unity 전체 PlayMode **93/93(188.74초)**,EditMode **125/125(0.71초)** 통과. UI 최종 배치 수정 뒤 인터미션 관련 **3/3(3.75초)** 재통과. 새 테스트는 실제 포인터/Submit 장비 초안·취소·저장 실패·재시도,없는 성장 기록 조회 불변/특성 초안·적용·실패 복원,상점2페이지 거래 유지 및 모달 차단을 포함한다. 기존 특성 테스트는 선택→적용 경로로 갱신했다. 최종4개 어셈블리의 Unity 실제 API DLL 컴파일도 통과했다. ManagedChecks는 이번 변경에서 실행하지 않았다. Intermission/playmode-results.json,editmode-results.json,final-intermission-tests.json,api-check.log.
- Game View **5해상도×글자100/130%×장비/상점/성장/장착 대상 =40조합**에서 활성 버튼 경계 이탈0건·활성 문구 넘침0건. 1024×768/1280×800/1366×768/1920×1080/2560×1080을 포함하며 프레임6040→82516 진행을 기록했다. 대표3해상도 PNG를 직접 확인했다. 첫 검수에서 발견한 이미지 FitInParent 컨테이너 이탈,초상 이름표와 승급 비교표/호위 설명 높이를 수정한 뒤 최종40조합을 재검증했다. 별도 임시 저장 루트·메모리 콜백을 사용하며 검수용 Lv19/골드1280은 사용자 데이터가 아니다.
- Windows 일반 빌드 **25.210초/오류0/경고10** 성공. 기존 Pipeline·개발 전처리기·직렬화·셰이더/TMP 경고가 남는다. 최신 실행 파일은 **Builds/WindowsIntermission/TalesTactics.exe**이며 폴더 전체가 필요하다. Intermission/windows-build.json.
- 실제 화면은 Editor Game View 검수다. Windows 일반 플레이어의 물리 입력/화면,실물 게임패드·다른PC/DPI·사람의6장 완주/장기 밸런스는 이번 범위에 포함하지 않는다. 공개 Release 업로드는 포함하지 않는다. 상세 조작·스크린샷·아트 제작 프롬프트는 [Intermission/README.md](Intermission/README.md)를 따른다.
- 새 실행 파일12초 숨김 시작에서 프로세스 유지·로그 오류/예외0건을 확인했다(Intermission/startup-check.json,windows-startup.log,build-hashes.json). 사용자 저장3개 SHA256은 전후 불변이며 추가 슬롯/설정을 생성하지 않았다(user-save-check.json). 검사에서 실행한 플레이어만 종료하고 Play/Full HD/runInBackground=false/Scene dirty=false를 복구했다. 캡처 임시 에셋·동적 폰트 캐시·자동 검사 CSV는 정리했다.
