# SD 전환 후속 보완 — 2026-10-27

합류·무료 훈련 안내, 일반 무기 7종, 실제 캠페인 20화 자동 진행, UI 1260조합 및 보스 방향 160조합의 구현·검증을 완료했다. 크레스 연속 동작 보정과 최종 일반 Windows 빌드·시작 검수도 완료했다. 현재 `develop` 작업이며 공개 릴리즈 완료 기록이 아니다.

## 완료한 구현과 실제 결과

| 항목 | 구현·실제 확인 범위 | 증거 |
| --- | --- | --- |
| 합류·무료 훈련 안내 | 실제 합류한 동료만 이름·이번 합류 배지 표시, 미합류 숨김 유지. 6명 편성을 바꾸지 않고 대기 인원 안내. 출전 중 저레벨 동료의 무료 훈련으로 연결하며 명시적 실행·저장 성공 후 적용 | [구현·회귀·실제 화면](../PreparationGuidance/README.md) |
| 일반 무기 7종 | 검 외 7유형의 초기 상품 추가. 메인 1화 완료·240G, 기존 에셋/전용무기/가격 보존. 실제 카탈로그·호환 유형·잠금·구매·착용·재로드·페이지/필터 회귀 통과 | [상품·설치·검증](../StandardWeapons/README.md), [원화 검수](../StandardWeapons/art-review.md) |
| 실제 Windows 20화 진행 | 새 격리 저장에서 실제 Director/UI 전투·전후 대화·보상·다음 장·타이틀 재로드 경로 20/20 완료. 구매24건/7320G, 모든 타이틀 왕복 전후 스냅샷 일치, 오류0 | [진행 결과·원본 보고서](../CampaignFlowReview/2026-10-27-results.md) |
| UI 경계·클릭 영역 | 실제 Windows 84상태 × 5해상도 × 글자3크기 = 1260/1260. 중복/누락 조합·경계/겹침 문제·실행 오류0. 새 안내 캡처8장 육안 확인, 원본4장 보관 | [최종 결과·PNG](../PreparationGuidance/README.md#최종-실제-windows-결과와-보관-캡처), [보고서](../PreparationGuidance/Validation/report.json) |
| 오리진/보스 방향 | 실제 Windows 보스20명 × 상태창/전장 × 4방향 = 160/160. 실제 sprite 일치·표시 정상, 오류0. 오리진 원본8장 육안 확인 및 보관 | [오리진 결과·8장·한계](origin-review.md), [전체 방향 보고서](Validation/origin/directions.json) |

20화 자동 진행은 단일 seed·고정 편성/구매 정책과 timeScale 4로 실행했다. 새 무기는 소총·방패·창·지팡이 4종이 이 경로에서 실제 구매됐으며, 나머지3종은 별도 상점 UI 회귀에서 확인했다. 무료 합류 훈련은 이 자동 캠페인에서 사용하지 않았다. 최종 출전6명의 레벨은10~41로, 해당 결과를 사람의 난이도·지원 역할 성장 체감·적정 전투 시간 인증으로 사용하지 않는다.

## 실제 Unity 검사와 이력

| 실행 | 결과 | 원본 |
| --- | --- | --- |
| EditMode, 최종 v6 적용 상태 | 387/387, 5.82초, 실패·건너뜀·미결정0 | [JSON](Validation/editmode-387-v6-final.json) |
| 전체 PlayMode | 244/244, 648.40초, 실패·건너뜀·미결정0 | [JSON](../CampaignFlowReview/2026-10-27/tests/playmode-244.json) |
| 배지 보정 뒤 합류 안내 관련 PlayMode | 2/2, 5.57초. 합류 조회/미합류 숨김, 6인 편성 보존·훈련 모드 읽기 전용 검사 | [JSON](Validation/tests/playmode-guidance-final.json) |

위 실행들은 서로 다른 시점의 실제 Unity 검사다. 후속 관련2개를 전체244개 재실행으로 세지 않으며, 전체 PlayMode 후 배지 관련2개를 재검사했고, 마지막 아트 변경은 전체 EditMode와 실제12동작으로 확인했다. `Tools/ManagedChecks` 결과와도 구분한다.

초기 실패는 삭제하거나 통과로 바꾸지 않았다. v5 초기 EditMode는386/387로, `cless_14_1` 몸/무기 비율1.65368581이 상한1.65를 초과했다. [초기 실패 JSON](Validation/tests/editmode-v5-initial-failure.json)을 보관하고 v5/220 실제387통과와 구분한다. 별도 batchmode PlayMode1건은 `WaitForEndOfFrame` 미지원으로 실패했으며 [원본](Validation/tests/playmode-batch-unsupported.json)을 보존한다. 이 실행을 게임 회귀 통과로 세지 않는다.

첫 UI 검수의 합류 배지/버튼 겹침30조합은 배지를 설명 대상 버튼의 자식으로 배치한 뒤1260조합을 다시 통과했다. 검사 기준을 완화하지 않았다. 첫 보스 방향 검수의 레그레타 비활성 모델은 fixture 시작 뒤 `RefreshViews()`와 활성 렌더러 확인을 보강한 후160조합 재실행으로 확인했다. 두 이력의 상세는 각 결과 문서에 남겼다.

## 크레스 최종 연속 동작

크레스80프레임은 원본 네 시트의 native RGBA를 정수 픽셀 이동으로 패킹했다. v6에서 생성 도구로 보완한 `cless_19_2` 한 프레임만 선택했고 나머지79프레임의 전체 메타데이터·RGBA가 v5와 같다. 후면 `cless_18_1`은 실제 부츠 위치와 루트 이동을 분리해 X 피벗만 보정했다. 원화 GUID/local ID·PPU220·Y 기준·전투 판정은 유지한다.

최종 Windows에서 일반 공격·마신검·궁극기 × 네 방향 **12/12**를 실행했다. 실제684연속PNG와12전장PNG를 생성했고 판정1회·독립 Resolver·격자·난수·6모델·방향·효과 정리·사용자/격리 저장 불변을 통과했다. 문제였던6프레임을 순서대로 열어 후면 좌우 튐과 복귀 머리 크기 변화를 재검수했다. 후면 양부츠 중심 전환은 이전 +40/-72px에서 **-15.75/-16px**로 같은 회수 방향이 됐다. 남은 웅크림·일어서기와 시점 차이는 손그림 포즈 변화로 수용했다. 모든 캐릭터의 모든 동작을 사람이 검수했다는 뜻은 아니다.

[최종 원본 결과](Validation/motion-final/result.json) · [원본 캡처 해시](Validation/motion-final/capture-manifest.json) · [실제 전장](Validation/motion-final/battlefield.png) · [후면 보정 후](Validation/motion-final/back-0059.png) · [복귀 포즈 보정 후](Validation/motion-final/right-0065.png) · [이전 v5 증거](Validation/motion-v5/result.json) · [제작·표적 육안 검수](../SdCharacters/2026-10-24/cless-native-review.md).

확대 캡처는 실제 실행 중 크레스만 별도 카메라로 렌더링한 화면이다. UI/효과/다른 유닛은 확대 카메라에서만 제외하며 실제 전투 시뮬레이션은 유지한다. PNG를 합성하거나 재인코딩하지 않았다. 선별 원본25장과 결과JSON을 보관했다. 제작 PNG는 기존 `Art/MotionPremium/cless.png`와 `Art/Crossover/origin.png`에 적용했고, 중복 생성 후보는 로컬 `Art/SdCharacters`에 그대로 두며 커밋에는 포함하지 않는다. 새 클론은 production 에셋으로 빌드할 수 있고, 원화 재생성/재임포트에는 문서의 원본 후보 준비가 별도로 필요하다.

제작 근거: [크레스 정적 검수](../SdCharacters/2026-10-24/cless-native-review.md), [원화 생성 이력](../SdCharacters/2026-10-24/cless-native-provenance.json), [오리진 생성 이력](../SdCharacters/2026-10-24/origin-clarity-v2-provenance.json), [오리진 적용 측정](../SdCharacters/2026-10-24/origin-clarity-v2-production.json). 오리진의72포즈 정적 제작 검토와 이번 실제4방향 표시 검수는72포즈 전체의 연속 동작 검수와 다르다.

## 최신 일반 빌드와 남은 검수

최종 일반 Windows 빌드 **46.79초·오류0·경고2**, 검수 빌드 **37.73초·오류0·경고3**로 성공했다. 일반 DLL은 검수/Fixture/Editor 타입을 포함하지 않는다. 원화159장·4,898스프라이트의 실제 참조·rect·outline·PPU·pivot을 다시 검사했다. [일반 빌드](Validation/build-windows-complete.json), [경고 내역](Validation/build-windows-complete-messages.txt), [검수 빌드](Validation/build-review-v6.json), [타입 분리](Validation/player-type-isolation-complete-retry.json), [최종 아트 감사](Validation/production-audit-final.json).

첫 시작 검사는20초 내 창 핸들을 관찰하지 못해 실패했으며 자기 프로세스만 종료했다. [초기 원본](Validation/windows-startup-complete.json)을 유지한다. 재실행은5.02초부터60.02초까지 창/응답 정상, 오류 로그0·정상 종료(exit0, 강제종료 없음)였다. 자동 저장으로 `WindowMaximized true→false`, `SeenContextHints` 누락→기본값0만 바뀌었으므로, 시작 검수 JSON의 저장 불변 실패를 통과로 바꾸지 않았다. 원본 해시와 같은 백업으로 해당 설정 파일 하나만 바이트 복원했고 **사용자8파일 최종 SHA256 일치**를 별도로 확인했다. [재실행 원본](Validation/windows-startup-complete-retry.json), [복원 내역·최종 파일 비교](Validation/user-files-restored-final.json), [기존 프로세스 보존](Validation/existing-processes-complete-retry.json). 실행 검사에는 기존 비사용 후처리 셰이더 경고4줄이 있었으며 오류와 구분한다. 다른PC 성능이나 사람의 조작 검증으로 확대하지 않는다.

반복 메달 보상은 [분산 설계](../StandardWeapons/reward-options.md)만 작성했고 실제 지급은 바꾸지 않았다. 다른 PC/DPI·장시간 성능·사람이 이야기를 읽으며 진행하는 전체 캠페인, 가격/장비 구매와 무료 훈련 안내의 체감 판단은 별도 검수다. 기존 저장 형식과 EXP/보상/전용무기 수치는 이번 후속 보완에서 바꾸지 않았다.
