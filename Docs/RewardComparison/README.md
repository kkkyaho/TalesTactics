# 획득 보상: 동료별 장비 비교

2026-11-18 / develop / 구현·자동 검증 완료 · 사람의 플레이 평가는 후속

## 구현 범위

결과의 보상 카드를 열면, 합류했으며 해당 장비를 사용할 수 있는 동료를 이전/다음 버튼으로 선택한다. 현재 장비와 획득 장비의 능력치 보너스·고유 효과, 동료의 HP·MP·공격·마력·방어·마방·민첩·이동·도약의 현재 값과 교체 후 값, 부호가 있는 변화량을 표시한다. 같은 장비를 이미 착용한 경우를 명시한다.

현재 저장된 레벨·승급·특성·성장 방향·다른 슬롯을 포함한 `EquipmentLoadout.Preview`로 계산하며, 비교에는 독립적인 장비 슬롯만 사용한다. 저장 객체·인벤토리·인터미션 장비 초안을 변경하지 않는다. 실제 장착은 기존 인터미션의 장비 관리에서 한다. 결과의 계속 버튼은 상세창이 열린 동안 비활성이고, 필수 후일담 흐름은 유지한다.

닫기와 Esc/패드 취소 모두 보상 카드로 선택을 돌려놓는다. 기존 `GamepadPointer.FocusButton`을 사용해 활성 패드의 화면 커서도 함께 이동한다. 상세창의 최종 레이아웃이 적용된 다음 프레임에 선택 상태를 확인하고 포커스를 다시 맞춰 커서 위치를 유지한다. 호환 동료가 1명 이하이면 전환 버튼을 비활성화한다. 기존 효과 설명과 호환 동료 이름 텍스트는 보존했다.

## 현재 검증 상태

실제 Unity Editor에서 `RewardComparison` PlayMode 3개를 세 화면 크기로 실행했고 모두 통과했다. 글자 배율 1.0/1.3에서 동료 순환·현재 장비/효과·특성/성장 반영·능력치 하락·동일 장비·단일/빈 호환 목록·저장 및 저장 호출 불변·키보드/패드·필수 후일담을 검사한다.

| Editor GameView 실제 크기 | 결과 | 소요 시간 | 결과 원문 |
| --- | --- | --- | --- |
| 1280×720 | 3/3 통과 | 31.49초 | [720p 재검](../CommercialSlice/Validation/ui-rewardcomparison-720p-attempt4.json) |
| 1024×768 | 3/3 통과 | 17.33초 | [768p 재검](../CommercialSlice/Validation/ui-rewardcomparison-768p-attempt4.json) |
| 1920×1080 | 3/3 통과 | 23.37초 | [1080p 재검](../CommercialSlice/Validation/ui-rewardcomparison-1080p-attempt4.json) |

세 실행의 capture manifest에서 실제 화면 크기와 글자 배율을 확인했고, 각 28장씩 총 84장 PNG가 존재한다. 이는 준비·승리·입력을 구성한 fixture에서 실제 결과/비교 UI를 검사한 근거다. 이 표적 검사만으로 자연 캠페인 완주·Windows 실행·모든 화면의 수동 시각 승인을 주장하지 않는다. Windows 자동 완주는 아래 별도 실행 결과로 구분한다. 각 manifest 경로와 SHA, 실패를 포함한 순서는 [검증 이력](../CommercialSlice/Validation/validation-history.json)에 남겼다.

초기 720p 검사는 커서 포커스가 403.713013px 벗어나 2/3이었다. 제품의 최종 레이아웃 이후 포커스 갱신을 수정하고 기존 2px 미만 조건을 유지한 재검에서 통과했다. 그 사이 attempt3은 실제 테스트 전에 도메인 재로드 호출이 5000ms에 시간 초과한 인프라 실패다. 명시적 재로드와 Editor 준비 상태 확인 후 attempt4가 성공했으며, 실행하지 못한 시도를 통과로 세지 않는다.

최종 UI 수정을 포함한 실제 Unity 전체 [PlayMode 297/297 · 1097.76초](../CommercialSlice/Validation/full-playmode-final.json)를 통과했다. 실패·건너뜀·미결정은 0이다. 별도 표적 검사 횟수를 전체 개수에 더하지 않는다. 통합 [EditMode 537/537 · 32.99초](../CommercialSlice/Validation/full-editmode-final.json)도 통과했으나 마지막 UI 포커스 수정 전의 결과다. 따라서 그 EditMode 실행이 이후 UI 수정까지 포괄한다고 해석하지 않으며, 최종 UI는 위 표적·전체 PlayMode 및 새 빌드로 확인했다.

