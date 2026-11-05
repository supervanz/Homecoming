using UnityEngine;

namespace Homecoming.Core
{
    /// <summary>XZ 평면에 원을 Gizmo 선으로 그린다.</summary>
    public static class GizmoCircle
    {
        private const int Segments = 24;

        public static void Draw(Vector3 center, float radius)
        {
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
