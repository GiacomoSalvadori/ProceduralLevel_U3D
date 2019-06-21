using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGenerator : MonoBehaviour
{
    #region Properties
    //PUBLIC
    [Header("Obj to build level")]
    public GameObject tile;
    public GameObject tileCurve;
    public GameObject startBlock;
    public GameObject endBlock;
    [Header("Level block props")]
    public BlockDimension blockSize;
    public int blockNum = 1;
    public float levelSize = 10.0f;
    [Header("Camera")]
    public MultipleTargetCamera cam;
    //PRIVATE
    private float size = 1.0f;
    private int level = 6;
    private int dimension = 0;
    private float lastX = 0.0f;
    private float lastY = 0.0f;
    private float maxY;
    private float minY;
    private float maxX;
    private float minX;
    private int pointsNumber;
    Vector3 offset = Vector3.zero;
    private List<Point> points;
    private List<Vector3> gridPositions;
    private List<Vector3> objPos;
    private PathPoints pathPoints;
    #endregion

    void Awake()
    {
        dimension = (int)Mathf.Pow(2, level);
        points = new List<Point>();
        objPos = new List<Vector3>();
        gridPositions = new List<Vector3>();
        pathPoints = new PathPoints(Vector3.zero, Vector3.zero);
    }

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    public PathPoints Generate()
    {
        Debug.Log("START TIME: " + System.DateTime.Now.ToString("G"));
        ConstructLevel();
        objPos.Clear();
        objPos.Add(new Vector3(maxX + levelSize, maxY + levelSize, 0.0f));
        objPos.Add(new Vector3(maxX + levelSize, minY - levelSize, 0.0f));
        objPos.Add(new Vector3(minX - levelSize, minY - levelSize, 0.0f));
        objPos.Add(new Vector3(minX - levelSize, maxY + levelSize, 0.0f));
        cam.Recalculate(objPos);
        Debug.Log("END TIME: " + System.DateTime.Now.ToString("G"));

        return pathPoints;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            CleanLevel();
            Generate();
        }
    }

    private void CreateHilbertCurve()
    {
        points.Clear();
        lastX = 0.0f;
        lastY = 0.0f;
        int levelDelta = Random.Range(0, 3);
        Debug.Log("Generating a Hilbert curve of level "+(level + levelDelta));
        HilbertDrawer(level +levelDelta, size, 0);
    }

    private void HilbertDrawer(int depth, float dx, float dy)
    {
        if (depth > 1)
            HilbertDrawer(depth - 1, dy, dx);

        SavePosition(dx, dy);
        if (depth > 1)
            HilbertDrawer(depth - 1, dx, dy);

        SavePosition(dy, dx);
        if (depth > 1)
            HilbertDrawer(depth - 1, dx, dy);

        SavePosition(-dx, -dy);
        if (depth > 1)
            HilbertDrawer(depth - 1, -dy, -dx);
    }

    private void SavePosition(float dx, float dy)
    {
        Vector3 from = new Vector3(lastX, lastY, 0.0f);
        Vector3 to = new Vector3(dx + lastX, dy + lastY, 0.0f);
        int x = pointsNumber / dimension;
        int y = pointsNumber % dimension;
        pointsNumber++;
        float xGrid = from.x / size;
        float yGrid = from.y / size;
        points.Add(new Point(from, to, x, y, xGrid, yGrid));

        lastX = lastX + dx;
        lastY = lastY + dy; ;
    }

    private void GenerateGrid()
    {
        int numRows = blockSize.rows + Random.Range(0, blockSize.randomDelta+1);
        int numCols = blockSize.cols + Random.Range(0, blockSize.randomDelta+1);
        Debug.Log("Generating grid of "+numRows+"X"+numCols);

        if (numRows > 0 && numCols > 0)
        {
            int maxX = dimension - numRows;
            int maxY = dimension - numCols;

            int xRandom = Random.Range(0, maxX+1);
            int yRandom = Random.Range(0, maxY+1);

            gridPositions.Clear();
            Vector3 worldBottomLeft = new Vector3(xRandom, yRandom, 0.0f);
            
            for (int x = 0; x < numRows; x++)
            {
                for (int y = 0; y < numCols; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * size) + Vector3.up * (y * size);
                    gridPositions.Add(worldPoint);
                }
            }
        }
    }

    private List<Point> CalculatePath()
    {
        List<Point> solution = new List<Point>();

        points.OrderBy(p => p.x).ThenBy(p => p.y);
        foreach (Point p in points)
        {
            if (gridPositions.Contains(p.position))
            {
                if (solution.Count > 0)
                {// Check if the current point and last point added to solution are neighbours
                    if (NeighbourPoints(p.position, solution[solution.Count - 1].position))
                    {
                        solution.Add(p);
                    }
                }
                else // Is the first point of the grid reached
                {
                    solution.Add(p);
                }
            }
        }

        return solution;
    }

    private bool NeighbourPoints(Vector3 position, Vector3 lastPos)
    {
        Vector3 posUp = position + (Vector3.up * size);
        Vector3 posDown = position + (Vector3.down * size);
        Vector3 posLeft = position + (Vector3.left * size);
        Vector3 posRight = position + (Vector3.right * size);

        List<Vector3> pNeighbours = new List<Vector3>();
        pNeighbours.Add(posUp);
        pNeighbours.Add(posDown);
        pNeighbours.Add(posLeft);
        pNeighbours.Add(posRight);

        List<Point> neighbours = points.FindAll(x => pNeighbours.Contains(x.position));
        foreach (Point p in neighbours)
        {
            if (p.position == lastPos)
                return true;
        }

        return false;
    }

    private List<Point> GenerateBlock(bool regenerateHilbert = true)
    {
        if(regenerateHilbert)
            CreateHilbertCurve();

        GenerateGrid();

        return CalculatePath();
    }

    private void ConstructLevel()
    {
        List<Point> blockPieces = new List<Point>();
        int c = 1;

        for (int i = 0; i < blockNum; i++)
        {
            blockPieces = GenerateBlock();

            c = 1;
            Vector3 pivot = Vector3.zero;
            if(blockPieces.Count > 0)
            {
                GameObject blockContainer = new GameObject();
                blockContainer.name = "BlockContainer_"+i.ToString();
                blockContainer.tag = "BlockContainer";
                foreach (Point p in blockPieces)
                {
                    if (c == 1)
                    {
                        if (i == 0)
                            pathPoints.startPoint = offset * levelSize;

                        pivot = p.position;
                        GameObject startObj = Instantiate(startBlock, offset * levelSize, Quaternion.identity);
                        startObj.transform.localScale = new Vector3(levelSize, levelSize, 0.0f);
                        blockContainer.transform.position = startObj.transform.position;
                        startObj.transform.parent = blockContainer.transform;
                        minY = startObj.transform.position.y;
                        maxY = startObj.transform.position.y;
                        minX = startObj.transform.position.x;
                        maxX = startObj.transform.position.x;

                        CheckBorders(startObj.transform.position);
                    } else
                    {
                        GameObject t;
                        Vector3 newPos = (p.position - pivot + offset) * levelSize;
                        CheckBorders(newPos);

                        if (c < blockPieces.Count)
                        {
                            float xPrev = p.position.x - blockPieces[c - 2].position.x;
                            float yPrev = p.position.y - blockPieces[c - 2].position.y;
                            float xNext = p.position.x - blockPieces[c].position.x;
                            float yNext = p.position.y - blockPieces[c].position.y;
                            float zRot = 0.0f;

                            if ((xPrev != 0.0f && yNext != 0.0f) || (xNext != 0.0f && yPrev != 0.0f))
                            {
                                zRot = GroundPieceRot(xPrev, yPrev, xNext, yNext);
                                t = Instantiate(tileCurve, newPos, Quaternion.Euler(new Vector3(0.0f, 0.0f, zRot)), blockContainer.transform);
                            } else
                            {
                                if (yPrev != 0.0f)
                                    zRot = 90.0f;

                                t = Instantiate(tile, newPos, Quaternion.Euler(new Vector3(0.0f, 0.0f, zRot)), blockContainer.transform);
                            }

                            
                            t.transform.localScale = new Vector3(levelSize, levelSize, 1.0f);
                        }

                        if (c == blockPieces.Count)
                        {
                            offset += p.position - pivot;
                            pathPoints.endPoint = newPos;
                            GameObject endObj = Instantiate(endBlock, newPos, endBlock.transform.rotation, blockContainer.transform);
                            endObj.transform.localScale = new Vector3(levelSize, levelSize, 0.0f);
                        }
                    }
                    c++;
                }
            }
            blockPieces.Clear();
        }
    }

    private float GroundPieceRot(float xPrev, float yPrev, float xNext, float yNext)
    {
        float zRot = 0.0f;
        
        if (yNext < 0.0f)
        {
            if (xPrev > 0.0f)
                zRot = 0.0f;
            else if (xPrev == 0.0f)
                zRot = 180.0f;
            else
                zRot = 270.0f;
        }
        else if (yNext > 0.0f)
        {
            if (xPrev < 0.0f)
                zRot = 180.0f;
            else if (xPrev == 0.0f)
                zRot = 270.0f;
            else
                zRot = 90.0f;
        }
        else if (xNext > 0.0f)
        {
            if (yPrev > 0)
                zRot = 90.0f;
        }
        else if (xNext < 0.0f)
        {
            if (yPrev > 0)
                zRot = 180.0f;
            else if (yPrev == 0.0f)
                zRot = 180.0f;
            else if (yPrev < 0.0f)
                zRot = 270.0f;
        }

        return zRot;
    }

    private void CheckBorders(Vector3 pos)
    {
        if (pos.x > maxX)
            maxX = pos.x;
        else if (pos.x < minX)
            minX = pos.x;

        if (pos.y > maxY)
            maxY = pos.y;
        else if (pos.y < minY)
            minY = pos.y;
    }

    private void CleanLevel()
    {
        GameObject[] block = GameObject.FindGameObjectsWithTag("BlockContainer");

        if (block.Length > 0)
        {
            foreach (GameObject b in block)
            {
                Destroy(b);
            }
        }
    }
}

[System.Serializable]
public struct BlockDimension
{
    public int rows;
    public int cols;
    public int randomDelta;
}

public struct PathPoints
{
    public Vector3 startPoint;
    public Vector3 endPoint;

    public PathPoints(Vector3 startPoint_, Vector3 endPoint_)
    {
        startPoint = startPoint_;
        endPoint = endPoint_;
    }
}