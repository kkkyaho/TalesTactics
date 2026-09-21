# 프로젝트 목표

Unity를 사용해 파이널판타지 택틱스와 파랜드 택틱스 계열의 2.5D 턴제 Tactical RPG를 구현한다.

이 프로젝트는 단순한 기능 샘플이나 짧은 프로토타입이 아니라, 실제로 계속 확장 가능한 게임 프로젝트를 목표로 한다.

이번 작업에서 가장 중요한 원칙은 다음과 같다.

1. 한두 기능만 구현하고 작업을 멈추지 않는다.
2. 먼저 현재 프로젝트 전체를 분석하고 장기 TODO를 만든다.
3. 서로 의존성이 있는 작업을 묶어 가능한 범위까지 연속해서 진행한다.
4. 컴파일 오류나 실제 결정이 필요한 심각한 블로커가 없다면 다음 작업을 스스로 선택해서 계속 진행한다.
5. 매 단계마다 사용자 확인을 요구하지 않는다.
6. 코드만 만들어놓지 말고 실제 Unity Scene에서 테스트 가능한 상태까지 연결한다.
7. 임시 구현을 하더라도 이후 교체하기 쉬운 구조로 만든다.
8. 작업 종료 시 현재까지 구현한 내용, 테스트 결과, 남은 TODO를 명확하게 정리한다.

---

# 작업 진행 방식

이번 프로젝트에서는 짧은 작업 주기를 피한다.

예를 들어 다음처럼 하지 않는다.

* Grid 클래스 하나 작성 후 종료
* Unit 클래스 하나 작성 후 종료
* 버튼 하나 추가 후 종료
* 다음 작업 여부를 사용자에게 질문

대신 다음처럼 진행한다.

예:

Grid 데이터 구조
→ Grid 생성
→ 타일 선택
→ 이동 가능 범위 계산
→ Pathfinding
→ Unit 이동
→ 이동 테스트 Scene
→ 오류 수정

까지 하나의 연속 작업으로 처리한다.

가능하다면 한 세션에서 서로 연결된 여러 TODO를 완료한다.

작업 전에 반드시:

1. 현재 저장소/프로젝트 구조 분석
2. 이미 구현된 코드 확인
3. 기존 코드 재사용 가능 여부 확인
4. TODO 우선순위 결정

을 먼저 수행한다.

작업 중 이미 정상 동작하는 코드를 불필요하게 전면 재작성하지 않는다.

---

# 기술 방향

## 엔진

Unity 기반.

현재 프로젝트의 Unity 버전과 패키지를 먼저 확인하고 그 환경을 우선 사용한다.

임의로 대규모 버전 변경을 하지 않는다.

---

# 그래픽 구조

그래픽은 다음 구조로 한다.

* 2.5D Tactical RPG
* 3D Grid Map
* 2D Character Sprite
* Orthographic Camera
* 쿼터뷰
* 고저차 지원
* 캐릭터 4방향 표현
* 필요하면 카메라 회전은 향후 확장 가능하도록 설계

캐릭터 스프라이트 기본 방향:

* Front
* Back
* Left
* Right

기본 애니메이션:

* Idle
* Walk
* Attack
* Skill
* Cast
* Guard
* Damage
* Dead
* Ultimate

캐릭터 특성에 따라 전용 애니메이션을 추가한다.

---

# 캐릭터 디자인 원칙

이번 프로토타입에서 캐릭터 외형은 원작 게임의 캐릭터 디자인을 최대한 정확하게 재현하는 것을 목표로 한다.

임의로 재해석하거나 다른 스타일로 변경하지 않는다.

특히:

* 헤어스타일
* 머리색
* 얼굴 인상
* 의상
* 갑옷
* 장신구
* 무기
* 캐릭터별 대표 색상
* 실루엣

을 원작 공식 디자인 기준으로 맞춘다.

현재 대상 캐릭터:

1. 크레스 알베인

   * Tales of Phantasia

2. 민트 아드네이드

   * Tales of Phantasia

3. 벨벳 크라우

   * Tales of Berseria

4. 파라 엘스테드

   * Tales of Eternia

5. 티아 그란츠

   * Tales of the Abyss

6. 제이드 커티스

   * Tales of the Abyss

7. 나탈리아 루츠 키믈라스카 란발디아

   * Tales of the Abyss

8. 알펜

   * Tales of Arise

9. 시온 아이메리스

   * Tales of Arise

10. 키사라

