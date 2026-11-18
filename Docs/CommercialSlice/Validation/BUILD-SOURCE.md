# 빌드 입력과 실행본 연결 기록

`Capture-CommercialBuildSource.ps1`은 이전 `Logs/RewardVariety20261110/build-inputs.py`와 `RewardVariety/Validation/after-binary-source.json`의 기록 구조를 확장한다. Unity·플레이어를 실행하지 않고 소스·사용자 저장·Git을 변경하지 않는다. 이 도구의 작성이나 파서 확인은 실제 빌드 검증 완료를 뜻하지 않는다.

## 실행 순서

최종 소스 편집과 테스트가 끝나고 Editor가 멈춘 상태에서 일반 빌드와 검수 빌드 각각 다음 순서를 수행한다. 캡처 중에도 소스를 수정하지 않는다. `SourceLabel`은 한 쌍에서 동일해야 한다.

1. 해당 출력 폴더가 없는 상태에서 `Before`를 실행한다. 출력 JSON의 `manifest` 절대 경로를 보관한다.
2. 기존 `Build-CommercialSlice.cs`로 해당 빌드를 만든다. 예약 응답이 아니라 `build-result.json`의 실제 성공을 확인한다.
3. 플레이어 실행 전에 `After`를 실행한다. `BeforeManifestPath`는 1의 경로, `BuildResultPath`는 해당 빌드의 최종 결과 경로다.

```powershell
# 일반 빌드 직전
& 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe' -NoProfile -ExecutionPolicy Bypass -File 'C:/dev/TalesTactics/Docs/CommercialSlice/Validation/Capture-CommercialBuildSource.ps1' -Phase Before -Kind Normal -SourceLabel 'CommercialSlice final working tree; local-only music retained'

# 일반 빌드 성공 직후: 두 자리표시자를 실제 절대 경로로 바꾼다.
& 'C:/Windows/System32/WindowsPowerShell/v1.0/powershell.exe' -NoProfile -ExecutionPolicy Bypass -File 'C:/dev/TalesTactics/Docs/CommercialSlice/Validation/Capture-CommercialBuildSource.ps1' -Phase After -Kind Normal -SourceLabel 'CommercialSlice final working tree; local-only music retained' -BeforeManifestPath '<일반 Before의 before-source.json>' -BuildResultPath '<일반 빌드의 build-result.json>'
```

검수 빌드도 `-Kind Review`로 같은 두 단계를 별도로 수행한다. 두 빌드의 입력이 같아야 하는 경우 최종 `inputAggregateSHA256`도 일치하는지 확인한다. 선택 사항인 `-TestSourceManifestPath`는 이미 작성된 테스트 소스 기록의 경로·크기·해시만 연결한다. 테스트 통과 여부를 판정하지 않는다.

`After`의 `manifest`가 가리키는 `after-binary-source.json`을 후속 Windows 검수의 `-SourceManifestPath`로 전달한다. 검수 실행에는 Review 쌍을 지정하고 Normal 쌍의 기록도 함께 보관한다. 후속 wrapper는 이 파일의 경로·해시를 기록할 뿐 내용을 대신 검증하지 않으므로 이 도구의 `passed=true`를 직접 확인한다.

## 기록과 판정 범위

매 호출은 `Logs/CommercialSlice20261117/BuildSource-<Kind>-<Phase>-<GUID>/`에 새 기록을 만든다. 출력·기록 경로에 junction 또는 다른 reparse point가 있으면 거부한다. 이전 파일을 덮어쓰거나 빌드 폴더를 정리하지 않는다. `Before`는 이미 존재하는 빌드를 소급 연결하지 못하도록 출력 폴더가 없어야 한다.

- `inputs-before.json` / `inputs-after.json`: `Assets`, `Packages`, `ProjectSettings` 전체 파일의 경로·크기·SHA256. 미추적·무시된 파일과 사용자 로컬 음원을 포함한다. Content·Resources·Runtime·씬·설정·의존성 선언을 모두 기록한다.
- `input-comparison.json`: 기존 파일의 변경뿐 아니라 추가·삭제도 비교한다. 시각만 바뀐 파일은 내용 변경으로 세지 않는다. 캡처 도중 파일 목록·크기·시각이 바뀌면 그 캡처를 거부한다.
- `output-files.json`: 빌드 전체 파일과 집계 해시. EXE·Runtime DLL·UnityPlayer DLL은 최종 기록에도 따로 표시한다. 음원 등 패키지 내부 콘텐츠는 해당 패키지 파일의 해시로 연결되며 원본 파일별 실제 포함 여부를 판정하지 않는다.
- `after-binary-source.json`: 사전/사후 입력 기록, BuildReport, 빌드 GUID·Unity 버전·define·실행 파일을 연결한다. 성공/보호 파일/도우미 해시/정확한 출력/씬/플랫폼/캡처와 빌드의 시간 순서도 검사한다. 하나라도 불일치하면 `passed=false`와 실패 이유를 남기고 종료 코드 1을 반환한다.

전체 Assets를 포함하므로 실제 패키지 의존성보다 넓은 입력 목록이다. 설치된 Unity와 도구 바이너리, `Library`의 import 캐시·PackageCache 자체는 해시하지 않는다. Unity 버전과 package manifest/lock은 기록한다. 사전·사후 동일성은 중간에 바뀌었다가 복원된 파일이나 동일 결과의 재현 가능한 컴파일까지 증명하지 않는다. 실제 테스트·화면·Windows 완주는 각각의 별도 증거를 사용한다.

음원과 이를 포함한 모든 빌드는 로컬 전용이다. 이 도구는 업로드하거나 커밋·푸시하지 않는다.