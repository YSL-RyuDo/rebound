# RE:BOUND MOD schema 1

게임 버전 `1.0.0`, ZIP 루트에 `manifest.json`이 있어야 합니다. 감싸는 최상위 폴더를 추가하지 마세요.

```text
manifest.json
data/content.json
assets/sprites/custom.png
assets/audio/custom.wav
```

```json
{
  "schemaVersion": 1,
  "modId": "my-flight",
  "name": "My Flight",
  "author": "Your name",
  "version": "1.0.0",
  "targetGameVersion": "1.0.0",
  "description": "A lighter recovery drone",
  "license": "Your actual resource license",
  "content": ["data/content.json"]
}
```

콘텐츠 JSON은 `ContentPack` 형식입니다. `flight`, `weapons`, `enemies`, `upgrades`, `stages`, `assets`, `story` 배열을 사용합니다. 전체 필드와 기본값은 `Assets/Rebound/Full/ContentData.cs`, 작동하는 예시는 `SampleMods/`를 참고하세요. JSON 객체는 Unity JsonUtility가 읽으며 알 수 없는 필드는 무시되므로 철자를 정확히 맞춰야 합니다.

## 추가와 교체

- 모든 ID는 영문 소문자로 시작하고 소문자·숫자·밑줄·하이픈만 사용합니다. 최대 64자입니다.
- `replace: false` 또는 생략: 해당 종류에 존재하지 않는 ID를 **추가**합니다.
- `replace: true`: 해당 종류에 이미 존재하는 ID를 **전체 교체**합니다. 부분 패치가 아니므로 완전한 레코드를 작성하세요.
- 설치는 자동 활성화하지 않습니다. 사용자가 MOD Manager에서 활성화합니다.
- 적용 순서는 `modId`의 ordinal 오름차순으로 고정합니다.
- 동일한 종류·ID를 활성 MOD 둘이 변경하면 뒤에 오는 패키지 전체를 거부합니다. 마지막 적용으로 조용히 덮어쓰지 않습니다.
- 패키지 안의 중복 ID·잘못된 교체 플래그·없는 무기/적 참조도 거부합니다.
- 패키지 적용은 트랜잭션 방식입니다. 오류 발생 시 해당 패키지를 제외한 이전 콘텐츠가 유지됩니다.
- 비활성화 후 새 게임을 시작하면 기본 데이터·스프라이트·오디오에서 다시 로드합니다.

기본 기체 ID는 `re01`, 무기는 `pulse`, 스테이지는 `powerplant`입니다. 기체·스테이지를 새 ID로 추가하면 메인 메뉴의 FRAME/STAGE 버튼으로 선택할 수 있습니다. 무기는 기체의 `weapon`, 적은 웨이브의 `enemies`로 연결합니다. 신규 업그레이드는 조건을 충족하면 후보 풀에 들어갑니다.

## 지원 콘텐츠

| 종류 | 주요 필드 |
|---|---|
| flight | gravity, thrust, speed, maxFall, interval, charge, rebound, range, hp, skin, weapon |
| weapons | damage, interval, speed, size, crit, count, pierce, spread, pattern, sprite |
| enemies | hp, speed, interval, armor, xp, damage, pattern, sprite |
| upgrades | effect, amount, maxLevel, minLevel, weight, icon |
| stages | bossTime, bossHp, background, waves |
| assets | kind (`sprite`/`audio`), path |

무기 패턴은 `line`, `fan`. 적 패턴은 `straight`, `track`, `shield`, `shoot`. 업그레이드 효과는 `damage`, `haste`, `multi`, `pierce`, `crit`, `range`, `rebound`, `chain`, `charge`, `thrust`, `fall`, `guard`, `repair`를 지원합니다. 새로운 코드를 실행하는 패턴 추가는 지원하지 않습니다.

웨이브는 `start`, `end`, `interval`, `difficulty`, `count`, `minY`, `maxY`, `enemies`를 지정합니다. 활성 시간 구간에서 간격마다 반복합니다. 동시에 겹치는 웨이브는 배열에서 먼저 일치한 항목을 사용하므로 구간을 겹치지 않게 작성하세요.

`story`는 기본 게임의 대사 데이터이며 schema 1 MOD에서의 대사 교체는 지원하지 않습니다. 보스 AI는 게임 코드에 정의되어 있고 MOD 스테이지에서 보스 등장 시간과 HP를 조절할 수 있습니다.

## 그래픽 및 오디오

```json
{
  "assets": [
    {"id":"my_skin","name":"My skin","kind":"sprite","path":"assets/sprites/custom.png"},
    {"id":"shot","name":"My shot sound","kind":"audio","path":"assets/audio/custom.wav"}
  ]
}
```

스킨·투사체·적·아이콘·배경 데이터에 sprite ID를 지정합니다. 기본 절차적 이미지 ID는 `player`, `drone`, `hunter`, `shield`, `gunner`, `boss`, `bullet`, `ring`, `spark`, `reactor`, `upgrade`입니다. 기본 이미지·오디오 ID는 데이터 레지스트리에 AssetData로 등록되어 있지 않으므로 처음 교체할 때 `replace:false`로 에셋 정의를 추가합니다.

오디오 ID는 `shot`, `hit`, `explode`, `hurt`, `rise`, `charge`, `rebound`, `level`, `select`, `button`, `win`, `lose`, `warning`; 음악은 `menu`, `battle`, `boss`, `clear`입니다. 같은 ID의 오디오 에셋을 제공하면 해당 소리가 바뀝니다.

- PNG: 최대 2048×2048, 투명 배경 권장. 종횡비를 유지하는 스프라이트로 로드합니다.
- WAV: PCM16, mono/stereo, 8–48kHz, 최대 60초. 음악은 루프 가능하게 제작하세요.
- 사용·수정·배포 권한이 있는 리소스만 사용하고 manifest에 실제 라이선스를 기록하세요.

## 제한 및 오류

- ZIP 최대 32MiB, 압축 해제 총 32MiB, 개별 파일 8MiB, 최대 256개 ZIP 엔트리.
- 확장자 `.json`, `.png`, `.wav`만 허용. DLL·C#·실행 파일은 허용하지 않습니다.
- 절대경로·상위 경로·역슬래시·중복 경로·심볼릭 링크 거부.
- JSON 수치와 패턴을 검사합니다. 상세 허용 범위는 `ModValidator.Pack`이 기준입니다.
- 이미 설치된 modId는 덮어쓰지 않습니다. 해당 MOD를 비활성화하고 폴더를 보관한 뒤 새 버전을 설치하세요.
- 오류는 MOD Manager와 MOD 폴더의 `mod-errors.log`에 기록합니다.
- 원격 서버 연결은 아직 없으며 임의 서버 주소도 사용하지 않습니다.

## 샘플 차이

LIGHT FRAME: 중력 4.2, 추진 5.2, 이동 6, 최대 낙하 5, 전용 스킨과 추가 비행 업그레이드.

OVERCHARGE: 중력 8.5, 추진 5.8, 최대 낙하 7, 충전 34, 반동 피해 24, 전기 탄환과 추가 연쇄 업그레이드.

두 샘플은 같은 기본 기체를 교체하므로 하나씩 사용하세요. ZIP 설치와 Windows 플레이어에서의 실제 수치 반영은 통합 검증 대상입니다.
