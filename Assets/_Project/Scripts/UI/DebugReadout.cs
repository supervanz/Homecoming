using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.UI
{
    /// <summary>
    /// 임시 디버그 표시 (M6 HUD 전까지). 체력·무기 단계·폭탄·경과 시간·시드를 왼쪽 위에 보여준다.
    /// </summary>
    public class DebugReadout : MonoBehaviour
    {
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private int fontSize = 18;
        [SerializeField] private Vector2 position = new Vector2(12f, 12f);

        private GUIStyle _style;

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = Color.white } };
            Health h = player.Health;
            string text =
                "[DEBUG]\n" +
                $"HP {h.Current}/{h.Max}{(player.IsInvincible ? "  (invincible)" : "")}\n" +
                $"Weapon Lv {weapon.Level}/{weapon.MaxLevel}\n" +
                $"Bomb {bomb.Count}{(bomb.IsActive ? "  (active)" : "")}\n" +
                $"Time {spawner.Elapsed:0.0}s\n" +
                $"Seed {GameRandom.Seed}";
            GUI.Label(new Rect(position.x, position.y, 400f, 200f), text, _style);
        }
    }
}
