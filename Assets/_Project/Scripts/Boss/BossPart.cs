using System;
using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>
    /// 부위 파괴 가능한 보스 부위 (태양전지판 포대). 본체와 별도 체력.
    /// 부서지면 이 부위에서 나오던 탄이 멈춘다.
    /// </summary>
    [RequireComponent(typeof(HitTarget))]
    public class BossPart : MonoBehaviour
    {
        [Tooltip("부서지면 숨길 외형")]
        [SerializeField] private GameObject visual;
        [Tooltip("탄 발사 위치")]
        [SerializeField] private Transform muzzle;

        private HitTarget _target;

        public event Action<BossPart> Destroyed;

        public HitTarget Target => _target != null ? _target : (_target = GetComponent<HitTarget>());
        public bool IsDestroyed { get; private set; }
        public GameObject Visual => visual;
        public Vector3 MuzzlePosition => muzzle.position;

        private void Awake()
        {
            Target.Health.Died += _ => Break();
        }

        public void Activate(int hp, float bombDamageScale, int contactDamage)
        {
            IsDestroyed = false;
            visual.SetActive(true);
            Target.Health.ResetHealth(hp);
            Target.BombDamageScale = bombDamageScale;
            Target.ContactDamage = contactDamage;
            Target.SourceTag = "Boss_" + name;
            Target.Invulnerable = false;
            Target.Register();
        }

        /// <summary>격파: 판정 해제 + 외형 숨김</summary>
        private void Break()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            Target.Unregister();
            visual.SetActive(false);
            Destroyed?.Invoke(this);
        }

        /// <summary>페이즈 전환으로 떨어져 나감: 판정만 해제하고 외형은 잔해 연출에 넘긴다.</summary>
        public void Detach()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            Target.Unregister();
        }
    }
}
