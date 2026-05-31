using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float distance = 65000f;
        [SerializeField] float minDistance = 50000f;
        [SerializeField] float maxDistance = 180000f;
        [SerializeField] float orbitSpeed = 120f;
        [SerializeField] float scrollSensitivity = 40f;
        [SerializeField] float minPitch = -20f;
        [SerializeField] float maxPitch = 80f;

        float yaw;
        float pitch = 25f;

        void Start()
        {
            if (target == null)
            {
                var bootstrap = FindFirstObjectByType<PlanetBootstrap>();
                if (bootstrap != null)
                {
                    target = bootstrap.transform;
                }
            }

            ConfigureDistanceFromPlanet();
        }

        void ConfigureDistanceFromPlanet()
        {
            var bootstrap = FindFirstObjectByType<PlanetBootstrap>();
            if (bootstrap != null && bootstrap.PlanetWorld != null)
            {
                float outerRadius = bootstrap.PlanetWorld.ApproximateOuterRadius;
                minDistance = outerRadius * 1.08f;
                maxDistance = outerRadius * 3.5f;
                distance = outerRadius * 1.35f;
                return;
            }

            minDistance = 50000f;
            maxDistance = 180000f;
            distance = 65000f;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleInput();
            UpdateTransform();
        }

        void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.isPressed)
                {
                    Vector2 delta = Mouse.current.delta.ReadValue();
                    yaw += delta.x * orbitSpeed * Time.deltaTime;
                    pitch -= delta.y * orbitSpeed * Time.deltaTime;
                }
            }
#else
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
            }
#endif

            float scroll = ReadScrollDelta();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distance -= scroll * scrollSensitivity;
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed)
                {
                    distance -= scrollSensitivity * 8f * Time.deltaTime;
                }

                if (Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed)
                {
                    distance += scrollSensitivity * 8f * Time.deltaTime;
                }
            }
#else
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
            {
                distance -= scrollSensitivity * 8f * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
            {
                distance += scrollSensitivity * 8f * Time.deltaTime;
            }
#endif

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        static float ReadScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.scroll.ReadValue().y;
            }
#endif
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis("Mouse ScrollWheel");
#elif !ENABLE_INPUT_SYSTEM
            return Input.GetAxis("Mouse ScrollWheel");
#else
            return 0f;
#endif
        }

        void UpdateTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        }
    }
}
