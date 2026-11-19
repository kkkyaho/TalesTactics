# Android 병행 개발 검증 — 2026-11-20

## 2026-11-20 메뉴 개선 — 구현·검증 완료

실제 [EditMode 703/703·22.92초](Validation/Menus/editmode.json), [PlayMode 319/319·709.08초](Validation/Menus/playmode.json), 화면 56/56조건을 통과했다. 별도 신규 메뉴 2개는 전체 PlayMode에도 포함된다.

일반 Windows 빌드는 90.42초·오류 0·경고 4로 성공했고, 검수·테스트 코드 제외를 확인했다. 일반 EXE를 직접 실행한 결과는 아니다. 음원 제외 Android 빌드는 323.45초·오류 0·경고 6로 성공했다. APK는 214,429,140바이트이며 패키지·ARM64·가로 화면·서명·ZIP 정렬/CRC 검사를 통과했다. 전체 PlayerSettings 복원과 Android IL2CPP 입력 DLL의 검수·테스트 코드 제외도 확인했다.

원본 설정·음원·기존 APK 16개, 실제 저장/설정 9개, 검증본의 영구 설정·메타·폰트 1,221개를 최종 비교해 보존을 확인했다. 작업본·검증본 소스/meta 35개도 일치한다. 시작 기준에 포함된 임시 테스트 장면 메타 1개의 정상 정리는 별도로 기록했다. 테스트 산출물 변화도 별도 집계하며 전체 사용자 폴더 불변으로 표현하지 않는다. 연결 휴대폰 0대로 실제 설치·터치·앱 중단/복귀·성능·음원 재생은 미검증이다. Editor 검사와 APK 빌드 성공을 실기기 플레이 성공으로 표현하지 않는다.

[Windows 빌드](Validation/Menus/windows-build.json) · [Windows 코드 제외](Validation/Menus/windows-player-type-isolation.json) · [Android 빌드](Validation/Menus/android-build.json) · [APK 검사](Validation/Menus/apk-verification.json) · [Android 코드 제외](Validation/Menus/android-managed-isolation.json) · [보존 요약](Validation/Menus/final-preservation.json)

[구현 범위·실패 이력·남은 한계](MENUS.md). 이번 검증용 APK는 로컬에만 보관하며 기존 공개 APK에는 이번 메뉴 변경이 없다. 아래 결과는 2026-11-19 기반 구현의 과거 검증이다.

## 2026-11-19 기반 구현과 실행 결과

Windows·Android가 전투·성장·캠페인·저장 규칙을 공유하도록 모바일 터치, 안전영역, 전투 명령 UI, 명령 경계 자동 저장, 설정 분리와 APK 빌드 경로를 추가했다. 사용자 작업본의 빌드 대상은 전환하지 않고 음원이 없는 별도 검증 checkout에서 Unity 6000.6.0f1을 실행했다.

- 설정 복원 보완 전 실제 Unity [EditMode 703/703·38.4초](Validation/full-editmode-final.json), [전체 PlayMode 317/317·726.49초](Validation/full-playmode-final.json). 실패·건너뜀·미결정은 모두 0이다.
- 위 317개에 신규 모바일 [17/17](Validation/mobile-tests-final-subset.json)이 포함된다. TouchState 입력, 탭/드래그/핀치/UI 차단, 안전영역 재진입, 실제 Scene UI, 명령 경계 저장·실패 복구·재개·보상 중복 방지를 검사했다. 별도 17개를 더 실행한 결과로 합산하지 않으며 Android 실기기 결과가 아니다.
- 일반 Windows [빌드 성공·106.32초·오류 0·경고 4](Validation/windows-build-final.json). 경고는 Pipeline 런타임 설정, 기존 검수 코드의 지시문, 디버그 셰이더 2개 제거 안내다. [실행본·DLL·BuildReport 해시](Validation/windows-build-manifest.json)와 [Editor·테스트·검수 코드 제외](Validation/windows-player-type-isolation.json)를 확인했다. 이번 일반 EXE를 직접 실행한 결과는 아니다.
- Windows Editor의 모바일 조작 모드로 [최종 화면 6장](Validation/visual-review.json)을 육안 확인했다. 1920×1080 타이틀·거점·명령·이동 확정, 2340×1080 명령·이동 확정이며 [화면 밖 버튼·텍스트 영역 초과 0](Validation/capture-final-report.json)이다. 임시 GameView 크기 2개를 제거하고 선택 크기를 복원했다. 휴대폰·노치·물리적 버튼 크기·모든 메뉴의 검증으로 확대하지 않는다.

