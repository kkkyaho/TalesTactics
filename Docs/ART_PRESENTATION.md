# 5번 아트·연출 진행 기록

## 현재 구현

- 10명 × 앞/뒤/좌/우 총 40개 스프라이트. CharacterData 참조로 연결하며 파일명을 런타임 코드에 넣지 않는다.
- 원본 시트는 imagegen 생성 결과이며 공식 원화 파일 자체를 배포 에셋으로 복사하지 않았다.
- 카메라 회전을 고려한 방향 표시와 이동 경로별 바라보기.
- Idle/Walk/Attack/Skill/Cast/Guard/Damage/Dead/Ultimate 9개 상태의 **Transform 기반 기본 동작**. 별도 Animator가 지정되면 기존 Animator를 우선한다.
- 공격 준비 → 판정 → 타격 효과 순서, 속성색 궤적·충격 고리, 실제 HP 변화 숫자, 피격/KO 표현.
- 파라 타이밍 진행 고리와 성공 구간 색. 기존 입력 판정과 자동 타이밍 규칙을 유지한다.
- 공격/타격/회복/시전의 자체 합성 효과음 4개. 음악과 별도 AudioSource를 사용하고 재시작 시 정지한다.
- 임시 효과는 수명 종료 시 제거하며 Restart에서 코루틴과 전장 하위 효과를 함께 정리한다.
- 전체 10명 Skill/Ultimate의 4방향 준비·발동 160개 프레임, 89개 기술별 연출 설정과 10개 궁극기 패턴. 현재 상세는 아래 2026-09-27 기록을 따른다.
- 보스 BGM 선택 및 궁극기 테마 재생/원래 음악 위치 복귀. 실제 BGM 파일은 아직 연결하지 않았다.

## 완료로 간주하지 않는 부분

- 생성 시트의 윤곽 잔여 픽셀, 장비의 방향별 일관성, 일부 가장자리 여백을 다듬는 최종 아트 검수.
- 공격/시전/방어/피격은 단일 포즈, 보행은 4프레임, 쓰러짐은 3프레임, 기술/궁극기는 준비·발동 2프레임이다. 9상태의 연결은 완료했지만 고밀도 중간 작화와 방향별 장비·체형의 최종 보정은 남아 있다.
- 89개 기술의 패턴·준비/복귀 시간을 연결했다. 일반 기술은 공통 절차적 패턴을 조합하며 10명 궁극기는 서로 다른 패턴이다. 원작 연출을 기술별로 완전히 재현한 것은 아니다.
- 제공 BGM/음원 파일이 프로젝트에 없어 실제 음악 연결은 대기 중이다. 원작 음원을 내려받지 않았다.

따라서 5번 전체는 진행 중이다. 사용자 요청으로 검증된 중간 구현을 커밋·푸시하며, 위 미완료 범위를 전체 완료로 표현하지 않는다.

## 후속 단계 — 4방향 행동 포즈

