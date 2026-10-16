# RE:BOUND 통합 검증 기록

## 증강 이미지·문구 및 제작 영상 — 2026-10-16

Unity 2022.3.62f3 PlayMode **21/21 통과**, 실패 0. 이번 실행에는 실제 MOD ZIP을 설치한 후 증강 카드의 사용자 아이콘·한글 이름·설명·계산된 효과 값을 확인하고 선택 시 능력치 증가까지 검사하는 회귀 테스트를 추가했습니다. 아이콘 원색/종횡비 유지와 긴 설명의 스크롤 영역 크기도 검증했습니다. 결과 원본: `Docs/TestResults/workshop-video-playmode.xml`.

Windows 빌드 성공: `Logs/workshop-video-final-build.log`. 이전 EditMode 11/11 결과는 아래 v1.1 기록이며 이번 UI 수정에서 다시 실행한 결과로 표시하지 않습니다. 제작부터 설치·실제 플레이까지는 별도의 Windows 녹화로 추가 확인하며 결과와 영상 안내는 `Docs/WORKSHOP_VIDEO.md`에 기록합니다.

실제 Windows 새 녹화 완료: **18,165프레임 / 30fps / 605.5초**. 제작 씬에서 PNG 10개를 가져오고 기존/새 증강 문구·효과를 수정한 뒤 ZIP 내보내기, 설치, 활성화, 새 게임, 자연 레벨업과 재시도를 수행했습니다. 종료 시 초안 콘텐츠가 아닌 설치된 MOD를 사용했고 새 증강이 선택된 상태였습니다. `workshop-video-complete.json`, `workshop-video-evidence.txt`를 참고하세요. 마지막 패키징 빌드는 자막 챕터 전환 기록의 순서도 수정한 `Logs/workshop-video-package-build.log`입니다. UI/전투 코드는 촬영본과 동일합니다.

## v1.1 Workshop — 2026-10-16

Unity 2022.3.62f3 / Windows x64. EditMode **11/11**, PlayMode **20/20** 통과. 최신 빌드와 Windows 통합 검증 결과는 `Docs/TestResults/workshop-automated-summary.json`, `workshop-windows.txt`, `workshop-base-windows.txt`에 보존합니다.

추가 검사: 치명타/추진/낙하 속도 상한과 최대 단계에서 후보 제외, 이전 선택창 재사용 방지, 모든 증강 소진 시 실제 점수 지급, 복합 효과와 내부 보상 구분, 초안 저장/재열기, ZIP 내보내기/설치/재편집, PNG 가져오기, 불량 편집의 트랜잭션 거부, 궤적별 실제 이동, 제작 씬→게임→제작 씬 전환.

Windows 자동 검사는 실제 빌드의 UI 버튼 콜백으로 파동 탄환 및 프리즘 복합 증강을 작성하고, PNG와 폭발 색을 수정한 뒤 ZIP 설치·초안 플레이·실제 전투 이펙트를 확인합니다. 사용자 초안/설치 폴더와 분리된 로그 폴더에서 실행합니다. 사람이 모든 화면을 클릭한 검사는 아니며, 캡처는 카메라/Canvas를 직접 렌더링해 검토합니다.

재실행:

```powershell
& '.\Builds\FullWindows\REBOUND.exe' --workshop-smoke --smoke-output 'C:\Temp\ReboundWorkshopCheck'
```

출력 폴더는 매번 새 폴더를 사용하세요. 일반 실행에서는 검증 코드가 작동하지 않습니다. 테스트 플레이는 최고 기록을 갱신하지 않습니다. 현 버전의 수동 확인 절차는 `Docs/WORKSHOP_GUIDE.md`에 있습니다. 장시간 밸런스, 다양한 PC/해상도에서 실제 마우스·키보드 조작, 한국어 이외 사용자 폰트 검증은 남아 있습니다.

## v1.0 이전 검증 기록

검증일: 2026-10-14 (한국 시간). Unity 2022.3.62f3, Windows x64.

## 결과

- Unity 전체 게임 컴파일 및 씬/데이터/샘플 패키지 생성: 성공.
- Edit Mode: **5개 통과 / 0개 실패**.
- Play Mode: **17개 통과 / 0개 실패**. 전체 게임 11개와 기존 Phase 1 회귀 테스트 6개.
- Windows x64 Standalone 빌드: 성공.
- 실제 Windows 실행 파일의 스크립트 기반 통합 실행: 성공. 샘플 ZIP 2개 설치·적용, 기본 콘텐츠 복구, 보스 3단계, 승리, 재시작, 게임오버 전환 확인.
- 자동 테스트 요약: `Docs/TestResults/automated-summary.json`.

실행 파일 검증은 Unity Editor 내부 실행이 아니라 생성된 `Builds/FullWindows/REBOUND.exe`를 NVIDIA RTX 4060 / Direct3D 11 환경에서 실행했습니다. 사람의 키보드 입력을 자동으로 가장한 것이 아니라 게임 입력 API를 호출하는 명시적 검증 모드입니다.

