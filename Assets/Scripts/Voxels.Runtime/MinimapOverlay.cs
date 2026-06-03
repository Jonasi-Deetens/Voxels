using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class MinimapOverlay : MonoBehaviour
    {
        [SerializeField] int mapRadiusHex = 12;
        [SerializeField] float cellPixels = 4f;
        [SerializeField] bool visible = true;

        WorldScroller scroller;
        WorldSettings settings;

        public void Initialize(WorldScroller worldScroller, WorldSettings worldSettings)
        {
            scroller = worldScroller;
            settings = worldSettings;
        }

        void OnGUI()
        {
            if (!visible || scroller == null || settings == null)
            {
                return;
            }

            HexCoord center = scroller.PlayerWorldHex;
            float size = mapRadiusHex * 2f + 1f;
            float mapSize = size * cellPixels;
            Rect mapRect = new Rect(Screen.width - mapSize - 16f, 16f, mapSize, mapSize);
            GUI.Box(mapRect, "Map");

            for (int dq = -mapRadiusHex; dq <= mapRadiusHex; dq++)
            {
                for (int dr = -mapRadiusHex; dr <= mapRadiusHex; dr++)
                {
                    HexCoord hex = center.Add(new HexCoord(dq, dr));
                    if (!scroller.HexWorld.IsInsideWorld(hex))
                    {
                        continue;
                    }

                    float elevation = settings.SeaLevelLayer;
                    if (scroller.HexWorld.TryGetColumn(hex, out BlockColumn column))
                    {
                        elevation = column.SurfaceHeight;
                    }

                    float normalized = Mathf.InverseLerp(
                        settings.SeaLevelLayer - 20f,
                        settings.SeaLevelLayer + 24f,
                        elevation);
                    Color color = Color.Lerp(new Color(0.15f, 0.35f, 0.75f), new Color(0.35f, 0.65f, 0.25f), normalized);

                    float px = mapRect.x + (dq + mapRadiusHex) * cellPixels;
                    float py = mapRect.y + (dr + mapRadiusHex) * cellPixels;
                    var cell = new Rect(px, py, cellPixels - 0.5f, cellPixels - 0.5f);
                    GUI.color = color;
                    GUI.DrawTexture(cell, Texture2D.whiteTexture);
                }
            }

            GUI.color = Color.white;
            float cx = mapRect.x + mapRadiusHex * cellPixels;
            float cy = mapRect.y + mapRadiusHex * cellPixels;
            GUI.Box(new Rect(cx, cy, cellPixels, cellPixels), string.Empty);
        }
    }
}