* Tales of Arise

특히 이전 이미지 작업에서 파라와 나탈리아의 외형이 원작과 차이가 컸으므로 두 캐릭터는 더욱 주의한다.

캐릭터 디자인 구현 시 임의 디자인보다 공식 설정/제공된 레퍼런스 이미지를 우선한다.

실제 이미지 에셋이 아직 없다면:

* Placeholder Sprite를 사용할 수 있다.
* 단, CharacterData에는 최종 디자인 교체를 고려한 Sprite/Animator 참조 구조를 만들어둔다.
* 코드에 특정 이미지 파일명을 하드코딩하지 않는다.

---

# 음악 / 사운드 방향

음악 역시 각 캐릭터와 원작 게임의 분위기를 최대한 반영한다.

가능하면:

* 전투 BGM
* 캐릭터 테마
* 보스 BGM
* 승리 BGM
* 스토리 BGM

을 원작 분위기에 맞게 배치한다.

중요:

사용자가 프로젝트에 제공한 원작 음원 또는 사용 권한이 있는 파일이 있다면 그것을 연결할 수 있도록 한다.

음원 파일이 프로젝트에 존재하지 않는다면 임의로 인터넷에서 저작권 음원을 다운로드하지 않는다.

대신:

* AudioManager
* BGM ID
* Character Theme ID
* Battle Theme ID
* Victory Theme ID

등의 구조를 먼저 만들어 실제 음원을 나중에 쉽게 넣을 수 있게 한다.

예:

CharacterThemeType
BattleThemeType
StageThemeType

같은 구조로 관리한다.

프로토타입 단계에서는 원작 곡명/용도 메타데이터와 Placeholder AudioClip을 사용할 수 있다.

---

# 게임 기본 시스템

## 전투

Speed 기반 개인 턴제.

아군 턴 / 적군 턴 방식이 아니다.

캐릭터의 SPD에 따라 행동 순서를 계산한다.

초기에는 단순 SPD 순서를 사용하되 향후:

* 행동 후 Delay
* 무거운 기술 Delay
* 대기 시 Turn Advantage

등을 확장할 수 있도록 TurnScheduler를 분리한다.

예:

ITurnScheduler

구현체:

SpeedTurnScheduler

향후:

CTTurnScheduler

교체 가능하도록 설계한다.

---

# 한 턴의 기본 구조

캐릭터는 한 턴에:

* Move 1회
* Action 1회

가능하다.

순서는 자유롭다.

가능:

Move → Attack
Move → Skill
Attack → Move
Skill → Move

불가능:

Move → Move
Attack → Attack

특수 스킬은 예외 가능.

---

# 방향 시스템

4방향을 사용한다.

* Front
* Back
* Left
* Right

턴 종료 시 바라보는 방향을 결정한다.

공격 방향에 따라:

정면
< 측면
< 후면

순으로 공격자가 유리해지는 구조를 지원한다.

정확한 보정값은 데이터화한다.

하드코딩하지 않는다.

---

# Grid / Terrain

3D Grid.

각 Tile은 최소 다음 데이터를 가진다.

GridCoordinate
WorldPosition
Height
Walkable
MovementCost
TerrainType
Occupant

고저차 사용.

MOV:
평면 이동 범위

JMP:
오르내릴 수 있는 높이

지원할 지형:

Normal
Water
Obstacle
HighGround
LowGround

향후 확장 가능한 enum/data 구조를 사용한다.

---

# 이동

Dijkstra 또는 A* 기반.

필요 기능:

* 이동 가능 범위 표시
* 실제 이동 Path 표시
* 장애물 처리
* 다른 Unit 충돌 처리
* 고저차 처리
* MOV/JMP 처리
* 이동 취소
* 원래 위치 복귀

---

# Battle State

BattleController에 거대한 switch/if를 몰아넣지 않는다.

State Machine 사용.

예:

BattleStart
TurnStart
Command
MoveSelection
ActionSelection
TargetSelection
ActionExecution
FacingSelection
TurnEnd
BattleEnd

상태별 클래스를 분리한다.

---

# 캐릭터 데이터 구조

Data와 Runtime을 분리한다.

## CharacterData / UnitData

ScriptableObject 사용.

포함:

ID
Name
Job
Portrait
Sprites
Animator
WeaponType
BaseStats
GrowthStats
SkillList
UltimateSkill
PassiveSkill
Promotion
AudioTheme

## UnitRuntime

전투 중 데이터.

