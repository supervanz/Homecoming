using System.Collections.Generic;
using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Player;
using UnityEngine;

namespace Homecoming.Bot
{
    /// <summary>
    /// 봇 입력. 사람과 같은 IShipInputSource 경로로 이동·사격·폭탄 입력만 보낸다 (게임 상태를 바꾸는 치트 없음).
    /// 매 프레임: 9방향 후보 위치의 위험도(탄 예상 경로·레이저·적 몸체)를 내다보고, 가장 안전하면서
    /// 적과 x를 맞추는 쪽을 고른다. 위험이 기준을 넘으면 폭탄. 반응 지연·노이즈·집중력 저하로 실수한다.
    /// </summary>
    public class BotInputSource : MonoBehaviour, IShipInputSource
    {
        private static readonly Vector2[] Directions =
        {
            Vector2.zero, Vector2.right, Vector2.left, Vector2.up, Vector2.down,
            new Vector2(1, 1).normalized, new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized, new Vector2(-1, -1).normalized,
        };

        private BotSkill _skill;
        private StageContext _ctx;
        private PlayerMovement _move;
        private PlayerBomb _bomb;
        private CircleHitbox _hitbox;
        private Homecoming.Weapon.PowerUpPool _powerUps;
        private System.Random _rng;

        private readonly Queue<ShipInputFrame> _pending = new Queue<ShipInputFrame>();
        private ShipInputFrame _lastOutput;
        private float _lapseTimer;
        private float _bombCooldown;

        public BotSkill Skill => _skill;
        /// <summary>현재 위치의 위험도 (디버그 표시용)</summary>
        public float CurrentRisk { get; private set; }

        public void Init(BotSkill skill, StageContext ctx, PlayerMovement move, PlayerBomb bomb, CircleHitbox hitbox, Homecoming.Weapon.PowerUpPool powerUps, int seed)
        {
            _powerUps = powerUps;
            _skill = skill;
            _ctx = ctx;
            _move = move;
            _bomb = bomb;
            _hitbox = hitbox;
            _rng = new System.Random(seed);
            _pending.Clear();
        }

        private float Rand() => (float)_rng.NextDouble();

        public ShipInputFrame ReadInput()
        {
            float dt = Time.deltaTime;
            _bombCooldown -= dt;

            // 집중력 저하: 잠시 입력을 바꾸지 못한다 (사격은 유지)
            if (_lapseTimer > 0f)
            {
                _lapseTimer -= dt;
                return _lastOutput;
            }
            if (Rand() < _skill.lapseChancePerSecond * dt)
            {
                _lapseTimer = _skill.lapseDuration;
                return _lastOutput;
            }

            // 반응 지연: 지금 판단한 입력은 reactionFrames 뒤에 나간다
            _pending.Enqueue(Decide());
            ShipInputFrame output = _pending.Count > _skill.reactionFrames ? _pending.Dequeue() : new ShipInputFrame();
            output.Fire = true;
            _lastOutput = output;
            _lastOutput.Bomb = false;
            return output;
        }

        private ShipInputFrame Decide()
        {
            Vector3 pos = Flat(transform.position);
            float speed = _move.MaxSpeed;
            float radius = _hitbox.Radius + _skill.safetyMargin;
            PlayArea area = _ctx.PlayArea;
            float halfW = area.Size.x * 0.5f, halfH = area.Size.y * 0.5f;
            float targetX = FindTargetX(pos);
            float preferredZ = area.Center.z + _skill.preferredZ;
            bool hasItem = FindNearestPowerUp(pos, out Vector3 item);

            CurrentRisk = Risk(pos, Vector3.zero, radius);

            Vector2 best = Vector2.zero;
            float bestCost = float.MaxValue;
            foreach (Vector2 d in Directions)
            {
                Vector3 vel = new Vector3(d.x, 0f, d.y) * speed;
                float cost = Risk(pos, vel, radius);
                cost *= 1f + _skill.dodgeNoise * (Rand() * 2f - 1f);

                Vector3 end = area.Clamp(pos + vel * 0.25f, _move.EdgeMargin);
                if (hasItem)
                {
                    // 파워업이 있으면 적 정렬 대신 파워업 쪽으로 (위험도가 크면 그쪽은 여전히 피한다)
                    cost += _skill.powerUpWeight * (end - item).magnitude / halfW;
                }
                else
                {
                    cost += _skill.alignWeight * Mathf.Abs(end.x - targetX) / halfW;
                    cost += _skill.preferredZWeight * Mathf.Abs(end.z - preferredZ) / halfH;
                }

                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = d;
                }
            }

            var frame = new ShipInputFrame { Move = best, Fire = true };
            if (CurrentRisk > _skill.bombRiskThreshold && _bombCooldown <= 0f && _bomb.Count > 0 && !_bomb.IsActive)
            {
                _bombCooldown = _skill.bombCooldown;
                if (Rand() < _skill.bombChance) frame.Bomb = true;
            }
            return frame;
        }

        /// <summary>vel로 움직일 때 lookahead 동안의 위험도 합</summary>
        private float Risk(Vector3 p0, Vector3 vel, float radius)
        {
            PlayArea area = _ctx.PlayArea;
            int samples = Mathf.Max(1, _skill.lookaheadSamples);
            var bullets = _ctx.EnemyBullets.Active;
            var targets = HitTargetRegistry.Active;
            float risk = 0f;

            for (int k = 0; k <= samples; k++)
            {
                float t = _skill.lookahead * k / samples;
                float w = 1f - 0.5f * k / samples; // 가까운 미래일수록 중요
                Vector3 p = area.Clamp(p0 + vel * t, _move.EdgeMargin);

                for (int i = 0; i < bullets.Count; i++)
                {
                    var b = bullets[i];
                    Vector3 bp = Flat(b.Position + b.Velocity * t);
                    float r = radius + b.Radius;
                    float d = (bp - p).magnitude;
                    float reach = r * 2f;
                    if (d >= reach) continue;
                    float x = 1f - d / reach;
                    risk += w * x * x * (d < r ? 4f : 1f);
                }

                foreach (var laser in BossLaser.All)
                    if (laser.IsDangerous && laser.Covers(p, radius)) risk += w * 3f;

                for (int i = 0; i < targets.Count; i++)
                {
                    var tg = targets[i];
                    if (tg.ContactDamage <= 0) continue;
                    if (tg.Hitbox.Overlaps(p, radius)) risk += w * 2f;
                }
            }
            return risk;
        }

        /// <summary>플레이 영역 안의 가장 가까운 파워업</summary>
        private bool FindNearestPowerUp(Vector3 pos, out Vector3 nearest)
        {
            nearest = Vector3.zero;
            if (_powerUps == null || _skill.powerUpWeight <= 0f) return false;
            var items = _powerUps.Active;
            float bestD = float.MaxValue;
            for (int i = 0; i < items.Count; i++)
            {
                Vector3 p = Flat(items[i].Position);
                if (!_ctx.PlayArea.Contains(p, 0f)) continue;
                float d = (p - pos).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    nearest = p;
                }
            }
            return bestD < float.MaxValue;
        }

        /// <summary>앞쪽(위)에 있는 가장 가까운 적의 x. 없으면 화면 중앙.</summary>
        private float FindTargetX(Vector3 pos)
        {
            var targets = HitTargetRegistry.Active;
            float bestX = _ctx.PlayArea.Center.x;
            float bestD = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 c = targets[i].Hitbox.Center;
                if (c.z <= pos.z) continue;
                if (!_ctx.PlayArea.Contains(c, 0f)) continue;
                float d = (c - pos).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    bestX = c.x;
                }
            }
            return bestX;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
