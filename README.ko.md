# RE:BOUND

[English](README.md), [Korean](README.ko.md), [Japanese](README.ja.md)

하강하며 충전하고 상승하며 공격하는 중력 슈팅 게임과 궤적 제작 워크숍입니다.

| 프로젝트 정보 | 내용 |
|---|---|
| 개발 상태 | 플레이 가능한 프로토타입 |
| 사용 기술 | C#, Unity 2D, Local content |

[개발 사례 보기](https://ysl-ryudo.github.io/ko/work/rebound/), [Portfolio](https://ysl-ryudo.github.io/ko/)

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

| 영역 | 구현 |
|---|---|
| 제작 | 워크숍 매개변수와 궤적 수식 |
| 평가 | 워크숍과 게임이 공유하는 궤적 평가기 |
| 로딩, 플레이 | 로컬 ZIP 검증, 적, 웨이브, 강화 요소 |

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
