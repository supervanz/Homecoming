using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// M11 WebGL 용량 줄이기: 생성 모델(GLB)에 들어 있는 4096·2048 무압축 텍스처를 1024 압축 텍스처로 따로 만들고,
    /// 재질이 그것을 쓰게 바꾼다. GLB 안의 원본 텍스처는 더 이상 참조되지 않아 빌드에서 빠진다.
    /// 화면에서 기체는 수백 픽셀이라 1024로 충분하다. 다시 실행해도 같은 결과가 된다.
    /// </summary>
    public static class TextureShrinkBuilder
    {
        private const string ModelDir = "Assets/_Project/Art/Models";
        private const string TexDir = "Assets/_Project/Art/Textures/Models";
        private const string MatDir = "Assets/_Project/Art/Materials";
        private const int Size = 1024;

        [MenuItem("Homecoming/Shrink Model Textures (M11)")]
        public static string Build()
        {
            Directory.CreateDirectory(TexDir);
            var log = new System.Text.StringBuilder();

            // 적·보스 재질 (ModelSwapBuilder가 만든 URP Lit)
            foreach (var (glb, mat) in new[] { ("sat_aim", "M_Aimer"), ("carrier", "M_Carrier"), ("boss_shell", "M_BossShell") })
            {
                Texture2D color = Shrink(glb, "Color", false, log);
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{mat}.mat");
                m.SetTexture("_BaseMap", color);
                EditorUtility.SetDirty(m);
            }

            // 플레이어: glTF 재질 → URP Lit (색 + 노멀). 피탄 플래시와 같은 셰이더 계열로 맞춘다
            Texture2D pColor = Shrink("player_ship", "Color", false, log);
            Texture2D pNormal = Shrink("player_ship", "NormalGL", true, log);
            Material pm = PlayerMaterial(pColor, pNormal);
            AssetDatabase.SaveAssets();
            log.AppendLine(AssignPlayer(pm));
            return log.ToString();
        }

        // GPU에서 1024로 줄여 PNG로 저장하고 압축 임포트
        private static Texture2D Shrink(string glb, string prefix, bool normal, System.Text.StringBuilder log)
        {
            Texture2D src = AssetDatabase.LoadAllAssetsAtPath($"{ModelDir}/{glb}.glb").OfType<Texture2D>().First(t => t.name.StartsWith(prefix));
            var rt = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32, normal ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, normal);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            string path = $"{TexDir}/T_{glb}_{prefix}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !normal;
            imp.maxTextureSize = Size;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            log.AppendLine($"{glb} {prefix}: {src.width} -> {Size} ({path})");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material PlayerMaterial(Texture2D color, Texture2D normal)
        {
            string path = $"{MatDir}/M_PlayerShip.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", color);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Metallic", 0.5f);
            m.SetFloat("_Smoothness", 0.4f);
            m.SetColor("_EmissionColor", new Color(0.002f, 0.002f, 0.002f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            m.EnableKeyword("_EMISSION");
            var ed = (MaterialEditor)Editor.CreateEditor(m);
            ed.customShaderGUI?.ValidateMaterial(m);
            Object.DestroyImmediate(ed);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static string AssignPlayer(Material m)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Stage01.unity", OpenSceneMode.Single);
            m = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/M_PlayerShip.mat"); // 씬을 연 뒤 다시 불러온다
            var player = Object.FindAnyObjectByType<Homecoming.Player.PlayerDamageReceiver>(FindObjectsInactive.Include);
            Transform model = player.transform.Find("Visual/Model");
            int n = 0;
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterial = m;
                EditorUtility.SetDirty(r);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"player renderers -> M_PlayerShip: {n}";
        }
    }
}
