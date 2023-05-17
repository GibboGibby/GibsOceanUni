using System.Collections.Generic;
using UnityEngine;
namespace GibsOcean {
public class OceanRenderer : MonoBehaviour
{
    [SerializeField] 
    WaveGen wavesGenerator;
    [SerializeField]
    Transform viewer;
    [SerializeField]
    Material oceanMaterial;

    [SerializeField]
    float lengthScale = 10;
    [SerializeField, Range(1, 40)]
    int vertexDensity = 30;
    [SerializeField, Range(0, 8)]
    int clipLevels = 8;
    [SerializeField, Range(0, 100)]
    float skirtSize = 50;

    List<Utils.MeshUtil.MeshTransform> rings = new List<Utils.MeshUtil.MeshTransform>();
    List<Utils.MeshUtil.MeshTransform> trims = new List<Utils.MeshUtil.MeshTransform>();
    Utils.MeshUtil.MeshTransform center;
    Utils.MeshUtil.MeshTransform skirt;
    Quaternion[] trimRotations;
    int previousVertexDensity;
    float previousSkirtSize;

    private Material[] materials;

    private void Start()
    {
        if (viewer == null)
            viewer = Camera.main.transform;

        oceanMaterial.SetTexture("_Displacement_c0", wavesGenerator.cascade0.Displacement);
        oceanMaterial.SetTexture("_Derivatives_c0", wavesGenerator.cascade0.Derivatives);
        oceanMaterial.SetTexture("_Turbulence_c0", wavesGenerator.cascade0.Turbulence);
        
        oceanMaterial.SetTexture("_Displacement_c1", wavesGenerator.cascade1.Displacement);
        oceanMaterial.SetTexture("_Derivatives_c1", wavesGenerator.cascade1.Derivatives);
        oceanMaterial.SetTexture("_Turbulence_c1", wavesGenerator.cascade1.Turbulence);
        
        oceanMaterial.SetTexture("_Displacement_c2", wavesGenerator.cascade2.Displacement);
        oceanMaterial.SetTexture("_Derivatives_c2", wavesGenerator.cascade2.Derivatives);
        oceanMaterial.SetTexture("_Turbulence_c2", wavesGenerator.cascade2.Turbulence);

        materials = new Material[3];
        materials[0] = new Material(oceanMaterial);
        materials[0].EnableKeyword("CLOSE");

        materials[1] = new Material(oceanMaterial);
        materials[1].EnableKeyword("MID");
        materials[1].DisableKeyword("CLOSE");

        materials[2] = new Material(oceanMaterial);
        materials[2].DisableKeyword("MID");
        materials[2].DisableKeyword("CLOSE");
        
        

        trimRotations = new Quaternion[]
        {
            Quaternion.AngleAxis(180, Vector3.up),
            Quaternion.AngleAxis(90, Vector3.up),
            Quaternion.AngleAxis(270, Vector3.up),
            Quaternion.identity,
        };

        InstantiateMeshes();
    }

    public void SetMaterial(Material newMaterial, int matNumber)
    {
        materials[matNumber].CopyPropertiesFromMaterial(newMaterial);
        
        materials[0].EnableKeyword("CLOSE");
        materials[1].EnableKeyword("MID");
        materials[1].DisableKeyword("CLOSE");
        materials[2].DisableKeyword("MID");
        materials[2].DisableKeyword("CLOSE");
    }

    public void SetMaterials(Material newMaterial)
    {
        for (int i = 0; i < 3; i++)
            materials[i].CopyPropertiesFromMaterial(newMaterial);
        
        materials[0].EnableKeyword("CLOSE");
        materials[1].EnableKeyword("MID");
        materials[1].DisableKeyword("CLOSE");
        materials[2].DisableKeyword("MID");
        materials[2].DisableKeyword("CLOSE");
        
    }

    public Material GetMaterial()
    {
        return materials[0];
    }

    private void Update()
    {
        if (rings.Count != clipLevels || trims.Count != clipLevels
            || previousVertexDensity != vertexDensity || !Mathf.Approximately(previousSkirtSize, skirtSize))
        {
            InstantiateMeshes();
            previousVertexDensity = vertexDensity;
            previousSkirtSize = skirtSize;
        }

        UpdatePositions();
        UpdateMaterials();
    }

    void UpdateMaterials()
    {
        int activeLevels = ActiveLodlevels();
        center.MeshRenderer.material = GetMaterial(clipLevels - activeLevels - 1);

        for (int i = 0; i < rings.Count; i++)
        {
            rings[i].MeshRenderer.material = GetMaterial(clipLevels - activeLevels + i);
            trims[i].MeshRenderer.material = GetMaterial(clipLevels - activeLevels + i);
        }
    }

    Material GetMaterial(int lodLevel)
    {
        if (lodLevel - 2 <= 0)
            return materials[0];

        if (lodLevel - 2 <= 2)
            return materials[1];

        return materials[2];
    }

