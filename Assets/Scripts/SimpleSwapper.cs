using UnityEngine;

public class SimpleSwapper : MonoBehaviour
{
    [Header("Objects")]
    public GameObject meshObject;
    public GameObject gaussianSplatObject;
    
    [Header("Control")]
    [SerializeField] private bool _showMesh = true;
    
    public bool showMesh
    {
        get { return _showMesh; }
        set 
        { 
            _showMesh = value;
            UpdateDisplay();
        }
    }
    
    [Header("Debug")]
    public KeyCode toggleKey = KeyCode.Space;
    
    void Start()
    {
        UpdateDisplay();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }
    
    [ContextMenu("Toggle Mesh/Gaussian")]
    public void Toggle()
    {
        showMesh = !showMesh;
        UpdateDisplay();
    }
    
    [ContextMenu("Show Mesh")]
    public void ShowMesh()
    {
        showMesh = true;
        UpdateDisplay();
    }
    
    [ContextMenu("Show Gaussian Splat")]
    public void ShowGaussianSplat()
    {
        showMesh = false;
        UpdateDisplay();
    }
    
    public void UpdateDisplay()
    {
        if (meshObject != null)
            meshObject.SetActive(_showMesh);
            
        if (gaussianSplatObject != null)
            gaussianSplatObject.SetActive(!_showMesh);
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 100, 200, 100));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"Current: {(showMesh ? "Mesh" : "Gaussian Splat")}");
        
        if (GUILayout.Button("Toggle (Space)"))
        {
            Toggle();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}