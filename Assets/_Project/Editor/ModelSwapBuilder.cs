using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// M7 외형 교체: 생성한 GLB 메쉬를 적·보스 프리팹의 프리미티브 자리에 넣는다.
    /// 로직(컴포넌트·판정)은 그대로 두고 외형 오브젝트만 바꾼다 (CLAUDE.md 9장).
    /// 재질은 GLB의 텍스처를 쓰는 URP Lit으로 만든다: glTF 셰이더는 피탄 플래시가 쓰는
    /// _EmissionColor / _BaseColor 속성이 없기 때문이다. 다시 실행해도 같은 결과가 된다.
    /// </summary>
    public static class ModelSwapBuilder
    {
        private const string ModelDir = "Assets/_Project/Art/Models";
        private const string MatDir = "Assets/_Project/Art/Materials";
        private const string PrefabDir = "Assets/_Project/Prefabs";

        private struct Swap
        {
            public string prefab;       // 프리팹 경로 (PrefabDir 기준)
            public string parent;       // 외형을 넣을 자식 경로
            public string[] remove;     // 지울 프리미티브 (parent 기준)
            public string glb;          // 모델 파일 이름 (확장자 없음)
            public string material;     // 만들 재질 이름
            public float yaw;           // 앞이 화면 아래(-Z)를 보도록 하는 회전
            public float width;         // 맞출 가로 폭 (X)
            public float top;           // 모델 윗면 높이. 플레이 평면(0)보다 조금만 위로: 모델 밑을 지나는 적 탄이 가려지지 않게 (13-1)
        }

        private static readonly Swap[] Swaps =
        {
            new Swap { prefab = "Enemies/Enemy_Aimer", parent = "Visual", remove = new[] { "Body", "Antenna", "Sensor" },
                       glb = "sat_aim", material = "M_Aimer", yaw = 270f, width = 1.3f, top = 0.2f },
            new Swap { prefab = "Enemies/Enemy_Carrier", parent = "Visual", remove = new[] { "Hull", "TurretL", "TurretR", "SensorL", "SensorR" },
                       glb = "carrier", material = "M_Carrier", yaw = 270f, width = 3.2f, top = 0.2f },
            // 보스는 외피만 교체: 코어와 좌우 포대는 프리미티브 유지 (외피가 떨어지면 코어가 드러남)
            // top은 안테나 끝 기준. 2.45면 돔 윗면이 약 1.0으로 코어(센서 윗면 0.8)를 덮고, 측면 모듈 윗면(약 0.5)이 포대 팔 높이와 맞는다
            new Swap { prefab = "Boss_Station", parent = "Shell", remove = new[] { "Hull", "SideL", "SideR", "Antenna" },
                       glb = "boss_shell", material = "M_BossShell", yaw = 270f, width = 5.2f, top = 2.45f },
        };

        [MenuItem("Homecoming/Swap Generated Models (M7)")]
        public static string Build()
        {
            var log = new System.Text.StringBuilder();
            foreach (Swap s in Swaps)
            {
                string glbPath = $"{ModelDir}/{s.glb}.glb";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
                if (model == null) { log.AppendLine($"skip {s.prefab}: {glbPath} missing"); continue; }
                Material mat = MakeMaterial(s.material, glbPath);

                string prefabPath = $"{PrefabDir}/{s.prefab}.prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                Transform parent = root.transform.Find(s.parent);
                foreach (string r in s.remove)
                {
                    Transform t = parent.Find(r);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
                Transform old = parent.Find("Model");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
                inst.name = "Model";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.Euler(0f, s.yaw, 0f);
                inst.transform.localScale = Vector3.one;
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = ShadowCastingMode.On;
                }

                // 회전한 상태의 경계로 폭을 맞추고, 가운데를 평면 원점에 둔다
                Bounds b = LocalBounds(inst.transform, parent);
                float k = s.width / b.size.x;
                inst.transform.localScale = Vector3.one * k;
                b = LocalBounds(inst.transform, parent);
                inst.transform.localPosition = new Vector3(-b.center.x, s.top - b.max.y, -b.center.z);
                b = LocalBounds(inst.transform, parent);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                log.AppendLine($"{s.prefab}: {s.glb} scale={k:0.###} size={b.size}");
            }
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        private static Bounds LocalBounds(Transform model, Transform space)
        {
            var mf = model.GetComponentsInChildren<MeshFilter>(true);
            bool any = false;
            Bounds result = default;
            foreach (var f in mf)
            {
                Bounds mb = f.sharedMesh.bounds;
                Vector3 c = mb.center, e = mb.extents;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = space.InverseTransformPoint(f.transform.TransformPoint(corner));
                    if (!any) { result = new Bounds(p, Vector3.zero); any = true; }
                    else result.Encapsulate(p);
                }
            }
            return result;
        }

        // GLB의 기본색 텍스처를 쓰는 URP Lit. 피탄 플래시용 _EMISSION은 BakedEmissive로 켜 둔다
        // (None이면 URP 머티리얼 검사가 키워드를 끈다).
        private static Material MakeMaterial(string name, string glbPath)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            // TextureShrinkBuilder가 만든 1024 압축본이 있으면 그것을 쓴다 (WebGL 용량, M11)
            string glbName = System.IO.Path.GetFileNameWithoutExtension(glbPath);
            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Art/Textures/Models/T_{glbName}_Color.png")
                ?? AssetDatabase.LoadAllAssetsAtPath(glbPath).OfType<Texture2D>().FirstOrDefault(t => t.name.StartsWith("Color"));
            m.SetTexture("_BaseMap", baseTex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0.55f);
            m.SetFloat("_Smoothness", 0.45f);
            m.SetColor("_EmissionColor", new Color(0.002f, 0.002f, 0.002f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            m.EnableKeyword("_EMISSION");
            var ed = (MaterialEditor)Editor.CreateEditor(m);
            ed.customShaderGUI?.ValidateMaterial(m);
            Object.DestroyImmediate(ed);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
