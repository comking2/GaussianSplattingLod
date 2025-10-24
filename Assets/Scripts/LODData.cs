using UnityEngine;

[System.Serializable]
public class LODData
{
    public GameObject meshObject;
    public GameObject gaussianSplatObject;
    public float switchDistance = 50f;
    public float hysteresis = 5f;
    public bool isUsingGaussianSplat = false;
}