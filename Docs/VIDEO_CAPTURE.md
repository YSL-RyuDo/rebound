# 기본 플레이와 MOD 적용 영상

`GameplayRecorder`는 `--rebound-video` 옵션을 명시한 Windows 실행에서만 동작하는 녹화 도구입니다. 일반 실행에는 영향을 주지 않습니다.

- 1280×720, 30fps, 전체 172초.
- 기본 게임 플레이, LIGHT FRAME 설치·활성화·플레이, OVERCHARGE 활성화·플레이를 순서대로 기록합니다.
- Unity 카메라와 Canvas를 프레임별로 렌더링해 FFmpeg의 H.264 인코더로 전달합니다.
- `AudioRenderer`로 같은 프레임의 실제 게임 오디오를 스테레오 float PCM으로 저장한 뒤 AAC로 합칩니다.
- 자동 조작은 일반 `InputAxis`/`Rise` 및 UI Button의 onClick을 사용합니다. HP·XP·피해·적·스테이지 시간을 변경하지 않습니다.
- 레벨업 선택 화면도 그대로 기록하고 자동으로 후보를 선택합니다. 실패하면 결과 화면을 보여준 뒤 Retry합니다.
- 별도 설정/MOD 디렉터리를 사용하므로 사용자의 플레이 기록을 덮어쓰지 않습니다.

구간:

| 시간 | 내용 |
|---|---|
| 00:00–00:07 | 메뉴·스토리 |
| 00:07–01:00 | 기본 플레이 |
| 01:02–01:12 | LIGHT FRAME 설치·활성화 |
| 01:12–01:55 | LIGHT FRAME 플레이 |
| 01:57–02:07 | LIGHT FRAME 해제·OVERCHARGE 활성화 |
| 02:07–02:50 | OVERCHARGE 플레이 |

명령줄에는 `--video-output` 출력 폴더와 `--ffmpeg` 로컬 인코더 경로도 지정합니다. 기본 출력은 `video.mp4`, `audio.f32`, 진행·완료 기록입니다. 캡처 파일은 `Builds/` 아래에 보관하고 소스 Git에 넣지 않습니다. 최종 영상은 GitHub Release 첨부 파일로 제공합니다.
