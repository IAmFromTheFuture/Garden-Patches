using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using TMPro;
using Patches.View;
using Patches.Presenter;

namespace Patches.Editor
{
    public class SetupScene : EditorWindow
    {
        [MenuItem("Tools/Setup Patches Scene")]
        public static void CreatePatchesHierarchy()
        {
            // 1. Setup folders
            string prefabDir = "Assets/Prefabs";
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
                AssetDatabase.Refresh();
            }

            // 2. Create / Find core components
            GameObject eventSystemObj = FindOrCreateGameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            
            GameObject mainCameraObj = GameObject.Find("Main Camera");
            Camera mainCamera = mainCameraObj != null ? mainCameraObj.GetComponent<Camera>() : Camera.main;
            if (mainCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera", typeof(Camera));
                mainCamera = camObj.GetComponent<Camera>();
            }

            // 3. Create GameManager GameObject
            GameObject gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj == null)
            {
                gameManagerObj = new GameObject("GameManager");
            }
            GameManager gameManager = gameManagerObj.GetComponent<GameManager>() ?? gameManagerObj.AddComponent<GameManager>();
            GridView gridView = gameManagerObj.GetComponent<GridView>() ?? gameManagerObj.AddComponent<GridView>();
            FlowerPoolManager poolManager = gameManagerObj.GetComponent<FlowerPoolManager>() ?? gameManagerObj.AddComponent<FlowerPoolManager>();

            // 4. Create UI Prefabs programmatically if they do not exist
            string cellPrefabPath = $"{prefabDir}/CellPrefab.prefab";
            string cluePrefabPath = $"{prefabDir}/CluePrefab.prefab";
            string patchPrefabPath = $"{prefabDir}/PatchPrefab.prefab";

            GameObject cellPrefab = LoadOrCreateCellPrefab(cellPrefabPath);
            GameObject cluePrefab = LoadOrCreateCluePrefab(cluePrefabPath);
            GameObject patchPrefab = LoadOrCreatePatchPrefab(patchPrefabPath);

            // Load or Create flower prefabs
            List<GameObject> flowerPrefabs = LoadOrCreateFlowerPrefabs(prefabDir);

            // Assign prefabs to GridView
            var gridViewSerialized = new SerializedObject(gridView);
            gridViewSerialized.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab.GetComponent<CellView>();
            gridViewSerialized.FindProperty("_cluePrefab").objectReferenceValue = cluePrefab.GetComponent<ClueView>();
            gridViewSerialized.ApplyModifiedProperties();

            // Assign flower prefabs list to FlowerPoolManager
            var poolSerialized = new SerializedObject(poolManager);
            var prefabsProp = poolSerialized.FindProperty("_flowerPrefabs");
            prefabsProp.ClearArray();
            for (int i = 0; i < flowerPrefabs.Count; i++)
            {
                prefabsProp.InsertArrayElementAtIndex(i);
                prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = flowerPrefabs[i];
            }
            poolSerialized.ApplyModifiedProperties();

