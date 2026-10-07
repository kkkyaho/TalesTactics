# 중간 동작 및 외형 일관성 개선 — 2026-10-04

요청한 그래픽 추가 개선(1번)을 영웅10명·일반 적9종·다오스 전체에 적용했다. 기존 3등신 고밀도 픽셀 콘셉트, 머리·복장·무기와 몬스터별 실루엣을 기준으로 20개 보충 시트, 총320개 방향별 그림을 제작했다.

## 적용 범위

- 각 시트는 공격 준비·공격 후속·기술 준비·복귀의 4행 × 앞/뒤/오른쪽/왼쪽 4열이다.
- 기본 공격4프레임, 일반 기술5프레임, 궁극기4프레임으로 연결했다. 기존 타격·시전·궁극기 핵심 그림을 유지하며 준비/복귀 그림은 공유한다. 기술89개마다 독립된 전신 애니메이션을 제작한 것은 아니다.
- 준비 단계는 실제 ReleaseSkill 호출 전까지 발동 그림으로 넘어가지 않는다. 발동 후 회복 시간에 맞춰 후속·복귀를 재생한다. 전투 판정·피해·비용·행동 시간은 바꾸지 않았다.
- 기존 보행4프레임·쓰러짐3프레임과 맵·음악은 유지했다. 이번 항목은 캐릭터 중간 동작 개선이다.

## 화면 검수

Unity에서 실제 임포트된 Sprite와 전투 Material로 렌더링한 비교 화면이다. 열은 캐릭터, 행은 Idle/Windup/Impact/Follow/Prepare/Recover 순서다. 네 방향에서 크기·체형·머리·복장·무기 식별, 잘림과 인접 그림 혼입을 확인했다. 숙임·무기 들어올림·슬라임 변형은 의도적인 자세 차이로 유지했다.

| 대상 | 앞 | 뒤 | 오른쪽 | 왼쪽 |
|---|---|---|---|---|
| 영웅10명 | [화면](heroes-front.png) | [화면](heroes-back.png) | [화면](heroes-right.png) | [화면](heroes-left.png) |
| 적9종·다오스 | [화면](enemies-front.png) | [화면](enemies-back.png) | [화면](enemies-right.png) | [화면](enemies-left.png) |

임시 갤러리는 Play Mode에서만 생성했고 종료 후 제거했다. Scene은 저장하지 않았다. 시온 공격 준비/복귀 행의 좌우 열 순서를 Sprite 참조에서 보정했다. 초안의 민트 잘림·파라/알펜 경계 혼입을 발견해 시트를 재생성한 뒤 채택했다. 최종320칸은 alpha 경계 여백·빈 칸·불투명 배경 검사를 통과했다([alpha-audit.json](alpha-audit.json)).

## 재현 및 검증

`Tools/animation-polish-generation.json`에 채택 생성 경로와 프롬프트를 보존했다. 내장 imagegen으로 원본 PNG를 생성했으며 Python은 읽기 전용 alpha 분석과 CSV 작성만 수행한다. `MeasureAnimationPolish.py` → `AnimationPolishImporter.Import()`를 명시적으로 실행한다. 사용자 에셋 수정 후 자동 재실행하지 않는다. Importer는 BattleCatalog의 실제20종만 대상으로 삼고 Sprite Editor API로 경계·윤곽·피벗 및 동작 참조를 설정한다. 능력치/스킬/성장 데이터는 재생성하지 않는다.

- 실제 Unity EditMode **95/95**, PlayMode **54/54** 통과: [EditMode](editmode-results.json), [PlayMode](playmode-results.json).
- 신규 검사는 단계별 프레임 제한·레거시 클립, 전체20종/4방향 참조, 실제 준비 대기→발동→복귀 및 쓰러짐/대기 중단을 확인한다.
- 빌드 및 Windows 실행 결과는 [VALIDATION.md](../VALIDATION.md)의 이 날짜 기록을 따른다.

이 검수는 전6장 사람 수동 완주, 사람의 난이도 평가, 실제 게임패드 기기별 검수 또는 원작 작화에 대한 외부 전문가 승인을 대신하지 않는다.
