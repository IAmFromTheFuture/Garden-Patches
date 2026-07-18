using UnityEngine;
using UnityEngine.UI;

namespace Patches.View
{
    public class FlowerTile : MonoBehaviour
    {
        [SerializeField] private Image _image;

        public int FlowerIndex { get; set; }

        private void Awake()
        {
            EnsureImageBound();
        }

        private void EnsureImageBound()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }

        public void SetSprite(Sprite sprite)
        {
            EnsureImageBound();
            if (_image != null)
            {
                _image.sprite = sprite;
            }
        }
    }
}
