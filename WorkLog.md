# Work Log

## July 18, 2026

* **Nested Prefab Variant Support:** Updated the flower prefab generation workflow to use **Prefab Variants**. The 15 flower presets now inherit from a common `FlowerBase.prefab` (empty root with `FlowerTile`, child GameObject with `Image`), allowing global edits to propagate automatically.
* **Layout-Flexible Component Lookups:** Refactored `FlowerTile.cs` component resolution to search children (`GetComponentInChildren<Image>`), natively supporting child gameobject name changes (e.g., from `Image` to `Flower`).
* **Fixed Level Restart Destruction bug:** Updated `FlowerPoolManager.ReleaseFlower` to unparent recycled tiles before parent `PatchView` destruction on level load/restart.
* **Component Activation & Restoring Visuals:** Fixed issue where pool recycled gameobjects would have their child `Image` and root `Animator` components disabled upon parent destruction. Updated pool `actionOnGet` to recursively enable all `Animator`, `Image` components, and activate all child hierarchy GameObjects.
* **Triggered Placement Animations:** Integrated the `"Bloom"` trigger animation to fire automatically on placement in `PatchView.cs` when flower tiles are retrieved and aligned on the board.
* **Cleaned Up Setup Scripts:** Deleted the automatic layout-resetting `SetupScene.cs` script to protect custom UI panel hierarchy orders, canvas configurations, and panel prefabs.
* **Strict Git Controls Enforced:** Adhered to user policies by logging local changes and leaving staging, committing, and pushing entirely to the user.
