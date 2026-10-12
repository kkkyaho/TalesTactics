# 0.1.0-preview.4 공개 테스트 배포

[공개 릴리즈와 Windows 다운로드](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.4)

preview.3 이후의 행동 경험치·교전 구성, 난이도/임무 변형, 반격/지원/되감기/재도전, 간결한 임무·전투 예측, 캐릭터 크기·HP·이동·방향 UI와 창 조절 개선을 포함한다. [변경 내역](release-notes.md).

- 태그/패키지 소스: `e1ec7bb57bab6b55235ab94ac6e3758b6670da57` (`v0.1.0-preview.4`). 게임 Assets/Packages/ProjectSettings는 실제 검증한 `1be4bd3`과 동일하며, 릴리즈 준비에서는 배포 도구·안내만 변경했다.
- Unity6000.6.0f1, 제품 버전0.1.0, Windows x64 일반 빌드 `Builds/WindowsGroundFacing`. 새 패키지를 위한 게임 재빌드는 하지 않았다.
- ZIP: `TalesTactics-0.1.0-preview.4-windows-x64.zip`, **205,831,290바이트**,202개 파일+manifest.
- SHA256: `b21ccd8bdea3c53fbb91d7e24b38eadd98f88f9e17f65585fd95cb149d3b5df1`.
- 실행용 ZIP과 체크섬을 공개 prerelease로 게시했다. GitHub 자동 Source code ZIP은 실행용이 아니다. 이전 릴리즈는 보존한다.

## 저장 호환

캠페인 V1/V2 읽기·이관, V3 읽기/쓰기, 설정V1, 중단 전투V1~V10 읽기/V10 쓰기다. 경험치 이관은 기존 레벨 진행률을 새100 EXP 기준으로 변환하며 이관 전 저장을 `.v1.bak`/`.v2.bak`로 보관한다. **preview.3 이하로 돌아갈 때는 업데이트 전 저장 백업이 필요하다.**

게임 종료 후 ZIP의 `Backup-Saves.ps1`로 먼저 백업하고 새 버전 폴더에 전체 압축 해제한다. 백업 도구에 `.v2.bak` 포함을 추가했다. 실행 중인 게임이 있으면 백업을 거부하는 보호는 유지한다. 이번 작업에서는 사용자가 실행 중인 게임을 종료하거나 사용자 저장을 백업/변경하지 않았다.

## 검증

- 최신 게임 증거: 전체 PlayMode119/119, 직전 시스템 EditMode202/202, 최신 방향UI 화면8조합, 일반 빌드36.280초/오류0. [상세 검증](../../GroundFacing/README.md). 게임 테스트를 이번 배포 작업에서 다시 실행한 것은 아니다.
- 배포 도구 **14/14** 통과. V2 이관 보관본, 실행 중 백업 거부, 저장 원본 불변, manifest와 실제 저장 버전 일치, 변조/누락/추가 파일·개발 검수 코드·잘못된 경로 차단을 검사했다. 테스트용 임시 저장과 모의 프로세스 목록을 사용해 사용자 게임에 영향이 없다.
- 검증된 빌드 파일 목록/실행 파일/Runtime DLL 해시를 확인한 뒤 ZIP을 만들고, 모든202개 파일의 ZIP 내부 해시와 Windows PowerShell 압축 해제 무결성 검사를 통과했다. 로그·개인 저장·개인 음원은 포함하지 않는다.
- 업로드된 두 첨부의 GitHub SHA256과 로컬 해시 일치. 인증 없이 릴리즈 조회, ZIP 다운로드 HEAD200, 체크섬 GET200 및 내용 일치를 확인했다(`public-download-check.json`). 전체 ZIP의 원격 재다운로드 검사는 수행하지 않았다.
- 기존 Pipeline 런타임 미설정과 URP 디버그 셰이더 stripping 안내3건은 남는다. 이번 압축 해제 실행본의 독립 플레이 검사는 하지 않았으며, 시각 검수는 직전 Editor Game View다. 실물 장치/다른 PC/DPI·장기 밸런스는 별도 검수 대상이다.
