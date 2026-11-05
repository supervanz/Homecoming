using Homecoming.Core;
using UnityEngine;

namespace Homecoming.Player
{
    /// <summary>XZ 평면(Y=0) 위 이동. 플레이 영역 밖으로 나가지 않는다.</summary>
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private ShipInput input;
        [SerializeField] private PlayArea playArea;

        /// <summary>이번 프레임 실제 이동 속도 (경계에 막히면 0에 가까워짐)</summary>
        public Vector3 Velocity { get; private set; }
        public float MaxSpeed => config.moveSpeed;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 move = input.Current.Move;
            Vector3 before = transform.position;
            Vector3 target = before + new Vector3(move.x, 0f, move.y) * (config.moveSpeed * dt);
            Vector3 after = playArea.Clamp(target, config.edgeMargin);

            transform.position = after;
            Velocity = (after - before) / dt;
        }
    }
}
