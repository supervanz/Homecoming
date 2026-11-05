using UnityEngine;

namespace Homecoming.Bot
{
    /// <summary>
    /// 봇 실력 단계 (초보 / 보통 / 숙련). 완벽한 봇은 밸런스 기준이 될 수 없으므로
    /// 반응 지연, 회피 정확도, 폭탄 판단, 집중력 저하로 사람처럼 실수하게 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Homecoming/Bot Skill", fileName = "BotSkill")]
    public class BotSkill : ScriptableObject
    {
        [Header("Target (CLAUDE.md 11-5)")]
        [Tooltip("이 실력 단계가 목표로 하는 클리어율 범위")]
        [Range(0f, 1f)] public float targetClearRateMin = 0.4f;
        [Range(0f, 1f)] public float targetClearRateMax = 0.6f;

        [Header("Reaction")]
        [Tooltip("판단한 입력이 실제로 나가기까지의 지연 (프레임, 60fps 기준)")]
        [Min(0)] public int reactionFrames = 10;

        [Header("Dodge accuracy")]
        [Tooltip("탄 경로를 미리 내다보는 시간 (초)")]
        public float lookahead = 0.5f;
        [Tooltip("내다보는 시간을 나누는 샘플 수")]
        [Min(1)] public int lookaheadSamples = 6;
        [Tooltip("탄과 유지하려는 여유 거리")]
        public float safetyMargin = 0.25f;
        [Tooltip("위험도 판단에 섞이는 노이즈 (클수록 잘못 판단)")]
        public float dodgeNoise = 0.3f;

        [Header("Attention lapses")]
        [Tooltip("초당 집중력이 끊길 확률")]
        [Range(0f, 1f)] public float lapseChancePerSecond = 0.06f;
        [Tooltip("집중력이 끊기면 입력을 바꾸지 못하는 시간 (초)")]
        public float lapseDuration = 0.3f;

        [Header("Bomb judgement")]
        [Tooltip("현재 위치 위험도가 이 값을 넘으면 폭탄을 고려한다")]
        public float bombRiskThreshold = 1.8f;
        [Tooltip("기준을 넘었을 때 실제로 폭탄을 쓸 확률 (판단마다)")]
        [Range(0f, 1f)] public float bombChance = 0.6f;
        [Tooltip("폭탄을 쓴 뒤 다시 고려하기까지 (초)")]
        public float bombCooldown = 2f;

        [Header("Power-ups")]
        [Tooltip("화면에 파워업이 있으면 그쪽으로 가려는 정도. 위험도가 우선이라 위험한 곳의 파워업은 쫓지 않는다")]
        public float powerUpWeight = 0.6f;

        [Header("Attack")]
        [Tooltip("적과 x를 맞추려는 정도")]
        public float alignWeight = 0.6f;
        [Tooltip("머물고 싶은 세로 위치 (플레이 영역 중심 기준, 아래가 -)")]
        public float preferredZ = -5.5f;
        [Tooltip("선호 세로 위치를 지키려는 정도")]
        public float preferredZWeight = 0.15f;
    }
}
