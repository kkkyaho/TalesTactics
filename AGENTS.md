# 후속 작업 지침

README.md, Docs/TODO.md, Docs/ARCHITECTURE.md, Docs/VALIDATION.md부터 읽는다.
Unity 버전은 6000.6.0f1. 현재 신규 프로젝트이지만 구현 코드가 이미 있으므로 재생성/전면 재작성하지 않는다.
실제 EditMode 34개/PlayMode 5개 및 Windows 빌드/기본 UI 검수가 완료되었다. 최신 증거와 한계는 Docs/VALIDATION.md를 따른다.
Tools/ManagedChecks 결과는 엔진 독립 규칙 확인이며 Unity 테스트 결과로 표현하지 않는다.
DemoContent.Create는 초기 에셋 값을 작성하므로, Content 에셋에 사용자 수정이 생긴 뒤 무조건 재실행하지 않는다.
기획에 없는 원격 업로드/원작 음원 다운로드를 하지 않는다.

Unity CLI 계정 ACL 문제 시 Computer Use 스킬의 node_repl + @oai/sky를 확인한다. 이 경로로 Unity 메뉴/테스트/빌드/플레이어 조작에 성공했다. 브라우저 전용 cua API 제한만 보고 Windows 제어 불가로 단정하지 않는다.