- 10명 × 4방향 × Attack/Cast/Guard/Damage = 160개 포즈를 CharacterData.Poses에 연결했다. 기존 방향 그림 40개와 별도다.
- 공격은 준비 자세 → 공격 포즈(0.08–0.34초) → 기본 자세로 복귀한다. 피격 포즈는 0.28초 뒤 복귀하고 방어·시전은 상태가 유지되는 동안 표시한다.
- Skill은 Attack, Ultimate는 Cast 포즈를 재사용한다. 기술·궁극기마다 새로 그린 독립 포즈가 아니다. Idle/Walk/Dead는 기존 방향 그림과 Transform 연출을 유지한다.
- 카메라 기준 방향 선택, 기존 Animator 우선, 누락된 포즈의 기본 방향 그림 대체를 유지한다. 별도 웅크린 방어 그림에 기존 세로 축소를 중복 적용하지 않는다.
- 시트 행/열 간격이 불규칙해 Tools/character-pose-layout.csv에 각 그림의 실제 알파 윤곽 경계와 발 중심 pivot을 기록했다. Tools/character-pose-outlines.csv의 개별 렌더링 윤곽으로 이웃 그림의 조각을 제외한다. Tools/ImportCharacterPoses.cs.txt를 Editor eval로 실행해 2D Sprite API로 분할한다. 원본 이미지 픽셀은 변경하지 않는다.
- Tools/MeasureCharacterPoses.py는 Pillow/NumPy로 원본 알파를 읽어 분할/윤곽 CSV를 작성하는 분석 도구다. 그림을 다시 그리거나 원본 PNG를 저장하지 않는다. 분리된 큰 무기/새 레이아웃을 갖는 교체 시트는 자동 결과를 그대로 신뢰하지 말고 다시 검수한다.
- 임포트는 Poses 참조만 교체하며 능력치/장비/스킬/Animator/기존 방향 아트를 보존한다. 사용자 포즈 교체 후 자동 재실행하지 않는다. 원본 시트를 교체하면 분할 좌표도 다시 검수해야 한다.
- 생성 원화의 윤곽 잡색, 장비 방향 일관성, 자세 사이 체형/크기 차이는 최종 아트 보정 대상이다. 연속 프레임 애니메이션 완성을 뜻하지 않는다.
- poses-front/back/right/left-preview.png는 Game View에서 160개 포즈를 검수한 임시 갤러리다. 행 순서는 공격/시전/방어/피격. 기존 장면에 저장하지 않으며 Play Mode 종료로 폐기한다.
- 최종 실제 Unity EditMode 88/88·PlayMode 36/36 통과: unity-poses-editmode-results.json, unity-poses-playmode-results.json. 새 PlayMode 검사는 10명×4방향 포즈 선택, 공격 복귀, 방어 유지, 누락 포즈 대체를 포함한다. 도중 테스트 도구 문제와 복구 과정은 VALIDATION.md 참조.
- 최종 Windows 개발/일반 빌드 성공: unity-poses-development-build.json, unity-poses-windows-build.json. 실제 플레이어 캠페인 회귀 통과 및 사용자 저장 불변: PlayerReviews/ba00f74860894b488a721abb8692741e-summary.json.

## 후속 단계 — 파라·나탈리아 걷기·쓰러짐

- 파라·나탈리아 각 4방향 × 걷기 4프레임/쓰러짐 3프레임, 총 56개 Sprite를 추가했다. 원본은 기존 외형 시트를 참고한 imagegen 결과다.
- Walk는 8fps 반복, Dead는 6fps로 휘청임→무릎 꿇음→바닥 자세를 재생하고 마지막 프레임을 유지한다. 방향 전환/동일 KO 상태 갱신은 재생 시간을 초기화하지 않는다. 부활하면 기본 자세와 크기를 복구한다.
- 새 프레임에는 기존 보행 bob/기울기 및 KO 세로 축소를 중복 적용하지 않는다. 나머지 캐릭터·누락 프레임에는 기존 방향 그림/Transform 연출을 유지한다. 별도 Animator 우선 정책도 동일하다.
- DirectionalSpriteClip은 빈 배열/누락 프레임, 음수·비정상 시간 및 FPS를 안전하게 처리한다. 런타임에 이미지 파일을 찾지 않고 직렬화된 Sprite 참조를 사용한다.
- Tools/MeasureLocomotion.py는 원본 알파를 읽어 locomotion-layout/outlines.csv만 작성한다. Tools/ImportLocomotion.cs.txt는 Sprite Editor API로 해당 56개를 분할하고 Walk/Dead 필드만 교체한다. 능력치·장비·기존 160개 포즈는 보존한다.
- 나탈리아 첫 걷기 시트는 그림끼리 붙어 있어 채택하지 않았다. 여백을 다시 만든 natalia-walk-v2.png를 사용한다. 방향별 장비/체형의 최종 일관성 보정과 더 많은 중간 프레임은 여전히 남아 있다.
- 기존 ImportCharacterPoses 도구도 Walk/Dead를 보존하도록 바꿨다. 사용자 아트 편집 이후 임포트 도구를 무조건 재실행하지 않는다.
- 실제 Unity EditMode 90/90·PlayMode 38/38 통과(unity-locomotion-editmode-results.json, unity-locomotion-playmode-results.json). 별도 ManagedChecks 90/90은 엔진 독립 검사다. locomotion-frames-preview.png에서 56개 프레임을 Game View로 검수했다. 행 순서는 걷기 0–3, 쓰러짐 0–2다.
- Windows 개발/일반 빌드 성공(unity-locomotion-development-build.json, unity-locomotion-windows-build.json). 플레이어 캠페인 회귀 및 사용자 저장 불변 확인: PlayerReviews/8106f621368046a7984dabc4180f5a3a-summary.json. 검증된 중간 구현을 푸시하며 5번 전체 완료를 뜻하지 않는다.

## 후속 단계 — 전체 10명 걷기·쓰러짐

