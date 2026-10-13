# 확장 구현 작업 기록

2026-10-13, `codex/crossover-campaign`, 기준 develop `172b7fd`. 이 문서는 출시 완료 기록이 아니다. `CrossoverReady`는 true로 활성화했고 실제 EditMode333/333·전체 PlayMode132/132가 통과했다. 사용자가 일반 Windows 실행본을 연 뒤 현재까지의 작업을 main에 머지하도록 명시적으로 요청했다. 직접 입력 검수 미완료를 유지한 소스 스냅샷으로 커밋·develop 통합·main 반영·푸시한다. 공개 패키지 배포는 포함하지 않는다.

현재 남은 차단: Windows 검수 플레이어 위의 보안 네트워크 허용 창을 사용자가 직접 취소해야 한다. Computer Use guidance의 보안 권한 요청 조작 금지 때문에 대신 누르지 않았으며 비개발 검수 빌드에서도 창이 남아 있다. 비동기 질문으로 사용자의 취소 조작을 요청했다. 그동안 최종 일반/개발 빌드(각 오류0/경고3), 실제20전투 재시뮬레이션20승리, API 참조 컴파일4개와 문서 정리를 마쳤다. 아래 항목은 각 시점의 이력이며 현재 상태는 이 문단과 README를 우선한다.

재개 지점: `Docs/CrossoverStatus/manual-review.json`의 비개발 검수 플레이어 PID133448/윈도우1575148(현재 반환값이며 다시 열거할 것). 보안창 해제 후 새 메인1화 출전/대사/이동·취소/공격예측/중단·이어하기/재도전/동료 이야기 진입을 직접 확인한다. `--manual-review` GUID는 bb20f8583075428b8ceecf4e44067300이고 F12 캡처는 그 독립 저장 폴더에 생성된다. 원래 Unity PID88888, 원래 게임창1441924는 건드리지 않는다. 최종 검증/빌드 로그는 `editmode-enabled.xml`333/333, `playmode-enabled.xml`132/132, `build-release-final.log`, `build-development-final.log`. `CrossoverReview`는 일반 빌드 옵션에 TALESTACTICS_REVIEW만 더한 검수 실행본이며 공개용 일반 빌드는 이 도구를 포함하지 않는다. UI 완료 후 사용자 저장 해시를 다시 비교하고 문서의 미완료 표기를 갱신한다.

원격 develop을 재조회했고 기준172b7fd와 동일했다. 원래 C:/dev/TalesTactics는 develop이며 사용자 `.codex-remote-attachments/`만 untracked다. 실행 중 원래 작업공간을 변경하지 않도록 최종 검증 후 현재 브랜치 커밋과 원격 develop의 fast-forward 반영을 검토한다. 이후 사용자 명시 요청에 따라 main도 develop의 현재 구현으로 fast-forward 반영한다. 생성 탈락 PNG6종은 D:/TalesTacticsCrossover/d3d2/RejectedArt에 보존했고 생산용35종 알파 검사는 모두 통과했다. Unity가 자동 변경한 설정/동적 폰트/기존 테스트 CSV는 원복했다.

## 현재 구현

- 기존 0–5장 인덱스를 보존하고 메인 6–25, 개인 이야기 26–55를 추가했다. 메인 20화 전후 대사, 합류 조건, 개인 30개 단계와 전용 무기 10개 보상 경로가 있다.
- 주요 보스 6종의 민트 타임 스톱은 준비 취소/기절 1회, 실제 행동 2회 저항 규칙을 사용한다. 중단 저장 v11, 전투 시작 상태 v6, 되돌리기·재도전에 상태를 포함했다.
- 전용 무기의 소유자 제한, 10개 전용 효과, 예측과 실행, 중복 보상 방지 검사를 추가했다.
- 보스 20종의 기술 60개를 `CrossoverSkillRules`로 작성했고 `CrossoverContentInstaller.InstallBatch`로 실제 Content에 추가했다. 기존 아군 초기 에셋을 재생성하지 않았다. 추가 에셋 107개(캐릭터/일반 공격/기술/무기/장치), 카탈로그 활성화는 보류 중이다.
- 예고→발동→빈틈, HP 단계 보호, 소환진 점령·상한, 가나벨트 분신 취소, 오르도 예고 범위 고정, 장치 복구, 고지대/빛 장치 보호, 지속 위험 지역과 구조/출구 표시를 연결 중이다.

