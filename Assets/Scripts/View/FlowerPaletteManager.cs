using System.Collections.Generic;
using UnityEngine;

namespace Patches.View
{
    public class FlowerPaletteManager
    {
        private readonly List<int> _availableIndices = new List<int>();
        private readonly HashSet<int> _inUseIndices = new HashSet<int>();

        public FlowerPaletteManager(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _availableIndices.Add(i);
            }
        }

        public int ClaimUniqueIndex()
        {
            foreach (var index in _availableIndices)
            {
                if (!_inUseIndices.Contains(index))
                {
                    _inUseIndices.Add(index);
                    return index;
                }
            }
            // Fallback if we run out of unique indices
            if (_availableIndices.Count > 0)
            {
                return _availableIndices[Random.Range(0, _availableIndices.Count)];
            }
            return 0;
        }

        public void ReleaseIndex(int index)
        {
            _inUseIndices.Remove(index);
        }
    }
}
