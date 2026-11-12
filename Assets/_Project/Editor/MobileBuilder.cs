using Homecoming.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Homecoming.EditorTools
{
    /// <summary>
    /// 모바일·WebGL 준비 (M11): Stage01의 플레이어에 터치 조작을 연결하고, WebGL 플레이어 설정을 세로 화면 기준으로 맞춘다.
    /// 다시 실행해도 같은 결과가 된다.
    /// </summary>
    public static class MobileBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Stage01.unity";

        [MenuItem("Homecoming/Setup Mobile + WebGL (M11)")]
        public static string Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var input = Object.FindAnyObjectByType<PlayerInputSource>(FindObjectsInactive.Include);
            var movement = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            GameObject hudGo = GameObject.Find("HUD");
            var hud = hudGo.GetComponent<UIDocument>();

            var touch = input.GetComponent<TouchControls>() ?? input.gameObject.AddComponent<TouchControls>();
            Set(touch, "movement", movement);
            Set(touch, "cam", Camera.main);
            Set(touch, "hud", hud);
            Set(input, "touch", touch);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // WebGL: 세로 9:16 캔버스, Unity Play 등 어떤 서버에서도 열리도록 Gzip + 압축 해제 대체
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            AssetDatabase.SaveAssets();

            return $"touch on {input.name}, canvas {PlayerSettings.defaultWebScreenWidth}x{PlayerSettings.defaultWebScreenHeight}, {PlayerSettings.WebGL.compressionFormat}";
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
