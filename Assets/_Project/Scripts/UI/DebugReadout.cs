using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Homecoming.UI
{
    /// <summary>
    /// 개발용 디버그 표시. F1로 켜고 끈다 (기본 꺼짐).
    /// 체력·무기 단계·폭탄·경과 시간·시드·보스 상태를 왼쪽 위에 보여준다.
    /// </summary>
    public class DebugReadout : MonoBehaviour
    {
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private bool visible = false;
        [SerializeField] private int fontSize = 18;
        [SerializeField] private Vector2 position = new Vector2(12f, 12f);

        private GUIStyle _style;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = Color.white } };

            Health h = player.Health;
            string text =
                "[DEBUG] F1\n" +
                $"HP {h.Current}/{h.Max}{(player.IsInvincible ? "  (invincible)" : "")}\n" +
                $"Weapon Lv {weapon.Level}/{weapon.MaxLevel}\n" +
                $"Bomb {bomb.Count}{(bomb.IsActive ? "  (active)" : "")}\n" +
                $"Time {spawner.Elapsed:0.0}s\n" +
                $"Seed {GameRandom.Seed}";

            if (bossDirector != null && bossDirector.BossSpawned && !bossDirector.StageCleared)
            {
                BossController b = bossDirector.Boss;
                text += $"\nBoss {b.State}  HP {b.Core.Health.Current}/{b.Core.Health.Max} ({b.HpRatio:P0})";
                for (int i = 0; i < b.Panels.Count; i++)
                {
                    BossPart p = b.Panels[i];
                    text += $"\n  Panel{i} {(p.IsDestroyed ? "destroyed" : $"{p.Target.Health.Current}/{p.Target.Health.Max}")}";
                }
            }
            GUI.Label(new Rect(position.x, position.y, 500f, 400f), text, _style);
        }
    }
}
