# REBOUND — Phase 1

독자적인 PC 2D 횡스크롤 중력 자동 슈팅 프로토타입. 외부 게임 이미지·UI·음원은 사용하지 않습니다.

## 환경 및 실행

작업 시작 당시 폴더에는 `.git`만 있었으므로 새 Unity 프로젝트를 구성했습니다.

- Unity **2022.3.62f3 LTS**, Built-in Render Pipeline, 2D 모드.
- `Packages/manifest.json`: Unity 기본 모듈 + `com.unity.test-framework` **1.1.33**. Input System, URP, TMP 등의 추가 의존성 없음.
- `ProjectSettings/ProjectSettings.asset`: **Active Input Handling = Input Manager (Old)** (`activeInputHandler: 0`). `Input.GetKey(A/D)`와 `Input.GetKeyDown(Space)`를 사용하므로 축 매핑 작업 불필요.
- Windows x64, 기본 창 1280×720. 카메라는 창 비율이 달라도 16:9 플레이 영역을 유지합니다.

1. Unity Hub에서 **Add → Add project from disk**로 이 폴더를 등록합니다.
2. **2022.3.62f3**로 열고 초기 임포트가 끝날 때까지 기다립니다.
3. `Assets/Rebound/Scenes/Game.unity`를 더블클릭합니다. 또는 상단 **REBOUND → Create or Open Phase 1 Game**을 실행합니다.
4. **Play**를 누르고 **Game 뷰를 클릭해 키보드 포커스**를 줍니다.
5. **A/D** 이동, **Space를 매번 눌러** 상승합니다. 누르고 있는 것만으로 연속 상승하지 않습니다. 사격은 자동입니다.
6. 약 3유닛 하강하면 에너지가 65 이상 충전됩니다. 추락하기 전에 Space를 누르면 상승과 전방 반동 공격이 함께 발동합니다.
7. HP가 0이 되면 **Retry 버튼 또는 R**로 다시 시작합니다.

메뉴는 기존 Game 씬이 있으면 열기만 하며 씬·설정·프리팹을 덮어쓰지 않습니다. 씬을 의도적으로 다시 만들려면 기존 씬을 먼저 다른 이름으로 보관하고 메뉴를 실행하세요. 새로 만든 씬은 Build Settings에 자동 등록됩니다.

설정 확인 위치: **Edit → Project Settings → Player → Other Settings → Active Input Handling → Input Manager (Old)**. 이 프로젝트에는 이미 적용되어 있습니다.

Windows 실행 파일 생성: **REBOUND → Build Windows Prototype**. 결과는 `Builds/Windows/REBOUND.exe`이며 배포 시 같은 디렉터리의 Data 폴더와 DLL도 함께 전달해야 합니다.

## 구현 구조 및 파일 목록

`Assets/Rebound/Runtime/` 아래에 다음 코드를 생성했습니다.

| 파일 | 책임 |
|---|---|
| `PrototypeSettings.cs` | 이동·체력·무기·적·에너지 밸런스 데이터. 추후 JSON 값 주입 대상으로 실행 로직과 분리 |
| `PlayerController.cs` | Update 입력 수집, FixedUpdate 상승·중력·이동·경계·추락 복귀 |
| `PlayerHealth.cs` | HP 3, 접촉 피해·추락 피해·1.2초 무적·점멸 |
| `WeaponController.cs` | 오른쪽 자동 발사, 물리 틱 기반 발사 간격 |
| `ProjectilePool.cs` | 32발 사전 생성, 대여/반환, 고갈 시 해당 발사 생략 |
| `Projectile.cs` | 투사체 이동, 적 충돌, 수명·화면 이탈·게임 종료 시 반환 |
| `EnemyController.cs` | 적 HP·왼쪽 이동·플레이어 접촉·처치·화면 이탈 |
| `EnemySpawner.cs` | 오른쪽에서 일정 간격으로 적 1종 생성 |
| `FallEnergySystem.cs` | 실제 물리 위치의 하강 거리만 0~100 에너지로 누적 |
| `ReboundAttack.cs` | 에너지 소모, 전방 사각 범위 피해, 짧은 충격파 표시 |
| `GameManager.cs` | 생존 시간·처치 수·게임 종료·씬 재시작·화면 비율 |
| `GameHud.cs` | HP·에너지·발동 기준선·생존 시간·조작법·Retry UI |
| `Rebound.Runtime.asmdef` | 런타임 어셈블리 분리 |

