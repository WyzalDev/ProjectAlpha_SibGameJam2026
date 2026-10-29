using _Game.Scripts.Infrastructure.GameRunner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace _Game.Scripts.Editor
{
    public class GameStartup
    {
        [InitializeOnLoadMethod]
        static void SetStartScene()
        {
            QuickStartBridge.SceneName = SceneManager.GetActiveScene().name;

            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Game/Scenes/Bootstrap.unity");
        }
    }
}