# 알펜·6화 그래픽 시범 개선

2026-11-24 사용자 승인에 따라 **알펜 1명과 메인 6화 전장**에 시범 적용했다. 실제 전체 Unity 검사 710개·321개, PC·Android 빌드와 최종 보존 대조를 완료했다.

[실제 화면 전후 비교](Evidence/gallery.html) · [원본 PNG·촬영 조건·해시](Evidence/manifest.json)

## 적용 내용

- 알펜의 기본 대기·호흡·눈감음·복귀 4상태 × 4방향, 일반 공격의 준비·접촉·회수와 피격 4상태 × 4방향, 총 32개 원화를 적용했다.
- 5.4초 대기 주기 중 눈감음은 0.1초로 제한하고, 발 기준점을 유지했다. 실제 새 그림을 사용할 때만 기존 UV 대기 변형을 끈다.
- 메인 6화 「귀환의 대가」(CampaignStage 11)의 바닥 대비·굴곡·수목 명암·채도를 낮추고 작은 식생을 줄였다. 같은 유적 테마의 다른 스테이지에는 적용하지 않는다.
- 새 32개 포즈의 머리 기준점을 추가해 HP바를 정렬했다. 피격 자세 이후 남은 반동 구간에는 새 기본 대기 그림을 사용한다.

SD 비율, 은발·청색 천·검은 갑옷·주황색 검의 정체성과 현재 장별 조명을 유지했다. 기존 이동·기술·궁극기·전용무기 원화, 전투 시점과 수치, 저장 규칙, 지도 높이·충돌·목표는 유지한다. 전용무기용 새 원화가 없으면 기존 전용무기 외형을 사용한다.

일반 플레이에서는 메인6화에 알펜을 편성하고 전용무기가 아닌 기본 외형 장비로 확인한다. 다른 스테이지에서는 기존 원화가 표시된다.

## 실제 비교와 시각 평가

실제 6화 전투의 동일 편성·카메라로 개선 전 12장, 개선 후 14장을 촬영했다. 최종 14장에는 1280×720 네 방향, 이동·명령·기술 목록, 1024×768 및 2340×1080 가로 화면이 포함된다. 네 방향 모두 새 머리 기준점과 현재 원화에 맞는 투영 그림자를 확인했다.

동작은 개선된 전장 위의 별도 검사용 알펜에서 기존/새 원화만 전환해 각각 297개 실제 진행 프레임을 저장했다. 네 방향의 대기와 눈감음, 정면 일반 공격·피격·복귀를 관측했다. 최종 피격 0.351초 프레임은 새 대기로 복귀하며 옛 그림이 끼지 않는다. 이 촬영은 공격 판정이나 적의 반응을 발생시키지 않는 표현 검사이며 전체 전투 플레이와 구분한다.

로컬 연속 비교: [동작 재생·방향·속도 선택](../../Logs/VisualPilot20261124/motion-comparison-final.html). 이 링크는 로컬 작업본에서만 열리며 원본 594프레임은 원격에 추가하지 않는다. 공개 비교에는 원본 PNG 17장을 가공 없이 보존했다. PNG 촬영·저장 시간 때문에 이 시퀀스의 간격을 게임 성능 측정으로 사용하지 않는다.

최종 점수는 다음과 같다. 기존 화면도 같은 등급이므로 전반적인 품질 등급이 상승했다고 주장하지 않는다. 대기 표현과 바닥 정리는 개선됐고, 더 큰 변화에는 후속 원화·연출 작업이 필요하다.

palette discipline: 3/5 — 바닥의 세부 대비를 줄였으며 실제 재질 검사에서 기본 회색 재질·오류 shader가 없다.
style uniformity: 3/5 — 기존 SD 외곽선과 색상 정체성을 유지한다. 인물과 배경의 밀도 차이는 일부 남는다.
lighting depth: 3/5 — 기존 환경광·발 접촉·몸 그림자를 유지하며 새 원화가 그림자에 반영된다. 새 입체 조명을 추가한 것은 아니다.
silhouette: 3/5 — 일반 전투 거리에서 알펜이 구분되고 네 방향에서 잘림이 없다. 전장 전체를 축소하면 표정은 작다.
UI intentionality: 4/5 — HP바 중심 어긋남을 수정했고 명령·기술·가로 화면에서 새 잘림이 없다.
feedback visibility: 3/5 — 호흡·눈감음·공격 준비/접촉/회수·피격 복귀가 구분된다. 적·효과·소리가 맞물리는 타격감의 상향 평가는 미포함이다.

첫 화면을 최종본으로 쓰지 않았다. 초안에서 발견한 HP바 중심 어긋남, 피격 회복 중 옛 원화 노출을 수정한 뒤 전부 다시 촬영했다. 비교용 배우도 다른 유닛에서 떨어진 빈 칸으로 옮겨 구도를 명확히 했다. 최종 정지 화면과 동작 시퀀스를 별도로 읽기 검토했다.

