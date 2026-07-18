# Visual Design Specification: Garden Patches

## 1. Aesthetic Direction & Theme Mapping
The abstract puzzle grid is completely reimagined as a dynamic layout of soil plots. The gameplay progression transitions the board from a barren, un-tilled layout into a lush, vibrant, fully bloomed custom garden.

### State-to-Visual Translation Matrix

| Game Data State | Visual Metaphor | Visual Elements & Rendering |
| :--- | :--- | :--- |
| **Empty Cell** | Fresh Lawn / Fallow Grass | Soft green tile texturing with clean, subtle boundary grid lines. |
| **Clue Cell** | Sprout Signpost / Seed Marker | An elegant small wooden garden stake displaying the area number and the geometric constraint symbol. |
| **Transient Patch** | Tilled Mud / Raw Soil | Rich brown, tilled mud texture. No vegetation or sprouts populate this state due to unoptimized or invalid layout parameters. |
| **Valid Patch** | Blooming Flower Bed | The underlying tilled mud base transitions instantly, populating with fully bloomed flowers of a single distinct color. |

---

## 2. Camera, Perspective & Boundary Constraints

### 2.1 Strict 2D Top-Down Viewport
* **Perspective:** Complete 2D orthographic top-down presentation. Zero isometric tilt, angling, or 2.5D foreshortening. 
* **Grid Boundaries:** Border outlines must remain minimalist and exceptionally thin (1–2 pixels maximum relative to canvas design resolution). This maximizes real estate for the flora assets and ensures the grid graphics look crisp on high-density mobile viewports.

---

## 3. Animation & Juice Architecture (UI View Layer)

To maximize runtime performance within Unity UI (UGUI), visual transitions are driven via lightweight UI tweening engines (like DOTween or LeanTween) and sprite swap events rather than resource-heavy physics layers.

### 3.1 The "Extrusion" Animation (Dragging/Resizing)
* **Action:** Occurs as a player drags to draw or expand a mud patch block.
* **Visual Juice:** The new rows or columns of mud do not snap into place instantly. Instead, they scale up from 0 to 1 using an organic bounce curve (Elastic Ease-Out), making the soil appear to turn over dynamically.

### 3.2 The "Bloom" Animation (Transition to Valid)
* **Action:** Triggered the exact frame a transient patch passes its validation logic upon touch release.
* **Visual Juice:** 1. The background mud image color smoothly transitions or adjusts tones slightly to frame the active flowerbed.
    2. Individual 2D flower sprites pop from the center of each cell within the patch using a scale-up tween with cascading micro-delays (0.05 seconds between cells). This creates a natural, wave-like blooming progression across the custom rectangle.

### 3.3 The "Uproot" Animation (Deletion)
* **Action:** Occurs when a valid patch is tapped for removal.
* **Visual Juice:** The flowers rapidly scale down to 0 with a swift squash-and-stretch pop animation, and the background mud texture cross-fades back into standard green lawn tile graphics.

---

## 4. Technical Rendering & Prefab Tiling

To maintain excellent rendering performance and a clean asset pipeline, the view layer avoids stretching single large textures across variable patch shapes and instead utilizes cell-level instanced prefabs.

[ PatchView Container ]
├── [ Cell UI Slot (0,0) ] ──> Instantiates: Plant_Lavender_Prefab (x4 Tiled)
├── [ Cell UI Slot (1,0) ] ──> Instantiates: Plant_Lavender_Prefab (x4 Tiled)
└── [ Cell UI Slot (2,0) ] ──> Instantiates: Plant_Lavender_Prefab (x4 Tiled)

### 4.1 Prefab-Based Grid Tiling
* **Structure:** Each individual grid cell inside a Valid Patch acts as a localized container slot. It does not stretch a single image component across wide boundaries.
* **Modular Prefabs:** Plants are structured as lightweight UI prefabs containing pre-arranged, repeating 2D flat sprite collections (e.g., a neat 2x2 cluster of identical or slightly varied flower assets).
* **Instantiation Loop:** When a patch becomes valid, the `PatchView` loops through its coordinate matrix and spawns the assigned plant prefab variant directly into each cell slot container.
* **Color Palette Manager Integration:** The system's color palette manager passes the unassigned color choice straight to the instantiation loop, selecting the corresponding plant prefab variant or applying a precise material tint to the flower petal sprites.
* **Batching Optimization:** Because the layout runs identical repeating prefabs, Unity UI can bundle the matching graphics elements into single-pass draw operations, keeping performance metrics exceptionally high and draw calls low even on large 12x12 grid arrays.
