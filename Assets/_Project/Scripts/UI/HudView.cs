using System.Collections.Generic;
using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;
using UnityEngine.UIElements;

namespace Homecoming.UI
{
    /// <summary>HUD: 점수, 체력, 폭탄, 무기 단계, 보스 체력바, WARNING / STAGE CLEAR 배너.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private ScoreKeeper score;
        [SerializeField] private BossDirector bossDirector;
        [Tooltip("WARNING 깜빡임 속도 (회/초)")]
        [SerializeField] private float warningBlinkRate = 4f;

        private Label _score, _weapon, _banner;
        private VisualElement _hp, _bombs, _boss, _bossFill;
        private readonly List<VisualElement> _panelFills = new List<VisualElement>();
        private readonly List<VisualElement> _hpPips = new List<VisualElement>();
        private readonly List<VisualElement> _bombPips = new List<VisualElement>();
        private int _lastScore = -1, _lastLevel = -1, _lastHp = -1, _lastBombs = -1;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            _score = root.Q<Label>("score");
            _weapon = root.Q<Label>("weapon");
            _banner = root.Q<Label>("banner");
            _hp = root.Q("hp");
            _bombs = root.Q("bombs");
            _boss = root.Q("boss");
            _bossFill = root.Q("boss-fill");
            _panelFills.Clear();
            for (int i = 0; ; i++)
            {
                VisualElement e = root.Q($"panel-fill-{i}");
                if (e == null) break;
                _panelFills.Add(e);
            }
            _lastScore = _lastLevel = _lastHp = _lastBombs = -1;
        }

        private void Update()
        {
            if (_score == null) return;

            if (score.Score != _lastScore)
            {
                _lastScore = score.Score;
                _score.text = $"SCORE {_lastScore:D7}";
            }
            if (weapon.Level != _lastLevel)
            {
                _lastLevel = weapon.Level;
                _weapon.text = $"LV {_lastLevel}";
            }

            Health h = player.Health;
            if (h.Current != _lastHp)
            {
                _lastHp = h.Current;
                SetPips(_hp, _hpPips, h.Max, h.Current, "pip");
            }
            if (bomb.Count != _lastBombs)
            {
                _lastBombs = bomb.Count;
                SetPips(_bombs, _bombPips, Mathf.Max(_lastBombs, _bombPips.Count), _lastBombs, "pip bomb-pip");
            }

            UpdateBoss();
            UpdateBanner();
        }

        private static void SetPips(VisualElement container, List<VisualElement> pips, int max, int filled, string classes)
        {
            while (pips.Count < max)
            {
                var pip = new VisualElement();
                foreach (var c in classes.Split(' ')) pip.AddToClassList(c);
                container.Add(pip);
                pips.Add(pip);
            }
            for (int i = 0; i < pips.Count; i++)
                pips[i].EnableInClassList("pip--empty", i >= filled);
        }

        private void UpdateBoss()
        {
            bool show = bossDirector.BossSpawned && !bossDirector.StageCleared;
            _boss.EnableInClassList("boss--visible", show);
            if (!show) return;

            BossController b = bossDirector.Boss;
            _bossFill.style.width = Length.Percent(b.HpRatio * 100f);
            for (int i = 0; i < _panelFills.Count; i++)
            {
                float ratio = 0f;
                if (i < b.Panels.Count && !b.Panels[i].IsDestroyed)
                {
                    Health ph = b.Panels[i].Target.Health;
                    ratio = ph.Max > 0 ? ph.Current / (float)ph.Max : 0f;
                }
                _panelFills[i].style.width = Length.Percent(ratio * 100f);
            }
        }

        private void UpdateBanner()
        {
            if (bossDirector.WarningActive)
            {
                bool on = Mathf.FloorToInt(Time.time * warningBlinkRate) % 2 == 0;
                _banner.text = on ? "WARNING" : "";
                _banner.EnableInClassList("banner--clear", false);
            }
            else if (bossDirector.StageCleared)
            {
                _banner.text = "STAGE CLEAR";
                _banner.EnableInClassList("banner--clear", true);
            }
            else
            {
                _banner.text = "";
            }
        }
    }
}
