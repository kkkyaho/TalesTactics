# Android 테스트 APK 다운로드 — 2026-11-19

## 음원 포함 테스트판

[휴대폰에서 음원 포함 APK 다운로드](https://github.com/kkkyaho/TalesTactics/releases/download/android-music-test-20261119/TalesTactics-android-music-test-20261119-arm64.apk) — 234,146,468바이트(약223.3MiB), 사용자 지정6곡과 기존 곡 포함. 이 APK의 공개 업로드는 사용자가 명시 승인했다. [적용 내용·검증·한계](LOCAL-MUSIC.md)를 따른다.

## 기존 음원 제외 테스트판

[휴대폰에서 APK 다운로드](https://github.com/kkkyaho/TalesTactics/releases/download/android-test-20261119/TalesTactics-android-test-20261119-arm64.apk) · [테스트 배포 페이지](https://github.com/kkkyaho/TalesTactics/releases/tag/android-test-20261119)

사용자가 공개 GitHub 업로드를 명시적으로 승인한 뒤 음원이 제외된 APK와 체크섬만 게시했다. 최초 자동 승인 검토의 공개 범위 확인 요청과 이후 사용자 승인 기록은 로컬 Logs/AndroidDownload20261119에 보존했다.

- APK: 214,425,556바이트(약 204.5MiB), Android 8/API26 이상·ARM64·가로 화면·테스트 서명.
- SHA256: `91345f4e907dafce6a9ef066fbcdf6e4037108dff1b642b006a2ce31d87e1483`.
- 빌드 실행 ID: `0ee6b446b2524efe90b13d2ddbd5dd82`. 기존 빌드·설정 복원 검사는 [검증 기록](VALIDATION.md)을 따른다.
- [익명 API 확인](Validation/android-download-verify.json): 공개 배포 상태, APK·체크섬 크기/해시, 태그 기준점을 확인했다.
- [익명 전체 다운로드](Validation/android-download-anonymous.json): 로그인·쿠키 없이 HTTP200으로 214,425,556바이트를 수신했고 원본 해시와 일치했다. 인증서/호스트 검증을 유지했으며 GitHub와 release-assets.githubusercontent.com만 거쳤다.
- 실기기 설치·실행·성능·음향 검사는 아직 미완료다. [실기기 확인표](DEVICE-TEST.md)를 따른다.

다운로드 태그 `android-test-20261119`는 기존 공개 develop의 `808d4331aa24a6466062d898574e575073e32afc`에 연결된다. APK에는 해당 기준점에 추가한 모바일 변경이 포함된다. GitHub의 자동 Source code 압축 파일은 이 추가 변경을 포함하지 않는다. 이번 게시에서는 소스 커밋을 푸시하거나 main·기존 태그를 변경하지 않았다. 사용자 음원·음원 포함 빌드·비공개 로컬 이력도 업로드하지 않았다.

첫 업로드에서 GitHub가 초안 URL에 `untagged-...`를 사용해 예상 주소 검사가 중단됐다. 업로드된 APK의 크기·해시를 읽기 확인한 뒤 동일 파일을 재업로드하지 않고 체크섬만 추가했다. 이어 기본 인증서 저장소의 issuer 확인 실패를 표준 certifi CA 묶음으로 해결했으며 TLS 검증을 끄지 않았다. 각 실패 기록은 삭제하지 않았다. 최종 공개 후 API·전체 다운로드 검사를 별도로 통과했다.