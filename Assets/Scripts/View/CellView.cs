using UnityEngine;

namespace Patches.View
{
    public class CellView : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public void Initialize(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
