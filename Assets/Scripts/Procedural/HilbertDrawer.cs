using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HilbertDrawer : MonoBehaviour
{
    #region Properties
    //PUBLIC
    [Header("Auto draw and resize")]
    public bool autoDraw = false;
    [Header("Size of the Hilbert segment")]
    public float size = 10.0f;
    [Header("Level of Hilbert curve")]
    public int level = 2;
    [Header("Camera ref (should be ortographic)")]
    public Camera cam; // Camera should be ortographic
    [Header("Object used to visualize Hilbert curve")]
    public GameObject line;
    public GameObject container;
    //PRIVATE
    private float LastX = 0.0f;
    private float LastY = 0.0f;
    private float LastZ = 0.0f;
    private float totalLength = 0.0f;
    private int pointsNumber;
    private List<Point> points; //Point[,] points;
    #endregion

    private void Awake()
    {
        pointsNumber = 0;
        points = new List<Point>();
        container.transform.position = transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        HilbertCalculator(level, size, 0);

        List<Point> po = new List<Point>();
        points.OrderBy(p => p.x).ThenBy(p => p.y);
        if (points.Count > 0 && line)
        {
            int c = 1;
            foreach (Point p in points)
            {
                Vector3 angle = new Vector3(0.0f, 0.0f, p.rotation);
                GameObject l = Instantiate(line, p.position, Quaternion.Euler(angle), container.transform);
                l.name = "Line_" + c.ToString() + "_" + p.x + "_" + p.y;
                c++;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (autoDraw)
        {
            AutoSizeDraw();
            if (level > 4)
                level = 4;

            Hilbert(level, size, 0);
        }
    }

    public Vector3 RetrieveFirstPosition()
    {
        if (points.Count > 0)
            return points[0].position;
        else
            return Vector3.zero;
    }

    public int Size()
    {
        return (int)Mathf.Pow(2, level);
    }

    public List<Point> CalculatePath(List<Vector3> gridPoint)
    {
        List<Point> solution = new List<Point>();
        foreach (Point p in points)
        {
            if (gridPoint.Contains(p.position)) {
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
        foreach(Point p in neighbours)
        {
            if (p.position == lastPos)
                return true;
        }

        return false;
    }

    // Draw a Hilbert curve.
    private void Hilbert(int depth, float dx, float dy)
    {
        if (depth > 1)
            Hilbert(depth - 1, dy, dx);

        DrawRelative(dx, dy);
        if (depth > 1)
            Hilbert(depth - 1, dx, dy);

        DrawRelative(dy, dx);
        if (depth > 1)
            Hilbert(depth - 1, dx, dy);

        DrawRelative(-dx, -dy);
        if (depth > 1)
            Hilbert(depth - 1, -dy, -dx);
    }

    // Draw the line (LastX, LastY)-(LastX + dx, LastY + dy) and
    // update LastX = LastX + dx, LastY = LastY + dy.
    private void DrawRelative(float dx, float dy)
    {
        Vector3 from = new Vector3(LastX, LastY, LastZ);
        Vector3 to = new Vector3(dx + LastX, dy + LastY, LastZ);

        Gizmos.DrawLine(from, to);
        LastX = LastX + dx;
        LastY = LastY + dy;
    }

    private void HilbertCalculator(int depth, float dx, float dy)
    {
        if (depth > 1)
            HilbertCalculator(depth - 1, dy, dx);

        SavePosition(dx, dy);
        if (depth > 1)
            HilbertCalculator(depth - 1, dx, dy);

        SavePosition(dy, dx);
        if (depth > 1)
            HilbertCalculator(depth - 1, dx, dy);

        SavePosition(-dx, -dy);
        if (depth > 1)
            HilbertCalculator(depth - 1, -dy, -dx);
    }

    private void SavePosition(float dx, float dy)
    {
        Vector3 from = new Vector3(LastX, LastY, 0.0f);
        Vector3 to = new Vector3(dx + LastX, dy + LastY, 0.0f);
        int x = pointsNumber / Size();
        int y = pointsNumber % Size();
        pointsNumber++;
        float xGrid = from.x / size;
        float yGrid = from.y / size;
        points.Add(new Point(from, to, x, y, xGrid, yGrid));

        LastX = LastX + dx;
        LastY = LastY + dy; ;
    }

    private void AutoSizeDraw()
    {
        float camHeight = 2.0f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        totalLength = (float)(0.8 * camHeight);

        size = (float)(totalLength / (Mathf.Pow(2, level) - 1));
        Gizmos.color = Color.white;
        //Start from bottom left
        Vector3 startPosition = new Vector3(0.0f, 1, cam.nearClipPlane);

        Vector3 pos = cam.ScreenToWorldPoint(startPosition);

        LastX = pos.x + (totalLength / 2);
        LastY = pos.y + 1;
        LastZ = pos.z;
    }
}

public struct Point
{
    public Vector3 position;
    public Vector3 nextPosition;
    public float rotation;
    public int x;
    public int y;
    public float xGrid;
    public float yGrid;

    public Point(Vector3 position_, Vector3 nextPosition_, int x_, int y_, float xGrid_, float yGrid_)
    {
        position = position_;
        nextPosition = nextPosition_;
        rotation = (position_.x - nextPosition_.x) * 90.0f + ((nextPosition_.y - position_.y) < 0 ? 180.0f : 0.0f);
        x = x_;
        y = y_;
        xGrid = xGrid_;
        yGrid = yGrid_;
    }
}