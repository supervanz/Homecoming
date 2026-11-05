using Homecoming.Boss;
using Homecoming.Weapon;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Homecoming.Core
{
    /// <summary>
    /// 피격 판정 디버그 표시 (11-1). F2로 켜고 끈다. 빌드에서도 동작한다.
    /// 플레이어 판정(분홍), 적 탄(빨강), 맞을 수 있는 대상(하양), 발사 중 레이저(빨강 선)를 평면 위에 원/선으로 그린다.
    /// </summary>
    public class HitboxOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private CircleHitbox playerHitbox;
        [SerializeField] private BulletPool enemyBullets;
        [SerializeField] private Color playerColor = new Color(1f, 0.3f, 1f, 1f);
        [SerializeField] private Color bulletColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color targetColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private int segments = 20;
        [Tooltip("평면 위로 살짝 띄워 그린다")]
        [SerializeField] private float height = 0.3f;

        private Material _material;

        private void OnEnable() => RenderPipelineManager.endCameraRendering += Draw;
        private void OnDisable() => RenderPipelineManager.endCameraRendering -= Draw;

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) visible = !visible;
        }

        private void Draw(ScriptableRenderContext context, Camera cam)
        {
            if (!visible || cam.cameraType != CameraType.Game) return;
            if (_material == null)
            {
                _material = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                _material.SetInt("_ZTest", (int)CompareFunction.Always);
            }

            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.modelview = cam.worldToCameraMatrix;
            _material.SetPass(0);
            GL.Begin(GL.LINES);

            var targets = HitTargetRegistry.Active;
            for (int i = 0; i < targets.Count; i++)
                Circle(targets[i].Hitbox.Center, targets[i].Hitbox.Radius, targetColor);

            var bullets = enemyBullets.Active;
            for (int i = 0; i < bullets.Count; i++)
                Circle(bullets[i].Position, bullets[i].Radius, bulletColor);

            if (playerHitbox != null && playerHitbox.isActiveAndEnabled)
                Circle(playerHitbox.Center, playerHitbox.Radius, playerColor);

            GL.End();
            GL.PopMatrix();
        }

        private void Circle(Vector3 c, float r, Color color)
        {
            GL.Color(color);
            c.y = height;
            Vector3 prev = c + new Vector3(r, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector3 next = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                GL.Vertex(prev);
                GL.Vertex(next);
                prev = next;
            }
        }
    }
}
