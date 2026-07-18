using System.Collections.Generic;
using UnityEngine;

namespace Patches.View
{
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
            // Fallback if we run out of unique colors
            if (_availableColors.Count > 0)
            {
                return _availableColors[Random.Range(0, _availableColors.Count)];
            }
            return Color.white;
        }

        public void ReleaseColor(Color color)
        {
            _inUseColors.Remove(color);
        }
    }
}
