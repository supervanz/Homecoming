using System.Collections;
using UnityEngine;

namespace Homecoming.Boss
{
    public enum LaserMode
    {
        /// <summary>startAngle 방향으로 고정</summary>
        Fixed,
        /// <summary>발사 중 startAngle → endAngle로 쓸어 온다</summary>
        Sweep,
    }

    /// <summary>예고선을 보여준 뒤 레이저를 쏜다. 각도 0 = 화면 아래쪽.</summary>
    [CreateAssetMenu(menuName = "Homecoming/Boss/Laser Action", fileName = "BA_Laser")]
    public class BossLaserAction : BossActionConfig
    {
        public LaserMode mode = LaserMode.Fixed;
        [Tooltip("시작 각도 (도, 0 = 아래)")]
        public float startAngle = 0f;
        [Tooltip("Sweep에서 끝 각도")]
        public float endAngle = 0f;
        [Tooltip("예고선 표시 시간 (초)")]
        public float telegraphTime = 1.2f;
        [Tooltip("레이저 발사 시간 (초)")]
        public float fireTime = 1.2f;
        [Tooltip("레이저 판정 폭")]
        public float width = 1.6f;
        public float length = 40f;
        public int damage = 1;

        public override IEnumerator Run(BossController boss)
        {
            yield return boss.Laser.Play(this, boss.CoreMuzzle);
        }
    }
}
