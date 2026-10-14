# Phase 1 검증 기록

검증일: 2026-10-14 (한국 시간). Unity 2022.3.62f3 Windows Editor.

## 실제 수행한 검증

- Unity `-batchmode -nographics -executeMethod ReboundSceneBuilder.Build`: 성공. Game 씬, 설정 에셋, 프리팹, 도형 이미지 생성 확인.
- Unity 스크립트 컴파일: 성공. C# 컴파일 오류 없음.
- Unity Test Framework **PlayMode**: 최종 **6개 통과, 0개 실패**, 실행 시간 약 **8.74초**.
- Windows x64 Player 빌드: **성공**. `Builds/Windows/REBOUND.exe`와 데이터·런타임 DLL 생성 확인. 빌드 로그: `UnityWindowsBuild.log`.
- 결과 원본: `TestResults/playmode-final.xml`.
- 로그: `UnityCreate.log`, `UnityBuild.log`, `UnityTestsFinal.log`. 로그·테스트 결과는 로컬 산출물로 Git에서 제외합니다.

| 테스트 | 확인 내용 | 결과 |
|---|---|---|
| `MovementRiseGravityAndBounds` | 좌우 이동, 상승 속도, 중력에 의한 하강 전환, 좌우·상단 경계, 최대 낙하 속도 | 통과 |
| `RiseCooldownAndEnemyContact` | 상승 0.25초 간격, 간격 경과 후 재상승, 적 접촉 피해, 무적 중 추가 접촉 피해 차단 | 통과 |
| `EnergyUsesDistanceAndReboundOnlyHitsFront` | 상승 시 충전 없음, 거리 기반 충전, 기준 미달 소비 차단, 전방 범위 피해, 뒤·거리 밖 적 제외 | 통과 |
| `RealDescentChargesAndRiseConsumesEnergy` | 실제 물리 하강으로 65 이상 충전, 상승 입력과 반동 발동·에너지 소비 연계 | 통과 |
| `AutomaticFireKillsAndPoolReuses` | 자동 사격과 실제 Collider2D 충돌로 적 처치, 탄환 반환·재사용, 중복 반환 방지 | 통과 |
| `FallDamageInvulnerabilityGameOverAndRetry` | 추락 피해, 안전 위치 복귀, 에너지 초기화, 무적, HP 0 종료, 시간 정지, 실제 Game 씬 재로딩 | 통과 |

자동 테스트는 입력 수집 이후의 `SetMoveAxis`/`RequestRise`를 호출합니다. 물리·충돌·씬 로딩은 실제 Unity 엔진에서 실행했지만 OS 키보드 이벤트를 주입한 테스트는 아닙니다.

## 직접 확인이 남은 항목

헤드리스 테스트이므로 실제 Game 뷰의 화면 품질, 마우스로 Retry 클릭, 실제 A/D·Space 키보드 포커스, 30/60/144 FPS의 체감 차이는 확인하지 않았습니다. Windows 실행 파일은 빌드까지 확인했으며 직접 실행해 조작하지 않았습니다. 자동 테스트 통과를 수동 플레이 검증으로 간주하지 않습니다.

수동 확인 순서:

1. README 순서로 Game 씬을 열고 Play, Game 뷰를 클릭합니다.
2. A/D를 길게 눌러 왼쪽 40% 영역의 양 끝을 확인합니다.
3. Space 한 번으로 상승하고 손을 떼면 떨어지는지 확인합니다. 길게 누르기만 해서는 연속 상승하지 않아야 합니다.
4. Space를 약 0.3초 간격으로 눌러 상단 제한을 확인합니다.
5. 약 3유닛 하강한 뒤 게이지가 노란색으로 바뀌면 Space를 누릅니다. 전방 충격파와 에너지 소비를 확인합니다.
6. 적과 높이를 맞춰 기본 탄환 3발로 처치되는지 확인합니다.
7. 하단으로 추락해 HP 1 감소, 안전 위치 복귀, 점멸을 확인합니다. 총 3회 추락하면 Game Over가 나와야 합니다.
8. Retry 버튼과 R 키를 각각 사용해 HP 3, 에너지 0, 처치 수 0으로 초기화되는지 확인합니다.
9. Game 뷰 비율을 바꿔도 플레이 영역과 HUD가 같은 16:9 영역에 유지되는지 확인합니다.

## 테스트 재실행

Editor에서 **Window → General → Test Runner → PlayMode → Run All**.

명령줄에서는 프로젝트를 열고 있는 Editor를 닫은 후 아래 PowerShell 명령을 실행할 수 있습니다. `-runTests`에는 `-quit`를 추가하지 않습니다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Users\GCT\Documents\ChatGPT\2번째 MOD게임' `
  -runTests -testPlatform PlayMode `
  -testResults 'C:\Users\GCT\Documents\ChatGPT\2번째 MOD게임\TestResults\playmode-final.xml' `
  -logFile 'C:\Users\GCT\Documents\ChatGPT\2번째 MOD게임\UnityTestsFinal.log'
```

## 알려진 제약

- 기능 자동 테스트에서 재현된 실패는 없습니다. UI 시각 검증과 실제 조작 감각은 위 순서로 확인해야 합니다.
- UI는 Phase 1용 영문 IMGUI이며 접근성·한글 현지화·정식 UI 디자인은 후속 작업입니다.
- 적 생성·제거와 반동의 적 목록 스냅샷에는 할당이 있습니다. 기본 탄환만 고정 크기로 풀링합니다.
- 기본 탄환 풀을 과도하게 줄이거나 발사 속도를 높여 풀이 고갈되면 해당 발사를 생략합니다.
- 상단 HUD 공간을 확보하기 위해 실제 상단 이동 경계는 월드 Y=3.4입니다.
- 에너지 기준 충족 시 다음 유효 상승 입력에서 자동으로 반동을 사용하며, 별도 발동 키는 없습니다.
- MOD JSON 파서·경험치·레벨업 시스템은 Phase 2 범위로 포함하지 않았습니다.
