# 새 아트 Windows 화면·마우스 검수

2026-09-30, 코드 기준 `bba7188`의 기존 Windows 빌드를 Computer Use의 `node_repl + @oai/sky`로 조작했다. 이전 세션의 창 활성화 오류가 이번 세션에서는 재현되지 않았다. 도구나 게임 코드를 수정하지 않았으므로 해당 오류의 근본 원인 해결을 의미하지 않는다.

실제 게임 영역은 1280×800이며 캡처에는 Windows 제목 표시줄이 포함된다. 그림은 Windows 창에서 직접 얻은 원본 JPEG다. 검수 중 누락된 그림, 메뉴 글자 잘림 또는 기본 카메라에서 전장을 가리는 HUD는 발견하지 못했다.

## 확인한 흐름

| 실행본 | 화면 기반 조작 및 관찰 | 증거 |
| --- | --- | --- |
| 일반 배포 | 출전 준비, 1장 도입 건너뛰기, 숲 전장/영웅/적 표시 | [출전](release-deployment.jpg), [1장](release-chapter1.jpg) |
| 일반 배포 | Move의 청색 테두리, 합법 타일 클릭과 보행/청색 경로, 이동 완료, Undo 원위치 복원, 우회전 | [이동 범위](release-move-range.jpg), [이동 완료](release-moved.jpg), [취소](release-undo.jpg), [회전](release-rotated.jpg) |
| 격리 개발 | 완료 저장 로드, 2~6장 선택/도입 첫 대사/건너뛰기/전장 진입/Restart, 목록 2페이지 전환 | [격리 출전](isolated-deployment.jpg), [2장](chapter2.jpg), [3장](chapter3.jpg), [4장](chapter4.jpg), [5장](chapter5.jpg), [6장](chapter6.jpg), [장 목록](chapter-page2.jpg) |
| 격리 개발 | 6장 청색 이동 범위, 파라의 다오스 인접 타일 이동, 적색 공격 사거리 | [이동](chapter6-move-range.jpg), [공격](chapter6-attack-range.jpg) |
| 격리 개발 | 다오스 타일 클릭 시 금색 선택 테두리와 피해43/100%/무속성×1 미리보기, 실행 버튼 활성화 | [실제 대상 미리보기](chapter6-dhaos-preview.jpg) |
| 격리 개발 | 대상 선택을 유지하며 확대·우회전·초기화, 취소 시 범위 표시 해제, Restart로 출전 복귀 | [확대](chapter6-zoom.jpg), [회전](chapter6-rotated-preview.jpg), [초기화](chapter6-camera-reset.jpg), [취소](chapter6-cancel.jpg), [복귀](chapter6-restart.jpg) |

금색 선택 범위는 실제 마우스 클릭으로 합법 대상인 다오스를 선택해 얻었다. 이전 Editor의 표시 함수 직접 호출 캡처와 구분한다. 공격 실행 버튼은 누르지 않았으며 이번 기록은 피해 판정이나 6장 수동 승리의 증거가 아니다.

## 저장·실행 증거

- 일반 배포는 기존 사용자 저장을 읽었다. 전투를 완료하거나 저장하는 메뉴를 사용하지 않았다.
- 2~6장은 `--manual-review 7d58cb747397401587b342fd7d2e02b8`로 실행한 별도 슬롯을 사용했다. 기존 정상 전투 자동 완주 `3739dcc79fc54d92b0b9f8feca3c8587`의 완료 저장을 복사했으며 값/해금/HP를 편집하지 않았다. 1990G와 기본 3인 편성(크레스·파라Lv9, 민트Lv1)이다. 2026-10-01 후속 전투 화면과 동일 원본 저장을 확인해 종전의 '기본3인Lv9' 표기를 정정했다. 이는 이번 세션에서 장들을 완주했다는 의미가 아니다.
- 사용자 원본·백업과 격리 저장의 시작/종료 SHA256이 각각 일치한다. 실행 파일2개와 Runtime DLL2개의 해시도 이전 `PixelInteraction/build-hashes.json`과 일치했다. [save-integrity.json](save-integrity.json) 참조.
- 개발 검수 수집기의 `errors.txt`가 없고 player.log의 Error/Exception/Assert 검색 결과는0건이다. 두 검수 플레이어를 종료한 뒤 실행 중인 TalesTactics 프로세스가 없음을 확인했다.
- [hardware.txt](hardware.txt), [events.txt](events.txt), [performance.csv](performance.csv)는 개발 세션의 수동 조작 중 자동 수집 기록이다. 저장소의 events.txt는 줄 끝 공백만 제거했다. 약442.6초의 메뉴/대기/전장 혼합 구간으로 성능 벤치마크나 장시간 안정성 시험으로 사용하지 않는다. Windows 작업 집합은 조회 불가로 빈칸이다.

이번 변경은 검수 문서와 증거만 추가한다. 코드·에셋·빌드를 변경하지 않았으며 Unity 테스트/빌드를 다시 실행하지 않았다. 기존 실제 EditMode93/93, PlayMode53/53, Windows 정상 캠페인 회귀 결과는 [VALIDATION.md](../VALIDATION.md)를 따른다. 모든 기술의 수동 발동, 새 아트로 전6장 수동 완주, 전문가 외형 감수, 사람의 체감 난이도·실물 게임패드 검수는 포함하지 않는다.
