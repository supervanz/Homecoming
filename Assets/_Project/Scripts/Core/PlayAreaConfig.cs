using UnityEngine;

namespace Homecoming.Core
{
    [CreateAssetMenu(menuName = "Homecoming/Play Area Config", fileName = "PlayAreaConfig")]
    public class PlayAreaConfig : ScriptableObject
    {
        [Tooltip("플레이 영역 크기 (X = 가로, Y = 세로/Z축)")]
        public Vector2 size = new Vector2(16f, 18f);
    }
}
