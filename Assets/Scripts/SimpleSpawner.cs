using UnityEngine;
using System.Collections.Generic;

public class SimpleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject meshPrefab;
    public GameObject gaussianSplatPrefab;
    
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float spacing = 5f;
    
    [Header("Control")]
    public bool spawnOnStart = true;
    public bool showMesh = true;
    
    [Header("Debug")]
    public KeyCode toggleAllKey = KeyCode.T;
    public KeyCode meshKey = KeyCode.M;
    public KeyCode gaussianKey = KeyCode.G;
    
    private List<SimpleSwapper> mSwappers = new List<SimpleSwapper>();
    
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnObjects();
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleAllKey))
        {
            ToggleAll();
        }
        
        if (Input.GetKeyDown(meshKey))
        {
            ShowAllMesh();
        }
        
        if (Input.GetKeyDown(gaussianKey))
        {
            ShowAllGaussian();
        }
    }
    
    [ContextMenu("Spawn Objects")]
    public void SpawnObjects()
    {
        ClearObjects();
        
        if (meshPrefab == null || gaussianSplatPrefab == null)
        {
            Debug.LogError("Mesh Prefab or Gaussian Splat Prefab is not assigned!");
            return;
        }
        
        Vector3 startPos = transform.position;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 position = startPos + new Vector3(x * spacing, 0, z * spacing);
                
                // 컨테이너 오브젝트 생성
                GameObject container = new GameObject($"Object_{x}_{z}");
                container.transform.position = position;
                container.transform.parent = transform;
                
                // 메시 오브젝트 생성
                GameObject meshObj = Instantiate(meshPrefab, position, Quaternion.identity, container.transform);
                meshObj.name = "Mesh";
                
                // 가우시안 스플랫 오브젝트 생성
                GameObject gaussianObj = Instantiate(gaussianSplatPrefab, position, Quaternion.identity, container.transform);
                gaussianObj.name = "GaussianSplat";
                
                // SimpleSwapper 컴포넌트 추가
                SimpleSwapper swapper = container.AddComponent<SimpleSwapper>();
                swapper.meshObject = meshObj;
                swapper.gaussianSplatObject = gaussianObj;
                swapper.showMesh = showMesh;
                swapper.toggleKey = KeyCode.None; // 개별 토글 비활성화
                
                mSwappers.Add(swapper);
            }
        }
        
        Debug.Log($"Spawned {mSwappers.Count} objects with mesh/gaussian swap capability");
    }
    
    [ContextMenu("Clear Objects")]
    public void ClearObjects()
    {
        mSwappers.Clear();
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    
    [ContextMenu("Toggle All")]
    public void ToggleAll()
    {
        showMesh = !showMesh;
        
        foreach (var swapper in mSwappers)
        {
            if (swapper != null)
            {
                swapper.showMesh = showMesh;
            }
        }
        
        Debug.Log($"Toggled all to: {(showMesh ? "Mesh" : "Gaussian Splat")}");
    }
    
    [ContextMenu("Show All Mesh")]
    public void ShowAllMesh()
    {
        showMesh = true;
        
        foreach (var swapper in mSwappers)
        {
            if (swapper != null)
                swapper.ShowMesh();
        }
        
        Debug.Log("Showing all as Mesh");
    }
    
    [ContextMenu("Show All Gaussian")]
    public void ShowAllGaussian()
    {
        showMesh = false;
        
        foreach (var swapper in mSwappers)
        {
            if (swapper != null)
                swapper.ShowGaussianSplat();
        }
        
        Debug.Log("Showing all as Gaussian Splat");
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"Simple Mesh/Gaussian Swapper");
        GUILayout.Label($"Objects: {mSwappers.Count}");
        GUILayout.Label($"Current: {(showMesh ? "Mesh" : "Gaussian Splat")}");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Toggle All (T)"))
        {
            ToggleAll();
        }
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All Mesh (M)"))
        {
            ShowAllMesh();
        }
        if (GUILayout.Button("All Gaussian (G)"))
        {
            ShowAllGaussian();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}