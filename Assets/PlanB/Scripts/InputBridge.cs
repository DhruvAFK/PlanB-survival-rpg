using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlanB
{
    public static class InputBridge
    {
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                return Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0)), 1);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
#else
                return Vector2.zero;
#endif
            }
        }
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return new Vector2(Screen.width / 2f, Screen.height / 2f);
#endif
            }
        }
        public static bool ShootHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(0);
#else
                return false;
#endif
            }
        }
        public static bool ReloadPressed { get { return Key(0); } }
        public static bool InteractPressed { get { return Key(1); } }
        public static bool PausePressed { get { return Key(2); } }
        public static bool TrapPressed { get { return Key(3); } }
        public static bool BashPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1);
#else
                return false;
#endif
            }
        }
        static bool Key(int key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (key == 0 ? k.rKey.wasPressedThisFrame : key == 1 ? k.eKey.wasPressedThisFrame : key == 2 ? k.escapeKey.wasPressedThisFrame : k.tKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key == 0 ? KeyCode.R : key == 1 ? KeyCode.E : key == 2 ? KeyCode.Escape : KeyCode.T);
#else
            return false;
#endif
        }
    }
}
