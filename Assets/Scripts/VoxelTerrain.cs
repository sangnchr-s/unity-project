using UnityEngine;
using System.Collections.Generic;

public class VoxelTerrain : MonoBehaviour
{
    [Header("World")]
    public int worldSize = 40;
    public int chunkSize = 10;
    public float voxelSize = 0.5f;

    [Header("Cylinder")]
    public float cylinderRadius = 6f;
    public float cylinderLength = 20f;

    [Header("Rendering")]
    public Material material;

    [Header("Color Gradient (GLOBAL Z)")]
    public Gradient heightGradient;

    [Header("Update")]
    public float updateInterval = 0.1f;

    [Header("Generation")]
    [SerializeField] bool createOnlyCylinderChunks = true;
    [SerializeField] int cylinderChunkPadding = 1;

    private Chunk[,,] chunks;
    private float timer;

    void Start()
    {
        InitChunks();

        foreach (var c in chunks)
        {
            if (c != null)
                c.Generate();
        }

        foreach (var c in chunks)
        {
            if (c != null)
                c.UpdateMesh();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            UpdateDirtyChunks();
            timer = 0f;
        }
    }

    void InitChunks()
    {
        int safeChunkSize = Mathf.Max(1, chunkSize);
        chunkSize = safeChunkSize;
        int count = Mathf.Max(1, Mathf.CeilToInt(worldSize / (float)safeChunkSize));
        chunks = new Chunk[count, count, count];
        int createdChunks = 0;

        for (int x = 0; x < count; x++)
        for (int y = 0; y < count; y++)
        for (int z = 0; z < count; z++)
        {
            if (createOnlyCylinderChunks && !ChunkIntersectsCylinderBounds(x, y, z, safeChunkSize))
                continue;

            GameObject go = new GameObject($"Chunk_{x}_{y}_{z}");
            go.transform.parent = transform;

            Chunk c = go.AddComponent<Chunk>();
            c.Init(
                safeChunkSize,
                voxelSize,
                new Vector3Int(x * safeChunkSize, y * safeChunkSize, z * safeChunkSize),
                worldSize,
                cylinderRadius,
                cylinderLength,
                material,
                this
            );

            chunks[x, y, z] = c;
            createdChunks++;
        }

        Debug.Log($"[VoxelTerrain] Created {createdChunks} chunks out of {count * count * count} possible.");
    }

    bool ChunkIntersectsCylinderBounds(int x, int y, int z, int safeChunkSize)
    {
        float padding = Mathf.Max(0, cylinderChunkPadding) * safeChunkSize;
        float center = worldSize / 2f;
        float halfLength = cylinderLength / 2f;

        Vector3 min = new Vector3(x, y, z) * safeChunkSize;
        Vector3 max = min + Vector3.one * safeChunkSize;

        if (max.z < center - halfLength - padding || min.z > center + halfLength + padding)
            return false;

        float closestX = Mathf.Clamp(center, min.x, max.x);
        float closestY = Mathf.Clamp(center, min.y, max.y);
        float dx = closestX - center;
        float dy = closestY - center;
        float radiusWithPadding = cylinderRadius + padding;

        return dx * dx + dy * dy <= radiusWithPadding * radiusWithPadding;
    }

    void UpdateDirtyChunks()
    {
        foreach (var c in chunks)
        {
            if (c != null && c.dirty)
            {
                c.UpdateMesh();
                c.dirty = false;
            }
        }
    }

    public float GetDensityGlobal(int gx, int gy, int gz)
    {
        int chunkX = gx / chunkSize;
        int chunkY = gy / chunkSize;
        int chunkZ = gz / chunkSize;

        if (chunkX < 0 || chunkY < 0 || chunkZ < 0 ||
            chunkX >= chunks.GetLength(0) ||
            chunkY >= chunks.GetLength(1) ||
            chunkZ >= chunks.GetLength(2))
            return -1f;

        Chunk c = chunks[chunkX, chunkY, chunkZ];
        if (c == null || c.density == null) return -1f;

        int lx = gx - c.offset.x;
        int ly = gy - c.offset.y;
        int lz = gz - c.offset.z;

        lx = Mathf.Clamp(lx, 0, c.size);
        ly = Mathf.Clamp(ly, 0, c.size);
        lz = Mathf.Clamp(lz, 0, c.size);

        return c.density[lx, ly, lz];
    }

    public void Dig(Vector3 worldPos, float radius, float power)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        float r2 = radius * radius;

        foreach (var c in chunks)
        {
            if (c != null && c.BoundsContains(local, radius))
                c.Dig(local, r2, power);
        }
    }
}

public class Chunk : MonoBehaviour
{
    public int size;
    float voxelSize;
    public Vector3Int offset;

    int worldSize;
    float cylinderRadius;
    float cylinderLength;

    VoxelTerrain world;

    public float[,,] density;
    public bool dirty;

    Mesh mesh;
    MeshFilter mf;
    MeshCollider mc;
    MeshRenderer mr;