## 실제 검증 증거

최신 추가 결과: `editmode-collapse.xml` 및 `editmode-death.xml` 각각331/331. `playmode-graphical-2.xml`129/130에서 새 보스 쓰러짐 프레임 누락을 발견해21형태의4방향2프레임을 추가했다. `playmode-graphical-3.xml`130/130 통과(확장 다오스/골렘/초반 히아데스 포함). 이후 민간인 시각 표시와 검사를 추가했으므로 이 부분은 추가 검증 대상이다.

`balance-audit.csv` 재실행은20/20승리. 각 장 권장 진입 레벨의 독립 시뮬레이션이며 연속 캠페인 성장·사람의 UI 완주·모든 편성의 밸런스를 보증하지 않는다. 피해/자원 강제 회복이나 강제 처치 없이 실제 이동·기술·대기 명령을 사용했다.

증거 경로는 `D:/TalesTacticsCrossover/d3d2/Logs/`이다. API 컴파일은 실제 Unity 테스트와 구분한다.

- 초기 EditMode 215개 중 205 통과: 되돌리기 프레임의 v11 허용 누락 수정.
- 타임 스톱 시점 실제 EditMode `editmode-timestop.xml` 218/218.
- 실제 PlayMode `playmode-timestop.xml` 1/1. 이 결과는 전체 PlayMode 통과를 뜻하지 않는다.
- 통합 1차 `editmode-integration-1.xml` 223/229: 레거시 범위 기대값, 장애물/출발점 충돌, 신규 목표 enum의 훈련 경로 등을 수정.
- 통합 2차 `editmode-integration-2.xml` 240/240.
- 보스 패턴 `editmode-patterns-1.xml` 265/265. 이후 반격·Slow·경로 지뢰·Content 설치 변경은 이 결과에 포함되지 않는다.
- `editmode-fronts-trials.xml` 274/274. `editmode-content-1.xml` 329/329, `editmode-content-2.xml` 330/330. 실제 Content 60기술 일치, 40전투의 세 스케줄러 저장/되돌리기/재도전, 10명 시련의 실제 기술 조건을 확인했다. 이후 지연 붕괴 변경은 재검증 중이다.
- 전체 batch PlayMode `playmode-integration-1.xml` 100/127: WaitForEndOfFrame 미지원 등의 환경 실패와 레거시 범위 검사 실패가 있었다. 이를 전체 통과로 취급하지 않는다.
- 별도 그래픽 Editor `playmode-graphical-1.xml` 126/127. golem을 포함한 새 적의 공격5프레임에 기존4프레임 기대값을 적용한 검사1개가 실패했다. 이후 레거시 다오스/골렘 에셋을 원복하고 확장 전용 복사 에셋으로 분리했으며, 새 적과 기존 적의 프레임 기준을 구분했다. 새 씬 테스트3개 추가 후 전체 재검증 필요.
- `balance-audit-initial.csv`: 권장 진입 레벨, 실제 이동/기술/가드/상태 규칙을 사용하는 20화 시뮬레이션에서19승리,13화500턴 경과. 자동 조작기가 첫 선택 신호의 순서를 무시한 문제를 수정했다. 재실행 전이며, 이는 UI 조작 검증과 별개다.
- Runtime/Editor/EditMode/PlayMode 실제 API 참조 컴파일 여러 차례 통과. 최신 변경 후 재검증 필요.
- `install-crossover.log`: Unity batch 정상 종료, 60기술·10무기 설치 메시지 확인. 새 카탈로그는 아직 완성 아트 검증 전이다.

## 아트 상태

쓰러짐 시트6장(21형태×8프레임)과 민간인4종×4방향을 추가했다. 선택된 Unity rect는1708개이다. 쓰러짐 시트는32/32×5장, 인간형 히아데스8/8, 민간인16/16의 알파 연결/경계 검사를 통과했으며 이미지 원본 픽셀을 수정하지 않았다. `CrossoverCivilianView`는 신호에 연결된 비전투 피난민을 표시하고16화에서는 구조된 행렬을 선두 아군에 따라 이동시킨다.

