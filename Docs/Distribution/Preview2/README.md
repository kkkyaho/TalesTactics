# 0.1.0-preview.2 공개 테스트 배포

공개 릴리즈: https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.2

최종 일반 WindowsSystemUi 빌드의 시스템·UI 개선을 포함한다. 게임 소스/태그는 `5696f6e0d4801ea252eb018ca5c04ce499616e27`, 제품 버전은 기존 `0.1.0`, 배포 번호는 `0.1.0-preview.2`다. 이전 공개 preview.1은 유지한다. 최신 변경분은 현재 작업 브랜치에 있으며 main 병합은 수행하지 않았다.

- ZIP: `TalesTactics-0.1.0-preview.2-windows-x64.zip`, 199,339,510바이트, 202개 파일과 manifest.
- SHA256: `b34addd6fd52238d3dcde21c4fb6f1cd506b9dbd85ea994fafcb254bf0746fa7`.
- ZIP과 별도 `.zip.sha256`을 공개 prerelease에 첨부했다. 개인 음원/사용자 저장/개발 검수 코드는 포함하지 않는다.
- 게시 후 로그인/인증 없이 ZIP 전체와 체크섬을 다시 내려받아 HTTP200, 파일 크기와 SHA256 일치를 확인했다(public-download-check.json). 공개 태그도 게임 소스 커밋과 일치한다.
- 동일 빌드의 실제 Unity125/87 및 Windows6장·재실행 검수는 SystemUi 기록을 사용한다. 게임 코드가 바뀌지 않아 이번 포장에서 Unity를 재빌드하거나 전체 테스트를 반복하지 않았다.
- 배포 도구13개 재실행 통과(4.406초), ZIP 내부 파일/해시 검사 및 Windows PowerShell5.1 압축 해제본202개 검사 통과. 실행 뒤에도 배포 파일 불변을 확인했다.
- 사용자 저장3개를 Builds/SaveBackups의 새 날짜/GUID 폴더에 백업하고 검사했다. 실제 저장/백업 내용은 Git/Release에 포함하지 않으며 실행 전후 원본 해시는 불변이다.
- 압축 해제한 일반 실행 파일109초 정상 유지·로그 예외/오류0건을 확인했다. 사용하지 않는 일부 URP 후처리 셰이더가 제거되었다는 경고는 남는다.
- Computer Use 창 캡처는 검고 활성화 복구도 실패했다. 실제 일반 Windows 화면/물리 입력 검수 성공으로 계산하지 않는다. 작업에서 실행한 플레이어만 종료했다.
- 사람의6장 완주/장기 난이도, 실물 패드, 다양한 PC/DPI/전체화면, 최종 작화·청음 평가는 남아 있다. 이 배포는 해당 검수를 마친 최종 정식 출시가 아니다.

증거: package-result.json, package-manifest.json, tool-tests.txt, extracted-verification.txt, post-startup-verification.txt, extracted-startup.json, backup-verification.txt, user-save-check.json, published-release.json, public-download-check.json.

windows-build.json과 build-hashes.json은 기존 SystemUi 빌드 보고서/해시의 값을 배포 도구가 요구하는 형식으로 감싼 것이다. 새 빌드 증거를 만들거나 해시를 대체하지 않았다. 원본 보고서의 실행 파일 경로·전체 파일 크기와 기존 EXE/Runtime DLL SHA256을 대조하여 패키징했다.

업데이트는 새 폴더에 전체 압축 해제한다. 새 중단 저장을 구 실행 파일에서 읽지 못할 수 있으므로 이전 버전 복귀 시 업데이트 전 백업을 사용한다. 공개 릴리즈의 ZIP을 내려받으며 GitHub 자동 Source code ZIP은 실행 파일이 아니다.
