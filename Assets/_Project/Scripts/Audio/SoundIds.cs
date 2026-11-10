namespace Homecoming.Audio
{
    /// <summary>효과음 종류 (CLAUDE.md 14-3). 실제 클립·볼륨은 SoundConfig에 있다.</summary>
    public enum SoundId
    {
        None = 0,
        // Player
        PlayerFireLv1, PlayerFireLv2, PlayerFireLv3,
        PowerUpPickup, WeaponLevelUp, Bomb, PlayerHit, LowHpWarning, PlayerDeath,
        // Enemy
        EnemyHit, EnemyFire, ExplosionSmall, ExplosionMedium, DamageSparks,
        // Boss
        BossWarningAlarm, WarningDrone, LaserCharge, LaserFire, BossDroneSummon, BossPhaseTransition, BossDestroy,
        // UI / flow
        UiMove, UiSelect, GameStart, StageClear,
        // Ending (M9)
        ReentryRoar, WindRush, EngineFlyaway,
    }

    /// <summary>배경 음악 (CLAUDE.md 14-2)</summary>
    public enum MusicTrack
    {
        None = 0,
        Title, Stage, BossPhase1, BossPhase2, Ending, GameOver,
    }

    public enum SoundGroup { Sfx, Ui }
}
