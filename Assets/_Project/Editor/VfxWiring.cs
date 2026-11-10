using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Enemy;
using Homecoming.Player;
using Homecoming.Vfx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// M8 VFX 컴포넌트 연결: 적·보스 프리팹에 피탄 플래시와 손상 상태를 붙이고,
    /// Stage01 씬에 VFX 풀·히트스톱·카메라 흔들림·이벤트 연결을 넣는다. 다시 실행해도 중복으로 붙지 않는다.
    /// </summary>
    public static class VfxWiring
    {
        private const string ConfigPath = "Assets/_Project/Data/VfxConfig.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Stage01.unity";
        private const string PrefabDir = "Assets/_Project/Prefabs/";

        [MenuItem("Homecoming/Wire VFX")]
        public static string Wire()
        {
            EnableEmission("Assets/_Project/Art/Materials/M_EnemyPrimitive.mat");

            var cfg = AssetDatabase.LoadAssetAtPath<VfxConfig>(ConfigPath);
            foreach (string name in new[] { "Enemy_Zako", "Enemy_Aimer" })
                EditPrefab(PrefabDir + "Enemies/" + name + ".prefab", root => Flash(root.gameObject, cfg, root.GetComponent<Health>(), root.Find("Visual")));

            EditPrefab(PrefabDir + "Enemies/Enemy_Carrier.prefab", root =>
            {
                HitFlash f = Flash(root.gameObject, cfg, root.GetComponent<Health>(), root.Find("Visual"));
                Damage(root.gameObject, cfg, root.GetComponent<Health>(), VfxFaction.Ai, root, new Vector3(0f, 0.6f, 0f), 1.6f, f);
            });

            EditPrefab(PrefabDir + "Boss_Station.prefab", root =>
            {
                Health core = root.GetComponent<Health>();
                HitFlash[] flashes = root.GetComponents<HitFlash>();
                HitFlash shellFlash = Flash(root.gameObject, cfg, core, root.Find("Shell"), flashes, 0);
                HitFlash coreFlash = Flash(root.gameObject, cfg, core, root.Find("CoreVisual"), flashes, 1);
                Damage(root.gameObject, cfg, core, VfxFaction.Ai, root, new Vector3(0f, 0.8f, 0f), 2.2f, coreFlash);
                foreach (BossPart part in root.GetComponentsInChildren<BossPart>(true))
                {
                    Health h = part.GetComponent<Health>();
                    HitFlash pf = Flash(part.gameObject, cfg, h, part.Visual.transform);
                    // 외형에 붙여 전환 때 함께 떨어져 나가고, 부서지면 함께 사라지게 한다
                    Damage(part.gameObject, cfg, h, VfxFaction.Ai, part.Visual.transform, new Vector3(0f, 0.5f, 0f), 1.4f, pf);
                }
                _ = shellFlash;
            });

            var carrier = AssetDatabase.LoadAssetAtPath<EnemyConfig>("Assets/_Project/Data/Enemies/E_Carrier.asset");
            carrier.deathVfx = VfxId.ExplosionMedium;
            EditorUtility.SetDirty(carrier);

            // 씬: 먼저 연 다음 에셋을 불러와야 참조가 저장된다
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            cfg = AssetDatabase.LoadAssetAtPath<VfxConfig>(ConfigPath);

            var player = Object.FindAnyObjectByType<PlayerDamageReceiver>(FindObjectsInactive.Include);
            var bomb = Object.FindAnyObjectByType<PlayerBomb>(FindObjectsInactive.Include);
            var director = Object.FindAnyObjectByType<BossDirector>(FindObjectsInactive.Include);
            Camera cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();

            GameObject vfx = GameObject.Find("Vfx") ?? new GameObject("Vfx");
            var pool = GetOrAdd<VfxPool>(vfx);
            Set(pool, "config", cfg);
            var stop = GetOrAdd<HitStop>(vfx);
            var shake = GetOrAdd<CameraShake>(cam.gameObject);
            Set(shake, "config", cfg);
            var stage = GetOrAdd<StageVfx>(vfx);
            Set(stage, "pool", pool);
            Set(stage, "cameraShake", shake);
            Set(stage, "hitStop", stop);
            Set(stage, "player", player);
            Set(stage, "bomb", bomb);
            Set(stage, "bossDirector", director);

            Health ph = player.Health != null ? player.Health : player.GetComponent<Health>();
            Damage(player.gameObject, cfg, ph, VfxFaction.Human, player.transform, new Vector3(0f, 0.7f, -0.2f), 1.2f, null);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return $"wired: player={player.name} bomb={bomb.name} boss={director.name} camera={cam.name}";
        }

        private static void EnableEmission(string path)
        {
            // 피탄 플래시를 MaterialPropertyBlock의 _EmissionColor로 내기 위해 _EMISSION을 켠다.
            // 기본 발광은 거의 검정이라 평소 모습은 바뀌지 않는다.
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            m.SetColor("_EmissionColor", new Color(0.002f, 0.002f, 0.002f));
            // None이면 URP의 머티리얼 검사(재임포트 시)가 _EMISSION을 꺼 버린다. 베이크를 쓰지 않으므로 BakedEmissive는 영향 없음.
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            m.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
        }

        private static void EditPrefab(string path, System.Action<Transform> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            edit(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static HitFlash Flash(GameObject go, VfxConfig cfg, Health h, Transform visual, HitFlash[] existing = null, int index = 0)
        {
            existing ??= go.GetComponents<HitFlash>();
            HitFlash f = index < existing.Length ? existing[index] : go.AddComponent<HitFlash>();
            Set(f, "config", cfg);
            Set(f, "health", h);
            Set(f, "visual", visual != null ? visual.gameObject : null);
            return f;
        }

        private static void Damage(GameObject go, VfxConfig cfg, Health h, VfxFaction faction, Transform anchor, Vector3 offset, float scale, HitFlash flash)
        {
            var d = GetOrAdd<DamageStateVfx>(go);
            var so = new SerializedObject(d);
            so.FindProperty("config").objectReferenceValue = cfg;
            so.FindProperty("health").objectReferenceValue = h;
            so.FindProperty("faction").enumValueIndex = (int)faction;
            so.FindProperty("anchor").objectReferenceValue = anchor;
            so.FindProperty("localOffset").vector3Value = offset;
            so.FindProperty("scale").floatValue = scale;
            so.FindProperty("flash").objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
