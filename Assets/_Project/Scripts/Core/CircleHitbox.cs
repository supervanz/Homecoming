using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>XZ 평면 위의 원형 판정. 카메라 각도와 무관하게 평면 좌표로만 계산한다.</summary>
    public class CircleHitbox : MonoBehaviour
    {
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 1f, 1f);

        public float Radius
        {
            get => radius;
            set => radius = value;
        }

        public Vector3 Center => new Vector3(transform.position.x, 0f, transform.position.z);

        public bool Overlaps(Vector3 point, float otherRadius)
        {
            Vector3 d = Center - point;
            d.y = 0f;
            float r = radius + otherRadius;
            return d.sqrMagnitude <= r * r;
        }

        // 피격 판정 디버그 표시 (Scene 뷰, Game 뷰의 Gizmos 토글)
        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            GizmoCircle.Draw(Center, radius);
        }
    }
}