CurrentHP
CurrentMP
Position
Facing
CurrentBuffs
CurrentDebuffs
TurnState
Cooldown
SpecialGauge

ScriptableObject 원본 값을 직접 변경하지 않는다.

---

# 기본 능력치

HP
MP
STR
MAG
DEF
MDF
SPD
MOV
JMP

최대 레벨:

50

레벨업은 기본적으로 고정 성장.

랜덤 스탯 성장 사용하지 않는다.

---

# 직업

자유 전직은 없다.

모든 캐릭터는 고정 직업.

기본적으로 1회 승급.

승급 조건:

스토리 조건
+
레벨 조건

직업 전환 트리는 만들 필요 없다.

---

# 출전

현재 플레이어블:

10명

전투 최대 출전:

6명

출전 선택 UI를 향후 지원할 수 있도록 데이터 구조를 구성한다.

---

# 장비

3슬롯.

Weapon
Armor
Accessory

장비 강화:

+1
+2
+3

같은 강화 시스템은 사용하지 않는다.

상위 장비로 교체하는 방식.

---

# 사망

HP 0:

Battle KO

현재 전투에서만 전투불능.

영구 사망 없음.

전투 종료 후 복귀.

---

# 전투 승리 조건

현재 기본:

모든 적 전멸.

하지만 향후:

Boss Kill
Reach Position
Protect NPC
Survive Turns

등을 추가할 수 있도록 VictoryCondition을 인터페이스 또는 데이터 기반으로 설계한다.

---

# 플레이어 캐릭터 확정 스킬

아래 구성은 현재 합의된 기준이다.

기술 이름과 기술의 본래 콘셉트는 원작을 최대한 따른다.

SRPG용:

* 사거리
* 피해 배율
* 범위
* MP
* 상태효과

등은 데이터로 조절 가능하게 구현한다.

---

# 크레스 알베인

역할:

정통 검사 / 시공검사

MOV 4
JMP 2

일반 기술:

1. 마신검
2. 호아파참
3. 추사우
4. 봉황천구
5. 마신쌍파참
6. 차원참
7. 시공창파참

궁극기:

명공참상검

비연연각은 크레스에게 주지 않는다.

---

# 민트 아드네이드

역할:

순수 힐러 / 서포터

일반 기술:

1. 퍼스트 에이드
2. 힐
3. 너스
4. 피코 해머
5. 리커버
6. 레이즈 데드
7. 리저렉션

궁극기:

타임 스톱

피코 해머는 단순 상태이상 스킬이 아니다.

피코 해머에 실제 Damage를 넣는다.

권장:

MAG 기반 피해
+
일정 확률 스턴

---

# 벨벳 크라우

역할:

고기동 하이리스크 근접 딜러

핵심 시스템:

Consume Claw

컨슘 클로 사용 후 벨벳 자신의 턴 기준 3턴 동안:

* Heaven's Claw
* Hell's Claw
* Nightmare Claw

사용 가능.

3턴이 지나면 사용 불가능.

컨슘 클로 상태:

STR +20%
SPD +15%

헤븐즈 클로:

고위력 단일
강한 HP 흡수

헬즈 클로:

자신 주변 광역
게이지 회복

나이트메어 클로:

초고위력 단일
DEF 관통
강한 HP 흡수

나이트메어 클로는 컨슘 클로 이후 다른 공격을 최소 1회 사용해야 발동 가능하게 한다.

궁극기:

Impulse Desire

궁극기는 컨슘 클로 연계의 최종 단계로 사용할 수 있도록 한다.

---

# 파라 엘스테드

역할:

고기동 격투 콤보 딜러

MOV 5
JMP 3

일반 기술:

1. 장저파
2. 삼산화
3. 비연연각
4. 연아탄
5. 와룡공파
6. 쌍장저파
7. 사자전후

궁극기:

獅吼爆砕陣
사후폭쇄진

원작 특성을 반영한다.

발동:

모든 일반 기술 습득
+
사자전후 사용
+
사자전후 연출 중 파라가 회전하는 특정 타이밍에 추가 입력

성공:

사후폭쇄진 발동

실패:

사자전후만 발동

소모 MP 기준:

50

타이밍 입력 시스템을 구현하되 옵션에서 자동 입력 모드를 향후 지원할 수 있게 한다.

---

# 티아 그란츠

역할:

범위 회복 / 성가 버프 / 빛속성 마법

일반 기술:

