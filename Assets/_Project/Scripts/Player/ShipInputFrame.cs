using UnityEngine;

namespace Homecoming.Player
{
    /// <summary>한 프레임 동안의 기체 입력. 사람과 봇이 같은 형식을 쓴다.</summary>
    public struct ShipInputFrame
    {
        public Vector2 Move;
        public bool Fire;
        public bool Bomb;
    }
}
