# 2026-10-16 / v1.1 변경 기록

## 문제와 수정

기존 후보 필터는 `현재 단계 < 최대 단계`만 확인했습니다. 다른 증강이나 MOD로 치명타 100%, 추진 10, 최대 낙하 속도 하한 2 등에 먼저 도달하면 아직 단계가 남은 증강을 선택해도 최종 수치가 바뀌지 않았습니다.

이제 후보마다 다음 단계의 능력치를 새로 계산하고 상한 적용 후 비교합니다. 무효 후보를 제외하며 선택 때도 다시 검사합니다. 선택창 세대 번호로 이전 버튼 콜백의 중복 처리를 막습니다. 유효 후보 부족 시 실제 점수가 증가하는 보상을 제공합니다. 내부 보상 ID는 MOD가 사용할 수 없는 형식으로 구분하여 복합 효과의 레거시 문자열과 혼동하지 않습니다.

실버·골드·플래티넘·프리즘 희귀도, 색상, 현재→다음 단계, 실제 수치 변화, 복합 효과 스크롤을 추가했습니다. 13종 효과를 최대 8개까지 조합하는 신규 증강 데이터를 지원합니다.

## 새 파일

| 파일 | 역할 |
|---|---|
| Assets/Rebound/Full/AugmentRules.cs | 등급·효과 조합·수치 변화 비교 |
| Assets/Rebound/Full/ProjectileMotion.cs | 직선/파동/포물선/유도 공통 물리 계산 |
| Assets/Rebound/Full/WorkshopProject.cs | 초안·가져오기·검증·내보내기 서비스 |
| Assets/Rebound/Full/ModWorkshop.cs | 제작 화면과 애니메이션 미리보기 |
| Assets/Rebound/Full/WorkshopSmoke.cs | 명시적 옵션으로 실행하는 Windows UI/전투 검증 |
| Assets/Rebound/Scenes/ModWorkshop.unity | 플레이어 빌드에도 포함하는 제작 씬 |
| Assets/Rebound/Tests/EditMode/WorkshopTests.cs | 데이터·ZIP·PNG·궤적 회귀 검사 |
| Docs/WORKSHOP_GUIDE.md | 제작 방법·단위·범위·파일 위치 |

각 Unity 에셋의 `.meta`도 포함합니다.

## 수정 파일

- ContentData.cs, Resources/base-content.json: 궤적, 이펙트, 복합 증강, 희귀도와 기본값.
- Progression.cs: 후보 유효성, 등급 추첨, 선택 재검증, 선택창 세대, 실제 변화 표시.
- CombatWorld.cs: 풀 재사용 시 궤적 초기화, 공통 물리 이동, 명중 이펙트 연결.
- ArtFactory.cs, PlayerFlight.cs: 사용자 PNG 색 유지, 데이터 기반 이펙트.
- GameSession.cs: Workshop 전환, 초안 테스트 콘텐츠 분리, 배경 표시 방식.
- GameUI.cs: 제작 메뉴, 증강 카드, 한국어 MOD 이름을 표시할 수 있는 OS 폰트.
- Modding.cs: 확장 스키마의 범위·효과·정수 증분·등급 검증, v1.0 패키지 호환.
- Assets/Editor/FullGameBuilder.cs, ProjectSettings: Workshop 생성/빌드 포함, 버전 1.1.0.
- Tests/PlayMode/FullGameTests.cs: 상한·최대 단계·오래된 선택·복합 효과·씬 전환 검증.
- SampleMods 두 content.json과 StreamingAssets ZIP: 새 기본 필드를 포함하도록 다시 생성.
- README, MOD_GUIDE, RELEASE_NOTES, VALIDATION, 테스트 결과 및 화면 캡처 갱신.

기존 Phase 1 씬·코드와 사용자가 설치한 MOD는 보존합니다. Windows 배포 폴더는 기존 `Builds/FullWindows`, 배포 ZIP은 기존 `Builds/REBOUND.zip`을 갱신합니다.

## 영상 및 증강 이미지·문구 보완

- GameUI.cs: 사용자 증강 설명을 카드에 표시하고 계산된 실제 효과를 함께 유지합니다. 아이콘 PNG의 원색과 종횡비를 보존하며 텍스트 길이에 맞게 스크롤 높이를 계산합니다.
- ModWorkshop.cs: 증강 미리보기에도 아이콘 원색·이름·설명을 표시합니다. 녹화 도구가 실제 편집 필드를 찾을 수 있도록 UI 오브젝트 이름을 지정합니다.
- WorkshopRecorder.cs / GameSession.cs: 명시적 옵션에서만 실제 제작·설치·플레이와 게임 오디오를 녹화합니다.
- FullGameTests.cs: 내보낸 MOD의 아이콘·한글 문구·실제 효과가 설치 후 카드에 반영되는 회귀 검사.
- DemoAssets/NeonWave/: 원본 PNG 10개와 생성 프롬프트·출처.
- Tools/finish_workshop_video.py: 한국어 자막·챕터·오디오를 합쳐 MP4와 SRT를 생성합니다.
- Docs/WORKSHOP_VIDEO.md, Screenshots/11~13, TestResults/workshop-video-*: 실제 완성 영상 및 검증 기록.

## 확인 범위와 제한

EditMode/PlayMode 결과와 Windows 실행 검증은 `VALIDATION.md` 및 `Docs/TestResults/workshop-*.json/txt`에 기록합니다. Windows 검증은 UI 버튼 콜백을 호출하는 자동 통합 검사이며 사람이 마우스/키보드로 전체 게임을 클리어한 테스트를 뜻하지 않습니다.

새 효과 코드를 실행하는 스크립트 MOD, 자유 곡선 에디터, 스프라이트 시트 애니메이션, 서버 게시 기능은 이번 범위에 없습니다. 다음 단계는 조건부 증강 트리거·곡선 키프레임·애니메이션·패키지 업데이트 UI와 밸런스 조정입니다.