1. 퍼스트 에이드
2. 나이트메어
3. 힐링 서클
4. 홀리 송
5. 홀리 랜스
6. 리저렉션
7. 저지먼트

궁극기:

이노센트 샤인

티아는 민트보다 순수 회복력은 낮지만:

공격 마법
성가 버프
범위 지원

이 강하다.

---

# 제이드 커티스

역할:

장거리 광역 포닉 술사 + 창술

기본 공격:

창
사거리 1~2

일반 기술:

1. 스탈라그마이트
2. 스플래시
3. 그라운드 대셔
4. 썬더 블레이드
5. 프리즘 소드
6. 앱솔루트
7. 메테오 스톰
8. 인디그네이션

인디그네이션은 궁극기가 아니다.

일반 최상위 스킬이다.

궁극기:

Mystic Cage
미스틱 케이지

미스틱 케이지는:

대범위 피해
+
이동 방해
+
SPD/MDF 약화

계열의 공간 제압 궁극기로 구현한다.

---

# 나탈리아

역할:

장거리 아처 + 보조 힐러

일반 기술:

1. 피어싱 라인
2. 스톰 엣지
3. 힐
4. 스타 스트로크
5. 갤런트 배러지
6. 큐어
7. 리바이브

리바이브:

미리 걸어놓는 자동 부활 버프.

궁극기:

아스트랄 레인

원작 디자인 정확도에 특히 신경 쓴다.

---

# 알펜

역할:

HP 소모형 화염검 딜러

핵심:

일반 검술
→ Flaming Edge 연계

일반 기술:

1. 마신검
2. 비룡승파
3. 추사우·알파
4. 패왕참

염검:

5. 봉황천구
6. 인페르널 토렌트
7. 인시너레이션 웨이브

염검은 MP보다는 HP를 주 자원으로 사용한다.

궁극기:

Scarlet Outburst
스칼렛 아웃버스트

---

# 시온 아이메리스

역할:

장거리 총기 딜러 + 힐러

일반 기술:

1. 마그나 레이
2. 퍼스트 에이드
3. 제미니 아쿠아
4. 트레스 벤토스
5. 힐링 서클
6. 그라비타스 필드
7. 레저렉션

궁극기:

Consuming Wildfire
컨슈밍 와일드파이어

그라비타스 필드는 적을 범위 중심으로 끌어들이는 기능을 지원한다.

---

# 키사라

역할:

메인 탱커 / 가드 / 반격

MOV 3
JMP 2

핵심 시스템:

가드

턴 종료 시 방향 선택.

정면 피해 크게 감소.
측면 피해 일부 감소.
후면은 보호되지 않음.

가드 성공 후:

Guard Ignition
가드 이그니션

활성.

일부 기술 강화.

일반 기술:

1. 가디언 필드
2. 호아파참
3. 피어싱 로어
4. 라이온즈 하울
5. 플레이밍 메테오
6. 이그니어스 디스차지
7. 빙장화

궁극기:

Flare Demolisher
플레어 데몰리셔

가드 성공 이후 강력한 반격형 궁극기로 구현한다.

---

# 스킬 시스템 설계

스킬별 클래스를 무작정 만들지 않는다.

예:

FireSkill.cs
HealSkill.cs
PoisonSkill.cs

같은 구조를 남발하지 않는다.

다음 구조를 우선한다.

SkillData

* Skill ID
* Name
* MP Cost
* HP Cost
* Range
* Area
* Element
* Target Type
* Animation
* Effects[]

Effect 예:

DamageEffect
HealEffect
BuffEffect
DebuffEffect
PushEffect
PullEffect
ReviveEffect
StatusEffect
MoveEffect

스킬 하나가 여러 Effect를 조합하도록 한다.

예:

Guardian Field

DamageEffect
+
HealEffect

Gravitas Field

DamageEffect
+
PullEffect

Piko Hammer

DamageEffect
+
ChanceStatusEffect(Stun)

---

# UI

초기 전투 UI:

현재 Unit
HP
MP
Turn Order

명령:

Move
Attack
Skill
Wait

Skill 클릭:

Skill List
→ Range 표시
→ Target 선택
→ Preview
→ 실행

가능하면 피해 Preview도 지원한다.

예:

Damage 120~135
Hit 90%

---

# 개발 1차 목표

가장 먼저 완성할 Vertical Slice:

