using UnityEngine;

namespace Voxels.Runtime
{
    public sealed class PlayerToolState : MonoBehaviour
    {
        [SerializeField] PlayerToolMode activeTool = PlayerToolMode.StonePickaxe;
        [SerializeField] bool bucketFilled;

        public PlayerToolMode ActiveTool => activeTool;
        public bool BucketFilled => bucketFilled;

        public void CycleTool()
        {
            activeTool = activeTool switch
            {
                PlayerToolMode.Hand => PlayerToolMode.WoodenPickaxe,
                PlayerToolMode.WoodenPickaxe => PlayerToolMode.StonePickaxe,
                PlayerToolMode.StonePickaxe => PlayerToolMode.IronPickaxe,
                PlayerToolMode.IronPickaxe => PlayerToolMode.Shovel,
                PlayerToolMode.Shovel => PlayerToolMode.Bucket,
                _ => PlayerToolMode.Hand,
            };

            if (activeTool != PlayerToolMode.Bucket)
            {
                bucketFilled = false;
            }
        }

        public void SetTool(PlayerToolMode tool)
        {
            activeTool = tool;
            if (tool != PlayerToolMode.Bucket)
            {
                bucketFilled = false;
            }
        }

        public void SetBucketFilled(bool filled) => bucketFilled = filled;
    }
}
