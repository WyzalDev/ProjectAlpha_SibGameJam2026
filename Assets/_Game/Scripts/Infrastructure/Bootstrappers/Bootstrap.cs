using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Common.Pause;
using _Game.Scripts.Core.Common.Sound;
using _Game.Scripts.Infrastructure.GameRunner;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.Scripts.Infrastructure.Bootstrappers
{
    public class Bootstrap : MonoBehaviour
    {
        //TODO когда появиться занавеска [SerializeField] private Curtain _curtainPrefab;
        [SerializeField] private CheckStartFromGameplayConfig _checkStartFromGameplayConfig;
        [SerializeField] private OstPlayerController _ostPlayerController;
        
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            //TODO добавить когда появиться FMOD звуки и музыка ServiceLocator.Register<ISoundService>(new SoundService());
            //TODO добавить когда появиться FMOD звуки и музыка OstPlayerController ostPlayerController = Instantiate(_ostPlayerController, transform);
            //TODO добавить когда появиться FMOD звуки и музыка ServiceLocator.Register<OstPlayerController>(ostPlayerController);
            ServiceLocator.Register<EventBus>(new EventBus());
            var inputHandler = new InputHandler();
            inputHandler.Init();
            ServiceLocator.Register<IInputHandler>(inputHandler);
            ServiceLocator.Register<IPauseController>(new PauseController());

            //TODO поменять на корутину и когда будет занавеска await curtain.Show();

            GameRunnerHelper.CheckStartFromGameplay(_checkStartFromGameplayConfig);
            
            if (QuickStartBridge.IsQuickStart && ! "Bootstrap".Equals(QuickStartBridge.SceneName))
            {
                SceneManager.LoadScene(QuickStartBridge.SceneName);
                return;
            }
            Debug.Log("Default Pipeline: Boot - Menu - Gameplay");
            SceneManager.LoadScene("Menu");
        }
    }
}