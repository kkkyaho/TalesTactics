# 후속 작업 지침

README.md, Docs/TODO.md, Docs/ARCHITECTURE.md, Docs/VALIDATION.md부터 읽는다.
Unity 버전은 6000.6.0f1. 현재 신규 프로젝트이지만 구현 코드가 이미 있으므로 재생성/전면 재작성하지 않는다.
최우선은 Unity 실제 import/Play Mode/빌드 검증이다. 이전 세션에서 라이선스 IPC가 거부되었다.
Tools/ManagedChecks 결과는 엔진 독립 규칙 확인이며 Unity 테스트 결과로 표현하지 않는다.
DemoContent.Create는 초기 에셋 값을 작성하므로, Content 에셋에 사용자 수정이 생긴 뒤 무조건 재실행하지 않는다.
기획에 없는 원격 업로드/원작 음원 다운로드를 하지 않는다.
