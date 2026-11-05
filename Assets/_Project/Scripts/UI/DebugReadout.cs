using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.UI
{
    /// <summary>
    /// 임시 디버그 표시 (M6 HUD 전까지). 체력·무기 단계·폭탄·경과 시간·시드, 보스 상태, WARNING을 보여준다.
    /// </summary>
    public class DebugReadout : MonoBehaviour
    {
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private int fontSize = 18;
        [SerializeField] private int bannerFontSize = 48;
        [SerializeField] private Vector2 position = new Vector2(12f, 12f);

        private GUIStyle _style;
        private GUIStyle _banner;

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = Color.white } };
            _banner ??= new GUIStyle(GUI.skin.label) { fontSize = bannerFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.25f, 0.25f) } };

            Health h = player.Health;
            string text =
                "[DEBUG]\n" +
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

            if (bossDirector == null) return;
            var center = new Rect(0f, Screen.height * 0.35f, Screen.width, 80f);
            if (bossDirector.WarningActive)
            {
                if (Mathf.FloorToInt(Time.time * 4f) % 2 == 0) GUI.Label(center, "WARNING", _banner);
            }
            else if (bossDirector.StageCleared)
            {
                GUI.Label(center, "STAGE CLEAR", _banner);
            }
        }
    }
}