재질 검사는 실제 6화의 renderer 449개에서 Default-Material·null shader·error shader 각각 0개, ParticleSystem 0개를 기록했다. 흰색 UI tint는 기존 남색·금색 SdPanel 원화의 색을 유지하는 용도이며, 글자는 의도한 미색·서체 크기를 사용한다. 이 관측은 HP 위치 보완 전 초안에서 얻었고 최종 수정에서 재질이나 UI 색은 바꾸지 않았다.

## 검증 기록

최종 [전체 EditMode 710/710·17.85초](Validation/editmode.json)와 [전체 PlayMode 321/321·761.16초](Validation/playmode.json)를 통과했다. 실패·스킵·미확정은 0개다. 새 검사 7개·2개는 이 전체 수에 포함된다. 별도 [부분 EditMode 7/7](Validation/targeted-editmode.json)·[부분 PlayMode 2/2](Validation/targeted-playmode.json)를 전체 수에 다시 더하지 않는다. 일반 [Windows 빌드](Validation/windows-build.json)는 95.82초·오류0·경고4, [Android 빌드](Validation/android-build.json)는 356.49초·오류0·경고6으로 성공했다. 시간은 BuildReport 기준이며 Android 대상 전환·텍스처 준비/복원에 걸린 전체 시간과 다르다.

[실제 산출물](Validation/artifact-proof.json)의 APK는 215,089,810바이트, SHA256은 b9b4352ec281c6c370afb269f4ccf167814fadc25b42b25f12a093dbf4056aca다. [패키지·ARM64·가로 화면·서명·ZIP CRC/정렬](Validation/apk-verification.json), [새 원화와 연결 에셋의 Android 포함](Validation/android-packed-pilot-art.json), [Windows 실행 코드 제외14조건](Validation/windows-code-isolation.json), [Android IL2CPP 입력 코드 제외15조건](Validation/android-code-isolation.json)을 확인했다. 코드 제외 검사는 APK 설치·네이티브 실행 검사와 구분한다.

Windows 경고4개는 Pipeline 설정 안내, 기존 검수 코드의 지시문, 내부 디버그 셰이더 제외2개다. Android 경고6개는 Pipeline 설정 안내, 기존 검수 지시문, LiberationSans importer 버전, TextMeshPro 대형 함수 C++ 분리 안내3개다. 경고 원문은 빌드 보고서에 보존했다.

[Editor 복원19조건](Validation/editor-state.json)을 확인했다. 검증본은 Win64·PC_RPAsset·Play 꺼짐·원래 자동 프레임 설정으로 돌아왔으며 임시 배우와 복구 대기 파일이 없다. [최종 보존 대조20조건](Validation/protection-summary.json)을 통과했다. 실제 저장/설정9개·ROOT 기존 수정12개·PUBLIC 기존 수정23개·설정/폰트29개·기존 APK3개·제품28개를 보존했고, 보호 목록3,289개에서 예상외 차이는0개다. [측정 CSV2개는 생성본을 보존하고 초기 바이트로 복구](Validation/csv-restoration.json)했다. 사용자 폴더에는 [회귀 테스트 산출물65파일·35폴더와 TestResults.xml 변경1개](Validation/regression-artifact-provenance.json)가 남으며, 기존 파일 삭제는0개다. 전체 사용자 폴더 불변으로 표현하지 않으며 첫 엄격 검사 실패도 보존했다.

[실행 이력과 한계](execution-notes.md)

## 원화와 재현

- [대기 원본](../../Assets/TalesTactics/Art/VisualPilot/Alphen/idle-v1.png), [공격·피격 원본](../../Assets/TalesTactics/Art/VisualPilot/Alphen/action-v1.png): 내장 이미지 생성 도구로 제작한 각각 1254×1254 PNG.
- [프롬프트 원문](art-prompts.md), [생성 방식·출처·원본 해시](art-provenance.json), [32개 rect·발 기준점·PPU](art-layout.json).
- [32개 머리 기준점 측정](head-anchors.json). 기존 168개 항목은 보존했다.
- 새 원본 PNG의 alpha와 픽셀은 유지하고 Unity Sprite metadata로 연결했다. PPU 188.5로 기존 몸체 크기에 맞췄다.
- 가져오기는 새 시트 두 장과 해시를 확인하는 VisualPilotArtImporter만 명시적으로 실행했다. DemoContent.Create는 실행하지 않았다.
- 실제 비교용 VisualPilotCapture와 importer는 Editor 코드다. 일반 실행본에서 제외 여부를 별도 검사한다.

## 남은 범위

전 캐릭터·전 스테이지 확대, 전용무기의 새 원화, 표면 굴곡 조명, 대표 기술 연출 추가는 미포함이다. 이번 시범의 동작과 바닥 방향을 실제로 플레이해 확인한 다음 확대한다.

연결된 Android 기기는 0대다. 휴대폰 설치·물리적 터치 크기·노치·앱 복귀·발열·지속 프레임·음원 재생은 아직 확인하지 않았다. Editor 가로 화면과 APK 빌드 성공을 실기기 플레이로 표현하지 않는다. 새 APK나 사용자 음원을 공개하지 않는다.
