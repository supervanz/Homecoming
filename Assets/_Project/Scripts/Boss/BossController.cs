using System;
using System.Collections;
using System.Collections.Generic;
using Homecoming.Core;
using Homecoming.Enemy;
using UnityEngine;

namespace Homecoming.Boss
{
    /// <summary>
    /// 보스 진행 관리: 등장 → 1페이즈(외피) → 전환(외피 탈락) → 2페이즈(코어) → 발악 → 격파.
    /// 각 페이즈에서는 BossConfig의 행동 목록을 순서대로 반복한다.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        public enum BossState { Inactive, Entering, Phase1, Transition, Phase2, Desperation, Defeated }

        [SerializeField] private BossConfig config;
        [Tooltip("본체 판정 (보스 루트)")]
        [SerializeField] private HitTarget core;
        [Tooltip("태양전지판 포대들")]
        [SerializeField] private BossPart[] panels = new BossPart[0];
        [Tooltip("2페이즈 진입 시 떨어져 나갈 외피")]
        [SerializeField] private Transform shell;
        [Tooltip("코어 탄·레이저 발사 위치")]
        [SerializeField] private Transform coreMuzzle;
        [SerializeField] private BossLaser laser;

        private readonly List<Coroutine> _parallel = new List<Coroutine>();
        private Coroutine _main;
        private Coroutine _loop;
        private Vector3 _hoverCenter;
        private float _swayTime;

        public event Action Defeated;

        public BossState State { get; private set; } = BossState.Inactive;
        public BossConfig Config => config;
        public StageContext Context { get; private set; }
        public EnemyPool EnemyPool { get; private set; }
        public BossLaser Laser => laser;
        public Transform CoreMuzzle => coreMuzzle;
        public HitTarget Core => core;
        public IReadOnlyList<BossPart> Panels => panels;
        public Vector3 CorePosition => Flat(coreMuzzle.position);
        public float HpRatio => core.Health.Max > 0 ? core.Health.Current / (float)core.Health.Max : 0f;

        private void Awake()
        {
            core.Health.Died += _ => OnDefeated();
        }

        public void Begin(StageContext ctx, EnemyPool pool)
        {
            Context = ctx;
            EnemyPool = pool;

            Vector3 c = ctx.PlayArea.Center;
            transform.position = c + new Vector3(config.entryFrom.x, 0f, config.entryFrom.y);
            _hoverCenter = c + new Vector3(config.hoverPosition.x, 0f, config.hoverPosition.y);
            _swayTime = 0f;

            core.Health.ResetHealth(config.coreHp);
            core.BombDamageScale = config.bombDamageScale;
            core.ContactDamage = config.contactDamage;
            core.Hitbox.Radius = config.shellRadius;
            core.Invulnerable = true;
            core.Register();
            foreach (var p in panels) p.Activate(config.panelHp, config.bombDamageScale, config.contactDamage);
            shell.gameObject.SetActive(true);

            _main = StartCoroutine(Enter());
        }

        private IEnumerator Enter()
        {
            State = BossState.Entering;
            while ((transform.position - _hoverCenter).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, _hoverCenter, config.entrySpeed * Time.deltaTime);
                yield return null;
            }
            core.Invulnerable = false;
            State = BossState.Phase1;
            StartLoop(config.phase1Actions);
        }

        private void Update()
        {
            if (State == BossState.Inactive || State == BossState.Entering || State == BossState.Defeated) return;

            _swayTime += Time.deltaTime;
            Vector3 p = _hoverCenter;
            p.x += Mathf.Sin(_swayTime * Mathf.PI * 2f / config.swayPeriod) * config.swayAmplitude;
            transform.position = p;

            if (State == BossState.Phase1 && HpRatio <= config.phase2Threshold)
            {
                StopLoop();
                _main = StartCoroutine(Transition());
            }
            else if (State == BossState.Phase2 && HpRatio <= config.desperationThreshold)
            {
                StopLoop();
                State = BossState.Desperation;
                StartLoop(config.desperationActions);
            }
        }

        private IEnumerator Transition()
        {
            State = BossState.Transition;
            core.Invulnerable = true;
            Context.EnemyBullets.ReleaseAll();

            var debris = new List<Transform> { shell };
            foreach (var p in panels)
            {
                if (p.IsDestroyed) continue;
                p.Detach();
                debris.Add(p.Visual.transform);
            }

            for (float t = 0f; t < config.transitionDuration; t += Time.deltaTime)
            {
                float dt = Time.deltaTime;
                foreach (var d in debris)
                {
                    d.position += new Vector3(0f, -2f, -config.debrisFallSpeed) * dt;
                    d.Rotate(40f * dt, 0f, 60f * dt, Space.Self);
                }
                yield return null;
            }
            foreach (var d in debris) d.gameObject.SetActive(false);

            core.Hitbox.Radius = config.coreRadius;
            core.Invulnerable = false;
            State = BossState.Phase2;
            StartLoop(config.phase2Actions);
        }

        private void StartLoop(BossActionConfig[] actions)
        {
            _loop = StartCoroutine(ActionLoop(actions));
        }

        private IEnumerator ActionLoop(BossActionConfig[] actions)
        {
            if (actions == null || actions.Length == 0) yield break;
            int i = 0;
            while (true)
            {
                BossActionConfig a = actions[i % actions.Length];
                i++;
                if (a == null)
                {
                    yield return null;
                    continue;
                }
                if (a.runInParallel)
                {
                    _parallel.Add(StartCoroutine(a.Run(this)));
                    yield return null; // 병렬 행동만 있는 목록에서 무한 루프 방지
                }
                else
                {
                    // IEnumerator를 직접 yield해야 StopCoroutine(_loop) 시 함께 멈춘다.
                    yield return a.Run(this);
                    yield return new WaitForSeconds(config.actionGap);
                }
            }
        }

        private void StopLoop()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            foreach (var c in _parallel)
                if (c != null) StopCoroutine(c);
            _parallel.Clear();
            laser.Stop();
        }

        /// <summary>발사 지점 목록을 채운다. 부서진 포대는 빠진다.</summary>
        public void GetEmitterPositions(BossEmitter emitter, List<Vector3> result)
        {
            result.Clear();
            if (emitter == BossEmitter.Core || emitter == BossEmitter.CoreAndPanels)
                result.Add(CorePosition);
            if (emitter == BossEmitter.Panels || emitter == BossEmitter.CoreAndPanels)
                foreach (var p in panels)
                    if (!p.IsDestroyed) result.Add(Flat(p.MuzzlePosition));
        }

        private void OnDefeated()
        {
            if (State == BossState.Defeated || State == BossState.Inactive) return;
            State = BossState.Defeated;
            StopLoop();
            if (_main != null) StopCoroutine(_main);
            core.Unregister();
            foreach (var p in panels) p.Detach();
            Context.EnemyBullets.ReleaseAll();
            // 격파 연출(폭발·히트스톱)은 M8, 엔딩 시퀀스는 M9에서 연결한다.
            Debug.Log("Boss defeated");
            gameObject.SetActive(false);
            Defeated?.Invoke();
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
