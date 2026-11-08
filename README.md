# Eclipse Frontier — Unity vertical slice

Prototype based on `text.txt`. The project targets Unity 6 and builds its 3D forest and HUD at runtime from primitives, so it has no external asset dependencies.

## Run

1. Open this folder in Unity Hub using Unity `6000.0.35f1` or a compatible Unity 6 editor.
2. Open `Assets/_Project/Scenes/Veilwood.unity`.
3. Press Play.

Use **WASD** or the arrow keys to explore. Choose an encounter from the panel on the right. Selecting the Hollow Stag transports the Wayfarer to its own 3D arena; movement remains enabled during this boss encounter. In combat, use **Extract**, **Shake Flask** (a brief kneeling animation), **Defend**, and **Burst**. The action timer only decreases when an action is taken; gathered Essence converts into Gold, EXP, and Legacy Essence. Accumulate 100 Legacy Essence to create a Mini Clone. Progress is saved locally with PlayerPrefs.

## Prototype scope

This is a playable Unity vertical slice, not the complete 3D RPG production scope in the design document. It demonstrates the exploration-to-encounter loop, action-cost timer, Essence extraction, automatic reservoir distribution, enemy resistance/phases, partial rewards, leveling, and clone unlock. Art, animation, scene streaming, skill trees, companion collection, and production save infrastructure remain future work.
# game8
