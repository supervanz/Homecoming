using Homecoming.Boss;
using Homecoming.Bot;
using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Homecoming.Vfx
{
    /// <summary>
    /// 스테이지 VFX 연결: 게임 이벤트를 받아 어떤 이펙트·흔들림·히트스톱을 낼지 정한다 (13장).
    /// 게임 로직 쪽은 이벤트만 보내고 이펙트를 모른다 (StageAudio와 같은 구조).
    /// </summary>
    public class StageVfx : MonoBehaviour
    {
        [SerializeField] private VfxPool pool;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private HitStop hitStop;
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private BossDirector bossDirector;

        private VfxConfig Config => pool.Config;

        private void Awake()
        {
            // 시뮬레이션은 같은 시드면 같은 판이 나와야 하므로 시간을 건드리는 히트스톱을 끄고,
            // 빠르게 도는 판에서 이펙트 비용도 아낀다.
            bool simulating = SimulationSession.Active != null;
            pool.Enabled = !simulating;
            hitStop.Enabled = !simulating;
        }

        private void OnEnable()
        {
            HitTarget.AnyDamaged += OnTargetDamaged;
            EnemyUnit.Killed += OnEnemyKilled;
            player.Hit += OnPlayerHit;
            player.Health.Died += OnPlayerDied;
            bomb.BombUsed += OnBomb;
            bossDirector.Boss.StateChanged += OnBossState;
            foreach (var p in bossDirector.Boss.Panels) p.Destroyed += OnPanelDestroyed;
        }

        private void OnDisable()
        {
            HitTarget.AnyDamaged -= OnTargetDamaged;
            EnemyUnit.Killed -= OnEnemyKilled;
            if (player != null) { player.Hit -= OnPlayerHit; player.Health.Died -= OnPlayerDied; }
            if (bomb != null) bomb.BombUsed -= OnBomb;
            if (bossDirector != null && bossDirector.Boss != null)
            {
                bossDirector.Boss.StateChanged -= OnBossState;
                foreach (var p in bossDirector.Boss.Panels) p.Destroyed -= OnPanelDestroyed;
            }
        }

        private void Update()
        {
            // 화면 흔들림 끄기/켜기 (13-5, 멀미 고려)
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame)
            {
                bool next = !cameraShake.Enabled;
                ScreenShakeSetting.Set(next);
                Debug.Log($"Screen shake {(next ? "on" : "off")}");
            }
        }

        private void Play(VfxId id, Vector3 position)
        {
            if (!pool.Play(id, position)) return;
            VfxConfig.Effect e = Config.Find(id);
            cameraShake.Shake(e.shake, e.shakeDuration);
            hitStop.Freeze(e.hitStop);
        }

        private void OnTargetDamaged(HitTarget t)
        {
            if (t.IsAlive) Play(VfxId.HitSpark, t.Hitbox.Center);
        }

        private void OnEnemyKilled(EnemyUnit e) => Play(e.Config.deathVfx, e.Target.Hitbox.Center);
        private void OnPanelDestroyed(BossPart p) => Play(VfxId.ExplosionMedium, p.Target.Hitbox.Center);
        private void OnPlayerHit(string _, int __) => Play(VfxId.PlayerHit, player.transform.position);
        private void OnPlayerDied(Health _) => Play(VfxId.PlayerDeath, player.transform.position);
        private void OnBomb() => Play(VfxId.Bomb, player.transform.position);

        private void OnBossState(BossController.BossState state)
        {
            BossController boss = bossDirector.Boss;
            if (state == BossController.BossState.Transition) Play(VfxId.PhaseTransition, boss.CorePosition);
            else if (state == BossController.BossState.Defeated) Play(VfxId.BossDestroy, boss.CorePosition);
        }
    }
}
