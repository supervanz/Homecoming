namespace Homecoming.Ending
{
    /// <summary>
    /// 엔딩 시퀀스의 연출 단위 (CLAUDE.md 15-1). Timeline의 EndingCue 트랙에 클립으로 놓고,
    /// 클립의 시작·길이로 타이밍을 정한다.
    /// </summary>
    public enum EndingCue
    {
        /// <summary>2. 정적: 조작 종료, 기체를 화면 아래 중앙으로, 낮은 드론</summary>
        Silence,
        /// <summary>3. 대기권 돌입: 기체 주변 열기·불꽃·연기, 화면 진동</summary>
        Heat,
        /// <summary>4. 하늘로 전환: 검은 우주 → 파란 하늘 (배경·포스트·조명·지구·별)</summary>
        SkyBlend,
        /// <summary>5. 귀환: 기체가 화면 위로 가속하며 사라짐</summary>
        FlyAway,
        /// <summary>6. 구름 통과: 구름이 화면을 빠르게 지나감</summary>
        Clouds,
        /// <summary>7. "WELCOME HOME" 텍스트가 나타남</summary>
        Welcome,
        /// <summary>8. 화면 암전 (텍스트는 남음)</summary>
        FadeOut,
        /// <summary>8. 남은 텍스트가 사라짐</summary>
        TextFade,
        /// <summary>효과음 한 번 (클립 시작 시)</summary>
        Sound,
        /// <summary>배경 음악 전환 (클립 시작 시)</summary>
        Music,
    }
}
