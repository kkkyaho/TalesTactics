# TalesTactics — 개발 시작 안내

Unity 6000.6.0f1 / Windows / URP 17.6.0 기반 2.5D Tactical RPG 프로젝트입니다.

## 현재 상태

전투 vertical slice의 코드, 캐릭터 데이터 생성기, Scene 생성기와 검증 도구를 구현했습니다. **Unity 라이선스 IPC가 이 실행 환경에서 거부되어 TestBattle Scene의 실제 생성, Play Mode, Windows 빌드는 아직 검증하지 못했습니다.** 플레이 가능한 완성 빌드로 간주하지 마세요.

- 런타임·에디터·테스트 C#을 설치된 Unity 실제 API DLL로 각각 컴파일: 통과.
- 엔진 독립 규칙 테스트 30개: 통과. Unity API 일부를 대체한 별도 .NET 테스트입니다.
- 다른 게임 프로젝트는 수정하지 않았습니다. API 검사에 이미 설치된 동일 uGUI/InputSystem DLL만 읽었습니다.

## 실행

1. Unity Hub에서 이 README가 있는 `TalesTactics` 폴더를 기존 프로젝트로 추가합니다.
2. 설치된 **6000.6.0f1**으로 엽니다. 패키지 다운로드와 최초 import를 기다립니다.
3. 초기화 코드가 `Assets/TalesTactics/Scenes/TestBattle.unity`와 `Content`의 실제 ScriptableObject를 생성합니다. 자동 생성되지 않으면 상단 **Tales Tactics → Create Test Battle**을 실행합니다.
4. Play를 눌러 출전 화면에서 1–6명을 선택하고 **전투 시작**을 누릅니다. 기본 선택은 크레스·민트·파라, 적은 4명입니다.
5. 모든 일반 스킬은 **훈련: Lv25** 모드로 확인합니다. 일반 모드는 Lv1부터 저장된 성장치를 사용합니다.

`Launch.ps1`은 설치된 에디터에서 프로젝트를 여는 선택적 편의 스크립트입니다. 테스트 Scene은 기존 사용자 작업을 자동으로 덮어쓰지 않습니다. 생성 이후 수치 변경은 Content의 asset을 편집하세요. 생성기 코드는 초기 데이터의 출처이며 기획 원본은 Docs/GAME_DESIGN.md입니다.

## 조작

- Move → 파란 타일 클릭: 경로를 따라 이동. Undo Move로 원위치 복귀.
- Attack 또는 Skill → 타일 선택 → 피해/효과 Preview → 실행.
- 이동과 행동 순서는 자유. 공격 후 이동도 가능. 이동 뒤 행동하면 이동 취소가 잠깁니다.
- Wait 또는 Guard → Front/Back/Left/Right 선택 → 턴 종료.
- 파라의 사자전후: 모든 일반 기술 해금, MP50 이상일 때 회전 진행률 40–75%에서 Space 또는 버튼 입력. 성공하면 MP50의 사후폭쇄진, 실패하면 사자전후만 실행. 자동 입력 옵션 제공.
- ESC: 선택 취소. 진행 중인 연출·적 턴은 취소하지 않습니다.
- 승패 화면에서 Restart → 출전 화면. KO는 다음 전투에서 회복됩니다.

## 확장 지점

- CharacterData: 초상화, Front/Back/Left/Right Sprite, Animator, 능력치/성장, 스킬, 승급, 테마.
- Animator를 연결할 경우 Int 파라미터 `Facing`, `Action`을 Definitions.cs enum 값에 맞춥니다.
- SkillData: MP/HP/게이지, 사거리, 범위, 속성, 대상, 복수 효과, 해금 레벨, 연계 조건.
- AudioLibrary: battle/victory/boss/story 및 10명 테마 ID. 실제 음원은 제공된 파일을 연결합니다.
- 저장: Unity persistentDataPath 아래 campaign.json, 교체 시 .bak 보관. 훈련 모드에서는 경험치를 저장하지 않습니다.
- 최종 한국어 폰트는 BattleHud.Font에 지정하세요. 현재는 Windows 맑은 고딕을 실행 시 읽으며 폰트 파일은 포함하지 않습니다.

## 검증 명령

- 엔진 독립: PowerShell에서 `./Tools/ManagedChecks/Run.ps1`
- Unity: Window → General → Test Runner → EditMode → Run All.
- Scene 검증 후 빌드: Tales Tactics → Build Windows Player.
- 정적 API 검사: 최초 Unity import 후 `./Tools/CompileApi.ps1`.

구현 구조, 현재 한계와 다음 작업은 Docs/ARCHITECTURE.md 및 Docs/TODO.md를 확인하세요.
