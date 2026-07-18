# Game Design Document: Patches

## 1. Executive Summary
* **Game Title:** Patches (Working Title)
* **Genre:** Spatial Logic / Grid-Based Puzzle
* **Target Platforms:** Cross-platform Mobile & Web (Optimized for Unity UI Desktop/Mobile viewports)
* **Inspiration:** Inspired by LinkedIn Patches — LinkedIn's take on the Japanese classic Shikaku, designed by Principal Puzzlemaster Thomas Snyder.
* **Core Objective:** Partition an entire grid into rectangular patches so that every clue sits inside exactly one patch, every patch matches whatever its clue specifies (area and shape family), and every cell of the grid belongs to exactly one patch. No overlaps, no gaps. The puzzle is won when the board is 100% covered.

---

## 2. Core Gameplay Loops & Mechanics

### 2.1 The Grid Environment
The game is played on a 2D grid matrix of size Width x Height (standard sizes are 6x6, 10x10, or 12x12). The grid contains empty cells and a set of predefined static Clue Cells.

### 2.2 Clue Cell Anatomy
Each clue cell encodes up to two layout criteria:
1. **Area Constraint (Integer):** Specifies the exact total number of cells the resulting patch must occupy.
2. **Shape Constraint (Icon):** Specifies the geometric family/orientation of the rectangle.

### 2.3 Structural Shape Families
* **Square:** Width equals height (e.g., 1x1, 2x2, 3x3).
* **Tall:** Height is strictly greater than width (e.g., 1x2, 1x3, 2x3). Pure squares do not satisfy this constraint.
* **Wide:** Width is strictly greater than height (e.g., 2x1, 3x1, 3x2). Pure squares do not satisfy this constraint.
* **Freeform:** Any valid rectangle where Width * Height = Area (can be square, tall, or wide).

---

## 3. Player Interaction & Input State Machine

The input handling is managed via a strict Finite State Machine (FSM) to handle responsive drag-and-drop gameplay without input conflicts.

### 3.1 Drag-to-Draw Mechanic
* **Initiation:** The player presses down anywhere on the grid (on an empty cell or a clue cell) to establish an anchor point (Start Cell) and drags toward a Current Cell.
* **Dynamic Bounding Box Clamping (No-Overlap Rule):** While dragging, the temporary preview box dynamically calculates its boundaries. It automatically halts/clamps its growth at the edge of any cell already occupied by a locked **Valid Patch**. The user cannot physically drag over or clip pre-existing valid layouts.

### 3.2 Touch Release Tri-State Outcomes
When the pointer/finger is lifted, the system evaluates the bounds against three rules (Clue Count, Shape Type, and Area Size):

1. **Zero Clues Included:** The selection is immediately discarded. The preview box vanishes instantly with no change to data.
2. **Exactly One Clue Included + Fails Constraints:** The block is committed to the board as a **Transient Patch** (Grey Box).
3. **Exactly One Clue Included + Passes Constraints:** The block is committed to the board as a permanently locked **Valid Patch**.

### 3.3 The Transient Patch ("Grey Box") Lifecycle
To keep things simple (KISS principle), the board allows **only one** uncommitted Transient Patch to exist at a time.
* **Visual Presentation:** Renders in a distinct, neutral warning hue (Color B / Grey).
* **Space Obstruction:** It temporarily occupies space, blocking new drag previews from crossing into its territory.
* **Auto-Evaporation:** If the player starts a *new* drag action anywhere else on the board, the old Transient Patch is instantly destroyed, freeing its cells before the new drag logic executes.
* **Resizing Mode:** If the player clicks *inside* the active Transient Patch and drags, the system enters Block-Based Extrusion.

### 3.4 Block-Based Extrusion Math
When expanding or contracting a Transient Patch, the rectangle scales based on its structural unit dimensions rather than single-cell increments:
* Pulling a 2x1 block downward adds an entire row of 2 cells at a time (turning it into a 2x2 block).
* Pulling a 2x1 block to the right adds an entire column of 1 cell at a time (turning it into a 3x1 block).

### 3.5 Deletion Loop
* Performing a clean tap/click (Pointer Down + Pointer Up with zero drag distance) directly on a **Valid Patch** instantly deletes it.
* Its cells return to the open pool, and its assigned color ID is safely returned to the palette pool.

---

## 4. Visuals, Color Engine, & Win Loop

### 4.1 Color Allocation Engine
* **Transient State:** Rendered in a fixed configuration color (**Color B** / Grey).
* **Valid State:** Upon passing validation, the view requests a unique random color from the available **Unused Color Pool**. This guarantees that adjacent completed patches don't easily blend into the same color.

### 4.2 Win Conditions
The grid validates completion metrics automatically after every successful patch placement. The game is won when:
1. 100% of the cells in the grid matrix have an associated patch ID.
2. There are zero Transient Patches remaining on the board.
3. Every individual patch accurately fulfills its underlying shape strategy validation check.

### 4.3 Star Rating System
Upon winning, the player receives a 1-to-3 star rating based on completion speed against preset map time thresholds:
* **3 Stars (Gold):** Time <= Gold Threshold
* **2 Stars (Silver):** Time <= Silver Threshold
* **1 Star (Bronze):** Time > Silver Threshold