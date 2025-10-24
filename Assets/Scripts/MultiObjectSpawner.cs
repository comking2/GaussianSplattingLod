using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnSettings
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public Vector2 spacing = new Vector2(5f, 5f);
    
    [Header("Random Offset")]
    public float randomOffsetRange = 1f;
    
    [Header("Height Variation")]
    public bool useRandomHeight = true;
    public float minHeight = 0f;
    public float maxHeight = 2f;
}

public class MultiObjectSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject prefabToSpawn;
    public GameObject gaussianSplatPrefab;
    
    [Header("Spawn Settings")]
    public SpawnSettings spawnSettings;
    
    [Header("LOD Settings")]
    public float lodSwitchDistance = 50f;
    public float lodHysteresis = 5f;
    
    [Header("Auto Spawn")]
    public bool spawnOnStart = true;
    
    private List<GameObject> mSpawnedObjects = new List<GameObject>();
    
    void Start()
    {
        if (spawnOnStart && prefabToSpawn != null && mSpawnedObjects.Count == 0)
        {
            SpawnObjects();
        }
    }
    
    [ContextMenu("Spawn Objects")]
    public void SpawnObjects()
    {
        // 이미 생성된 오브젝트가 있으면 먼저 삭제
        if (mSpawnedObjects.Count > 0)
        {
            ClearSpawnedObjects();
        }
        
        if (prefabToSpawn == null)
        {
            Debug.LogError("No prefab assigned to spawn!");
            return;
        }
        
        Vector3 startPosition = transform.position;
        
        for (int x = 0; x < spawnSettings.gridWidth; x++)
        {
            for (int z = 0; z < spawnSettings.gridHeight; z++)
            {
                Vector3 spawnPosition = CalculateSpawnPosition(startPosition, x, z);
                GameObject spawnedObjParent = new GameObject($"SpawnedObject_{x}_{z}");
                spawnedObjParent.transform.parent = this.transform;
                GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, spawnedObjParent.transform);
                // LODSwitcher 컴포넌트 추가
                LODSwitcher lodSwitcher = spawnedObjParent.AddComponent<LODSwitcher>();
                
                // LODData 초기화
                lodSwitcher.lodData = new LODData();
                
                // LOD 설정
                lodSwitcher.lodData.switchDistance = lodSwitchDistance;
                lodSwitcher.lodData.hysteresis = lodHysteresis;
                lodSwitcher.lodData.meshObject = spawnedObj;

                // 가우시안 스플랫 오브젝트 생성 및 설정
                if (gaussianSplatPrefab != null)
                {
                    GameObject gaussianObj = Instantiate(gaussianSplatPrefab, spawnPosition, Quaternion.identity, spawnedObjParent.transform);
                    gaussianObj.name = $"GaussianSplat_{x}_{z}";
                    lodSwitcher.lodData.gaussianSplatObject = gaussianObj;

                    gaussianObj.SetActive(false);

                }
                spawnedObjParent.SetActive(false);
                mSpawnedObjects.Add(spawnedObjParent);
            }
        }
        
        Debug.Log($"Spawned {mSpawnedObjects.Count} objects with LOD system");
    }
    
    private Vector3 CalculateSpawnPosition(Vector3 _startPos, int _x, int _z)
    {
        Vector3 gridPosition = new Vector3(
            _x * spawnSettings.spacing.x,
            0,
            _z * spawnSettings.spacing.y
        );
        
        // Add random offset
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnSettings.randomOffsetRange, spawnSettings.randomOffsetRange),
            0,
            Random.Range(-spawnSettings.randomOffsetRange, spawnSettings.randomOffsetRange)
        );
        
        // Add height variation
        float height = spawnSettings.useRandomHeight 
            ? Random.Range(spawnSettings.minHeight, spawnSettings.maxHeight)
            : 0f;
        
        return _startPos + gridPosition + randomOffset + Vector3.up * height;
    }
    
    [ContextMenu("Clear Spawned Objects")]
    public void ClearSpawnedObjects()
    {
        foreach (GameObject obj in mSpawnedObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        mSpawnedObjects.Clear();
    }
    
    public List<GameObject> GetSpawnedObjects()
    {
        return new List<GameObject>(mSpawnedObjects);
    }
    
    void OnDrawGizmosSelected()
    {
        if (spawnSettings == null) return;
        
        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position;
        
        for (int x = 0; x < spawnSettings.gridWidth; x++)
        {
            for (int z = 0; z < spawnSettings.gridHeight; z++)
            {
                Vector3 pos = startPos + new Vector3(
                    x * spawnSettings.spacing.x,
                    0,
                    z * spawnSettings.spacing.y
                );
                Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
            }
        }
    }
}