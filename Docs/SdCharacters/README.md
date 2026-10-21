# SD 캐릭터 원화 제작과 임포트

이 문서는 제작 구조와 재현 절차를 설명한다. 개별 원화의 정적 측정·육안 승인과 Unity 통합 검증은 별도 단계다. 최종 적용·테스트·빌드의 실제 결과는 [검증 기록](../VALIDATION.md)과 [SD 표현 검수 기록](../SdArtDirection/README.md)을 따른다.

## 원본 집합과 참조 보존

| 원본 폴더 (`Assets/TalesTactics/Art/`) | PNG | 스프라이트 이름 | 직접 참조 이름 |
|---|---:|---:|---:|
| PixelCampaign | 62 | 1,016 | 1,016 |
| MotionPremium | 10 | 760 | 760 |
| Crossover | 35 | 1,708 | 1,680 |
| SignatureWeapons | 52 | 1,414 | 1,414 |
| 합계 | 159 | 4,898 | 4,870 |

[production-manifest.json](production-manifest.json)은 Content와 전용무기 에셋에서 참조한 원본 시트 집합, 원본 GUID·SHA256, 스프라이트 이름을 기록한다. 참조하는 시트에서는 현재 직접 사용하지 않는 스프라이트 이름도 보존한다. 별도 `Art/Characters`의 미사용 구형 원화는 이 집합에 포함하지 않는다. 캐릭터의 이동·공격·시전·가드·피격·쓰러짐과 보정 방향 스트립은 원래 프레임 수·순서·이름으로 유지한다. 전용무기 시트는 해당 무기 디자인을 유지하며 기본 무기로 되돌리지 않는다.

원화는 built-in `image_gen`의 스타일 편집으로 제작한다. 원본을 정확한 포즈·배치 입력으로, 승인된 SD 원화를 그림체 참고로 구분한다. RGBA 원본 출력을 보관하며, 셰이더의 채도 변경을 새 캐릭터 원화의 대체물로 취급하지 않는다. 10인 초상화의 production 경로는 `Assets/TalesTactics/Resources/TalesTactics/FormationPortraits.png`이며 기존 5×2 순서와 UV 사용을 유지한다.

## 프롬프트와 검수 근거

- [generation-prompts.json](generation-prompts.json): 초기 크레스·초상화의 입력 역할, 원본 생성 경로, 공통 제작 프롬프트.
- [pixel-progress.json](pixel-progress.json): 주인공 PixelCampaign 41장과 MotionPremium 10장의 후보 경로·생성 파일·프롬프트·개별 검수 메모.
- [enemy-progress.json](enemy-progress.json): 일반 적과 PixelCampaign 다오스 21장의 제작 기록.
- [crossover-progress.json](crossover-progress.json): Crossover 35장의 제작·수정·검수 기록.
- [signature-progress.json](signature-progress.json): 전용무기 52장의 제작·수정·검수 기록.
- `*-measurement.json`: 원본/후보 해시, 원본 스프라이트 식별자, 모든 프레임의 rect·pivot·outline, PPU와 투명도, `visualApproved`·`visualReview`.
- [pixel-production-audit.json](pixel-production-audit.json): 주인공 51장의 정적 파일·프레임 일치 감사. 엔진 테스트 결과가 아니다.

각 진행 기록의 `prompt`와 있는 경우 `promptHistory`를 함께 읽는다. 승인되지 않은 실패 후보는 승인 후보 목록과 구분한다. 생성 원본의 파일명만 기록된 항목은 최초 생성 경로 안내와 함께 추적한다.

## 측정과 승인 절차

프로젝트 루트에서 Pillow·NumPy가 있는 Python으로 실행한다. 이 명령은 PNG 픽셀이나 Unity meta를 편집하지 않는다.

```text
python Tools/MeasureSdCharacterArt.py inventory
python Tools/MeasureSdCharacterArt.py measure <원본 PNG 상대경로> <후보 PNG 상대경로> <측정 JSON 상대경로>
python Tools/MeasureSdCharacterArt.py approve <측정 JSON 상대경로> "원본과 대조한 포즈·방향·장비 및 수정 사항"
```

`inventory`는 전환 전 원본 집합을 처음 기록하는 단계다. 적용 후 PNG/meta를 새로운 원본인 것처럼 재기록하지 않는다. 재제작할 때는 해당 제작 시점의 원본과 후보를 복구하고 해시를 맞춘 뒤 측정한다.

측정기는 원본 meta의 스프라이트 이름·순서를 기준으로 불투명 연결 성분을 대응시킨다. 장갑 두 개나 떨어진 무기처럼 한 스프라이트에 여러 성분이 있을 수 있다. 원본에 그려져 있지만 meta에서 불러오지 않는 인물은 별도 앵커로 추적해 유효 프레임으로 잘못 합치지 않는다. 54개를 불러오는 일부 Crossover 시트의 추가 18개 그림이 해당한다.

