using UnityEngine;

namespace Homecoming.Player
{
    /// <summary>좌우 이동 속도에 따라 외형 모델만 기울인다. 판정 위치에는 영향이 없다.</summary>
    public class PlayerRoll : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerMovement movement;
        [Tooltip("기울일 외형 모델 (로직 오브젝트의 자식)")]
        [SerializeField] private Transform visual;

        public float CurrentAngle { get; private set; }

        private void Update()
        {
            float normalized = movement.MaxSpeed > 0f ? movement.Velocity.x / movement.MaxSpeed : 0f;
            // 오른쪽으로 갈 때 오른쪽 날개가 내려가도록 Z축 음의 방향으로 기울인다.
            float target = -Mathf.Clamp(normalized, -1f, 1f) * config.rollMaxAngle;
            float t = 1f - Mathf.Exp(-config.rollResponse * Time.deltaTime);
            CurrentAngle = Mathf.Lerp(CurrentAngle, target, t);
            visual.localRotation = Quaternion.Euler(0f, 0f, CurrentAngle);
        }
    }
}