- 크레스·민트·벨벳·티아·제이드·알펜·시온·키사라의 224개 프레임을 추가했다. 기존 파라·나탈리아 56개와 합쳐 전체 10명 × 4방향 × 보행4/쓰러짐3 = 280개다. 위 두 단계의 구현 범위는 당시 기록이며 현재 Walk/Dead는 전원 프레임 클립을 사용한다.
- 기존 turnaround 시트를 참조해 생성한 PNG 16장을 Sprite Editor API로 분할했다. 원본 픽셀은 수정하지 않고, 알파 분석으로 프레임 경계·pivot·렌더링 윤곽을 지정한다. MeasureLocomotion.py에 캐릭터 ID를 전달하면 선택한 캐릭터의 CSV 행만 갱신한다.
- ImportLocomotion.cs.txt의 ids를 명시적으로 선택해 작은 묶음으로 실행한다. 해당 Walk/Dead를 교체하므로 사용자 편집 이후 자동 재실행하지 않는다. 이번에는 새 8명만 임포트했으며 기존 파라·나탈리아 데이터 및 공격/시전/방어/피격 포즈를 보존했다.
- 실제 Unity EditMode 90/90·PlayMode 38/38 통과(unity-locomotion-roster-editmode-results.json, unity-locomotion-roster-playmode-results.json). 테스트 수는 같지만 보행 4방향 반복/Idle 복귀 및 KO 진행/유지/부활 검사의 대상이 2명에서 10명으로 늘었다. 별도 ManagedChecks 90/90도 통과했다.
- Game View 1920×1080에서 새 224개 프레임의 잘림·이웃 그림 혼입·배경 투명도를 확인했다. 증거: locomotion-cless-mint-preview.png, locomotion-velvet-tear-preview.png, locomotion-jade-alphen-preview.png, locomotion-shionne-kisara-preview.png. 임시 갤러리는 Play Mode 종료로 폐기했다.
- 4프레임 보행과 3프레임 쓰러짐을 연결한 단계다. 방향별 장비/얼굴/체형의 일관성, 가장자리 잔여 픽셀, 부드러운 중간 작화는 최종 보정 대상이다. 기술/궁극기 전용 포즈·상세 연출·제공 BGM도 남아 있으므로 5번 전체 완료는 아니다.
- Windows 개발/일반 빌드 및 실제 플레이어 캠페인 회귀 통과: unity-locomotion-roster-development-build.json, unity-locomotion-roster-windows-build.json, PlayerReviews/68ca64ec4d8a4864bd1f3c54142dc776-summary.json. 빌드 경고와 검증 한계는 VALIDATION.md에 기록했다.

## 공식 외형 자료

| 캐릭터 | 확인한 공식 자료 |
|---|---|
| 크레스·민트 | https://tales-ch.jp/titles/top/ |
| 벨벳 | https://tales-ch.jp/titles/tob/ |
| 파라 | https://to-readinglive.tales-ch.jp/eternia/index.html |
| 티아·제이드·나탈리아 | https://tales-ch.jp/titles/toa/ |
| 나탈리아 보조 자료 | https://www.bandainamcoent.co.jp/cs/list/talesoffandom2/character/natalia.php |
| 알펜·시온·키사라 | https://tales-ch.jp/titles/toarise/ |

확인한 페이지 일부는 상반신 자료이므로 하반신/후면 세부의 정확성을 모두 증명하지는 않는다. 파라의 녹색 단발·주황 의상·붉은 어깨 망토, 나탈리아의 금발 단발·갈색 머리띠·청록/백색 의상·노란 목장식을 우선 반영했다.

## 재실행 및 교체

Unity Editor가 열려 있고 Play Mode가 아닐 때 Tools/ImportCharacterArt.cs.txt 내용을 CLI eval로 실행한다. 2D Sprite 패키지의 편집 capability를 먼저 확인하며 실패 시 중단한다. 기존 Sprite GUID를 보존한다. 이 명령은 지정한 10명 아트 참조를 교체하므로 사용자 아트 교체 후 무조건 재실행하지 않는다. DemoContent.Create는 실행하지 않는다.

Tools/CreatePresentationAudio.cs.txt는 존재하는 WAV와 AudioEntry.Clip을 덮어쓰지 않는다. 제공 음악은 Content/AudioLibrary의 battle/story/victory 항목에 AudioClip을 지정한다. 파일 출처는 SourceMetadata에 기록한다. 런타임은 누락된 음악을 무음으로 처리한다.

## 검증 증거

