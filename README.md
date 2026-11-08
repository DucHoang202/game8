# Eclipse Frontier — Unity vertical slice

Prototype based on `text.txt`. The project targets Unity 6 and builds its 3D forest and HUD at runtime from primitives, so it has no external asset dependencies.

## Run

1. Open this folder in Unity Hub using Unity `6000.6.8f1` or a compatible Unity 6.6 editor.
2. Open `Assets/_Project/Scenes/Veilwood.unity`.
3. Press Play.

Movement uses Unity's Input System package (`1.20.1`), with WASD, arrow-key, and gamepad left-stick bindings. Choose an encounter from the panel on the right. Selecting the Hollow Stag transports the Wayfarer to its own 3D arena; movement remains enabled during this boss encounter. The boss HUD keeps the arena visible without a full-screen dark overlay. Combat has three actions: **Empty Flask** (kneel and shake out the last Essence), **Aerial Rune Strike** (vault onto the boss's armor as it topples onto its back), and **Grounding Shove** (a low forward-and-back guard break). The action timer only decreases when an action is taken; gathered Essence converts into Gold, EXP, and Legacy Essence. Accumulate 100 Legacy Essence to create a Mini Clone. Progress is saved locally with PlayerPrefs.

## Import a Sketchfab / Blender character

1. Import the character into Unity as FBX (recommended). A native `.blend` file can also be imported if Blender is installed and configured for Unity.
2. For a rigged character, set the FBX **Rig > Animation Type** to **Humanoid**, then Apply. A T-pose model can be used without an animation controller.
3. Select the imported model's root asset in the Project window and choose **Eclipse Frontier > Use Selected Model as Main Character**.
4. The tool creates `Assets/_Project/Resources/PlayerCharacter.prefab`; the game loads and scales it automatically on the next Play. Remove that prefab to return to the capsule placeholder.

## Prototype scope

This is a playable Unity vertical slice, not the complete 3D RPG production scope in the design document. It demonstrates the exploration-to-encounter loop, action-cost timer, Essence extraction, automatic reservoir distribution, enemy resistance/phases, partial rewards, leveling, and clone unlock. Imported characters are shown in their imported pose and follow the player root; full humanoid animation retargeting, scene streaming, skill trees, companion collection, and production save infrastructure remain future work.
# game8
