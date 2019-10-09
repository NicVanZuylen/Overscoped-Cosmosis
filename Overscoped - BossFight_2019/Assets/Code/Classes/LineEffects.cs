using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/*
 * Description: Handles line stretching, bending and coiling effects using bezier curves and a compute shader to position line points.
 * Author: Nic Van Zuylen
*/

struct LineEffectParameters
{
    public int m_nWaveCount;
    public float m_fRopeShakeDelay;
    public float m_fShakeSpeed;
    public float m_fShakeMagnitude;
    public float m_fRippleWaveAmp;
    public float m_fPopThickness;
    public float m_fPopRate;
    public float m_fImpactShakeDuration;
    public float m_fImpactShakeMult;
    public float m_fLineThickness;
}

class LineEffects
{
    // Compute shader
    private ComputeBuffer m_outputPointBuffer;
    private ComputeBuffer m_bezierPointBuffer; // Contains points for the main and wobble bezier curves.
    private ComputeBuffer m_bezierIntPointBuffer;
    private int m_nPointKernelIndex;
    private const int m_nWobbleBezierCount = 16;

    // Effect parameters.
    private ComputeShader m_computeShader;
    private int m_nWaveCount = 3;
    private float m_fRopeShakeDelay = 0.1f;
    private float m_fShakeSpeed = 0.1f;
    private float m_fShakeMagnitude = 0.05f;
    private float m_fRippleWaveAmp = 0.15f;
    private float m_fPopThickness = 2.0f;
    private float m_fPopRate = 3.0f;
    private float m_fImpactShakeDuration = 0.6f;
    private float m_fImpactShakeMult = 5.0f;
    private float m_fLineThickness;

    // Effect variables.
    private Bezier m_ropeCurve;
    private Vector3[] m_v3GrapLinePoints;
    private Vector3[] m_v3ShakeVectors;
    private float m_fShakeTime;
    private float m_fRippleMult;
    private float m_fImpactShakeTime;
    private float m_fCurrentLineThickness;
    private float m_fGrappleTime;

    public LineEffects(ComputeShader shader, LineEffectParameters effectParameters, int nLinePositionCount)
    {
        m_computeShader = shader;

        m_nWaveCount = effectParameters.m_nWaveCount;
        m_fRopeShakeDelay = effectParameters.m_fRopeShakeDelay;
        m_fShakeSpeed = effectParameters.m_fShakeSpeed;
        m_fShakeMagnitude = effectParameters.m_fShakeMagnitude;
        m_fRippleWaveAmp = effectParameters.m_fRippleWaveAmp;
        m_fPopThickness = effectParameters.m_fPopThickness;
        m_fPopRate = effectParameters.m_fPopRate;
        m_fImpactShakeDuration = effectParameters.m_fImpactShakeDuration;
        m_fImpactShakeMult = effectParameters.m_fImpactShakeMult;
        m_fLineThickness = effectParameters.m_fLineThickness;

        const int nPointCount = 4;

        m_outputPointBuffer = new ComputeBuffer(nLinePositionCount * 2, sizeof(float) * 3); // Multiplied by two to include wobble effect vectors.
        m_bezierPointBuffer = new ComputeBuffer(nPointCount + m_nWobbleBezierCount, sizeof(float) * 3); // Contains points for the main and wobble bezier curves.
        m_bezierIntPointBuffer = new ComputeBuffer((nPointCount + m_nWobbleBezierCount) * nLinePositionCount, sizeof(float) * 3);

        m_ropeCurve = new Bezier(nPointCount);

        // Rope points.
        m_v3GrapLinePoints = new Vector3[nLinePositionCount];
        m_v3ShakeVectors = new Vector3[m_nWobbleBezierCount];

        m_v3ShakeVectors[0] = Vector3.zero;
        m_v3ShakeVectors[m_v3ShakeVectors.Length - 1] = Vector3.zero;

        m_nPointKernelIndex = shader.FindKernel("CSMain");
        shader.SetBuffer(m_nPointKernelIndex, "outPoints", m_outputPointBuffer);
        shader.SetBuffer(m_nPointKernelIndex, "bezierPoints", m_bezierPointBuffer);
        shader.SetBuffer(m_nPointKernelIndex, "bezierIntPoints", m_bezierIntPointBuffer);

        shader.SetInt("inPointCount", nPointCount);
        shader.SetInt("inWobblePointCount", m_nWobbleBezierCount);

        m_computeShader = shader;
    }

    public void DestroyBuffers()
    {
        m_outputPointBuffer.Release();
        m_bezierPointBuffer.Release();
        m_bezierIntPointBuffer.Release();
    }