- unity-art-editmode-results.json: 실제 Unity EditMode 88/88.
- unity-art-playmode-results.json: 실제 Unity PlayMode 30/30. 아트·동작·방향, 임시 효과 수명/재시작, 효과음 소스 분리 검사 포함.
- unity-art-hpbar-playmode-results.json: HP 바 위치 수정 후 방향·카메라 회전·머리 위 표시 1개 재검사 통과.
- unity-art-development-build.json / unity-art-windows-build.json: Windows 개발·일반 빌드 성공. PlayerReviews/6615117d84a24aeb88ba48e0e01c2188-summary.json: 실제 실행 파일 캠페인 회귀 통과, 사용자 저장 불변.
- ManagedChecks 88/88은 엔진 독립 규칙 검사이며 Unity 테스트와 별개다.
- art-battle-preview.png: Unity Game View의 6인 배치 화면. 최종 아트·프레임 애니메이션 또는 Windows 수동 완주 검수 완료를 뜻하지 않는다.

## 후속 단계 — 캐릭터별 VFX와 대상별 피드백

CharacterData.VisualStyle에 캐릭터 기본값, SkillData.VisualStyle에 기술별 선택적 재정의를 둔다. Automatic은 캐릭터 기본값 또는 무기 유형을 사용한다. 원작 연출의 완전한 복제를 뜻하지 않는 프로젝트용 절차적 효과다.

| 캐릭터 | 기본 표현 |
|---|---|
| 크레스 | 교차 검격 호 |
| 민트 | 성광 고리·십자 |
| 벨벳 | 3줄 클로 궤적 |
| 파라 | 이중 충격파·방사선 |
| 티아 | 3중 음파 |
| 제이드 | 긴 창 찌르기 |
| 나탈리아 | 이동하는 화살 궤적 |
| 알펜 | 검격과 불꽃 기둥 |
| 시온 | 총탄 궤적·교차 섬광 |
| 키사라 | 이중 육각 방패 |

- 궁극기는 유형별 기본 도형에 회전 문양 두 개를 더한다. 색상은 기술 속성을 따른다.
- 회복은 녹색 상승 십자, 부활은 금색 기둥, HP 변화 없는 보조 기술은 보호 문양을 사용한다.
- 판정 전에 대상 목록을 보관하므로 KO/밀치기/부활 후에도 실제 대상마다 효과가 표시된다. HP 숫자는 실제 변화만 표시한다.
- 시전자 HP 비용은 `HP -수치`로 분리하며 피격 동작을 재생하지 않는다. 흡혈 회복이 시전자의 공격 동작을 덮어쓰지 않는다.
- 같은 공격·기술·피격 상태의 연속 호출은 동작 시간을 새로 시작한다. 타이밍 진행 갱신은 매 프레임 동작을 초기화하지 않는다.
- CombatEffect는 0.6초 뒤 소멸하며 Restart에서 전장과 함께 제거된다. 재질은 공유한다.
- Tools/ConfigureCombatVisuals.cs.txt는 Automatic인 기존 캐릭터만 갱신한다. 수동 지정값·전투 수치·스킬은 보존한다.
- 화면 검수에서 기존 URP Unlit 하이라이트 재질이 정점 색/알파를 무시하는 것을 확인했다. VFX와 타이밍 고리는 이미 연결된 Sprites/Default 재질을 공유해 색·페이드를 표시한다.
- unity-vfx-editmode-results.json: 실제 EditMode 88/88. unity-vfx-playmode-results.json: 실제 PlayMode 34/34. 별도 ManagedChecks 88/88.
- vfx-gallery-preview.png: 10가지 기본 기하 표현의 Game View 검수. Tools/ReviewCombatEffects.cs.txt로 만든 임시 정지 갤러리이며 실제 전투 UI에 표시되는 메뉴가 아니다. 검수 후 시간 배율을 1로 복구하고 Play Mode를 종료했다.
- 최종 Windows 개발/일반 빌드 성공: unity-vfx-development-build.json, unity-vfx-windows-build.json. 첫 개발 빌드의 Pipeline 분석 단계 실패와 재시도 기록은 VALIDATION.md를 따른다.
- 실제 플레이어 캠페인 회귀 통과: PlayerReviews/100d320ccf4d488c86833f7892d5a47d-summary.json. 기존 사용자 저장/백업 불변.

## 2026-09-27 — 전체 기술·궁극기 단계 연출

