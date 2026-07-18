using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Patches.Domain;
using Patches.Model;
using Patches.View;
using Patches.Events;

namespace Patches.Presenter
{
    public class InputPresenter : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private enum FsmState { Idle, DrawingNew, Extruding }

        [Header("References")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private PatchView _patchPrefab;
        [SerializeField] private RectTransform _gridContainer;

        [Header("Color Options")]
        [SerializeField] private Color _transientColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Color B / Grey
        [SerializeField] private Color _previewColor = new Color(0.2f, 0.6f, 0.9f, 0.4f);   // Transparent blue
        [SerializeField] private List<Color> _themeColors = new List<Color>
        {
            new Color(0.9f, 0.3f, 0.3f), // Soft Red
            new Color(0.3f, 0.8f, 0.3f), // Soft Green
            new Color(0.9f, 0.7f, 0.2f), // Soft Gold
            new Color(0.6f, 0.3f, 0.9f), // Soft Purple
            new Color(0.2f, 0.8f, 0.8f)  // Soft Cyan
        };

        private GridModel _model;
        private FlowerPaletteManager _paletteManager;
        private FsmState _state = FsmState.Idle;

        // Visual Instantiated Trackers
        private readonly Dictionary<string, PatchView> _activePatchViews = new Dictionary<string, PatchView>();
        private PatchView _transientPatchView;
        private PatchView _previewPatchView;

        // Interaction State Trackers
        private Vector2Int _drawStartCell;
        private Vector2Int _extrusionStartCell;
        private PatchBounds _anchorBounds;
        private PatchBounds _currentPreviewBounds;
        
        private string _clickedPatchId;
        private bool _isClickOnly;
        private Vector2Int _lastLoggedCell = new Vector2Int(-99, -99);

        public void Initialize(GridModel model)
        {
            ClearAllActiveViews();

            _model = model;
            _paletteManager = new FlowerPaletteManager(15);

            // Register to model changes
            _model.OnPatchAdded += HandlePatchAdded;
            _model.OnPatchRemoved += HandlePatchRemoved;
            _model.OnTransientPatchUpdated += HandleTransientPatchUpdated;
            _model.OnTransientPatchCleared += HandleTransientPatchCleared;

            // Instantiate drag preview (hidden initially)
            if (_patchPrefab != null)
            {
                _previewPatchView = Instantiate(_patchPrefab, _gridView.PatchesRoot);
                _previewPatchView.name = "Drag_Preview";
                _previewPatchView.gameObject.SetActive(false);
            }
        }

        public void ClearAllActiveViews()
        {
            // Destroy all active patch views
            foreach (var kvp in _activePatchViews)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _activePatchViews.Clear();

            // Destroy transient patch view
            if (_transientPatchView != null)
            {
                Destroy(_transientPatchView.gameObject);
                _transientPatchView = null;
            }

            // Destroy preview patch view
            if (_previewPatchView != null)
            {
                Destroy(_previewPatchView.gameObject);
                _previewPatchView = null;
            }

            // Unsubscribe from old model if it exists
            if (_model != null)
            {
                _model.OnPatchAdded -= HandlePatchAdded;
                _model.OnPatchRemoved -= HandlePatchRemoved;
                _model.OnTransientPatchUpdated -= HandleTransientPatchUpdated;
                _model.OnTransientPatchCleared -= HandleTransientPatchCleared;
            }
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.OnPatchAdded -= HandlePatchAdded;
                _model.OnPatchRemoved -= HandlePatchRemoved;
                _model.OnTransientPatchUpdated -= HandleTransientPatchUpdated;
                _model.OnTransientPatchCleared -= HandleTransientPatchCleared;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_model == null || _gridView == null) return;

            Vector2Int cell = GetCellUnderPointer(eventData);
            Debug.Log($"[InputPresenter] OnPointerDown at screen {eventData.position}, cell {cell}, click valid: {IsWithinGrid(cell)}");
            if (!IsWithinGrid(cell)) return;

            _isClickOnly = true;
            _clickedPatchId = null;

            Patch clickedPatch = _model.GetPatchAt(cell.x, cell.y);
            Debug.Log($"[InputPresenter] Clicked patch state: {(clickedPatch != null ? clickedPatch.State.ToString() : "None")}");

            if (clickedPatch != null)
            {
                if (clickedPatch.State == PatchState.Valid)
                {
                    // Clicked inside valid patch -> Prepare for potential click-to-delete
                    _clickedPatchId = clickedPatch.Id;
                    _state = FsmState.Idle;
                    Debug.Log($"[InputPresenter] Selected valid patch {clickedPatch.Id} for deletion check.");
                }
                else if (clickedPatch.State == PatchState.Transient)
                {
                    // Clicked inside active Transient patch -> Enter Extrusion mode
                    _clickedPatchId = clickedPatch.Id; // Allow click-to-delete for Transient patch
                    _state = FsmState.Extruding;
                    _extrusionStartCell = cell;
                    _anchorBounds = clickedPatch.Bounds;
                    _currentPreviewBounds = clickedPatch.Bounds;
                    
                    ShowPreview(_currentPreviewBounds);
                    Debug.Log($"[InputPresenter] Entered Extrusion state. Anchor bounds: {_anchorBounds}");
                }
            }
            else
            {
                // Clicked an empty cell (or starting drag elsewhere evaporates transient patch)
                if (_model.TransientPatch != null)
                {
                    Debug.Log("[InputPresenter] Clearing existing transient patch.");
                    _model.ClearTransientPatch();
                }

                _state = FsmState.DrawingNew;
                _drawStartCell = cell;
                _currentPreviewBounds = new PatchBounds(cell.x, cell.x, cell.y, cell.y);
                
                ShowPreview(_currentPreviewBounds);
                Debug.Log($"[InputPresenter] Entered DrawingNew state. Start cell: {_drawStartCell}");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_model == null || _state == FsmState.Idle) return;

            Vector2Int cell = GetCellUnderPointer(eventData);
            
            if (cell != _lastLoggedCell)
            {
                _lastLoggedCell = cell;
                Debug.Log($"[InputPresenter] OnDrag cell: {cell}, state: {_state}");
            }
            
            // Mark as not a click anymore if the mouse leaves the initial cell coordinates
            if (_state == FsmState.DrawingNew && cell != _drawStartCell)
            {
                _isClickOnly = false;
            }
            else if (_state == FsmState.Extruding && cell != _extrusionStartCell)
            {
                _isClickOnly = false;
            }

            // Clamp coordinate within grid
            cell.x = Mathf.Clamp(cell.x, 0, _model.Width - 1);
            cell.y = Mathf.Clamp(cell.y, 0, _model.Height - 1);

            if (_state == FsmState.DrawingNew)
            {
                _currentPreviewBounds = GetClampedBounds(_drawStartCell.x, _drawStartCell.y, cell.x, cell.y);
                ShowPreview(_currentPreviewBounds);
            }
            else if (_state == FsmState.Extruding)
            {
                PatchBounds candidate = CalculateExtrusion(cell.x, cell.y);

                // Ensure candidate is valid: no overlaps with Valid patches, contains the original clue cell, area >= 1
                if (IsExtrusionValid(candidate))
                {
                    _currentPreviewBounds = candidate;
                    ShowPreview(_currentPreviewBounds);
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log($"[InputPresenter] OnPointerUp. ClickOnly: {_isClickOnly}, state: {_state}, current bounds: {_currentPreviewBounds}");
            HidePreview();

            if (_model == null) return;

            if ((_state == FsmState.DrawingNew || _state == FsmState.Extruding) && _currentPreviewBounds.Area <= 1)
            {
                Debug.Log("[InputPresenter] Single cell selection/click released. Clearing transient patch.");
                _model.ClearTransientPatch();
            }
            else
            {
                bool isActuallyClick = _isClickOnly || (_state == FsmState.Extruding && _currentPreviewBounds == _anchorBounds);

                if (isActuallyClick)
                {
                    // Simple tap action
                    if (!string.IsNullOrEmpty(_clickedPatchId))
                    {
                        // Clicked a valid or transient patch -> Delete it
                        Debug.Log($"[InputPresenter] Click delete for patch: {_clickedPatchId}");
                        _model.RemovePatch(_clickedPatchId);
                    }
                }
                else
                {
                    // Drag release actions
                    if (_state == FsmState.DrawingNew)
                    {
                        bool committed = _model.TryCommitPatch(_currentPreviewBounds, out var patch);
                        Debug.Log($"[InputPresenter] Drag release. TryCommitPatch result: {committed}, State: {(patch != null ? patch.State.ToString() : "None")}");
                    }
                    else if (_state == FsmState.Extruding)
                    {
                        bool resized = _model.TryResizeTransientPatch(_currentPreviewBounds);
                        Debug.Log($"[InputPresenter] Drag release. TryResizeTransientPatch result: {resized}");
                    }
                }
            }

            _state = FsmState.Idle;
            _clickedPatchId = null;
            _isClickOnly = false;
            _lastLoggedCell = new Vector2Int(-99, -99);
        }

        #region Helper Calculations
        private Vector2Int GetCellUnderPointer(PointerEventData eventData)
        {
            if (_gridContainer == null || _gridView == null) return new Vector2Int(-1, -1);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gridContainer, 
                eventData.position, 
                eventData.pressEventCamera, 
                out Vector2 localPos
            );

            // Translate from centered pivot space to bottom-left relative space
            Vector2 bottomLeftPos = localPos + _gridContainer.rect.size / 2f;
            Vector2 offset = _gridView.GetGridStartOffset();
            Vector2 relativePos = bottomLeftPos - offset;

            int x = Mathf.FloorToInt(relativePos.x / _gridView.CellSize);
            int y = Mathf.FloorToInt(relativePos.y / _gridView.CellSize);

            return new Vector2Int(x, y);
        }

        private bool IsWithinGrid(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < _model.Width && cell.y >= 0 && cell.y < _model.Height;
        }

        private PatchBounds GetClampedBounds(int startX, int startY, int targetX, int targetY)
        {
            int stepX = targetX >= startX ? 1 : -1;
            int stepY = targetY >= startY ? 1 : -1;

            int clampedX = startX;
            int clampedY = startY;

            // Step along X first
            for (int x = startX + stepX; x != targetX + stepX; x += stepX)
            {
                bool colOverlaps = false;
                int minY = Mathf.Min(startY, clampedY);
                int maxY = Mathf.Max(startY, clampedY);
                for (int y = minY; y <= maxY; y++)
                {
                    if (_model.IsCellOccupiedByValidPatch(x, y))
                    {
                        colOverlaps = true;
                        break;
                    }
                }
                if (colOverlaps) break;
                clampedX = x;
            }

            // Step along Y
            for (int y = startY + stepY; y != targetY + stepY; y += stepY)
            {
                bool rowOverlaps = false;
                int minX = Mathf.Min(startX, clampedX);
                int maxX = Mathf.Max(startX, clampedX);
                for (int x = minX; x <= maxX; x++)
                {
                    if (_model.IsCellOccupiedByValidPatch(x, y))
                    {
                        rowOverlaps = true;
                        break;
                    }
                }
                if (rowOverlaps) break;
                clampedY = y;
            }

            return new PatchBounds(
                Mathf.Min(startX, clampedX),
                Mathf.Max(startX, clampedX),
                Mathf.Min(startY, clampedY),
                Mathf.Max(startY, clampedY)
            );
        }

        private PatchBounds CalculateExtrusion(int cx, int cy)
        {
            int minX = _anchorBounds.MinX;
            int maxX = _anchorBounds.MaxX;
            int minY = _anchorBounds.MinY;
            int maxY = _anchorBounds.MaxY;

            float centerX = (_anchorBounds.MinX + _anchorBounds.MaxX) / 2f;
            float centerY = (_anchorBounds.MinY + _anchorBounds.MaxY) / 2f;

            bool resizeRight = _extrusionStartCell.x >= centerX;
            bool resizeTop = _extrusionStartCell.y >= centerY;

            if (resizeRight)
            {
                maxX = _anchorBounds.MaxX + (cx - _extrusionStartCell.x);
                maxX = Mathf.Max(maxX, minX); // prevent crossing
            }
            else
            {
                minX = _anchorBounds.MinX + (cx - _extrusionStartCell.x);
                minX = Mathf.Min(minX, maxX); // prevent crossing
            }

            if (resizeTop)
            {
                maxY = _anchorBounds.MaxY + (cy - _extrusionStartCell.y);
                maxY = Mathf.Max(maxY, minY); // prevent crossing
            }
            else
            {
                minY = _anchorBounds.MinY + (cy - _extrusionStartCell.y);
                minY = Mathf.Min(minY, maxY); // prevent crossing
            }

            return new PatchBounds(minX, maxX, minY, maxY);
        }

        private bool IsExtrusionValid(PatchBounds candidate)
        {
            // Bounds within grid
            if (candidate.MinX < 0 || candidate.MaxX >= _model.Width || candidate.MinY < 0 || candidate.MaxY >= _model.Height)
            {
                return false;
            }

            // Must contain the original clue cell
            if (_model.TransientPatch != null)
            {
                var clue = _model.TransientPatch.Clue;
                if (!candidate.Contains(clue.X, clue.Y))
                {
                    return false;
                }
            }

            // Must not overlap any Valid Patch
            foreach (var patch in _model.Patches)
            {
                if (patch.State == PatchState.Valid && patch.Bounds.Overlaps(candidate))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region View Visual Management
        private void ShowPreview(PatchBounds bounds)
        {
            if (_previewPatchView == null) return;
            _previewPatchView.gameObject.SetActive(true);
            _previewPatchView.Initialize("Preview", bounds, _gridView.CellSize, _previewColor, _gridView);
        }

        private void HidePreview()
        {
            if (_previewPatchView == null) return;
            _previewPatchView.gameObject.SetActive(false);
        }

        private void HandlePatchAdded(Patch patch)
        {
            if (_patchPrefab == null || _gridContainer == null) return;

            int flowerIndex = _paletteManager.ClaimUniqueIndex();
            PatchView viewObj = Instantiate(_patchPrefab, _gridView.PatchesRoot);
            viewObj.name = $"Patch_{patch.Id}";
            viewObj.Initialize(patch.Id, patch.Bounds, _gridView.CellSize, flowerIndex, _gridView);

            _activePatchViews[patch.Id] = viewObj;
            Debug.Log($"[InputPresenter] Added visual patch: {patch.Id}, bounds: {patch.Bounds}");

            // Trigger visual event
            GridEvents.OnPatchPlaced?.Invoke(patch.Id, patch.Bounds, GetFlowerColor(flowerIndex));
        }

        private void HandlePatchRemoved(Patch patch)
        {
            if (_activePatchViews.TryGetValue(patch.Id, out var view))
            {
                _activePatchViews.Remove(patch.Id);
                
                if (view.FlowerIndex >= 0)
                {
                    _paletteManager.ReleaseIndex(view.FlowerIndex);
                }

                Destroy(view.gameObject);
                Debug.Log($"[InputPresenter] Removed visual patch: {patch.Id}");
            }

            // Trigger visual event
            GridEvents.OnPatchRemoved?.Invoke(patch.Id, patch.Bounds);
        }

        private Color GetFlowerColor(int index)
        {
            switch (index)
            {
                case 0: return new Color(0.96f, 0.96f, 0.86f); // Beige
                case 1: return new Color(0.2f, 0.4f, 0.8f);    // Blue
                case 2: return new Color(0.0f, 0.8f, 0.8f);    // Cyan
                case 3: return new Color(0.2f, 0.8f, 0.2f);    // Green
                case 4: return new Color(0.78f, 0.6f, 0.96f);  // Lilac
                case 5: return new Color(0.75f, 1.0f, 0.0f);   // Lime
                case 6: return new Color(1.0f, 0.0f, 1.0f);    // Magenta
                case 7: return new Color(0.6f, 1.0f, 0.8f);    // Mint
                case 8: return new Color(1.0f, 0.6f, 0.0f);    // Orange
                case 9: return new Color(1.0f, 0.8f, 0.6f);    // Peach
                case 10: return new Color(1.0f, 0.75f, 0.8f);  // Pink
                case 11: return new Color(0.5f, 0.0f, 0.5f);   // Purple
                case 12: return new Color(1.0f, 0.0f, 0.0f);   // Red
                case 13: return new Color(1.0f, 1.0f, 1.0f);   // White
                case 14: return new Color(1.0f, 1.0f, 0.0f);   // Yellow
                default: return Color.white;
            }
        }

        private void HandleTransientPatchUpdated(Patch patch)
        {
            if (_patchPrefab == null || _gridContainer == null) return;

            if (_transientPatchView == null)
            {
                _transientPatchView = Instantiate(_patchPrefab, _gridView.PatchesRoot);
                _transientPatchView.name = "TransientPatch";
            }

            _transientPatchView.gameObject.SetActive(true);
            _transientPatchView.Initialize(patch.Id, patch.Bounds, _gridView.CellSize, _transientColor, _gridView);

            // Trigger visual event
            var validator = ShapeValidatorFactory.GetValidator(patch.Clue.ShapeType);
            bool isValid = validator.Validate(patch.Bounds, patch.Clue.RequiredArea);
            Debug.Log($"[InputPresenter] Updated visual transient patch bounds: {patch.Bounds}, isValid: {isValid}");
            GridEvents.OnTransientPatchUpdated?.Invoke(patch.Bounds, isValid);
        }

        private void HandleTransientPatchCleared()
        {
            if (_transientPatchView != null)
            {
                Destroy(_transientPatchView.gameObject);
                _transientPatchView = null;
                Debug.Log("[InputPresenter] Cleared visual transient patch");
            }

            // Trigger visual event
            GridEvents.OnTransientPatchCleared?.Invoke();
        }
        #endregion
    }
}
