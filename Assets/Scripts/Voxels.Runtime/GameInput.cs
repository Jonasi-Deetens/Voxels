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

        public static bool WasPrimaryPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
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

        public static bool WasDebugTogglePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = GetKeyboard();
            return keyboard != null && keyboard.f3Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F3);
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
