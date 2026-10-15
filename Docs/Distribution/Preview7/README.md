# 0.1.0-preview.7 Windows 릴리즈 준비

타이틀·인터미션·공통 일러스트 아이콘·전장 가독성과 편성 세로 스크롤·장비 비교를 포함한다. [변경 내역과 저장 호환](release-notes.md), [게임 검증과 실제 화면](../../CommercialUxProposal/IMPLEMENTATION.md).

일반 Windows 실행본은 `D:/TalesTacticsHelpGuide/Builds/WindowsPreview7/TalesTactics.exe`다. 실제 Unity6000.6.0f1 빌드59.916471초, 오류0/경고3. 제품 버전0.1.0이며 배포 버전은 manifest에서0.1.0-preview.7로 구분한다. 게임 Runtime DLL의 SHA256은 직전 CommercialUxWindows 검증본과 동일하다. 릴리즈 빌드의 검수 타입 제외를 확인했다.

배포 도구14개 검사 통과. 실제 BuildReport 파일 목록과 실행 파일/Runtime DLL 해시를 보관했다. 패키지는 전체 파일 해시·필수 Unity 파일·폰트 라이선스·저장 및 검수 코드 제외를 검사한다. 기존 사용자 저장과 실행 게임을 보존한다.

## 패키지 검증

- 태그/manifest 소스: `2f1e6e15f2143ba3fb2ee6ff2e57f7015a7f8c9c`.
- ZIP: `D:/TalesTacticsHelpGuide/Builds/Releases/TalesTactics-0.1.0-preview.7-windows-x64.zip`,519,249,574바이트,202개 파일+manifest.
- SHA256: `25956cc2869758157f2300828f96c79809c99d7e6d69d56fba5b43f379897ca5`.
- 패키징 중 ZIP 전체 해시 검사와 새 폴더의 Windows PowerShell 검사202파일을 통과했다. 실행 후에도202파일 무결성을 다시 확인했다.
- 압축 해제 일반 실행본34.55초 시작 유지, 초기화 확인·수집 오류0. 숨김 시작 검사이며 사람의 화면/플레이 검수로 표현하지 않는다.
- 기존 사용자 저장/설정8파일은 앞선 검증 해시와 일치하고 기존 실행 게임을 유지했다.
- 최초 실행 검증 래퍼는 성공한 PowerShell 스크립트에 부적절하게 `$LASTEXITCODE`를 검사하여 실패를 표시했다. 실제 무결성 결과는 성공이었으며, 반환 객체와 예외를 사용하는 재검사도 통과했다. 빌드나 패키지 수정은 없었다.
- [패키지](package.json), [압축 해제 검사](extracted-verification.json), [시작 검사](startup.json), [시작 후 검사](post-startup-verification.json).

## 게시 상태

`main`에 검증된 develop 변경을 fast-forward 통합하고 `v0.1.0-preview.7` 태그를 생성했다. **GitHub Release 게시·ZIP 첨부 업로드는 미완료**다. GitHub CLI는 로그인되어 있지 않았으며, 인증 확인을 포함한 셸 명령은 자동 승인 검토에서 정책 차단됐다. 별도 브라우저 자동화도 현재 URL을 확인할 수 없어 중단했다. 인증 우회를 하지 않았다.

GitHub CLI 로그인 이후 기존 태그를 사용해 `release-notes.md`와 ZIP/체크섬2개를 prerelease로 게시하고 공개 다운로드 해시·크기를 검증해야 한다. 현재 공개 다운로드는 preview.6이며 이 문서로 게시 완료를 주장하지 않는다.
