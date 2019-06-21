using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    #region Properties
    //PUBLIC
    [Header("Grid properties")]
    public int rows = 4;
    public int col = 4;
    [Header("Obj to draw solution")]
    public GameObject arrow;
    public GameObject start;
    public GameObject end;
    //PRIVATE
    private bool isPlaying = false;
    private float size = 1;
    private int maxX;
    private int maxY;
    private int moveX;
    private int moveY;
    private List<Vector3> points;
    private List<Point> solution;
    private HilbertDrawer drawer;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        isPlaying = true;
        points = new List<Vector3>();
        solution = new List<Point>();
        drawer = GameObject.Find("Drawer").GetComponent<HilbertDrawer>();
        transform.position = drawer.RetrieveFirstPosition();
        size = drawer.size;
        maxX = drawer.Size() - rows;
        maxY = drawer.Size() - col;
        moveX = 0;
        moveY = 0;
        CalculateGrid();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    void OnDrawGizmos()
    {
        if (isPlaying)
        {
            if (points.Count > 0)
            {
                foreach (Vector3 p in points)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(p, Vector3.one * size);
                    Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                    Gizmos.DrawCube(p, Vector3.one * size);
                }
            }
        }
    }

    public void CalculateGrid()
    {
        Vector3 worldBottomLeft = transform.position;
        points.Clear();

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < col; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * size) + Vector3.up * (y * size);
                points.Add(worldPoint);
            }
        }

        solution = drawer.CalculatePath(points);
        DrawSolution();
    }

    private void DrawSolution()
    {
        GameObject[] arrows = GameObject.FindGameObjectsWithTag("Arrow");

        if (arrows.Length > 0)
            foreach (GameObject a in arrows)
                Destroy(a);

        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");
        if (blocks.Length > 0)
            foreach (GameObject b in blocks)
                Destroy(b);

        if (solution.Count > 0)
        {
            int c = 0;
            foreach (Point p in solution)
            {
                c++;
                float rotZ = 0.0f;
                if (c < solution.Count)
                {
                    Point nextPoint = solution[c];
                    if (nextPoint.position.x - p.position.x > 0)
                        rotZ = -90.0f;
                    else if (nextPoint.position.x - p.position.x < 0)
                        rotZ = 90.0f;

                    if (nextPoint.position.y - p.position.y < 0)
                        rotZ = 180.0f;
                }
                Vector3 rot = new Vector3(0.0f, 0.0f, rotZ);

                if (c == 1)
                    Instantiate(start, p.position, Quaternion.identity);
                else if (c == solution.Count)
                    Instantiate(end, p.position, Quaternion.identity);
                else
                    Instantiate(arrow, p.position, Quaternion.Euler(rot));
            }
        }
    }

    private void Move()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            moveX--;
            if (moveX < 0)
            {
                moveX = 0;
            }
            else
            {
                transform.position -= Vector3.right * size;
                CalculateGrid();
            }
        } else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            moveX++;
            if(moveX > maxX)
            {
                moveX = maxX;
            } else
            {
                transform.position += Vector3.right * size;
                CalculateGrid();
            }
        } else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            moveY++;
            if (moveY > maxY)
            {
                moveY = maxY;
            }
            else
            {
                transform.position += Vector3.up * size;
                CalculateGrid();
            }
        } else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            moveY--;
            if (moveY < 0)
            {
                moveY = 0;
            }
            else
            {
                transform.position -= Vector3.up * size;
                CalculateGrid();
            }
        }

    }
}
