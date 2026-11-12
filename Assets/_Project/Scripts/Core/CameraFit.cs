using Homecoming.Vfx;
using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 화면 비율이 바뀌어도 플레이 영역 전체가 화면 안에 들어오도록 카메라를 시선 반대 방향으로 물린다.
    /// 시야각(FOV)은 그대로 두어 원근 왜곡이 커지지 않게 한다. 영역이 이미 들어오면 움직이지 않는다
    /// (가로 16:9에서는 원래 위치 그대로). 게임 로직(평면 위 좌표·판정)은 바꾸지 않는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private PlayArea playArea;
        [Tooltip("영역 바깥으로 더 보여 줄 여유 (유닛). 기체 외형이 경계에서 잘리지 않게")]
        [SerializeField] private float worldMargin = 0.8f;
        [Tooltip("화면 위쪽 여백 (비율). 점수·보스 체력바가 영역을 가리지 않게")]
        [SerializeField] private float topPadding = 0.07f;
        [Tooltip("화면 아래쪽 여백 (비율). 체력·폭탄·무기 단계 표시가 영역을 가리지 않게")]
        [SerializeField] private float bottomPadding = 0.08f;
        [Tooltip("좌우 여백 (비율)")]
        [SerializeField] private float sidePadding = 0.02f;
        [Tooltip("최대로 물러날 거리 (유닛)")]
        [SerializeField] private float maxPullBack = 120f;

        private Camera _cam;
        private Vector3 _basePosition;
        private float _lastAspect = -1f;

        /// <summary>지금 물러난 거리 (유닛)</summary>
        public float PullBack { get; private set; }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _basePosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (Mathf.Approximately(_cam.aspect, _lastAspect)) return;
            _lastAspect = _cam.aspect;
            Fit();
        }

        public void Fit()
        {
            Vector3 back = -transform.forward;
            // 물러날수록 화면 안으로 들어오므로 이분 탐색으로 가장 작은 거리를 찾는다
            float lo = 0f, hi = maxPullBack;
            if (Fits(0f)) hi = 0f;
            else
                for (int i = 0; i < 24; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (Fits(mid)) hi = mid; else lo = mid;
                }
            PullBack = hi;
            Vector3 p = _basePosition + transform.parent.InverseOrIdentity(back) * hi;
            if (CameraShake.Instance != null && CameraShake.Instance.transform == transform) CameraShake.Instance.SetBasePosition(p);
            transform.localPosition = p;
        }

        private bool Fits(float pullBack)
        {
            Vector3 saved = transform.localPosition;
            transform.localPosition = _basePosition + transform.parent.InverseOrIdentity(-transform.forward) * pullBack;
            Vector2 half = playArea.Size * 0.5f + Vector2.one * worldMargin;
            Vector3 c = playArea.Center;
            bool ok = true;
            for (int i = 0; i < 4 && ok; i++)
            {
                var corner = new Vector3(c.x + (i % 2 == 0 ? -half.x : half.x), 0f, c.z + (i < 2 ? -half.y : half.y));
                Vector3 v = _cam.WorldToViewportPoint(corner);
                ok = v.z > 0f && v.x >= sidePadding && v.x <= 1f - sidePadding && v.y >= bottomPadding && v.y <= 1f - topPadding;
            }
            transform.localPosition = saved;
            return ok;
        }
    }

    internal static class TransformDirectionExtensions
    {
        /// <summary>월드 방향을 부모 기준 방향으로 바꾼다. 부모가 없으면 그대로.</summary>
        public static Vector3 InverseOrIdentity(this Transform parent, Vector3 worldDir) =>
            parent != null ? parent.InverseTransformDirection(worldDir) : worldDir;
    }
}