`Art/Crossover`의 PNG는 생산용 후보이며, 알파/프레임 구조 통과만으로 방향/무기 손/동작 품질 통과를 주장하지 않는다. 콘셉트 보드를 잘라서 게임 모델로 쓰지 않았다.

- `Tools/AuditCrossoverArt.py`는 픽셀을 바꾸지 않고 구성요소·가장자리·프레임 수를 검사한다.
- 첫 artorius 70개 구성요소, 첫 demitel 90개로 제외. artorius-v2는 72개지만 후면 공격/손 방향 수정 필요.
- artorius-v3는 시각 검토에서 추가 행과 배경 주변 잔여 픽셀 등 결함이 보여 제외한다. importer에 연결하지 말 것.
- demitel-v2 재생성 완료, 새 원작/창작 보스 후보까지 후속 전체 검사 필요.
- `MeasureCrossoverArt.py`와 `CrossoverArtImporter` 실행 완료. 현재1524개 rect (20보스×72+초반 히아데스72+장치12). PNG 픽셀은 수정하지 않고 Unity Sprite rect/outline/pivot/방향 연결을 작성했다.
- 자미르의 방향쌍 배열과 가나벨트의 왼쪽/정면/오른쪽/후면 배열을 실제 시트에 맞춰 교정했다. 다오스 왼쪽, 시구레/볼란 후면은 별도18포즈를 생성해 연결했다. 잘못된 손/방향 프레임은 `ApprovedPose`에서 제외·대체했다. 볼란 첫 후면 후보는17개 연결 실루엣이어서 제외, v2는18개 통과.
- 초반 히아데스는 인간형 별도 에셋이며3화에서는 Spark Wave만 사용한다. 16화 변이형과 모습/사용 기술을 구분한다. 초기 골렘은 붕괴 기술을 사용하지 않는다.
- 이번 시각 검토에서20종 전체 시트를 확인했으나 실제 전장/카메라/애니메이션 연속 검수는 남아 있다. raw PNG의 알파0에 남은 RGB가 view_image에 색으로 보일 수 있으므로 불투명 배경으로 오판하지 말 것.

## 반드시 남은 작업

1. 아르토리우스 왼손/후면 등 아트 결함 교정, 20종 전체 시각 검사, 스프라이트 경계·발 피벗·4방향·모션 연결. 장치의 실제 표현도 필요하다.
2. 11화 실제 부대 대기/전환, 20개 서로 다른 지형, 캐릭터별10시련 조건은 구현했다. 구조/호위의 인물 표현과 장별 실제 플레이 검수를 마무리할 것. 계약은 서→동→북 신호와 남쪽 문양을 사용한다.
3. 신규 메커니즘과 Content 일치 검사. `RefreshStagedSkillsBatch`로 미공개60기술의 Slow/애니메이션 연결을 갱신했다. 마지막 파괴 장치 순서, 예약 부대 활성 턴 검증, 붕괴 지연 발동/잔해 이동비용을 추가했다. 최신 검증이 필요하다.
4. 원작 무기10종 출처는 `weapon-sources.json`에 기록했다(2차 공략 자료). 신규 설정/효과와 구분한다. 원작 음원 다운로드 금지.
5. 실제 Content를 사용하는 EditMode/전체 PlayMode, 신규 20화+30개 개인 단계의 실제 진행/보상/중단/재도전/되돌리기/옛 저장 호환 검증. 기존 테스트의 6장 전제는 레거시로 유지하고 신규 범위 검사를 별도로 둘 것.
6. Windows 일반/개발 빌드와 독립 저장 경로의 실제 UI 검수. `C:/dev/TalesTactics`의 실행 중 Unity/게임과 사용자 저장은 보존한다.
7. README/TODO/ARCHITECTURE/VALIDATION을 최종 사실대로 업데이트, Unity가 자동 변경한 ProjectSettings/폰트/URP 파일 diff 검토. 전체 완료·관련 검증 통과 후 develop 통합, 커밋 및 push. main 변경 금지.

현재 의미 있는 진척이 가능하며 외부 차단은 없다.
