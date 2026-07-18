using System;
using UnityEngine;
using Patches.Domain;

namespace Patches.Events
{
    public static class GridEvents
    {
        // Placed a locked valid patch: ID, Bounds, Color
        public static Action<string, PatchBounds, Color> OnPatchPlaced;

        // Transient patch updated: Bounds, IsValid shape
        public static Action<PatchBounds, bool> OnTransientPatchUpdated;

        // Transient patch cleared
        public static Action OnTransientPatchCleared;

        // Placed patch removed: ID, Bounds
        public static Action<string, PatchBounds> OnPatchRemoved;

        // Puzzle solved: Stars count (1-3)
        public static Action<int> OnPuzzleSolved;
    }
}
