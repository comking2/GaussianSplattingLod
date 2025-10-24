// Assets/Editor/GSPlyExporter.cs
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GSPlyExporter : EditorWindow
{
    public enum SampleMode { Vertex, TriangleCentroid }
    public enum ColorEnc { Aras, SHCentered, CustomAffine } // f_dc 인코딩

    [SerializeField] GameObject target;
    [SerializeField] SampleMode mode = SampleMode.Vertex;

    // 렌더 파라미터
    [SerializeField] float alpha = 0.25f;                                  // 0..1 → logit
    [SerializeField] Vector3 manualSigma = new Vector3(0.01f, 0.01f, 1e-6f); // σ 수동값
    [SerializeField] bool useMaterialAlbedo = true;
    [SerializeField] bool bakeSkinned = true;
    [SerializeField] bool exportNormals = true; // 기록만 토글
    [SerializeField] float colorBoost = 1.0f;

    // σ 추정
    [SerializeField] bool autoSigmaXY = true;   // σx,σy 자동
    [SerializeField] float sigmaK = 0.8f;       // r→σ (0.1~2.0)
    [SerializeField] float sigmaZConst = 1e-6f; // σz 상수

    // 색 인코딩/보정
    [SerializeField] ColorEnc colorEnc = ColorEnc.Aras;
    [SerializeField] float exposure = 1.0f;            // 선형 노출
    [SerializeField] Vector3 aRGB = new Vector3(1,1,1);// CustomAffine a
    [SerializeField] Vector3 bRGB = new Vector3(0,0,0);// CustomAffine b

    // 선택적 색감 보정(선형 영역)
    [SerializeField] Vector3 whiteBalance = new Vector3(1f,1f,1f); // R,G,B 게인
    [SerializeField] float saturation = 1.0f;    // 0.5~1.5
    [SerializeField] float contrast  = 1.0f;     // 0.5~1.5
    [SerializeField] float gammaOut  = 1.0f;     // 0.7~1.5 (선형 pow)

    // 회전 기록 순서
    [SerializeField] bool rotWFirst = true;      // rot_0..3 = w,x,y,z

    [MenuItem("Tools/GS/Export PLY (3DGS)…")]
    static void Open() => GetWindow<GSPlyExporter>("GS PLY Exporter (3DGS)").Show();

    void OnGUI()
    {
        target = (GameObject)EditorGUILayout.ObjectField("Target", target, typeof(GameObject), true);
        mode = (SampleMode)EditorGUILayout.EnumPopup("Sample Mode", mode);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
        alpha = EditorGUILayout.Slider("Alpha (0..1)", alpha, 0f, 1f);
        manualSigma = EditorGUILayout.Vector3Field("Manual σ(x,y,z)", manualSigma);
        useMaterialAlbedo = EditorGUILayout.Toggle("Use Material Albedo", useMaterialAlbedo);
        bakeSkinned = EditorGUILayout.Toggle("Bake Skinned", bakeSkinned);
        exportNormals = EditorGUILayout.Toggle("Export Normals", exportNormals);
        colorBoost = EditorGUILayout.Slider("Color Boost", colorBoost, 0.1f, 3.0f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sigma", EditorStyles.boldLabel);
        autoSigmaXY = EditorGUILayout.Toggle("Auto σx,σy (geom-based)", autoSigmaXY);
        sigmaK = EditorGUILayout.Slider("σ scale k", sigmaK, 0.1f, 2.0f);
        sigmaZConst = EditorGUILayout.FloatField("σz constant", sigmaZConst);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Color Encoding", EditorStyles.boldLabel);
        colorEnc = (ColorEnc)EditorGUILayout.EnumPopup("f_dc Encoding", colorEnc);
        exposure = EditorGUILayout.Slider("Exposure (linear)", exposure, 0.5f, 2.0f);
        if (colorEnc == ColorEnc.CustomAffine)
        {
            aRGB = EditorGUILayout.Vector3Field("a (R,G,B)", aRGB);
            bRGB = EditorGUILayout.Vector3Field("b (R,G,B)", bRGB);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Look/Feel Corrections (linear)", EditorStyles.boldLabel);
        whiteBalance = EditorGUILayout.Vector3Field("White Balance (R,G,B)", whiteBalance);
        saturation   = EditorGUILayout.Slider("Saturation", saturation, 0.5f, 1.5f);
        contrast     = EditorGUILayout.Slider("Contrast",  contrast,  0.5f, 1.5f);
        gammaOut     = EditorGUILayout.Slider("GammaOut",  gammaOut,  0.7f, 1.5f);

        EditorGUILayout.Space();
        rotWFirst = EditorGUILayout.Toggle("Write rot as w,x,y,z", rotWFirst);

        GUI.enabled = target;
        if (GUILayout.Button("Export .ply (3DGS spec)"))
        {
            var path = EditorUtility.SaveFilePanel("Save PLY", Application.dataPath, target.name + "_gs.ply", "ply");
            if (!string.IsNullOrEmpty(path))
            {
                var opt = new Options
                {
                    mode = mode,
                    alpha = alpha,
                    manualSigma = manualSigma,
                    useMaterialAlbedo = useMaterialAlbedo,
                    bakeSkinned = bakeSkinned,
                    exportNormals = exportNormals,
                    colorBoost = colorBoost,
                    autoSigmaXY = autoSigmaXY,
                    sigmaK = sigmaK,
                    sigmaZConst = sigmaZConst,
                    colorEnc = colorEnc,
                    exposure = exposure,
                    aRGB = aRGB,
                    bRGB = bRGB,
                    whiteBalance = whiteBalance,
                    saturation = saturation,
                    contrast = contrast,
                    gammaOut = gammaOut,
                    rotWFirst = rotWFirst
                };
                try { Export(target, path, opt); EditorUtility.RevealInFinder(path); }
                catch (Exception e) { Debug.LogError(e); }
            }
        }
        GUI.enabled = true;
    }

    struct Options
    {
        public SampleMode mode;
        public float alpha;
        public Vector3 manualSigma;
        public bool useMaterialAlbedo;
        public bool bakeSkinned;
        public bool exportNormals;
        public float colorBoost;

        public bool autoSigmaXY;
        public float sigmaK;
        public float sigmaZConst;

        public ColorEnc colorEnc;
        public float exposure;
        public Vector3 aRGB;
        public Vector3 bRGB;

        public Vector3 whiteBalance;
        public float saturation;
        public float contrast;
        public float gammaOut;

        public bool rotWFirst;
    }

    struct SplatSample
    {
        public Vector3 pos;
        public Vector3 nrm;         // 단위벡터
        public Color colLinear;     // 선형 RGB 0..1
        public Vector3 sigma;       // σx,σy,σz
        public SplatSample(Vector3 p, Vector3 n, Color lin, Vector3 s)
        { pos = p; nrm = n.sqrMagnitude > 0 ? n.normalized : Vector3.up; colLinear = lin; sigma = s; }
    }

    class MeshData
    {
        public Vector3[] vertices;
        public Vector3[] normals;   // null 가능
        public Vector2[] uvs;       // null 가능
        public Color[] colors;      // null 가능
        public int[] indices;
        public int vertCount => vertices.Length;
    }

    static void Export(GameObject go, string path, Options opt)
    {
        var meshes = CollectMeshes(go, opt.bakeSkinned, out var sources);
        if (meshes.Count == 0) throw new Exception("No mesh found.");

        var mainTex = FindMainTexture(sources);
        var materialColor = FindMaterialColor(sources);

        var samples = new List<SplatSample>(1 << 18);
        bool firstSample = true;

        Color SampleLinearAlbedo(Vector2 uv)
        {
            if (mainTex != null)
            {
                var srgb = mainTex.GetPixelBilinear(uv.x, uv.y); // sRGB 샘플
                var lin = srgb.linear;                           // sRGB→Linear
                if (firstSample) { Debug.Log($"Sampled linear: {lin}, UV: {uv}"); firstSample = false; }
                return lin;
            }
            return materialColor.linear;
        }

        foreach (var m in meshes)
        {
            float[] rVertex = null;
            if (opt.mode == SampleMode.Vertex && opt.autoSigmaXY)
                rVertex = EstimateVertexRadiusByArea(m);

            if (opt.mode == SampleMode.Vertex)
            {
                for (int i = 0; i < m.vertCount; i++)
                {
                    var pos = m.vertices[i];
                    var nrm = m.normals != null && m.normals.Length == m.vertCount ? m.normals[i] : Vector3.up;

                    Color lin = opt.useMaterialAlbedo
                        ? SampleLinearAlbedo(m.uvs != null && m.uvs.Length == m.vertCount ? m.uvs[i] : Vector2.zero)
                        : ((m.colors != null && m.colors.Length == m.vertCount) ? m.colors[i].linear : materialColor.linear);

                    lin *= opt.colorBoost;

                    Vector3 sigma = opt.autoSigmaXY
                        ? new Vector3(
                            Mathf.Max(1e-12f, opt.sigmaK * rVertex[i]),
                            Mathf.Max(1e-12f, opt.sigmaK * rVertex[i]),
                            Mathf.Max(1e-12f, opt.sigmaZConst))
                        : new Vector3(
                            Mathf.Max(1e-12f, opt.manualSigma.x),
                            Mathf.Max(1e-12f, opt.manualSigma.y),
                            Mathf.Max(1e-12f, opt.manualSigma.z));

                    samples.Add(new SplatSample(pos, nrm, lin, sigma));
                }
            }
            else
            {
                var idx = m.indices;
                for (int t = 0; t < idx.Length; t += 3)
                {
                    int i0 = idx[t], i1 = idx[t + 1], i2 = idx[t + 2];
                    var p0 = m.vertices[i0]; var p1 = m.vertices[i1]; var p2 = m.vertices[i2];
                    var pos = (p0 + p1 + p2) / 3f;

                    Vector3 nrm;
                    if (m.normals != null && m.normals.Length == m.vertCount)
                        nrm = (m.normals[i0] + m.normals[i1] + m.normals[i2]).normalized;
                    else
                        nrm = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));

                    Color lin;
                    if (opt.useMaterialAlbedo)
                    {
                        var uv0 = m.uvs != null ? m.uvs[i0] : Vector2.zero;
                        var uv1 = m.uvs != null ? m.uvs[i1] : Vector2.zero;
                        var uv2 = m.uvs != null ? m.uvs[i2] : Vector2.zero;
                        lin = SampleLinearAlbedo((uv0 + uv1 + uv2) / 3f);
                    }
                    else
                    {
                        var c0 = m.colors != null ? m.colors[i0].linear : materialColor.linear;
                        var c1 = m.colors != null ? m.colors[i1].linear : materialColor.linear;
                        var c2 = m.colors != null ? m.colors[i2].linear : materialColor.linear;
                        lin = ((c0 + c1 + c2) / 3f);
                    }

                    lin *= opt.colorBoost;

                    float area = 0.5f * Vector3.Cross(p1 - p0, p2 - p0).magnitude;
                    float r = area > 1e-20f ? Mathf.Sqrt(area / Mathf.PI) : 0f;

                    Vector3 sigma = opt.autoSigmaXY
                        ? new Vector3(
                            Mathf.Max(1e-12f, opt.sigmaK * r),
                            Mathf.Max(1e-12f, opt.sigmaK * r),
                            Mathf.Max(1e-12f, opt.sigmaZConst))
                        : new Vector3(
                            Mathf.Max(1e-12f, opt.manualSigma.x),
                            Mathf.Max(1e-12f, opt.manualSigma.y),
                            Mathf.Max(1e-12f, opt.manualSigma.z));

                    samples.Add(new SplatSample(pos, nrm, lin, sigma));
                }
            }
        }

        WritePly3DGS(path, samples, opt);
        Debug.Log($"PLY written: {path}  vertices={samples.Count}");
    }

    static List<MeshData> CollectMeshes(GameObject go, bool bakeSkinned, out List<(Renderer r, Material[] mats)> src)
    {
        var list = new List<MeshData>();
        src = new List<(Renderer, Material[])>();

        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (!mf.sharedMesh || !mr) continue;
            var md = ExtractMesh(mf.sharedMesh, mf.transform.localToWorldMatrix);
            list.Add(md); src.Add((mr, mr.sharedMaterials));
        }

        foreach (var sk in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!sk.sharedMesh) continue;
            Mesh baked = new Mesh();
            if (bakeSkinned) sk.BakeMesh(baked, true);
            else baked = UnityEngine.Object.Instantiate(sk.sharedMesh);

            var md = ExtractMesh(baked, sk.localToWorldMatrix);
            list.Add(md); src.Add((sk, sk.sharedMaterials));
        }
        return list;
    }

    static MeshData ExtractMesh(Mesh mesh, Matrix4x4 l2w)
    {
        var md = new MeshData();
        var v = mesh.vertices;
        var n = mesh.normals;
        var uvList = new List<Vector2>();
        mesh.GetUVs(0, uvList);
        var uv = uvList.Count == mesh.vertexCount ? uvList.ToArray() : null;
        var c = mesh.colors;

        var V = new Vector3[v.Length];
        var hasN = n != null && n.Length == v.Length;
        var N = hasN ? new Vector3[n.Length] : null;

        for (int i = 0; i < v.Length; i++)
        {
            V[i] = l2w.MultiplyPoint3x4(v[i]);
            if (hasN)
            {
                var nn = l2w.MultiplyVector(n[i]);
                N[i] = nn.sqrMagnitude > 0 ? nn.normalized : Vector3.up;
            }
        }

        md.vertices = V;
        md.normals = N;
        md.uvs = uv;
        md.colors = (c != null && c.Length == v.Length) ? c : null;
        md.indices = mesh.triangles;
        return md;
    }

    static Texture2D FindMainTexture(List<(Renderer r, Material[] mats)> sources)
    {
        foreach (var (r, mats) in sources)
        {
            foreach (var m in mats)
            {
                if (!m) continue;
                var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap")
                         : m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                if (tex is Texture2D t2)
                {
                    if (t2.isReadable) return t2;

                    // 읽기 불가 텍스처 임시 복사: sRGB 유지
                    var rt = RenderTexture.GetTemporary(t2.width, t2.height);
                    Graphics.Blit(t2, rt);
                    var readable = new Texture2D(t2.width, t2.height, TextureFormat.RGBA32, false, false); // sRGB
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    readable.ReadPixels(new Rect(0, 0, t2.width, t2.height), 0, 0);
                    readable.Apply();
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    return readable;
                }
            }
        }
        return null;
    }

    static Color FindMaterialColor(List<(Renderer r, Material[] mats)> sources)
    {
        foreach (var (r, mats) in sources)
        {
            foreach (var m in mats)
            {
                if (!m) continue;
                if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
                if (m.HasProperty("_Color")) return m.GetColor("_Color");
                if (m.HasProperty("_MainColor")) return m.GetColor("_MainColor");
            }
        }
        return Color.white;
    }

    static Quaternion LookRotationFromNormal(Vector3 n)
    {
        var f = n.sqrMagnitude > 0 ? n.normalized : Vector3.forward;
        var up = Mathf.Abs(Vector3.Dot(f, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        var t = Vector3.Cross(up, f).normalized;
        var u = Vector3.Cross(f, t);
        var m = new Matrix4x4();
        m.SetColumn(0, new Vector4(t.x, t.y, t.z, 0));
        m.SetColumn(1, new Vector4(u.x, u.y, u.z, 0));
        m.SetColumn(2, new Vector4(f.x, f.y, f.z, 0));
        m.SetColumn(3, new Vector4(0, 0, 0, 1));
        return m.rotation;
    }

    static void WritePly3DGS(string path, List<SplatSample> samples, Options opt)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs, new UTF8Encoding(false));

        // 3DGS 헤더: f_rest_0..44 포함
        var sb = new StringBuilder();
        sb.AppendLine("ply");
        sb.AppendLine("format binary_little_endian 1.0");
        sb.AppendLine($"element vertex {samples.Count}");
        sb.AppendLine("property float x");
        sb.AppendLine("property float y");
        sb.AppendLine("property float z");
        sb.AppendLine("property float nx");
        sb.AppendLine("property float ny");
        sb.AppendLine("property float nz");
        sb.AppendLine("property float f_dc_0");
        sb.AppendLine("property float f_dc_1");
        sb.AppendLine("property float f_dc_2");
        for (int i = 0; i < 45; i++) sb.AppendLine($"property float f_rest_{i}");
        sb.AppendLine("property float opacity");
        sb.AppendLine("property float scale_0");
        sb.AppendLine("property float scale_1");
        sb.AppendLine("property float scale_2");
        sb.AppendLine("property float rot_0");
        sb.AppendLine("property float rot_1");
        sb.AppendLine("property float rot_2");
        sb.AppendLine("property float rot_3");
        sb.AppendLine("end_header");
        bw.Write(Encoding.ASCII.GetBytes(sb.ToString()));

        const float Y00 = 0.2820947918f; // SH DC 정규화

        // 알파→로짓
        float a = Mathf.Clamp(opt.alpha, 1e-6f, 1f - 1e-6f);
        float a_logit = Mathf.Log(a / (1f - a));

        foreach (var s in samples)
        {
            Vector3 p = SafeV3(s.pos);

            // 회전계산은 실제 법선 사용, 기록은 옵션 적용
            Vector3 nForRot = SafeV3(s.nrm);
            Quaternion q = SafeQ(LookRotationFromNormal(nForRot));
            Vector3 nToWrite = opt.exportNormals ? nForRot : Vector3.forward;

            // 선형 색 + 노출
            Color c = s.colLinear * opt.exposure;

            // 선택적 색감 보정(선형)
            // 1) 화이트밸런스
            c.r *= opt.whiteBalance.x;
            c.g *= opt.whiteBalance.y;
            c.b *= opt.whiteBalance.z;
            // 2) 채도 (BT.709 luma)
            float Y = 0.2126f*c.r + 0.7152f*c.g + 0.0722f*c.b;
            c.r = Mathf.Lerp(Y, c.r, opt.saturation);
            c.g = Mathf.Lerp(Y, c.g, opt.saturation);
            c.b = Mathf.Lerp(Y, c.b, opt.saturation);
            // 3) 대비(피벗 0.5)
            c.r = (c.r - 0.5f)*opt.contrast + 0.5f;
            c.g = (c.g - 0.5f)*opt.contrast + 0.5f;
            c.b = (c.b - 0.5f)*opt.contrast + 0.5f;
            // 4) 감마 유사 보정(선형 pow)
            float invGamma = 1.0f / Mathf.Max(1e-6f, opt.gammaOut);
            c.r = Mathf.Pow(Mathf.Clamp01(c.r), invGamma);
            c.g = Mathf.Pow(Mathf.Clamp01(c.g), invGamma);
            c.b = Mathf.Pow(Mathf.Clamp01(c.b), invGamma);

            // f_dc 인코딩
            float fdc0, fdc1, fdc2;
            switch (opt.colorEnc)
            {
                case ColorEnc.SHCentered: // (c-0.5)/Y00
                    fdc0 = (c.r - 0.5f) / Y00;
                    fdc1 = (c.g - 0.5f) / Y00;
                    fdc2 = (c.b - 0.5f) / Y00;
                    break;
                case ColorEnc.CustomAffine: // (a*c + b)/Y00
                    fdc0 = (opt.aRGB.x * c.r + opt.bRGB.x) / Y00;
                    fdc1 = (opt.aRGB.y * c.g + opt.bRGB.y) / Y00;
                    fdc2 = (opt.aRGB.z * c.b + opt.bRGB.z) / Y00;
                    break;
                default: // Aras: c * Y00
                    fdc0 = c.r * Y00;
                    fdc1 = c.g * Y00;
                    fdc2 = c.b * Y00;
                    break;
            }

            // σ → ln(σ)
            Vector3 sg = s.sigma;
            float sx = Mathf.Log(Mathf.Max(1e-12f, sg.x));
            float sy = Mathf.Log(Mathf.Max(1e-12f, sg.y));
            float sz = Mathf.Log(Mathf.Max(1e-12f, sg.z));

            // 쓰기
            bw.Write(p.x); bw.Write(p.y); bw.Write(p.z);                    // x y z
            bw.Write(nToWrite.x); bw.Write(nToWrite.y); bw.Write(nToWrite.z); // nx ny nz
            bw.Write(SafeF(fdc0)); bw.Write(SafeF(fdc1)); bw.Write(SafeF(fdc2)); // f_dc_0..2
            for (int i = 0; i < 45; i++) bw.Write(0f);                      // f_rest_0..44
            bw.Write(a_logit);                                              // opacity(logit)
            bw.Write(sx); bw.Write(sy); bw.Write(sz);                       // scale_0..2 (ln σ)
            if (opt.rotWFirst) { bw.Write(q.w); bw.Write(q.x); bw.Write(q.y); bw.Write(q.z); }
            else               { bw.Write(q.x); bw.Write(q.y); bw.Write(q.z); bw.Write(q.w); }
        }
    }

    static float SafeF(float v) => float.IsFinite(v) ? v : 0f;
    static Vector3 SafeV3(Vector3 v) => new Vector3(SafeF(v.x), SafeF(v.y), SafeF(v.z));
    static Quaternion SafeQ(Quaternion q)
    {
        float m = Mathf.Sqrt(q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w);
        if (m < 1e-8f) return Quaternion.identity;
        float inv = 1f / m;
        return new Quaternion(q.x*inv, q.y*inv, q.z*inv, q.w*inv);
    }

    static float[] EstimateVertexRadiusByArea(MeshData m)
    {
        var rSum = new float[m.vertCount];
        var rCnt = new int[m.vertCount];
        var idx = m.indices;
        for (int t = 0; t < idx.Length; t += 3)
        {
            int i0 = idx[t], i1 = idx[t+1], i2 = idx[t+2];
            var p0 = m.vertices[i0]; var p1 = m.vertices[i1]; var p2 = m.vertices[i2];
            float area = 0.5f * Vector3.Cross(p1 - p0, p2 - p0).magnitude;
            if (area <= 1e-20f) continue;
            float r = Mathf.Sqrt(area / Mathf.PI); // 등가원 반경
            rSum[i0] += r; rSum[i1] += r; rSum[i2] += r;
            rCnt[i0]++;  rCnt[i1]++;  rCnt[i2]++;
        }
        var rAvg = new float[m.vertCount];
        float fallback = 0f;
        if (m.vertCount >= 2)
        {
            int sample = Mathf.Min(m.vertCount - 1, 256);
            for (int i = 1; i <= sample; ++i) fallback += (m.vertices[i] - m.vertices[0]).magnitude;
            fallback = (fallback / Mathf.Max(1, sample)) * 0.25f;
        }
        for (int i = 0; i < m.vertCount; i++)
            rAvg[i] = rCnt[i] > 0 ? (rSum[i] / rCnt[i]) : fallback;
        return rAvg;
    }
}