투명도·종횡비·프레임 누락·캔버스 경계·과도한 합침·모호한 대응을 검사하고, 각 알파 윤곽의 rect와 outline을 측정한다. 캐릭터의 가로 pivot은 원본 앵커를 새 rect에 대응시키며 세로는 발 기준을 사용한다. 이름이 방향형 캐릭터 패턴이 아닌 아이콘은 원래 중심 앵커를 보존한다. `footAnchorDeltaPixels`는 알파 하단 영역 중심과 원본 앵커의 차이이며, 임포트 pivot의 이동량이나 발 접지 통과 여부가 아니다. 누운 자세·긴 무기는 큰 값이 나올 수 있어 실제 애니메이션 검수가 필요하다.

수치 통과 후 모든 포즈를 원본과 육안 대조한다. 방향·손 위치·무기 각도·걷는 발·공격 중간 동작·KO를 확인하고 `approve`에 구체적 근거를 남긴다. 칸이 붙거나 잘렸으면 승인하지 않고 여백 또는 특정 무기 끝만 수정한 후 다시 측정한다. `directionOrder`는 일반 시트 설명값이며, `shionne-right` 같은 보정 스트립의 실제 방향은 원본 이름·이미지·개별 검수 메모를 따른다.

## Unity 적용과 보관

`SdCharacterArtImporter.ImportCandidate`는 후보 경로만 불러와 제작 중 비교한다. `ValidateProductionCoverage`는 manifest와 현재 Unity 의존성으로 얻은 전체 원본 집합의 정확한 일치, 중복 없음, 전체 육안 승인, 원본/후보 해시와 스프라이트 이름을 검사한다. `ApplyCompleteProduction`은 이 조건을 만족한 전체 집합에만 적용하며 원본 GUID·spriteID·local fileID를 유지한다. 예외 시 저장해 둔 원본 PNG/meta를 복원한다. 기존 콘텐츠 초기화나 예전 원화 임포터를 다시 실행하지 않는다.

실제 Unity의 참조·mesh·rect·PPU 확인과 6인 전장 가독성·동작 접지·방향 전환 검수는 이 정적 측정 이후 수행한다. 측정 통과 또는 `visualApproved`만으로 엔진 통합 완료를 선언하지 않는다.

제작 중 후보 경로는 `Assets/TalesTactics/Art/SdCharacters/`이다. 최종 통합 후 중복 후보 PNG는 로컬 `Temp` 보관 경로로 옮기고, Git에는 기존 production 폴더의 적용 PNG/meta와 측정·프롬프트 근거를 남기는 방침이다. 측정 JSON의 후보 경로와 해시는 **임포트 당시 provenance**이므로 후보 보관 후 현재 경로에서 재실행 가능한 설정이라는 뜻이 아니다. 생성 도구의 원본 출력은 유지한다.

## 적용 후 크레스 동작 보완

실제 56프레임 연속 캡처에서 기본 크레스 공격의 윤곽이 대기 동작보다 부드러워 보이는 점을 확인해 칼날·갑옷·머리 윤곽을 다시 그렸다. 새 원화는 동일한 1,254×1,254 해상도이며 해상도 증가로 표현하지 않는다. [정확한 프롬프트](cless-motion-clarity-prompt.txt), [후속 측정](cless-motion-refinement-measurement.json), [이전/이후 해시와 사유](cless-motion-revision.json)를 함께 보존한다.

`ApplyApprovedRevision(measurementPath, expectedCurrentSha256)`는 전체 최초 적용 이후의 단일 원화 교체용이다. manifest의 최종 승인 경로·원본 식별자·프레임 이름과 현재 적용 PNG의 기대 해시가 모두 맞아야 실행하며, 실패 시 교체 직전 PNG/meta로 복원한다. 최초 원본 해시를 현재 PNG로 바꿔 최초 적용을 다시 허용하지 않는다.

초기 `pixel-progress.json`, `pixel-production-audit.json`과 정적 후보 검수는 해당 제작 시점의 기록이다. 최종 manifest는 후속 측정을 가리키며, [등록 감사](registration-audit.json)는 원본 snapshot과 최종 후보 기준으로 159장·4,898프레임을 다시 대조한다. 실제 적용 완료의 근거는 [최종 Unity production 검사](../SdArtDirection/Validation/production-final.json)이다.
원화 임포트 내부에서 실제 메시·rect·PPU·픽셀 단위 피벗·GUID/local ID를 공통 검사하며, 실패는 백업 복원 범위 안에서 발생한다. 같은 공통 검사를 최종 production 감사에서도 사용한다. 실제 크레스80프레임 정상 확인 및 임시 측정값의 피벗·rect·PPU·local ID 변조4종 거부는 [읽기 전용 Unity 탐침](../SdArtDirection/Validation/import-guard-probe.json)에 기록했다. 이 탐침은 원화 파일을 바꾸지 않는다.
