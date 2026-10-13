# 0.1.0-preview.5 공개 테스트 배포

[공개 릴리즈와 Windows 다운로드](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.5)

preview.4 이후 전장 지형 아트, 영웅 10명의 모션·상태창, 그래픽·UI/UX 일괄 개선을 포함한다. [변경 내역](release-notes.md), [실제 개선 화면](../../VisualUx/README.md).

- 태그/패키지 소스: `e41c4cbecafc7877b97e5a307b617dd9b61b6244`. Assets/Packages/ProjectSettings는 실제 전체 검증을 통과한 `c10b3e1`과 동일하다.
- Unity 6000.6.0f1, 제품 버전0.1.0, Windows x64 일반 빌드 `Builds/WindowsPreview5`.
- ZIP: `TalesTactics-0.1.0-preview.5-windows-x64.zip`, **296,965,927바이트**,202개 파일+manifest.
- SHA256: `4c90a3cc5dc0d6a18179f54a15c874729a2c0a2ff61deff7ac7423b1f47b5762`.
- 실행 ZIP과 체크섬을 공개 prerelease로 게시했다. GitHub 자동 Source code ZIP은 실행용이 아니다.

## 저장 호환

**preview.4와 저장 형식은 동일하다.** 캠페인 V1~V3 읽기/V3 쓰기, 중단 전투 V1~V10 읽기/V10 쓰기, 설정 V1이다. 게임 종료 후 `Backup-Saves.ps1`로 저장을 백업하고 새 폴더에 전체 압축 해제한다. preview.3 이하로 복귀하려면 해당 버전 사용 당시의 백업이 필요하다. 이번 배포 작업에서는 사용자 저장을 수정하거나 실행 중이던 게임을 종료하지 않았다.

## 검증

- 동일 게임 소스의 실제 Unity EditMode **202/202**, 최종 PlayMode **126/126**, 상태창360·준비60·전장72조합과 기술126페이지. 직전 개발 Windows 실행본에서 독립1장26턴·22공격·승리/실제 저장을 확인했다. 이번 배포에서는 게임 테스트를 반복하지 않았다.
- 릴리즈용 일반 Windows 빌드를 다시 생성했다. **24.685초, 오류0/기존 경고3건**, 실제 BuildReport 파일199개를 기록했다. 실행 파일/Runtime DLL SHA256이 직전 VisualUx 검증본과 같으며 개발 검수 타입3종은 제외됐다.
- 배포 도구 **14/14** 통과. ZIP 내부 모든 파일 해시, 압축 해제202개 파일의 무결성 및 실행 후 재검사를 통과했다. 개인 저장·로그·개인 음원·개발 검수 코드는 포함하지 않는다.
- 압축 해제 일반 실행본의12초 시작 유지, 오류/예외0, 사용자 저장·백업·설정6개 SHA256 불변 및 기존 실행 중 게임 유지를 확인했다. 시작 검사는 숨김 프로세스로 수행했으며 화면/플레이 검수로 주장하지 않는다.
- GitHub 첨부 두 개의 서버 SHA256과 로컬 해시 일치. 인증 없는 릴리즈 조회, ZIP HEAD200, 체크섬 GET200/내용 일치를 확인했다. 원격 ZIP 전체 재다운로드는 수행하지 않았다.
- 기존 Pipeline 런타임 미설정 및 URP DebugOccluder/DebugOcclusionTest stripping 경고3건이 남는다. 다른PC/DPI·물리 게임패드·전6장 수동 완주·장시간 성능·최종 상용 출시 판정은 별도다.

main은 요청된 릴리즈 아카이브로 갱신하며 일반 개발은 develop에서 계속한다.
