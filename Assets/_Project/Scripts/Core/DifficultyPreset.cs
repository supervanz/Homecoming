using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 난이도 프리셋 (Easy / Normal / Hard). 각 설정 에셋의 기본 수치에 배율·보정값을 얹는다.
    /// Normal은 모두 1배·0이라 기본 수치 그대로다. 세밀한 조정은 개별 설정 에셋에서 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Homecoming/Difficulty Preset", fileName = "Difficulty")]
    public class DifficultyPreset : ScriptableObject
    {
        [Header("Player")]
        [Tooltip("이동 속도 배율")]
        public float playerMoveSpeedScale = 1f;
        [Tooltip("연사 간격 배율 (작을수록 빠르게 쏨)")]
        public float playerFireIntervalScale = 1f;
        [Tooltip("피격 판정 반경 배율 (작을수록 쉬움)")]
        public float playerHitRadiusScale = 1f;
        [Tooltip("피격 후 무적 시간 배율")]
        public float invincibleTimeScale = 1f;
        [Tooltip("최대 체력에 더하는 값")]
        public int playerMaxHpBonus = 0;
        [Tooltip("시작 폭탄 수에 더하는 값")]
        public int bombCountBonus = 0;

        [Header("Enemy")]
        [Tooltip("일반 적 체력 배율")]
        public float enemyHpScale = 1f;
        [Tooltip("일반 적 이동 속도 배율 (궤도 포대는 배경 속도라 제외)")]
        public float enemySpeedScale = 1f;
        [Tooltip("적 사격 간격 배율 (작을수록 자주 쏨)")]
        public float enemyFireIntervalScale = 1f;
        [Tooltip("적 탄 속도 배율 (보스 포함)")]
        public float enemyBulletSpeedScale = 1f;

        [Header("Boss")]
        [Tooltip("보스 본체·포대 체력 배율")]
        public float bossHpScale = 1f;
        [Tooltip("보스 행동 사이 간격 배율 (작을수록 몰아침)")]
        public float bossActionGapScale = 1f;

        [Header("Items")]
        [Tooltip("파워업 드롭 확률 배율 (확정 드롭은 제외)")]
        public float dropChanceScale = 1f;
    }
}
