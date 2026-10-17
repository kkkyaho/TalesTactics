# 실제 카탈로그 기술 인벤토리

기준 커밋: `37e41a176e664907e2ffba1f782a2da586766554`. `BattleCatalog.asset`의 GUID를 `.meta`로 해석해 실제 캐릭터와 기술 참조를 따라갔다. Unity 실행·Content 수정·재생성 없이 추출했다. 이 문서는 이번 전체 기술 연출 작업 **이전 기준선**이다.

## 집계

| 구분 | 수 |
|---|---:|
| 카탈로그 캐릭터 에셋 | 43 (아군 10 / 기본 Enemy 1 / Enemies 29 / variant 3) |
| 고유 기술 ID / 에셋 | 182 / 182 |
| 공유·variant 중복 포함 기술 참조 | 190 |
| 아군 기본공격 | 10 |
| 아군 일반기 | 68 |
| 아군 궁극기 | 10 |
| 기존 정확한 ID 전용 연출 | 3 |
| 남은 아군 일반기·궁극기 | 75 |
| 적·보스·전술 고유 기술 | 94 (기본공격 29 / 보스 패턴 60 / 다오스 블래스트 1 / 전술 4) |
| 누락 GUID / 중복 기술 ID | 0 / 0 |

아군 일반·궁극기 78개는 원작 캐릭터 기반 기술이다. 이 분류만으로 개별 명칭·동작의 원작 일치를 검증한 것은 아니다. 보스 패턴은 `SourceNote`상 **원작 기술명 37개 + 창작 기술명 23개**이며, 둘 다 전술 효과는 프로젝트 재설계다. 나머지 적 기본공격·전술 34개는 개별 원작 출처가 명시되지 않아 별도로 분류했다.

전체 원본 수치·효과 배열·소유자·파일 경로·SHA256은 [catalog.json](catalog.json)에 있다. `DisplayName`은 에셋 원문을 그대로 보존했다. 영어/일본어 이름에 임의 한글 번역을 붙이지 않았다. `Pulses`는 시각 반복 수이고 실제 피해 횟수가 아니다. `effectCount`는 직렬화된 효과 항목 수다.

## 기존 전용 연출

| ID | 이름 | 전용 시그니처 | 준비 / 이동 / 회복(초) |
|---|---|---|---|
| cless.0 | 마신검 | DemonFang | 0.40 / 0.35 / 0.65 |
| jade.3 | 썬더 블레이드 | ThunderBlade | 0.65 / 0.40 / 1.10 |
| jade.6 | 메테오 스톰 | MeteorStorm | 0.65 / 0.60 / 1.10 |

`Runtime/Presentation/SkillSignature.cs`의 정확한 ID 세 개만 전용 시그니처를 선택한다. `ImpactSignatures.cs`, `ImpactArt.cs`, `ImpactParticles.cs`가 전용 메시·아트·풀을 사용하고 `SkillAudioPalette.cs`가 세 전용 합성 음색을 선택한다. 나머지는 `SkillVisualPattern`, 속성, 캐릭터/무기 스타일에 따른 공통 연출이다. 궁극기의 고유 Pattern 명칭은 기존 패턴 기하를 가리키며 세 기술과 같은 정확한 ID 전용 시그니처는 아니다.

## 아군 소유 기술

