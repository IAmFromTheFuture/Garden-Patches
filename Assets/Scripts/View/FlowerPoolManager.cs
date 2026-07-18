using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;

namespace Patches.View
{
    public class FlowerPoolManager : MonoBehaviour
    {
        public static FlowerPoolManager Instance { get; private set; }

        [Header("Flower Prefabs")]
        [SerializeField] private List<GameObject> _flowerPrefabs = new List<GameObject>();

        private readonly Dictionary<int, ObjectPool<GameObject>> _pools = new Dictionary<int, ObjectPool<GameObject>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject GetFlower(int index, Transform parent)
        {
            if (index < 0 || index >= _flowerPrefabs.Count)
            {
                Debug.LogError($"[FlowerPoolManager] Invalid flower index: {index}");
                return null;
            }

            if (!_pools.ContainsKey(index))
            {
                InitializePool(index);
            }

            GameObject flower = _pools[index].Get();
            if (flower != null)
            {
                flower.transform.SetParent(parent, false);
            }
            return flower;
        }

        public void ReleaseFlower(GameObject flowerInstance)
        {
            if (flowerInstance == null) return;

            FlowerTile tile = flowerInstance.GetComponent<FlowerTile>();
            if (tile != null && _pools.TryGetValue(tile.FlowerIndex, out var pool))
            {
                // Unparent to prevent destruction when the parent PatchView is destroyed
                flowerInstance.transform.SetParent(transform, false);
                pool.Release(flowerInstance);
            }
            else
            {
                // Fallback if not pooled or pool missing
                Destroy(flowerInstance);
            }
        }

        private void InitializePool(int index)
        {
            int localIndex = index; // Capture for lambda closure
            _pools[index] = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    if (_flowerPrefabs[localIndex] == null)
                    {
                        Debug.LogError($"[FlowerPoolManager] Flower prefab at index {localIndex} is null!");
                        return new GameObject("NullFlower");
                    }
                    GameObject go = Instantiate(_flowerPrefabs[localIndex]);
                    FlowerTile tile = go.GetComponent<FlowerTile>() ?? go.AddComponent<FlowerTile>();
                    tile.FlowerIndex = localIndex;
                    return go;
                },
                actionOnGet: go => 
                {
                    go.SetActive(true);
                    Image img = go.GetComponent<Image>();
                    if (img != null) img.enabled = true;
                    FlowerTile tile = go.GetComponent<FlowerTile>();
                    if (tile != null) tile.enabled = true;
                },
                actionOnRelease: go => go.SetActive(false),
                actionOnDestroy: go => Destroy(go),
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 100
            );
        }
    }
}