- 내장 imagegen으로 기존 캐릭터 방향 시트를 참조한 `Assets/TalesTactics/Art/Characters/<id>-specials.png` 10장을 생성했다. 생성 프롬프트는 SPECIAL_ART_PROMPTS.md에 보관했다. 10명 × 4방향 × Skill/Ultimate × 준비/발동 = 새 160개 프레임이다. 기존 480개 포즈/프레임과 합쳐 640개이며 원본 PNG 픽셀은 수정하지 않았다.
- MeasureSpecials.py는 알파 연결 성분 16개/시트를 검사하고 실제 경계·발 pivot·렌더링 윤곽 CSV만 작성한다. ImportSpecials.cs.txt는 Sprite Editor Data Provider API로 분할하고 Poses.Skill/Ultimate만 연결한다. 사용자 편집 후 자동 재실행하지 않는다. 일반 기술의 각기 다른 전용 그림 68세트를 만든 것은 아니며 캐릭터별 Skill 준비/발동 포즈를 공유한다.
- CharacterMotion.BeginSkill은 준비 프레임을 유지하고 ReleaseSkill은 실제 판정 뒤 발동 프레임으로 바꾼다. 회복 시간이 끝나면 기본 자세로 돌아간다. 카메라 상대 4방향, 누락 클립 대체, 사용자 Animator 우선은 유지한다.
- 전체 89개 기술(아군 88개+적 기본 공격 1개)에 Pattern/Pulses/Windup/Recovery/Size를 설정했다. Tools/skill-presentation.csv가 적용표이며 ImportSkillPresentation.cs.txt는 Automatic인 필드만 채운다. 일반 기술은 검격·찌르기·상승·파동·폭발·비·기둥·번개·얼음·운석·회복·보호 등 공통 기하 패턴을 공유한다.
- 10명 궁극기는 검격 문양/시계/클로/충격파/빛기둥/감옥/화살비/화염검/총격화염/방패 문양으로 구분한다. 원작 애니메이션 복제가 아닌 프로젝트용 절차적 VFX다. 회복/부활 대상에는 기존 전용 효과를 우선한다.
- 기술명·축소 준비 고리 → 준비 시간 → 판정 한 번 → 발동 프레임·타격 효과 → 복귀 순서다. 시각 반복은 HP/MP/비용/판정 횟수를 늘리지 않는다. 기존 파라 타이밍 입력 규칙과 성공 구간은 변경하지 않았다.
- boss BGM이 있으면 2장에서 재생하며 누락 시 battle을 사용한다. 궁극기 테마가 있으면 재생 후 이전 음악의 샘플 위치/반복/재생 상태로 복귀한다. 누락 테마는 음악을 끊지 않고 Restart는 복귀 예약까지 취소한다. 실제 음원 파일이 없어 음악 연결과 청음은 대기다. 임시 테스트용 무음 AudioClip은 에셋으로 저장하지 않았다.
- 실제 Unity EditMode 90/90, PlayMode 42/42 통과. 새 검사는 10명×4방향 준비/발동/복귀, 10개 궁극기 기하 구분, 준비 중 수치 불변과 판정 1회, Restart 정리, 음악 복귀/누락 처리를 검증한다. 별도 ManagedChecks 90/90은 엔진 독립 검사다.
- Game View 1920×1080: special-front/back/right/left-preview.png에서 160개 포즈의 경계·투명도·이웃 조각 혼입을 확인했다. special-finales-preview.png는 기하 비교를 위해 같은 색으로 만든 10개 궁극기 정지 갤러리다. 실제 전투 색은 속성을 따른다. 시간 배율을 복구하고 Play Mode를 종료해 임시 갤러리를 폐기했다.
- Windows 개발/일반 빌드 성공 및 실제 플레이어 3회 캠페인 회귀 통과. 증거는 unity-special-development-build.json, unity-special-windows-build.json, PlayerReviews/8fde9a7b98ae496c8cd266ad21b52ff6-summary.json. 기존 사용자 저장/백업이 유지됐다.
- 기존 99개 캐릭터/스킬 에셋을 HEAD와 비교해 새 Skill/Ultimate/Presentation 필드와 기존 기본값의 명시 직렬화 외에는 모두 보존된 것을 확인했다. DemoContent/Scene 재생성은 하지 않았다.
- 이번 단계로 기술/궁극기의 전용 준비·발동과 실행 연결은 완료했다. 최종 원작 외형의 정확성, 방향·동작 사이 장비/체형 일관성, 더 부드러운 중간 작화, 제공 BGM 연결/청음은 남는다. 따라서 5번 전체 완료로 표시하지 않는다.
