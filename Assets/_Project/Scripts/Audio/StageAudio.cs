using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Weapon;
using UnityEngine;

namespace Homecoming.Audio
{
    /// <summary>
    /// 스테이지 사운드 연결: 게임 이벤트를 받아 어떤 효과음·음악을 낼지 정한다 (14-2, 14-3, 14-4).
    /// 게임 로직 쪽은 이벤트만 보내고 소리를 모른다.
    /// </summary>
    public class StageAudio : MonoBehaviour
    {
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private GameFlow flow;
        [Tooltip("폭탄 덕킹 시간 (초)")]
        [SerializeField] private float bombDuck = 1.5f;
        [Tooltip("페이즈 전환 덕킹 시간 (초)")]
        [SerializeField] private float transitionDuck = 1.5f;
        [Tooltip("보스 격파 덕킹 시간 (초)")]
        [SerializeField] private float bossDestroyDuck = 3f;

        private bool _warning;

        private static SfxPlayer Sfx => SfxPlayer.Instance;
        private static MusicPlayer Music => MusicPlayer.Instance;
        private static void Play(SoundId id) { if (Sfx != null) Sfx.Play(id); }

        private void OnEnable()
        {
            weapon.Fired += OnFired;
            weapon.PowerUpCollected += OnPowerUp;
            weapon.LevelChanged += OnLevelUp;
            bomb.BombUsed += OnBomb;
            player.Hit += OnPlayerHit;
            player.Health.Died += OnPlayerDied;
            HitTarget.AnyDamaged += OnTargetDamaged;
            BulletEmitter.Fired += OnEnemyFired;
            EnemyUnit.Killed += OnEnemyKilled;
            BossLaser.TelegraphStarted += OnLaserCharge;
            BossLaser.FireStarted += OnLaserFire;
            BossSummonAction.Summoned += OnSummon;
            bossDirector.Boss.StateChanged += OnBossState;
            foreach (var p in bossDirector.Boss.Panels) p.Destroyed += OnPanelDestroyed;
            flow.ResultReady += OnResult;
            flow.MenuChosen += OnMenuChosen;
        }

        private void OnDisable()
        {
            if (weapon != null) { weapon.Fired -= OnFired; weapon.PowerUpCollected -= OnPowerUp; weapon.LevelChanged -= OnLevelUp; }
            if (bomb != null) bomb.BombUsed -= OnBomb;
            if (player != null) { player.Hit -= OnPlayerHit; player.Health.Died -= OnPlayerDied; }
            HitTarget.AnyDamaged -= OnTargetDamaged;
            BulletEmitter.Fired -= OnEnemyFired;
            EnemyUnit.Killed -= OnEnemyKilled;
            BossLaser.TelegraphStarted -= OnLaserCharge;
            BossLaser.FireStarted -= OnLaserFire;
            BossSummonAction.Summoned -= OnSummon;
            if (bossDirector != null && bossDirector.Boss != null)
            {
                bossDirector.Boss.StateChanged -= OnBossState;
                foreach (var p in bossDirector.Boss.Panels) p.Destroyed -= OnPanelDestroyed;
            }
            if (flow != null) { flow.ResultReady -= OnResult; flow.MenuChosen -= OnMenuChosen; }
            if (Sfx != null) Sfx.StopAllLoops();
        }

        private void Start()
        {
            if (Music != null) Music.Play(MusicTrack.Stage);
        }

        private void Update()
        {
            if (Sfx == null) return;

            // 저체력 경고 (반복)
            Health h = player.Health;
            bool low = player.IsAlive && flow.State == GameFlow.FlowState.Playing && h.Current <= Sfx.Config.lowHpThreshold;
            if (low) Sfx.StartLoop(SoundId.LowHpWarning);
            else Sfx.StopLoop(SoundId.LowHpWarning);

            // WARNING: 음악이 멎고 경보음 + 저음 드론 (14-2)
            if (bossDirector.WarningActive && !_warning)
            {
                _warning = true;
                if (Music != null) Music.Stop();
                Sfx.StartLoop(SoundId.BossWarningAlarm);
                Sfx.StartLoop(SoundId.WarningDrone);
            }
        }

        private void OnFired(int level) => Play(level >= 3 ? SoundId.PlayerFireLv3 : level == 2 ? SoundId.PlayerFireLv2 : SoundId.PlayerFireLv1);
        private void OnPowerUp() => Play(SoundId.PowerUpPickup);
        private void OnLevelUp(int _) => Play(SoundId.WeaponLevelUp);
        private void OnPlayerHit(string source, int damage) => Play(SoundId.PlayerHit);
        private void OnTargetDamaged(HitTarget _) => Play(SoundId.EnemyHit);
        private void OnEnemyFired() => Play(SoundId.EnemyFire);
        private void OnEnemyKilled(EnemyUnit e) => Play(e.Config.deathSound);
        private void OnPanelDestroyed(BossPart _) => Play(SoundId.ExplosionMedium);
        private void OnLaserCharge() => Play(SoundId.LaserCharge);
        private void OnLaserFire() => Play(SoundId.LaserFire);
        private void OnSummon() => Play(SoundId.BossDroneSummon);
        private void OnMenuChosen() => Play(SoundId.UiSelect);

        private void OnBomb()
        {
            Play(SoundId.Bomb);
            if (Sfx != null) Sfx.Duck(bombDuck);
            if (Music != null) Music.Duck(bombDuck);
        }

        private void OnPlayerDied(Health _)
        {
            Play(SoundId.PlayerDeath);
            if (Sfx != null) Sfx.StopLoop(SoundId.LowHpWarning);
            if (Music != null) Music.Stop();
        }

        private void OnBossState(BossController.BossState state)
        {
            switch (state)
            {
                case BossController.BossState.Entering:
                    if (Sfx != null) { Sfx.StopLoop(SoundId.BossWarningAlarm); Sfx.StopLoop(SoundId.WarningDrone); }
                    if (Music != null) Music.Play(MusicTrack.BossPhase1);
                    break;
                case BossController.BossState.Transition:
                    Play(SoundId.BossPhaseTransition);
                    if (Music != null) Music.Duck(transitionDuck);
                    break;
                case BossController.BossState.Phase2:
                    if (Music != null) Music.Play(MusicTrack.BossPhase2);
                    break;
                case BossController.BossState.Defeated:
                    Play(SoundId.BossDestroy);
                    if (Sfx != null) Sfx.Duck(bossDestroyDuck);
                    if (Music != null) Music.Stop();
                    break;
            }
        }

        private void OnResult()
        {
            if (Sfx != null) Sfx.StopAllLoops();
            if (flow.State == GameFlow.FlowState.Cleared) Play(SoundId.StageClear);
            else if (Music != null) Music.Play(MusicTrack.GameOver);
        }
    }
}
