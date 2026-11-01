using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.Scripts.Core.Features.End
{
    public class GameplaySceneButton : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        public void LoadGameplayScene()
        {
            if (string.IsNullOrWhiteSpace(_gameplaySceneName))
            {
                Debug.LogWarning(
                    $"{nameof(GameplaySceneButton)}: Gameplay scene name is empty.",
                    this);

                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(_gameplaySceneName))
            {
                Debug.LogError(
                    $"{nameof(GameplaySceneButton)}: Scene '{_gameplaySceneName}' is not available in Build Settings.",
                    this);

                return;
            }

            SceneManager.LoadScene(_gameplaySceneName);
        }
    }
}