using Patches.Data;

namespace Patches.Domain
{
    public class Patch
    {
        public string Id { get; private set; }
        public PatchBounds Bounds { get; set; }
        public PatchState State { get; set; }
        public PuzzleClue Clue { get; private set; }

        public Patch(string id, PatchBounds bounds, PatchState state, PuzzleClue clue)
        {
            Id = id;
            Bounds = bounds;
            State = state;
            Clue = clue;
        }
    }
}
