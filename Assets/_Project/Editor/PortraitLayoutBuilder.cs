using System.Collections.Generic;
using Homecoming.Boss;
using Homecoming.Core;
using Homecoming.Enemy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// 세로 화면(9:16) 배치. 플레이 영역 16×18 → 10×18로 좁히고, 그에 맞춰 카메라·등장 위치·편대·경로·보스를 다시 놓는다.
    /// 세로 길이는 그대로라 적이 화면을 지나는 시간(5분 타임라인)은 유지된다.
    /// 배율을 곱하지 않고 목표값을 직접 적으므로, 다시 실행해도 같은 결과가 된다.
    /// </summary>
    public static class PortraitLayoutBuilder
    {
        private const string Data = "Assets/_Project/Data/";
        private const float CameraPitch = 65f;
        private const float TargetAspect = 9f / 16f;

        // 가로 16 기준 등장 X → 가로 10 기준 등장 X. 옆에서 들어오는 스윕(±10.5)은 영역 밖 ±6.8에서 시작
        private static readonly Dictionary<float, float> SpawnX = new Dictionary<float, float>
        {
            { 0f, 0f }, { 2f, 1.25f }, { 3f, 1.9f }, { 4f, 2.5f }, { 5f, 3.1f }, { 6f, 3.75f }, { 10.5f, 6.8f },
        };

        [MenuItem("Homecoming/Apply Portrait Layout (9:16)")]
        public static string Build()
        {
            var log = new System.Text.StringBuilder();

            // 플레이 영역
            var area = Load<PlayAreaConfig>("PlayAreaConfig.asset");
            area.size = new Vector2(10f, 18f);
            EditorUtility.SetDirty(area);

            // 편대: 기체 폭 때문에 등장 위치보다 덜 좁힌다 (×0.8 안팎)
            SetOffsets("Formations/F_V5.asset", new Vector2(0, 0), new Vector2(-1.1f, 1.2f), new Vector2(1.1f, 1.2f), new Vector2(-2.2f, 2.4f), new Vector2(2.2f, 2.4f));
            SetOffsets("Formations/F_StopShoot2.asset", new Vector2(-2f, 0), new Vector2(2f, 0));
            SetOffsets("Formations/F_DroneSummon.asset", new Vector2(-2.2f, -1), new Vector2(2.2f, -1), new Vector2(-1.1f, -2), new Vector2(1.1f, -2));

            // 경로: 가로 흔들림·가로지르기 폭을 새 영역에 맞춘다
            SetPath("Paths/Path_Snake.asset", new Vector2(0, 0), new Vector2(2f, -4), new Vector2(-2f, -8), new Vector2(2f, -12), new Vector2(-2f, -16), new Vector2(0, -22));
            SetPath("Paths/Path_Sweep.asset", new Vector2(0, 0), new Vector2(7f, -4), new Vector2(15f, -6));

            // 등장 위치
            var tl = Load<StageTimeline>("Stage/Stage01Timeline.asset");
            foreach (SpawnEntry e in tl.entries)
            {
                float ax = Mathf.Abs(e.spawnPosition.x);
                float nx = MapX(ax);
                e.spawnPosition.x = Mathf.Sign(e.spawnPosition.x) * nx;
            }
            EditorUtility.SetDirty(tl);

            // 보스: 패널 포함 폭 12 → 9 (외형만 0.75배, 판정 반경·레이저 폭도 같은 비율). 흔들림은 영역 안에 머물도록 줄임
            var boss = Load<BossConfig>("Boss/BossConfig.asset");
            boss.shellRadius = 1.5f;
            boss.coreRadius = 0.75f;
            boss.swayAmplitude = 0.6f;
            EditorUtility.SetDirty(boss);
            SetLaserWidth("Boss/Actions/BA1_CenterLaser.asset", 1.35f);
            SetLaserWidth("Boss/Actions/BA2_SweepLaser.asset", 1.05f);
            ScaleBossPrefab(log);

            // 별 배경: 세로 화면 전체(평면 아래 y=-4에서 가로 ±11, 세로 -13~+22)를 덮는다
            var scroll = Load<ScrollConfig>("ScrollConfig.asset");
            scroll.fieldSize = new Vector2(24f, 38f);
            scroll.starCount = 120;
            EditorUtility.SetDirty(scroll);

            AssetDatabase.SaveAssets();
            log.Append(PlaceCamera());
            return log.ToString();
        }

        private static float MapX(float ax)
        {
            foreach (var kv in SpawnX)
                if (Mathf.Abs(kv.Key - ax) < 0.01f || Mathf.Abs(kv.Value - ax) < 0.01f) return kv.Value; // 이미 바뀐 값이면 그대로
            Debug.LogWarning($"[PortraitLayout] unmapped spawn x {ax}, scaled by 0.625");
            return ax * 0.625f;
        }

        // 보스 외형만 줄이고 위치를 안쪽으로. 레이저(Laser)는 월드 크기로 그리므로 건드리지 않는다
        private static void ScaleBossPrefab(System.Text.StringBuilder log)
        {
            const string path = "Assets/_Project/Prefabs/Boss_Station.prefab";
            const float k = 0.75f;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Transform t = root.transform;
            t.Find("Shell").localScale = Vector3.one * k;
            t.Find("CoreVisual").localScale = Vector3.one * k;
            t.Find("CoreMuzzle").localPosition = new Vector3(0f, 0f, -0.8f * k);
            foreach (string side in new[] { "PanelL", "PanelR" })
            {
                Transform p = t.Find(side);
                float sx = side == "PanelL" ? -1f : 1f;
                p.localPosition = new Vector3(sx * 4.8f * k, 0f, 0.3f * k);
                p.Find("Visual").localScale = Vector3.one * k;
                p.Find("Muzzle").localPosition = new Vector3(0f, 0f, -0.6f * k);
                p.GetComponent<CircleHitbox>().Radius = 1.2f * k;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            log.AppendLine("boss visuals x0.75, panels at ±3.6");
        }

        // 9:16에서 플레이 영역 전체가 들어오는 가장 가까운 위치 (CameraFit과 같은 여백 기준)
        private static string PlaceCamera()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Stage01.unity", OpenSceneMode.Single);
            Camera cam = Camera.main;
            var fit = cam.GetComponent<CameraFit>();
            var so = new SerializedObject(fit);
            float margin = so.FindProperty("worldMargin").floatValue;
            float top = so.FindProperty("topPadding").floatValue;
            float bottom = so.FindProperty("bottomPadding").floatValue;
            float side = so.FindProperty("sidePadding").floatValue;
            var area = Load<PlayAreaConfig>("PlayAreaConfig.asset");

            cam.transform.rotation = Quaternion.Euler(CameraPitch, 0f, 0f);
            cam.aspect = TargetAspect;
            Vector3 fwd = cam.transform.forward;
            float hx = area.size.x * 0.5f + margin, hz = area.size.y * 0.5f + margin;
            bool Fits(float d)
            {
                cam.transform.position = -fwd * d;
                foreach (var c in new[] { new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz) })
                {
                    Vector3 v = cam.WorldToViewportPoint(c);
                    if (v.z <= 0f || v.x < side || v.x > 1f - side || v.y < bottom || v.y > 1f - top) return false;
                }
                return true;
            }
            float lo = 1f, hi = 200f;
            for (int i = 0; i < 40; i++) { float m = (lo + hi) * 0.5f; if (Fits(m)) hi = m; else lo = m; }
            cam.transform.position = -fwd * hi;
            cam.ResetAspect();
            EditorUtility.SetDirty(cam.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"camera pitch {CameraPitch} at {cam.transform.position}";
        }

        private static T Load<T>(string rel) where T : Object => AssetDatabase.LoadAssetAtPath<T>(Data + rel);

        private static void SetOffsets(string rel, params Vector2[] offsets)
        {
            var f = Load<FormationConfig>(rel);
            f.memberOffsets = offsets;
            EditorUtility.SetDirty(f);
        }

        private static void SetPath(string rel, params Vector2[] points)
        {
            var p = Load<PathConfig>(rel);
            for (int i = 0; i < points.Length && i < p.points.Length; i++) p.points[i].position = points[i];
            EditorUtility.SetDirty(p);
        }

        private static void SetLaserWidth(string rel, float width)
        {
            var a = Load<BossLaserAction>(rel);
            a.width = width;
            EditorUtility.SetDirty(a);
        }
    }
}
