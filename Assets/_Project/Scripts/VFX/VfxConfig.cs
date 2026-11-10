using System;
using UnityEngine;

namespace Homecoming.Vfx
{
    /// <summary>
    /// VFX 강도 설정 (CLAUDE.md 13-7). 이펙트 프리팹, 화면 흔들림, 히트스톱, 손상 상태 구간을
    /// 코드 수정 없이 조정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Homecoming/Vfx Config", fileName = "VfxConfig")]
    public class VfxConfig : ScriptableObject
    {
        [Serializable]
        public class Effect
        {
            public VfxId id;
            public PooledEffect prefab;
            [Tooltip("미리 만들어 둘 개수")]
            [Min(0)] public int prewarm = 4;
            [Tooltip("동시에 재생할 수 있는 최대 개수 (넘으면 새 요청은 무시)")]
            [Min(1)] public int maxSimultaneous = 8;
            [Tooltip("재생 시 크기 배율")]
            public float scale = 1f;
            [Tooltip("화면 흔들림 세기 (0이면 흔들지 않음). 중형 폭발 이상에서만 쓴다 (13-5)")]
            [Min(0f)] public float shake = 0f;
            [Tooltip("화면 흔들림 시간 (초)")]
            [Min(0f)] public float shakeDuration = 0f;
            [Tooltip("히트스톱 시간 (실제 초). 페이즈 전환과 보스 격파에만 쓴다 (13-5)")]
            [Min(0f)] public float hitStop = 0f;
        }

        [Header("Layering (13-1)")]
        [Tooltip("이펙트를 그릴 높이. 플레이 평면(0)보다 아래에 두어 불투명한 적 탄이 항상 앞에 보이게 한다")]
        public float effectHeight = -0.6f;
        [Tooltip("동시에 재생 중인 일회성 이펙트 전체 상한 (WebGL 오버드로 제한)")]
        [Min(1)] public int maxActiveEffects = 40;

        [Header("Hit flash (13-5)")]
        [Tooltip("피탄 시 하얀 플래시 시간 (초). 1~2프레임")]
        public float hitFlashDuration = 0.05f;
        [ColorUsage(false, true)] public Color hitFlashColor = new Color(2f, 2f, 2f, 1f);

        [Header("Screen shake (13-5)")]
        [Tooltip("옵션 기본값. 플레이어가 끄면 PlayerPrefs에 저장된다")]
        public bool screenShakeDefault = true;
        [Tooltip("흔들림 최대 이동량 상한")]
        public float maxShake = 0.6f;

        [Header("Damage states (13-4-1) - 남은 체력 비율 상한")]
        [Range(0f, 1f)] public float damageStage1 = 0.7f;
        [Range(0f, 1f)] public float damageStage2 = 0.4f;
        [Range(0f, 1f)] public float damageStage3 = 0.15f;
        [Tooltip("AI 기체 손상 이펙트 (1단계 스파크, 2단계 불꽃, 3단계 큰 불꽃)")]
        public GameObject[] aiDamagePrefabs = new GameObject[3];
        [Tooltip("플레이어 기체 손상 이펙트 (따뜻한 주황 불꽃)")]
        public GameObject[] humanDamagePrefabs = new GameObject[3];
        [Tooltip("3단계에서 기체가 깜빡이는 주기 (초)")]
        public float criticalBlinkInterval = 0.25f;
        [ColorUsage(false, true)] public Color criticalBlinkColor = new Color(1.2f, 0.15f, 0.1f, 1f);

        public Effect[] effects = new Effect[0];

        public Effect Find(VfxId id)
        {
            foreach (var e in effects) if (e.id == id) return e;
            return null;
        }

        public GameObject[] DamagePrefabs(VfxFaction f) => f == VfxFaction.Human ? humanDamagePrefabs : aiDamagePrefabs;
    }
}
