using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>
    /// 카메라·플레이 영역은 고정하고 배경만 아래로 흘려 종스크롤을 표현한다.
    /// 프리미티브 단계라 작은 큐브를 별처럼 깐다. M7에서 배경 아트로 교체한다.
    /// </summary>
    public class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private ScrollConfig config;
        [SerializeField] private Material starMaterial;

        private Transform[] _stars;
        private float[] _speeds;
        private float[] _sizes;

        /// <summary>별 크기 배율 (엔딩의 하늘 전환에서 0으로 줄여 사라지게 한다)</summary>
        public void SetVisibility(float v)
        {
            if (_stars == null) return;
            for (int i = 0; i < _stars.Length; i++)
                _stars[i].localScale = Vector3.one * (_sizes[i] * Mathf.Clamp01(v));
        }

        private void Start()
        {
            var rng = new System.Random(config.seed);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            _stars = new Transform[config.starCount];
            _speeds = new float[config.starCount];
            _sizes = new float[config.starCount];
            Vector2 half = config.fieldSize * 0.5f;

            for (int i = 0; i < config.starCount; i++)
            {
                var go = new GameObject($"Star{i}");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = cube;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = starMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                float size = Range(config.minStarSize, config.maxStarSize);
                go.transform.localScale = Vector3.one * size;
                _sizes[i] = size;
                go.transform.localPosition = new Vector3(Range(-half.x, half.x), -config.depth, Range(-half.y, half.y));
                _stars[i] = go.transform;
                _speeds[i] = config.scrollSpeed * Range(config.parallaxRange.x, config.parallaxRange.y);
            }
        }

        private void Update()
        {
            if (_stars == null) return;
            float half = config.fieldSize.y * 0.5f;
            float dt = Time.deltaTime;
            for (int i = 0; i < _stars.Length; i++)
            {
                Vector3 p = _stars[i].localPosition;
                p.z -= _speeds[i] * dt;
                if (p.z < -half) p.z += config.fieldSize.y;
                _stars[i].localPosition = p;
            }
        }
    }
}
