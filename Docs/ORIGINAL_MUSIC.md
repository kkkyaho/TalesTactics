# 프로젝트 오리지널 음악

원작 녹음·음원·샘플을 다운로드하거나 사용하지 않은 프로젝트용 합성 연주다. `Tools/ComposeOriginalMusic.py`에 음표, 조성, 템포, 화음, 악기 합성 및 편곡을 함께 보관한다. 기존 작품의 곡명/음원으로 표기하지 않는다.

| ID | BPM | 길이(초) | 역할 |
|---|---:|---:|---|
| battle | 132 | 29.09 | 기본 전투, 베이스·타악기·관악 계열 멜로디 |
| boss | 152 | 25.26 | 2장 전투, 빠른 단조 진행 |
| story | 84 | 22.86 | 이야기, 피아노와 지속 화음 |
| victory | 144 | 6.67 | 승리 팡파르, 반복 없음 |
| cless.theme | 132 | 7.27 | 검사의 관악 계열 테마 |
| mint.theme | 96 | 10.00 | 종소리 계열 테마 |
| velvet.theme | 144 | 6.67 | 낮은 단조 발현음 테마 |
| farah.theme | 148 | 6.49 | 빠르고 밝은 관악 테마 |
| tear.theme | 100 | 9.60 | 느린 관악 테마 |
| jade.theme | 120 | 8.00 | 피아노 테마 |
| natalia.theme | 124 | 7.74 | 밝은 발현음 테마 |
| alphen.theme | 140 | 6.86 | 상승하는 관악 테마 |
| shionne.theme | 112 | 8.57 | 단조 종소리 테마 |
| kisara.theme | 116 | 8.28 | 낮은 관악 테마 |

원본은 `Assets/TalesTactics/Audio/Original/*.wav`의 44.1kHz/16bit/스테레오 파일이다. 멜로디·화음·베이스·타악기를 개별 합성하고 짧은 스테레오 딜레이와 시작/끝 4ms 경계 처리를 적용한다. 최대 진폭은 0.72 이하이며 각 파일의 RMS·길이·SHA256은 `original-music-manifest.json`에 기록했다.

Unity에서는 Vorbis 품질 0.85로 임포트한다. battle/boss/story는 Streaming, 나머지는 CompressedInMemory와 preload를 사용한다. MusicVolume 기본값은 0.28, 효과음은 별도 소스의 0.45다. 궁극기는 연출이 진행되는 동안 테마의 첫 구간을 재생한 뒤 이전 음악의 샘플 위치로 복귀한다.

재생 흐름: 이야기 → 전투/보스 → 궁극기 테마 → 이전 전투 음악 → 승리. Restart는 음악·효과음·테마 복귀 예약을 정지한다. 기존의 무음 대체/음원 누락 처리는 계속 지원한다.

`Tools/ComposeOriginalMusic.py`는 이미 있는 WAV를 덮어쓰지 않는다. `Tools/ImportOriginalMusic.cs.txt`는 AudioLibrary의 Clip이 비어 있는 항목만 채우므로 사용자 음원을 보존한다. 제공 음원으로 교체할 때는 AudioLibrary의 해당 Clip과 SourceMetadata를 Editor에서 변경한다. 전체 초기 데이터 생성기는 실행하지 않는다.

합성 음악의 주관적인 악기 질감·음악적 선호는 사용자 피드백으로 조정할 수 있다. 파일의 신호/클리핑 검사와 실제 플레이어 출력 검사는 사람이 청음한 평가와 구분한다.
