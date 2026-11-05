using System;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>체력 값과 피해·사망 이벤트만 담당한다.</summary>
    public class Health : MonoBehaviour
    {
        public int Max { get; private set; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        public event Action<Health, int> Damaged;
        public event Action<Health> Died;

        public void ResetHealth(int max)
        {
            Max = Mathf.Max(1, max);
            Current = Max;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(this, amount);
            if (Current == 0) Died?.Invoke(this);
        }
    }
}