새 일반 Windows 빌드는 [67.2941321초 · 오류 0 · 경고 3](../CommercialSlice/Validation/windows-build-normal.json), REVIEW 빌드는 [42.2166559초 · 오류 0 · 경고 6](../CommercialSlice/Validation/windows-build-review.json)으로 성공했다. 경고와 초기 빌드 실패 이력은 원문 및 [검증 이력](../CommercialSlice/Validation/validation-history.json)에 보존한다. 일반 실행본은 이번에 실행하지 않았으며, 일반 빌드 성공을 일반 실행본의 수동 플레이 완료로 표현하지 않는다.

새 REVIEW 실행본의 [실제 Windows 자동 캠페인](../CommercialSlice/Validation/windows-campaign-summary.json)은 **20/20화, 24,697검사, 저장 재로드 176회, PNG 48장, 저장 JSON 40개**를 완료했다. 오류 0, 정상 종료 코드 0, 강제 종료 없음이다. 원본은 `Logs/CommercialSlice20261117/CampaignWindows-5063e4b1da144f958d6611eaf9930305/`에 있다. 기존 RewardVariety 실행본의 결과를 재사용하지 않았다. 고정 자동 정책의 완주이며 사람이 20화를 플레이했거나 모든 PNG를 육안 승인했다는 뜻은 아니다.

전체 검증 이후 [보존 대조](../CommercialSlice/Validation/final-preservation.json)에서 작업 전 수정 파일 **12/12**와 사용자 루트의 저장·설정·백업 파일 **9/9**가 기존 해시와 일치했다. 전체 작업 중에는 `StorageTests` 아래 파일 130개·디렉터리 70개가 추가되고 기존 `TestResults.xml` 1개가 바뀌었으며, 기존 파일·디렉터리 제거는 없었다. 따라서 사용자 전체 트리가 불변이었다고 표현하지 않는다. 편집기는 PlayMode 종료, 원래 장면·설정 복원과 장면 dirty=false를 `Logs/CommercialSlice20261117/editor-final-state.json`에 확인했다.

## 초기 검증 이력

작업 초기에 Unity 6000.6.0f1의 Roslyn과 Unity 참조를 사용한 제품 및 PlayMode 소스 정적 컴파일은 각각 종료 코드 0이었고 경고 40개/7개를 보존했다. 당시 기록은 Editor 테스트·렌더 검증이 아니다. [당시 소스 SHA와 정적 검사](Validation/static-source-check.json).

초기 `Scene Backup Detected` 안내와 Pipeline 연결 장애, Computer Use Node 초기화 실패 두 번 때문에 실행 검증을 진행하지 못했던 시점이 있었다. 이후 Editor 응답을 복구하고 위 실제 검사를 수행했다. 따라서 그 초기 장애를 현재의 미실행 상태로 해석하지 않는다. 복구 사본이나 기존 장면을 삭제·덮어쓰기한 것으로 기록하지 않는다.

작업 시작 때 수정돼 있던 12개 파일의 바이트 사본과 SHA256은 `Logs/RewardComparison20261117/protected/` 및 `protected.json`에 보존했고 사용자 데이터의 작업 전 해시도 기록했다. 진행 중 감사와 위 전체 검증 이후의 보존 대조는 구분한다.

## 완료 범위와 후속 플레이

구현과 위 자동 테스트·빌드·Windows 자동 완주·보존 대조를 완료했다. 사람에게 비교 정보가 이해되는지, 실제 장비 선택에 도움이 되는지, 전투 난이도와 성장 선택이 재미있는지는 별도 플레이 평가가 필요하다. [10분 첫 진입과 후속 검수 안내](../CommercialSlice/PLAYTEST.md)에 따라 사용성을 확인한다. 자동 테스트 통과를 인간 대상 밸런스 평가 완료로 해석하지 않는다.

완료 변경은 `develop`의 로컬 커밋 범위다. 사용자 제공 음원과 그 이력이 포함돼 있으므로 일반 자동 푸시 지침보다 [로컬 음원 정책](../LocalMusicPolicy.md)이 우선하며, 원격 업로드·main 반영·공개 릴리즈는 하지 않는다.
