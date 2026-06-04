# Voxels — manual QA checklist

Run in Unity 6 (`6000.4.x`) after pulling the branch. Play **SampleScene** with **Voxels → Setup Default Content** if assets are missing.

## Boot & world
- [ ] Scene loads without console errors
- [ ] World build overlay completes; player spawns on solid ground
- [ ] F3 debug shows hex coords, chunks, sun/time-of-day, weather line

## Movement & camera
- [ ] WASD move, sprint (Shift), jump (Space), swim in water
- [ ] F4 creative: fly (Space/Ctrl), no break cost

## Blocks
- [ ] LMB break (progress bar when targeting); survival drops pickup
- [ ] RMB place consumes hotbar stack in survival
- [ ] MMB pick copies block to hotbar
- [ ] Dirty edits persist after walking away and returning (chunk reload)

## Save / load (F5 / F6)
- [ ] Break a block, F5 save, F6 load — block stays broken
- [ ] Weather and player position restore after load (manifest v3)

## Stats (survival)
- [ ] Health / stamina / hunger bars visible (hidden in creative)
- [ ] Sprint drains stamina; regen when idle
- [ ] Hunger drains over time; **E** eats fungus/grass from hotbar
- [ ] Deep water drains breath; drowning damage at 0
- [ ] Night creatures deal damage; health regens when fed and safe
- [ ] Death respawns at world spawn with partial health
- [ ] F5/F6 preserves health, stamina, hunger, breath (save v4)

## Tools (T cycles)
- [ ] Wooden / stone / iron pickaxe speeds on stone
- [ ] Shovel speeds on dirt/sand
- [ ] Bucket: RMB on water fills; RMB places water when filled

## Crafting (G or HUD Craft — no recipe list shown)
- [ ] With 2 dirt in inventory, craft produces stone (feedback text only)
- [ ] "Can't craft" when missing ingredients

## Weather & sky
- [ ] Hex clouds drift; weather changes across biomes or over time
- [ ] Rain/snow particles; rain/wind audio in wet weather
- [ ] Storm: brief sky flashes; fog thickens

## Night
- [ ] Night creatures spawn in dark biomes; despawn at dawn
- [ ] Creatures chase and damage player (health in HUD)

## Settings (Escape)
- [ ] Menu toggles fog, weather, cloud density
- [ ] Performance preset Low/Balanced/High applies (view distance feel)

## Automated (Editor Test Runner)
- [ ] **Voxels.Tests** — all Edit Mode tests green
- [ ] **Voxels.PlayModeTests** — run if Play Mode batch available
