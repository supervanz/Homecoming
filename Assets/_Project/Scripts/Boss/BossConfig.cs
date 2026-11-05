using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>보스 수치와 페이즈별 행동 순서.</summary>
    [CreateAssetMenu(menuName = "Homecoming/Boss/Boss Config", fileName = "BossConfig")]
    public class BossConfig : ScriptableObject
    {
        [Header("Durability")]
        public int coreHp = 1500;
        [Tooltip("태양전지판 포대 하나의 체력. 본체 체력과 별개")]
        public int panelHp = 150;
        [Tooltip("이 비율 이하가 되면 2페이즈 (외피 탈락)")]
        [Range(0f, 1f)] public float phase2Threshold = 0.5f;
        [Tooltip("이 비율 이하가 되면 발악 패턴")]
        [Range(0f, 1f)] public float desperationThreshold = 0.15f;
        [Tooltip("폭탄 피해 배율")]
        public float bombDamageScale = 0.3f;
        public int contactDamage = 1;

        [Header("Hitbox")]
        [Tooltip("1페이즈 (외피 장착) 본체 판정 반경")]
        public float shellRadius = 2f;
        [Tooltip("2페이즈 (코어 노출) 본체 판정 반경")]
        public float coreRadius = 1f;

        [Header("Movement")]
        [Tooltip("플레이 영역 중심 기준 등장 시작 위치")]
        public Vector2 entryFrom = new Vector2(0f, 15f);
        [Tooltip("플레이 영역 중심 기준 전투 위치")]
        public Vector2 hoverPosition = new Vector2(0f, 5.5f);
        public float entrySpeed = 3f;
        [Tooltip("좌우 흔들림 폭")]
        public float swayAmplitude = 1.5f;
        [Tooltip("좌우 흔들림 주기 (초)")]
        public float swayPeriod = 7f;

        [Header("Phase transition")]
        [Tooltip("외피가 떨어지는 동안 보스 무적 시간 (초)")]
        public float transitionDuration = 2f;
        [Tooltip("외피 잔해가 떨어지는 속도")]
        public float debrisFallSpeed = 4f;

        [Header("Actions")]
        [Tooltip("순차 행동 사이 간격 (초)")]
        public float actionGap = 0.8f;
        public BossActionConfig[] phase1Actions = new BossActionConfig[0];
        public BossActionConfig[] phase2Actions = new BossActionConfig[0];
        public BossActionConfig[] desperationActions = new BossActionConfig[0];
    }
}
