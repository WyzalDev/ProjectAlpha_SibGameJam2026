using UnityEngine;

namespace _Game.Scripts.Infrastructure.GameRunner
{
    /// <summary>
    /// берет на себя логику проверки значения из конфига: нужно ли запустить геймплейную сцену или или по дефолт пайпу
    /// </summary>
    public static class GameRunnerHelper
    {
        public static void CheckStartFromGameplay(CheckStartFromGameplayConfig config)
        {
            if (!Application.isEditor)
                return;
			
            bool enabledStartFromGameplay = config.AlwaysStartFromGameplay;
			
            if (enabledStartFromGameplay) 
                QuickStartBridge.IsQuickStart = true;
        }
    }
}