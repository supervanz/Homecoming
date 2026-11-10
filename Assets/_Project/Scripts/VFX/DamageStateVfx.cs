using Homecoming.Audio;
using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Vfx
{
    /// <summary>
    /// 남은 체력에 따라 손상 상태를 단계적으로 보여준다 (13-4-1).
    /// 100~70% 정상 / 70~40% 간헐 스파크 / 40~15% 지속 불꽃 / 15% 이하 큰 불꽃 + 깜빡임.
    /// 중형 병기, 보스 부위·본체, 플레이어 기체에 붙인다.
    /// </summary>
    public class DamageStateVfx : MonoBehaviour
    {
        [SerializeField] private VfxConfig config;
        [SerializeField] private Health health;
        [SerializeField] private VfxFaction faction = VfxFaction.Ai;
        [Tooltip("이펙트를 붙일 위치. 보스 부위는 외형(떨어져 나가는 쪽)에 붙여 함께 사라지게 한다")]
        [SerializeField] private Transform anchor;
        [Tooltip("anchor 기준 위치. 기체 윗면 높이에 맞춘다")]
        [SerializeField] private Vector3 localOffset = Vector3.zero;
        [Tooltip("이펙트 크기 배율 (기체 크기에 맞춤)")]
        [SerializeField] private float scale = 1f;
        [Tooltip("깜빡임을 낼 HitFlash (없으면 깜빡임 생략)")]
        [SerializeField] private HitFlash flash;

        private ParticleSystem[] _stages;
        private int _stage = -1;

        public int Stage => _stage;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (anchor == null) anchor = transform;
            GameObject[] prefabs = config.DamagePrefabs(faction);
            _stages = new ParticleSystem[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null) continue;
                GameObject go = Instantiate(prefabs[i], anchor);
                go.transform.localPosition = localOffset;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * scale;
                _stages[i] = go.GetComponent<ParticleSystem>();
                _stages[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnEnable()
        {
            _stage = -1;
            SetStage(0);
        }

        private void Update()
        {
            int s = 0;
            if (health.Max > 0 && !health.IsDead)
            {
                float r = health.Current / (float)health.Max;
                if (r <= config.damageStage3) s = 3;
                else if (r <= config.damageStage2) s = 2;
                else if (r <= config.damageStage1) s = 1;
            }
            SetStage(s);
        }

        private void SetStage(int s)
        {
            if (s == _stage) return;
            bool worse = s > _stage && _stage >= 0;
            _stage = s;
            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stages[i] == null) continue;
                bool on = i == s - 1;
                if (on && !_stages[i].isPlaying) _stages[i].Play(true);
                else if (!on && _stages[i].isPlaying) _stages[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (flash != null) flash.CriticalBlink = s >= 3;
            // 손상 단계가 올라갈 때 스파크·불꽃 지글거림 (14-3)
            if (worse && SfxPlayer.Instance != null) SfxPlayer.Instance.Play(SoundId.DamageSparks);
        }
    }
}
