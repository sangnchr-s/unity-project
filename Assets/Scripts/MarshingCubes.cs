using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubes
{
    static readonly Vector3[] corner = {
        new Vector3(0,0,0), new Vector3(1,0,0),
        new Vector3(1,0,1), new Vector3(0,0,1),
        new Vector3(0,1,0), new Vector3(1,1,0),
        new Vector3(1,1,1), new Vector3(0,1,1)
    };

    static readonly int[,] edges = {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    public static void Generate(float[,,] d, float scale,
        List<Vector3> verts, List<int> tris)
    {
        int s = d.GetLength(0) - 1;

        for (int x = 0; x < s; x++)
            for (int y = 0; y < s; y++)
                for (int z = 0; z < s; z++)
                {
                    MarchCube(new Vector3(x, y, z), d, scale, verts, tris);
                }
    }
    public static void GenerateChunk(float[,,] d, float scale,
        List<Vector3> verts, List<int> tris, int zSize)
    {
        int sx = d.GetLength(0) - 1;
        int sy = d.GetLength(1) - 1;

        for (int x = 0; x < sx; x++)
            for (int y = 0; y < sy; y++)
                for (int z = 0; z < zSize; z++)  // только нужный Z-срез
                    MarchCube(new Vector3(x, y, z), d, scale, verts, tris);
    }
    static void MarchCube(Vector3 pos, float[,,] d, float scale,
        List<Vector3> verts, List<int> tris)
    {
        float[] cube = new float[8];

        for (int i = 0; i < 8; i++)
        {
            Vector3 c = pos + corner[i];
            cube[i] = d[(int)c.x, (int)c.y, (int)c.z];
        }

        int config = 0;
        for (int i = 0; i < 8; i++)
            if (cube[i] > 0) config |= 1 << i;

        int edgeFlags = ResourcesTables.edgeTable[config];
        if (edgeFlags == 0) return;

        Vector3[] edgeVerts = new Vector3[12];

        for (int i = 0; i < 12; i++)
        {
            if ((edgeFlags & (1 << i)) != 0)
            {
                int a = edges[i, 0];
                int b = edges[i, 1];

                Vector3 p1 = (pos + corner[a]) * scale;
                Vector3 p2 = (pos + corner[b]) * scale;

                float d1 = cube[a];
                float d2 = cube[b];

                float t = d1 / (d1 - d2);
                edgeVerts[i] = p1 + t * (p2 - p1);
            }
        }

        for (int i = 0; ResourcesTables.triTable[config, i] != -1; i += 3)
        {
            int idx = verts.Count;

            verts.Add(edgeVerts[ResourcesTables.triTable[config, i]]);
            verts.Add(edgeVerts[ResourcesTables.triTable[config, i + 1]]);
            verts.Add(edgeVerts[ResourcesTables.triTable[config, i + 2]]);

            tris.Add(idx);
            tris.Add(idx + 1);
            tris.Add(idx + 2);
        }
    }
}