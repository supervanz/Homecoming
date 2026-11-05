# Credits

AI로 생성한 원화·메쉬·사운드와 외부 에셋의 출처를 기록한다 (CLAUDE.md 10장).

## AI 생성 에셋

생성 도구: Unity AI Generators (`unity ai` CLI 플러그인 0.1.0-alpha.11), 2026-11-05

| 게임 내 파일 | 용도 | 생성 단계 (모델) | 포인트 | 비고 |
|---|---|---|---|---|
| `Art/Models/player_ship.glb` | 플레이어 유랑선 | 컨셉 이미지 `gemini-nano-banana-2-1` → 배경 제거 `photoroom-bg-removal` → 3D `model3d-tripo-p2` (Tripo P2, glb, 10k faces, PBR) | 8 + 3 + 186 | 임포트 후 Y 180° 회전, 1.4배 |
| `Art/Models/sat_small.glb` | 잡몹 위성, 보스 소환 드론 | 컨셉 이미지 `gemini-nano-banana-2-1` → 배경 제거 `photoroom-bg-removal` → 3D `model3d-tripo-p2` (Tripo P2, glb) | 8 + 3 + 186 | 생성 텍스처가 회색 한 가지로 나와 메쉬만 사용. 재질은 `M_EnemyPrimitive`(흰 금속)로 교체. 2배 |

- 컨셉 이미지와 생성 기록 원본은 에셋 준비 작업 폴더(`SourceAssets/`)에 보관되어 있고, 게임 빌드에는 들어가지 않는다.

## 패키지

| 패키지 | 용도 |
|---|---|
| glTFast (`com.unity.cloud.gltfast` 6.20.0) | `.glb` 모델 임포트 |
