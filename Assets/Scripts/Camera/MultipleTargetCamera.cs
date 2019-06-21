using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MultipleTargetCamera : MonoBehaviour
{
    #region Properties
    //PUBLIC
    public List<Vector3> targets;
    public Vector3 offset;
    public float zoomLimiter = 50.0f;
    //PRIVATE
    private Vector3 velocity;
    private Camera cam;
    #endregion

    void Awake()
    {
        cam = GetComponent<Camera>();
    }


    void LateUpdate()
    {
        
    }

    public void Recalculate(List<Vector3> points)
    {
        if (points.Count > 0)
        {
            targets.Clear();

            foreach (Vector3 p in points)
                targets.Add(p);

            Move();
            Zoom();
        }
    }

    private void Move()
    {
        Debug.Log("move camera "+targets.Count);
        Vector3 centerPoint = GetCenterPoint();
        if (centerPoint != transform.position)
            centerPoint += offset;

        transform.position = centerPoint;
    }

    private void Zoom()
    {
        Debug.Log("zoom camera " + targets.Count);
        float newZoom = GetGreatestDistance();
        newZoom = (newZoom == 0.0f ? 10.0f : newZoom);
        cam.orthographicSize = newZoom / zoomLimiter;
    }

    private float GetGreatestDistance()
    {
        Bounds bounds = new Bounds(targets[0], Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i]);
        }
        Debug.Log("Bond size "+ bounds.size.x);
        return (bounds.size.x > bounds.size.y ? bounds.size.x : bounds.size.y);
    }

    private Vector3 GetCenterPoint()
    {
        if (targets.Count == 1)
            return targets[0];
        
        if (targets.Count <= 0)
        {
            Debug.Log("t -> " + targets.Count);
            return transform.position;
        }
            

        Bounds bounds = new Bounds(targets[0], Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i]);
        }
        Debug.Log("Center "+bounds.center);
        return bounds.center;
    }
}
