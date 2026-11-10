using System.Collections.Generic;
using System.IO;
using Homecoming.Vfx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// M8 VFX 에셋 생성기. 텍스처·머티리얼·파티클 프리팹·VfxConfig를 코드로 만든다.
    /// 다시 실행하면 같은 경로의 에셋을 같은 설정으로 다시 만든다 (GUID 유지).
    /// 폭발 = 섬광 + 불덩이 + 파편 + 충격파 링 + 잔광 (13-6). 연기는 쓰지 않는다 (13-4).
    /// </summary>
    public static class VfxBuilder
    {
        private const string Root = "Assets/_Project/VFX";
        private const string TexDir = Root + "/Textures";
        private const string MatDir = Root + "/Materials";
        private const string PrefabDir = Root + "/Prefabs";
        private const string ConfigPath = "Assets/_Project/Data/VfxConfig.asset";

        private static Material _soft, _ring, _debris;
        private static Mesh _cube;

        // 진영 색 (13-2)
        private static readonly Color AiSpark = new Color(0.75f, 0.9f, 1f);
        private static readonly Color AiFlash = new Color(0.85f, 0.92f, 1f);
        private static readonly Color AiFireHot = new Color(1f, 0.85f, 0.75f);
        private static readonly Color AiFire = new Color(1f, 0.25f, 0.15f);
        private static readonly Color AiFireEnd = new Color(0.35f, 0.02f, 0.03f);
        private static readonly Color HuFireHot = new Color(1f, 0.95f, 0.7f);
        private static readonly Color HuFire = new Color(1f, 0.55f, 0.1f);
        private static readonly Color HuFireEnd = new Color(0.5f, 0.15f, 0f);
        private static readonly Color Metal = new Color(0.62f, 0.64f, 0.68f);
        private static readonly Color SolarBlue = new Color(0.12f, 0.2f, 0.45f);

        [MenuItem("Homecoming/Build VFX")]
        public static void Build()
        {
            Directory.CreateDirectory(TexDir);
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(PrefabDir);

            Texture2D softTex = WriteTexture("T_VfxSoft", r => Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f));
            Texture2D ringTex = WriteTexture("T_VfxRing", r => Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.07f, 2f)));
            _soft = AdditiveMaterial("M_VfxSoft", softTex);
            _ring = AdditiveMaterial("M_VfxRing", ringTex);
            _debris = DebrisMaterial("M_VfxDebris");
            _cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            var fx = new Dictionary<VfxId, PooledEffect>
            {
                [VfxId.HitSpark] = SaveEffect("Vfx_HitSpark", 0.3f, BuildHitSpark),
                [VfxId.ExplosionSmall] = SaveEffect("Vfx_ExplosionSmall", 0.9f, BuildExplosionSmall),
                [VfxId.ExplosionMedium] = SaveEffect("Vfx_ExplosionMedium", 1.1f, BuildExplosionMedium),
                [VfxId.PlayerHit] = SaveEffect("Vfx_PlayerHit", 0.5f, BuildPlayerHit),
                [VfxId.PhaseTransition] = SaveEffect("Vfx_PhaseTransition", 1.4f, BuildPhaseTransition),
                [VfxId.Bomb] = SaveEffect("Vfx_Bomb", 0.9f, BuildBomb),
                [VfxId.BossDestroy] = SaveEffect("Vfx_BossDestroy", 1.8f, BuildBossDestroy),
                [VfxId.PlayerDeath] = SaveEffect("Vfx_PlayerDeath", 1.2f, BuildPlayerDeath),
            };

            GameObject[] ai =
            {
                SaveLoop("Vfx_DamageAi_1", t => BuildDamage(t, 1, false)),
                SaveLoop("Vfx_DamageAi_2", t => BuildDamage(t, 2, false)),
                SaveLoop("Vfx_DamageAi_3", t => BuildDamage(t, 3, false)),
            };
            GameObject[] human =
            {
                SaveLoop("Vfx_DamageHuman_1", t => BuildDamage(t, 1, true)),
                SaveLoop("Vfx_DamageHuman_2", t => BuildDamage(t, 2, true)),
                SaveLoop("Vfx_DamageHuman_3", t => BuildDamage(t, 3, true)),
            };

            WriteConfig(fx, ai, human);
            AssetDatabase.SaveAssets();
            Debug.Log("[VfxBuilder] VFX assets built");
        }

        // ---------- 일회성 이펙트 (13-3 위계: 아래로 갈수록 크고 밝고 길다) ----------

        private static void BuildHitSpark(Transform t)
        {
            var flash = Layer(t, "Flash", _soft);
            Burst(flash, 1); Life(flash, 0.06f, 0.06f); Size(flash, 0.7f, 0.7f); Speed(flash, 0f, 0f);
            Colors(flash, Grad(AiFlash, AiFlash, 0.6f));

            var sparks = Layer(t, "Sparks", _soft);
            Burst(sparks, 6); Life(sparks, 0.1f, 0.18f); Size(sparks, 0.06f, 0.09f); Speed(sparks, 5f, 9f);
            Colors(sparks, Grad(Color.white, AiSpark, 1f)); Stretch(sparks, 0.035f); Circle(sparks, 0.1f);
        }

        private static void BuildExplosionSmall(Transform t)
        {
            Flash(t, AiFlash, 2.4f, 0.08f);
            Fireball(t, 6, 0.6f, 1.1f, 0.22f, 0.32f, 0.3f, AiFireHot, AiFire, AiFireEnd);
            Debris(t, "Debris", 5, 0.08f, 0.18f, 3f, 6f, 0.45f, 0.7f, Metal, Metal);
            Embers(t, 6, 1f, 3f, 0.35f, 0.55f, AiFire);
        }

        private static void BuildExplosionMedium(Transform t)
        {
            Flash(t, AiFlash, 3.8f, 0.1f);
            Fireball(t, 10, 1f, 1.8f, 0.3f, 0.42f, 0.6f, AiFireHot, AiFire, AiFireEnd);
            Debris(t, "Debris", 7, 0.1f, 0.22f, 3f, 7f, 0.6f, 0.9f, Metal, Metal);
            Debris(t, "PanelShards", 5, 0.12f, 0.3f, 2.5f, 6f, 0.6f, 0.9f, SolarBlue, SolarBlue);
            Shockwave(t, 5f, 0.3f, AiSpark, 0f);
            Embers(t, 12, 1.5f, 4f, 0.4f, 0.7f, AiFire);
        }

        private static void BuildPlayerHit(Transform t)
        {
            Flash(t, HuFire, 2.6f, 0.08f);
            var sparks = Layer(t, "Sparks", _soft);
            Burst(sparks, 14); Life(sparks, 0.18f, 0.28f); Size(sparks, 0.08f, 0.12f); Speed(sparks, 5f, 9f);
            Colors(sparks, Grad(HuFireHot, HuFire, 1f)); Stretch(sparks, 0.04f); Circle(sparks, 0.15f);
            Shockwave(t, 3f, 0.25f, HuFire, 0f);
        }

        private static void BuildPhaseTransition(Transform t)
        {
            Flash(t, AiFlash, 6f, 0.12f);
            Fireball(t, 16, 1.5f, 2.5f, 0.38f, 0.48f, 1.6f, AiFireHot, AiFire, AiFireEnd);
            Debris(t, "Debris", 12, 0.12f, 0.3f, 4f, 8f, 0.8f, 1.2f, Metal, Metal);
            Debris(t, "PanelShards", 12, 0.2f, 0.45f, 3f, 7f, 0.8f, 1.2f, SolarBlue, SolarBlue);
            Shockwave(t, 9f, 0.4f, AiSpark, 0f);
            var sparks = Layer(t, "Sparks", _soft);
            Burst(sparks, 20); Life(sparks, 0.2f, 0.35f); Size(sparks, 0.08f, 0.12f); Speed(sparks, 6f, 11f);
            Colors(sparks, Grad(Color.white, AiSpark, 1f)); Stretch(sparks, 0.035f); Circle(sparks, 1f);
        }

        private static void BuildBomb(Transform t)
        {
            Flash(t, HuFire, 10f, 0.15f);
            var embers = Layer(t, "Embers", _soft);
            Burst(embers, 40); Life(embers, 0.4f, 0.65f); Size(embers, 0.12f, 0.2f); Speed(embers, 9f, 15f);
            Colors(embers, Grad(HuFireHot, HuFire, 1f)); Stretch(embers, 0.03f); Circle(embers, 0.3f);
            Shockwave(t, 14f, 0.5f, HuFire, 0f);
        }

        private static void BuildBossDestroy(Transform t)
        {
            Flash(t, Color.white, 14f, 0.2f);
            for (int i = 0; i < 3; i++)
            {
                var fb = Fireball(t, 14, 2f, 3.4f, 0.3f, 0.4f, 2f, AiFireHot, AiFire, AiFireEnd);
                fb.name = "Fireball" + i;
                SetBurstTime(fb, i * 0.08f);
            }
            Debris(t, "Debris", 22, 0.15f, 0.35f, 5f, 11f, 1f, 1.5f, Metal, Metal);
            Debris(t, "PanelShards", 18, 0.25f, 0.5f, 4f, 9f, 1f, 1.5f, SolarBlue, SolarBlue);
            Shockwave(t, 18f, 0.5f, Color.white, 0f);
            Shockwave(t, 12f, 0.45f, AiSpark, 0.2f);
            Embers(t, 30, 3f, 8f, 0.6f, 1f, AiFire);
        }

        private static void BuildPlayerDeath(Transform t)
        {
            Flash(t, HuFire, 5f, 0.12f);
            Fireball(t, 12, 1.2f, 2f, 0.35f, 0.45f, 0.6f, HuFireHot, HuFire, HuFireEnd);
            Debris(t, "Debris", 14, 0.1f, 0.25f, 3f, 8f, 0.7f, 1.1f, Metal, new Color(0.55f, 0.35f, 0.2f));
            Shockwave(t, 7f, 0.4f, HuFire, 0f);
            Embers(t, 16, 2f, 5f, 0.5f, 0.8f, HuFire);
        }

        // ---------- 손상 상태 (13-4-1) ----------

        private static void BuildDamage(Transform t, int stage, bool human)
        {
            Color hot = human ? HuFireHot : AiFireHot;
            Color fire = human ? HuFire : AiFire;
            Color end = human ? HuFireEnd : AiFireEnd;
            Color spark = human ? HuFireHot : AiSpark;

            // 루트 = 스파크 (1단계는 간헐적, 이후 지속)
            ParticleSystem sparks = t.GetComponent<ParticleSystem>();
            Defaults(sparks, _soft, true);
            var main = sparks.main; main.duration = 1f;
            Life(sparks, 0.12f, 0.22f); Size(sparks, 0.05f, 0.08f); Speed(sparks, 2f, 4.5f);
            Colors(sparks, Grad(Color.white, spark, 1f)); Stretch(sparks, 0.04f); Sphere(sparks, 0.25f);
            var em = sparks.emission;
            if (stage == 1)
            {
                em.SetBursts(new[]
                {
                    new ParticleSystem.Burst(0f, 4) { probability = 0.6f },
                    new ParticleSystem.Burst(0.5f, 4) { probability = 0.6f },
                });
            }
            else em.rateOverTime = stage == 2 ? 6f : 12f;

            var flame = Layer(t, "Flame", _soft, true);
            float rate = stage == 1 ? 3f : stage == 2 ? 14f : 26f;
            float sMin = stage == 1 ? 0.2f : stage == 2 ? 0.35f : 0.6f;
            float sMax = stage == 1 ? 0.3f : stage == 2 ? 0.55f : 0.9f;
            var fem = flame.emission; fem.rateOverTime = rate;
            Life(flame, 0.25f, 0.4f); Size(flame, sMin, sMax); Speed(flame, 0.3f, 0.8f); Sphere(flame, 0.2f);
            Colors(flame, Grad3(hot, fire, end, 0.9f));
            SizeCurve(flame, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.2f)));

            if (stage >= 2)
            {
                // 파편이 조금씩 떨어짐
                var drip = Layer(t, "Debris", _debris, true);
                MeshRender(drip);
                var dem = drip.emission; dem.rateOverTime = stage == 2 ? 1.5f : 3f;
                Life(drip, 0.5f, 0.8f); Speed(drip, 0.8f, 2f); Sphere(drip, 0.2f);
                Size3D(drip, 0.06f, 0.14f);
                Colors(drip, new Gradient { colorKeys = new[] { new GradientColorKey(Metal, 0f), new GradientColorKey(Metal, 1f) } });
                ShrinkOut(drip);
            }
        }

        // ---------- 레이어 조합 ----------

        private static void Flash(Transform t, Color c, float size, float life)
        {
            var p = Layer(t, "Flash", _soft);
            Burst(p, 1); Life(p, life, life); Size(p, size, size); Speed(p, 0f, 0f);
            Colors(p, Grad(c, c, 1f));
            SizeCurve(p, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.8f)));
        }

        private static ParticleSystem Fireball(Transform t, int count, float sMin, float sMax, float lMin, float lMax,
            float radius, Color hot, Color fire, Color end)
        {
            var p = Layer(t, "Fireball", _soft);
            Burst(p, count); Life(p, lMin, lMax); Size(p, sMin, sMax); Speed(p, 0.4f, 1.6f); Circle(p, radius);
            Colors(p, Grad3(hot, fire, end, 1f));
            SizeCurve(p, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.6f)));
            return p;
        }

        private static void Debris(Transform t, string name, int count, float sMin, float sMax, float vMin, float vMax,
            float lMin, float lMax, Color a, Color b)
        {
            var p = Layer(t, name, _debris);
            MeshRender(p);
            Burst(p, count); Life(p, lMin, lMax); Speed(p, vMin, vMax); Circle(p, 0.2f);
            Size3D(p, sMin, sMax);
            var main = p.main;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            ShrinkOut(p);
        }

        private static void Shockwave(Transform t, float size, float life, Color c, float delay)
        {
            var p = Layer(t, "Shockwave", _ring);
            Burst(p, 1); Life(p, life, life); Size(p, size, size); Speed(p, 0f, 0f);
            Colors(p, Grad(c, c, 0.9f));
            SizeCurve(p, new AnimationCurve(new Keyframe(0f, 0.05f, 0f, 3f), new Keyframe(1f, 1f, 0.3f, 0f)));
            // 링은 카메라가 아닌 평면에 눕힌다
            var r = p.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            SetBurstTime(p, delay);
        }

        private static void Embers(Transform t, int count, float vMin, float vMax, float lMin, float lMax, Color c)
        {
            var p = Layer(t, "Embers", _soft);
            Burst(p, count); Life(p, lMin, lMax); Size(p, 0.05f, 0.09f); Speed(p, vMin, vMax); Circle(p, 0.3f);
            Colors(p, Grad(Color.Lerp(c, Color.white, 0.4f), c, 1f));
        }

        // ---------- 파티클 설정 도우미 ----------

        private static ParticleSystem Layer(Transform parent, string name, Material m, bool loop = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ParticleSystem>();
            Defaults(p, m, loop);
            return p;
        }

        private static void Defaults(ParticleSystem p, Material m, bool loop)
        {
            var main = p.main;
            main.playOnAwake = false;
            main.loop = loop;
            main.duration = loop ? 1f : 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 64;
            main.startSpeed = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = p.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[0]);
            var shape = p.shape;
            shape.enabled = false;
            var r = p.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = m;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 3f;
        }

        private static void Burst(ParticleSystem p, int count)
        {
            var em = p.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }

        private static void SetBurstTime(ParticleSystem p, float time)
        {
            var em = p.emission;
            var b = em.GetBurst(0);
            b.time = time;
            em.SetBurst(0, b);
            var main = p.main;
            main.duration = Mathf.Max(main.duration, time + 0.05f);
        }

        private static void Life(ParticleSystem p, float a, float b) { var m = p.main; m.startLifetime = new ParticleSystem.MinMaxCurve(a, b); }
        private static void Size(ParticleSystem p, float a, float b) { var m = p.main; m.startSize = new ParticleSystem.MinMaxCurve(a, b); }
        private static void Speed(ParticleSystem p, float a, float b) { var m = p.main; m.startSpeed = new ParticleSystem.MinMaxCurve(a, b); }

        private static void Size3D(ParticleSystem p, float a, float b)
        {
            var m = p.main;
            m.startSize3D = true;
            m.startSizeX = new ParticleSystem.MinMaxCurve(a, b);
            m.startSizeY = new ParticleSystem.MinMaxCurve(a * 0.3f, b * 0.3f);
            m.startSizeZ = new ParticleSystem.MinMaxCurve(a, b * 1.5f);
            m.startRotation3D = true;
            m.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var rot = p.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rot.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        }

        // 평면(XZ) 위 원에서 바깥으로 퍼진다
        private static void Circle(ParticleSystem p, float radius)
        {
            var s = p.shape;
            s.enabled = true;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius = radius;
            s.rotation = new Vector3(90f, 0f, 0f);
        }

        private static void Sphere(ParticleSystem p, float radius)
        {
            var s = p.shape;
            s.enabled = true;
            s.shapeType = ParticleSystemShapeType.Sphere;
            s.radius = radius;
        }

        private static void Stretch(ParticleSystem p, float velocityScale)
        {
            var r = p.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = velocityScale;
            r.lengthScale = 1f;
        }

        private static void MeshRender(ParticleSystem p)
        {
            var r = p.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = _cube;
            r.alignment = ParticleSystemRenderSpace.Local;
        }

        private static void Colors(ParticleSystem p, Gradient g)
        {
            var c = p.colorOverLifetime;
            c.enabled = true;
            c.color = g;
        }

        private static void SizeCurve(ParticleSystem p, AnimationCurve curve)
        {
            var s = p.sizeOverLifetime;
            s.enabled = true;
            s.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void ShrinkOut(ParticleSystem p) =>
            SizeCurve(p, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));

        private static Gradient Grad(Color from, Color to, float alpha) => new Gradient
        {
            colorKeys = new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha, 0.5f), new GradientAlphaKey(0f, 1f) },
        };

        private static Gradient Grad3(Color a, Color b, Color c, float alpha) => new Gradient
        {
            colorKeys = new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.35f), new GradientColorKey(c, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha * 0.8f, 0.6f), new GradientAlphaKey(0f, 1f) },
        };

        // ---------- 에셋 저장 ----------

        /// <summary>일회성 이펙트: 루트는 보이지 않는 타이머 파티클 하나. 전체 길이가 끝나면 풀로 돌아간다.</summary>
        private static PooledEffect SaveEffect(string name, float totalLife, System.Action<Transform> build)
        {
            var go = new GameObject(name);
            var root = go.AddComponent<ParticleSystem>();
            Defaults(root, _soft, false);
            var main = root.main;
            main.duration = totalLife;
            main.startLifetime = totalLife;
            main.startSize = 0f;
            main.maxParticles = 1;
            Burst(root, 1);
            root.GetComponent<ParticleSystemRenderer>().enabled = false;
            go.AddComponent<PooledEffect>();
            build(go.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<PooledEffect>();
        }

        private static GameObject SaveLoop(string name, System.Action<Transform> build)
        {
            var go = new GameObject(name);
            go.AddComponent<ParticleSystem>();
            build(go.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Texture2D WriteTexture(string name, System.Func<float, float> alphaOfRadius)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(alphaOfRadius(Mathf.Sqrt(dx * dx + dy * dy)));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            string path = $"{TexDir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material LoadOrCreate(string name, string shader)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = Shader.Find(shader);
            return m;
        }

        // 가산 혼합: 겹칠수록 밝아지고 깊이를 쓰지 않는다. 불투명한 적 탄 뒤에 그려진다 (13-1).
        private static Material AdditiveMaterial(string name, Texture2D tex)
        {
            Material m = LoadOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material DebrisMaterial(string name)
        {
            Material m = LoadOrCreate(name, "Universal Render Pipeline/Particles/Lit");
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0.8f);
            m.SetFloat("_Smoothness", 0.55f);
            m.SetFloat("_Surface", 0f);
            m.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void WriteConfig(Dictionary<VfxId, PooledEffect> fx, GameObject[] ai, GameObject[] human)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<VfxConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<VfxConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }
            // 흔들림은 중형 폭발 이상, 히트스톱은 페이즈 전환·보스 격파만 (13-5)
            cfg.effects = new[]
            {
                Entry(VfxId.HitSpark, fx, 8, 12, 0f, 0f, 0f),
                Entry(VfxId.ExplosionSmall, fx, 6, 10, 0f, 0f, 0f),
                Entry(VfxId.ExplosionMedium, fx, 3, 5, 0.15f, 0.25f, 0f),
                Entry(VfxId.PlayerHit, fx, 1, 2, 0.22f, 0.25f, 0f),
                Entry(VfxId.PhaseTransition, fx, 1, 1, 0.3f, 0.5f, 0.12f),
                Entry(VfxId.Bomb, fx, 1, 2, 0.35f, 0.5f, 0f),
                Entry(VfxId.BossDestroy, fx, 1, 1, 0.6f, 1f, 0.2f),
                Entry(VfxId.PlayerDeath, fx, 1, 1, 0.4f, 0.6f, 0f),
            };
            cfg.aiDamagePrefabs = ai;
            cfg.humanDamagePrefabs = human;
            EditorUtility.SetDirty(cfg);
        }

        private static VfxConfig.Effect Entry(VfxId id, Dictionary<VfxId, PooledEffect> fx, int prewarm, int max,
            float shake, float shakeDuration, float hitStop) => new VfxConfig.Effect
        {
            id = id, prefab = fx[id], prewarm = prewarm, maxSimultaneous = max, scale = 1f,
            shake = shake, shakeDuration = shakeDuration, hitStop = hitStop,
        };
    }
}