추가 생성 파일:

- `Assets/Editor/ReboundSceneBuilder.cs`: 씬·프리팹·설정·도형 이미지 생성 및 Windows 빌드 메뉴.
- `Assets/Rebound/Scenes/Game.unity`: 참조 연결을 완료한 플레이 가능 씬.
- `Assets/Rebound/Data/PrototypeSettings.asset`: Inspector에서 조정하는 초기 설정.
- `Assets/Rebound/Prefabs/Enemy.prefab`, `Projectile.prefab`: 임시 도형 프리팹.
- `Assets/Rebound/Art/PrototypeSquare.png`: 직접 생성한 흰색 정사각형. 색·크기로 플레이어·적·탄환·배경을 구성.
- `Assets/Rebound/Tests/PlayMode/PhaseOneTests.cs`, `Rebound.PlayModeTests.asmdef`: 실제 Unity 물리와 씬 재로딩을 사용하는 자동 테스트.
- 각 에셋의 `.meta`: GUID와 씬 참조 보존.
- `Packages/manifest.json`, `packages-lock.json`: 패키지 구성.
- `ProjectSettings/`: Unity가 생성한 기본 프로젝트 설정 및 2D·입력·화면·빌드 씬 설정.
- `.gitignore`: 캐시·로그·로컬 빌드·테스트 산출물 제외.
- `README.md`, `VALIDATION.md`: 실행·설계·검증 기록.

## 초기 밸런스

`Assets/Rebound/Data/PrototypeSettings.asset`을 선택해 Inspector에서 조절합니다.

| 항목 | 값 |
|---|---:|
| 중력 | 6 units/s² |
| 상승 초기 속도 | 4.8 units/s |
| 좌우 이동 | 5 units/s |
| 최대 낙하 속도 | 6 units/s |
| 상승 입력 간격 | 0.25초 |
| 이동 영역 | 화면 왼쪽 40% (몸체 여백 제외) |
| HP / 무적 | 3 / 1.2초 |
| 기본 발사 간격 / 피해 | 0.22초 / 1 |
| 투사체 속도 / 수명 / 풀 크기 | 15 / 2초 / 32 |
| 적 생성 간격 / HP / 이동 속도 | 1.1초 / 3 / 2.2 |
| 하강 1유닛당 에너지 | 22 |
| 반동 발동 기준 / 피해 / 범위 | 65 / 5 / 6.5×3.2 |

상승은 에너지와 무관하게 항상 입력 간격 조건만 만족하면 가능합니다. 상승 시 속도를 4.8로 설정하는 방식이므로 하강 중에도 같은 상승 감각을 제공합니다. 반동이 발동하면 저장 에너지를 전부 소비합니다. 입력 간격 안에서 누른 Space는 무시합니다. 추락은 접촉 무적 여부와 무관하게 HP를 1 줄이고, 에너지를 초기화하며 안전 위치로 복귀합니다. 순간이동 거리는 에너지에 포함되지 않습니다. 게임 종료 시 모든 공격·적 이동·플레이어 이동과 생존 시간이 정지합니다.

## 범위와 후속 작업

Phase 1만 구현했습니다. 현재 도형 및 IMGUI 기반 영문 HUD이며 음향, 애니메이션, 한국어 폰트, 키 재설정, 일시정지 메뉴는 없습니다. 적은 1종이고 기본 투사체만 풀링합니다. 밸런스는 초기값이며 플레이 체감 검증이 필요합니다. 검증 범위는 `VALIDATION.md`를 확인하세요.

Phase 2 후보: 경험치 드롭·획득, 레벨업 3지선다, MOD JSON 스키마·검증·적용, 무기 변형, 적 패턴과 난이도 곡선. 아직 구현하지 않았습니다.
