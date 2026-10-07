# 새 아트 대표 전투 연출 검수 — 2026-10-01

6장 Windows 개발 플레이어를 Computer Use(`node_repl + @oai/sky`)로 조작하여 기본 공격, 공격/회복 스킬, 보스 반격과 재출전을 확인했다. 지난 화면 검수의 공격 미리보기 이후 실제 발동 흐름을 확인하는 후속 작업이다. 게임 코드·에셋·빌드는 변경하지 않았다.

## 실행 조건

- Unity6000.6.0f1, Windows11, RTX3060/i5-12400F, 1280×800. 캡처22장은 제목 표시줄을 포함한 원본 창 JPEG다.
- 기본 SPD/AI, 6장, 크레스Lv9·파라Lv9·민트Lv1의 기본3인 편성. 권장6인 난이도 평가를 위한 편성이 아니다.
- 격리 슬롯 `4974b814b4554128a3d15cf451038bba`. 정상 전투 자동 완주 `3739dcc79fc54d92b0b9f8feca3c8587`의 저장을 복사했다. 1990G/6장 완료 상태이며 수치·해금·HP 편집, 강제 피해, 자동 전투 명령을 사용하지 않았다.

## 확인 결과

| 항목 | 화면에서 확인한 동작 | 증거 |
| --- | --- | --- |
| 기본 공격 | 파라가 다오스 인접 타일로 이동 후 공격. 피해43 미리보기, 공격 자세/적색 피격 표시, 게이지0→20, 행동 사용과 Undo 비활성화 | [미리보기](01-basic-preview.jpg), [피격](02-basic-impact.jpg), [완료](03-basic-complete.jpg) |
| 마신검 | 크레스 스킬 상세→목표 선택→직선 범위→다오스 선택→실행. 기술명/시전 자세, MP109→103, 게이지0→20, 대기 복귀 | [상세](04-skill-details.jpg), [피해58](05-skill-preview.jpg), [시전](06-skill-windup.jpg), [비용](07-skill-cost.jpg) |
| 피해 일치 | 다음 다오스 턴에서 HP214/315. 두 공격의 미리보기43+58=101과 감소량 일치 | [다오스 HP](08-dhaos-damage-total.jpg) |
| 회복 | 민트 퍼스트 에이드 선택/시전, MP120→114. 이후 피격한 크레스에게 재사용하여 녹색 효과/+60 표시와 MP114→108 확인. 회복 미리보기77은 상한 적용 전 값이며 실제 회복 표시는60 | [미리보기](09-heal-preview.jpg), [첫 시전](10-heal-windup.jpg), [첫 비용](11-heal-cost.jpg), [재시전](19-heal2-start.jpg), [실제 회복](20-heal2-impact.jpg), [사용 후](21-heal2-result.jpg) |
| 다오스 블래스트 | 정상 AI가 기술 선택. 기술명/시전 자세→크레스·파라의 피격과 각각30 피해 숫자→다음 적 턴/블래스트 결과 문구 확인 | [턴 시작](14-boss-turn-start.jpg), [시전](15-boss-windup.jpg), [피격](16-boss-impact.jpg), [결과 문구](17-boss-recovery.jpg) |
| 방어·KO | Guard 사용 후 방향 선택. 정상 적 공격으로 파라 HP246→141→42, 이후 쓰러진 그림과 턴 목록 제외 확인. HP0 숫자 자체의 캡처는 없음 | [피격 후 HP](12-party-damaged.jpg), [쓰러진 상태](18-party-ko.jpg) |
| 재출전 | Restart→출전→6장 시작→도입 건너뛰기. 파라 생존/HP246/246·MP109/109·게이지0, 기본 위치와 명령 가능 상태 복원 | [복원](22-restart-restored.jpg) |

다오스의 첫 두 반격은 시작/이후 상태만 기록했다([첫 피해 누계](08-dhaos-damage-total.jpg), [두 번째 턴 시작](13-dhaos-start.jpg)). 세 번째 반격에서는 추가 입력 없이 약250ms 간격의 창 관찰로 블래스트의 짧은 발동 구간을 확보했다. 전체 프레임 영상이나 다오스 레이저의 직접 시각 검수는 아니다. 첫 회복은 대상의 직전 HP 숫자가 없어 실제 회복량을 별도로 주장하지 않는다.

## 보존·한계

- 사용자 원본/백업 SHA256과 격리 저장의 복사 원본/검수 종료 SHA256이 일치한다. 기존 두 exe/두 Runtime DLL도 이전 해시와 같다. [save-integrity.json](save-integrity.json).
- 수집기 errors.txt 없음, player.log의 Error/Exception/Assert 검색0건, 종료 후 TalesTactics 프로세스 없음.
- [events.txt](events.txt), [hardware.txt](hardware.txt), [performance.csv](performance.csv)는 약570초 혼합 전투/메뉴 구간의 수동 조작 중 자동 수집 기록이다. events.txt는 줄 끝 공백만 제거했다. 장시간 성능 검증으로 사용하지 않는다.
- 이 기록은 대표 동작 검수다. 전6장 수동 완주, 보스 격파, 모든 기술/방향의 연속 프레임 검수, 사람의 난이도 평가·전문가 외형 감수·실물 패드 검수를 완료한 것은 아니다.
- 코드/에셋 변경이 없어 Unity 테스트와 빌드를 재실행하지 않았다. 기존 EditMode93/93·PlayMode53/53 및 정상 전투 자동 완주 결과는 [VALIDATION.md](../VALIDATION.md)를 따른다.
