# 0.1.0-preview.6 공개 테스트 배포

[릴리즈와 Windows 다운로드](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.6)

새 메인20화·개인30단계와 구6장 제거, 피해/EXP·머리 위 방향 표시, 화염·검격 개선을 포함한다. [변경 내역과 저장 호환](release-notes.md), [실제 전투 영상과 게임 검증](../../VfxQuality/README.md).

일반 Windows 빌드는 `Builds/WindowsPreview6`이며 검수 define을 사용하지 않는다. `windows-build.json`은 실제 Unity BuildReport의 파일 목록/크기를 기록한다. 제품 버전은0.1.0, 배포 버전은 manifest에0.1.0-preview.6으로 구분한다. 게임 코드와 Content는 PlayMode137/137을 통과한6a44585와 같다. 이번 배포는 패키지 도구·안내 및 Editor 빌드 기록만 변경한다.

패키지는 전체 파일의 크기와 SHA256, 필수 Unity 파일·폰트 라이선스, 사용자 저장·로그·개발 검수 코드 제외를 검사한다. 배포 도구의 checkpoint 메타데이터를 실제 V11에 맞추고 새 검수 타입도 차단한다. 실행 ZIP과 체크섬을 공개 prerelease로 게시했다. [게시 결과](published-release.json).

배포 준비 검증: 실제 일반 BuildReport **성공/오류0/경고3**,139.3772689초, 파일199개. 실행 파일/Runtime DLL의 SHA256은 직전 VfxQuality 검증본과 동일하다. API 참조4개 컴파일, 배포 도구14/14 통과(22.511초). 이번 단계에서 전체 Unity 게임 테스트를 반복하지 않았다.

## 패키지 검증

- 태그/manifest 소스: `c9efa1254840a25a22cb8780c71f02a543eda367`.
- ZIP: `TalesTactics-0.1.0-preview.6-windows-x64.zip`,382,894,147바이트,202개 파일+manifest.
- SHA256: `8e8962899963cc208d1b8eccb3b99afad1222bb69e21694f7f2e08cb6b986d38`.
- ZIP 내부 전체 해시와 압축 해제본의 Windows PowerShell 무결성 검사를 통과했다. 실행 시작 후 재검사도 통과했다.
- 압축 해제 일반 실행본의12초 시작 유지, 수집 오류0, 기존 사용자 저장/백업/설정6개 해시 불변 및 기존 플레이어 유지 확인. 숨김 실행의 시작 검사이며 직접 입력/화면 검수로 표현하지 않는다.
- [패키지 근거](package.json), [실행 시작 근거](startup.json), [배포 도구 검사](package-tests.txt).
- GitHub 첨부2개의 서버 SHA256/크기가 로컬과 일치한다. 인증 없는 릴리즈 조회200, 두 첨부 HEAD200, 체크섬 GET200과 내용 일치를 확인했다. 원격 ZIP 전체 재다운로드는 하지 않았다. [공개 링크 검증](public-check.json).

새 저장/캠페인 기록의 preview.5 이하 호환을 보장하지 않는다. 구6장 중단 기록은 보존하되 이어하기를 막는다. 기존 저장 성장·장비·골드·이력은 유지한다. 사람의 전체 캠페인 완주·장시간 성능 검증과 최종 상용 출시 판정은 별도다.
