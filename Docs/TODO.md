# TODO

## DONE — 코드 구현 및 엔진 독립 확인

- [x] 새 작업 폴더 분석, 기존 코드 없음 확인, 원본 기획 보존.
- [x] Unity 6000.6.0f1 공식 URP 템플릿 파일 배치, 버전 고정.
- [x] Character/Skill/Rules/Equipment/Audio ScriptableObject와 Runtime 분리.
- [x] 10×9 테스트 맵, 고저차, Water 비용, 장애물, 점유, Dijkstra 경로.
- [x] 이동 1회/행동 1회, 양방향 순서, 경로 표시, 이동 취소.
- [x] SPD 개인 턴, 교체 가능한 Scheduler 및 승리 조건.
- [x] 상태별 클래스, 타겟/피해 미리보기, HP/MP/KO/부활.
- [x] 10명 및 68개 일반 기술/10개 궁극기 데이터 생성기.
- [x] 벨벳/파라/알펜/키사라 고유 규칙, 자동 부활, 끌어당김.
- [x] 적 4명 AI, 승패, Restart, 1–6명 출전 선택, Lv25 훈련 옵션.
- [x] Scene 생성기: 카메라, uGUI, EventSystem, Battle 시스템 참조 연결.
- [x] 성장/승급/장비/저장 기본 구조, 승리 EXP 및 파일 저장 연결.
- [x] Audio ID 및 Sprite/Animator 교체 구조.
- [x] 런타임/에디터/테스트 각각 실제 Unity API DLL 참조 컴파일 통과.
- [x] 엔진 독립 규칙 테스트 34개 통과, 자동 전투 종료 확인.
- [x] 재실행 도구 및 후속 세션 문서.

## 실행 검증 및 배포 기록

- [x] 사용자 Unity에서 TestBattle/Content/TMP 및 .meta 생성 확인.
- [x] GitHub main 최초 업로드 확인 (49d815a).
- [x] 초기화 참조 검증, TMP import 순서 및 한자 대체 폰트 보강.
- [x] 엔진 독립 테스트 34개와 실제 Unity API 어셈블리 4개 컴파일 통과.
- [x] 실제 Unity EditMode 34개 / PlayMode 5개 테스트 통과. 전장 viewport 보강 후 PlayMode 재실행 통과.
- [x] Windows 빌드, 출전·이동·취소·스킬 목록·턴 전환·Restart 수동 검수. 승패는 PlayMode 테스트에서 검증.
- [x] 결과 검토 및 검증 기록 정리. 커밋/푸시 여부는 Git main과 origin/main으로 확인.

CLI는 Windows 계정 ACL로 연결 불가. Computer Use의 node_repl + @oai/sky로 Unity 메뉴, Test Runner, Windows 빌드 및 플레이어를 직접 조작하는 경로를 확인했다. 브라우저용 cua API의 native 비활성화를 전체 Computer Use 제한으로 오해하지 않는다.

## LATER

- [ ] 공식 레퍼런스 기반 10명 최종 캐릭터 아트. 파라/나탈리아 우선 검수.
- [ ] 4방향 × Idle/Walk/Attack/Skill/Cast/Guard/Damage/Dead/Ultimate 및 전용 VFX.
- [ ] 제공 음원/BGM 연결, 정교한 타이밍 연출, 히트 피드백.
- [x] 스킬 상세 정보, 비용/사거리/효과/사용 불가 사유 및 복합 효과 미리보기.
- [ ] 스킬별 직선/콘/고저차/시야/속성 저항 및 밸런스.
- [x] 장비 3슬롯 교체 UI, 능력치 미리보기, 호환성 검사, 적용·저장 및 훈련 반영.
- [ ] 장비 수량·획득·상점, 캠페인/승급 이벤트, 세이브 마이그레이션.
- [ ] CT 스케줄러/Utility AI/보스·도착·호위·생존 승리 조건 구현체.
- [x] 카메라 회전/확대/축소/초기화 및 마우스 버튼 UI.
- [ ] 게임패드, 배포용 한국어 폰트.

- [ ] 실행 파일에서 전투 끝까지 수동 승리/패배, 장시간 메모리/FPS 검수.
- [x] 좁은 세로 창용 HUD 재배치 및 전장 확대 조작. PlayMode 6개와 Windows 빌드/마우스 검수 통과.
