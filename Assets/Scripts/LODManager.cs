using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LODManager : MonoBehaviour
{    
    [Header("Auto Detection")]
    public bool autoDetectLODSwitchers = true;
    public bool includeChildren = true;
    
    [Header("Performance Settings")]
    public bool enableCulling = true;
    public float cullingDistance = 200f;
    
    [Header("Statistics")]
    public bool showStatistics = true;
    
    [Header("Debug")]
    public KeyCode refreshKey = KeyCode.R;
    public KeyCode toggleStatsKey = KeyCode.T;
    
    private Camera mTargetCamera;
    private List<LODSwitcher> lodSwitchers = new List<LODSwitcher>();
    private int mMeshObjectsActive = 0;
    private int mGaussianSplatsActive = 0;
    private int mCulledObjects = 0;
    
    void Start()
    {
        mTargetCamera = Camera.main;
        
        if (autoDetectLODSwitchers)
        {
            DetectLODSwitchers();
        }
    }
    
    void Update()
    {
        HandleInput();
        UpdateStatistics();
        
        if (enableCulling)
        {
            UpdateCulling();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(refreshKey))
        {
            DetectLODSwitchers();
        }

        if (Input.GetKeyDown(toggleStatsKey))
        {
            showStatistics = !showStatistics;
        }
    }
    
    public void ActiveLODSwitchers()
    {
        lodSwitchers.Clear();

        if (includeChildren)
        {
            lodSwitchers.AddRange(GetComponentsInChildren<LODSwitcher>(true));
        }
        else
        {
            lodSwitchers.AddRange(FindObjectsOfType<LODSwitcher>());
        }
        foreach (var switcher in lodSwitchers)
        {
            if (switcher != null)
                switcher.gameObject.SetActive(true);
        }

        Debug.Log($"Active {lodSwitchers.Count} LOD Switchers");
    }
    
    [ContextMenu("Detect LOD Switchers")]
    public void DetectLODSwitchers()
    {
        lodSwitchers.Clear();
        
        if (includeChildren)
        {
            lodSwitchers.AddRange(GetComponentsInChildren<LODSwitcher>());
        }
        else
        {
            lodSwitchers.AddRange(FindObjectsOfType<LODSwitcher>());
        }
        
        Debug.Log($"Detected {lodSwitchers.Count} LOD Switchers");
    }
    
    void UpdateStatistics()
    {
        mMeshObjectsActive = 0;
        mGaussianSplatsActive = 0;
        mCulledObjects = 0;
        
        foreach (var switcher in lodSwitchers)
        {
            if (switcher == null) continue;
            
            if (!switcher.gameObject.activeInHierarchy)
            {
                mCulledObjects++;
                continue;
            }
            
            if (switcher.IsUsingGaussianSplat())
                mGaussianSplatsActive++;
            else
                mMeshObjectsActive++;
        }
    }
    
    void UpdateCulling()
    {
        if (mTargetCamera == null) return;
        
        foreach (var switcher in lodSwitchers)
        {
            if (switcher == null) continue;
            
            float distance = Vector3.Distance(switcher.transform.position, mTargetCamera.transform.position);
            bool shouldBeCulled = distance > cullingDistance;
            
            if (switcher.gameObject.activeSelf == shouldBeCulled)
            {
                switcher.gameObject.SetActive(!shouldBeCulled);
            }
        }
    }
    
    [ContextMenu("Force All to Mesh")]
    public void ForceAllToMesh()
    {
        enableCulling = false;
        foreach (var switcher in lodSwitchers)
        {
            if (switcher != null)
                switcher.ForceMeshLOD();
        }
    }
    
    [ContextMenu("Force All to Gaussian Splat")]
    public void ForceAllToGaussianSplat()
    {
        enableCulling = false;
        foreach (var switcher in lodSwitchers)
        {
            if (switcher != null)
                switcher.ForceGaussianSplatLOD();
        }
    }
    
    public void SetLODDistance(float _distance)
    {
        foreach (var switcher in lodSwitchers)
        {
            if (switcher != null)
                switcher.lodData.switchDistance = _distance;
        }
    }
    
    public void SetUpdateInterval(float _interval)
    {
        foreach (var switcher in lodSwitchers)
        {
            if (switcher != null)
            {
                switcher.updateEveryFrame = _interval <= 0;
                switcher.updateInterval = _interval;
            }
        }
    }
    
    void OnGUI()
    {
        if (!showStatistics) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 320, 300));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"LOD Statistics");
        GUILayout.Label($"FPS: {(1.0f / Time.deltaTime):F1}");
        GUILayout.Label($"Frame Time: {Time.deltaTime * 1000:F2}ms");
        GUILayout.Label($"Total LOD Objects: {lodSwitchers.Count}");
        GUILayout.Label($"Mesh Objects Active: {mMeshObjectsActive}");
        GUILayout.Label($"Gaussian Splats Active: {mGaussianSplatsActive}");
        GUILayout.Label($"Culled Objects: {mCulledObjects}");

        GUILayout.Space(10);
        if (GUILayout.Button("Active LOD Switchers"))
        {
            ActiveLODSwitchers();
        }
        
        
        if (GUILayout.Button("Refresh LOD Switchers"))
        {
            DetectLODSwitchers();
        }
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All Mesh"))
        {
            ForceAllToMesh();
        }
        if (GUILayout.Button("All Gaussian"))
        {
            ForceAllToGaussianSplat();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void OnDrawGizmosSelected()
    {
        if (mTargetCamera != null && enableCulling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mTargetCamera.transform.position, cullingDistance);
        }
    }
}