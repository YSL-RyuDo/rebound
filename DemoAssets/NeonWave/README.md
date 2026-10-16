# 네온 웨이브 제작 리소스

MOD Workshop 영상에서 실제로 가져온 원본 PNG입니다. 기본 게임 리소스를 덮어쓰지 않으며 Workshop에서 사용할 파일들입니다.

| 파일 | 가져올 항목 |
|---|---|
| player.png | FLIGHT / re01 |
| background.png | STAGES / powerplant |
| enemy.png | ENEMIES / scout |
| hunter.png | ENEMIES / hunter |
| shield.png | ENEMIES / shield |
| gunner.png | ENEMIES / gunner |
| bullet.png | WEAPONS / pulse |
| effect.png | EFFECTS / explosion |
| haste.png | UPGRADES / haste → 냉각 회로 |
| augment.png | UPGRADES / 새 ID neon_core → 네온 과충전 |

배경·기체·적 4종·증강 아이콘 2종은 이번 프로젝트용으로 **OpenAI 내장 image_gen**에서 생성했습니다. 전체 사용 프롬프트는 [prompts.json](prompts.json)에 있습니다. 참고 이미지나 기존 상용 게임 리소스는 사용하지 않았습니다. 생성 결과를 수정·리사이즈하지 않고 복사했습니다. 배경은 RGB, 나머지 생성 스프라이트는 실제 투명 알파 PNG입니다.

bullet.png와 effect.png는 Python/Pillow의 다각형으로 직접 만든 소형 픽셀 리소스입니다. 생성 이미지에 독점성이나 제3자 권리 보증을 부여하지 않습니다. 패키지에는 출처를 `AI-assisted demo resources`로 기록했습니다.

캐릭터와 몬스터의 충돌 크기는 이미지의 픽셀 크기와 별개입니다. PNG 교체는 정적 이미지 교체이며 애니메이션 편집 기능은 아닙니다.
