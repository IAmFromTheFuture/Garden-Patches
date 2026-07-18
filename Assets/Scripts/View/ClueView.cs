using UnityEngine;
using TMPro;
using Patches.Domain;

namespace Patches.View
{
    public class ClueView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _areaText;
        [SerializeField] private TextMeshProUGUI _shapeTypeText;

        public void Initialize(int area, ShapeType shapeType)
        {
            if (_areaText != null)
            {
                _areaText.text = area.ToString();
            }

            if (_shapeTypeText != null)
            {
                switch (shapeType)
                {
                    case ShapeType.Square:
                        _shapeTypeText.text = "■"; // Unicode square
                        break;
                    case ShapeType.Tall:
                        _shapeTypeText.text = "▮"; // Unicode tall rectangle
                        break;
                    case ShapeType.Wide:
                        _shapeTypeText.text = "▬"; // Unicode wide rectangle
                        break;
                    case ShapeType.Freeform:
                        _shapeTypeText.text = ""; // Freeform has no special icon / blank
                        break;
                }
            }
        }
    }
}
