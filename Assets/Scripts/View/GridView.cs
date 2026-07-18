using System.Collections.Generic;
using UnityEngine;
using Patches.Data;
using Patches.Domain;

namespace Patches.View
{
    public class GridView : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private ClueView _cluePrefab;

        [Header("Containers")]
        [SerializeField] private RectTransform _gridContainer;

        public float CellSize { get; private set; }
        
        private readonly List<CellView> _spawnedCells = new List<CellView>();
        private readonly List<ClueView> _spawnedClues = new List<ClueView>();

        private RectTransform _cellsRoot;
        private RectTransform _patchesRoot;
        private RectTransform _cluesRoot;

        public RectTransform PatchesRoot
        {
            get
            {
                EnsureContainersExist();
                return _patchesRoot;
            }
        }

        private int _gridWidth;
        private int _gridHeight;

        private void EnsureContainersExist()
        {
            if (_gridContainer == null) return;

            _cellsRoot = GetOrCreateContainer("Cells_Root");
            _patchesRoot = GetOrCreateContainer("Patches_Root");
            _cluesRoot = GetOrCreateContainer("Clues_Root");

            _cellsRoot.SetSiblingIndex(0);
            _patchesRoot.SetSiblingIndex(1);
            _cluesRoot.SetSiblingIndex(2);
        }

        private RectTransform GetOrCreateContainer(string name)
        {
            Transform t = _gridContainer.Find(name);
            if (t != null)
            {
                return t.GetComponent<RectTransform>();
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_gridContainer, false);
            
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = _gridContainer.pivot;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            return rt;
        }

        public void GenerateGrid(int width, int height, List<PuzzleClue> clues)
        {
            _gridWidth = width;
            _gridHeight = height;

            ClearGrid();

            if (_gridContainer == null)
            {
                Debug.LogError("Grid container RectTransform is not assigned on GridView!");
                return;
            }

            EnsureContainersExist();

            // Calculate dynamic cell size to fit the container bounds
            float containerWidth = _gridContainer.rect.width;
            float containerHeight = _gridContainer.rect.height;

            float cellWidth = containerWidth / width;
            float cellHeight = containerHeight / height;

            // Maintain square cells
            CellSize = Mathf.Min(cellWidth, cellHeight);

            // Calculate offset to center the grid within the container
            float gridTotalWidth = CellSize * width;
            float gridTotalHeight = CellSize * height;
            Vector2 startOffset = new Vector2(
                (containerWidth - gridTotalWidth) / 2f,
                (containerHeight - gridTotalHeight) / 2f
            );

            // Spawn cell backgrounds
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CellView cell = Instantiate(_cellPrefab, _cellsRoot);
                    cell.name = $"Cell_{x}_{y}";
                    cell.Initialize(x, y);

                    RectTransform rt = cell.GetComponent<RectTransform>();
                    rt.pivot = Vector2.zero;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.zero;
                    rt.sizeDelta = new Vector2(CellSize, CellSize);
                    rt.anchoredPosition = startOffset + new Vector2(x * CellSize, y * CellSize);

                    _spawnedCells.Add(cell);
                }
            }

            // Spawn clue views
            foreach (var clue in clues)
            {
                ClueView clueObj = Instantiate(_cluePrefab, _cluesRoot);
                clueObj.name = $"Clue_{clue.X}_{clue.Y}";
                clueObj.Initialize(clue.RequiredArea, clue.ShapeType);

                RectTransform rt = clueObj.GetComponent<RectTransform>();
                rt.pivot = Vector2.zero;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.sizeDelta = new Vector2(CellSize, CellSize);
                rt.anchoredPosition = startOffset + new Vector2(clue.X * CellSize, clue.Y * CellSize);

                _spawnedClues.Add(clueObj);
            }
        }

        public Vector2 GetCellAnchoredPosition(int x, int y)
        {
            if (_gridContainer == null) return Vector2.zero;

            float gridTotalWidth = CellSize * _gridWidth;
            float gridTotalHeight = CellSize * _gridHeight;
            Vector2 startOffset = new Vector2(
                (_gridContainer.rect.width - gridTotalWidth) / 2f,
                (_gridContainer.rect.height - gridTotalHeight) / 2f
            );

            return startOffset + new Vector2(x * CellSize, y * CellSize);
        }

        public Vector2 GetGridStartOffset()
        {
            if (_gridContainer == null) return Vector2.zero;
            float gridTotalWidth = CellSize * _gridWidth;
            float gridTotalHeight = CellSize * _gridHeight;
            return new Vector2(
                (_gridContainer.rect.width - gridTotalWidth) / 2f,
                (_gridContainer.rect.height - gridTotalHeight) / 2f
            );
        }

        public CellView GetCellAt(int x, int y)
        {
            foreach (var cell in _spawnedCells)
            {
                if (cell.X == x && cell.Y == y) return cell;
            }
            return null;
        }

        public void ClearGrid()
        {
            foreach (var cell in _spawnedCells)
            {
                if (cell != null) Destroy(cell.gameObject);
            }
            _spawnedCells.Clear();

            foreach (var clue in _spawnedClues)
            {
                if (clue != null) Destroy(clue.gameObject);
            }
            _spawnedClues.Clear();
        }
    }
}
