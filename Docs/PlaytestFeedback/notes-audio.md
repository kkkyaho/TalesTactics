# 지정 로컬 음악 연결 — 2026-11-02 작업 / 11-03 검증

사용자가 지정한 Downloads의 MP3 여섯 개를 `Assets/TalesTactics/Audio/LocalMusic`에 원본 바이트 그대로 복사했다. 원격 다운로드·원본 파일 수정·기존 프로젝트 곡 삭제는 없다. 기존 오리지널 음악14개와 효과음4개를 보존하고 새 음악 ID6개를 추가했다. 실제 Unity 테스트 결과와 Windows 재생 검수 상태는 아래에 구분하여 기록한다.

| 원본 파일 | 파일 메타데이터에서 읽은 제목 | 길이(초) | 재생 위치 |
|---|---|---:|---|
| `99AA233359F955C515.mp3` | Yume wa Owaranai ~ Psx Version | 257.959 | 타이틀 |
| `Tales of Destiny 2 - Wilderness.mp3` | 제목 태그 없음. 파일명의 Wilderness 사용 | 188.160 | 인터미션 거점 |
| `9953EC3359F9562305.mp3` | Fighting of The Spirit | 181.525 | 주요 보스전·동료 전용무기 시련 |
| `9993893359F955C218.mp3` | Threatening Sky | 119.536 | 협곡·문서고 지형의 일반 전투, 주요 보스전 전 대화 |
| `99898D3359F955C018.mp3` | Good Wine Makes A Good Blood | 85.577 | 성소·수문 지형의 일반 전투 |
| `99852C3359F955BE1A.mp3` | Decisive | 96.601 | 마지막20화 전투 및 전투 전 대화 |

Windows Shell의 `System.Media.Duration`, `System.Title`, `System.Audio.SampleRate`, `System.Audio.ChannelCount`를 읽었다. 여섯 원본은 모두44,100Hz·2채널이며 총26,788,762바이트다. 이는 메타데이터 확인이며 전곡 청취 평가는 아니다. 원본과 프로젝트 복사본의 SHA-256이 일치했다.

| 파일 | SHA-256 |
|---|---|
| `99AA233359F955C515.mp3` | `391CFFF8272AE7D6D05BAD77001A85AFBB14F14B2F704B9B8A93498628D9539E` |
| `Tales of Destiny 2 - Wilderness.mp3` | `C9E16EED15BA9C8A0BCB7D7E718932BAEAB08F5B2E540622B6705E9BDC109D12` |
| `9953EC3359F9562305.mp3` | `15B974B7CBF295E40DE0D5FD03848A2ED259839A9839502A683C68E05EC4E3C2` |
| `9993893359F955C218.mp3` | `7E86314D9C6CA6896B36E4E8E49ED4D9ED0FB49876871D74B75A34E4E2B0F3EC` |
| `99898D3359F955C018.mp3` | `9F19FFD36FE67296AC6017543D9D05974558E6D6978B216CDD6280CFD9AF6302` |
| `99852C3359F955BE1A.mp3` | `60795178D607B4F39B33AE060761C5BAC2CA9E4785128B6E0FC56C349C7B7704` |

`BattleMusicProgram`은 진행 중인 `Session.CampaignStage`, 보스 표시와 기존 지형 테마를 기준으로 선곡한다. 메뉴의 선택 장이 바뀌어도 실행 중인 전투 음악은 바뀌지 않는다. 새 게임·불러오기·타이틀 복귀·거점 재표시, 대화·전투·중단 재개·되감기·재도전에서 기존 진입 함수를 이용한다. 거점의 탭을 다시 그려도 재생 중인 같은 곡은 처음부터 시작하지 않는다.

기존 `battle`은 훈련·유적 일반전, `boss`는 일반 중계핵 지형 전투와 새 곡이 없는 호환 카탈로그의 보스전, `story`는 보통 대화와 결전 이후, `victory`는 승리 결과에 유지한다. 기존 캐릭터10개 테마는 비오의 및 해당 동료의 개인 이야기에서 사용한다. 지정 곡이 없는 카탈로그는 기존 `battle`/`boss`/`story`로 돌아간다. 선곡은 전투·저장 상태나 난수를 변경하지 않는다.