| 캐릭터 | 일반기 ID와 이름 | 궁극기 | 기본공격 |
|---|---|---|---|
| 크레스 알베인 (`cless`) | cless.0 마신검<br>cless.1 호아파참<br>cless.2 추사우<br>cless.3 봉황천구<br>cless.4 마신쌍파참<br>cless.5 차원참<br>cless.6 시공창파참 | cless.ultimate 명공참상검 | cless.attack 공격 |
| 민트 아드네이드 (`mint`) | mint.0 퍼스트 에이드<br>mint.1 힐<br>mint.2 너스<br>mint.3 피코 해머<br>mint.4 리커버<br>mint.5 레이즈 데드<br>mint.6 리저렉션 | mint.ultimate 타임 스톱 | mint.attack 공격 |
| 벨벳 크라우 (`velvet`) | velvet.0 컨슘 클로<br>velvet.1 헤븐즈 클로<br>velvet.2 헬즈 클로<br>velvet.3 나이트메어 클로 | velvet.ultimate Impulse Desire | velvet.attack 공격 |
| 파라 엘스테드 (`farah`) | farah.0 장저파<br>farah.1 삼산화<br>farah.2 비연연각<br>farah.3 연아탄<br>farah.4 와룡공파<br>farah.5 쌍장저파<br>farah.6 사자전후 | farah.ultimate 사후폭쇄진 / 獅吼爆砕陣 | farah.attack 공격 |
| 티아 그란츠 (`tear`) | tear.0 퍼스트 에이드<br>tear.1 나이트메어<br>tear.2 힐링 서클<br>tear.3 홀리 송<br>tear.4 홀리 랜스<br>tear.5 리저렉션<br>tear.6 저지먼트 | tear.ultimate 이노센트 샤인 | tear.attack 공격 |
| 제이드 커티스 (`jade`) | jade.0 스탈라그마이트<br>jade.1 스플래시<br>jade.2 그라운드 대셔<br>jade.3 썬더 블레이드<br>jade.4 프리즘 소드<br>jade.5 앱솔루트<br>jade.6 메테오 스톰<br>jade.7 인디그네이션 | jade.ultimate 미스틱 케이지 / Mystic Cage | jade.attack 공격 |
| 나탈리아 (`natalia`) | natalia.0 피어싱 라인<br>natalia.1 스톰 엣지<br>natalia.2 힐<br>natalia.3 스타 스트로크<br>natalia.4 갤런트 배러지<br>natalia.5 큐어<br>natalia.6 리바이브 | natalia.ultimate 아스트랄 레인 | natalia.attack 공격 |
| 알펜 (`alphen`) | alphen.0 마신검<br>alphen.1 비룡승파<br>alphen.2 추사우·알파<br>alphen.3 패왕참<br>alphen.4 봉황천구<br>alphen.5 인페르널 토렌트<br>alphen.6 인시너레이션 웨이브 | alphen.ultimate 스칼렛 아웃버스트 | alphen.attack 공격 |
| 시온 아이메리스 (`shionne`) | shionne.0 마그나 레이<br>shionne.1 퍼스트 에이드<br>shionne.2 제미니 아쿠아<br>shionne.3 트레스 벤토스<br>shionne.4 힐링 서클<br>shionne.5 그라비타스 필드<br>shionne.6 레저렉션 | shionne.ultimate 컨슈밍 와일드파이어 | shionne.attack 공격 |
| 키사라 (`kisara`) | kisara.0 가디언 필드<br>kisara.1 호아파참<br>kisara.2 피어싱 로어<br>kisara.3 라이온즈 하울<br>kisara.4 플레이밍 메테오<br>kisara.5 이그니어스 디스차지<br>kisara.6 빙장화 | kisara.ultimate 플레어 데몰리셔 | kisara.attack 공격 |

## 전체 기술

효과의 `M`/`P`는 Magic 여부, 숫자는 Power다. 효과의 상세 Chance·Flat·Duration·Distance·Drain 및 소비량은 JSON에 그대로 저장했다. Pattern과 속성은 현재 에셋의 enum 값을 이름으로 해석했다.

### 아군 기본공격 (10)

| ID | 에셋 표시명 | 소유 캐릭터/역할 | 효과 | Pattern | Element | Pulses / Effects | 기존 전용 |
|---|---|---|---|---|---|---|---|
| cless.attack | 공격 | cless:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| mint.attack | 공격 | mint:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| velvet.attack | 공격 | velvet:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| farah.attack | 공격 | farah:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| tear.attack | 공격 | tear:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| jade.attack | 공격 | jade:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| natalia.attack | 공격 | natalia:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| alphen.attack | 공격 | alphen:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| shionne.attack | 공격 | shionne:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| kisara.attack | 공격 | kisara:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |

