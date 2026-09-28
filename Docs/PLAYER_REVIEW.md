# Windows 캠페인 자동 검수

`CampaignPlayerReview`는 개발 빌드에서만 명시적으로 실행되는 검수 도구다. 일반 배포 빌드에는 포함되지 않는다. Scene/Content를 재생성하거나 캐릭터 수치를 변경하지 않는다.

## 재실행

1. 실행 중인 Unity에서 `TestBattle`이 저장되어 있는지 확인한다.
2. Unity CLI로 별도의 개발 빌드를 만든다.

```powershell
unity command build --project-path C:/dev/TalesTactics --caller plugin --skill unity-cli --target StandaloneWindows64 --outputPath Builds/CampaignReview/TalesTactics.exe --options '["Development"]' --scenes '["Assets/TalesTactics/Scenes/TestBattle.unity"]' --confirm true --format json
unity command build_status --project-path C:/dev/TalesTactics --caller plugin --skill unity-cli --format json
```

3. `build_status`에서 `completed` / `Succeeded`를 확인한 뒤 실행한다.

```powershell
./Tools/RunCampaignReview.ps1
```

## 검수 범위

- 일곱 개의 **서로 다른 Windows 플레이어 프로세스**를 순차 실행한다.
- 1차: 신규 캠페인에서 상점 UI로 가죽 갑옷 구매·크레스 장착 후 2장 잠금, 6명 Lv1 출전, 정상 이동/일반 공격/방향 선택으로 1장 승리, 보상 저장.
- 2차: 격리 파일 재로드, 2장 UI 선택, 적 Lv3 전투 승리, 두 장 완료 및 출전 전원 Lv3/EXP0 확인.
- 3차: 3장 UI 선택, 적 Lv4 전투 승리, 세 장 완료 및 출전 전원 Lv4/EXP0 확인.
- 4~6차: 각 프로세스에서 다음 장 목록을 거쳐4~6장 선택·정상 전투·전후 이야기·보상 저장. 최종 Lv9/EXP0,1990G(가죽 갑옷 구매 차감), 수호의 메달2개/단련 갑옷1개를 확인.
- 7차: 저장 파일 재로드와 완료 UI, 민트 1명으로 대기하여 정상 적 공격으로 패배, 저장 불변 및 재출전 HP 회복 확인.
- 직접 HP를 줄이거나 완료 플래그·성장치를 주입하지 않는다. 기본 공격 전술은 기존 `EnemyPlanner`를 재사용하고 아군의 명령은 UI submit 이벤트 및 타일 선택 상태를 거친다.
- 검수는 4배 시간 배율에서 실행되며, 단계당 300초/플레이어 200턴 제한과 외부 프로세스 330초 제한을 둔다.
- `CampaignFile`에 새 GUID 경로를 주고 `BattleDirector.PersistCampaign`에 연결한다. 실제 `campaign.json`과 `.bak`는 실행 전후 SHA256으로 불변을 확인한다.
- 검수 슬롯은 `persistentDataPath/Reviews/<GUID>/campaign.json`에 보존한다. 결과 JSON은 `Docs/PlayerReviews`에 복사한다.
- 예외·오류 로그, 정상 종료 코드, 기대 결과를 모두 검사한다. 각 요약은 플레이어와 Runtime DLL의 SHA256을 포함한다.

## 해석상의 한계

자동 이벤트 검수는 물리 마우스/키보드 입력, 모든 스킬, 수동 전투 완주 또는 일반 배포 빌드의 시각 검수를 대체하지 않는다. 시작 시 일반 저장 파일을 읽는 기존 코드는 유지하며, 검수 시작 후 별도 `CampaignFile`로 교체한다. 상점 구매·장비 적용도 격리된 PersistCampaign 콜백을 사용한다. 재실행 시 구매 수량·장착 ID·정확한 골드(최종 1990G)를 추가 확인한다. 승급·타이밍 설정 버튼은 누르지 않는다.

숨김 실행 환경에서 화면 캡처가 검게 나올 수 있다. 이 경우 이미지 파일을 만들지 않고 `captureWarnings`에 기록하며, 자동 전투·저장 결과와 시각 검수 여부를 구분한다. 픽셀이 있는 캡처도 자동으로 시각 검수 통과로 간주하지 않는다.

장비 보상 검수: 각 장 완료 후 생명의 부적/철검을 각각 1개와 3장 강화 갑옷 보상을 소유하는지 실제 파일에서 확인하고 프로세스 재실행 뒤에도 같은 수량을 검사한다.

전용 맵/이야기 검수: 캠페인 시작 버튼 이후 도입 대사를 끝까지 진행한다. 1~6장 전용 맵에서 정상 전투로 승리하고 후일담을 읽은 뒤 저장 바이트 불변을 확인한다.

CT+Utility 회귀는 같은 개발 빌드에서 ./Tools/RunCampaignReview.ps1 -Tactical 로 실행한다. 적과 검수용 아군이 UtilityPlanner를 사용하며 동일한 격리 저장 검증을 수행한다. 기본 실행은 기존 SPD/기본 AI를 유지한다.

경제 검수에서는 추첨값2500을 주입하여 1장 가죽 갑옷/2·3장 강화 갑옷의 추가 드롭을 보장하고 실제 파일 재실행 수량을 검사한다. 3장 종료 시 강화 갑옷은3개,6장 종료 시6개(확정1+2~6장 추가5개)다. 게임 기본 RNG는 변경하지 않으며 25%/15%/60% 구간 분포는 EditMode에서 전수 검사한다.
