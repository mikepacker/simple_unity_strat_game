using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    /// <summary>move grid slightly up so its always visible, and not colliding with the terrain it generates over.</summary>
    public const float GRIDYTRANSLATE = 0.03f;

    private float fpsTimePassed = 0.0f;
    private uint fpsFrameSum = 0;
    public Text consoleText;
    string totalNumberOfVert = "";

    /// <summary>All meshes required to draw the HexGrid</summary>
    public List<GameObject> GridMeshes { get; private set; }

    /// <summary>map logical x,z coordinates to an actual point.</summary>
    public Dictionary<int, Dictionary<int, HexCell>> Coord2GridPiece { get; private set; }

    private List<float> uniqueWordSpaceXValues;
    private List<float> uniqueWordSpaceZValues;
    private Dictionary<float, int> xCoord2WorldSpaceX = new Dictionary<float, int>();
    private Dictionary<float, int> zCoord2WorldSpaceZ = new Dictionary<float, int>();

    public Vector2 WorldToCoordRatio { get; private set; }

    /// <summary>Get the current cell based on logical coordinates.</summary>
    public HexCell GetCellFromCoord(int x, int z)
    {
        if (Coord2GridPiece.ContainsKey(x) && Coord2GridPiece[x].ContainsKey(z))
            return Coord2GridPiece[x][z];

        return null;
    }

    void Start()
    {
        DateTime startGrid = DateTime.Now;

        Coord2GridPiece = new Dictionary<int, Dictionary<int, HexCell>>();
        GridMeshes = new List<GameObject>();
        GenerateHexGrid(new Vector2(1, 1));

        uniqueWordSpaceXValues = new List<float>(xCoord2WorldSpaceX.Keys);
        uniqueWordSpaceXValues.Sort();
        uniqueWordSpaceZValues = new List<float>(zCoord2WorldSpaceZ.Keys);
        uniqueWordSpaceZValues.Sort();

        WorldToCoordRatio = ComputeWordToLogicalRatios();

        using (StreamWriter sw = new StreamWriter("timing.log", true))
        {
            sw.WriteLine("Grid generated on: " + DateTime.Now.ToLongDateString());
            string timeToGen = "Time to generate: " + (DateTime.Now - startGrid).TotalSeconds;
            sw.WriteLine(timeToGen);

            int totalVerticies = 0;
            uint totalIndexes = 0;
            foreach (GameObject obj in GridMeshes)
            {
                Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
                totalIndexes += mesh.GetIndexCount(0);
                totalVerticies += mesh.vertexCount;
            }

            int hexCellCount = 0;
            foreach (Dictionary<int, HexCell> dic in Coord2GridPiece.Values)
                hexCellCount += dic.Values.Count;


            totalNumberOfVert = string.Format("{2}\nTotal verticies: {0}\nTotal Indexes: {1}\nHex Cell Count: {3}", totalVerticies, totalIndexes, timeToGen, hexCellCount);
            sw.WriteLine(totalNumberOfVert);
        }
    }

    private void Update()
    {
        if (consoleText != null)
        {
            if (fpsTimePassed > 3)
            {
                consoleText.text = totalNumberOfVert + "\nFPS:" + Math.Round(fpsFrameSum / fpsTimePassed, 2).ToString();
                fpsFrameSum = 0;
                fpsTimePassed = 0;
            }
            fpsTimePassed += Time.deltaTime;
            fpsFrameSum++;
        }


        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit floorHit;
        if (Terrain.activeTerrain.GetComponent<Collider>().Raycast(camRay, out floorHit, 1000f))
        {
            ConvertWorldToLogical(floorHit.point);

        }
    }

    #region HEXGRID_GENERATION_FUNCTIONS

    /// <summary>Given the required verticies/indicies create the game object</summary>
    private GameObject CreateGridObject(Vector3[] verticies, int[] indicies)
    {
        GameObject gridMesh = new GameObject();

        gridMesh.name = "GridMesh";
        gridMesh.transform.position = new Vector3(0, GRIDYTRANSLATE, 0);


        MeshFilter meshFilter = (MeshFilter)gridMesh.AddComponent(typeof(MeshFilter));
        gridMesh.AddComponent(typeof(MeshRenderer)); ;
        MeshRenderer meshRend = gridMesh.GetComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.SetIndices(indicies, MeshTopology.Lines, 0);


        meshFilter.mesh = mesh;

        meshRend = gridMesh.GetComponent<MeshRenderer>();
        meshRend.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshRend.receiveShadows = false;
        meshRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshRend.material = new Material(Shader.Find(" Diffuse"));
        meshRend.material.color = Color.black;

        return gridMesh;
    }

    public void AddSubMesh(Vector3[] verticies, int[] indicies)
    {
        //Mesh mesh = game.GetComponent<MeshFilter>().mesh;
        //mesh.subMeshCount++;
        //mesh.Set
    }

    private bool IsPositionInBounds(Vector3 v)
    {
        //could use terrain height map and renderer bounds put goal is to have "layer" over terrain so grid isnt generated everywhere.
        Vector3 vUp = v;
        vUp.y += 10f;

        ////generate a ray cast downward and see if it hits the "Terrain".
        Ray downRay = new Ray(vUp, Vector3.down);
        RaycastHit floorHit;

        bool hit = Terrain.activeTerrain.GetComponent<Collider>().Raycast(downRay, out floorHit, 100f);
        //bool hit = Physics.Raycast(downRay, out floorHit, 20f, LayerMask.GetMask("Terrain"));
        return hit;

        //const float BOUND = 88f;
        //return v.x < BOUND && v.x > -BOUND && v.z < BOUND && v.z > -BOUND;
    }

    /// <summary> Generate the entire hex grid in this function. Input the start position in x,z coordinates. </summary>
    public void GenerateHexGrid(Vector2 startPos)
    {
        int gameMeshIndex = 0;
        List<Vector3> verticies = new List<Vector3>(2000000); //each mesh can only contain 65000 verticies
        List<int> indicies = new List<int>(verticies.Count * 2);  //approximate guess.

        Queue<HexCell> hexCellPiecesToAdd = new Queue<HexCell>();

        //starting coordinates of the grid.
        HexCell startingCell = new HexCell(this, new Vector3(startPos.x, 0, startPos.y), 0, 0);
        hexCellPiecesToAdd.Enqueue(startingCell);
        RegisterCellToGrid(startingCell);

        while (hexCellPiecesToAdd.Count > 0)
        {
            HexCell currentCell = hexCellPiecesToAdd.Dequeue();

            currentCell.GenearteHexCell(verticies, indicies, gameMeshIndex);
            GenAllNeighbourPointsNotGeneratedYet(currentCell, hexCellPiecesToAdd);

            //a gridmesh in unity cannot contain more than 65k verticies, so we need to spread the hex grid over several
            if (verticies.Count > 64950)
            {
                indicies.TrimExcess();
                verticies.TrimExcess();
                GridMeshes.Add(CreateGridObject(verticies.ToArray(), indicies.ToArray()));
                verticies = new List<Vector3>(65000);
                indicies = new List<int>(65000 * 4);
                gameMeshIndex++;
            }

        }
        GridMeshes.Add(CreateGridObject(verticies.ToArray(), indicies.ToArray()));
    }

    /// <summary>Find the amount in world X to go 1 coordinate X in hexcell, same with Z</summary>
    /// <param name="worldToLogicalX"></param>
    /// <param name="worldToLogicalZ"></param>
    private Vector2 ComputeWordToLogicalRatios()
    {
        //Computer the center of an adjacent cell at point +1,+1 (Vector 1 and 2)
        HexCell cell = new HexCell(null, new Vector3(0, 0, 0), 0, 0);
        cell.GenearteHexCell(new List<Vector3>(), new List<int>(), 0);
        Vector3 adjacentCellCenter = Vector3.Lerp(cell.Vertex[1], cell.Vertex[2], 0.5f) * 2;

        return new Vector2(adjacentCellCenter.x, adjacentCellCenter.z);
    }

    private HexCell GetHexCellHit(Vector3 hitPoint)
    {
        //Convert the world coorindates into potentail logical coordinates.
        int xCoord = (int)(hitPoint.x / WorldToCoordRatio.x); //always round down for x only.
        int yCoord = Convert.ToInt32(hitPoint.y / WorldToCoordRatio.y); //round to nearest int

        //2 potential hit points.{xCoord,yCoord} .. {xCoord+1,yCoord}
        HexCell p1 = GetCellFromCoord(xCoord, yCoord);
        HexCell p2 = GetCellFromCoord(xCoord + 1, yCoord);

        //whichever is closer to the hitpoint is the proper intersect hexcell.
        Vector3 dis1 = p1.Center - hitPoint;
        Vector3 dis2 = p2.Center - hitPoint;
        return dis1.magnitude < dis2.magnitude ? p1 : p2;
    }

    private void GenAllNeighbourPointsNotGeneratedYet(HexCell refPiece, Queue<HexCell> cellsToGenerate)
    {
        //  0__1    where each vertex represents
        // 5/  \2
        // 4\__/3
        Vector3 midPoint = Vector3.Lerp(refPiece.Vertex[0], refPiece.Vertex[1], 0.5f);

        for (int i = 0; i <= 5; i++)
        {
            Vector3 point = Quaternion.AngleAxis(i * 60, Vector3.up) * (midPoint - refPiece.Center);
            point *= 2;
            point += refPiece.Center;
            if (IsPositionInBounds(point))
            {
                int newCoordX = refPiece.CoordX + HexCell.CoordOffSets[i, 0];
                int newCoordZ = refPiece.CoordZ + HexCell.CoordOffSets[i, 1];
                if (Coord2GridPiece.ContainsKey(newCoordX) && Coord2GridPiece[newCoordX].ContainsKey(newCoordZ))
                    continue;

                //Generate the new cell and enque it to be populated.
                HexCell newCell = new HexCell(this, point, newCoordX, newCoordZ);
                cellsToGenerate.Enqueue(newCell);

                RegisterCellToGrid(newCell);
            }
        }
    }

    public void RegisterCellToGrid(HexCell cell)
    {
        //Add the hashtable value for the new Cell so that logical coordinate is not generated again.
        if (!Coord2GridPiece.ContainsKey(cell.CoordX))
            Coord2GridPiece.Add(cell.CoordX, new Dictionary<int, HexCell>());
        Coord2GridPiece[cell.CoordX].Add(cell.CoordZ, cell);
    }

    #endregion

}