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
- [x] 엔진 독립 규칙 테스트 30개 통과, 자동 전투 종료 확인.
- [x] 재실행 도구 및 후속 세션 문서.

## IN PROGRESS — 외부 환경에 막힘

- [ ] Unity AssetDatabase 최초 import 및 실제 TestBattle/Content 생성.
- [ ] Unity Test Runner 실행, Play Mode, 화면/입력/셰이더/TMP 검증.
- [ ] Windows 플레이어 빌드 및 실제 승리/패배 플레이테스트.

차단 근거: work/create.log 및 work/import.log의 `Connection to channel LicenseClient-USER-PC refused`. 캐시 쓰기 권한은 승인받았지만 재시도에서도 IPC 연결 거부. 현재 산출물에 생성 완료된 TestBattle Scene 또는 실행 파일은 없고 생성 코드가 있다. 전체 vertical slice 완료로 표시하지 않는다.

## NEXT

1. 사용자 계정의 정상 Unity Hub/Editor에서 이 프로젝트를 열어 초기화 실행.
2. Console 오류 수정 → EditMode 테스트 → Play Mode에서 출전/이동/취소/공격/스킬/AI/승패/재시작 검수.
3. Inspector 생성 참조, TMP Essentials/한국어 폰트, URP Sprite 셰이더 확인.
4. Windows 빌드 → 실제 화면/입력 검증. 결과를 Docs/VALIDATION.md 갱신.
5. 생성된 .meta 포함 로컬 버전관리 도입 및 최초 커밋. 원격 저장소는 별도 지정 시 연결.

## LATER

- [ ] 공식 레퍼런스 기반 10명 최종 캐릭터 아트. 파라/나탈리아 우선 검수.
- [ ] 4방향 × Idle/Walk/Attack/Skill/Cast/Guard/Damage/Dead/Ultimate 및 전용 VFX.
- [ ] 제공 음원/BGM 연결, 정교한 타이밍 연출, 히트 피드백.
- [ ] 스킬별 직선/콘/고저차/시야/속성 저항, 상세 툴팁과 밸런스.
- [ ] 장비 교체 UI, 캠페인/승급 이벤트, 세이브 마이그레이션.
- [ ] CT 스케줄러/Utility AI/보스·도착·호위·생존 승리 조건 구현체.
- [ ] 카메라 회전, 게임패드, 배포용 한국어 폰트.
