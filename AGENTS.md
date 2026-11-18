# 후속 작업 지침

README.md, Docs/TODO.md, Docs/ARCHITECTURE.md, Docs/VALIDATION.md부터 읽는다.
Unity 버전은 6000.6.0f1. 현재 신규 프로젝트이지만 구현 코드가 이미 있으므로 재생성/전면 재작성하지 않는다.
실제 EditMode 50개/PlayMode 10개 및 Windows 빌드/기본 UI 검수가 완료되었다. 최신 증거와 한계는 Docs/VALIDATION.md를 따른다.
Tools/ManagedChecks 결과는 엔진 독립 규칙 확인이며 Unity 테스트 결과로 표현하지 않는다.
DemoContent.Create는 초기 에셋 값을 작성하므로, Content 에셋에 사용자 수정이 생긴 뒤 무조건 재실행하지 않는다.
기획에 없는 원격 업로드/원작 음원 다운로드를 하지 않는다.

사용자 지속 지침: 요청한 작업을 모두 완료하고 관련 검증이 정상 통과하면 별도 승인 질문 없이 커밋하고 현재 작업 브랜치에 푸시한다. 실패하거나 미완료인 작업을 완료로 표현하거나 푸시하지 않는다.

## 브랜치 운영 — 사용자 지속 지침

- 기본 작업 브랜치는 `develop`이다. 앞으로 모든 개발 작업은 `develop`에서 진행한다.
- 별도 작업 브랜치가 필요하면 반드시 최신 `develop`을 기준으로 생성하고, 완료한 변경은 `develop`으로 통합한다. `main`이나 다른 작업 브랜치에서 새 작업 브랜치를 만들지 않는다.
- `main`은 릴리즈가 확정된 상태를 보관하는 마스터/아카이브 브랜치다. 일반 개발 커밋을 직접 만들거나 자동으로 머지하지 않는다.
- 사용자가 릴리즈·`main` 반영을 요청한 경우에만 검증된 `develop` 변경을 `main`에 반영한다.

Unity CLI 계정 ACL 문제 시 Computer Use 스킬의 node_repl + @oai/sky를 확인한다. 이 경로로 Unity 메뉴/테스트/빌드/플레이어 조작에 성공했다. 브라우저 전용 cua API 제한만 보고 Windows 제어 불가로 단정하지 않는다.

## 사용자 제공 음원 — 로컬 전용 (2026-11-03 사용자 지침)

- 사용자가 제공한 음원은 로컬 커밋과 로컬 플레이에만 사용한다. 일반적인 커밋·푸시·릴리즈 요청만으로 원격 업로드를 허용하지 않는다. 이 제한은 위 자동 푸시 지침보다 우선한다.
- 현재 대상은 `Assets/TalesTactics/Audio/LocalMusic/`의 MP3 6곡과 이후 사용자 제공·교체 음원이다. 해당 음원이 포함된 소스 커밋·태그·GitHub Release·빌드 ZIP도 추가 공개하지 않는다.
- `codex/local-only-music-20261103`은 로컬 보존 브랜치이며 원격에 푸시하지 않는다. 로컬 파일과 Unity 연결은 보존한다.
- 기존 원격 develop/main과 Preview9 태그에는 음원이 이미 포함되어 있다. `.gitignore`나 새 삭제 커밋만으로 과거 이력에서 제거됐다고 표현하지 않는다. 원격 이력 재작성·강제 푸시·태그 교체는 구체적 범위를 설명하고 별도 승인을 받은 뒤 수행한다.
- 현황과 정리 대상은 `Docs/LocalMusicPolicy.md`를 따른다.

## 음원을 제외한 원격 소스 — 2026-11-18 승인

사용자가 로컬 음원과 원본 develop을 보존한 별도 공개 소스의 검증·푸시를 승인했다. 원격 develop의 최신 소스에는 LocalMusic6곡과 AudioLibrary의 해당 연결이 없고 기존 곡으로 대체 재생한다. 원본 로컬 develop과 공개 develop은 다른 이력이다. 음원 포함 비공개 로컬 커밋을 공개 develop의 부모·merge 부모로 넣지 않는다. 이후 게시도 당시 원격 develop을 단일 부모로 한 음원 제외 소스만 검증하여 일반 푸시한다. 과거 원격 음원 이력·main·태그를 재작성할 권한은 없다. Docs/SourceOnlyPublish/README.md를 먼저 읽는다.
