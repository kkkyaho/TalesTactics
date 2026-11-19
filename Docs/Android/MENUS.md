# Android 설정·도움말 개선 — 2026-11-20

**상태: 구현·전체 Unity 검사·화면 확인·Windows/Android 빌드·최종 보존 검사 완료.** 실제 휴대폰 검증은 미완료다.

## 바뀐 동작

- Android 설정에서 실제 적용되지 않는 PC 해상도와 창 모드 선택을 숨겼다. PC에서는 기존 표시 설정을 유지한다.
- 모바일 설정·조작 페이지의 버튼과 탭을 60 Canvas 단위 높이로 배치하고 창을 늘렸다. 닫기 버튼과 탭이 겹치지 않도록 위치를 분리했다. 화면의 실제 터치 크기와 사용성은 기기에서 확인해야 한다.
- 음악·효과음·타이밍·턴 순서·효과 등의 설정은 초안 상태에서 편집하고 적용 시 저장한다. 적용하지 않고 닫으면 초안을 버린다. 모바일 설정은 기존 `settings-mobile.json`에 저장하며 PC 설정 파일을 바꾸지 않는다.
- 조작 페이지는 이동·대상 터치 후 명시적 확정, 한 손가락 전장 이동과 두 손가락 확대·축소를 안내한다. 글자 설정의 저장 결과도 표시한다.
- 도움말의 높이·방향, 상태이상, 궁극기, 편성·성장, 대화 페이지에서 V 키·즉시 클릭 공격·Space 타이밍·마우스 휠·키보드 대화 안내를 각각 임무의 위험 버튼·예측 후 실행·타이밍 버튼·목록 드래그·대화 버튼 안내로 바꿨다. PC 설명과 공통 전투·성장·저장 규칙을 보존한다.

제품 변경은 `BattleSystemHud.cs`, `BattleHelpHud.cs`이며 기존 Android 입력·UI·저장 기반 구현을 사용한다. 도움말 전체 12쪽의 모바일 입력 안내를 코드에서 대조했다. 도움말 탭·편성·상점 등 모든 메뉴를 이번에 확대하거나 실기기 검수한 것은 아니다.

## 확인한 범위와 결과

- 전체 [EditMode 703/703·22.92초](Validation/Menus/editmode.json), [PlayMode 319/319·709.08초](Validation/Menus/playmode.json). 실패·건너뜀·미결정 0.
- 신규 메뉴 [2/2·15.67초](Validation/Menus/menu-tests.json)는 전체 319개에도 포함된다. 초안 취소·적용·재로드와 PC 설정 보존, 메뉴 경계·겹침·텍스트 영역·모달 차단·터치 도움말 연결을 확인했다.
- [화면 56/56조건](Validation/Menus/capture-report.json): 1280×720/2340×1080, 글자 100/130%, 설정·조작·도움말 14페이지. 검사 문제 0건. [대표 화면 검토](Validation/Menus/visual-review.json).

일반 Windows 빌드는 90.42초·오류 0·경고 4로 성공했고, 검수·테스트 코드 제외를 확인했다. 일반 EXE를 직접 실행한 결과는 아니다.

음원 제외 Android 빌드는 323.45초·오류 0·경고 6로 성공했다. APK는 214,429,140바이트이며 패키지·ARM64·가로 화면·서명·ZIP 정렬/CRC 검사를 통과했다. 전체 PlayerSettings 복원과 Android IL2CPP 입력 DLL의 검수·테스트 코드 제외도 확인했다.

APK SHA-256: `f245f37069cc2b59861f910b2549c543a3193d0c7f44cebfc6f7e7c24f932478`. 검증용 로컬 산출물이며 새 APK를 업로드하지 않았다. BuildReport 크기와 실제 APK 파일 크기는 서로 다른 집계다.

원본 설정·음원·기존 APK 16개, 실제 저장/설정 9개, 검증본의 영구 설정·메타·폰트 1,221개를 최종 비교해 보존을 확인했다. 작업본·검증본 소스/meta 35개도 일치한다. 시작 기준에 포함된 임시 테스트 장면 메타 1개의 정상 정리는 별도로 기록했다. 테스트 산출물 변화도 별도 집계하며 전체 사용자 폴더 불변으로 표현하지 않는다.

[Windows 빌드](Validation/Menus/windows-build.json) · [Windows 코드 제외](Validation/Menus/windows-player-type-isolation.json) · [Android 빌드](Validation/Menus/android-build.json) · [APK 검사](Validation/Menus/apk-verification.json) · [Android 코드 제외](Validation/Menus/android-managed-isolation.json) · [보존 요약](Validation/Menus/final-preservation.json)

연결 휴대폰 0대로 실제 설치·터치·앱 중단/복귀·성능·음원 재생은 미검증이다. Editor 검사와 APK 빌드 성공을 실기기 플레이 성공으로 표현하지 않는다. 화면 검사는 Windows Editor의 터치 모드이며 실제 노치·손가락 조작·물리적 버튼 크기 확인이 아니다. [초기 실패·중단·재실행 이력](Validation/Menus/execution-notes.md)을 보존한다.

## 소스 반영과 배포 범위

Windows·Android 빌드와 최종 보존 검사를 통과한 음원 없는 소스만 공개 develop 반영 대상으로 삼는다. 실기기 검증과 정식 릴리즈는 별도 미완료다.

원본 로컬 develop은 음악 포함 이력을 보존한다. 공개 변경에는 당시 원격 develop만 단일 부모로 연결하며, 원본의 비공개 음악 포함 조상을 merge하거나 직접 푸시하지 않는다. [소스 전용 반영 방식](https://github.com/kkkyaho/TalesTactics/blob/808d4331aa24a6466062d898574e575073e32afc/Docs/SourceOnlyPublish/README.md)을 따른다.

음원 공개 예외는 [음원 정책](../LocalMusicPolicy.md)에 명시된 2026-11-19 APK 한 개와 체크섬뿐이다. 이번 메뉴 변경으로 다시 만든 음원 포함 APK, MP3 원본과 음악 포함 소스 이력에는 그 승인이 확대되지 않는다. 기존 공개 다운로드는 이번 메뉴 개선을 포함하지 않는다.