    void UpdatePositions()
    {
        int k = GridSize();
        int activeLevels = ActiveLodlevels();

        float scale = ClipLevelScale(-1, activeLevels);
        Vector3 previousSnappedPosition = Snap(viewer.position, scale * 2);
        center.Transform.position = previousSnappedPosition + OffsetFromCenter(-1, activeLevels);
        center.Transform.localScale = new Vector3(scale, 1, scale);

        for (int i = 0; i < clipLevels; i++)
        {
            rings[i].Transform.gameObject.SetActive(i < activeLevels);
            trims[i].Transform.gameObject.SetActive(i < activeLevels);
            if (i >= activeLevels) continue;

            scale = ClipLevelScale(i, activeLevels);
            Vector3 centerOffset = OffsetFromCenter(i, activeLevels);
            Vector3 snappedPosition = Snap(viewer.position, scale * 2);

            Vector3 trimPosition = centerOffset + snappedPosition + scale * (k - 1) / 2 * new Vector3(1, 0, 1);
            int shiftX = previousSnappedPosition.x - snappedPosition.x < float.Epsilon ? 1 : 0;
            int shiftZ = previousSnappedPosition.z - snappedPosition.z < float.Epsilon ? 1 : 0;
            trimPosition += shiftX * (k + 1) * scale * Vector3.right;
            trimPosition += shiftZ * (k + 1) * scale * Vector3.forward;
            trims[i].Transform.position = trimPosition;
            trims[i].Transform.rotation = trimRotations[shiftX + 2 * shiftZ];
            trims[i].Transform.localScale = new Vector3(scale, 1, scale);

            rings[i].Transform.position = snappedPosition + centerOffset;
            rings[i].Transform.localScale = new Vector3(scale, 1, scale);
            previousSnappedPosition = snappedPosition;
        }

        scale = lengthScale * 2 * Mathf.Pow(2, clipLevels);
        skirt.Transform.position = new Vector3(-1, 0, -1) * scale * (skirtSize + 0.5f - 0.5f / GridSize()) + previousSnappedPosition;
        skirt.Transform.localScale = new Vector3(scale, 1, scale);
    }

    int ActiveLodlevels()
    {
        return clipLevels - Mathf.Clamp((int)Mathf.Log((1.7f * Mathf.Abs(viewer.position.y) + 1) / lengthScale, 2), 0, clipLevels);
    }

    float ClipLevelScale(int level, int activeLevels)
    {
        return lengthScale / GridSize() * Mathf.Pow(2, clipLevels - activeLevels + level + 1);
    }

    Vector3 OffsetFromCenter(int level, int activeLevels)
    {
        return (Mathf.Pow(2, clipLevels) + GeometricProgressionSum(2, 2, clipLevels - activeLevels + level + 1, clipLevels - 1))
               * lengthScale / GridSize() * (GridSize() - 1) / 2 * new Vector3(-1, 0, -1);
    }

    float GeometricProgressionSum(float b0, float q, int n1, int n2)
    {
        return b0 / (1 - q) * (Mathf.Pow(q, n2) - Mathf.Pow(q, n1));
    }

    int GridSize()
    {
        return 4 * vertexDensity + 1;
    }

    Vector3 Snap(Vector3 coords, float scale)
    {
        if (coords.x >= 0)
            coords.x = Mathf.Floor(coords.x / scale) * scale;
        else
            coords.x = Mathf.Ceil((coords.x - scale + 1) / scale) * scale;

        if (coords.z < 0)
            coords.z = Mathf.Floor(coords.z / scale) * scale;
        else
            coords.z = Mathf.Ceil((coords.z - scale + 1) / scale) * scale;

        coords.y = 0;
        return coords;
    }

    void InstantiateMeshes()
    {
        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
        {
            if (child != transform)
                Destroy(child.gameObject);
        }
        rings.Clear();
        trims.Clear();

        int n = GridSize();
        center = Utils.MeshUtil.InstantiateMeshTransform("Center", materials[materials.Length - 1], transform, Utils.MeshUtil.CreatePlane(2 * n, 2 * n, 1, Utils.MeshUtil.Seams.All));
        
        Mesh ring = Utils.MeshUtil.CreateRing(n, 1);
        Mesh trim = Utils.MeshUtil.CreateTrim(n, 1);
        for (int i = 0; i < clipLevels; i++)
        {
            rings.Add(Utils.MeshUtil.InstantiateMeshTransform("Ring " + i, materials[materials.Length - 1], transform, ring));
            trims.Add(Utils.MeshUtil.InstantiateMeshTransform("Trim " + i, materials[materials.Length - 1], transform, trim));
        }
        skirt = Utils.MeshUtil.InstantiateMeshTransform("Skirt", materials[materials.Length - 1], transform, Utils.MeshUtil.BuildSkirt(n, skirtSize));
        
    }
    
}
}