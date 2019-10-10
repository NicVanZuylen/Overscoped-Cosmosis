using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestPlate : PullObject
{
    [Tooltip("Amount of health for the chestplate. (beam does 1 damage per second)")]
    [SerializeField]
    private float m_fHealth = 5.0f;

    [Tooltip("Amount of time until the object will dissolve.")]
    [SerializeField]
    private float m_fDissolveTime = 10.0f;

    private PlayerBeam m_playerBeamScript;
    private BossBehaviour m_bossScript;
    private Material m_material;

    new private void Awake()
    {
        base.Awake();

        m_playerBeamScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBeam>();
        m_bossScript = GetComponentInParent<BossBehaviour>();

        m_material = GetComponent<MeshRenderer>().material;
    }

    new private void Update()
    {
        m_fDissolveTime = Mathf.Max(m_fDissolveTime - Time.deltaTime, 0.0f);

        m_material.SetFloat("_Dissolve", Mathf.Min(m_fDissolveTime, 1.0f));

        // Disable when fully dissolved.
        if (m_fDissolveTime <= 0.0f)
            gameObject.SetActive(false);
    }

    public override void Trigger(Vector3 playerDirection)
    {
        base.Trigger(playerDirection);

        m_bossScript.ProgressStage();
    }

    /*
    Description: Deal damage from the beam to the chestplate, and destroy it if the health reaches zero. 
    */
    public void DealBeamDamage()
    {
        m_fHealth -= Time.deltaTime;

        Debug.Log("Damage!");

        if(m_fHealth <= 0.0f)
        {
            // Remove chestplate & progress stage.
            Trigger(Vector3.zero);
            enabled = true;

            // Turn of fresnel.
            m_material.SetFloat("_FresnelOnOff", 0.0f);

            // Disable player beam.
            m_playerBeamScript.UnlockBeam(false);
        }
    }
}