* 테스트 맵 1개
* 플레이어 3명 이상
* 적 3~4명
* Grid
* 고저차
* 이동
* Turn
* 공격
* 스킬
* HP/MP
* KO
* Enemy AI
* 승리
* 패배
* Restart

캐릭터 아트가 완성되지 않아도 Placeholder로 기능을 먼저 완성한다.

하지만 캐릭터 데이터와 Sprite 구조는 최종 원작 디자인 에셋으로 교체하기 쉽게 만든다.

---

# AI

1단계 AI:

공격 가능한 적 검색
→ 가장 적절한 목표 선택
→ 공격

공격 불가능:
→ 적에게 접근
→ 이동
→ 가능한 행동
→ 턴 종료

향후 Utility AI로 확장 가능하도록 설계한다.

Score 예:

Kill +100
Low HP Target +30
Back Attack +25
High Ground +15
Danger Tile -30

---

# Save

초기에는 최소한:

Character Level
EXP
Equipment
Story Progress
Unlocked Skills

저장 구조를 만든다.

Battle Runtime 자체를 저장하는 기능은 뒤로 미뤄도 된다.

---

# 코드 품질

다음 원칙을 지킨다.

* SOLID를 무리하게 적용해서 코드가 지나치게 복잡해지지 않게 한다.
* Manager 하나에 모든 책임을 넣지 않는다.
* ScriptableObject와 Runtime State를 분리한다.
* Magic Number를 피한다.
* 전투 수치는 Data로 조정 가능하게 한다.
* Inspector에서 가능한 데이터를 확인할 수 있게 한다.
* Null 참조에 취약하지 않게 한다.
* 로그는 의미 있게 남긴다.
* 필요 없는 Debug.Log 남발 금지.
* namespace를 사용한다.
* 파일/클래스 명명 규칙을 일관되게 유지한다.

---

# 작업 지속성

매번 작은 작업 하나가 끝났다고 응답을 종료하지 않는다.

현재 작업이 정상 완료되면 TODO 목록에서 다음 우선순위 작업으로 바로 이동한다.

다음 조건 중 하나일 때까지 계속 작업한다.

1. 컴파일 또는 실행을 막는 외부 환경 문제
2. 사용자만 결정할 수 있는 중대한 게임 디자인 결정
3. 필요한 외부 에셋/권한이 없어 진행 불가능
4. 현재 계획된 큰 작업 묶음을 모두 완료

단순히:

“다음으로 무엇을 할까요?”

라고 묻고 중단하지 않는다.

합리적으로 판단 가능한 사항은 현재 기획을 기준으로 판단하여 계속 구현한다.

---

# 검증

코드를 작성한 뒤 반드시 가능한 범위에서 검증한다.

확인:

* Unity 컴파일 오류
* C# 오류
* NullReference 가능성
* Scene 연결 상태
* Prefab Reference
* ScriptableObject Reference
* Battle Flow
* 이동
* 타겟 선택
* Turn 변경
* KO
* Victory

가능하면 테스트용 Scene을 만들어 실제 플레이 흐름을 검증한다.

---

# 문서

프로젝트 안에 다음 문서를 유지한다.

Docs/

GAME_DESIGN.md
ARCHITECTURE.md
CHARACTERS.md
SKILLS.md
TODO.md

작업하면서 최신 상태로 갱신한다.

TODO.md에는:

DONE
IN PROGRESS
NEXT
LATER

형태로 상태를 유지한다.

다음 Codex 세션에서도 이 파일을 읽고 그대로 이어서 작업할 수 있게 한다.

---

# 지금 시작할 작업

현재 저장소를 먼저 전체 분석한다.

그 뒤 필요한 경우 다음 순서로 작업한다.

1. 프로젝트 구조 정리
2. Core Data Model
3. Grid System
4. Unit Runtime
5. Selection
6. Movement / Pathfinding
7. Turn System
8. Battle State Machine
9. Basic Attack
10. Generic Skill System
11. HP / MP / KO
12. Character ScriptableObjects
13. 캐릭터별 고유 시스템
14. Enemy AI
15. Victory / Defeat
16. Battle UI
17. Test Stage
18. Save 기본 구조
19. Audio 구조
20. Character Art 연결 구조

이미 구현된 부분이 있다면 다시 만들지 말고 기존 구현을 분석해서 이어서 진행한다.

첫 응답은 장문의 설명보다 프로젝트 분석 및 실제 작업을 시작하는 데 집중한다.

가능한 범위까지 중단하지 않고 구현을 계속 진행하라.
