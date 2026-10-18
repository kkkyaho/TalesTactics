# 0.1.0-preview.8 Windows 공개 테스트 배포

**빌드·패키지 검증 완료, 공개 게시 준비 중.** [릴리즈 예정 페이지](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.8), [변경 내역과 저장 호환](release-notes.md).

preview.7 이후의 전체 기술 연출 확장, 공통 창 내부 여백·텍스트 배치 보완, 전장 가운데 버튼 드래그, 상태별 턴 종료 안내, 적 기술 조회, 보상 아이템 상세, 장비 착용자·초안·재고 표시, 중앙 레벨업 효과를 포함한다. 메인20화·개인 이야기30단계 및 기존 전투 규칙과 저장 형식은 유지한다.

## 소스와 빌드

- 검증된 게임 소스: `99a794d` (develop). 태그/manifest 대상은 `0cc8747ee6475656d7601c50206195dba958e7e4`이며 게임 소스 이후에는 릴리즈 문서만 추가했다.
- 일반 Windows 릴리즈 빌드 경로: `D:/TalesTacticsHelpGuide/Builds/WindowsPreview8/TalesTactics.exe`.
- Unity6000.6.0f1, 제품 버전0.1.0. 배포 버전0.1.0-preview.8은 manifest로 구분한다.
- 캠페인 V1~V3 읽기/V3 쓰기, 설정 V1, 중단 전투 V11. 업데이트 전 저장 백업과 새 폴더 압축 해제는 [배포 안내](../README.md)를 따른다.
- 실제 일반 빌드122.411638초, 오류0/경고3. Runtime SHA256은 직전 검증본과 동일하며 모든 Review/Fixture 타입 제외를 확인했다. 경고는 Pipeline 런타임 미설정과 URP 내부 디버그 셰이더2개의 제외 안내다. [BuildReport](windows-build.json), [코드 분리](build-code-isolation.json).

## 기존 게임 검증

전체 기술 연출 단계의 실제 Unity PlayMode202/202, Windows 아군 기술176/176은 [SkillRosterVfx 기록](../../SkillRosterVfx/README.md)을 따른다. UI 경계 점검 단계의 Editor1,065/1,065와 Windows213/213은 [FinalUiBounds 기록](../../FinalUiBounds/README.md)에 보존했다.

최신 피드백 단계는 실제 전체 PlayMode218/220 후 테스트 전제·대기 조건을 수정하여 카메라1/1·레벨업3/3 통과, 마지막 레이아웃 수정 뒤 관련10/10·장비8/8 추가 통과다. 최종 단일 전체220/220 실행은 아니다. API 참조4개 컴파일 통과이며 이번 단계에서 EditMode/ManagedChecks는 재실행하지 않았다.

Editor80상태×5해상도×3글자배율1,200/1,200, 최종 Windows1902×991×3배율×80상태240/240, 수집 오류0. 마지막 보스 요약 문구 조건은 Editor 전체 검수 후 최종 Windows에서 확인했다. 사용자 저장·백업·설정8파일 SHA256 불변과 일반 DLL의 검수 코드 제외를 확인했다. [원시 결과·실제 캡처·한계](../../TesterFeedback/README.md).

## 패키지와 게시 검증

- 배포 도구 **14/14** 통과. 실제 BuildReport199개 파일 및 EXE/Runtime 해시 보존.
- ZIP: `D:/TalesTacticsHelpGuide/Builds/Releases/TalesTactics-0.1.0-preview.8-windows-x64.zip`.
- 크기 **528,625,741바이트**, 배포 파일 **202개+manifest**, SHA256 `ea61e935e22a6ff93306ba0480230fe73dbd5bc273d972432177a175f6c4ea1a`.
- ZIP 생성 중 전체 파일 해시 검사, 새 폴더 압축 해제202파일 검사 통과. 실행 후에도202파일 무결성 통과.
- 압축 해제 일반 실행본 **30.12초** 유지, 초기화·응답 확인, 오류0. 숨김 시작 검사이며 사람의 화면/플레이 검수는 아니다.
- 사용자 저장·백업·설정8파일 SHA256 불변. 검사로 새로 시작한 프로세스만 종료했다.
- [패키지](package.json), [manifest](package-manifest.json), [압축 해제 검사](extracted-verification.json), [시작 검사](startup.json), [실행 후 검사](post-startup-verification.json).
- 공개 게시와 첨부 서버 해시·익명 다운로드 검사는 다음 단계에서 기록한다.

게임 검증과 릴리즈 패키지 검증은 구분한다. 다른 PC/DPI·물리 입력 장치·효과음 실청취·전체 캠페인 수동 완주·장시간 성능은 이번 기록으로 보장하지 않는다.
