# 0.1.0-preview.6 공개 테스트 배포

[릴리즈와 Windows 다운로드](https://github.com/kkkyaho/TalesTactics/releases/tag/v0.1.0-preview.6)

새 메인20화·개인30단계와 구6장 제거, 피해/EXP·머리 위 방향 표시, 화염·검격 개선을 포함한다. [변경 내역과 저장 호환](release-notes.md), [실제 전투 영상과 게임 검증](../../VfxQuality/README.md).

일반 Windows 빌드는 `Builds/WindowsPreview6`이며 검수 define을 사용하지 않는다. `windows-build.json`은 실제 Unity BuildReport의 파일 목록/크기를 기록한다. 제품 버전은0.1.0, 배포 버전은 manifest에0.1.0-preview.6으로 구분한다. 게임 코드와 Content는 PlayMode137/137을 통과한6a44585와 같다. 이번 배포는 패키지 도구·안내 및 Editor 빌드 기록만 변경한다.

패키지는 전체 파일의 크기와 SHA256, 필수 Unity 파일·폰트 라이선스, 사용자 저장·로그·개발 검수 코드 제외를 검사한다. 배포 도구의 checkpoint 메타데이터를 실제 V11에 맞추고 새 검수 타입도 차단한다. 압축 파일, 해시와 공개 상태 검증 결과는 후속 배포 기록에 남긴다.

배포 준비 검증: 실제 일반 BuildReport **성공/오류0/경고3**,139.376초, 파일199개. 실행 파일/Runtime DLL의 SHA256은 직전 VfxQuality 검증본과 동일하다. API 참조4개 컴파일, 배포 도구14/14 통과(22.511초). 이번 단계에서 전체 Unity 게임 테스트를 반복하지 않았다.

새 저장/캠페인 기록의 preview.5 이하 호환을 보장하지 않는다. 구6장 중단 기록은 보존하되 이어하기를 막는다. 기존 저장 성장·장비·골드·이력은 유지한다. 사람의 전체 캠페인 완주·장시간 성능 검증과 최종 상용 출시 판정은 별도다.
