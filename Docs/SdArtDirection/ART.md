# SD 미술 전환 — 원화와 재현 기록

사용자가 확인한 디스가이아풍 시안을 기준으로, 큰 얼굴과 압축된 체형, 짙은 남색 윤곽, 3단계 셀 명암을 실제 게임 자산에 적용한다. 별도 게임의 원화나 음원을 다운로드하지 않는다.

기준 소스는 `c7c4940f583ee834934e1ffbc5ccb28d4d1a74ab`이다. 이 문서는 원화 출처와 처리 방법을 기록하며 최종 Unity 검증 완료를 뜻하지 않는다. 최종 검증은 README와 VALIDATION의 실행 결과를 따른다.

## 제작 방식

모든 새 래스터 원화는 기본 제공 `image_gen`으로 생성/편집했다. CLI/API fallback은 사용하지 않았다. 편집 대상은 먼저 이미지로 확인했으며, 생성 결과를 육안 검토하고 프로젝트에 복사했다. Python은 알파 영역·연결 요소·기존 메타데이터의 읽기와 표시 사각형 측정에만 사용한다. PNG 픽셀을 필터링·합성·리사이즈하는 데 사용하지 않았다.

공통 화풍 참조는 `C:/Users/USER-PC/.codex/generated_images/01a1382c-15f7-70d2-a550-3a56303cb362/exec-d19f55af-aedf-4051-8777-de2bc6eca992.png`이다.

생성 지시의 공통 요약(원문 그대로의 인용은 아님):
- 원본의 의미, 수량, 배치, 방향과 실루엣을 유지하고 짙은 남색 선·넓은 셀 명암으로 재일러스트한다.
- 캐릭터는 표현력 있는 SD 얼굴과 짧은 체형으로 바꾸되 프레임별 자세, 무기 손잡이 방향, 쓰러진 자세, 앞·뒤·좌·우를 보존한다.
- 투명 아틀라스는 실제 알파와 셀 사이 여백을 유지한다. 배경색·체커보드·바닥 그림자·추가 글자를 넣지 않는다.
- UI는 남색 바탕과 절제한 황동 장식, 청록색 선택 상태를 사용한다. 실제 사물 일러스트를 기하학적 기호로 대체하지 않는다.
- 지형은 기존 보행·카메라·장면의 구조를 유지하고, 사진 같은 미세 잡음을 줄여 넓고 또렷한 면으로 그린다.

개별 실제 프롬프트와 원본 파일은 [캐릭터 기록](../SdCharacters/generation-prompts.json), 각 `*-progress.json`·`*generation*.json`, [UI 아이콘](../SdUiTheme/icon-generation.json), [UI 프레임](../SdUiTheme/ART.md), [파노라마 6종](PANORAMA_ART.md)에 보관한다.

## 타이틀·거점·지형 채택 원화

아래 생성 원본의 공통 폴더는 `C:/Users/USER-PC/.codex/generated_images/01a1382c-15f7-70d2-a550-3a56303cb362/`이며 원본은 그대로 유지한다. 최종 리소스의 공통 폴더는 `Assets/TalesTactics/Resources/TalesTactics/`이다.

| 프로젝트 리소스 | 채택 생성 파일 |
|---|---|
| TitleBackground.png | exec-11de0e63-75f8-46c0-90be-cd501734a416.png |
| FormationBackdrop.png | exec-87068c42-3980-4701-9336-9d16f99d2ac5.png |
| Ornate/Trees.png | exec-cd7bc513-e383-4b04-a25a-80432dcb7ec4.png |
| TerrainArt/Surfaces.png | exec-ac051446-67db-465c-92ac-e277f8072bb0.png |
| TerrainArt/RuinsFloor.png | exec-787729b4-b047-4a13-9326-5ea40661d510.png |
| TerrainArt/CanyonFloor.png | exec-217505c9-472b-455f-9a89-16b7f17fabd5.png |
| TerrainArt/ObservatoryFloor.png | exec-5c688001-aa13-4e9a-b00f-e068262918db.png |
| TerrainArt/SluiceFloor.png | exec-65def42d-077a-45ca-9ce2-e68b8813ebf8.png |
| TerrainArt/Architecture.png | exec-d125f274-ee66-440d-85b7-2fbab66b5ef9.png |
| TerrainArt/Foliage.png | exec-a86724cb-cba4-4799-9080-0e066c77f05c.png |
| Ornate/Ground.png | exec-664d9f58-3a19-43a4-a418-037b01f4cc48.png |
| TerrainArt/Cliff.png | exec-63ba0200-00f8-48c6-9c70-38ef01ca892e.png |
| TerrainArt/WallFacade.png | exec-5222bf59-593f-48fd-89eb-7ef4b89a91a3.png |

파노라마 6개는 [별도 기록](PANORAMA_ART.md)을 따른다. 기존 리소스 GUID는 유지한다. Trees·Architecture·Foliage의 실제 불투명 범위는 [측정 사각형](terrain-atlas-regions.json)과 `SdTerrainAtlas`에 반영한다. 지형 셰이더는 명암 단계를 정리하고, 파노라마는 전장과 구분되도록 채도·명암을 조절한다. 전장 메시의 칸 좌표·고저차·충돌·이동 규칙은 그대로다.

## 캐릭터 전체 교체

`Docs/SdCharacters/production-manifest.json`은 Content와 전용무기 에셋이 참조하는 **159장, 스프라이트 이름 4,898개, 실제 참조 이름 4,870개**를 명시한다. 각 측정 기록은 교체 이전 PNG 해시·GUID·스프라이트 ID/local ID, 후보 PNG 해시, 표시 사각형·피벗·윤곽을 담는다.

`SdCharacterArtImporter.ApplyCompleteProduction`은 실제 Unity 의존성과 목록이 정확히 일치하고 모든 원화가 측정·육안 승인된 경우에만 실행된다. 기존 스프라이트 이름/ID를 유지하고, 실패 시 원본 PNG/meta를 복원한다. 기존 Content 생성기는 다시 실행하지 않는다.

`SdArtValidation.AuditProduction`은 교체 이후에도 새 PNG 해시, 전체 프레임 이름, GUID/local ID, 실제 스프라이트 메시·사각형·PPU와 전용무기 참조를 검사한다. 측정 데이터는 임포트 당시 증거이므로 변환을 마친 원본에 초기 임포터를 무조건 다시 실행하지 않는다.

## 실제 화면과 검증

Unity 실행 캡처는 시안과 구분한다. 전장 미리보기 20장은 새 원화 적용 이후 실제 전장 카메라로 다시 만든다. 편성 6명·스크롤·창 내부 여백·저장 데이터·전투 판정은 별도로 검사한다. 투명 배경이나 프레임 이름의 기계 검사만으로 화풍·애니메이션 품질 검수가 끝났다고 판단하지 않는다.



크레스 MotionPremium 후속 원화는 같은 1,254×1,254 크기에서 공격 윤곽을 보완했다. [후속 원화·측정·해시와 적용 절차](../SdCharacters/README.md#적용-후-크레스-동작-보완)를 따른다. 새 공통 SdPanel의 실제 nine-slice는240px이며, 기존170px 임포트 규칙이 덮어쓰지 않도록 별도 구분한다.
