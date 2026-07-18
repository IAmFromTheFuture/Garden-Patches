using System.Collections.Generic;
using UnityEngine;
using Patches.Domain;

namespace Patches.Data
{
    [System.Serializable]
    public struct PuzzleClue
    {
        public int X;
        public int Y;
        public int RequiredArea;
        public ShapeType ShapeType;

        public PuzzleClue(int x, int y, int requiredArea, ShapeType shapeType)
        {
            X = x;
            Y = y;
            RequiredArea = requiredArea;
            ShapeType = shapeType;
        }
    }

    [CreateAssetMenu(fileName = "NewLevel", menuName = "Patches/Level Data", order = 1)]
    public class PuzzleLevelSO : ScriptableObject
    {
        public string PuzzleId;
        public int GridWidth;
        public int GridHeight;
        public int GoldTimeSeconds;
        public int SilverTimeSeconds;
        public List<PuzzleClue> Clues = new List<PuzzleClue>();
    }
}
