# RE:BOUND

[English](README.md), [Korean](README.ko.md), [Japanese](README.ja.md)

하강하며 충전하고 상승하며 공격하는 중력 슈팅 게임과 궤적 제작 워크숍입니다.

| 프로젝트 정보 | 내용 |
|---|---|
| 개발 상태 | 플레이 가능한 프로토타입 |
| 사용 기술 | C#, Unity 2D, Local content |

[개발 사례 보기](https://ysl-ryudo.github.io/ko/work/rebound/), [Portfolio](https://ysl-ryudo.github.io/ko/)

## 게임 전체에서 맡은 구현

궤적 수식 도구뿐 아니라 비행과 전투, 성장, 런 상태와 저장, 콘텐츠 워크숍까지 게임 흐름을 개발했습니다. 수식 파서는 게임의 구성 요소 하나이며 프로젝트 전체가 아닙니다.

**플레이 흐름**

런 시작 → 비행과 반동 → 웨이브와 보스 전투 → 성장 선택 → 클리어와 실패 → 저장과 콘텐츠 제작

소스 링크는 확인한 버전에 고정하고 기여 링크는 제가 작성한 병합 외 변경을 가리킵니다. 공동 파일을 연결했다고 단독 작성이라는 뜻은 아닙니다. 비공개 소스는 저장소 접근 권한이 필요합니다.

### 비행과 반동 전투

플레이어 비행과 전투 월드의 반동 상호작용을 구현했습니다. 반동 공격은 적과 적 투사체에 작용하고 연쇄 공격에서는 이미 맞은 대상을 추적합니다. CombatWorld에서 적과 투사체, 보스 단계 변화를 관리합니다.

[PlayerFlight.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/PlayerFlight.cs), [내 변경 1ffa420](https://github.com/YSL-RyuDo/rebound/commit/1ffa420944e866e69b4422903bf1095ef0855494), [CombatWorld.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/CombatWorld.cs), [내 변경 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

### 성장 선택과 능력치 재계산

경험치 기준과 대기 중인 레벨업, 가중치 기반 성장 선택지를 구현했습니다. 최대 단계이거나 실제 변화가 없는 선택지는 제외하고 유효한 강화가 없을 때의 대체 보상을 둡니다. 능력치는 UI에서 반복 가산하지 않고 기본값과 강화 단계로 다시 계산합니다.

[Progression.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/Progression.cs), [내 변경 1ffa420](https://github.com/YSL-RyuDo/rebound/commit/1ffa420944e866e69b4422903bf1095ef0855494)

### 런 상태와 저장 경계

메뉴와 이야기, 플레이, 일시정지와 레벨업, 결과와 워크숍 상태를 구현했습니다. 일시정지 관련 상태에서는 게임 시간을 제어합니다. 테스트 모드의 런은 최고 점수와 클리어 기록을 저장하지 않아 실험과 일반 진행을 구분합니다.

[GameSession.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/GameSession.cs), [내 변경 79d77f2](https://github.com/YSL-RyuDo/rebound/commit/79d77f2e3996f7e5249117a453d76027073f8af1), [SaveManager.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/SaveManager.cs), [내 변경 cfc789a](https://github.com/YSL-RyuDo/rebound/commit/cfc789aea5dbd9e2e34847cfed4c3dbb2bb0ff53)

### 콘텐츠와 궤적 제작

데이터 기반 게임과 함께 워크숍의 프로젝트 편집, 미리보기와 가져오기 및 내보내기를 구현했습니다. 궤적 언어로 움직임을 제한된 수치식 형식 안에서 작성하도록 했으며, 고정 패턴이나 임의 스크립트와의 차이는 아래에서 설명합니다.

[ModWorkshop.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/ModWorkshop.cs), [내 변경 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721), [WorkshopProject.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/WorkshopProject.cs), [내 변경 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

## 내 기여

낙하 충전, 상승 공격, 전투 웨이브, 성장 선택, 보스 진행과 설정 저장을 구현했습니다.

로컬 ZIP MOD 검증과 탄환 궤적, 적, 웨이브, 증강 제작 도구를 구현했습니다.

수치, 수식 탄환 제작과 게임, 제작 도구가 공유하는 궤적 계산을 추가했습니다.

## 기여 근거

[v1.2 source baseline](https://github.com/YSL-RyuDo/rebound/tree/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

커밋은 설명한 변경의 근거이며 기여율 점수가 아닙니다. 외부 에셋과 팀원 작업의 크레딧은 구분합니다.

## 구현과 설계

중력 슈팅 게임과 콘텐츠 제작 도구가 작성된 궤적을 같은 방식으로 해석해야 합니다. 로컬 콘텐츠는 게임에 반영되기 전에 검증해야 합니다.

하강 충전, 상승 공격 루프와 궤적, 적, 웨이브, 증강 제작 도구를 구현했습니다. 매개변수, 수식으로 작성한 궤적은 게임과 제작 도구에서 평가기를 공유하며 로컬 ZIP을 검증합니다.

## 코드의 동작과 구현 방식을 택한 이유

확인한 구현이 어떤 제약을 다루며 어떤 장단점이 생기는지 설명합니다. 설계 효과에 대한 설명은 코드 분석이며, 비교한 모든 대안을 당시 직접 구현했다는 뜻은 아닙니다. 기여 링크는 공동 프로젝트 안의 담당 변경을 가리키고 소스 링크는 기존 접근 권한을 유지합니다.

### 사용자 궤적에 범위가 제한된 수식 언어를 쓰는 이유

**구현 흐름**

TrajectoryFormula는 정해진 변수, 연산자, 함수로 표현한 수식을 구문 트리로 분석합니다. 수식 길이, 노드 수, 깊이에 제한을 두고 분석 결과를 캐시하며, 유한하지 않거나 허용 범위를 넘는 좌표를 거부합니다. 유효성 검사는 수명 구간을 표본 검사하고 실제 실행에서도 반환값을 검사합니다.

**이 구조가 필요한 이유**

궤적에 필요한 것은 좌표를 계산하는 기능이며 임의의 C# 코드를 실행하는 기능까지는 아닙니다. 작은 언어를 사용하면 지원하는 동작을 명시하고 분석 및 계산 구조에 제한을 둘 수 있습니다. 캐시는 매 갱신마다 같은 트리를 다시 만드는 일을 줄이고, 실행 중 검사는 유한한 표본 검사에서 놓칠 수 있는 값을 다룹니다.

**대안과 현재 구현의 절충**

정해진 궤적 목록만 제공하면 검증은 쉽지만 표현이 제한됩니다. 임의 스크립트는 자유도가 높은 만큼 실행과 검증 범위도 크게 늘어납니다. 현재 구문 트리는 그 사이에서 작은 언어 안의 수식을 조합해 궤적을 만드는 방식입니다.

**장단점과 검증 범위**

범용 확장성을 줄이는 대신 구현과 검증 범위를 좁히는 선택입니다. 함수를 추가하려면 파서와 계산기를 함께 바꿔야 합니다. 표본 검사는 모든 시간에 대한 연속성이나 정확성을 증명하지 않으며, 사용하는 수식이 크게 늘어나면 캐시 정책도 다시 검토해야 합니다.

[rebound/TrajectoryFormula.cs](https://github.com/YSL-RyuDo/rebound/blob/52994738ab20e2c209c5d359b96527cc268417f2/Assets/Rebound/Full/TrajectoryFormula.cs), [rebound/TrajectoryFormulaTests.cs](https://github.com/YSL-RyuDo/rebound/blob/52994738ab20e2c209c5d359b96527cc268417f2/Assets/Rebound/Tests/EditMode/TrajectoryFormulaTests.cs)

### 성장 선택지가 실제 변화를 만들어야 하는 이유

**구현 흐름**

Progression은 레벨과 최대 단계, 능력치 변화 미리보기로 후보를 거릅니다. 가중치로 세 개까지 고르고 남는 자리는 점수 보상으로 채웁니다. 세대 번호로 이전 선택지의 입력을 거부할 수 있고, 대기 중인 레벨업을 하나씩 처리합니다.

**이 구조가 필요한 이유**

강화 단계가 남아 있어도 최종 능력치 상한에 걸리면 효과가 없을 수 있습니다. 다시 계산한 능력치를 비교하면 실제 변화를 확인할 수 있습니다. 여러 레벨업과 늦은 UI 입력이 겹칠 때도 클릭이 어느 선택지 묶음에 속하는지 구분해야 합니다.

**대안과 현재 구현의 절충**

단계만 확인하면 계산이 적지만 능력치 상한을 놓칩니다. 효과를 누적 적용하면 재계산을 줄이는 대신 적용 순서와 중복 가산 문제가 생길 수 있습니다. 기본 데이터와 단계로 다시 계산하면 추적하기 쉽지만 미리보기마다 계산이 추가됩니다.

**장단점과 검증 범위**

세대 검사는 호출 측에서 세대 번호를 전달했을 때 적용됩니다. 모든 강화가 상한인 경우, 여러 레벨업 대기, 오래된 클릭과 선택 직전의 무효화를 검사해야 합니다. 점수 대체 보상이 있다는 사실이 강화 확률의 밸런스를 입증하지는 않습니다.

[rebound/Progression.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/Progression.cs)

### 워크숍 시험을 실제 진행 기록과 구분한 이유

**구현 흐름**

GameSession은 제작한 콘텐츠를 실행할 때 테스트 모드를 사용합니다. FinishRun은 이 모드에서 최고 점수와 클리어 기록 갱신을 건너뛰고 일반 런에서만 저장합니다. 워크숍 진입 전에는 진행 중인 런을 정리합니다.

**이 구조가 필요한 이유**

제작 중에는 일부러 쉬운 설정이나 극단적인 수치, 미완성 콘텐츠를 실행할 수 있습니다. 그 결과를 일반 성과로 저장하면 편집 실험과 플레이 진행이 섞입니다.

**대안과 현재 구현의 절충**

실행 파일을 따로 만들면 더 넓게 분리되지만 반복 제작이 느려집니다. 테스트 저장을 별도로 두면 유연하지만 수명 관리가 필요합니다. 현재는 같은 런타임에서 모드 검사로 진행 기록의 쓰기를 제외하므로 가벼운 대신 각 저장 경로를 확인해야 합니다.

**장단점과 검증 범위**

이는 게임 진행 데이터의 경계이며 운영체제 수준의 샌드박스가 아닙니다. 테스트 런의 성공과 실패 전후, 일반 플레이로 돌아간 뒤의 저장 내용을 비교하는 검증이 필요합니다.

[rebound/GameSession.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/GameSession.cs), [rebound/SaveManager.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/SaveManager.cs)

## 실행, 코드 확인

[ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)의 Unity 버전으로 프로젝트를 여세요. 패키지 잠금 파일과 직렬화된 에셋을 유지하고, Unity를 열기 전에 문서에서 설명한 브랜치를 선택하세요.

Unity scene: [Assets/Rebound/Scenes/REBOUND.unity](Assets/Rebound/Scenes/REBOUND.unity)

## 개발, 검증 범위

2026 ~ 10 ~ 19 검증 기록에는 EditMode 22개, PlayMode 23개 통과와 Windows 빌드가 기록돼 있습니다. 이 README 작업에서 재실행한 검사가 아닙니다. 원격 MOD 서비스는 미연결이며 장시간 수동 밸런스, 접근성, 극단적인 궤적의 연속 충돌은 검증 범위 밖입니다.

## 상세 문서

[기존 개발, 사용, 검증 상세 안내](docs/DEVELOPMENT_REFERENCE.ko.md)

[VALIDATION.md](VALIDATION.md)

[Docs/WORKSHOP_GUIDE.md](Docs/WORKSHOP_GUIDE.md)

[Docs/TRAJECTORY_VERSIONS.md](Docs/TRAJECTORY_VERSIONS.md)

[Docs/RESOURCES.md](Docs/RESOURCES.md)

## 관련 저장소

[rebound](https://github.com/YSL-RyuDo/rebound), 비공개, 접근 권한 필요
