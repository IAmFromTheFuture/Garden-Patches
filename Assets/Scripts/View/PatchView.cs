using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Patches.Domain;

namespace Patches.View
{
    public class PatchView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private RectTransform _rectTransform;
        
        private float _cellSize;
        private GridView _gridView;
        private readonly List<GameObject> _spawnedFlowers = new List<GameObject>();
        
        public string PatchId { get; private set; }
        public int FlowerIndex { get; private set; }

        public void Initialize(string patchId, PatchBounds bounds, float cellSize, Color assignedColor, GridView gridView)
        {
            PatchId = patchId;
            FlowerIndex = -1; // Not using flowers for this state
            _cellSize = cellSize;
            _gridView = gridView;
            
            ClearFlowers();

            if (_backgroundImage != null)
            {
                _backgroundImage.color = assignedColor;
            }
            
            if (_rectTransform != null)
            {
                _rectTransform.pivot = Vector2.zero;
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.zero;
            }

            UpdateSpatialPosition(bounds);
        }

        public void Initialize(string patchId, PatchBounds bounds, float cellSize, int flowerIndex, GridView gridView)
        {
            PatchId = patchId;
            FlowerIndex = flowerIndex;
            _cellSize = cellSize;
            _gridView = gridView;

            ClearFlowers();

            if (_backgroundImage != null)
            {
                // Rich tilled mud brown color for valid patches
                _backgroundImage.color = new Color(0.35f, 0.23f, 0.14f, 1f);
            }

            if (_rectTransform != null)
            {
                _rectTransform.pivot = Vector2.zero;
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.zero;
            }

            UpdateSpatialPosition(bounds);

            // Spawn flower tiles from the pool for each cell inside the patch
            if (FlowerPoolManager.Instance != null)
            {
                for (int y = bounds.MinY; y <= bounds.MaxY; y++)
                {
                    for (int x = bounds.MinX; x <= bounds.MaxX; x++)
                    {
                        int localX = x - bounds.MinX;
                        int localY = y - bounds.MinY;
                        
                        GameObject flower = FlowerPoolManager.Instance.GetFlower(flowerIndex, transform);
                        if (flower != null)
                        {
                            RectTransform rt = flower.GetComponent<RectTransform>();
                            rt.pivot = Vector2.zero;
                            rt.anchorMin = Vector2.zero;
                            rt.anchorMax = Vector2.zero;
                            rt.sizeDelta = new Vector2(_cellSize, _cellSize);
                            rt.anchoredPosition = new Vector2(localX * _cellSize, localY * _cellSize);
                            _spawnedFlowers.Add(flower);
                        }
                    }
                }
            }
        }

        public void UpdateSpatialPosition(PatchBounds bounds)
        {
            if (_rectTransform == null || _gridView == null) return;

            _rectTransform.sizeDelta = new Vector2(bounds.Width * _cellSize, bounds.Height * _cellSize);
            
            // Align position with grid's cell positions (taking centering offset into account)
            Vector2 position = _gridView.GetCellAnchoredPosition(bounds.MinX, bounds.MinY);
            _rectTransform.anchoredPosition = position;
        }

        private void OnDestroy()
        {
            ClearFlowers();
        }

        private void ClearFlowers()
        {
            if (FlowerPoolManager.Instance != null)
            {
                foreach (var flower in _spawnedFlowers)
                {
                    FlowerPoolManager.Instance.ReleaseFlower(flower);
                }
            }
            else
            {
                foreach (var flower in _spawnedFlowers)
                {
                    if (flower != null) Destroy(flower);
                }
            }
            _spawnedFlowers.Clear();
        }
    }
}
