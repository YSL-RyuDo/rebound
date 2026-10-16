# RE:BOUND 1.1 MOD Workshop

## 실행과 저장 위치

최신 실행 파일은 `Builds/FullWindows/REBOUND.exe`입니다. 메인 메뉴에서 **MOD WORKSHOP**을 누르세요.
Unity에서는 **REBOUND → Open MOD Workshop → Play**입니다. 씬은 `Assets/Rebound/Scenes/ModWorkshop.unity`이며 빌드에도 포함됩니다. 입력 설정은 기존 Input Manager (Old)를 유지합니다.

Windows 기본 폴더:

```text
%USERPROFILE%\AppData\LocalLow\Independent Prototype\REBOUND\
  Workshop\Drafts\<작업 ID>\draft.json
  Workshop\Drafts\<작업 ID>\assets\sprites\...
  Workshop\Exports\<MOD ID>-<버전>-<시간>-<고유값>.zip
  Mods\Inbox\
```

**FOLDER**는 Workshop 폴더를 엽니다. 초안 경로는 제작 화면 오른쪽에 나옵니다. 마지막 초안은 다음 실행 때 다시 엽니다. ZIP 가져오기는 Workshop 아래 별도 작업 폴더를 생성합니다. 기존 설치 MOD나 프로젝트 기본 데이터는 수정하지 않습니다.

## 제작 흐름

1. 상단 MOD ID, 이름, 제작자, 버전, 라이선스, 설명을 입력합니다. ID는 영문 소문자로 시작하는 소문자/숫자/밑줄/하이픈, 64자 이내입니다.
2. 왼쪽 카테고리와 항목을 선택합니다. 가운데 필드를 편집합니다. 긴 폼은 마우스 휠로 스크롤합니다.
3. **APPLY**로 검증하고 초안에 적용합니다. `*`는 초안에 들어간 항목입니다. 다른 항목으로 이동하기 전에 APPLY를 누르세요.
4. **CLONE / NEW**는 현재 항목을 복제하여 새 ID를 만듭니다. APPLY 전에는 ID도 편집할 수 있습니다. 기존 ID 편집은 교체, 새 ID 편집은 추가로 자동 처리합니다.
5. **REVERT / REMOVE**는 초안의 해당 항목을 제거합니다. 기본 항목이면 원래 값이 다시 보입니다. 다른 항목이 참조 중이면 제거를 거부합니다.
6. **TEST PLAY → START MISSION → LAUNCH**로 시험합니다. 이 초안만 적용하고 최고 기록을 저장하지 않습니다. Esc → Main Menu → Return to Workshop으로 돌아옵니다.
7. **EXPORT ZIP**으로 배포용 패키지를 만듭니다. 생성된 ZIP을 `Mods/Inbox`에 복사하고 게임의 MOD MANAGER에서 INSTALL INBOX ZIPs → ENABLE하면 일반 플레이에 적용됩니다.

새 기체/스테이지는 게임 메뉴의 FRAME/STAGE에서 선택합니다. 새 무기는 기체의 `weapon`에 ID를 지정합니다. 새 적은 스테이지 웨이브의 `enemies`에 쉼표로 ID를 넣습니다. 새 증강은 별도 연결 없이 자격을 만족하는 레벨업 후보에 들어갑니다.

**SAVE**는 현재 항목을 적용하고 저장합니다. **NEW DRAFT**는 별도 초안을 만듭니다. **LOAD DRAFT**는 아래 경로 입력란에 적은 초안 폴더를 엽니다. **IMPORT ZIP**은 기존 MOD를 별도 초안으로 가져옵니다.

## 이미지와 이펙트

탐색기에서 PNG 파일의 경로를 복사하여 아래 입력란에 붙여넣고 **IMPORT PNG**를 누릅니다. 파일은 초안에 복사되며 현재 항목에 자동 연결됩니다.

| 카테고리 | 바뀌는 이미지 |
|---|---|
| flight | 캐릭터 `skin` |
| weapons | 탄환 `sprite` |
| enemies | 몬스터 `sprite` |
| stages | 배경 `background` (가져올 때 stretch 자동 선택) |
| upgrades | 증강 `icon` |
| effects | 이펙트 `sprite` |

PNG는 2048×2048 이하, 개별 8 MiB 이하입니다. 투명 PNG를 지원합니다. 캐릭터·적·탄환·증강 아이콘의 사용자 이미지는 원래 색을 사용합니다. 이펙트는 지정한 color를 곱합니다. 배경은 `stretch` 전체 화면 또는 `tile` 반복 패턴을 선택합니다.

증강의 이미지와 문구를 바꾸려면 **UPGRADES → 증강 선택 → name / description 수정 → PNG 경로 입력 → IMPORT PNG → SAVE** 순서입니다. 새 증강은 먼저 CLONE / NEW로 새 ID를 지정합니다. 이름·설명·아이콘은 제작 미리보기와 실제 레벨업 카드에 표시됩니다. 카드에는 계산된 실제 효과 수치도 별도로 나오며, 긴 설명은 카드 내부를 스크롤하여 읽습니다. 설명은 자유 텍스트이므로 효과 값을 수정할 때 설명의 숫자도 함께 고쳐야 합니다. 예: haste의 amount를 0.25로 바꾸면 description도 `발사 속도 +25%`로 수정합니다.

