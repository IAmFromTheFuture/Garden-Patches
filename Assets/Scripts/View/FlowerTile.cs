using UnityEngine;
using UnityEngine.UI;

namespace Patches.View
{
    public class FlowerTile : MonoBehaviour
    {
        [SerializeField] private Image _image;

        public int FlowerIndex { get; set; }

        private Animator _animator;

        private void Awake()
        {
            EnsureImageBound();
            _animator = GetComponent<Animator>();
        }

        private void EnsureImageBound()
        {
            if (_image == null)
            {
                _image = GetComponentInChildren<Image>();
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

        public void TriggerBloom()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            if (_animator != null)
            {
                _animator.SetTrigger("Bloom");
            }
        }
    }
}
