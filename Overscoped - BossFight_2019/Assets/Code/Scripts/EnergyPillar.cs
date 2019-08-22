using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyPillar : MonoBehaviour
{
    [Tooltip("Charge the pillar will explode at when reached.")]
    [SerializeField]
    private float m_fMaxCharge = 100.0f;

    [Tooltip("Rate of pillar charge increase.")]
    [SerializeField]
    private float m_fChargeRate = 25.0f;

    private float m_fCharge = 0.0f;

    private void Awake()
    {
        
    }

    public void Charge(BossBehaviour bossScript)
    {
        m_fCharge += m_fChargeRate * Time.deltaTime;

        if (m_fCharge >= m_fMaxCharge)
            Explode(bossScript);
    }

    public void Explode(BossBehaviour bossScript)
    {
        // Stun boss.
        bossScript.m_bIsStuck = true;

        // Deactivate object. (Will have effects for destruction later.)
        gameObject.SetActive(false);
    }
}
