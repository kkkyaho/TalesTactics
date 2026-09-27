# 6번 — 입력·배포

게임패드는 Unity Input System의 Gamepad 장치를 사용한다. Windows의 Xbox/XInput 형태 버튼 이름을 기본으로 설명하며 PlayStation 대응 위치는 A=×, B=○다. 실제 연결 장치에서 지원되는 매핑은 Unity Input System과 해당 드라이버에 따른다.

| 조작 | 기능 |
|---|---|
| 왼쪽 스틱 | 노란 화면 커서 이동. 메뉴 버튼과 전장 타일 모두 선택 가능 |
| 방향키 | 현재 화면의 활성화된 버튼을 앞/뒤로 순환. 왼쪽/위는 이전, 오른쪽/아래는 다음 |
| A / × | 커서 위치 선택. 파라 타이밍 중에는 위치와 무관하게 타이밍 입력 |
| B / ○ | 전투 선택 취소 또는 출전 준비 메뉴로 복귀. 실행 연출·적 턴·결과·이야기·타이밍은 취소하지 않음 |
| LB / RB | 전장 카메라 90도 회전 |
| 오른쪽 스틱 위/아래 | 확대/축소 |
| 오른쪽 스틱 누르기 | 카메라 초기화 |

상점·장비·승급·이야기·설정·승패 화면은 동일한 커서로 모든 기존 버튼을 사용할 수 있다. 이야기의 건너뛰기와 결과의 재출전은 해당 버튼을 선택한다. 버튼의 잠금/비용/저장 실패 처리는 기존 UI 경로를 그대로 사용한다. 게임패드 연결 해제 시 커서가 숨겨지며 마우스 입력은 계속 동작한다. 다시 패드를 조작하면 복귀한다. 마우스 이동/클릭과 패드 조작 간에 자동으로 표시를 바꾼다. 진동·버튼 재설정·다인 로컬 입력은 이 범위에 포함하지 않는다.

## 포함 폰트

- Noto Sans CJK KR Regular, 버전 2.004, 원본 OTF 무수정.
- 공식 출처: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Korean
- SHA256: `6BCB2A0703AA137E874FC2DFFA85F6C21BA9A67FA329E81B8C801663AF7E992A`.
- 저작권: © 2014–2021 Adobe. SIL Open Font License 1.1.
- 라이선스 전문과 원본 폰트 메타데이터의 고지는 `Assets/StreamingAssets/Licenses`에 포함되어 Windows 빌드의 `TalesTactics_Data/StreamingAssets/Licenses`로 복사된다.
- 기본 글꼴: Resources/TalesTactics/Korean, 현재 소스/콘텐츠 문자 563개, 48pt/패딩5, 2048 정적 SDF 아틀라스 1개.
- 추가 문자: 같은 원본의 1024 동적 fallback, multi-atlas, Clear Dynamic Data On Build 활성화. 글꼴에 없는 문자는 지원되지 않는다.
- 사용자 지정 BattleHud.Font가 있으면 보존한다. 미지정 시 포함 폰트를 우선하며 Windows 맑은 고딕은 포함 에셋이 누락된 개발 환경의 비상 대체 경로다.

`Tools/ImportKoreanFont.cs.txt`는 Unity Editor API로 에셋을 생성하며 기존 결과를 덮어쓰지 않는다. 새 콘텐츠는 동적 fallback으로 표시할 수 있으며, 대규모 문자 추가 시 정적 아틀라스 갱신을 별도로 검토한다.

자동 장치 입력 검사는 InputSystem의 가상 Gamepad 이벤트를 사용한다. 실제 USB/Bluetooth 컨트롤러의 연결 상태와 기종별 드라이버를 직접 시험한 결과로 해석하지 않는다. 실제 검증 수치와 빌드 증거는 VALIDATION.md에 기록한다.