    List<Vector3> verts = new List<Vector3>(10000);
    List<int> tris = new List<int>(10000);
    List<Color> colors = new List<Color>(10000);
    List<Vector2> uvs = new List<Vector2>(10000);

    public void Init(int size, float voxelSize, Vector3Int offset,
                     int worldSize, float cylinderRadius, float cylinderLength,
                     Material material, VoxelTerrain world)
    {
        this.size = size;
        this.voxelSize = voxelSize;
        this.offset = offset;
        this.worldSize = worldSize;
        this.cylinderRadius = cylinderRadius;
        this.cylinderLength = cylinderLength;
        this.world = world;

        density = new float[size + 1, size + 1, size + 1];

        mf = gameObject.AddComponent<MeshFilter>();
        mc = gameObject.AddComponent<MeshCollider>();
        mr = gameObject.AddComponent<MeshRenderer>();

        mr.material = material;

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mf.mesh = mesh;

        transform.localPosition = (Vector3)offset * voxelSize;
    }

    public void Generate()
    {
        float cx = worldSize / 2f;
        float cy = worldSize / 2f;
        float cz = worldSize / 2f;

        float halfLen = cylinderLength / 2f;

        for (int x = 0; x <= size; x++)
        for (int y = 0; y <= size; y++)
        for (int z = 0; z <= size; z++)
        {
            Vector3 worldPos = (Vector3)(offset + new Vector3Int(x, y, z));

            float dx = worldPos.x - cx;
            float dy = worldPos.y - cy;
            float dz = worldPos.z - cz;

            float distFromAxis = Mathf.Sqrt(dx * dx + dy * dy);

            float outsideRadius = distFromAxis - cylinderRadius;
            float outsideLen = Mathf.Abs(dz) - halfLen;

            density[x, y, z] = -Mathf.Max(outsideRadius, outsideLen);
        }
    }

    public void UpdateMesh()
    {
        verts.Clear();
        tris.Clear();
        colors.Clear();
        uvs.Clear();

        int fieldSize = size + 1;
        float[,,] field = new float[fieldSize + 1, fieldSize + 1, fieldSize + 1];

        for (int x = 0; x <= fieldSize; x++)
        for (int y = 0; y <= fieldSize; y++)
        for (int z = 0; z <= fieldSize; z++)
        {
            int gx = offset.x + x;
            int gy = offset.y + y;
            int gz = offset.z + z;

            field[x, y, z] = world.GetDensityGlobal(gx, gy, gz);
        }

        MarchingCubes.Generate(field, voxelSize, verts, tris);

        // 🔥 ГЛОБАЛЬНЫЕ ГРАНИЦЫ ПО Z (НЕ локальные!)
        float minZ = 0f;
        float maxZ = worldSize * voxelSize;

        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = verts[i];

            // 🔥 КЛЮЧ: используем ГЛОБАЛЬНЫЕ координаты вручную
            float globalZ = (offset.z * voxelSize) + v.z;

            float t = Mathf.InverseLerp(minZ, maxZ, globalZ);

            Color col = world.heightGradient.Evaluate(t);
            colors.Add(col);

            // UV
            Vector3 n = v.normalized;
            Vector2 uv;

            if (Mathf.Abs(n.y) > Mathf.Abs(n.x) && Mathf.Abs(n.y) > Mathf.Abs(n.z))
                uv = new Vector2(v.x, v.z);
            else if (Mathf.Abs(n.x) > Mathf.Abs(n.z))
                uv = new Vector2(v.y, v.z);
            else
                uv = new Vector2(v.x, v.y);

            uv *= 0.2f;
            uvs.Add(uv);
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();

        if (Time.frameCount % 10 == 0)
            mc.sharedMesh = mesh;
    }

    public bool BoundsContains(Vector3 point, float radius)
    {
        Vector3 min = (Vector3)offset * voxelSize;
        Vector3 max = min + Vector3.one * size * voxelSize;

        return (point.x + radius >= min.x && point.x - radius <= max.x &&
                point.y + radius >= min.y && point.y - radius <= max.y &&
                point.z + radius >= min.z && point.z - radius <= max.z);
    }

    public void Dig(Vector3 local, float r2, float power)
    {
        int r = Mathf.CeilToInt(Mathf.Sqrt(r2) / voxelSize);

        int cx = Mathf.FloorToInt(local.x / voxelSize) - offset.x;
        int cy = Mathf.FloorToInt(local.y / voxelSize) - offset.y;
        int cz = Mathf.FloorToInt(local.z / voxelSize) - offset.z;

        for (int x = cx - r; x <= cx + r; x++)
        for (int y = cy - r; y <= cy + r; y++)
        for (int z = cz - r; z <= cz + r; z++)
        {
            if (x < 0 || y < 0 || z < 0 || x > size || y > size || z > size)
                continue;

            Vector3 worldPos = (Vector3)(offset + new Vector3Int(x, y, z)) * voxelSize;
            float sqrDist = (worldPos - local).sqrMagnitude;

            if (sqrDist < r2)
            {
                density[x, y, z] = -1f;
                dirty = true;
            }
        }
    }
}