## Android 설치와 최종 APK

[Android Build Support·SDK·NDK·OpenJDK 설치](Validation/android-module-install-result.json)를 완료했다. [설치 상태](Validation/android-install-state.json)는 JDK 17.0.18+8·NDK 27.2.12479018·ADB 36.0.0이며 연결 기기는 0대다.

설정 복원 보완 후 [두 번째 APK 빌드](Validation/android-build-final.json)가 성공했다. 사용자 제공 음원 없는 별도 검증 checkout의 ARM64·IL2CPP 빌드다.

| 항목 | 최종 결과 |
| --- | --- |
| 실행 ID | `0ee6b446b2524efe90b13d2ddbd5dd82` |
| 시작 UTC | `2026-11-19T06:29:27.4237717Z` |
| BuildReport | Succeeded·278.0694267초·오류 0·경고 2 |
| 실제 APK 크기 | 214,425,556바이트 |
| APK SHA256 | `91345F4E907DAFCE6A9EF066FBCDF6E4037108DFF1B642B006A2CE31D87E1483` |
| BuildReport sizeBytes | 1,211,521,723: 별도 집계값으로 실제 APK 크기가 아님 |

[최종 APK 파일 검사](Validation/android-apk-validation.json)에서 aapt 패키지 정보·테스트 서명·zipalign을 통과했다. 경고 2개는 Pipeline 런타임 설정 누락과 기존 LiberationSans 글꼴 importer 버전 안내다. [로컬 Android 테스트 APK](../../Builds/Android/TalesTactics.apk)를 준비했으며, 이후 사용자 명시 승인으로 [공개 테스트 다운로드](https://github.com/kkkyaho/TalesTactics/releases/tag/android-test-20261119)를 제공했다.

[최종 복원 검사](Validation/android-restoration-final.json)는 getter 17개·전체 PlayerSettings 직렬화와 텍스처 meta 1,192개·ProjectSettings 27개(총 1,219개 파일)의 원래 해시 일치를 확인했다. SaveAssets·Editor 정상 종료 뒤에도 PlayerSettings SHA256은 `EA354DE3488BCBD365584E59160490794740F5E0E0ED5F1D6CCA7EE37CE26182`로 같고 텍스처 복구 일지도 남지 않았다. [Android IL2CPP 입력 DLL 정적 검사](Validation/android-managed-isolation-attempt2.json)는 15/15를 통과했고 첫 빌드의 런타임 DLL과 동일했다. 검수·Editor·테스트 코드 제외와 필수 Android 타입 유지를 검사한 결과이며 APK 네이티브 코드 실행이나 실기기 결과가 아니다.

첫 빌드 이력은 따로 보존한다. [첫 APK](Validation/android-build-final-attempt1.json)는 624.965초·오류 0·경고 6으로 성공했고 [파일 검사](Validation/android-apk-validation-attempt1.json)도 통과했다. 당시 [복원 검사](Validation/android-restoration-final-attempt1.json)에서 getter 17개와 텍스처 meta 1,192개는 같았지만 PlayerSettings 직렬화 차이 2개가 남았다. 첫 복원 결과를 전체 설정 복원 성공으로 바꾸지 않는다.

**Android 실기기 실행은 미완료다.** APK 생성·서명 확인은 설치·실행·프레임·발열·메모리·음악 복귀의 검증을 대신하지 않는다. [실기기 확인표](DEVICE-TEST.md)는 모두 미실행 상태다.

## 실패·재검과 도구 상태

1. 초기 빌드 보고서의 오류/경고 개수 타입을 Unity API에 맞게 수정한 뒤 컴파일을 통과했다.
2. 최초 모바일 [8/11·3실패](Validation/mobile-tests-result-1.json)는 UI Raycaster 등록 대기, 열 수 없는 메뉴를 모달로 사용한 전제, 복제 기술과 기본 공격의 취소 경로 차이였다. 실제 등록·유효한 도움말 모달·각 취소 경로를 검사하도록 보완했다.
3. 저장 검사 파일의 새 GUID가 33자리여서 Unity가 파일을 제외했다. 미실행 6개를 성공으로 세지 않고 32자리 고유 GUID로 수정·가져오기했다.
4. 두 번째 모바일 [14/17·3실패](Validation/mobile-tests-result-2.json)는 배열 첫 유닛을 활성 유닛으로 가정한 검사와 이미 자동 재개한 종료 대화를 다시 재개한 검사였다. 실제 활성 ID·전체 체크포인트·디스크 재로드 후 대화와 보상 보존을 확인하도록 고쳤다. 제품 저장 규칙을 완화하지 않았다. 이후 별도 [17/17·30.94초](Validation/mobile-tests-final.json)를 통과했고, 최종 UI 수정 뒤에는 위 전체 317개 실행에서 다시 통과했다.
5. 전체 PlayMode 두 번의 [0개 실행](Validation/full-playmode-zero-selection.json), [두 번째 0개 실행](Validation/full-playmode-zero-selection-2.json)은 미실행으로 보존했다. Unity Test Framework가 도메인 재로드를 끈 다음 Play 진입에서 assembly 캐시 List를 비운 뒤 다시 로드하지 않는 경로를 확인했다. 검증 Editor만 재시작했다. 이어 시작한 전체 실행은 안전영역 재진입 누락을 고치기 위해 [취소](Validation/full-playmode-interrupted-layout-fix.json)했으며 통과로 세지 않았다. 최종 317개는 새 Editor에서 한 번에 통과했다.
6. 화면 재진입 시 안전영역 재적용, 카메라 버튼 줄바꿈, 모바일 타이밍 안내·버튼 높이, 턴 표시 높이를 보완했다. 첫 [캡처 실행](Validation/capture-attempt1.json)은 두 번째 해상도에서 거점 진입을 생략한 fixture 문제로 실패했다. 캡처 준비를 통일했고 [턴 표시 수정 전 영역 초과](Validation/capture-before-phase-label.json)도 최종 재캡처에서 해소됐다.
7. Windows 빌드는 최초 예약이 실행되지 않아 [대기 콜백 1개 제거](Validation/windows-build-queue-cancel.json) 후 직접 호출했다. 직접 요청은 [30초 응답 시간 초과](Validation/windows-build-direct-request.json)였으나 Unity 빌드는 계속 진행했고, 중복 요청 없이 실제 성공 보고서를 회수했다.
8. 빌드 직후 같은 Editor의 [거점 캡처](Evidence/hub-after-build-glyph-cache-issue.png)에서 늘·날·편 등 일부 한글이 누락됐다. 이 글자들은 동적 fallback에 의존하며 TMP 빌드 전처리는 atlas를 비우지만 다른 폰트의 fallback 캐시까지 정리하지 않는 경로가 있다. 검증 Editor를 완전히 재시작하고 재빌드 없이 캡처하자 정상 복구됐다. Editor 캐시 불일치라는 근거이며 독립 실행본의 글꼴 검증을 대신하지 않는다. 제품 코드·원본 글꼴을 수정하지 않았다.

9. [첫 APK의 설정 복원 재검토](Validation/android-settings-attempt1-review.json)에서 overrideDefaultApplicationIdentifier가 0→1로 남고 비어 있던 il2cppCompilerConfiguration에 Android:1이 생긴 것을 확인했다. API getter가 같아도 기본값의 직렬화 형태는 달라질 수 있어, 기존 17개 API 복원 뒤 전체 PlayerSettings의 메모리 스냅샷을 복원하도록 보완했다. 원시 설정 JSON은 파일·로그에 기록하지 않는다.
10. [첫 설정 회귀 보고서](Validation/android-settings-regression-attempt1.json)는 passed=false였지만 중첩 케이스 결과가 누락돼 원인을 확인할 수 없었다. 보고 직렬화를 JsonConvert로 바꾼 뒤 동일한 enum·map 접근·fixture·복원 순서·비교 조건으로 재실행해 [두 케이스 모두 통과](Validation/android-settings-regression.json)했다. Android 키가 없는 기본값 상태와 Android Debug·Standalone 명시값 상태에서 복원 2회·멱등성·전체 JSON 해시·직렬화 flag/map·getter를 확인했고, 종료 시 실행 전 메모리 상태도 복원했다. 최초 실패 원인은 미확정으로 남긴다.
11. 첫 빌드가 남긴 위 2개 설정만 검증본에서 복구하고 SaveAssets·정상 종료 후 [파일 해시를 확인](Validation/android-settings-repair.json)했다. SHA256은 EA354DE3488BCBD365584E59160490794740F5E0E0ED5F1D6CCA7EE37CE26182다. 원본 사용자 설정은 수정하지 않았다.

12. 첫 APK와 두 번째 APK 모두 요청 도구의 CLI 30초·서버 180초 응답 시간 초과가 있었지만 Unity 빌드는 계속 진행했다. [첫 요청](Validation/android-build-request.json)과 [두 번째 요청](Validation/android-build-request-attempt2.json)을 보존하고 중복 빌드 요청 없이 실제 성공 보고서를 회수했다. [최종 Console](Validation/android-console-final.json)의 현재 Unity 상태는 오류 0·경고 2다. 도구 버퍼에 남은 시간 초과 오류 2건은 별도 이력이며 삭제하거나 빌드 오류로 합산하지 않았다.

## 보존·한계와 다음 확인

[Windows 검사 직후 보존](Validation/final-preservation.json)과 [첫 Android 빌드 뒤 보존 검사](Validation/android-final-preservation-attempt1.json)에 이어, [최종 보존 검사](Validation/android-final-preservation.json)도 파일·음원 등 750개와 실제 저장/설정 9개의 SHA256 일치를 확인했다. 최종 코드·meta 32개는 작업본과 검증본이 일치한다. 전체 Unity 테스트 뒤 C#·asmdef·shader 입력 440개 중 439개는 유지했고 Editor의 AndroidBuild.cs 1개는 설정 복원을 보완했다. 위 703/317 전체 테스트와 Windows 빌드는 이 보완 전 결과이며, 보완 후에는 설정 회귀 2개·두 번째 실제 APK 빌드·전체 설정 복원과 IL2CPP 입력 DLL 정적 검사를 확인했다.

검증 폴더의 동적 글꼴 캐시와 설정 줄바꿈 변경은 [기존 분석](Validation/generated-input-drift.json)과 [원상 복원 미적용 기록](Validation/generated-restore-not-applied.json)에 보존했다. 첫 Android 복원 검사에 함께 잡힌 ShaderGraph 줄바꿈·UnityConnect 설정 schema 자동 갱신은 PlayerSettings의 2개 부수효과와 구분한다. 사용자 작업본은 건드리지 않았으며 746개 전체 입력이나 사용자 전체 디렉터리가 불변이었다고 표현하지 않는다.

초기의 [Windows 대상 오실행 차단](Validation/android-windows-guard.json), [설정 해시 보존](Validation/android-guard-settings.json), [실패 보고서](Validation/android-guard-result.json)는 당시 사전 검사다. 이후 첫 APK와 설정 복원을 보완한 두 번째 APK의 실제 결과를 위에 별도로 기록했다. 두 번째 빌드의 설정·텍스처 메타데이터 복원은 정상 종료 후 파일 해시까지 확인했다.

카탈로그가 직접 참조하는 PNG 160장의 기본 최대 크기 기준 RGBA32 추정은 약 1,799.79MiB, ASTC 6×6 추정은 약 200.44MiB다. 밉맵·정렬·다른 리소스를 제외한 텍스처 데이터 추정이며 실제 프로세스 메모리나 지원 기기 보장이 아니다. 목표 30fps도 측정 결과가 아니다.

휴대폰에서 첫 전투·앱 전환·프로세스 종료·이어하기·오디오 포커스 복귀를 확인한 뒤 전체 40전투·10대화, 작은 화면의 모든 메뉴, 장시간 성능을 검증한다. 위 음원 제외 APK와 체크섬은 이후 사용자 명시 승인으로 [공개 테스트 다운로드](https://github.com/kkkyaho/TalesTactics/releases/tag/android-test-20261119)에 게시했다. 사용자 제공 음원 원본·음원 포함 소스 이력과 다른 빌드는 계속 로컬 전용이며, 별도로 승인한 음원 포함 APK 한 개의 공개 예외는 [음원 정책](../LocalMusicPolicy.md)을 따른다. 당시 실기기 미검증으로 코드 커밋·푸시와 정식 릴리즈를 보류했던 사실은 보존한다. 현재 소스 반영 기준은 이 문서의 2026-11-20 항목을 따르며 실기기 검증과 정식 릴리즈는 아직 완료하지 않았다.