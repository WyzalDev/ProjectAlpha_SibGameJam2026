namespace _Game.Scripts.Infrastructure.GameRunner
{
    /// <summary>
    ///  класс хелпер связующее звено для полноценной работы механики корректного запуска плей мода с нужной сцены
    /// </summary>
    public static class QuickStartBridge
    {
        public static bool IsQuickStart { get; set; }
        public static string SceneName { get; set; }
    }
}