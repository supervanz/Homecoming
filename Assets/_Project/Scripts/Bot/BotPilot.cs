using Homecoming.Core;
using Homecoming.Player;
using UnityEngine;

namespace Homecoming.Bot
{
    /// <summary>
    /// 봇에게 기체를 맡긴다. Inspector에서 Enable Bot을 켜면 사람 입력 대신 봇 입력이 들어간다.
    /// 시뮬레이션은 ForcedSkill로 씬 설정과 관계없이 봇을 켠다.
    /// </summary>
    public class BotPilot : MonoBehaviour
    {
        [SerializeField] private bool enableBot;
        [SerializeField] private BotSkill skill;
        [SerializeField] private ShipInput input;
        [SerializeField] private StageContext context;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerBomb bomb;
        [SerializeField] private CircleHitbox playerHitbox;
        [SerializeField] private Homecoming.Weapon.PowerUpPool powerUps;

        /// <summary>시뮬레이션이 지정하는 실력 단계. 있으면 Enable Bot과 관계없이 봇을 켠다.</summary>
        public static BotSkill ForcedSkill { get; set; }

        public BotInputSource Bot { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => ForcedSkill = null;

        private void Start()
        {
            BotSkill s = ForcedSkill != null ? ForcedSkill : (enableBot ? skill : null);
            if (s == null) return;

            Bot = input.gameObject.AddComponent<BotInputSource>();
            Bot.Init(s, context, movement, bomb, playerHitbox, powerUps, StableSeed(GameRandom.Seed, s.name));
            input.SetSource(Bot);
            Debug.Log($"[BotPilot] bot enabled: {s.name}");
        }

        // 실행 환경과 관계없이 같은 값을 주는 해시 (string.GetHashCode는 환경마다 다를 수 있음)
        private static int StableSeed(int seed, string name)
        {
            unchecked
            {
                int h = seed * 31 + 17;
                foreach (char c in name) h = h * 31 + c;
                return h;
            }
        }
    }
}
