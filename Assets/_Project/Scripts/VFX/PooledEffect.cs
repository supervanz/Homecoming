using UnityEngine;

namespace Homecoming.Vfx
{
    /// <summary>
    /// 풀에서 꺼내 쓰는 일회성 파티클 이펙트. 루트 파티클이 멈추면(stopAction = Callback) 풀로 돌아간다.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledEffect : MonoBehaviour
    {
        private ParticleSystem _root;
        private System.Action<PooledEffect> _release;

        public VfxId Id { get; private set; }
        public ParticleSystem Root => _root != null ? _root : (_root = GetComponent<ParticleSystem>());

        public void Bind(VfxId id, System.Action<PooledEffect> release)
        {
            Id = id;
            _release = release;
            var main = Root.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        public void PlayAt(Vector3 position, float scale)
        {
            transform.position = position;
            transform.localScale = Vector3.one * scale;
            Root.Clear(true);
            Root.Play(true);
        }

        private void OnParticleSystemStopped() => _release?.Invoke(this);
    }
}
