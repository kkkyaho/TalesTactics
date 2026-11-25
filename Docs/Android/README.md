# PC·Android 병행 개발

2026-11-19 사용자가 Windows PC와 Android 모바일의 병행 개발을 요청했다. 전투 규칙·캐릭터·성장·캠페인 데이터·저장 형식은 공통으로 유지하며 입력·UI·표시 설정과 빌드를 플랫폼에 맞춘다.

## 2026-11-25 최신 APK 다운로드

동료10명의 새 모션과 모바일 메뉴 개선을 포함한 [최신 APK](https://github.com/kkkyaho/TalesTactics/releases/download/android-roster-test-20261125/TalesTactics-android-roster-test-20261125-arm64.apk)를 사용자 요청에 따라 게시했습니다. 사용자 제공6곡을 제외한 기본 음악 버전입니다. 로그인 없는 전체 다운로드·크기·해시 일치를 확인했습니다. [설치·검증·기존판 구분](DOWNLOAD.md). 실기기 플레이는 아직 확인하지 않았습니다.

## 2026-11-20 메뉴 개선 — 구현·검증 완료

Android의 PC 전용 표시 설정을 숨기고 설정·조작 버튼을 확대했으며 도움말을 터치 예측·명시적 실행 흐름에 맞췄다. 전체 EditMode 703/703·PlayMode 319/319, 화면 56/56조건, Windows·음원 제외 Android 빌드와 최종 파일 보존 검사를 통과했다. [변경·검증 결과](MENUS.md).

당시 실제 휴대폰 검증은 미완료였고 기존 공개 APK에 메뉴 개선은 포함되지 않았다. 2026-11-20에는 음원 없는 소스만 develop에 반영했으며, 이후 최신 APK 공개 결과는 위 2026-11-25 항목을 따른다. 원본 로컬 develop의 비공개 음원 조상을 공개 커밋에 연결하지 않는 [소스 전용 반영 방식](https://github.com/kkkyaho/TalesTactics/blob/808d4331aa24a6466062d898574e575073e32afc/Docs/SourceOnlyPublish/README.md)을 유지한다.

## 첫 Android 구현

- Android 가로 화면. 전장 탭은 이동 경로 또는 공격 예측을 선택하고 화면의 확정 버튼으로 실행한다. 반복 탭만으로 행동을 실행하지 않는다.
- 한 손가락 드래그로 전장 이동, 두 손가락 벌림/모음으로 확대·축소. UI에서 시작한 접촉과 취소된 접촉은 전장 행동으로 이어지지 않는다.
- 화면 안전영역에 주요 UI를 배치한다. 전투 명령·확정·취소 버튼과 방향 선택을 확대하고, 터치 UI에서는 기술을 3개씩 표시한다. PC의 기존 배치와 입력을 유지한다.
- 모바일은 훈련·튜토리얼을 제외한 캠페인에서 안전한 아군 명령 경계의 전투를 자동 저장한다. 공격·이동 연출, 적 턴과 선택 중에 앱이 종료되면 저장에 성공한 마지막 명령 경계부터 재개한다. 저장 실패는 안내하고 기존 파일을 보존한다.
- 전투 전 대화 도중 강제 종료는 해당 대화를 처음부터 시작한다. 전투 후 대화와 보상은 기존 저장 트랜잭션을 따른다.
- 모바일 설정은 `settings-mobile.json`으로 분리하며 PC 창 크기를 휴대폰에 적용하지 않는다. 초기 Android 프레임 목표는 30fps다. 이는 성능 측정 결과가 아니라 배터리·발열을 고려한 시작 설정이다.

## 빌드

Unity 6000.6.0f1의 Android Build Support·SDK·NDK·OpenJDK 설치를 완료했다. 현재 JDK 17.0.18+8·NDK 27.2.12479018·ADB 36.0.0을 사용한다. 사용자가 작업 중인 PC checkout의 대상을 전환하지 않고 별도의 검증 checkout에서 실행한다.

```powershell
unity build "<검증 checkout 절대 경로>" --target Android --execute-method TalesTactics.Editor.AndroidBuild.SourceOnlyApk --no-tail --log-file "<로그 절대 경로>"
```

- 결과: 검증 checkout의 `Builds/Android/TalesTactics.apk` 및 `build-result.json`.
- 테스트용 패키지 ID: `com.talestactics.game`. ARM64·IL2CPP, 최소 Android 8/API26·목표 API36, 가로 양방향 회전. 최종 지원 기기 목록은 실기기 결과로 확정한다.
- 로컬 테스트 서명이며 Play Store 제출용 서명·AAB·스토어 등록은 이번 범위가 아니다.
- `SourceOnlyApk`는 `Assets/TalesTactics/Audio/LocalMusic`에 메타데이터 외 파일이 있는 checkout을 거부한다. 다른 경로에 놓인 음원까지 자동 판별하지 않으므로 사용자 음원을 옮겨 이 검사를 우회하지 않는다. 로컬 음원 포함 APK는 `LocalApk`로 `Builds/Android/Local/`에 별도 생성할 수 있다. 사용자 제공 음원·이를 포함한 소스 이력과 다른 빌드는 로컬 전용이다. 2026-11-19 명시 승인된 APK 한 개와 체크섬만 공개 예외이며, 정확한 파일과 범위는 [음원 정책](../LocalMusicPolicy.md)의 승인 예외를 따른다. 이 예외는 이번 변경을 포함한 새 APK의 공개를 허용하지 않는다.
- 빌드용 설정은 정상 반환과 예외 처리에서 복원한다. 첫 APK 뒤 getter가 같아도 남는 직렬화 차이 2개를 발견해, 기존 API 복원 뒤 PlayerSettings 전체 메모리 스냅샷을 복원하도록 보완했다. 원시 설정 JSON은 파일·로그에 남기지 않는다. 회귀 2개와 보완 후 두 번째 실제 빌드를 통과했다. getter 17개·전체 직렬화와 텍스처 meta 1,192개·ProjectSettings 27개가 복원됐고, 저장·정상 종료 뒤에도 원래 파일 해시가 같았다. 프로세스 강제 종료는 복원을 보장하지 않으므로 별도 검증 checkout을 사용한다. 기존 원화·저장·Windows 설정을 임의로 재생성하지 않는다.
- `Assets/TalesTactics` 아래 Android 개별 설정이 없는 텍스처에 빌드 중 ASTC 6×6을 적용하고 원래 importer와 정확한 메타데이터를 복원한다. 원본 크기와 PC 압축 설정은 바꾸지 않는다. 중단된 텍스처 복구 기록은 `Library/TalesTactics/AndroidTextureSettingsRecovery.json`에 남고, 메뉴의 Restore Interrupted Android Texture Settings에서 복구한다.
- 실행마다 고유 ID와 시작 시간을 기록한다. 기존 APK는 실패 시에도 남을 수 있으므로 파일 존재만으로 성공을 판단하지 않고 해당 실행의 `build-result.json`을 확인한다.

## 2026-11-19 기반 구현의 확인 상태

설정 복원을 보완한 최종 두 번째 APK는 278.0694267초·오류 0·경고 2로 생성됐으며 서명·패키지·zipalign과 전체 설정·텍스처 메타데이터 복원을 통과했다. [로컬 Android 테스트 APK](../../Builds/Android/TalesTactics.apk)의 실제 크기는 214,425,556바이트, SHA256은 `91345F4E907DAFCE6A9EF066FBCDF6E4037108DFF1B642B006A2CE31D87E1483`다. 사용자 제공 음원을 제외한 테스트용 파일이며, 이후 사용자 명시 승인을 받아 [휴대폰 다운로드](https://github.com/kkkyaho/TalesTactics/releases/download/android-test-20261119/TalesTactics-android-test-20261119-arm64.apk)로 공개했다. 첫 빌드와 설정 복원 문제, 도구 시간 초과 이력은 [검증 기록](VALIDATION.md)에 보존했다. 당시 연결 기기 0대로 휴대폰 설치·실행이 미완료였고 코드 커밋·푸시와 정식 릴리즈를 보류했다. 이는 당시 진행 상태다. 현재 변경의 소스 반영 기준과 남은 실기기 한계는 위 메뉴 개선 항목과 [메뉴 검증 기록](MENUS.md)을 따른다.

## 검증 기준

1. 실제 Unity에서 기존 PC 입력·UI·저장 회귀와 신규 터치·모바일 저장·안전영역 검사를 수행한다.
2. 일반 Windows 실행본 및 Android APK를 각각 빌드한다. APK 성공은 실기기 플레이 성공과 구분한다.
3. 휴대폰에서 타이틀 → 인터미션 → 첫 전투 → 이동/공격/기술 → 앱 전환/재실행 → 이어하기를 확인한다.
4. 이후 전체 40전투·10대화의 화면·경로·저장 회귀, 장시간 메모리·발열·프레임·음악 복귀를 확인한다.
5. PC·모바일 저장 파일 형식을 공유하는 것과 기기 간 클라우드 동기화는 별개다. 클라우드 저장은 추가하지 않았다.

현재 실행 결과와 미완료 항목은 [검증 기록](VALIDATION.md)에 기록한다. 휴대폰 연결 후에는 [실기기 점검표](DEVICE-TEST.md)에 기기와 결과를 남긴다. 작성한 테스트 수나 코드 검토를 실제 실행 성공으로 세지 않는다.

[공개 APK 다운로드와 실제 다운로드 검증](DOWNLOAD.md): 사용자 승인 후 게시했으며, 익명 전체 다운로드의 바이트 수·SHA256이 원본과 일치했다.
