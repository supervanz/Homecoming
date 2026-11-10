using System.IO;
using System.Linq;
using Homecoming.Audio;
using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Ending;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Vfx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// M9 생성기: 지구 텍스처·머티리얼, 포스트프로세싱 프로필, 엔딩 Timeline, Stage01 씬 연결
    /// (카메라·라이팅·포스트·지구·엔딩). 다시 실행해도 같은 결과가 된다.
    /// </summary>
    public static class EndingBuilder
    {
        private const string ArtDir = "Assets/_Project/Art";
        private const string PostDir = ArtDir + "/PostProcess";
        private const string TimelinePath = "Assets/_Project/Data/Ending/Ending.playable";
        private const string ScenePath = "Assets/_Project/Scenes/Stage01.unity";

        [MenuItem("Homecoming/Build Ending (M9)")]
        public static string Build()
        {
            Directory.CreateDirectory(PostDir);
            Directory.CreateDirectory(Path.GetDirectoryName(TimelinePath));

            BuildEarthAssets();
            BuildProfiles();
            BuildTimeline();
            AssetDatabase.SaveAssets();
            return WireScene();
        }

        // ---------- 지구 ----------

        private static void BuildEarthAssets()
        {
            string texPath = ArtDir + "/Textures/T_Earth.png";
            File.WriteAllBytes(texPath, EarthTexture(512, 256).EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
            imp.wrapModeU = TextureWrapMode.Repeat;
            imp.wrapModeV = TextureWrapMode.Clamp;
            imp.SaveAndReimport();

            Material earth = LoadOrCreate(ArtDir + "/Materials/M_Earth.mat", "Universal Render Pipeline/Lit");
            earth.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            earth.SetColor("_BaseColor", Color.white);
            earth.SetFloat("_Smoothness", 0.35f);
            earth.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(earth);

            Material atmo = LoadOrCreate(ArtDir + "/Materials/M_Atmosphere.mat", "Homecoming/Atmosphere");
            atmo.SetColor("_Color", new Color(0.35f, 0.65f, 1f));
            atmo.SetFloat("_Power", 2.5f);
            atmo.SetFloat("_Intensity", 1.6f);
            EditorUtility.SetDirty(atmo);
        }

        // 구면 좌표에서 3방향 펄린 노이즈를 섞어 경도 이음매 없이 대륙·구름을 만든다 (외부 에셋 없이 절차적으로).
        private static Texture2D EarthTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color deep = new Color(0.02f, 0.07f, 0.22f), shallow = new Color(0.05f, 0.2f, 0.42f);
            Color green = new Color(0.13f, 0.28f, 0.1f), desert = new Color(0.46f, 0.38f, 0.22f), ice = new Color(0.92f, 0.95f, 1f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float lon = (x + 0.5f) / w * Mathf.PI * 2f;
                float lat = ((y + 0.5f) / h - 0.5f) * Mathf.PI;
                Vector3 p = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                float land = Fbm(p, 1.6f, 0f);
                float dry = Fbm(p, 3f, 50f);
                float cloud = Fbm(p, 4f, 100f);
                Color c = land > 0.52f
                    ? Color.Lerp(green, desert, Smooth(0.45f, 0.6f, dry))
                    : Color.Lerp(deep, shallow, Smooth(0.4f, 0.52f, land));
                float polar = Mathf.Abs(p.y);
                c = Color.Lerp(c, ice, Smooth(0.9f, 0.96f, polar));
                c = Color.Lerp(c, Color.white, Smooth(0.5f, 0.66f, cloud) * 0.8f);
                tex.SetPixel(x, y, c);
            }
            return tex;
        }

        // GLSL smoothstep(edge0, edge1, x). Mathf.SmoothStep(a, b, t)는 a~b 사이 보간이라 다르다.
        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static float Fbm(Vector3 p, float freq, float offset)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < 4; o++)
            {
                Vector3 q = p * freq + Vector3.one * (offset + 10f);
                float n = (Mathf.PerlinNoise(q.x, q.y) + Mathf.PerlinNoise(q.y + 31f, q.z) + Mathf.PerlinNoise(q.z + 67f, q.x)) / 3f;
                sum += n * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / norm;
        }

        // ---------- 포스트프로세싱 (3장: Bloom 약하게, Tonemapping, Vignette) ----------

        private static void BuildProfiles()
        {
            VolumeProfile space = Profile(PostDir + "/VP_Space.asset");
            var bloom = Add<Bloom>(space);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.65f);
            Add<Tonemapping>(space).mode.Override(TonemappingMode.ACES);
            var vig = Add<Vignette>(space);
            vig.intensity.Override(0.3f);
            vig.smoothness.Override(0.45f);
            var ca = Add<ColorAdjustments>(space);
            ca.contrast.Override(12f);
            ca.saturation.Override(5f);
            Add<WhiteBalance>(space).temperature.Override(-8f);

            // 엔딩 하늘: 밝고 따뜻하게. Sky Volume의 weight로 0 → 1 섞는다.
            VolumeProfile sky = Profile(PostDir + "/VP_Sky.asset");
            var sb = Add<Bloom>(sky);
            sb.threshold.Override(0.9f);
            sb.intensity.Override(0.9f);
            var sv = Add<Vignette>(sky);
            sv.intensity.Override(0.12f);
            var sc = Add<ColorAdjustments>(sky);
            sc.postExposure.Override(0.4f);
            sc.contrast.Override(0f);
            sc.saturation.Override(10f);
            Add<WhiteBalance>(sky).temperature.Override(12f);
        }

        private static VolumeProfile Profile(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(p, path);
            }
            foreach (var c in p.components.ToList()) Object.DestroyImmediate(c, true);
            p.components.Clear();
            EditorUtility.SetDirty(p);
            return p;
        }

        private static T Add<T>(VolumeProfile p) where T : VolumeComponent
        {
            T c = p.Add<T>();
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, p);
            return c;
        }

        // ---------- 엔딩 Timeline (15-1, 20~30초) ----------

        private static void BuildTimeline()
        {
            var tl = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            if (tl == null)
            {
                tl = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(tl, TimelinePath);
            }
            foreach (var t in tl.GetRootTracks().ToList()) tl.DeleteTrack(t);

            AnimationCurve smooth = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            AnimationCurve linear = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            AnimationCurve accel = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));

            var player = tl.CreateTrack<EndingCueTrack>(null, "Player");
            Clip(player, "2 Silence", EndingCue.Silence, 0f, 3f, smooth);
            Clip(player, "5 Fly away", EndingCue.FlyAway, 12f, 3f, accel);

            var fx = tl.CreateTrack<EndingCueTrack>(null, "Effects");
            Clip(fx, "3 Re-entry heat", EndingCue.Heat, 2.8f, 7.2f, linear);
            Clip(fx, "6 Clouds", EndingCue.Clouds, 14.5f, 4f, linear);

            var sky = tl.CreateTrack<EndingCueTrack>(null, "Sky");
            Clip(sky, "4 Space to sky", EndingCue.SkyBlend, 6f, 8f, smooth);

            var text = tl.CreateTrack<EndingCueTrack>(null, "Text");
            Clip(text, "7 WELCOME HOME", EndingCue.Welcome, 18.3f, 1.5f, smooth);
            Clip(text, "8 Fade to black", EndingCue.FadeOut, 22.5f, 2.5f, smooth);
            Clip(text, "8 Text fades", EndingCue.TextFade, 25f, 1.5f, smooth);

            var audio = tl.CreateTrack<EndingCueTrack>(null, "Audio");
            Clip(audio, "Re-entry roar", EndingCue.Sound, 2.6f, 0.5f, linear).sound = SoundId.ReentryRoar;
            Clip(audio, "Ending music", EndingCue.Music, 3.2f, 0.5f, linear).music = MusicTrack.Ending;
            Clip(audio, "Engine fly-away", EndingCue.Sound, 12f, 0.5f, linear).sound = SoundId.EngineFlyaway;
            Clip(audio, "Wind rush", EndingCue.Sound, 14.5f, 0.5f, linear).sound = SoundId.WindRush;

            // 9. 결과: 텍스트가 사라지고 잠깐 뒤 결과창
            tl.durationMode = TimelineAsset.DurationMode.FixedLength;
            tl.fixedDuration = 27.0;
            EditorUtility.SetDirty(tl);
        }

        private static EndingCueClip Clip(TrackAsset track, string name, EndingCue cue, float start, float duration, AnimationCurve curve)
        {
            TimelineClip c = track.CreateClip<EndingCueClip>();
            c.start = start;
            c.duration = duration;
            c.displayName = name;
            var a = (EndingCueClip)c.asset;
            a.cue = cue;
            a.curve = new AnimationCurve(curve.keys);
            EditorUtility.SetDirty(a);
            return a;
        }

        // ---------- 씬 ----------

        private static string WireScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 씬을 연 다음 에셋을 불러온다 (참조가 null로 저장되는 문제 방지)
            var space = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostDir + "/VP_Space.asset");
            var skyProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostDir + "/VP_Sky.asset");
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            var earthMat = AssetDatabase.LoadAssetAtPath<Material>(ArtDir + "/Materials/M_Earth.mat");
            var atmoMat = AssetDatabase.LoadAssetAtPath<Material>(ArtDir + "/Materials/M_Atmosphere.mat");
            var heatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/VFX/Prefabs/Vfx_ReentryHeat.prefab");
            var cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/VFX/Prefabs/Vfx_Clouds.prefab");
            var endingUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/Ending.uxml");
            Mesh sphere = SphereMesh(ArtDir + "/Models/Mesh_Sphere64.asset", 64, 32);

            // 카메라: 안티에일리어싱
            Camera cam = Camera.main;
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.Medium;
            camData.renderPostProcessing = true;
            EditorUtility.SetDirty(camData);

            // 포스트프로세싱
            Volume global = Object.FindObjectsByType<Volume>().First(v => v.name == "Global Volume");
            global.sharedProfile = space;
            EditorUtility.SetDirty(global);
            GameObject skyGo = Find("Sky Volume") ?? new GameObject("Sky Volume");
            Volume sky = Get<Volume>(skyGo);
            sky.isGlobal = true;
            sky.priority = 1f;
            sky.weight = 0f;
            sky.sharedProfile = skyProfile;

            // 라이팅: 차가운 키 라이트 + 지구 쪽 푸른 보조광 + 어두운 우주 환경광
            Light key = Object.FindObjectsByType<Light>().First(l => l.name == "Directional Light");
            key.color = new Color(0.86f, 0.92f, 1f);
            key.intensity = 2.2f;
            EditorUtility.SetDirty(key);
            GameObject fillGo = Find("Fill Light") ?? new GameObject("Fill Light");
            Light fill = Get<Light>(fillGo);
            fill.type = LightType.Directional;
            fill.color = new Color(0.4f, 0.55f, 0.9f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.07f, 0.08f, 0.11f);
            RenderSettings.ambientEquatorColor = new Color(0.045f, 0.05f, 0.065f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.07f, 0.12f);

            // 지구
            var spawner = Object.FindAnyObjectByType<WaveSpawner>();
            GameObject earthGo = Find("Earth") ?? new GameObject("Earth");
            // 자전축을 카메라 위쪽 방향(59°)으로 눕혀 극이 아닌 적도 쪽이 보이게, 23° 기울기
            earthGo.transform.rotation = Quaternion.Euler(59f, 0f, 23f);
            Renderer surface = Child(earthGo.transform, "Surface", sphere, earthMat, 1f);
            Renderer atmo = Child(earthGo.transform, "Atmosphere", sphere, atmoMat, 1.06f);
            _ = surface;
            var earth = Get<EarthApproach>(earthGo);
            Set(earth, "spawner", spawner);
            Set(earth, "atmosphere", atmo);
            // 카메라 시선 축 위 먼 곳에 두어, 플레이 영역 아래에서 원반처럼 점점 커지게 한다
            var eso = new SerializedObject(earth);
            eso.FindProperty("farPosition").vector3Value = new Vector3(0f, -401f, 98f);
            eso.FindProperty("nearPosition").vector3Value = new Vector3(0f, -250f, 78f);
            eso.FindProperty("farScale").floatValue = 70f;
            eso.FindProperty("nearScale").floatValue = 140f;
            eso.FindProperty("exitOffset").vector3Value = new Vector3(0f, -128f, -215f);
            eso.ApplyModifiedPropertiesWithoutUndo();

            // 플레이어 열기, 구름
            var receiver = Object.FindAnyObjectByType<PlayerDamageReceiver>(FindObjectsInactive.Include);
            Transform playerT = receiver.transform;
            ParticleSystem heat = Instance(heatPrefab, "ReentryHeat", playerT, new Vector3(0f, 0.3f, 0.3f), Quaternion.identity).GetComponent<ParticleSystem>();
            ParticleSystem clouds = Instance(cloudPrefab, "Clouds", null, new Vector3(0f, 9f, 26f), Quaternion.Euler(0f, 180f, 0f)).GetComponent<ParticleSystem>();

            // 엔딩 UI (HUD 0 < 엔딩 5 < 결과창 10)
            GameObject hudGo = Find("HUD");
            var hudDoc = hudGo.GetComponent<UIDocument>();
            GameObject uiGo = Find("EndingUI") ?? new GameObject("EndingUI");
            var doc = Get<UIDocument>(uiGo);
            Get<PanelRenderer>(uiGo);
            doc.panelSettings = hudDoc.panelSettings;
            doc.visualTreeAsset = endingUxml;
            doc.sortingOrder = 5f;
            EditorUtility.SetDirty(doc);

            // PanelRenderer가 자기 visualTreeAsset으로 같은 UI를 한 벌 더 그린다 (6000.6).
            // 그 사본은 스크립트가 갱신하지 않아 HUD 숫자가 겹쳐 보이고, 엔딩에서 HUD를 숨길 수 없다.
            // UIDocument 쪽 트리만 남긴다 (panelSettings 저장을 위해 PanelRenderer 컴포넌트는 유지).
            foreach (string n in new[] { "HUD", "Result", "EndingUI" })
            {
                var pr = Find(n).GetComponent<PanelRenderer>();
                if (pr == null) continue;
                pr.visualTreeAsset = null;
                EditorUtility.SetDirty(pr);
            }

            // 엔딩 진행
            GameObject endGo = Find("Ending") ?? new GameObject("Ending");
            var controller = Get<EndingController>(endGo);
            var director = Get<PlayableDirector>(endGo);
            var sequence = Get<EndingSequence>(endGo);
            var flow = Object.FindAnyObjectByType<GameFlow>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            foreach (var track in timeline.GetOutputTracks()) director.SetGenericBinding(track, controller);
            EditorUtility.SetDirty(director);

            Set(sequence, "director", director);
            Set(sequence, "controller", controller);
            Set(sequence, "flow", flow);
            Set(flow, "ending", sequence);

            Set(controller, "spawner", spawner);
            Set(controller, "bossDirector", Object.FindAnyObjectByType<BossDirector>());
            Set(controller, "context", Object.FindAnyObjectByType<StageContext>());
            Set(controller, "player", playerT);
            Set(controller, "movement", playerT.GetComponent<PlayerMovement>());
            Set(controller, "heat", heat);
            Set(controller, "clouds", clouds);
            Set(controller, "cam", cam);
            Set(controller, "cameraShake", cam.GetComponent<CameraShake>());
            Set(controller, "skyVolume", sky);
            Set(controller, "keyLight", key);
            Set(controller, "stars", Object.FindAnyObjectByType<BackgroundScroller>());
            Set(controller, "earth", earth);
            Set(controller, "hud", hudDoc);
            Set(controller, "endingUi", doc);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"wired: timeline tracks={timeline.GetOutputTracks().Count()} duration={timeline.duration}";
        }

        // 크게 띄워도 다각형 윤곽이 보이지 않도록 내장 구보다 촘촘한 UV 구 (반지름 0.5, 내장 구와 같은 UV 방향)
        private static Mesh SphereMesh(string path, int lon, int lat)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Sphere64" };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            for (int y = 0; y <= lat; y++)
            for (int x = 0; x <= lon; x++)
            {
                float u = x / (float)lon, v = y / (float)lat;
                float phi = u * Mathf.PI * 2f, theta = (v - 0.5f) * Mathf.PI;
                verts.Add(new Vector3(Mathf.Cos(theta) * Mathf.Cos(phi), Mathf.Sin(theta), Mathf.Cos(theta) * Mathf.Sin(phi)) * 0.5f);
                uvs.Add(new Vector2(1f - u, v));
            }
            for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int a = y * (lon + 1) + x, b = a + lon + 1;
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static GameObject Find(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t => t.name == name && t.parent == null).Select(t => t.gameObject).FirstOrDefault();

        private static T Get<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        private static Renderer Child(Transform parent, string name, Mesh mesh, Material mat, float scale)
        {
            Transform t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * scale;
            Get<MeshFilter>(go).sharedMesh = mesh;
            var r = Get<MeshRenderer>(go);
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        private static GameObject Instance(GameObject prefab, string name, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            Transform existing = parent != null ? parent.Find(name) : Find(name)?.transform;
            GameObject go = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            return go;
        }

        private static Material LoadOrCreate(string path, string shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = Shader.Find(shader);
            return m;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
