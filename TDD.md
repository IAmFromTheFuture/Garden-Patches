# Technical Design Document: Patches Architecture Blueprint

## 1. Architectural Pattern (MVP)
The engine utilizes a decoupled **Model-View-Presenter (MVP)** architecture to isolate puzzle logic from visual representation. 

The Model consists of pure C# classes with zero dependencies on Unity engine code (`MonoBehaviour`, `UnityEngine.UI`, etc.). This makes the core logic completely testable via standard automated unit tests.

[ Input Presenter ] <─── Consumes Unity UI Event Data
     │             │
     ▼ (Mutates)   ▼ (Fires Layout State Messages)
[ Grid Model ]     [ Grid View / Patch View Canvas Component ]

---

## 2. Data Structures & Domain Layer

### 2.1 Domain Enums and Bounds Struct
```csharp
public enum ShapeType { Square, Tall, Wide, Freeform }
public enum PatchState { Valid, Transient }

[System.Serializable]
public struct PatchBounds
{
    public int MinX;
    public int MaxX;
    public int MinY;
    public int MaxY;

    public int Width => (MaxX - MinX) + 1;
    public int Height => (MaxY - MinY) + 1;
    public int Area => Width * Height;

    public bool Contains(int x, int y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
}

2.2 Data Transfer Objects & Serialization (DTOs)

Implements decoupled localized JSON data fetching schemas to support straightforward level design updates.

[System.Serializable]
public class PuzzleClueDto
{
    public int x;
    public int y;
    public int requiredArea;
    public string shapeType;
}

[System.Serializable]
public class PuzzleLevelDto
{
    public string puzzleId;
    public int gridWidth;
    public int gridHeight;
    public int goldTimeSeconds;
    public int silverTimeSeconds;
    public System.Collections.Generic.List<PuzzleClueDto> clues;
}

public interface IGridDataProvider
{
    PuzzleLevelDto LoadPuzzle(string puzzleId);
}

3. Game Subsystems
3.1 Shape Validation Engine (Strategy Pattern)

Adheres strictly to the Open/Closed Principle (SOLID). New layout conditions can be implemented down the line without changing existing layout manager loops.

public interface IShapeValidator
{
    bool Validate(PatchBounds bounds, int requiredArea);
}

public class SquareValidator : IShapeValidator
{
    public bool Validate(PatchBounds bounds, int requiredArea) => 
        bounds.Width == bounds.Height && bounds.Area == requiredArea;
}

public class TallValidator : IShapeValidator
{
    public bool Validate(PatchBounds bounds, int requiredArea) => 
        bounds.Height > bounds.Width && bounds.Area == requiredArea;
}

public class WideValidator : IShapeValidator
{
    public bool Validate(PatchBounds bounds, int requiredArea) => 
        bounds.Width > bounds.Height && bounds.Area == requiredArea;
}

public class FreeformValidator : IShapeValidator
{
    public bool Validate(PatchBounds bounds, int requiredArea) => 
        bounds.Area == requiredArea;
}

3.2 Decoupled Native C# Event Broker

Manages data communication across layout boundaries cleanly without tight component dependencies.

public static class GridEvents
{
    public static System.Action<string, PatchBounds, UnityEngine.Color> OnPatchPlaced;
    public static System.Action<PatchBounds, bool> OnTransientPatchUpdated; // Bounds, IsValid
    public static System.Action OnTransientPatchCleared;
    public static System.Action<string, PatchBounds> OnPatchRemoved;
    public static System.Action<int> OnPuzzleSolved; // Star count
}

4. View Presentation Layer (Unity UI)

The game view runs inside a Unity UI Canvas set to Screen Space - Camera. The layout coordinates map directly to our data structures by aligning the UI Prefab RectTransform Anchors to the Bottom-Left corner (0,0).

4.1 Responsive Patch View Component

using UnityEngine;
using UnityEngine.UI;

public class PatchView : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private RectTransform _rectTransform;
    
    private float _cellSize;
    public string PatchId { get; private set; }

    public void Initialize(string patchId, PatchBounds bounds, float cellSize, Color assignedColor)
    {
        PatchId = patchId;
        _cellSize = cellSize;
        _backgroundImage.color = assignedColor;
        
        // Pivot and Anchors must be locked to (0,0) Bottom-Left in the UI prefab
        _rectTransform.pivot = Vector2.zero;
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.zero;

        UpdateSpatialPosition(bounds);
    }

    public void UpdateSpatialPosition(PatchBounds bounds)
    {
        _rectTransform.sizeDelta = new Vector2(bounds.Width * _cellSize, bounds.Height * _cellSize);
        _rectTransform.anchoredPosition = new Vector2(bounds.MinX * _cellSize, bounds.MinY * _cellSize);
    }
}

4.2 Presentation Palette Manager

Controls board runtime colors to ensure adjacent placements maintain distinct profiles.

using System.Collections.Generic;
using UnityEngine;

public class ColorPaletteManager
{
    private readonly List<Color> _availableColors;
    private readonly HashSet<Color> _inUseColors = new HashSet<Color>();

    public ColorPaletteManager(List<Color> themePalette)
    {
        _availableColors = new List<Color>(themePalette);
    }

    public Color ClaimUniqueColor()
    {
        foreach (var color in _availableColors)
        {
            if (!_inUseColors.Contains(color))
            {
                _inUseColors.Add(color);
                return color;
            }
        }
        return _availableColors[Random.Range(0, _availableColors.Count)]; // Fallback
    }

    public void ReleaseColor(Color color)
    {
        _inUseColors.Remove(color);
    }
}