### 원작 캐릭터 기반 아군 기술 (78)

| ID | 에셋 표시명 | 소유 캐릭터/역할 | 효과 | Pattern | Element | Pulses / Effects | 기존 전용 |
|---|---|---|---|---|---|---|---|
| cless.ultimate | 명공참상검 | cless:ultimate | DamageP×3.8 | SwordFinale | Light | 3 / 1 | None |
| cless.0 | 마신검 | cless:normal | DamageP×1.25 | Wave | None | 2 / 1 | DemonFang |
| cless.1 | 호아파참 | cless:normal | DamageP×1.45 | Rising | None | 1 / 1 | None |
| cless.2 | 추사우 | cless:normal | DamageP×1.65 | Thrust | None | 1 / 1 | None |
| cless.3 | 봉황천구 | cless:normal | DamageP×1.85 | Rising | Fire | 1 / 1 | None |
| cless.4 | 마신쌍파참 | cless:normal | DamageP×2.05 | Wave | None | 2 / 1 | None |
| cless.5 | 차원참 | cless:normal | DamageP×2.25 | Slash | None | 2 / 1 | None |
| cless.6 | 시공창파참 | cless:normal | DamageP×2.45 | Wave | Light | 2 / 1 | None |
| mint.ultimate | 타임 스톱 | mint:ultimate | Status:Stun | TimeStop | None | 1 / 1 | None |
| mint.0 | 퍼스트 에이드 | mint:normal | HealM×1.6 | Heal | None | 1 / 1 | None |
| mint.1 | 힐 | mint:normal | HealM×2.7 | Heal | None | 1 / 1 | None |
| mint.2 | 너스 | mint:normal | HealM×2.1 | Heal | None | 1 / 1 | None |
| mint.3 | 피코 해머 | mint:normal | DamageM×1.3, Status:Stun | Burst | None | 3 / 2 | None |
| mint.4 | 리커버 | mint:normal | Cleanse | Cleanse | None | 1 / 1 | None |
| mint.5 | 레이즈 데드 | mint:normal | Revive | Revive | None | 1 / 1 | None |
| mint.6 | 리저렉션 | mint:normal | Revive | Revive | None | 1 / 1 | None |
| velvet.ultimate | Impulse Desire | velvet:ultimate | DamageP×3.8 | ClawFinale | Dark | 3 / 1 | None |
| velvet.0 | 컨슘 클로 | velvet:normal | ConsumeClaw | Claw | None | 3 / 1 | None |
| velvet.1 | 헤븐즈 클로 | velvet:normal | DamageP×2 | Rising | None | 1 / 1 | None |
| velvet.2 | 헬즈 클로 | velvet:normal | DamageP×1.7, Gauge(caster) | Claw | Fire | 3 / 2 | None |
| velvet.3 | 나이트메어 클로 | velvet:normal | DamageP×3.2 | Claw | Dark | 3 / 1 | None |
| farah.ultimate | 사후폭쇄진 / 獅吼爆砕陣 | farah:ultimate | DamageP×3.8 | LionFinale | None | 3 / 1 | None |
| farah.0 | 장저파 | farah:normal | DamageP×1.25 | Wave | None | 2 / 1 | None |
| farah.1 | 삼산화 | farah:normal | DamageP×1.45 | Burst | None | 3 / 1 | None |
| farah.2 | 비연연각 | farah:normal | DamageP×1.65 | Rising | None | 1 / 1 | None |
| farah.3 | 연아탄 | farah:normal | DamageP×1.85 | Burst | None | 3 / 1 | None |
| farah.4 | 와룡공파 | farah:normal | DamageP×2.05 | Rising | None | 1 / 1 | None |
| farah.5 | 쌍장저파 | farah:normal | DamageP×2.25 | Wave | None | 2 / 1 | None |
| farah.6 | 사자전후 | farah:normal | DamageP×2.45 | Burst | None | 3 / 1 | None |
| tear.ultimate | 이노센트 샤인 | tear:ultimate | DamageM×3.8 | Radiance | Light | 3 / 1 | None |
| tear.0 | 퍼스트 에이드 | tear:normal | HealM×1.3 | Heal | None | 1 / 1 | None |
| tear.1 | 나이트메어 | tear:normal | DamageM×1.4, Status:Sleep | Sleep | Dark | 1 / 2 | None |
| tear.2 | 힐링 서클 | tear:normal | HealM×1.6 | Heal | None | 1 / 1 | None |
| tear.3 | 홀리 송 | tear:normal | Buff:Song | Barrier | None | 1 / 1 | None |
| tear.4 | 홀리 랜스 | tear:normal | DamageM×2.05 | Pillar | Light | 1 / 1 | None |
| tear.5 | 리저렉션 | tear:normal | Revive | Revive | None | 1 / 1 | None |
| tear.6 | 저지먼트 | tear:normal | DamageM×2.45 | Rain | Light | 3 / 1 | None |
| jade.ultimate | 미스틱 케이지 / Mystic Cage | jade:ultimate | DamageM×3.5, Debuff:Cage | Cage | Dark | 1 / 2 | None |
| jade.0 | 스탈라그마이트 | jade:normal | DamageM×1.25 | Pillar | Earth | 1 / 1 | None |
| jade.1 | 스플래시 | jade:normal | DamageM×1.45 | Burst | Water | 3 / 1 | None |
| jade.2 | 그라운드 대셔 | jade:normal | DamageM×1.65 | Wave | Earth | 2 / 1 | None |
| jade.3 | 썬더 블레이드 | jade:normal | DamageM×1.85 | Lightning | Lightning | 2 / 1 | ThunderBlade |
| jade.4 | 프리즘 소드 | jade:normal | DamageM×2.05 | Rain | Light | 3 / 1 | None |
| jade.5 | 앱솔루트 | jade:normal | DamageM×2.25 | Ice | Ice | 1 / 1 | None |
| jade.6 | 메테오 스톰 | jade:normal | DamageM×2.45 | Meteor | Fire | 3 / 1 | MeteorStorm |
| jade.7 | 인디그네이션 | jade:normal | DamageM×2.65 | Lightning | Lightning | 2 / 1 | None |
| natalia.ultimate | 아스트랄 레인 | natalia:ultimate | DamageP×3.8 | AstralRain | Light | 3 / 1 | None |
| natalia.0 | 피어싱 라인 | natalia:normal | DamageP×1.25 | Thrust | None | 1 / 1 | None |
| natalia.1 | 스톰 엣지 | natalia:normal | DamageP×1.45 | Wave | Wind | 2 / 1 | None |
| natalia.2 | 힐 | natalia:normal | HealM×1.6 | Heal | None | 1 / 1 | None |
| natalia.3 | 스타 스트로크 | natalia:normal | DamageP×1.85 | Rain | Light | 3 / 1 | None |
| natalia.4 | 갤런트 배러지 | natalia:normal | DamageP×2.05 | Rain | None | 3 / 1 | None |
| natalia.5 | 큐어 | natalia:normal | HealM×3 | Heal | None | 1 / 1 | None |
| natalia.6 | 리바이브 | natalia:normal | AutoRevive | Revive | None | 1 / 1 | None |
| alphen.ultimate | 스칼렛 아웃버스트 | alphen:ultimate | DamageP×3.8 | FlameFinale | Fire | 3 / 1 | None |
| alphen.0 | 마신검 | alphen:normal | DamageP×1.25 | Wave | None | 2 / 1 | None |
| alphen.1 | 비룡승파 | alphen:normal | DamageP×1.45 | Rising | None | 1 / 1 | None |
| alphen.2 | 추사우·알파 | alphen:normal | DamageP×1.65 | Thrust | None | 1 / 1 | None |
| alphen.3 | 패왕참 | alphen:normal | DamageP×1.85 | Slash | None | 2 / 1 | None |
| alphen.4 | 봉황천구 | alphen:normal | DamageP×2.2 | Rising | Fire | 1 / 1 | None |
| alphen.5 | 인페르널 토렌트 | alphen:normal | DamageP×2.8000002 | Wave | Fire | 2 / 1 | None |
| alphen.6 | 인시너레이션 웨이브 | alphen:normal | DamageP×3.4 | Wave | Fire | 2 / 1 | None |
| shionne.ultimate | 컨슈밍 와일드파이어 | shionne:ultimate | DamageP×3.8 | Wildfire | Fire | 3 / 1 | None |
| shionne.0 | 마그나 레이 | shionne:normal | DamageP×1.25 | Thrust | Light | 1 / 1 | None |
| shionne.1 | 퍼스트 에이드 | shionne:normal | HealM×1.5 | Heal | None | 1 / 1 | None |
| shionne.2 | 제미니 아쿠아 | shionne:normal | DamageP×1.65 | Burst | Water | 3 / 1 | None |
| shionne.3 | 트레스 벤토스 | shionne:normal | DamageP×1.85 | Wave | Wind | 2 / 1 | None |
| shionne.4 | 힐링 서클 | shionne:normal | HealM×1.5 | Heal | None | 1 / 1 | None |
| shionne.5 | 그라비타스 필드 | shionne:normal | DamageM×1.8, Pull | Gravity | Dark | 1 / 2 | None |
| shionne.6 | 레저렉션 | shionne:normal | Revive | Revive | None | 1 / 1 | None |
| kisara.ultimate | 플레어 데몰리셔 | kisara:ultimate | DamageP×3.8 | ShieldFinale | Fire | 3 / 1 | None |
| kisara.0 | 가디언 필드 | kisara:normal | DamageP×1.2, HealM×1.2(caster) | Barrier | None | 1 / 2 | None |
| kisara.1 | 호아파참 | kisara:normal | DamageP×1.45 | Rising | None | 1 / 1 | None |
| kisara.2 | 피어싱 로어 | kisara:normal | DamageP×1.65 | Thrust | None | 1 / 1 | None |
| kisara.3 | 라이온즈 하울 | kisara:normal | DamageP×1.85 | Burst | None | 3 / 1 | None |
| kisara.4 | 플레이밍 메테오 | kisara:normal | DamageP×2.05 | Meteor | Fire | 3 / 1 | None |
| kisara.5 | 이그니어스 디스차지 | kisara:normal | DamageP×2.25 | Burst | Fire | 3 / 1 | None |
| kisara.6 | 빙장화 | kisara:normal | DamageP×2.45 | Ice | Ice | 1 / 1 | None |