            // 5. Create Canvas
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }
            else
            {
                // Clear any stale panels/UI elements from previous setup runs
                for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(canvasObj.transform.GetChild(i).gameObject);
                }
            }
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Setup GraphicRaycaster
            canvasObj.GetComponent<GraphicRaycaster>();

            // 6. Create MainMenuPanel
            RectTransform mainMenuPanel = CreateUIPanel(canvas.transform, "MainMenuPanel", new Color(0.12f, 0.14f, 0.2f, 1f));
            mainMenuPanel.anchorMin = Vector2.zero;
            mainMenuPanel.anchorMax = Vector2.one;
            mainMenuPanel.sizeDelta = Vector2.zero;

            // Title Text inside MainMenuPanel
            TextMeshProUGUI titleText = CreateText(mainMenuPanel, "TitleText", "PATCHES", 96, TextAlignmentOptions.Center);
            RectTransform titleRt = titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.7f);
            titleRt.anchorMax = new Vector2(0.5f, 0.7f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(800, 200);

            // Play Button inside MainMenuPanel
            Button playButton = CreateUIButton(mainMenuPanel, "PlayButton", "PLAY", new Color(0.18f, 0.6f, 0.96f, 1f));
            RectTransform playBtnRt = playButton.GetComponent<RectTransform>();
            playBtnRt.anchorMin = new Vector2(0.5f, 0.4f);
            playBtnRt.anchorMax = new Vector2(0.5f, 0.4f);
            playBtnRt.anchoredPosition = Vector2.zero;
            playBtnRt.sizeDelta = new Vector2(300, 100);

            // Setup Play Button Event
            UnityEventTools.AddVoidPersistentListener(playButton.onClick, gameManager.PlayGame);

            // 7. Create GamePanel
            RectTransform gamePanel = CreateUIPanel(canvas.transform, "GamePanel", new Color(0.12f, 0.14f, 0.2f, 1f));
            gamePanel.anchorMin = Vector2.zero;
            gamePanel.anchorMax = Vector2.one;
            gamePanel.sizeDelta = Vector2.zero;

            // Grid Container inside GamePanel
            RectTransform gridContainer = CreateUIPanel(gamePanel, "GridContainer", new Color(0.15f, 0.18f, 0.25f, 1f));
            gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
            gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
            gridContainer.anchoredPosition = Vector2.zero;
            gridContainer.sizeDelta = new Vector2(700, 700);

            // Timer Text inside GamePanel
            TextMeshProUGUI timerText = CreateText(gamePanel, "TimerText", "00:00", 48, TextAlignmentOptions.Center);
            RectTransform timerRt = timerText.rectTransform;
            timerRt.anchorMin = new Vector2(0.5f, 0.9f);
            timerRt.anchorMax = new Vector2(0.5f, 0.9f);
            timerRt.anchoredPosition = Vector2.zero;
            timerRt.sizeDelta = new Vector2(300, 80);

            // 8. Create WinPanel inside GamePanel (initially inactive)
            RectTransform winPanel = CreateUIPanel(gamePanel, "WinPanel", new Color(0.1f, 0.1f, 0.12f, 0.95f));
            winPanel.anchorMin = Vector2.zero;
            winPanel.anchorMax = Vector2.one;
            winPanel.sizeDelta = Vector2.zero;
            winPanel.gameObject.SetActive(false);

            // Win Title Text
            TextMeshProUGUI winTitle = CreateText(winPanel, "WinTitle", "SOLVED!", 80, TextAlignmentOptions.Center);
            RectTransform winTitleRt = winTitle.rectTransform;
            winTitleRt.anchorMin = new Vector2(0.5f, 0.7f);
            winTitleRt.anchorMax = new Vector2(0.5f, 0.7f);
            winTitleRt.anchoredPosition = Vector2.zero;
            winTitleRt.sizeDelta = new Vector2(600, 150);

            // Star Rating Text
            TextMeshProUGUI winRatingText = CreateText(winPanel, "WinRatingText", "Rating: ★★★", 48, TextAlignmentOptions.Center);
            RectTransform winRatingRt = winRatingText.rectTransform;
            winRatingRt.anchorMin = new Vector2(0.5f, 0.55f);
            winRatingRt.anchorMax = new Vector2(0.5f, 0.55f);
            winRatingRt.anchoredPosition = Vector2.zero;
            winRatingRt.sizeDelta = new Vector2(600, 80);

            // Win Timer Text
            TextMeshProUGUI winTimerText = CreateText(winPanel, "WinTimerText", "Time: 00s", 40, TextAlignmentOptions.Center);
            RectTransform winTimerRt = winTimerText.rectTransform;
            winTimerRt.anchorMin = new Vector2(0.5f, 0.45f);
            winTimerRt.anchorMax = new Vector2(0.5f, 0.45f);
            winTimerRt.anchoredPosition = Vector2.zero;
            winTimerRt.sizeDelta = new Vector2(600, 80);

            // Restart Button inside WinPanel
            Button restartButton = CreateUIButton(winPanel, "RestartButton", "RESTART", new Color(0.24f, 0.7f, 0.44f, 1f));
            RectTransform restartBtnRt = restartButton.GetComponent<RectTransform>();
            restartBtnRt.anchorMin = new Vector2(0.5f, 0.3f);
            restartBtnRt.anchorMax = new Vector2(0.5f, 0.3f);
            restartBtnRt.anchoredPosition = Vector2.zero;
            restartBtnRt.sizeDelta = new Vector2(300, 80);

            // Setup Restart Button Event
            UnityEventTools.AddVoidPersistentListener(restartButton.onClick, gameManager.RestartLevel);

            // 9. Setup InputPresenter on the GridContainer (to receive pointer/drag events)
            InputPresenter oldPresenter = gameManagerObj.GetComponent<InputPresenter>();
            if (oldPresenter != null)
            {
                DestroyImmediate(oldPresenter);
            }

            InputPresenter inputPresenter = gridContainer.GetComponent<InputPresenter>() ?? gridContainer.gameObject.AddComponent<InputPresenter>();

            // Attach references to GameManager Inspector
            var managerSerialized = new SerializedObject(gameManager);
            managerSerialized.FindProperty("_gridView").objectReferenceValue = gridView;
            managerSerialized.FindProperty("_inputPresenter").objectReferenceValue = inputPresenter;
            managerSerialized.FindProperty("_mainMenuPanel").objectReferenceValue = mainMenuPanel.gameObject;
            managerSerialized.FindProperty("_gamePanel").objectReferenceValue = gamePanel.gameObject;
            managerSerialized.FindProperty("_timerText").objectReferenceValue = timerText;
            managerSerialized.FindProperty("_winPanel").objectReferenceValue = winPanel.gameObject;
            managerSerialized.FindProperty("_winRatingText").objectReferenceValue = winRatingText;
            managerSerialized.FindProperty("_winTimerText").objectReferenceValue = winTimerText;
            managerSerialized.ApplyModifiedProperties();

            // Hook up InputPresenter serialized properties
            var presenterSerialized2 = new SerializedObject(inputPresenter);
            presenterSerialized2.FindProperty("_gridView").objectReferenceValue = gridView;
            presenterSerialized2.FindProperty("_gridContainer").objectReferenceValue = gridContainer;
            presenterSerialized2.FindProperty("_patchPrefab").objectReferenceValue = patchPrefab.GetComponent<PatchView>();
            presenterSerialized2.ApplyModifiedProperties();

            // Hook up GridView serialized properties
            var gridViewSerialized2 = new SerializedObject(gridView);
            gridViewSerialized2.FindProperty("_gridContainer").objectReferenceValue = gridContainer;
            gridViewSerialized2.ApplyModifiedProperties();

            // Set UI Raycast target on the GridContainer so InputPresenter gets events
            Image containerImage = gridContainer.GetComponent<Image>();
            if (containerImage != null)
            {
                containerImage.raycastTarget = true;
            }

            // Save the scene setup
            EditorUtility.SetDirty(gameManagerObj);
            EditorUtility.SetDirty(canvasObj);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("Patches UI Hierarchy and Prefabs successfully constructed!");
        }

        #region Programmatic Prefab Generation
        private static GameObject LoadOrCreateCellPrefab(string path)
        {
            GameObject cell = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (cell != null)
            {
                Image img = cell.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.35f, 0.65f, 0.35f, 1f); // Soft grass green
                    if (img.raycastTarget)
                    {
                        img.raycastTarget = false;
                    }
                    EditorUtility.SetDirty(cell);
                }
                return cell;
            }

            GameObject temp = new GameObject("CellPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CellView));
            Image imgNew = temp.GetComponent<Image>();
            imgNew.color = new Color(0.35f, 0.65f, 0.35f, 1f); // Soft grass green
            imgNew.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            imgNew.type = Image.Type.Sliced;
            imgNew.raycastTarget = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            DestroyImmediate(temp);
            return prefab;
        }

        private static List<GameObject> LoadOrCreateFlowerPrefabs(string baseDir)
        {
            string flowerDir = $"{baseDir}/Flowers";
            if (!Directory.Exists(flowerDir))
            {
                Directory.CreateDirectory(flowerDir);
                AssetDatabase.Refresh();
            }

            string[] flowerNames = new string[] 
            { 
                "beige", "blue", "cyan", "green", "lilac", "lime", "magenta", "mint", 
                "orange", "peach", "pink", "purple", "red", "white", "yellow" 
            };

            List<GameObject> createdPrefabs = new List<GameObject>();

            for (int i = 0; i < flowerNames.Length; i++)
            {
                string name = flowerNames[i];
                string prefabPath = $"{flowerDir}/Flower_{name}.prefab";
                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                Sprite flowerSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/FinalFlowers/{name}.png");
                if (flowerSprite == null)
                {
                    Debug.LogError($"[SetupScene] Failed to load flower sprite at Assets/Sprites/FinalFlowers/{name}.png");
                    continue;
                }

                if (existing != null)
                {
                    // Update sprite and index
                    FlowerTile tile = existing.GetComponent<FlowerTile>();
                    if (tile != null)
                    {
                        tile.FlowerIndex = i;
                        tile.SetSprite(flowerSprite);
                    }
                    Image img = existing.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = flowerSprite;
                        img.color = Color.white;
                    }
                    EditorUtility.SetDirty(existing);
                    createdPrefabs.Add(existing);
                    continue;
                }

                GameObject temp = new GameObject($"Flower_{name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FlowerTile));
                Image imgNew = temp.GetComponent<Image>();
                imgNew.color = Color.white;
                imgNew.sprite = flowerSprite;
                imgNew.raycastTarget = false;

                FlowerTile tileNew = temp.GetComponent<FlowerTile>();
                tileNew.FlowerIndex = i;

                // Link image
                var serializedTile = new SerializedObject(tileNew);
                serializedTile.FindProperty("_image").objectReferenceValue = imgNew;
                serializedTile.ApplyModifiedProperties();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
                DestroyImmediate(temp);
                createdPrefabs.Add(prefab);
            }

            return createdPrefabs;
        }

        private static GameObject LoadOrCreateCluePrefab(string path)
        {
            GameObject clue = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (clue != null)
            {
                Image img = clue.GetComponent<Image>();
                if (img != null && img.raycastTarget)
                {
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(clue);
                }
                foreach (var text in clue.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (text.raycastTarget)
                    {
                        text.raycastTarget = false;
                        EditorUtility.SetDirty(clue);
                    }
                }
                return clue;
            }

            GameObject temp = new GameObject("CluePrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ClueView));
            Image imgNew = temp.GetComponent<Image>();
            imgNew.color = new Color(1f, 1f, 1f, 0.05f); // Transparent backdrop
            imgNew.raycastTarget = false;

            // Spacing rects inside Clue
            GameObject areaTextObj = new GameObject("AreaText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            areaTextObj.transform.SetParent(temp.transform);
            TextMeshProUGUI areaText = areaTextObj.GetComponent<TextMeshProUGUI>();
            areaText.text = "0";
            areaText.fontSize = 24;
            areaText.alignment = TextAlignmentOptions.Center;
            areaText.raycastTarget = false;
            RectTransform areaRt = areaText.rectTransform;
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.sizeDelta = Vector2.zero;

            GameObject shapeTextObj = new GameObject("ShapeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            shapeTextObj.transform.SetParent(temp.transform);
            TextMeshProUGUI shapeText = shapeTextObj.GetComponent<TextMeshProUGUI>();
            shapeText.text = "";
            shapeText.fontSize = 14;
            shapeText.color = new Color(1f, 1f, 1f, 0.5f);
            shapeText.alignment = TextAlignmentOptions.BottomRight;
            shapeText.raycastTarget = false;
            RectTransform shapeRt = shapeText.rectTransform;
            shapeRt.anchorMin = Vector2.zero;
            shapeRt.anchorMax = Vector2.one;
            shapeRt.sizeDelta = new Vector2(-4, -4); // margins

            // Link ClueView fields
            ClueView clueView = temp.GetComponent<ClueView>();
            var viewSerialized = new SerializedObject(clueView);
            viewSerialized.FindProperty("_areaText").objectReferenceValue = areaText;
            viewSerialized.FindProperty("_shapeTypeText").objectReferenceValue = shapeText;
            viewSerialized.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject LoadOrCreatePatchPrefab(string path)
        {
            GameObject patch = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (patch != null)
            {
                Image img = patch.GetComponent<Image>();
                if (img != null && img.raycastTarget)
                {
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(patch);
                }
                return patch;
            }

            GameObject temp = new GameObject("PatchPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PatchView));
            Image imgNew = temp.GetComponent<Image>();
            imgNew.color = Color.white;
            imgNew.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            imgNew.type = Image.Type.Sliced;
            imgNew.raycastTarget = false;

            // Link PatchView fields
            PatchView patchView = temp.GetComponent<PatchView>();
            var viewSerialized = new SerializedObject(patchView);
            viewSerialized.FindProperty("_backgroundImage").objectReferenceValue = imgNew;
            viewSerialized.FindProperty("_rectTransform").objectReferenceValue = temp.GetComponent<RectTransform>();
            viewSerialized.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            DestroyImmediate(temp);
            return prefab;
        }
        #endregion

        #region Helpers
        private static GameObject FindOrCreateGameObject(string name, params System.Type[] components)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name, components);
            }
            return obj;
        }

        private static RectTransform CreateUIPanel(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            
            Image img = obj.GetComponent<Image>();
            img.color = color;

            return obj.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI tm = obj.GetComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.alignment = alignment;
            tm.color = Color.white;

            return tm;
        }

        private static Button CreateUIButton(Transform parent, string name, string label, Color color)
        {
            GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);

            Image img = buttonObj.GetComponent<Image>();
            img.color = color;
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;

            Button btn = buttonObj.GetComponent<Button>();

            GameObject labelObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(buttonObj.transform, false);

            TextMeshProUGUI tm = labelObj.GetComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 28;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;

            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            return btn;
        }
        #endregion
    }
}
