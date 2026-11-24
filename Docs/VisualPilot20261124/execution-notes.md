# 실행 이력과 증거 범위

## 최종으로 사용할 실제 증거

- Logs/VisualPilot20261124/BeforeCapture/Episode06/result.json: 기존 제품의 실제 메인6화 12장.
- Logs/VisualPilot20261124/ActualEpisode06FinalV2/result.json: 수정 후 실제 메인6화 14장. 현재 원화의 머리 기준점·그림자 일치, 저장·캠페인 보존 확인.
- Logs/VisualPilot20261124/MotionBeforeFinal/result.json 및 MotionAfterFinal/result.json: 각297장. 같은 고립 배우·카메라에서 기존/새 원화를 비교하며 실제 전투 명령을 실행하지 않는다.
- Logs/VisualPilot20261124/comparison-final-browser-check.json: 네 방향·공격/피격·재생 진행·로딩·오류0을 확인한 비교 페이지 검사. 게임 테스트 수에 합산하지 않는다.
- 공개 Evidence에는 최종 원본 PNG17장을 바이트 그대로 복사하고 source result SHA256·파일 SHA256·카메라 조건을 manifest.json에 보존했다. 합성하거나 중간 프레임을 생성하지 않았다.

## 초안 및 실패 이력

1. 초기 넓은 환경 검수 목록에는 목표인 메인6화가 없었다. 기존90장 묶음은 목표6화의 전후 증거로 사용하지 않고 별도 실제6화 baseline12장을 촬영했다. nested 배열 직렬화가 부족했던 초기 보고서도 최종 증거와 구분한다.
2. 신규 실제 에셋 검사에서 NUnit 컬렉션 assertion에 Sprite를 넘겨 컴파일 오류가 발생했다. 참조 집합의 Contains 검사로 수정했으며 이후 실제 부분7개·전체710개 EditMode를 통과했다.
3. 초안에서 HP바가 새 머리보다 오른쪽에 놓이고 피격 회복 시 옛 대기 그림이 섞이는 것을 발견했다. 새32개 머리 기준점과 새 대기 복귀를 적용해 최종 촬영을 다시 했다. 초안 MotionBefore262장·MotionAfter289장은 남겨 두고 최종297장씩으로 대체했다.
4. 첫 최종 전장 검사 도구가 기존 이동 원화에도 새 authored-head 항목을 필수로 요구했다. 기존 방향별 fallback은 정상이다. 새 pilot32개에만 필수 조건을 적용한 FinalV2는14/14를 통과했다. 제품 코드는 이때 변경하지 않았다.
5. 공개 갤러리 생성 도구가 공격 phase를 하나의 이름으로 예상해 멈췄다. 실제 attack-prepare/attack-release 값을 반영해 성공했다. 부분 생성 파일은 Logs/VisualPilot20261124/EvidenceAttempt1에 보존했다.
6. 기본 셸·로컬 이미지 미리보기·CUA 초기화가 이 환경에서 실패했다. 승인된 로컬 셸과 System.Drawing 메모리 미리보기, 별도 headless Edge로 확인했다. 원본 PNG는 수정하지 않았다.

7. 첫 최종 보호 검사는 사용자 폴더 전체의 추가·변경0을 요구해 실패했다. 실제 저장9개와 제품·음원·기존 APK는 보존됐지만 전체 회귀 테스트가 StorageTests에65파일·35폴더를 만들고 TestResults.xml을 갱신했다. PUBLIC의 측정 CSV2개도 테스트가 갱신했다. 첫 실패 보고서는 덮어쓰지 않으며, CSV 생성본을 보존하고 초기 바이트로 복구했다. 최종 보존 검사는 처음 관측한 테스트 산출물의 정확한 해시와 출처를 별도 확인하고, 나머지 기존5,531파일·실제 저장9개·기존 사용자 변경에 대한 원본 바이트 검사를 유지해20조건을 통과했다.

실패·초안·도구 검사를 최종 Unity 통과 결과와 합산하지 않는다. Tools/ManagedChecks를 Unity 테스트로 표현하지 않는다.

## 보호 및 공개 범위

작업 시작 시 실제 저장/설정9개, 기존 APK3개, root/public의 보호 파일과 현재 사용자 트리의 해시를 보존했다. 테스트가 만드는 파일은 기존 저장 변경과 별도로 집계한다. [최종 보존 요약](Validation/protection-summary.json)은 원본 사용자 수정12개·검증본 기존 수정23개·음원·실제 저장9개·기존 APK3개·검증본 설정/폰트29개와 동결된 제품28개를 확인했다. 진단파일65개·폴더35개 추가 및 TestResults.xml1개 변경은 그대로 남기고, 기존 파일 삭제0·예상외 기존 파일 변경0을 별도로 확인했다. 첫 엄격 검사 실패4조건과 원문 해시를 보존했으며, 이전 TestResults.xml은 초기 해시만 확보했으므로 복원했다고 표현하지 않는다. 상세 사용자 경로/파일목록과 보호 바이트 사본은 공개하지 않는다.

원본 develop은 사용자 음원 이력을 로컬에 유지한다. 원격에는 별도 공개 checkout에서 현재 공개 develop을 단일 부모로 한 소스 커밋만 올린다. LocalMusic·새 APK/ZIP·비공개 로컬 부모를 포함하지 않으며 main·태그·과거 원격 이력을 변경하지 않는다.