    public void ProcessLine(LineRenderer line, PlayerController controller, Transform originNode, Vector3 v3Destination, float fProgress, bool bActive)
    {
        // ------------------------------------------------------------------------------------------------------------------------------
        // Effects

        if (!bActive)
        {
            // Expand thickness after use.
            m_fCurrentLineThickness = Mathf.MoveTowards(m_fCurrentLineThickness, m_fPopThickness, m_fPopRate * Time.deltaTime);

            // Set line shader opacity.
            line.material.SetFloat("_Opacity", 1.0f - (m_fCurrentLineThickness / m_fPopThickness));

            // Grapple is not active, play the poof effect.
            line.startWidth = m_fCurrentLineThickness;
            line.endWidth = m_fCurrentLineThickness;

            line.enabled = m_fCurrentLineThickness < (m_fPopThickness - 0.1f);

            return;
        }

        line.enabled = true;

        // Reset thickness to default when in use.
        m_fCurrentLineThickness = m_fLineThickness;
        line.startWidth = m_fCurrentLineThickness;
        line.endWidth = m_fCurrentLineThickness;

        // Reset line shader opacity.
        line.material.SetFloat("_Opacity", 1.0f);

        Vector3 v3DiffNoY = (v3Destination + originNode.position) * 0.5f;
        v3DiffNoY -= originNode.position;
        v3DiffNoY.y = 0.0f;

        // Get amount the player is look horizontally away to the destination.
        // This will be applied to the curve.
        Vector3 v3HorizontalVec = controller.LookRight();
        float fHorizontalAmount = Vector3.Dot(v3HorizontalVec, v3DiffNoY);

        Vector3 v3CurveCorner = originNode.position + v3DiffNoY - (fHorizontalAmount * v3HorizontalVec);

        // Curve points.
        m_ropeCurve.m_v3Points[0] = originNode.position;
        m_ropeCurve.m_v3Points[1] = v3CurveCorner;
        m_ropeCurve.m_v3Points[2] = v3Destination;
        m_ropeCurve.m_v3Points[m_ropeCurve.m_v3Points.Length - 1] = v3Destination;

        // Value specifiying the shake magnitude for this frame.
        float fShakeMag = m_fShakeMagnitude;

        // When hooked-in tension should be high so remove the wobble effect.
        if (fProgress >= 1.0f)
        {
            if (m_fImpactShakeTime > 0.0f && m_fRippleMult <= 0.1f)
            {
                // Rope shake during impact shake time.
                float fImpactShakeMult = Mathf.Clamp(m_fImpactShakeTime / m_fImpactShakeDuration, 0.0f, 1.0f);
                fShakeMag = m_fImpactShakeMult * fImpactShakeMult;

                // Override shake time to be zero, causing a very fast shake.
                m_fShakeTime = 0.0f;

                m_fImpactShakeTime -= Time.deltaTime;
            }

            // Lerp ripple multiplier to zero.
            m_fRippleMult = Mathf.Lerp(m_fRippleMult, 0.0f, 0.4f);
        }
        else
        {
            m_fRippleMult = m_fRippleWaveAmp;

            m_fImpactShakeTime = m_fImpactShakeDuration;
        }

        Vector3 v3WobbleShake = Vector3.zero;

        // Calculate shake offsets.
        if (m_fShakeTime <= 0.0f)
        {
            for (int i = 1; i < m_nWobbleBezierCount - 1; ++i)
            {
                Vector3 v3RandomOffset;

                float fRemainingMag = 1.0f;

                // Calculate random unit vector.
                v3RandomOffset.x = Random.Range(-1.0f, 1.0f);
                fRemainingMag -= Mathf.Abs(v3RandomOffset.x);

                v3RandomOffset.y = Random.Range(-fRemainingMag, fRemainingMag);
                fRemainingMag -= Mathf.Abs(v3RandomOffset.y);

                v3RandomOffset.z = Random.Range(-fRemainingMag, fRemainingMag);

                // Multiply by magnitude.
                m_v3ShakeVectors[i] = v3RandomOffset * fShakeMag;
            }

            m_fShakeTime = m_fRopeShakeDelay;
        }
        else
            m_fShakeTime -= Time.deltaTime;

        // Set compute shader globals...
        m_computeShader.SetFloat("inFlyProgress", fProgress);
        m_computeShader.SetFloat("inRippleMagnitude", m_fRippleMult);
        m_computeShader.SetFloat("inDeltaTime", Time.deltaTime);

        // Set compute shader buffer data...
        m_bezierPointBuffer.SetData(m_ropeCurve.m_v3Points, 0, 0, m_ropeCurve.m_v3Points.Length);
        m_bezierPointBuffer.SetData(m_v3ShakeVectors, 0, m_ropeCurve.m_v3Points.Length, m_nWobbleBezierCount);

        m_computeShader.Dispatch(m_nPointKernelIndex, Mathf.CeilToInt(line.positionCount / 256.0f), 1, 1);

        // Get compute shader output.
        m_outputPointBuffer.GetData(m_v3GrapLinePoints, 0, 0, line.positionCount);

        // Ensure points connect to start and end nodes.
        //m_v3GrapLinePoints[0] = originNode.position;

        // Apply points to line renderer.
        line.SetPositions(m_v3GrapLinePoints);

        // ------------------------------------------------------------------------------------------------------------------------------
    }
}