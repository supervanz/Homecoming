using System;
using System.Collections.Generic;
using UnityEngine;

namespace Homecoming.Enemy
{
    /// <summary>PathConfig를 따라 평면 위를 일정 속도로 이동한다. 끝에 도달하면 Finished를 알린다.</summary>
    public class PathFollower : MonoBehaviour
    {
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _waits = new List<float>();
        private int _target;
        private float _waitTimer;
        private float _speed;
        private bool _running;

        public event Action Finished;

        public void Begin(PathConfig path, Vector3 origin, bool mirror, float scrollSpeed)
        {
            BuildPoints(path, origin, mirror);
            _speed = path.useScrollSpeed ? scrollSpeed : path.speed;
            transform.position = _points[0];
            _target = 1;
            _waitTimer = _waits[0];
            _running = _points.Count > 1;
        }

        public void Stop() => _running = false;

        private void BuildPoints(PathConfig path, Vector3 origin, bool mirror)
        {
            _points.Clear();
            _waits.Clear();
            var raw = new List<Vector3>(path.points.Length);
            foreach (var p in path.points)
                raw.Add(origin + new Vector3(mirror ? -p.position.x : p.position.x, 0f, p.position.y));

            if (!path.smooth || raw.Count < 3)
            {
                _points.AddRange(raw);
                foreach (var p in path.points) _waits.Add(p.wait);
                return;
            }

            int sub = Mathf.Max(1, path.smoothSubdivisions);
            for (int i = 0; i < raw.Count - 1; i++)
            {
                Vector3 p0 = raw[Mathf.Max(0, i - 1)];
                Vector3 p1 = raw[i];
                Vector3 p2 = raw[i + 1];
                Vector3 p3 = raw[Mathf.Min(raw.Count - 1, i + 2)];
                for (int s = 0; s < sub; s++)
                {
                    _points.Add(CatmullRom(p0, p1, p2, p3, s / (float)sub));
                    _waits.Add(s == 0 ? path.points[i].wait : 0f);
                }
            }
            _points.Add(raw[raw.Count - 1]);
            _waits.Add(path.points[raw.Count - 1].wait);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private void Update()
        {
            if (!_running) return;
            float dt = Time.deltaTime;

            if (_waitTimer > 0f)
            {
                _waitTimer -= dt;
                return;
            }

            float step = _speed * dt;
            Vector3 pos = transform.position;
            while (step > 0f && _target < _points.Count)
            {
                Vector3 to = _points[_target] - pos;
                float dist = to.magnitude;
                if (dist > step)
                {
                    pos += to / dist * step;
                    step = 0f;
                }
                else
                {
                    pos = _points[_target];
                    step -= dist;
                    _waitTimer = _waits[_target];
                    _target++;
                    if (_waitTimer > 0f) break;
                }
            }
            pos.y = 0f;
            transform.position = pos;

            if (_target >= _points.Count && _waitTimer <= 0f)
            {
                _running = false;
                Finished?.Invoke();
            }
        }
    }
}
