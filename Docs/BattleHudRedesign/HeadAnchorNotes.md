# 머리 위 HP 위치 기준

`BoardUnitHudAnchors.cs`는 현재 표시 중인 스프라이트의 머리 꼭대기/가로 중심을 화면으로 투영한다. 원본 Sprite의 rect, pivot, pixelsPerUnit과 실제 SpriteRenderer Transform을 사용하므로 방향별 피벗, 캐릭터 몸체 배율, 카메라 회전, 모션의 이동·기울기·들썩임을 따른다. 머리와 게이지 사이의 간격은 화면 높이에 맞춘 5px이다.

`Resources/TalesTactics/UnitHeadAnchors.json`에는 Content가 참조하는 기본 4방향 **168개**의 위치가 있다. `Tools/BuildUnitHeadAnchors.py`는 원본 PNG 알파(128 초과)에서 머리 중심을 측정한다. 창/지팡이/도끼/꼬리가 더 높은 19개 방향은 검토한 머리 검색 영역을 사용한다. 두 골렘 원화는 같은 스프라이트 이름을 사용하므로 런타임 키는 이름과 원본 rect 크기를 함께 사용한다. 아틀라스 텍스처 이름에는 의존하지 않는다.

런타임은 처음 읽은 텍스트 데이터와 Sprite별 변환값을 캐시한다. Texture Read/Write, GPU readback, 매 프레임 알파 분석은 사용하지 않으며 아트/임포터를 변경하지 않는다. 별도 메타데이터가 없는 행동 프레임은 해당 방향의 기본 몸체 위치를 현재 렌더러 Transform에 적용한다. 따라서 공격 프레임의 긴 무기가 HP를 끌어올리지는 않지만, 개별 포즈의 머리를 프레임마다 추적하는 리깅은 아니다. 향후 미등록 아트는 해당 방향 스프라이트 bounds 상단으로 안전하게 대체한다.

재생성은 Pillow/NumPy가 설치된 Python에서 실행한다. 출력은 임시 디렉터리를 명시하며, 기존 Resources를 자동으로 덮어쓰지 않는다.

```powershell
python Tools/BuildUnitHeadAnchors.py --project . --output "$env:TEMP/TalesTacticsHeadAnchors"
```

출력된 `head-anchor-provenance.json`에는 측정 영역·피벗·원본 및 meta SHA256이 남는다. 원화나 rect를 변경한 경우 기본 4방향의 머리 위치 및 무기 제외 영역을 확인한 뒤 `UnitHeadAnchors.json`만 Resources에 복사한다. 이번 데이터는 별도 임시 폴더 재생성 결과와 SHA256이 일치했다. 실제 Unity 검증 결과는 상위 검증 문서에 기록한다.

`UnitHeadAnchorTests`는 전체 참조 방향의 메타데이터 누락, 파라의 투명 상단 여백, 제이드/보병의 창 끝 제외를 검사한다. PlayMode는 실제 renderer와 카메라 4방향 투영, 건강한 비선택 유닛 숨김, 마우스/피격/회복 표시, 6인 HP 겹침 및 로컬 배치 제한을 검사한다. 화면의 명령창이 머리 주변을 가렸을 때는 게이지를 다른 캐릭터 쪽으로 멀리 옮기지 않고 고정 상태창에 정보를 맡긴다.
