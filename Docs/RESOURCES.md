# 리소스와 라이선스 기록

## 직접 제작한 리소스

- `ArtFactory.cs`: 수학적 도형으로 생성하는 기체·드론·보스·탄환·에너지 링·리액터 배경. 제3자 이미지 사용 없음.
- `VisualEffects`: 스프라이트 기반 짧은 입자·충격파. 화면의 동시 입자 수를 제한합니다.
- `AudioDirector.cs`: 사인파·노이즈·엔벌로프 조합으로 만든 효과음 13종과 메뉴·전투·보스·클리어 음악 루프 4개. 음악은 16초 루프이며 타인의 녹음·멜로디·음원 파일을 사용하지 않았습니다.
- `SampleMods/`: 동일한 절차적 제작기를 사용해 생성한 PNG 및 직접 작성한 JSON. 샘플 manifest의 `CC0-1.0`은 이 샘플 콘텐츠에 적용합니다.
- 스토리·게임 이름 표기·UI 구성: 이 프로젝트를 위해 작성. 기존 상용 게임의 구체적 UI나 캐릭터 표현을 복제하지 않았습니다.

## 네온 웨이브 영상용 MOD

`DemoAssets/NeonWave/`의 배경·플레이어·적 4종·증강 아이콘 2종은 OpenAI 내장 image_gen으로 이번 프로젝트용으로 생성했습니다. 원본 파일과 전체 프롬프트는 해당 폴더에 보관합니다. 탄환·처치 이펙트 PNG는 Pillow 다각형으로 직접 제작했습니다. 이 MOD의 출처 표기는 `AI-assisted demo resources`이며 기존 절차적 샘플의 CC0 표기를 그대로 적용하지 않습니다.

## 사용 도구 / 패키지

- Unity Editor / Engine 2022.3.62f3 및 기본 모듈: Unity 제공 라이선스 적용.
- `com.unity.ugui@1.0.0`: Unity UI, 설치 패키지의 라이선스 및 Notices 적용.
- `com.unity.test-framework@1.1.33`, `com.unity.ext.nunit@1.0.6`: 개발 테스트 전용. 각 패키지의 LICENSE/Notices 적용.
- UI 글꼴: Windows의 `Malgun Gothic`, 없으면 Arial / Unity 내장 `LegacyRuntime.ttf`로 대체합니다. 한글 자막도 설치된 맑은 고딕을 사용합니다. 폰트 파일 자체를 복사·배포하지 않았습니다.

프로젝트 전체에 임의의 오픈소스 라이선스를 부여하지 않았습니다. GitHub 저장소는 비공개입니다. 공개 배포 전 프로젝트 소유자가 코드 라이선스와 Unity 사용 조건을 확인해야 합니다.
