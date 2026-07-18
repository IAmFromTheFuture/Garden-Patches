# Phased Implementation Plan: Patches

This document divides the development of the Shikaku-like puzzle game **Patches** into modular, self-contained phases. Each phase lists key requirements, target files, and context pointers so that different development sessions or subagents can resume work with minimal context overhead.

## Game Configuration Decisions
1. **Level Configuration**: Implemented via Unity **ScriptableObjects** (`PuzzleLevelSO`).
2. **Assembly Definitions (.asmdef)**: Omitted to keep compilation simple and lightweight for this small project.
3. **Input Handling**: Managed using standard Unity UI EventSystem handlers (`IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler`) on grid cells or the canvas, mapping screen positions to 2D grid coordinates.

---

## Phase 1: Core Domain, Shape Validators & Unit Tests
**Goal**: Build the core, non-Unity-dependent C# structures, shape verification strategies, and unit tests to ensure validation is 100% accurate before building the UI.

### Scope & Targets:
* **Target Files**:
  * [ShapeType.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Domain/ShapeType.cs) (Enums: `ShapeType`, `PatchState`)
  * [PatchBounds.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Domain/PatchBounds.cs) (Struct: coordinate limits, width, height, area, bounds overlap checks)
  * [ShapeValidators.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Domain/ShapeValidators.cs) (`IShapeValidator` interface, `SquareValidator`, `TallValidator`, `WideValidator`, `FreeformValidator`)
  * [ValidatorTests.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Tests/ValidatorTests.cs) (Unit tests verifying validator correctness)
* **Context Pointer**: Code in these files must remain free of `UnityEngine` dependencies (except for basic editor testing assemblies) to support headless unit testing.

---

## Phase 2: ScriptableObject Levels & Grid Model Setup
**Goal**: Implement level definitions as ScriptableObjects, model the board state, and render the static empty board and clue layout in the scene.

### Scope & Targets:
* **Target Files**:
  * [PuzzleLevelSO.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Data/PuzzleLevelSO.cs) (ScriptableObject representing Level Width, Height, Clue Positions, Clue Area, Shape Family, and Star Time Thresholds)
  * [GridModel.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Model/GridModel.cs) (Tracks grid cell occupancy, clues, active valid patches, and current transient patch)
  * [GridView.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/View/GridView.cs) (Dynamically spawns Grid Cells and Clue Views matching the selected Level SO)
  * [ClueView.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/View/ClueView.cs) (Displays numbers and shape constraint icons)
* **Context Pointer**: Refer to `Assets/Scenes/SampleScene.unity` for the UI Canvas configuration. Cell positions and dimensions are calculated dynamically based on screen viewport sizes.

---

## Phase 3: FSM Input & Drag-to-Draw Engine
**Goal**: Build the drag-to-draw interaction layer where players select bounds, ensuring that previews clamp/halt at pre-existing patch boundaries.

### Scope & Targets:
* **Target Files**:
  * [InputPresenter.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Presenter/InputPresenter.cs) (Grid-space coordinate translation, pointer drag monitoring, FSM input states)
  * [GridEvents.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Events/GridEvents.cs) (Dispatches event hooks like `OnTransientPatchUpdated`, `OnTransientPatchCleared`)
* **Context Pointer**: The "No-Overlap Rule" logic must run in `InputPresenter.cs` during drag updates, checking candidate preview bounds against occupied cells in `GridModel.cs` and clamping the bounding box to prevent clipping.

---

## Phase 4: Patch Lifecycles & Extrusion Math
**Goal**: Handle committing dragged selections to either Transient (Grey) or Valid (Colored) states, block-based extrusion resizing, and patch deletions.

### Scope & Targets:
* **Target Files**:
  * [InputPresenter.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/Presenter/InputPresenter.cs) (Release outcome checks, block extrusion math adding whole rows/columns, delete-on-tap logic)
  * [PatchView.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/View/PatchView.cs) (Instantiated UI prefab representing placed patches)
  * [ColorPaletteManager.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/View/ColorPaletteManager.cs) (Unused color pool allocator)
* **Context Pointer**: When pointer release occurs, validation runs against clue constraints. Extrusion scales active blocks in the direction of the drag based on the unit dimensions of the block.

---

## Phase 5: Win Loop, Timer, & Polish
**Goal**: Build game completion triggers, time tracking, star scoring system, and simple UI panels for win conditions.

### Scope & Targets:
* **Target Files**:
  * [GameManager.cs](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scripts/View/GameManager.cs) (Level loader, timer logic, win loop coordinator)
  * UI Panels in [SampleScene.unity](file:///c:/Users/Rishi/Documents/Unity%20Projects/Patches/GardenPatch/Assets/Scenes/SampleScene.unity) (Win Overlay, Star displays, Timer UI)
* **Context Pointer**: Solver checks run in `GridModel` checking for 100% cell coverage, zero transient blocks, and accurate shape validations. GameManager then maps time elapsed against the loaded level's thresholds.
