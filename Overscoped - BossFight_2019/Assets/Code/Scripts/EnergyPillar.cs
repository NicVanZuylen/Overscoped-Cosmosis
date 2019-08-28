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
    private ParticleSystem m_explosion = null;

    private float m_fCharge = 0.0f;

    private EnergyPillar[] m_energyPillars;
    private SphereCollider m_vicinityCollider;

    private static bool m_bPlayerWithinVicinity;

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

    /*
    Description: Get whether or not the player is within the vicinity of any of the energy pillars.
                 This function should only be used by the BossBehaviour script.
    Return Type: bool
    */
    public static bool PlayerWithinVicinity()
    {
        bool bResult = m_bPlayerWithinVicinity;

        // Reset if true.
        if (m_bPlayerWithinVicinity)
            m_bPlayerWithinVicinity = false;

        return bResult;
    }

    private void ResetCharge()
    {
        m_fCharge = 0;
    }

    private void OnTriggerStay(Collider other)
    {
        m_bPlayerWithinVicinity |= (other.tag == "Player");
    }
}