### 원작 기술명 · 전술 재설계 (37)

| ID | 에셋 표시명 | 소유 캐릭터/역할 | 효과 | Pattern | Element | Pulses / Effects | 기존 전용 |
|---|---|---|---|---|---|---|---|
| boss.demitel.0 | Hell Gem | demitel:normal | DamageP×1.4 | Slash | Dark | 1 / 1 | None |
| boss.demitel.1 | Summon Demon | demitel:normal | — | Burst | None | 1 / 0 | None |
| boss.jahmir.0 | Tractor Beam | jahmir:normal | DamageM×1, Status:Slow | Gravity | None | 1 / 2 | None |
| boss.jahmir.1 | Grave | jahmir:normal | DamageP×1.4 | Pillar | Earth | 1 / 1 | None |
| boss.jahmir.2 | Summon Demon | jahmir:normal | — | Burst | None | 1 / 0 | None |
| boss.shizel.0 | Fear Flare | shizel:normal | DamageM×1.2 | Burst | Dark | 1 / 1 | None |
| boss.shizel.1 | Summon Demon | shizel:normal | — | Burst | None | 1 / 0 | None |
| boss.shizel.2 | Eternal Finality | shizel:normal | DamageM×2.4 | SwordFinale | None | 1 / 1 | None |
| boss.hyades.0 | Spark Wave | hyades:normal | DamageM×1.1 | Burst | Lightning | 1 / 1 | None |
| boss.hyades.1 | Earth Shaker | hyades:normal | DamageP×1.4 | Slash | Earth | 1 / 1 | None |
| boss.hyades.2 | Thunder Blade | hyades:normal | DamageM×1.7 | Lightning | Lightning | 1 / 1 | None |
| boss.rassius.0 | Dragon Swarm | rassius:normal | DamageP×0.8, DamageP×0.8, DamageP×0.8 | Slash | None | 3 / 3 | None |
| boss.rassius.1 | Dragon Flash | rassius:normal | DamageP×1.8 | Thrust | None | 1 / 1 | None |
| boss.van.0 | Radiant Dragon | van:normal | DamageP×1.4 | Wave | None | 1 / 1 | None |
| boss.van.1 | Imperial Slaughter | van:normal | DamageP×0.8, DamageP×0.8, DamageP×0.8 | Slash | None | 3 / 3 | None |
| boss.asch.0 | Rending Saber | asch:normal | DamageP×2 | SwordFinale | None | 1 / 1 | None |
| boss.legretta.0 | Rage Laser | legretta:normal | DamageP×1.4 | Wave | Light | 1 / 1 | None |
| boss.legretta.1 | Holy Lance | legretta:normal | DamageM×1.6 | Pillar | Light | 1 / 1 | None |
| boss.legretta.2 | Prism Ballet | legretta:normal | DamageP×1.4 | AstralRain | Light | 3 / 1 | None |
| boss.artorius.0 | Ascension | artorius:normal | DamageM×1.5 | Burst | Light | 1 / 1 | None |
| boss.artorius.1 | Savage Wolf Fury | artorius:normal | DamageP×0.8, DamageP×0.8, DamageP×0.8 | SwordFinale | None | 3 / 3 | None |
| boss.shigure.0 | Rangetsu Eagle | shigure:normal | DamageP×2 | SwordFinale | None | 1 / 1 | None |
| boss.oscar.0 | Heavenly Torrent | oscar:normal | DamageP×1.4 | Slash | None | 1 / 1 | None |
| boss.vholran.0 | Tidal Wave | vholran:normal | DamageP×1.4 | Wave | Water | 1 / 1 | None |
| boss.vholran.1 | Lex Talionis | vholran:normal | DamageP×1.8 | Slash | None | 1 / 1 | None |
| boss.balseph.0 | Pyroclasm | balseph:normal | DamageP×2 | Meteor | Fire | 1 / 1 | None |
| boss.ganabelt.0 | Mine Barrage | ganabelt:normal | — | Burst | Light | 1 / 0 | None |
| boss.ganabelt.1 | Rage Laser | ganabelt:normal | DamageM×1.7 | Wave | Light | 1 / 1 | None |
| boss.ganabelt.2 | Indignation | ganabelt:normal | DamageM×2.5 | Lightning | Light | 1 / 1 | None |
| boss.origin.0 | Collapse | origin:normal | DamageM×1.7 | Burst | Lightning | 1 / 1 | None |
| boss.origin.1 | Barrier | origin:normal | Status:Guard | Burst | None | 1 / 1 | None |
| boss.maxwell.0 | Meteor Swarm | maxwell:normal | DamageM×1.8 | Meteor | None | 1 / 1 | None |
| boss.maxwell.1 | Tractor Beam | maxwell:normal | DamageM×1.3, Status:Slow | Gravity | None | 1 / 2 | None |
| boss.maxwell.2 | Cyclone | maxwell:normal | DamageM×1.2, Push | Burst | Wind | 1 / 2 | None |
| boss.dhaos.0 | Dhaos Laser | dhaos:normal | DamageM×1.65 | Wave | None | 1 / 1 | None |
| boss.dhaos.1 | Dhaos Blast | dhaos:normal | DamageM×1.2, Push | Burst | None | 1 / 2 | None |
| boss.dhaos.2 | Tetra Assault | dhaos:normal | DamageP×0.65, DamageP×0.65, DamageP×0.65, DamageP×0.65 | Slash | None | 4 / 4 | None |

