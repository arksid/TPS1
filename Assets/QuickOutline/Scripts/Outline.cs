//
//  Outline.cs
//  QuickOutline (safe version with isReadable guards)
//
//  Base by Chris Nolet, edits for isReadable guards
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour
{
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    public enum Mode
    {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly
    }

    public Mode OutlineMode
    {
        get { return outlineMode; }
        set { outlineMode = value; needsUpdate = true; }
    }

    public Color OutlineColor
    {
        get { return outlineColor; }
        set { outlineColor = value; needsUpdate = true; }
    }

    public float OutlineWidth
    {
        get { return outlineWidth; }
        set { outlineWidth = value; needsUpdate = true; }
    }

    [Serializable]
    private class ListVector3 { public List<Vector3> data; }

    [SerializeField] private Mode outlineMode;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 2f;

    [Header("Optional")]
    [SerializeField, Tooltip(
      "Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. " +
      "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes."
    )]
    private bool precomputeOutline;

    [SerializeField, HideInInspector] private List<Mesh> bakeKeys = new List<Mesh>();
    [SerializeField, HideInInspector] private List<ListVector3> bakeValues = new List<ListVector3>();

    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;

    private bool needsUpdate;

    void Awake()
    {
        // Cache renderers
        renderers = GetComponentsInChildren<Renderer>(true);

        // Instantiate outline materials
        outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
        outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));
        outlineMaskMaterial.name = "OutlineMask (Instance)";
        outlineFillMaterial.name = "OutlineFill (Instance)";

        // Retrieve or generate smooth normals
        LoadSmoothNormals();

        // Apply material properties immediately
        needsUpdate = true;
    }

    void OnEnable()
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            // Append outline shaders
            var materials = renderer.sharedMaterials.ToList();
            materials.Add(outlineMaskMaterial);
            materials.Add(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnValidate()
    {
        // Update material properties
        needsUpdate = true;

        // Clear cache when baking is disabled or corrupted
        if ((!precomputeOutline && bakeKeys.Count != 0) || bakeKeys.Count != bakeValues.Count)
        {
            bakeKeys.Clear();
            bakeValues.Clear();
        }

        // Generate smooth normals when baking is enabled
        if (precomputeOutline && bakeKeys.Count == 0)
        {
            Bake();
        }
    }

    void Update()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    void OnDisable()
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            // Remove outline shaders
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMaskMaterial);
            materials.Remove(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnDestroy()
    {
        // Destroy material instances
        if (outlineMaskMaterial) Destroy(outlineMaskMaterial);
        if (outlineFillMaterial) Destroy(outlineFillMaterial);
    }

    // -----------------------------
    // Baking (Editor) - optional
    // -----------------------------
    void Bake()
    {
        // Generate smooth normals for each mesh
        var bakedMeshes = new HashSet<Mesh>();

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = meshFilter ? meshFilter.sharedMesh : null;
            if (mesh == null) continue;

            // Skip duplicates
            if (!bakedMeshes.Add(mesh)) continue;

            // ⚠️ Skip if mesh is not readable
            if (!mesh.isReadable) continue;

            // Serialize smooth normals
            var smoothNormals = SmoothNormals(mesh);
            bakeKeys.Add(mesh);
            bakeValues.Add(new ListVector3 { data = smoothNormals });
        }
    }

    // -----------------------------
    // Runtime normals / UV4 setup
    // -----------------------------
    void LoadSmoothNormals()
    {
        // MeshFilter targets
        var meshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in meshFilters)
        {
            if (mf == null) continue;
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;

            // Skip if already processed
            if (!registeredMeshes.Add(mesh)) continue;

            // If we baked, fetch; else compute at runtime — only when readable
            List<Vector3> smoothNormals = null;
            var bakedIndex = bakeKeys.IndexOf(mesh);

            if (bakedIndex >= 0)
            {
                smoothNormals = bakeValues[bakedIndex].data;
            }
            else
            {
                if (!mesh.isReadable) continue; // ✅ hard guard
                smoothNormals = SmoothNormals(mesh);
            }

            // Store smooth normals in UV4 (index 3)
            if (mesh.isReadable) // ✅ SetUVs also requires readable
            {
                mesh.SetUVs(3, smoothNormals);
            }

            // Combine submeshes only if we have a renderer
            var r = mf.GetComponent<Renderer>();
            if (r != null)
            {
                CombineSubmeshes(mesh, r.sharedMaterials);
            }
        }

        // SkinnedMeshRenderer targets
        var skinneds = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in skinneds)
        {
            if (smr == null) continue;
            var mesh = smr.sharedMesh;
            if (mesh == null) continue;

            // Skip if already processed
            if (!registeredMeshes.Add(mesh)) continue;

            if (mesh.isReadable)
            {
                // Clear & rebuild UV4 using smooth normals
                var smooth = SmoothNormals(mesh);
                mesh.SetUVs(3, smooth);
            }
            else
            {
                // Not readable — ensure uv4 length is valid without accessing vertices
                // (leave as-is; no SetUVs since that also requires isReadable)
            }

            CombineSubmeshes(mesh, smr.sharedMaterials);
        }
    }

    // -----------------------------
    // Safe normal smoothing
    // -----------------------------
    List<Vector3> SmoothNormals(Mesh mesh)
    {
        // ✅ If not readable, return a dummy list with correct length to avoid errors
        if (mesh == null) return new List<Vector3>(0);
        if (!mesh.isReadable) return new List<Vector3>(mesh.vertexCount);

        // Group vertices by location
        var groups = mesh.vertices
                         .Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index))
                         .GroupBy(pair => pair.Key);

        // Copy normals to a new list
        var smoothNormals = new List<Vector3>(mesh.normals);

        // Average normals for grouped vertices
        foreach (var group in groups)
        {
            if (group.Count() == 1) continue;

            var smoothNormal = Vector3.zero;
            foreach (var pair in group) smoothNormal += smoothNormals[pair.Value];
            smoothNormal.Normalize();

            foreach (var pair in group) smoothNormals[pair.Value] = smoothNormal;
        }

        return smoothNormals;
    }

    // -----------------------------
    // Submesh combiner (unchanged)
    // -----------------------------
    void CombineSubmeshes(Mesh mesh, Material[] materials)
    {
        if (mesh == null) return;

        // Skip meshes with a single submesh
        if (mesh.subMeshCount == 1) return;

        // Skip if submesh count exceeds material count
        if (materials == null || mesh.subMeshCount > materials.Length) return;

        // Append combined submesh
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    // -----------------------------
    // Material props
    // -----------------------------
    void UpdateMaterialProperties()
    {
        if (outlineFillMaterial == null || outlineMaskMaterial == null) return;

        outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

        switch (outlineMode)
        {
            case Mode.OutlineAll:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineVisible:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineHidden:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineAndSilhouette:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.SilhouetteOnly:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
                break;
        }
    }
}
