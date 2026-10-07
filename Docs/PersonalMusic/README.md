# 개인용 BGM

음원 파일은 프로젝트/Assets/StreamingAssets/Resources에 넣지 않는다. 이 PC의 Application.persistentDataPath/PersonalMusic에서만 로드한다. Windows 기본 위치는 %USERPROFILE%/AppData/LocalLow/LocalTactics/TalesTactics/PersonalMusic이다.

- battle.mp3: 제공된 Mellow D Fantasy. 일반 전투·보스전·캐릭터 테마에 사용한다.
- story.mp3: 제공된 First Kiss (BJJ Original). 스토리·승리 화면에 사용한다.
- 파일이 없거나 읽지 못하면 기존 오리지널 음악을 사용한다. 효과음은 바꾸지 않는다.
- 동일한 개인 전투곡을 쓰는 궁극기는 곡을 처음부터 재시작하지 않는다. 승리는 반복하지 않으며 스토리/전투는 반복한다.
- 원본 파일은 수정하지 않고 개인 폴더에 복사했다. 음원과 음원이 포함된 배포물을 Git/릴리즈로 올리지 않는다. 연결 코드만 공유하며 일반 Unity 빌드에는 개인 폴더가 포함되지 않는다.

## 검증 (2026-10-08)

UnityWebRequestAudio 내장 모듈 추가 후 컴파일 오류0/경고0. 기존 음악 PlayMode3/3(6.91초) 통과: 원본14곡, 효과음 독립, 테마 재생 위치 복원/중단. 공통 테스트 준비에서는 PersonalMusicEnabled=false로 사용자 음원과 회귀 검사를 분리한다.

실제 제공MP3 로딩2/2 확인. 전투134.765초·스토리220.447초, 재생 중 샘플 위치 진행, 디코딩 PCM 비영점 확인. story→victory 반복 해제, boss/궁극기 동일 곡 유지, StopAll 후 재개 금지, 개인음원 비활성 시 원곡 대체를 Editor에서 검증했다. 소리가 스피커에서 실제 들리는지를 사람이 청취한 검사는 아니다. Editor 출력 샘플은0으로 관측돼 PCM/재생 위치 증거와 구분한다.

검수는 임시 저장 경로에서 수행했고 Play Mode 종료/runInBackground=false를 복구했다. 음원은 이 문서 폴더에도 포함하지 않는다. 다른 시스템/UI 작업 중인 파일은 이 변경에 포함하지 않는다.

개인음원 로더를 포함한 Windows 일반 빌드는 Builds/WindowsSystemUi/TalesTactics.exe이다. 공유 작업의 Docs/SystemUi/windows-build.json에서 Succeeded/38.877초/오류0/경고10을 확인했다. 원본14곡은 계속 빌드에 포함되며 개인MP3는 프로젝트 밖에서 읽는다. 빌드 폴더 내 MP3파일0건을 확인했다. 이 프로젝트의 개인음원 연결 코드만 커밋한다.