이펙트는 이미지, HTML 색상(`#40E5FF`), 크기 0.02–4, 수명 0.05–3초, 속도 0–12, 입자 수 1–32를 조절합니다. 새 이펙트는 무기의 `impactEffect`, 적의 `deathEffect`, 기체의 `reboundEffect`로 연결합니다. 반동 시각 크기는 실제 반동 범위에 size를 곱한 값입니다. 이미지 크기 변경만으로 피해 판정 범위가 바뀌지는 않습니다.

## 탄환 궤적

`pattern`은 line/fan 발사 배열, `trajectory`는 발사된 각 탄환의 이동 방식입니다.

| 필드 | 의미/범위 |
|---|---|
| trajectory | straight 직선, wave 파동, arc 포물선, homing 유도 |
| speed | 초기 속도, 3–30 units/s |
| amplitude | 파동 진폭, 0–3 units |
| frequency | 파동 주파수, 0–8 Hz |
| gravity | 포물선 수직 가속도, -20–20 units/s²; 양수는 아래로 |
| acceleration | 진행 속도의 가속도, -10–10 units/s² |
| turnRate | 유도 회전 속도, 0–360도/초 |
| launchAngle | 발사 방향 추가 각도, -90–90도 |
| lifetime | 탄환 수명, 0.1–10초 |
| spread | fan 발사 간격, 0–25도 |

전투와 미리보기는 같은 `ProjectileMotion.Step`을 사용합니다. 실제 전투는 전방 자동 조준과 현재 적 위치를 사용하고, 미리보기는 고정 표적 (12,1)을 사용하므로 표적 상황까지 동일한 것은 아닙니다. 화면 밖/수명 종료 탄환은 풀로 돌아갑니다. 예: wave, amplitude 0.6, frequency 2, speed 17.

## 증강 등급과 무효 선택 방지

실버/골드/플래티넘/프리즘은 이 프로젝트의 네 희귀도입니다. 특정 다른 게임의 수치나 규칙을 복제하지 않았습니다.
사용 가능한 등급 사이에서 가중치 **55/30/12/3**으로 먼저 등급을 뽑고, 해당 등급에서 각 증강의 `weight`로 후보를 선택합니다. 선택한 ID는 제거하여 3개 후보가 겹치지 않게 합니다. 남은 후보에 특정 등급이 없으면 나머지 등급으로 정규화합니다. 그러므로 매 카드의 실제 확률이 항상 55%/30%/12%/3%인 것은 아닙니다.

등급은 출현 희귀도입니다. 효과 수치를 자동 배율로 바꾸지 않습니다. 제작자가 amount와 최대 단계를 지정하여 등급별 세기를 설계합니다.

- 현재 단계가 maxLevel 이상이면 제외합니다.
- 현재 상태에 한 단계를 적용하고 모든 상한 처리 후 수치를 비교합니다. 아무 값도 바뀌지 않으면 제외합니다.
- 선택 순간에도 다시 검사합니다. 이전 선택창의 클릭은 다음 선택창에 적용되지 않습니다.
- 카드에는 실제 적용 전후 값과 현재/다음 단계가 표시됩니다. 복합 효과가 길면 카드 안을 스크롤할 수 있습니다.
- 유효 후보가 부족하면 점수 +100 보상으로 채웁니다. 이미 HP/에너지가 가득 찼는데 회복 보상만 제공하는 상황을 피했습니다.

ADD EFFECT로 최대 8개의 효과를 조합합니다. 조합이 있으면 단일 effect/amount를 대신합니다. damage/haste/range/rebound/charge/thrust/fall은 기본값 비율, crit은 확률 가산(0.1=10%p), multi/pierce/chain/guard/repair는 정수 증분입니다. 최대 단계 1–10, 최소 레벨 1–50, weight 0.01–100입니다. 이름과 설명도 직접 입력합니다.

## 이번 버전의 범위

데이터로 기존 13종 효과를 조합하는 새 증강, 기존 증강 수정, 4종 탄환 궤적과 이미지/입자 이펙트를 제작합니다. 새로운 C# 효과 코드, 자유 곡선 노드 에디터, 스프라이트 시트 애니메이션, 외부 셰이더 편집은 포함하지 않습니다. WAV 오디오 MOD는 기존 JSON 방식으로 지원하며 제작 화면의 가져오기 버튼은 PNG용입니다. 원격 MOD 게시 서버도 아직 없습니다.

다음 확장 후보: 효과 트리거/조건 시스템(처치 시 발동 등), 곡선 키프레임 편집, 애니메이션 프레임, 패키지 버전 업데이트 UI, 한글 UI 전체 적용. 장시간 난이도와 등급별 세기 조정은 실제 플레이 피드백을 받아 진행합니다.
