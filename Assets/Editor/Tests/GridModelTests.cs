using System.Collections.Generic;
using NUnit.Framework;
using Patches.Data;
using Patches.Domain;
using Patches.Model;

namespace Patches.Tests
{
    [TestFixture]
    public class GridModelTests
    {
        private GridModel _model;
        private List<PuzzleClue> _clues;

        [SetUp]
        public void Setup()
        {
            _model = new GridModel();
            _clues = new List<PuzzleClue>
            {
                new PuzzleClue(0, 0, 4, ShapeType.Square), // Square at (0,0)
                new PuzzleClue(2, 2, 2, ShapeType.Tall),    // Tall rect at (2,2)
                new PuzzleClue(0, 2, 2, ShapeType.Wide)     // Wide rect at (0,2)
            };
            // 3x3 Grid
            _model.Initialize(3, 3, _clues, 30, 60);
        }

        [Test]
        public void Initialize_SetsGridPropertiesCorrectly()
        {
            Assert.AreEqual(3, _model.Width);
            Assert.AreEqual(3, _model.Height);
            Assert.AreEqual(3, _model.Clues.Count);
            Assert.IsEmpty(_model.Patches);
            Assert.IsNull(_model.TransientPatch);
        }

        [Test]
        public void TryCommitPatch_WithNoClues_FailsAndDiscards()
        {
            // Bounds covering (1,0) to (2,1) contains no clues
            var bounds = new PatchBounds(1, 2, 0, 1);
            bool result = _model.TryCommitPatch(bounds, out var patch);

            Assert.IsFalse(result);
            Assert.IsNull(patch);
            Assert.IsNull(_model.TransientPatch);
        }

        [Test]
        public void TryCommitPatch_WithMultipleClues_FailsAndDiscards()
        {
            // Bounds covering (0,0) to (0,2) contains two clues: (0,0) and (0,2)
            var bounds = new PatchBounds(0, 0, 0, 2);
            bool result = _model.TryCommitPatch(bounds, out var patch);

            Assert.IsFalse(result);
            Assert.IsNull(patch);
            Assert.IsNull(_model.TransientPatch);
        }

        [Test]
        public void TryCommitPatch_WithOneCluePassingConstraints_CreatesValidPatch()
        {
            // Clue at (0,0) requires Square of area 4 (2x2)
            var bounds = new PatchBounds(0, 1, 0, 1);
            bool result = _model.TryCommitPatch(bounds, out var patch);

            Assert.IsTrue(result);
            Assert.IsNotNull(patch);
            Assert.AreEqual(PatchState.Valid, patch.State);
            Assert.AreEqual(1, _model.Patches.Count);
            Assert.IsNull(_model.TransientPatch);
            Assert.IsTrue(_model.IsCellOccupied(0, 0));
            Assert.IsTrue(_model.IsCellOccupied(1, 1));
            Assert.IsFalse(_model.IsCellOccupied(2, 2));
        }

        [Test]
        public void TryCommitPatch_WithOneClueFailingConstraints_CreatesTransientPatch()
        {
            // Clue at (0,0) requires Square of area 4. We draw a 2x1 block (area 2).
            var bounds = new PatchBounds(0, 1, 0, 0);
            bool result = _model.TryCommitPatch(bounds, out var patch);

            Assert.IsTrue(result);
            Assert.IsNotNull(patch);
            Assert.AreEqual(PatchState.Transient, patch.State);
            Assert.IsEmpty(_model.Patches); // Valid patches list remains empty
            Assert.IsNotNull(_model.TransientPatch);
            Assert.AreEqual(patch.Id, _model.TransientPatch.Id);
            Assert.IsTrue(_model.IsCellOccupied(0, 0));
            Assert.IsFalse(_model.IsCellOccupied(0, 1));
        }

        [Test]
        public void TryResizeTransientPatch_PromotesToValidWhenConstraintsMet()
        {
            // 1. Draw 2x1 block at (0,0) -> creates Transient Patch
            var bounds1 = new PatchBounds(0, 1, 0, 0);
            _model.TryCommitPatch(bounds1, out var transientPatch);
            Assert.IsNotNull(_model.TransientPatch);

            // 2. Resize transient patch to 2x2 -> meets Square(4) constraint
            var bounds2 = new PatchBounds(0, 1, 0, 1);
            bool result = _model.TryResizeTransientPatch(bounds2);

            Assert.IsTrue(result);
            Assert.IsNull(_model.TransientPatch); // Cleaned up
            Assert.AreEqual(1, _model.Patches.Count);
            Assert.AreEqual(PatchState.Valid, _model.Patches[0].State);
        }

        [Test]
        public void RemovePatch_DeletesValidAndTransientPatches()
        {
            // Commit a valid patch
            var bounds = new PatchBounds(0, 1, 0, 1);
            _model.TryCommitPatch(bounds, out var validPatch);
            Assert.AreEqual(1, _model.Patches.Count);

            // Remove it
            bool removed = _model.RemovePatch(validPatch.Id);
            Assert.IsTrue(removed);
            Assert.IsEmpty(_model.Patches);
            Assert.IsFalse(_model.IsCellOccupied(0, 0));
        }

        [Test]
        public void CheckWinCondition_ReturnsTrueOnlyWhenFullyCoveredAndValid()
        {
            // 3x3 board. Clues:
            // (0,0) Square 4 -> needs 2x2. Bounds: (0,0) to (1,1)
            // (0,2) Wide 2 -> needs 2x1. Bounds: (0,2) to (1,2)
            // (2,2) Tall 3 -> needs 1x3. Bounds: (2,0) to (2,2)
            // For a 3x3 grid to be 100% covered, the sum of areas of all patches must be 9.
            // Clue 1: (0,0) Square 4 -> (0,0) to (1,1)
            // Clue 2: (0,2) Wide 2 -> (0,2) to (1,2)
            // Clue 3: (2,2) Tall 3 -> (2,0) to (2,2)
            var winClues = new List<PuzzleClue>
            {
                new PuzzleClue(0, 0, 4, ShapeType.Square), // (0,0) to (1,1)
                new PuzzleClue(0, 2, 2, ShapeType.Wide),   // (0,2) to (1,2)
                new PuzzleClue(2, 2, 3, ShapeType.Tall)    // (2,0) to (2,2)
            };
            _model.Initialize(3, 3, winClues, 30, 60);

            // Placements
            Assert.IsTrue(_model.TryCommitPatch(new PatchBounds(0, 1, 0, 1), out _)); // Square 4
            Assert.IsTrue(_model.TryCommitPatch(new PatchBounds(0, 1, 2, 2), out _)); // Wide 2
            Assert.IsTrue(_model.TryCommitPatch(new PatchBounds(2, 2, 0, 2), out _)); // Tall 3

            // Check win condition
            bool won = _model.CheckWinCondition(20, out int stars);
            Assert.IsTrue(won);
            Assert.AreEqual(3, stars); // Gold rating (20s <= 30s)
        }
    }
}
