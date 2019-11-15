using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Description: Controls health and destruction of the boss chest barrier field.
 * Author: Nic Van Zuylen
*/

public class ChestPlate : MonoBehaviour
{
    [Tooltip("Amount of health for the chestplate. (beam does 1 damage per second)")]
    [SerializeField]
    public float m_fMaxHealth = 5.0f;

    [Tooltip("Amount of time for the field to pop when destroyed.")]
    [SerializeField]
    private float m_fPopTime = 0.5f;

    [Tooltip("Health bar material representing the force field health.")]
    [SerializeField]
    private Material m_healthFillMat = null;

    [SerializeField]
    private Material m_healthBarThumpMat = null;

    [Tooltip("Color gradient for the health bar as it lowers.")]
    [SerializeField]
    private Gradient m_healthGradient = null;

    [Tooltip("Collider for the boss's heart.")]
    [SerializeField]
    private GameObject m_heart = null;

    private PlayerBeam m_playerBeamScript; // Reference to the player beam controller script.
    private CameraEffects m_camEffects; // Reference to the camera FX controller script.
    private BossBehaviour m_bossScript; // Reference to the Boss AI script.
    private Material m_material; // Reference to the barrier material.
    public float m_fHealth; // Current health of the barrier.

    private void Awake()
    {
        m_playerBeamScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBeam>();
        m_camEffects = m_playerBeamScript.gameObject.GetComponentInChildren<CameraEffects>();
        m_bossScript = GetComponentInParent<BossBehaviour>();
        m_material = GetComponent<MeshRenderer>().sharedMaterial;

        // Heart is initially impossible to grapple.
        m_heart.layer = LayerMask.NameToLayer("NoGrapple");

        // Set initial material values.
        m_material.SetFloat("_Pop", 0.0f);
        m_material.SetFloat("_IsHit", 0.0f);

        // Initial health.
        m_fHealth = m_fMaxHealth;

        m_healthBarThumpMat.SetInt("_ThumpingSwitch", 0);

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

    private void OnDestroy()
    {
        m_material.SetFloat("_IsHit", 0.0f);
        m_healthFillMat.SetFloat("_Resource", 1.0f); // Reset health bar fill.
        m_healthBarThumpMat.SetInt("_ThumpingSwitch", 0);
    }

    /*
    Description: Deal damage from the beam to the chestplate, and destroy it if the health reaches zero. 
    */
    public void DealBeamDamage()
    {
        m_fHealth -= Time.deltaTime;
        m_bossScript.TakeHit(); 

        float fHealthPercentage = 1.0f - (m_fHealth / m_fMaxHealth);

        // Set barrier colour lerp.
        m_material.SetFloat("_IsHit", fHealthPercentage);

        // Set health bar fill & colour.
        m_healthFillMat.SetFloat("_Resource", 1.0f - fHealthPercentage);
        m_healthFillMat.color = m_healthGradient.Evaluate(fHealthPercentage);

        if (m_fHealth <= 0.0f && !enabled)
        {
            // Remove chestplate & progress stage.
            m_bossScript.ProgressStage();
            enabled = true;

            // Begin camera shake feedback.
            m_camEffects.ApplyShake(0.5f, 1.0f, true);

            // Begin health bar thumping.
            m_healthBarThumpMat.SetInt("_ThumpingSwitch", 1);

            // Reset heart layer to default.
            m_heart.layer = 0;

            // Disable player beam.
            m_playerBeamScript.UnlockBeam(false);
        }
    }
}
