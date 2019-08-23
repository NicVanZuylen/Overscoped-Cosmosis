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

    [Tooltip("Reference to Explosion Particle")]
    [SerializeField]
    private ParticleSystem m_explosion;

    private float m_fCharge = 0.0f;

    private EnergyPillar[] m_energyPillars;

    private void Awake()
    {
        m_energyPillars = FindObjectsOfType<EnergyPillar>();
        m_explosion.Stop();
    }

    public void Charge(BossBehaviour bossScript)
    {
        m_fCharge += m_fChargeRate * Time.deltaTime;

        if (m_fCharge >= m_fMaxCharge)
            Explode(bossScript);
    }

    public void Explode(BossBehaviour bossScript)
    {
        m_explosion.transform.position = new Vector3(transform.position.x,transform.position.y + 70, transform.position.z);
        // Stun boss.
        bossScript.m_bIsStuck = true;

        m_explosion.Play();

        foreach(EnergyPillar energyPiller in m_energyPillars)
        {
            energyPiller.ResetCharge();
        }

        //Add explosion effect, don't destory

        // Deactivate object. (Will have effects for destruction later.)
        gameObject.SetActive(false);
    }

    private void ResetCharge()
    {
        m_fCharge = 0;
    }
}