## 검증 범위

| 영역 | 검증 내용 |
|---|---|
| 이동 | 상승·중력·최대 낙하 속도·경계·추락·복귀·무적 |
| 에너지 | 실제 하강 충전·기준량·반동 소모·전방 범위·뒤쪽 제외 |
| 전투 | 자동 사격·연속 선분 충돌·적 처치·차폐 피해 감소·추적·적 발사 |
| 풀 | 기존 투사체 반환·중복 반환 방지, 전체 게임 384발 상한·반환 |
| 성장 | 연속 레벨업 큐·3개 고유 후보·최대 단계 제외·대체 보상·재계산 안정성 |
| 스테이지 | 각 시간 구간 웨이브·보스 등장·5분 경과 후 강제 종료 없음 |
| 보스 | HP 60%·25% 단계 전환·중력 교란·사망·클리어 |
| 상태 | 일시정지·설정에서 복귀·재시작·런 초기화 |
| 저장 | 잘못된 JSON 복구·정상 저장 재로드 |
| MOD | 실제 ZIP 설치·샘플 수치 적용·해제·기본 복원·결정적 충돌 거부 |
| 보안 | 경로 이탈·DLL·심볼릭 링크·중복 경로/ID·버전·NaN·잘못된 패턴·용량/파일 수 제한 |
| 수명 | 반복 재시작 뒤 AudioSource가 누적되지 않는지 확인 |

개발 중 발견한 씬 생성기의 ZIP Create 모드 처리, 자동 검증의 레벨업 대기, 오디오 재초기화 시 컴포넌트 정리를 수정했습니다. 추가 웨이브 테스트의 첫 샘플 시점이 생성 쿨다운 안에 있던 테스트 조건도 수정했습니다. 최종 결과는 위 통과 수치를 기준으로 합니다.

## 실행 화면 및 제한

숨겨진 Windows 테스트 창은 화면 갱신을 건너뛸 수 있어 스크린 캡처가 검게 나오는 현상이 있었습니다. 시각 검증용 캡처는 동일한 실행 파일의 카메라와 Canvas를 RenderTexture로 직접 렌더링하는 방식으로 수행합니다.

검증 모드는 스테이지 시간을 앞당기고 보스 HP를 조절하여 전환을 확인합니다. **사람이 실제로 5분 생존하고 보스를 격파한 플레이 테스트는 수행하지 않았습니다.** 실제 키보드 포커스, 모든 메뉴의 마우스 클릭, 소리의 청감 품질, 여러 PC의 성능과 밸런스는 수동 검증이 남아 있습니다. 기본 UI/스토리는 영문입니다.

서버 API 정보가 없어 원격 MOD 목록·다운로드는 미연결입니다. 로컬 설치는 작동하며 서버 연결 요구사항은 `Docs/SERVER_INTEGRATION.md`에 기록했습니다.

## 재실행

Unity: **Window → General → Test Runner**에서 EditMode 및 PlayMode를 각각 실행합니다. Unity 메뉴 **REBOUND → Build Full Windows Game**으로 다시 빌드합니다.

빌드 검증 전용 실행 예시:

```powershell
& '.\Builds\FullWindows\REBOUND.exe' --rebound-smoke --smoke-output 'C:\Temp\ReboundSmoke'
```

이 옵션을 붙이지 않으면 정상 게임으로 실행합니다. 검증 모드는 지정한 출력 폴더에 별도 설정·MOD·결과·이미지를 저장하며 실제 플레이 설정과 기록을 사용하지 않습니다. 검증 완료 시 자동 종료합니다.

로컬 원본 로그와 XML:

- `FullGenerate.log`, `FullBuildFinal.log`
- `FullEditTests.log`, `FullTestsFinal.log`
- `TestResults/full-editmode.xml`, `TestResults/full-playmode-final.xml`
- `Builds/SmokeRelease/player.log`, `results.txt`

## 수동 인수 테스트

1. 메뉴 → 시작 → 스토리 → LAUNCH. A/D·Space·Esc를 확인합니다.
2. 하강 충전 65 이상에서 상승하고 반동 이펙트·소리·피해를 확인합니다.
3. 레벨업 카드에 마우스 또는 1/2/3으로 선택하고 다시 움직이는지 확인합니다.
4. 설정에서 키·음량·음소거·화면 모드·해상도·흔들림을 바꾸고 재실행 후 유지되는지 확인합니다.
5. 일반 적 4종과 보스전까지 플레이하며 피해량·난이도·5분 목표 시간을 평가합니다.
6. 두 샘플 MOD를 하나씩 켜서 비행과 반동 차이를 느껴 보고, 둘 다 켜면 충돌 오류가 표시되는지 확인합니다.
7. HP 0 결과와 보스 처치 결과에서 Retry 및 Main Menu를 각각 확인합니다.
8. 1280×720, 1920×1080 및 비표준 창 크기에서 텍스트와 버튼 가독성을 확인합니다.
