using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 게임 로직이 일어나는 XZ 평면 위의 사각형 영역. 이 오브젝트의 위치가 중심이다.
    /// 카메라와 영역은 고정이고, 스크롤은 배경으로 표현한다.
    /// </summary>
    public class PlayArea : MonoBehaviour
    {
        [SerializeField] private PlayAreaConfig config;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.4f, 0.8f);

        public Vector2 Size => config.size;
        public Vector3 Center => new Vector3(transform.position.x, 0f, transform.position.z);

        public Vector3 Clamp(Vector3 position, float margin)
        {
            Vector2 half = config.size * 0.5f - new Vector2(margin, margin);
            Vector3 c = Center;
            return new Vector3(
                Mathf.Clamp(position.x, c.x - half.x, c.x + half.x),
                0f,
                Mathf.Clamp(position.z, c.z - half.y, c.z + half.y));
        }

        /// <summary>margin이 양수면 영역보다 넓게, 음수면 좁게 검사한다.</summary>
        public bool Contains(Vector3 position, float margin)
        {
            Vector2 half = config.size * 0.5f + new Vector2(margin, margin);
            Vector3 c = Center;
            return Mathf.Abs(position.x - c.x) <= half.x && Mathf.Abs(position.z - c.z) <= half.y;
        }

        private void OnDrawGizmos()
        {
            if (config == null) return;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(Center, new Vector3(config.size.x, 0f, config.size.y));
        }
    }
}
