# 0.1.0-preview.3 공개 테스트 배포

[공개 릴리즈와 Windows 다운로드](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.3)

전신 모델 편성, 장비·상점·성장/승급 인터미션 UI, 아군 자유 선택 턴을 포함합니다. 소스/태그는 `b8833b98ebc179ae5cecd0bfa55703fff431217b`, Unity는6000.6.0f1, 실행 파일 제품 버전은0.1.0, 배포 버전은0.1.0-preview.3입니다. 기존 preview.1/2 릴리즈와 작업 브랜치는 보존합니다.

- ZIP: `TalesTactics-0.1.0-preview.3-windows-x64.zip`, **205,827,544바이트**,202개 파일+manifest.
- SHA256: `c63ecc4bb60f22468a916a5f3e1dd81d1db23120d59ed43796c6b9e5daf5b45a`.
- 실행용 ZIP과 `.zip.sha256`을 공개 prerelease에 첨부했습니다. GitHub 자동 Source code ZIP은 실행 파일이 아닙니다.
- 게시 후 인증 없이 두 첨부 파일 전체를 다운로드하여 HTTP200·파일 크기·SHA256 일치를 확인했습니다. 태그도 위 소스 커밋과 일치합니다.
- 배포 도구13/13(3.783초), ZIP 내부 검사, Windows PowerShell5.1 압축 해제본202개 검사와 실행 후 불변 검사를 통과했습니다.
- 압축 해제본12초 숨김 시작에서 프로세스 유지·로그 예외/오류0건을 확인했습니다. 사용자 저장3개를 새 날짜/GUID 폴더에 백업하고 검사했으며 실행 전후 원본 SHA256은 불변입니다. 사용자 저장/백업·개인 음원·개발 검수 코드는 패키지에 포함하지 않았습니다.
- 게임 코드/콘텐츠는 변경하지 않았습니다. 직전 FreeTurns의 전체 Unity PlayMode97/97,최종 EditMode133/133,관련 PlayMode4/4와 최종 Windows 일반 빌드10.415초·오류0·기존 경고4의 검증을 사용했습니다. 기존 실행 파일/Runtime DLL 해시와 빌드 파일 목록·크기를 대조했습니다. 이번 포장으로 Unity 테스트 수를 늘리거나 새 빌드로 표시하지 않습니다.
- 화면 검수는 Editor Game View이며 Windows 일반 플레이어는 시작 로그 검사입니다. 실제 게임패드·다른PC/DPI·새 규칙으로 사람이6장을 완주한 난이도 평가는 남아 있습니다.

새 버전 ZIP은 새 폴더에 전체 압축 해제하세요. 기존 캠페인 V1/V2,중단 기록 V1/V2를 읽으며 새 중단 기록은 **V3**입니다. preview.2로 복귀할 때는 업데이트 전 저장 백업을 사용하세요. 구버전이 V3 중단 기록을 읽는다고 보장하지 않습니다.

증거: package-result.json,package-manifest.json,tool-tests.txt,extracted-verification.txt,post-startup-verification.txt,extracted-startup.json,backup-verification.txt,user-save-check.json,published-release.json,public-download-check.json.

windows-build.json은 FreeTurns/windows-build.json의 값을 배포 도구가 요구하는 형식으로 감쌌습니다. build-hashes.json은 같은 기존 검증 파일의 사본이며 값을 대체하거나 새 검증처럼 만들지 않았습니다.
