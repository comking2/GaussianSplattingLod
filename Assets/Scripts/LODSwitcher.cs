using UnityEngine;

public class LODSwitcher : MonoBehaviour
{
    [Header("LOD Settings")]
    public LODData lodData;
    
    [Header("Update Settings")]
    public bool updateEveryFrame = false;
    public float updateInterval = 0.1f;
    
    void Start()
    {
        if (lodData == null)
        {
            lodData = new LODData();
        }
    }

    
    private void SwitchLOD(bool useGaussian)
    {
        lodData.isUsingGaussianSplat = useGaussian;
        
        if (lodData.meshObject != null)
            lodData.meshObject.SetActive(!useGaussian);
            
        if (lodData.gaussianSplatObject != null)
            lodData.gaussianSplatObject.SetActive(useGaussian);
    }
    
    public bool IsUsingGaussianSplat()
    {
        return lodData != null && lodData.isUsingGaussianSplat;
    }
    
    [ContextMenu("Force Mesh LOD")]
    public void ForceMeshLOD()
    {
        SwitchLOD(false);
    }
    
    [ContextMenu("Force Gaussian Splat LOD")]
    public void ForceGaussianSplatLOD()
    {
        SwitchLOD(true);
    }
}