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
