using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Bezier
{
    //public Vector3 m_v3Start;
    public Vector3[] m_v3Points;
    //public Vector3 m_v3End;
    public Vector3[] m_v3InterpolatedPoints;

    /*
    Constructor:
    Param:
        int nCornerCount: The amount of corner points within the curve.
    */
    public Bezier(int nCornerCount)
    {
        //m_v3Start = Vector3.zero;
        //m_v3End = Vector3.zero;
        m_v3Points = new Vector3[nCornerCount + 2];
        m_v3InterpolatedPoints = new Vector3[nCornerCount + 2];
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

        // m_v3InterpolatedPoints contains the interpolated points within the line segments (between start, corners, end).
        m_v3InterpolatedPoints[0] = Vector3.Lerp(m_v3Points[0], m_v3Points[1], fDistance);

        for(int i = 1; i < m_v3InterpolatedPoints.Length; ++i)
        {
            // Interpolate between previous interpolated point and interpolated point in the current line segment.
            m_v3InterpolatedPoints[i] = Vector3.Lerp(m_v3InterpolatedPoints[i - 1], Vector3.Lerp(m_v3Points[i - 1], m_v3Points[i], fDistance), fDistance);
        }

        // Return final interpolation.
        //return Vector3.Lerp(m_v3InterpolatedPoints[m_v3InterpolatedPoints.Length - 1], Vector3.Lerp(m_v3Points[m_v3Points.Length - 1], m_v3End, fDistance), fDistance);
        return m_v3InterpolatedPoints[m_v3InterpolatedPoints.Length - 1];
    }
}
