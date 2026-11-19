# Android 음원 포함 테스트판 — 2026-11-19

사용자 요청에 따라 PC와 같은 타이틀·거점·전투 음원 6곡을 포함한 Android APK를 생성했다. 기존 음악 14곡·효과음 4개도 유지한다. 기존 `AndroidBuild.LocalApk` 경로를 사용했으며 제품 코드 변경은 없다.

- 파일: [로컬 음원 포함 APK](../../Builds/Android/Local/TalesTactics.apk). 이 파일 링크는 현재 PC의 경로이며 원격 다운로드 주소가 아니다.
- 실제 크기: 234,146,468바이트(223.30MiB).
- SHA256: `14b83b63efa6df3fcba9d004586c1269d9c5f822c705db47979cf43aa3ec007f`.
- Android 8/API26 이상·ARM64·버전0.1.0/code1·테스트 서명. 기존 공개 APK와 앱 식별자 및 서명 인증서가 같다. 실제 기기 업데이트 설치는 미검증이다.
- [실제 빌드](Validation/LocalMusic/build-result.json): 성공·299.7086초·오류0·경고2. 실행 ID `1edc145290374332aa26277e0b7c4fb1`. 임포트/복원 시간을 제외한 BuildReport 시간이며 집계 크기는 실제 APK 크기와 다르다.
- [EditMode 2/2](Validation/LocalMusic/editmode.json)·[PlayMode 4/4](Validation/LocalMusic/playmode.json): 6곡 실제 샘플 진행·화면 전환·볼륨 독립·테마 후 복귀·전투 준비 취소를 Windows Editor에서 확인했다.
- [Android 임포트 설정](Validation/LocalMusic/audio-imports.json): 6곡은 스테레오44.1kHz·Streaming이다. [BuildReport](Validation/LocalMusic/packed-audio.json)에 6개 원본 경로가 포함됐으며 모두 데이터 크기가 양수다.
- [독립 APK 검사](Validation/LocalMusic/apk-independent-verification.json): 패키지·서명·zipalign·ARM64·전후 해시 검사 통과. 6개 사용자 곡과 14개 기존 음악 이름/리소스 크기는 보조 증거이며 소리 재생 검사는 아니다.
- [보존 검사](Validation/LocalMusic/final-preservation.json): 검증 복사본 입력2363개가 원래 상태와 일치하고 임시 음원 연결을 제거했다. 로컬 보호 파일15개·기존 저장9개가 유지됐다. 원본 MP3와 공개 APK는 변경하지 않았다.

검증용 Editor 종료 요청은 45초 응답 시간 초과를 반환했다. 이후 프로젝트 인스턴스0과 실제 종료 로그를 확인하고 복원했으며 강제 종료하지 않았다. APK 빌드 오류와 구분한다. 원문은 `Logs/AndroidLocalMusic20261119/close.json`, `status-after-close.json`에 보존했다. 빌드 경고2개는 기존 Pipeline 설정 부재와 글꼴 importer 버전 안내다.

실제 Android 기기의 설치·재생·잠금/백그라운드 복귀·음악 끊김·발열은 아직 확인하지 않았다. [실기기 확인표](DEVICE-TEST.md)를 따른다.

사용자가 이 음원 포함 APK의 공개 업로드 질문에 “올려줘”라고 명시 승인한 뒤 [공개 APK 다운로드](https://github.com/kkkyaho/TalesTactics/releases/download/android-music-test-20261119/TalesTactics-android-music-test-20261119-arm64.apk)와 체크섬을 게시했다. [승인 범위](Validation/LocalMusic/publication-approval.json) · [공개 게시 결과](Validation/LocalMusic/publication.json) · [익명 API 확인](Validation/LocalMusic/publication-api.json). 기존 [음원 제외 APK](DOWNLOAD.md)는 그대로 유지한다. 이번 파일에만 공개 예외를 적용하며 음원 원본·음원 포함 소스 이력·로컬 음악 브랜치·다른 빌드는 계속 원격에 올리지 않는다. 자세한 범위는 [음원 정책](../LocalMusicPolicy.md)을 따른다.

공개 다운로드 태그는 이미 공개된 develop 기준점 `808d4331aa24a6466062d898574e575073e32afc`를 가리킨다. APK는 추가 모바일 변경과 음원 연결을 포함하므로 GitHub의 자동 Source code 압축 파일과 일치하지 않는다. 배포 설명에 이를 명시했으며 소스 브랜치 푸시·main 통합·기존 태그 변경은 하지 않았다.

[익명 전체 다운로드 검증](Validation/LocalMusic/publication-download.json): 로그인·쿠키 없이 공개 링크에서 HTTP200으로 234,146,468바이트를 받았고 SHA256이 원본 APK와 일치했다. 인증서·호스트 검증을 유지했다. 이 검사는 실제 휴대폰 설치·재생 검사와 구분한다.
