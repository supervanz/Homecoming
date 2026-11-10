# Credits

AI로 생성한 원화·메쉬·사운드와 외부 에셋의 출처를 기록한다 (CLAUDE.md 10장).

## AI 생성 에셋

생성 도구: Unity AI Generators (`unity ai` CLI 플러그인 0.1.0-alpha.11), 2026-11-05

| 게임 내 파일 | 용도 | 생성 단계 (모델) | 포인트 | 비고 |
|---|---|---|---|---|
| `Art/Models/player_ship.glb` | 플레이어 유랑선 | 컨셉 이미지 `gemini-nano-banana-2-1` → 배경 제거 `photoroom-bg-removal` → 3D `model3d-tripo-p2` (Tripo P2, glb, 10k faces, PBR) | 8 + 3 + 186 | 임포트 후 Y 180° 회전, 1.4배 |
| `Art/Models/sat_small.glb` | 잡몹 위성, 보스 소환 드론 | 컨셉 이미지 `gemini-nano-banana-2-1` → 배경 제거 `photoroom-bg-removal` → 3D `model3d-tripo-p2` (Tripo P2, glb) | 8 + 3 + 186 | 생성 텍스처가 회색 한 가지로 나와 메쉬만 사용. 재질은 `M_EnemyPrimitive`(흰 금속)로 교체. 2배 |

- 컨셉 이미지와 생성 기록 원본은 에셋 준비 작업 폴더(`SourceAssets/`)에 보관되어 있고, 게임 빌드에는 들어가지 않는다.

### 음악 (`Audio/BGM/`)

생성 모델: `elevenlabs-music-v2-5` (연주곡, mp3 44.1kHz 192kbps). 음악 6곡 합계 230 포인트.

| 파일 | 길이 | 용도 |
|---|---|---|
| `title.mp3` | 60초 | 타이틀 |
| `stage.mp3` | 120초 | 스테이지 |
| `boss_phase1.mp3` | 120초 | 보스 1페이즈 |
| `boss_phase2.mp3` | 60초 | 보스 2페이즈 |
| `ending.mp3` | 30초 | 엔딩 (M9) |
| `gameover.mp3` | 10초 | 게임오버 |

### 효과음 (`Audio/SFX/`)

생성 모델: `elevenlabs-sound-effects-v2` (wav 44.1kHz 스테레오). 효과음 28개 합계 738 포인트.

생성 후 ffmpeg(`volume` 필터)로 복사본의 음량만 보정했다. 원본은 `SourceAssets/SFX/`에 그대로 있다.

| 파일 | 보정 | 이유 |
|---|---|---|
| `enemy_hit.wav` | +27 dB | 원본 최대 -33 dB로 거의 들리지 않음 |
| `explosion_small.wav` | +30 dB | 원본 최대 -33 dB |
| `ui_move.wav` | +25.8 dB | 원본 최대 -29 dB |
| `ui_select.wav` | +14.8 dB | 원본 최대 -18 dB |
| `powerup_pickup.wav` | +7.5 dB | 원본 최대 -10.5 dB |
| `boss_phase_transition.wav`, `player_death.wav`, `player_fire_lv3.wav`, `weapon_levelup.wav` | -1 dB | 0 dB 클리핑 여유 확보 |

## 도구

| 도구 | 용도 |
|---|---|
| ffmpeg 9.0.2 | 효과음 음량 보정 |

## 패키지

| 패키지 | 용도 |
|---|---|
| glTFast (`com.unity.cloud.gltfast` 6.20.0) | `.glb` 모델 임포트 |
| Timeline (`com.unity.timeline` 6.6.0) | 엔딩 시퀀스 (M9) |

## 절차적 생성 (외부 에셋·AI 미사용)

- `Art/Textures/T_Earth.png`: 지구 텍스처. `Editor/EndingBuilder.cs`가 펄린 노이즈로 생성 (M9)
- `VFX/Textures/T_VfxSoft.png`, `T_VfxRing.png`: 파티클 텍스처. `Editor/VfxBuilder.cs`가 생성 (M8)
