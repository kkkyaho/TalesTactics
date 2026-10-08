# 10번 — 로컬 배포·업데이트 체계

최신 공개 배포는 **[0.1.0-preview.3](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.3)**다. 편성·인터미션 UI와 아군 자유 선택 턴을 담은 WindowsFreeTurns 일반 빌드이며 새 ZIP/체크섬을 게시했다. [최신 패키지·검증 기록과 V3 중단 저장 호환 안내](Preview3/README.md)를 따른다. 아래 preview.1 내용은 최초 배포 도구 구축 당시 기록이다.

검증된 WindowsAccessibility 일반 빌드를 `TalesTactics-0.1.0-preview.1-windows-x64.zip`으로 패키징했다. 배포 ZIP은 **Builds/Releases**에 있으며199,546,142바이트(약190.3MiB), 파일202개와 manifest를 포함한다. ZIP/실행 파일/저장 백업은 Git에 올리지 않는다. Git에는 도구·사용 안내·검증 기록만 포함한다.

SHA256: `10e792b1f683da7ece10d6d7f69267fbc3aba487cad56e2e737591575e739a11`

## 플레이어 흐름

1. [START-HERE.txt](START-HERE.txt)의 절차로 버전별 **새 폴더**에 전체 ZIP을 푼다.
2. 포함된 Verify-Package.ps1로 누락·변경·불필요한 파일이 없는지 확인한다.
3. TalesTactics.exe를 실행한다. 게임 폴더 밖의 기존3슬롯과 설정을 계속 사용한다.
4. 업데이트 전 게임을 종료하고 Backup-Saves.ps1로 원본/백업/이관/손상 보관본을 복사한다.
5. 새 버전도 새 폴더에 풀고 실행한다. 문제 시 이전 게임 폴더와 **업데이트 전 저장 백업**으로 복귀한다. 파일 자동 덮어쓰기·저장 자동 복원은 하지 않는다.

두 PowerShell 도구는 Windows PowerShell5.1에서 검사했다. 설치된 Python은 플레이어에게 필요하지 않다. 스크립트 실행 정책은 영구 변경하지 않으며, 해당 프로세스에만 적용하는 옵션과 수동 복사 대안을 안내한다. SHA256은 손상/변경 검사이며 게시자 서명이나 신뢰할 다운로드 출처를 대신하지 않는다.

## 제작 절차

Python3.10 이상과 Git이 필요하다. Unity 일반 Windows 빌드 및 관련 검증을 마친 뒤 실행한다. 예시 명령은 이미 만들어진 버전의 파일을 덮어쓰지 않으므로 후속 배포에서는 새 버전 번호와 변경 내역 파일을 사용한다.

```powershell
python Tools/PackageRelease.py pack --build Builds/WindowsAccessibility --evidence Docs/Accessibility --version 0.1.0-preview.1 --output Builds/Releases --notes Docs/Distribution/CHANGELOG-0.1.0-preview.1.txt
python Tools/PackageRelease.py verify Builds/Releases/TalesTactics-0.1.0-preview.1-windows-x64.zip
python Tools/test_release.py
```

도구는 Assets/Packages/ProjectSettings의 수정·미추적 파일, 실패한 빌드 보고서, 다른 빌드 경로, 이전 검증의 실행 파일/Runtime DLL 해시 불일치, 개발 검수 타입, 누락된 필수 파일, 사용자 저장 포함, 잘못된 경로·중복 엔트리를 거부한다. 백업 폴더·PDB/MDB/로그는 제외한다. 이번 실제 빌드에서는 Unity의 배포 제외 폴더 안1개 파일을 제외했다. 필요한 Unity 파일·폰트 고지는 유지했다.

manifest에는 전체 배포 파일의 크기/해시와 배포 버전, 게임 소스 커밋, Unity/제품 버전, 저장 호환 버전을 기록한다. ZIP을 임시 위치에서 검사한 뒤 최종 이름과 체크섬을 생성한다. 같은 버전의 ZIP/체크섬이 있으면 거부한다. 제작 도중 실패해 파일이 남았으면 원인을 확인하고 새 버전/별도 출력 위치를 사용한다.

이번 sourceCommit은 게임 코드와 콘텐츠의 `20b8504d3e1b2e929460dfe35a6cb5847dd292aa`다. 실행 파일은 제품 버전0.1.0이며 배포 번호0.1.0-preview.1은 manifest/변경 내역으로 구분한다. 새 배포 때 Unity/제품/저장 버전 메타데이터도 코드와 함께 검토해야 한다. 패키징 시점의 전체 파일 해시를 생성한 것으로, 이전 빌드 보고서에 모든 파일의 해시가 있었다는 뜻은 아니다.

## 검증과 한계

- 배포 도구13개 검사: ZIP 왕복·Windows PowerShell 폴더 검사·변조/누락/추가/경로 탈출/중복 차단·동일 버전 보호·별도 폴더 업데이트·개발 빌드/사용자 저장 차단·3슬롯/설정/보관본 백업과 원본 불변. [결과](tool-tests.txt). Unity 테스트 수에 합산하지 않는다.
- 최초 검사2회에서 실행 정책과 이 환경의 Get-FileHash 모듈 검색 문제를 발견했다. 테스트 프로세스 전용 실행 옵션 및 .NET SHA256 직접 사용으로 보완했다. 이전 실패 기록은 tool-tests-initial.txt, tool-tests-hash-module.txt.
- 실제 ZIP 생성 후 새 폴더에 풀어202개 파일을 Windows PowerShell로 검사했다. [생성 결과](package-result.json), [manifest](package-manifest.json), [압축 해제 검사](extracted-verification.txt).
- 사용자 campaign.json/백업2개를 Builds/SaveBackups의 새 날짜/GUID 폴더로 복사하고 검증했다. 원본/백업 SHA256 불변, settings/슬롯2/3 미생성. 실제 사용자 백업 내용은 Git에서 제외한다. [백업 검사](user-backup-check.json), [사용자 파일](user-files-after.json).
- 압축 해제한 일반 실행 파일을 숨김 실행해10초 후 프로세스 유지·Mono/PhysX/입력 초기화·로그 오류0건을 확인했다. 숨김 창 핸들이 없어 해당 검수 프로세스만 종료했다. 화면/프레임 진행 또는 정상 종료/사람 플레이 검수로 계산하지 않는다. [초기화 기록](release-startup.json). 실행 후에도 배포 파일 검사는 통과했다(post-startup-verification.txt).
- 게임 코드/콘텐츠는 바꾸지 않아 Unity 테스트·빌드를 재실행하지 않았다. 최신 실제 Unity102Edit/73Play 및7회 캠페인 검증은 직전 Accessibility 기록과 동일한 실행 파일/Runtime DLL에 해당한다. 이전V1 이관·V2 저장·미지원 최신 저장 보호는 기존 테스트 범위를 따른다.

이번10번은 로컬 배포 패키징과 전체 패키지 업데이트/백업 절차 완료다. 온라인 자동 업데이트·설치 프로그램·코드 서명·스토어 업로드는 구현/진행하지 않았다. 실제 배포 채널 결정 후 별도 작업이며, 7~9번의 남은 사람/장치 검수도 유지한다. 판매 가능한 최종 출시 승인을 의미하지 않는다.
