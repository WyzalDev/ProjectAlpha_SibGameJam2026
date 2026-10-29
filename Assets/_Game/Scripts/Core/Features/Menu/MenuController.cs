using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _Game.Scripts.Core.Features.Menu
{
    public class MenuController : MonoBehaviour
    {
        private void Update()
        {
            bool startPressed =
                (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (startPressed)
                SceneManager.LoadScene("Gameplay");
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(20f, 20f, 500f, 40f), "Press any key to start");
        }
    }
}
