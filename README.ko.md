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
