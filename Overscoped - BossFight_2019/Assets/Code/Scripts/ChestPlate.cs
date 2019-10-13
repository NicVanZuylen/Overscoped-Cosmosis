using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Controls health and destruction of the boss chest barrier field.
 * Author: Nic Van Zuylen
*/

public class ChestPlate : MonoBehaviour
{
    [Tooltip("Amount of health for the chestplate. (beam does 1 damage per second)")]
    [SerializeField]
    private float m_fMaxHealth = 5.0f;

    [Tooltip("Amount of time for the field to pop when destroyed.")]
    [SerializeField]
    private float m_fPopTime = 0.5f;

    private PlayerBeam m_playerBeamScript;
    private BossBehaviour m_bossScript;
    private Material m_material;
    private float m_fHealth;

    private void Awake()
    {
        m_playerBeamScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBeam>();
        m_bossScript = GetComponentInParent<BossBehaviour>();

        m_material = GetComponent<MeshRenderer>().material;

        m_material.SetFloat("_Pop", 0.0f);

        m_fHealth = m_fMaxHealth;

        m_material.SetFloat("_Lerp", 0.0f);

        // Disable script to prevent premature popping.
        enabled = false;
    }

    private void Update()
    {
        m_fPopTime = Mathf.Max(m_fPopTime - Time.deltaTime, 0.0f);

        // Set barrier pop level.
        m_material.SetFloat("_Pop", Mathf.Min(m_fPopTime, 1.0f));

        // Disable when fully dissolved.
        if (m_fPopTime <= 0.0f)
            gameObject.SetActive(false);
    }

    /*
    Description: Deal damage from the beam to the chestplate, and destroy it if the health reaches zero. 
    */
    public void DealBeamDamage()
    {
        m_fHealth -= Time.deltaTime;

        if(m_fHealth <= 0.0f)
        {
            // Remove chestplate & progress stage.
            m_bossScript.ProgressStage();
            enabled = true;

            // Set barrier colour lerp.
            m_material.SetFloat("_Lerp", 1.0f - (m_fHealth / m_fMaxHealth));

            // Disable player beam.
            m_playerBeamScript.UnlockBeam(false);
        }
    }
}