### 프로젝트 창작 보스 기술 (23)

| ID | 에셋 표시명 | 소유 캐릭터/역할 | 효과 | Pattern | Element | Pulses / Effects | 기존 전용 |
|---|---|---|---|---|---|---|---|
| boss.demitel.2 | 마력 착취 | demitel:normal | — | Burst | None | 1 / 0 | None |
| boss.rassius.2 | 기사의 약속 | rassius:normal | Status:Guard | Burst | None | 1 / 1 | None |
| boss.van.2 | 복제의 성역 | van:normal | — | Burst | None | 1 / 0 | None |
| boss.asch.1 | 검술 연격 | asch:normal | DamageP×0.9, DamageP×0.9 | Slash | None | 2 / 2 | None |
| boss.asch.2 | 간파의 자세 | asch:normal | Status:Guard | Burst | None | 1 / 1 | None |
| boss.artorius.2 | 감정 봉인진 | artorius:normal | Gauge | Burst | None | 1 / 1 | None |
| boss.shigure.1 | 발도 반격 | shigure:normal | Status:Guard | Burst | None | 1 / 1 | None |
| boss.shigure.2 | 일도 양단 | shigure:normal | DamageP×2.3 | Slash | None | 1 / 1 | None |
| boss.oscar.1 | 성료 기사의 지휘 | oscar:normal | — | Burst | None | 1 / 0 | None |
| boss.oscar.2 | 돌격 명령 | oscar:normal | DamageP×1.4 | Thrust | None | 1 / 1 | None |
| boss.vholran.2 | 빙결 추격 | vholran:normal | DamageM×1.3, Status:Root | Burst | Ice | 1 / 2 | None |
| boss.balseph.1 | 폭군의 내려찍기 | balseph:normal | DamageP×1.8 | Slash | Fire | 1 / 1 | None |
| boss.balseph.2 | 불타는 균열 | balseph:normal | DamageP×1.1 | Slash | Fire | 1 / 1 | None |
| boss.ordo.0 | 기록 재생 | ordo:normal | DamageM×1.7 | Burst | None | 1 / 1 | None |
| boss.ordo.1 | 인과의 봉합 | ordo:normal | — | Burst | None | 1 / 0 | None |
| boss.ordo.2 | 영원한 정지 | ordo:normal | DamageM×1.5, Status:Stun | TimeStop | None | 1 / 2 | None |
| boss.executor.0 | 경계 절단 | executor:normal | DamageP×1.4 | Thrust | None | 1 / 1 | None |
| boss.executor.1 | 처형 표식 | executor:normal | DamageP×2.1 | Thrust | None | 1 / 1 | None |
| boss.executor.2 | 방위 고정 | executor:normal | Status:Guard, Status:Root | Burst | None | 1 / 2 | None |
| boss.origin.2 | 사방의 계약 | origin:normal | DamageM×1.1, Push | Burst | None | 1 / 2 | None |
| boss.golem.0 | 지각 강타 | golem:normal | DamageP×1.3, Push | Slash | None | 1 / 2 | None |
| boss.golem.1 | 핵 재충전 | golem:normal | HealP×2 | Burst | None | 1 / 1 | None |
| boss.golem.2 | 유적 붕괴 | golem:normal | DamageP×1.4 | Slash | None | 1 / 1 | None |

