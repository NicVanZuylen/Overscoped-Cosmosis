using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestPlate : PullObject
{
    [Tooltip("Amount of health for the chestplate. (beam does 1 damage per second)")]
    [SerializeField]
    private float m_fHealth = 5.0f;

    private PlayerBeam m_playerBeamScript;
    private BossBehaviour m_bossScript;

    new void Awake()
    {
        base.Awake();

        m_playerBeamScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBeam>();
        m_bossScript = GetComponentInParent<BossBehaviour>();
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

        if(m_fHealth <= 0.0f)
        {
            // Remove chestplate.
            Trigger(Vector3.zero);

            // Progress stage.
            m_bossScript.ProgressStage();

            // Disable player beam.
            m_playerBeamScript.UnlockBeam(false);
        }
    }
}
