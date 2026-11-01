namespace _Game.Scripts.Core.Features.TriggerCutscene
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    namespace _Game.Scripts.Core.Features.SceneManagement
    {
        public class SceneLoadTrigger : MonoBehaviour
        {
            [Header("Settings")]
            [SerializeField] private string _sceneName;
            [SerializeField] private string _playerTag = "Player";

            private bool _isLoading;

            private void OnTriggerEnter(Collider other)
            {
                if (_isLoading || !other.CompareTag(_playerTag))
                    return;

                if (string.IsNullOrWhiteSpace(_sceneName))
                {
                    Debug.LogWarning(
                        $"{nameof(SceneLoadTrigger)}: Scene name is empty.",
                        this);

                    return;
                }

                if (!Application.CanStreamedLevelBeLoaded(_sceneName))
                {
                    Debug.LogError(
                        $"{nameof(SceneLoadTrigger)}: Scene '{_sceneName}' is not available in Build Settings.",
                        this);

                    return;
                }

                _isLoading = true;
                SceneManager.LoadScene(_sceneName);
            }
        }
    }
}