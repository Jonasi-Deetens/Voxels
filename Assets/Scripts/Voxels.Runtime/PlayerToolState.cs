using UnityEngine;

namespace Voxels.Runtime
{
    public sealed class PlayerToolState : MonoBehaviour
    {
        [SerializeField] PlayerToolMode activeTool = PlayerToolMode.Pickaxe;

        public PlayerToolMode ActiveTool => activeTool;

        public void CycleTool()
        {
            activeTool = activeTool switch
            {
                PlayerToolMode.Hand => PlayerToolMode.Pickaxe,
                PlayerToolMode.Pickaxe => PlayerToolMode.Shovel,
                _ => PlayerToolMode.Hand,
            };
        }

        public void SetTool(PlayerToolMode tool) => activeTool = tool;
    }
}