### 적/전술 · 개별 원작 출처 미지정 (34)

| ID | 에셋 표시명 | 소유 캐릭터/역할 | 효과 | Pattern | Element | Pulses / Effects | 기존 전용 |
|---|---|---|---|---|---|---|---|
| enemy.attack | 창 찌르기 | boundary-device:basicAttack, sentinel:basicAttack | DamageP×1 | CharacterStyle | None | 1 / 1 | None |
| wolf-attack | 송곳니 | wolf:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| slime-attack | 점액 타격 | slime:basicAttack | DamageM×1 | Automatic | Water | 1 / 1 | None |
| golem-attack | 바위 주먹 | golem:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| sentinel-attack | 창 찌르기 | sentinel:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| archer-attack | 유적의 화살 | archer:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| mage-attack | 그림자 탄 | mage:basicAttack | DamageM×1 | Automatic | Dark | 1 / 1 | None |
| polwigle-attack | 꼬리치기 | polwigle:basicAttack | DamageM×1 | Automatic | Water | 1 / 1 | None |
| eggbear-attack | 곰의 일격 | eggbear:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| penguinist-attack | 서리 부리 | penguinist:basicAttack | DamageM×1 | Automatic | Ice | 1 / 1 | None |
| dhaos-attack | 다오스 레이저 | dhaos:basicAttack | DamageM×1 | Automatic | Light | 1 / 1 | None |
| dhaos-blast | 다오스 블래스트 | dhaos:normal | DamageM×0.9 | Automatic | Light | 1 / 1 | None |
| boss.demitel.attack | 공격 | demitel:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.jahmir.attack | 공격 | jahmir:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.shizel.attack | 공격 | shizel:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.hyades.attack | 공격 | hyades:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.rassius.attack | 공격 | rassius:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.van.attack | 공격 | van:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.asch.attack | 공격 | asch:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.legretta.attack | 공격 | legretta:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.artorius.attack | 공격 | artorius:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.shigure.attack | 공격 | shigure:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.oscar.attack | 공격 | oscar:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.vholran.attack | 공격 | vholran:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.balseph.attack | 공격 | balseph:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.ganabelt.attack | 공격 | ganabelt:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.ordo.attack | 공격 | ordo:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.executor.attack | 공격 | executor:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.origin.attack | 공격 | origin:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| boss.maxwell.attack | 공격 | maxwell:basicAttack | DamageP×1 | Automatic | None | 1 / 1 | None |
| tactic.heal | 응급 치료 | catalog:tactical | HealP×1.2 | Automatic | None | 1 / 1 | None |
| tactic.burst | 예고 화염진 | catalog:tactical | DamageM×1.5 | Automatic | Fire | 1 / 1 | None |
| tactic.charge | 예고 돌진 | catalog:tactical | DamageP×1.6 | Automatic | None | 1 / 1 | None |
| tactic.nova | 다오스 붕괴진 | catalog:tactical | DamageM×1.8 | Automatic | Fire | 1 / 1 | None |

## 카탈로그 변형과 공유 참조

다오스와 록 골렘은 `Enemies`의 기존 캠페인 에셋과 `CrossoverVariants`의 크로스오버 에셋을 별도로 보유한다. 히아데스 초반형은 같은 세 보스 기술을 공유한다. 경계 장치는 기본 Enemy의 `enemy.attack`을 참조한다. 같은 캐릭터 ID의 서로 다른 에셋 및 공유 기술을 JSON의 `characterAsset`과 `catalogGroup`으로 구분했다.

## 참조에서 제외된 기술 에셋

없음.

## 검증 범위

GUID 참조 누락 0, 고유 SkillData ID 중복 0, enum 범위 유효성을 확인했다. 모든 참조 Content 파일 SHA256을 JSON에 기록했다. 이 결과는 에셋 정적 인벤토리이며 Unity 테스트 결과가 아니다.
