# Android 최신 모션·음원 포함 테스트판 — 2026-11-26

[휴대폰에서 APK 다운로드](https://github.com/kkkyaho/TalesTactics/releases/download/android-music-roster-test-20261126/TalesTactics-android-music-roster-test-20261126-arm64.apk) · [배포 페이지](https://github.com/kkkyaho/TalesTactics/releases/tag/android-music-roster-test-20261126) · [체크섬](https://github.com/kkkyaho/TalesTactics/releases/download/android-music-roster-test-20261126/TalesTactics-android-music-roster-test-20261126-arm64.apk.sha256)

동료10명의 네 방향 대기·호흡·눈 깜빡임·기본 공격·피격, 최신 모바일 설정·터치 도움말과 메인6화 지형 개선에 사용자 지정6곡을 함께 담았다. 타이틀1곡·인터미션1곡·전투4곡과 기존 음악14곡을 포함한다.

- APK 241,614,184바이트(약230.4MiB), SHA256 c32888454402ce10a221b8ae7e390c39b8c63c7bca2b5797be930b60e161a41b.
- Android8/API26 이상·ARM64·가로 화면, com.talestactics.game.
- versionName0.1.0/versionCode1과 기존 테스트 서명 유지. 날짜 태그로 구분하며 버전 번호를 올린 정식 릴리즈가 아니다.
- 휴대폰에서 파일을 열고 필요할 때 브라우저의 앱 설치를 허용한다. 기존 앱을 먼저 삭제하면 저장 데이터가 삭제될 수 있다. 실기기 덮어설치와 저장 계승은 미검증이다.

## 이번에 확인한 결과

음원 연결 EditMode2/2와 실제 Windows Editor 재생 PlayMode4/4를 통과했다. 타이틀·인터미션·전투·테마 후 복귀, 볼륨 독립과 실제 sample 진행을 검사했다. 이번 작업은 음원 연결과 APK 패키징 변경이며 기존 모션 전체 테스트를 다시 실행하지 않았다. [현재 실행 결과](Validation/MusicRoster20261126/validation-summary.json).

새 Android 빌드는 오류0·경고2, BuildReport 기준291.73초에 성공했다. 빌드 실행 ID는 154caf0dc6a649b796227c88645d03fa이다. 이 시간은 에셋 준비·대상 전환·복원까지 포함한 전체 작업 시간과 다르다. BuildReport에서 음원6곡 모두의 실제 스트리밍 리소스가 양수이며 동료 시트20장·프로필10개가 포함됨을 확인했다. 서명·ABI·가로 화면·ZIP CRC/정렬과 APK 해시를 확인했다. ZIP 정렬 검사는 네이티브 ELF 페이지 정렬 검사와 구분한다. [산출물 검증](Validation/MusicRoster20261126/artifact-summary.json).

검증용 작업본의 임시 음원 연결과 설정을 복원하고 원래 닫혀 있던 Editor를 정상 종료했다. 기존 저장·원화·음원·수정·APK의 보존은 위 검증 요약을 따른다. 실제 휴대폰 설치·플레이·음원 재생·터치·앱 복귀·발열·지속 성능은 확인하지 않았다.

## 소스와 공개 범위

공개 태그는 464b0873abad637fe30c192f0f64bdcfa2e2c83d를 가리킨다. 이 소스에 로컬 음원6곡과 AudioLibrary 연결을 임시 추가해 빌드했다. 공개/로컬 커밋의 게임 관련 공통2456파일은 동일하고 차이는 음원 연결과 음원 검사 소스다. 실제 작업본에 기존 렌더링/글꼴 설정 수정이 있었으므로 깨끗한 클론의 동일 바이트 재현을 주장하지 않는다. GitHub 자동 Source code 압축 파일에는 사용자 음원이 없다.

사용자가 항상 음원을 포함하도록 지시한 뒤 다시 APK와 다운로드 링크를 요청하여 이번 APK와 체크섬만 게시했다. MP3 원본·비공개 소스 이력·다른 음원 포함 빌드의 공개 범위는 확대하지 않는다. [정확한 공개 범위](Validation/MusicRoster20261126/publication-scope.json) · [음원 정책](../LocalMusicPolicy.md).

[게시 결과](Validation/MusicRoster20261126/publication.json), [익명 API 확인](Validation/MusicRoster20261126/publication-api.json), [로그인 없는 전체 다운로드](Validation/MusicRoster20261126/publication-download.json)로 HTTP200·원본 크기/해시 일치를 확인했다. 기존 다운로드·main·기존 태그는 유지했다. 이전 음원 제외판과 11월19일 음원판은 [다운로드 이력](DOWNLOAD.md)에 남아 있다.
