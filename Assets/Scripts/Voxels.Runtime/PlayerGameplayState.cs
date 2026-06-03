using UnityEngine;

namespace Voxels.Runtime
{
    public sealed class PlayerGameplayState : MonoBehaviour
    {
        public static PlayerGameplayState Instance { get; private set; }

        [SerializeField] bool creativeMode;

        public bool CreativeMode
        {
            get => creativeMode;
            set => creativeMode = value;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ToggleCreativeMode()
        {
            creativeMode = !creativeMode;
        }
    }
}
