using Homecoming.Audio;
using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Vfx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Homecoming.Ending
{
    /// <summary>
    /// 엔딩 연출을 실제로 적용한다 (CLAUDE.md 15장). 언제 무엇을 할지는 Timeline(EndingCueTrack)이 정하고,
    /// 여기서는 각 연출이 화면·소리에 어떻게 보이는지만 다룬다.
    /// 색 전환이 이야기의 결론이다: 검은 우주(추방) → 파란 하늘(귀향).
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField] private WaveSpawner spawner;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] private StageContext context;

        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private PlayerMovement movement;
        [Tooltip("정적 구간에 기체가 모일 위치 (화면 아래 중앙)")]
        [SerializeField] private Vector3 holdPosition = new Vector3(0f, 0f, -4f);
        [Tooltip("귀환: 화면 위로 날아가는 거리")]
        [SerializeField] private float flyDistance = 40f;
        [Tooltip("귀환: 날아가며 올라가는 높이 (연출용)")]
        [SerializeField] private float flyRise = 4f;

        [Header("Re-entry heat")]
        [SerializeField] private ParticleSystem heat;
        [Tooltip("대기권 돌입 중 화면 진동 세기")]
        [SerializeField] private float heatShake = 0.25f;

        [Header("Clouds")]
        [SerializeField] private ParticleSystem clouds;

        [Header("Sky blend")]
        [SerializeField] private Camera cam;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private Color skyColor = new Color(0.36f, 0.62f, 0.95f);
        [Tooltip("하늘 쪽 포스트프로세싱 (가중치 0 → 1)")]
        [SerializeField] private Volume skyVolume;
        [SerializeField] private Light keyLight;
        [SerializeField] private Color skyLightColor = new Color(1f, 0.93f, 0.8f);
        [SerializeField] private float skyLightIntensity = 2.6f;
        [SerializeField] private Color skyAmbient = new Color(0.55f, 0.68f, 0.85f);
        [SerializeField] private BackgroundScroller stars;
        [SerializeField] private EarthApproach earth;

        [Header("UI")]
        [SerializeField] private UIDocument hud;
        [SerializeField] private UIDocument endingUi;

        private Color _spaceColor, _spaceLightColor, _spaceSky, _spaceEquator, _spaceGround;
        private float _spaceLightIntensity;
        private Vector3 _holdFrom, _flyFrom;
        private ParticleSystem[] _heatLayers, _cloudLayers;
        private float[] _heatRates, _cloudRates;
        private VisualElement _fade;
        private Label _welcome;

        public bool Prepared { get; private set; }

        private void Awake()
        {
            CacheRates(heat, out _heatLayers, out _heatRates);
            CacheRates(clouds, out _cloudLayers, out _cloudRates);
            SetRate(_heatLayers, _heatRates, 0f);
            SetRate(_cloudLayers, _cloudRates, 0f);
        }

        private void OnEnable()
        {
            VisualElement root = endingUi.rootVisualElement;
            _fade = root.Q("ending-fade");
            _welcome = root.Q<Label>("ending-welcome");
            _fade.style.opacity = 0f;
            _welcome.style.opacity = 0f;
        }

        /// <summary>엔딩 시작 전 정리: 남은 적·탄 정리, 스폰 중지, 조작 종료, HUD 숨김.</summary>
        public void Prepare()
        {
            Prepared = true;
            spawner.Halt();
            bossDirector.enabled = false;
            if (bossDirector.Boss.gameObject.activeSelf) bossDirector.Boss.gameObject.SetActive(false); // F3 디버그 재생 시
            for (int i = EnemyRegistry.Active.Count - 1; i >= 0; i--) EnemyRegistry.Active[i].Retire();
            context.EnemyBullets.ReleaseAll();
            movement.enabled = false;
            hud.rootVisualElement.style.display = DisplayStyle.None;

            _spaceColor = cam.backgroundColor;
            _spaceLightColor = keyLight.color;
            _spaceLightIntensity = keyLight.intensity;
            _spaceSky = RenderSettings.ambientSkyColor;
            _spaceEquator = RenderSettings.ambientEquatorColor;
            _spaceGround = RenderSettings.ambientGroundColor;
            if (earth != null) earth.Freeze();
        }

        public void Begin(EndingCueClip c)
        {
            switch (c.cue)
            {
                case EndingCue.Silence:
                    _holdFrom = player.position;
                    if (MusicPlayer.Instance != null) MusicPlayer.Instance.Stop();
                    if (SfxPlayer.Instance != null) SfxPlayer.Instance.StartLoop(SoundId.WarningDrone);
                    break;
                case EndingCue.Heat:
                    heat.Play(true);
                    break;
                case EndingCue.FlyAway:
                    _flyFrom = player.position;
                    break;
                case EndingCue.Clouds:
                    clouds.Play(true);
                    break;
                case EndingCue.Sound:
                    if (SfxPlayer.Instance != null) SfxPlayer.Instance.Play(c.sound);
                    break;
                case EndingCue.Music:
                    if (MusicPlayer.Instance != null) MusicPlayer.Instance.Play(c.music);
                    break;
            }
        }

        public void Apply(EndingCueClip c, float p)
        {
            switch (c.cue)
            {
                case EndingCue.Silence:
                    player.position = Vector3.Lerp(_holdFrom, holdPosition, p);
                    break;
                case EndingCue.Heat:
                {
                    float env = Envelope(p, 0.2f, 0.8f);
                    SetRate(_heatLayers, _heatRates, env);
                    if (cameraShake != null) cameraShake.Shake(heatShake * env, 0.15f);
                    break;
                }
                case EndingCue.SkyBlend:
                    ApplySky(p);
                    break;
                case EndingCue.FlyAway:
                    player.position = _flyFrom + new Vector3(0f, flyRise * p, flyDistance * p);
                    break;
                case EndingCue.Clouds:
                    SetRate(_cloudLayers, _cloudRates, Envelope(p, 0.15f, 0.7f));
                    break;
                case EndingCue.Welcome:
                    _welcome.style.opacity = p;
                    break;
                case EndingCue.FadeOut:
                    _fade.style.opacity = p;
                    break;
                case EndingCue.TextFade:
                    _welcome.style.opacity = 1f - p;
                    break;
            }
        }

        public void End(EndingCueClip c)
        {
            switch (c.cue)
            {
                case EndingCue.Silence:
                    if (SfxPlayer.Instance != null) SfxPlayer.Instance.StopLoop(SoundId.WarningDrone);
                    break;
                case EndingCue.Heat:
                    heat.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    break;
                case EndingCue.FlyAway:
                    player.gameObject.SetActive(false);
                    break;
                case EndingCue.Clouds:
                    clouds.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    break;
            }
        }

        private void ApplySky(float p)
        {
            cam.backgroundColor = Color.Lerp(_spaceColor, skyColor, p);
            if (skyVolume != null) skyVolume.weight = p;
            keyLight.color = Color.Lerp(_spaceLightColor, skyLightColor, p);
            keyLight.intensity = Mathf.Lerp(_spaceLightIntensity, skyLightIntensity, p);
            RenderSettings.ambientSkyColor = Color.Lerp(_spaceSky, skyAmbient, p);
            RenderSettings.ambientEquatorColor = Color.Lerp(_spaceEquator, skyAmbient * 0.8f, p);
            RenderSettings.ambientGroundColor = Color.Lerp(_spaceGround, skyAmbient * 0.6f, p);
            if (stars != null) stars.SetVisibility(1f - p);
            if (earth != null) earth.SetExit(p);
        }

        // 0→rampIn 동안 0→1, holdUntil 이후 1→0
        private static float Envelope(float p, float rampIn, float holdUntil) =>
            Mathf.SmoothStep(0f, 1f, p / rampIn) * (1f - Mathf.SmoothStep(0f, 1f, (p - holdUntil) / (1f - holdUntil)));

        private static void CacheRates(ParticleSystem root, out ParticleSystem[] layers, out float[] rates)
        {
            layers = root != null ? root.GetComponentsInChildren<ParticleSystem>(true) : new ParticleSystem[0];
            rates = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++) rates[i] = layers[i].emission.rateOverTimeMultiplier;
        }

        private static void SetRate(ParticleSystem[] layers, float[] rates, float k)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                var em = layers[i].emission;
                em.rateOverTimeMultiplier = rates[i] * k;
            }
        }
    }
}
