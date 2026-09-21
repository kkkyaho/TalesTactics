# 검증 기록 — 2026-09-22

## 통과

1. Unity 6000.6.0f1 엔진 DLL, 설치된 동일 uGUI 2.6.0 및 InputSystem 어셈블리에 대해 C# API 컴파일.
   - TalesTactics.Runtime: PASS
   - TalesTactics.Editor: PASS
   - TalesTactics.Tests: PASS
   - Unity AssetDatabase 컴파일이 아니라 Roslyn 정적 API 호환성 확인이다.
   - 패키지 어셈블리는 기존 C:/dev/ProjectAbyss/ProjectAbyss/Library/ScriptAssemblies를 읽기만 했다. 해당 프로젝트는 변경하지 않았다.
2. 엔진 독립 NUnit 규칙 테스트: 30 PASS / 0 FAIL.
   - 가중치 이동/고저차/장애물·점유/경로/Root.
   - KO 스케줄러 제외, 행동 제한, 이동 취소, 행동 후 이동, 행동으로 이동 확정.
   - 비용 검증, SO 불변성, KO 점유 해제, 1회 자동 부활, 부활 충돌.
   - 정면/후면/가드, 컨슘 클로 자신의 3턴, 선행 공격 제한, HP 비용.
   - 스턴 기간, 회복 상한, Pull 점유 갱신, Cooldown, 성장/승급.
   - 자동 전투가 300턴 이내 실제 결과로 종료, 매 턴 점유 불변식 확인.
   - UnityShims.cs로 엔진 일부를 대체한 실행이다. Unity 콜백/렌더링/입력/파일 직렬화 검증을 의미하지 않는다.

실행 결과는 api-compile-results.txt와 managed-test-results.txt에 보관한다. 재현 도구는 Tools/CompileApi.ps1, Tools/ManagedChecks/Run.ps1.

## 차단된 검증

Unity 배치 실행을 두 번 시도했으나 라이선스 IPC가 거부되었다.

```
[Licensing::IpcConnector] Connection to channel LicenseClient-USER-PC refused
[Licensing::Module] Timed-out after 60.00s, waiting for channel: "LicenseClient-USER-PC"
```

첫 실행은 추가로 패키지 캐시 EPERM이 있었다. 사용자가 Unity 캐시 폴더 쓰기/네트워크 권한을 허용한 뒤 재시도에서도 라이선스 IPC가 거부되었다. 이 작업에서 띄운 두 번째 배치 프로세스만 종료했으며 기존 Unity 에디터들은 건드리지 않았다.

따라서 다음은 **미검증/미완료**다.

- Unity 최초 import 및 실제 Content ScriptableObject 생성.
- 실제 TestBattle Scene 생성 및 serialized reference 검증.
- Unity Test Runner, Play Mode, 화면·입력·한글 폰트·셰이더.
- 전투 UI를 통한 수동 승리/패배/Restart.
- Windows Player 빌드와 실행.

최종 에셋/Scene은 ProjectSetup의 첫 import 초기화 또는 메뉴로 생성한다. 기본 URP 템플릿 SampleScene이 있는 것은 TestBattle 생성 완료를 의미하지 않는다.

## 우선 수동 검수

1. Hub에서 프로젝트 열기 → 패키지 import → Console 오류 0 확인.
2. TestBattle 자동 생성 확인. 누락 시 Tales Tactics/Create Test Battle.
3. Play → 기본 3명 → 이동/취소/공격/힐/턴 종료.
4. 훈련 Lv25 → 벨벳/알펜/파라/키사라 및 부활/Pull 확인.
5. EditMode tests 실행 → Windows build → 실제 플레이 검수.

현재 사용된 데이터 수치는 초기 임시 밸런스이며 원작 아트/음악은 포함하지 않았다.
