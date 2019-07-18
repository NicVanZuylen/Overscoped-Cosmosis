using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier
{
    private Vector3 m_v3Start;
    private Vector3 m_v3Corner;
    private Vector3 m_v3End;

    public void SetStart(Vector3 v3Start)
    {
        m_v3Start = v3Start;
    }

    public void SetCorner(Vector3 v3Corner)
    {
        m_v3Corner = v3Corner;
    }

    public void SetEnd(Vector3 v3End)
    {
        m_v3End = v3End;
    }

    /*
    Description: Get the point at the specified percentage through the curve. 
    Return Type: Vector3
    Param:
        float fDistance: The distance along the curve to return, between 0 (start) and 1 (end).
    */
    public Vector3 Evaluate(float fDistance)
    {
        // Clamp between 0 and 1.
        fDistance = Mathf.Clamp(fDistance, 0.0f, 1.0f);

        // Distance along first line.
        Vector3 v3FirstLinePoint = Vector3.Lerp(m_v3Start, m_v3Corner, fDistance);

        // Distance along second line.
        Vector3 v3SecondLinePoint = Vector3.Lerp(m_v3Corner, m_v3End, fDistance);

        return Vector3.Lerp(v3FirstLinePoint, v3SecondLinePoint, fDistance);
    }
}
