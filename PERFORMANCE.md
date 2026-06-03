# Performance presets

`WorldSettings.ApplyPerformancePreset()` maps **Low / Medium / High** to streaming and mesh build knobs.

| Preset | View radius | Column cache | Background mesh | Greedy columns |
|--------|-------------|--------------|-----------------|----------------|
| Low    | 2 chunks    | 4096 cells   | Off             | On             |
| Medium | 3 chunks    | 8192 cells   | On              | On             |
| High   | 4 chunks    | 12288 cells  | On              | On             |

## Tuning tips

- **Chunk mesh padding** (`chunkMeshPadding`): raise if you see cracks at chunk borders; costs more vertices.
- **Build frame budget** (`buildFrameBudgetMs`): lower for smoother camera on weak GPUs; higher for faster initial load.
- **Greedy column meshing** (`useGreedyColumnMeshing`): fewer quads on flat terrain; disable when debugging per-block lighting.
- **Infinite worlds**: region saves under `persistentDataPath/voxels_world_save/regions/`; keep `columnCacheMaxCells` high enough for your travel speed.
- **Water**: `WaterSpreadUtility` runs at generation; `WaterFlowUtility.SettleAround` runs only on player edits (cheap local settle).

Use the in-game HUD (F3) and `WorldRuntimeProfiler` build summary to watch chunk queue depth and mesh build milliseconds.
