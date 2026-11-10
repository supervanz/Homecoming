namespace Homecoming.Vfx
{
    /// <summary>
    /// 일회성 이펙트 종류. 강도 위계 (CLAUDE.md 13-3) 순서대로 나열한다.
    /// 실제 프리팹·흔들림 세기는 VfxConfig에 있다.
    /// </summary>
    public enum VfxId
    {
        None = 0,
        HitSpark,          // 1. 피탄
        ExplosionSmall,    // 2. 소형 폭발 (잡몹)
        ExplosionMedium,   // 3. 중형 폭발 (중형 병기, 부위 파괴)
        PlayerHit,         // 4. 플레이어 피격
        PhaseTransition,   // 5. 보스 페이즈 전환 (외피 탈락)
        Bomb,              // 6. 폭탄
        BossDestroy,       // 7. 보스 격파
        PlayerDeath,       // 플레이어 기체 폭발 (게임오버)
    }

    /// <summary>손상 상태 표현의 진영 (13-4-1). 인간 = 주황 불꽃, AI = 흰·청색 스파크 + 붉은 불꽃</summary>
    public enum VfxFaction { Ai, Human }
}