기존 `PersonalMusic/battle.mp3`, `story.mp3` 개인 대체 기능은 기존 ID에 대해 유지한다. 새 지정6곡에는 적용하지 않으므로 타이틀·거점·새 전투 곡이 개인 대체 파일에 의해 덮이지 않는다. 음악/효과음은 기존 별도 AudioSource와 저장된 음량 설정을 사용하며, 비오의 종료 시 이전 음악의 클립·샘플 위치·반복/재생 상태를 복원한다.

가져오기는 `LocalMusicImport.Apply()`가 원본 SHA를 검사한 뒤 Unity `AudioImporter.SaveAndReimport()`를 호출하고 라이브 값을 다시 읽는 명시 작업이다. 기존 Content 초기화 함수는 실행하지 않는다. 새6곡의 설정은 **Streaming, Vorbis 품질0.85, 원본 샘플레이트 보존, stereo, preload 및 Load In Background**이며 최종 EditMode 검사에서 확인했다. MP3 복사본은 보존하지만 Unity 빌드용 데이터는 Vorbis로 재인코딩된다. MP3의 기존 손실을 복원하거나 측정하지 않은 음질·메모리 개선을 주장하지 않는다.

사전 Editor 측정은 `Logs/PlaytestFeedback20261102/editor-audio-baseline.json`에 있다: StandaloneWindows64, 출력48,000Hz, DSP1,024×4, AudioListener1개, AudioMixer0개, 정지 씬의 음악 소스1개(클립 없음·2D·Mixer 미지정), 씬 변경 없음. 긴 음악의 메모리를 한꺼번에 PCM으로 펼치지 않도록 스트리밍을 선택했으며 기존 소스·Mixer 구조와 장치/DSP 설정은 변경하지 않았다.

검증 항목:

- 새 EditMode2개: 기존14곡 참조 유지, 새6곡의 실제 importer 설정·유효 길이/채널·샘플레이트, 훈련·일반·보스·최종전·개인 이야기 선택, 전체56개 장에서 존재하는 음악 ID 및 RNG 보존.
- 새 PlayMode4개: 새6곡의 실제 클립 연결·샘플 진행·루프·음악/효과음 음량 분리·음소거·종료; 실제 타이틀→새 게임→거점→대화→전투→거점→타이틀 및 저장 음량; 실제4개 전투 장의 상황 선택·실행 중 장 우선·비오의 후 스트리밍 위치 복원·거점 전환 후 낡은 테마 복귀 취소; 메인13화와 동료2부의 경로 선택을 취소했을 때 인터미션 음악 및 원래 준비 메뉴 복귀·캠페인/저장 바이트/RNG 보존.
- 기존 오리지널14곡 재생 검사와 비오의 위치 복원 검사도 유지한다.
- `LocalMusicReview`는 Editor/`TALESTACTICS_REVIEW` 전용이다. Windows 실행 인수는 `--local-music-review <32자리 GUID>`이며 Director의 최초 로드 전에 `ReviewStorageBootstrap`으로 `LocalMusicReviews/<GUID>/Storage`에 격리한다. 결과는 같은 실행 폴더의 `result.json`이다. 최대120초, 실제6곡을 포함한14구간에서 재생 커서·DSP 진행·출력 파형의 비영점 peak/RMS를 기록하고 실제 화면 전환·비오의 복귀·음소거·저장 음량 복구·종료를 검사한다. `Passed`일 때만 정상 종료 코드0, 실패는1이며 잘못된 초기화는2다. 기존 사용자 최상위 파일SHA(정확한 Unity로그2개 제외)는 검수기에서, 재귀 파일/디렉터리 및 원래 실행 중인 게임은 외부 실행 래퍼에서 보호한다.

실제 실행 결과:

- 최종 전체 EditMode는 **469/469 통과, 17.97초**다. 새 `LocalMusicTests` 2개도 모두 통과했다. [실행 원문](../../Logs/PlaytestFeedback20261102/full-edit-final3.json)
- 전체 PlayMode는 **281/282 통과, 712.14초**다. 위 새 오디오 4개 검사는 모두 통과하여 실제6곡 재생 커서, 화면별 전환, 음량 설정, 비오의 복귀, 메인·동료 경로 선택 취소 후 인터미션 복귀를 확인했다. 유일한 실패는 기존 상점 fixture `SevenStandardWeaponCardsPurchaseEquipAndReloadThroughActualShop`였다. [전체 실행 원문](../../Logs/PlaytestFeedback20261102/full-play-final1.json)
- 해당 상점 fixture 보정 후 별도 실행은 **1/1 통과, 9.10초**다. 전체 PlayMode 282개를 한 번에 모두 통과한 결과로 합산하지 않는다. [상점 재실행 원문](../../Logs/PlaytestFeedback20261102/shop-fixture-final.json)
- Windows `LocalMusicReview`의 **14/14구간과 외부 실행 래퍼가 통과**했다. 한국시간 2026-11-03 00:05에 검수 빌드 `PlaytestFeedbackReview20261102Final3`를 숨김 창 1024×768로 실행했다. 타이틀·새 게임·거점·대화·전투·복귀, 지정6곡 직접 재생, 비오의 테마 및 이전 곡 복귀에서 실제 클립 연결·재생 커서와 DSP 진행·비영점 peak/RMS를 관측했다. 지정6곡은 모두 Streaming/Loaded, stereo 44,100Hz이며 출력 장치는48,000Hz, listener1개, DSP1,024×4였다. 기존14곡 연결, 반복 재생, 음소거·종료·저장 음량 복구와 직접 재생 중 캠페인/RNG 보존도 통과했다. [플레이어 원문](Validation/windows-music-player-report.json), [14구간 수치](Validation/windows-music-audio-manifest.json)
- Windows 검수는 전용 `LocalMusicReviews/5a3a52233f48485cb962dc78ada41685/Storage`에 격리했다. 실행 소유 PID80628이 **종료 코드0**으로 끝났고 강제 종료·cleanup 요청은 없었다. 사용자 전체 **4,959파일·2,383디렉터리**의 경로·SHA가 전후 동일했다. 정확한 최상위 Unity 로그2개만 제외했으며 원래 실행 중인 플레이어는0개였다. 일반 Final2 빌드의 검수 타입·참조 부재와 Final3의 부트스트랩 연결은 별도 metadata/IL 검사도 통과했다. [래퍼 요약과 원문 경로·SHA](Validation/windows-music-summary.json), [플레이어 로그](Validation/windows-music-player.log), [로그 오류 검색0건](Validation/windows-music-log-findings.json)

최종 검수 빌드는 **Succeeded, 33.5064796초, 오류0·경고7**이다. `LocalMusicReview.cs`의 사용 중단된 `FindObjectsByType` overload를 현재 overload로 바꾼 후 해당 경고2개는 사라졌다. 남은 경고는 Pipeline 설정 부재1개, 미컴파일 코드 변경 알림1개, 기존 `SdMotionDirectionReview`의 API 사용 중단4개, 기존 `VfxUpgradeReview`의 전처리 지시어 사용 중단1개이며 무경고 빌드로 표현하지 않는다. 일반 Final2에는 검수 전용 `LocalMusicReview` 코드가 포함되지 않는다. [최종 BuildReport](Validation/build-review-final.json)

Windows 수치는 재생 시작·짧은 진행 구간에 대한 관측이다. 일부 인접 cue의 peak/RMS가 동일하여 출력 버퍼에 이전 cue의 잔여 샘플이 포함될 가능성을 배제하지 않는다. 클립 연결과 커서 진행은 각 행에서 확인했지만, 파형 수치로 곡별 음원을 고유하게 식별하거나 전곡을 청취한 결과로 확대하지 않는다. 큰 재귀 사용자 목록은 버전 관리 요약에서만 생략하며 원문 경로·SHA와 개수는 보존했다. 플레이어 보고서·오디오 manifest·로그·로그 검색 결과는 원본과 SHA가 같은 복사본이다.

범위 밖: 원곡 자체 편집·음량 정규화·새 루프 구간 제작·크로스페이드·전곡 청취 및 모든 장의 수동 완주. 기본 루프는 파일 전체 반복이며 소스에 포함된 도입/끝 여백은 유지한다.
