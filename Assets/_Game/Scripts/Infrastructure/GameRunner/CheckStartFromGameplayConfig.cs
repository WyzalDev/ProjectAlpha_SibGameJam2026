using UnityEngine;

namespace _Game.Scripts.Infrastructure.GameRunner
{
    [CreateAssetMenu(fileName = "CheckStartFromGameplayConfig", menuName = "Configs/CheckStartFromGameplayConfig")]
    public class CheckStartFromGameplayConfig : ScriptableObject
    {
        // если Enabled == false то при запуске плей мода с любой сцены пойдет стандарт пайп: бут сцена - меню - геймплей
        // если true то позволяет запустить альтернативный пайп: инициализация глобальных сервисов - запуск сцены с которой апустили плей мод
        public bool AlwaysStartFromGameplay => _alwaysStartFromGameplay;

        [SerializeField] private bool _alwaysStartFromGameplay;
    }
}