using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Patches.Data;
using Patches.Domain;
using Patches.Model;
using Patches.Presenter;
using Patches.Events;

namespace Patches.View
{
    public class GameManager : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private PuzzleLevelSO _currentLevel;

        [Header("Views & Presenters")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private InputPresenter _inputPresenter;

        [Header("UI Canvas Elements")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _gamePanel;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private TextMeshProUGUI _winRatingText;
        [SerializeField] private TextMeshProUGUI _winTimerText;

        private GridModel _model;
        private float _timeElapsed;
        private bool _isSolved;

        private void Start()
        {
            if (_winPanel != null)
            {
                _winPanel.SetActive(false);
            }

            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.SetActive(true);
            }

            if (_gamePanel != null)
            {
                _gamePanel.SetActive(false);
            }
        }

        public void PlayGame()
        {
            if (_mainMenuPanel != null)
            {
                _mainMenuPanel.SetActive(false);
            }

            if (_gamePanel != null)
            {
                _gamePanel.SetActive(true);
            }

            LoadLevel();
        }

        private void Update()
        {
            if (!_isSolved && _model != null)
            {
                _timeElapsed += Time.deltaTime;
                UpdateTimerUI();
            }
        }

        private void LoadLevel()
        {
            _isSolved = false;
            _timeElapsed = 0f;
            UpdateTimerUI();

            int width, height, goldTime, silverTime;
            List<PuzzleClue> clues;

            if (_currentLevel != null)
            {
                width = _currentLevel.GridWidth;
                height = _currentLevel.GridHeight;
                goldTime = _currentLevel.GoldTimeSeconds;
                silverTime = _currentLevel.SilverTimeSeconds;
                clues = _currentLevel.Clues;
            }
            else
            {
                // Fallback to a perfect 6x6 Level 1
                width = 6;
                height = 6;
                goldTime = 45;
                silverTime = 90;
                clues = new List<PuzzleClue>
                {
                    new PuzzleClue(0, 0, 4, ShapeType.Square),
                    new PuzzleClue(0, 2, 8, ShapeType.Tall),
                    new PuzzleClue(2, 0, 8, ShapeType.Wide),
                    new PuzzleClue(2, 2, 4, ShapeType.Square),
                    new PuzzleClue(4, 2, 8, ShapeType.Tall),
                    new PuzzleClue(2, 4, 4, ShapeType.Square)
                };
                Debug.LogWarning("No level ScriptableObject assigned to GameManager. Using default 6x6 Level 1.");
            }

            // 1. Initialize core Model (pure C#)
            _model = new GridModel();
            _model.Initialize(width, height, clues, goldTime, silverTime);

            // 2. Setup event listeners
            _model.OnPatchAdded += HandlePatchChanged;
            _model.OnPatchRemoved += HandlePatchChanged;
            _model.OnTransientPatchCleared += HandleTransientPatchCleared;
            _model.OnSolved += HandleSolved;

            // 3. Generate the physical Grid View
            _gridView.GenerateGrid(width, height, clues);

            // 4. Initialize the Input Presenter
            _inputPresenter.Initialize(_model);
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.OnPatchAdded -= HandlePatchChanged;
                _model.OnPatchRemoved -= HandlePatchChanged;
                _model.OnTransientPatchCleared -= HandleTransientPatchCleared;
                _model.OnSolved -= HandleSolved;
            }
        }

        private void HandlePatchChanged(Domain.Patch patch)
        {
            CheckGameWin();
        }

        private void HandleTransientPatchCleared()
        {
            CheckGameWin();
        }

        private void CheckGameWin()
        {
            if (_isSolved) return;

            // Check if model solves successfully
            if (_model.CheckWinCondition((int)_timeElapsed, out int stars))
            {
                // Managed by HandleSolved event callback
            }
        }

        private void HandleSolved(int stars)
        {
            _isSolved = true;
            Debug.Log($"Level solved! Stars earned: {stars}");

            if (_winPanel != null)
            {
                _winPanel.SetActive(true);
            }

            if (_winRatingText != null)
            {
                string starString = new string('★', stars) + new string('☆', 3 - stars);
                _winRatingText.text = $"Rating: {starString} ({stars} Stars)";
            }

            if (_winTimerText != null)
            {
                _winTimerText.text = $"Time: {(int)_timeElapsed}s";
            }

            // Fire global grid solved event
            GridEvents.OnPuzzleSolved?.Invoke(stars);
        }

        private void UpdateTimerUI()
        {
            if (_timerText != null)
            {
                int min = (int)(_timeElapsed / 60);
                int sec = (int)(_timeElapsed % 60);
                _timerText.text = $"{min:D2}:{sec:D2}";
            }
        }

        public void RestartLevel()
        {
            if (_winPanel != null)
            {
                _winPanel.SetActive(false);
            }
            _gridView.ClearGrid();
            LoadLevel();
        }
    }
}
