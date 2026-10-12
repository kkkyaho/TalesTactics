# 생성 아트 지침

도구: 내장 `image_gen` 사용. 생성 이미지를 프로젝트 `Assets/TalesTactics/Resources/TalesTactics/TerrainArt/`에 원본 그대로 복사했다. 생성 시안은 `Concepts/`, 실제 Unity 캡처는 이 폴더의 `chapter*.png`로 구분한다.

다음은 에셋별 최종 제작 지침을 요약한 프롬프트 세트다.

| 에셋 | 제작 지침 |
| --- | --- |
| Surfaces | 4×4 재질 아틀라스. 정면 투영 석재·황동·수면·청회색 건축 재질. 칸마다 경계가 명확하고 글자나 UI 없이 고밀도 회화 디테일. |
| Foliage | 4×4 투명 장식 아틀라스. 덩굴·풀·건조 식생·물보라·빛. 각 칸 중앙에 독립 배치. |
| Architecture | 4×4 투명 건축 장식 아틀라스. 무너진 유적 기둥·램프·협곡 바위·황동 천문기구·깃발·수문 바퀴·수정. 게임 캐릭터와 어울리는 등각 2.5D 표현. |
| Ruins / Sanctum | 수직 깊이가 있는 고대 유적과 숲, 이끼 낀 아치·석교·폭포·안개, 청록/황동 색감의 원경. |
| Canyon | 해질녘의 사암 협곡·오래된 석교와 먼 성곽, 절벽의 깊이와 대기 원근. |
| Observatory / Relay | 푸른 밤하늘·높은 관측탑·천문기구·황동 장식과 다층 석교. |
| Sluice | 청색 수면·수로·거대한 수문과 황동 기계 장치, 먼 폭포와 안개. |
| 각 Floor | 정사각형 전체를 잇는 정투영 바닥. 무너진 유적 석판, 사암 바닥, 청회색 천문 문양, 수문용 석판과 금속 덮개. 타일 하나마다 같은 문양을 반복하지 않는 큰 구성. |
| WallFacade | 정면 직교 투영의 네 개 아치와 조각된 기둥, 코니스·돌의 마모·덩굴. 3:1 전체 폭을 채우고 하늘·원근·텍스트 없음. |

Cliff의 최종 전달 프롬프트:

> Production game asset: a single seamless horizontally and vertically tiling sandstone cliff wall diffuse texture, full bleed square. Premium extremely detailed painterly pixel-art aesthetic for a high-end 2.5D tactical fantasy RPG. Orthographic FRONT view of natural weathered canyon rock, NO perspective, NO sky, NO horizon, NO ground plane, NO frame, NO UI or text. Large irregular vertical sandstone slabs and deep natural fissures, fine carved wind erosion, layered sediment, pale beige highlights, muted warm russet and gray-brown shadows, sparse tiny dry roots. Natural irregular believable geologic shapes, NOT brickwork, NOT regular horizontal rectangular blocks, NOT a grid. Rich material depth with subtle ambient occlusion in fissures, soft even diffuse lighting, no cast shadows. Broad quiet rock surfaces alternating deep cracks so detail remains legible zoomed out. Restrained desaturated ochre palette, avoid vivid orange or lava red. Fill every pixel with cliff rock surface. Request high resolution square texture.
