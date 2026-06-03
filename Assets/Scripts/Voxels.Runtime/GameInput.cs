using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Voxels.Runtime
{
    static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static InputAction moveAction;
        static InputAction jumpAction;
        static InputAction sprintAction;
        static bool actionsResolved;
#endif

        public static Vector2 ReadMoveAxes()
        {
#if ENABLE_INPUT_SYSTEM
            EnsureActions();
            if (moveAction != null)
            {
                return moveAction.ReadValue<Vector2>();
            }

            return ReadKeyboardAxes();
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        public static bool WasJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            EnsureActions();
            if (jumpAction != null)
            {
                return jumpAction.WasPressedThisFrame();
            }

            Keyboard keyboard = GetKeyboard();
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool IsSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            EnsureActions();
            if (sprintAction != null)
            {
                return sprintAction.IsPressed();
            }

            Keyboard keyboard = GetKeyboard();
            return keyboard != null && keyboard.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        public static bool IsDescendHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = GetKeyboard();
            return keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
#endif
        }

        public static bool WasPrimaryPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool IsPrimaryHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        public static bool WasSecondaryPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        public static bool WasPickBlockPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(2);
#endif
        }

        public static bool WasDebugTogglePressedThisFrame() => WasKeyPressed(KeyCode.F3);

        public static bool WasCreativeTogglePressedThisFrame() => WasKeyPressed(KeyCode.F4);

        public static bool WasSavePressedThisFrame() => WasKeyPressed(KeyCode.F5);

        public static bool WasLoadPressedThisFrame() => WasKeyPressed(KeyCode.F6);

        public static bool WasCycleToolPressedThisFrame() => WasKeyPressed(KeyCode.T);

        public static bool WasCraftPressedThisFrame() => WasKeyPressed(KeyCode.G);

        public static bool WasHotbarSlotPressed(int index)
        {
            if (index < 0 || index > 8)
            {
                return false;
            }

            KeyCode key = KeyCode.Alpha1 + index;
            return WasKeyPressed(key) || WasKeyPressed(KeyCode.Keypad1 + index);
        }

        public static float ReadScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.scroll.ReadValue().y : 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        static bool WasKeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = GetKeyboard();
            if (keyboard == null)
            {
                return false;
            }

            return key switch
            {
                KeyCode.F3 => keyboard.f3Key.wasPressedThisFrame,
                KeyCode.F4 => keyboard.f4Key.wasPressedThisFrame,
                KeyCode.F5 => keyboard.f5Key.wasPressedThisFrame,
                KeyCode.F6 => keyboard.f6Key.wasPressedThisFrame,
                KeyCode.G => keyboard.gKey.wasPressedThisFrame,
                KeyCode.T => keyboard.tKey.wasPressedThisFrame,
                KeyCode.Alpha1 => keyboard.digit1Key.wasPressedThisFrame,
                KeyCode.Alpha2 => keyboard.digit2Key.wasPressedThisFrame,
                KeyCode.Alpha3 => keyboard.digit3Key.wasPressedThisFrame,
                KeyCode.Alpha4 => keyboard.digit4Key.wasPressedThisFrame,
                KeyCode.Alpha5 => keyboard.digit5Key.wasPressedThisFrame,
                KeyCode.Alpha6 => keyboard.digit6Key.wasPressedThisFrame,
                KeyCode.Alpha7 => keyboard.digit7Key.wasPressedThisFrame,
                KeyCode.Alpha8 => keyboard.digit8Key.wasPressedThisFrame,
                KeyCode.Alpha9 => keyboard.digit9Key.wasPressedThisFrame,
                KeyCode.Keypad1 => keyboard.numpad1Key.wasPressedThisFrame,
                KeyCode.Keypad2 => keyboard.numpad2Key.wasPressedThisFrame,
                KeyCode.Keypad3 => keyboard.numpad3Key.wasPressedThisFrame,
                KeyCode.Keypad4 => keyboard.numpad4Key.wasPressedThisFrame,
                KeyCode.Keypad5 => keyboard.numpad5Key.wasPressedThisFrame,
                KeyCode.Keypad6 => keyboard.numpad6Key.wasPressedThisFrame,
                KeyCode.Keypad7 => keyboard.numpad7Key.wasPressedThisFrame,
                KeyCode.Keypad8 => keyboard.numpad8Key.wasPressedThisFrame,
                KeyCode.Keypad9 => keyboard.numpad9Key.wasPressedThisFrame,
                _ => false,
            };
#else
            return Input.GetKeyDown(key);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static void EnsureActions()
        {
            if (actionsResolved)
            {
                return;
            }

            actionsResolved = true;
            InputActionAsset asset = InputSystem.actions;
            if (asset == null)
            {
                return;
            }

            InputActionMap playerMap = asset.FindActionMap("Player", false);
            if (playerMap == null)
            {
                return;
            }

            playerMap.Enable();
            moveAction = playerMap.FindAction("Move", false);
            jumpAction = playerMap.FindAction("Jump", false);
            sprintAction = playerMap.FindAction("Sprint", false);
            moveAction?.Enable();
            jumpAction?.Enable();
            sprintAction?.Enable();
        }

        static Vector2 ReadKeyboardAxes()
        {
            Keyboard keyboard = GetKeyboard();
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                y += 1f;
            }

            return new Vector2(x, y);
        }

        static Keyboard GetKeyboard()
        {
            return Keyboard.current ?? InputSystem.GetDevice<Keyboard>();
        }
#endif
    }
}
