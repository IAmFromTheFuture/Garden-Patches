using System;
using System.Collections.Generic;
using UnityEngine;
using Patches.Data;
using Patches.Domain;

namespace Patches.Model
{
    public class GridModel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<PuzzleClue> Clues { get; private set; }
        public List<Patch> Patches { get; private set; }
        public Patch TransientPatch { get; private set; }

        // Core pure C# events
        public event Action<Patch> OnPatchAdded;
        public event Action<Patch> OnPatchRemoved;
        public event Action<Patch> OnTransientPatchUpdated;
        public event Action OnTransientPatchCleared;
        public event Action<int> OnSolved; // Fires with star count

        private int _goldTimeSeconds;
        private int _silverTimeSeconds;

        public void Initialize(int width, int height, List<PuzzleClue> clues, int goldTime, int silverTime)
        {
            Width = width;
            Height = height;
            Clues = new List<PuzzleClue>();
            foreach (var clue in clues)
            {
                if (clue.RequiredArea <= 1)
                {
                    Debug.LogError($"[GridModel] Clue at ({clue.X}, {clue.Y}) has a required area of {clue.RequiredArea}. Single cell goals are not allowed!");
                    continue;
                }
                Clues.Add(clue);
            }
            Patches = new List<Patch>();
            TransientPatch = null;
            _goldTimeSeconds = goldTime;
            _silverTimeSeconds = silverTime;
        }

        public bool IsCellOccupiedByValidPatch(int x, int y)
        {
            foreach (var patch in Patches)
            {
                if (patch.State == PatchState.Valid && patch.Bounds.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsCellOccupied(int x, int y)
        {
            if (TransientPatch != null && TransientPatch.Bounds.Contains(x, y))
            {
                return true;
            }
            return IsCellOccupiedByValidPatch(x, y);
        }

        public Patch GetPatchAt(int x, int y)
        {
            if (TransientPatch != null && TransientPatch.Bounds.Contains(x, y))
            {
                return TransientPatch;
            }

            foreach (var patch in Patches)
            {
                if (patch.Bounds.Contains(x, y))
                {
                    return patch;
                }
            }
            return null;
        }

        public List<PuzzleClue> GetCluesInBounds(PatchBounds bounds)
        {
            var result = new List<PuzzleClue>();
            foreach (var clue in Clues)
            {
                if (bounds.Contains(clue.X, clue.Y))
                {
                    result.Add(clue);
                }
            }
            return result;
        }

        public bool TryCommitPatch(PatchBounds bounds, out Patch committedPatch)
        {
            committedPatch = null;

            // Check if bounds are within the grid
            if (bounds.MinX < 0 || bounds.MaxX >= Width || bounds.MinY < 0 || bounds.MaxY >= Height)
            {
                return false;
            }

            // Check if bounds overlap with any locked Valid Patch
            foreach (var patch in Patches)
            {
                if (patch.State == PatchState.Valid && patch.Bounds.Overlaps(bounds))
                {
                    return false;
                }
            }

            // Get clues inside bounds
            var cluesInBounds = GetCluesInBounds(bounds);
            if (cluesInBounds.Count != 1)
            {
                // Zero or multiple clues: immediately discard
                ClearTransientPatch();
                return false;
            }

            var clue = cluesInBounds[0];
            var validator = ShapeValidatorFactory.GetValidator(clue.ShapeType);
            bool isValid = validator.Validate(bounds, clue.RequiredArea);

            if (isValid)
            {
                // Clear transient patch if we had one
                ClearTransientPatch();

                // Create locked valid patch
                string id = Guid.NewGuid().ToString();
                committedPatch = new Patch(id, bounds, PatchState.Valid, clue);
                Patches.Add(committedPatch);
                OnPatchAdded?.Invoke(committedPatch);

                return true;
            }
            else
            {
                // Fails constraints: Create or update Transient Patch
                string id = TransientPatch?.Id ?? Guid.NewGuid().ToString();
                TransientPatch = new Patch(id, bounds, PatchState.Transient, clue);
                committedPatch = TransientPatch;
                OnTransientPatchUpdated?.Invoke(TransientPatch);

                return true;
            }
        }

        public bool TryResizeTransientPatch(PatchBounds newBounds)
        {
            if (TransientPatch == null) return false;

            // Check bounds are within the grid
            if (newBounds.MinX < 0 || newBounds.MaxX >= Width || newBounds.MinY < 0 || newBounds.MaxY >= Height)
            {
                return false;
            }

            // Check if new bounds overlap with any locked Valid Patch
            foreach (var patch in Patches)
            {
                if (patch.State == PatchState.Valid && patch.Bounds.Overlaps(newBounds))
                {
                    return false;
                }
            }

            // Ensure the clue is still exactly inside the new bounds
            var cluesInBounds = GetCluesInBounds(newBounds);
            if (cluesInBounds.Count != 1 || cluesInBounds[0].X != TransientPatch.Clue.X || cluesInBounds[0].Y != TransientPatch.Clue.Y)
            {
                return false;
            }

            // Run validation
            var validator = ShapeValidatorFactory.GetValidator(TransientPatch.Clue.ShapeType);
            bool isValid = validator.Validate(newBounds, TransientPatch.Clue.RequiredArea);

            if (isValid)
            {
                // Promote to Valid patch!
                var promotedPatch = new Patch(TransientPatch.Id, newBounds, PatchState.Valid, TransientPatch.Clue);
                TransientPatch = null;
                OnTransientPatchCleared?.Invoke();

                Patches.Add(promotedPatch);
                OnPatchAdded?.Invoke(promotedPatch);
            }
            else
            {
                // Stay Transient
                TransientPatch.Bounds = newBounds;
                OnTransientPatchUpdated?.Invoke(TransientPatch);
            }

            return true;
        }

        public void ClearTransientPatch()
        {
            if (TransientPatch != null)
            {
                TransientPatch = null;
                OnTransientPatchCleared?.Invoke();
            }
        }

        public bool RemovePatch(string id)
        {
            // Try removing from valid patches
            for (int i = 0; i < Patches.Count; i++)
            {
                if (Patches[i].Id == id)
                {
                    var p = Patches[i];
                    Patches.RemoveAt(i);
                    OnPatchRemoved?.Invoke(p);
                    return true;
                }
            }

            // Try removing from transient patch
            if (TransientPatch != null && TransientPatch.Id == id)
            {
                ClearTransientPatch();
                return true;
            }

            return false;
        }

        public bool CheckWinCondition(int timeElapsedSeconds, out int starRating)
        {
            starRating = 0;

            // 1. Must have zero transient patches
            if (TransientPatch != null) return false;

            // 2. All placed patches must be valid
            foreach (var patch in Patches)
            {
                if (patch.State != PatchState.Valid) return false;
                var validator = ShapeValidatorFactory.GetValidator(patch.Clue.ShapeType);
                if (!validator.Validate(patch.Bounds, patch.Clue.RequiredArea)) return false;
            }

            // 3. 100% cell coverage
            // Calculate total area of all valid patches
            int totalCoveredArea = 0;
            foreach (var patch in Patches)
            {
                totalCoveredArea += patch.Bounds.Area;
            }

            int gridArea = Width * Height;
            if (totalCoveredArea != gridArea) return false;

            // If we got here, it's a win! Calculate star rating
            if (timeElapsedSeconds <= _goldTimeSeconds)
            {
                starRating = 3;
            }
            else if (timeElapsedSeconds <= _silverTimeSeconds)
            {
                starRating = 2;
            }
            else
            {
                starRating = 1;
            }

            OnSolved?.Invoke(starRating);
            return true;
        }
    }
}
