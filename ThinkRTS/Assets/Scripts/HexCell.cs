using System.Collections.Generic;
using UnityEngine;

public class HexCell
{
    #region constructors
    public HexCell(HexGrid grid, Vector3 center, int coordX, int coordZ)
    {
        Center = center;
        this.grid = grid;
        CoordX = coordX;
        CoordZ = coordZ;
    }

    public HexCell(HexGrid grid, Vector3 center,  int coordX, int coordZ, int meshIndex) : this(grid, center, coordX, coordZ)
    {
        this.MeshIndex = meshIndex;
    }
    #endregion

    #region var_properties
    /// <summary> How big the Hex's radius is</summary>
    public static readonly Vector3 HexRadius = new Vector3(0, 0, 0.25f);

    public int MeshIndex; //which mesh index this HexGridPiece belongs too.
    /// <summary>logical coordinate system of the grid</summary>
    public int CoordX { get; private set; }
    /// <summary>logical coordinate system of the grid</summary>
    public int CoordZ { get; private set; } 
    /// <summary>The HexGrid that the HexCell belongs to.</summary>
    private HexGrid grid;

    /// <summary>Center/midpoint of the HexCell.</summary>
    public Vector3 Center { get; private set; }

    /// <summary> The offsets of each Vertex from the coordx/z to the neighbour. 
    /// 0 == top .. 1 == top right > 4 == bot left, 5 == top left</summary>
    public static readonly int[,] CoordOffSets = new int[,] { { 0, 2 },{ 1, 1 },{ 1, -1 },{ 0, -2 },{ -1, -1 },{ -1, 1 } };

    /// <summary>   0__1    where each vertex represents
    ///            5/  \2
    ///            4\__/3    </summary>
    public Vector3[] Vertex { get; private set; }

    public HexCell Top { get { return grid.GetCellFromCoord(CoordX, CoordZ + 2); } }
    public HexCell TopLeft { get { return grid.GetCellFromCoord(CoordX - 1, CoordZ + 1); } }
    public HexCell TopRight { get { return grid.GetCellFromCoord(CoordX + 1, CoordZ + 1); } }
    public HexCell Bottom { get { return grid.GetCellFromCoord(CoordX, CoordZ - 2); } }
    public HexCell BottomLeft { get { return grid.GetCellFromCoord(CoordX - 1, CoordZ - 1); } }
    public HexCell BottomRight { get { return grid.GetCellFromCoord(CoordX + 1, CoordZ - 1); } }

    #endregion

    public void GenearteHexCell(HexGrid grid, List<Vector3> verticies, List<int> indicies, int gridMeshIndex)
    {
        //  0__1    where each vertex represents
        // 5/  \2
        // 4\__/3

        //create the verticies
        Vertex = new Vector3[6];
        for(int i=0; i <= 5; i++)
        {
            Vertex[i] = Center + Quaternion.AngleAxis(i * 60 - 30f, Vector3.up) * HexRadius;
            Vertex[i].y = Terrain.activeTerrain.SampleHeight(Vertex[i]);
            verticies.Add(Vertex[i]);
        }

        //add indicies we only want to draw lines if there is not an adjacent HexCell, otherwise it has already been drawn.
        if (Top == null || Top.Vertex == null)
            indicies.AddRange(new int[] { verticies.Count - 6, verticies.Count - 5 });
        if(TopRight == null || TopRight.Vertex == null)
            indicies.AddRange(new int[] { verticies.Count - 5, verticies.Count - 4 });
        if (BottomRight == null || BottomRight.Vertex == null)
            indicies.AddRange(new int[] { verticies.Count - 4, verticies.Count - 3 });
        if (Bottom == null || Bottom.Vertex == null)
            indicies.AddRange(new int[] { verticies.Count - 3, verticies.Count - 2 });
        if (BottomLeft == null || BottomLeft.Vertex == null)
            indicies.AddRange(new int[] { verticies.Count - 2, verticies.Count - 1 });
        if (TopLeft == null || TopLeft.Vertex == null)
            indicies.AddRange(new int[] { verticies.Count - 1, verticies.Count - 6 });
    }
}


