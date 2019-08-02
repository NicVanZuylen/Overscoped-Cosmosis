using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTree;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(LineRenderer))]

public class BossBehaviour : MonoBehaviour
{
    [Tooltip("Player object reference.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Meteor object reference.")]
    [SerializeField]
    private Meteor m_meteor = null;

    [Tooltip("Armour object reference.")]
    [SerializeField]
    private GameObject m_armour = null;

    [SerializeField]
    private GameObject m_portal = null;

    [Tooltip("Origin point of the beam attack.")]
    [SerializeField]
    private Transform m_beamOrigin = null;

    [Tooltip("Distance in which the boss will attempt a slam attack.")]
    [SerializeField]
    private float m_fSlamDistance = 10.0f;

    public bool m_bIsStuck;

    [Header("Meteor")]
    [Tooltip("Amount of time before meteor attack can be used again.")]
    [SerializeField]
    private float m_fMeteorCD = 10.0f;
    private float m_fMeteorCDTimer;

    [Tooltip("The Rectangular extents of the meteor summon area.")]
    [SerializeField]
    private Vector3 m_v3MeteorSummonExtents;

    [Header("Portal Punch")]
    [Tooltip("Amount of time before portal punch attack can be used again.")]
    [SerializeField]
    private float m_fPortalPunchCD = 4.0f;
    private float m_fPortalPunchCDTimer;

    [Header("Beam")]
    [Tooltip("Amount of time before the beam attac k can be used again.")]
    [SerializeField]
    private float m_fBeamAttackCD = 7.0f;
    private float m_fBeamAttackCDTimer;

    [Tooltip("Maximum range of the beam attack.")]
    [SerializeField]
    private float m_fBeamMaxRange = 100.0f;

    [Tooltip("Damage per second of the beam attack.")]
    [SerializeField]
    private float m_fBeamDPS = 10.0f;

    [Tooltip("Duration of the beam attack.")]
    [SerializeField]
    private float m_fBeamDuration = 5.0f;

    [Tooltip("Maximum beam attack tracking speed.")]
    [SerializeField]
    private float m_fBeamTrackSpeed = 0.05f;

    [Header("Misc.")]
    [Tooltip("Amount of time spent stuck.")]
    [SerializeField]
    private float m_fStuckTime = 0.0f;

    [Tooltip("Minimum delay before any kind of attack is performed.")]
    [SerializeField]
    private float m_fTimeBetweenAttacks = 5.0f;


    private PlayerController m_playerController;
    private PlayerStats m_playerStats;
    private Animator m_animator;
    private LineRenderer m_beamLine;
    private BehaviourNode m_bossTree;
    private float m_fTimeSinceGlobalAttack = 0.0f;

    private Portal m_portalScript;
    private Vector3 m_v3BeamEnd;
    private Vector3 m_v3BeamDirection;
    private float m_fBeamTime;

    void Awake()
    {
        m_playerController = m_player.GetComponent<PlayerController>();
        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_animator = GetComponent<Animator>();
        m_beamLine = GetComponent<LineRenderer>();
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        // Initial attack cooldowns.
        m_fPortalPunchCDTimer = m_fPortalPunchCD;
        m_fMeteorCDTimer = m_fMeteorCD;
        m_fBeamAttackCDTimer = m_fBeamAttackCD;

        m_fBeamTime = m_fBeamDuration;

        m_portalScript = m_portal.GetComponent<Portal>();
        m_portal.SetActive(false);

        string treePath = Application.dataPath + "/Code/BossBehaviours/BossTreePhase1.xml";
        m_bossTree = BTreeEditor.BTreeEditor.LoadTree(treePath, this);
    }

    void Update()
    {
        if (CondIsIdleAnimation() == ENodeResult.NODE_FAILURE)
            m_animator.SetInteger("AttackID", 0);

        m_bossTree.Run();

        m_fTimeSinceGlobalAttack -= Time.deltaTime;

        m_fPortalPunchCDTimer -= Time.deltaTime;
        m_fMeteorCDTimer -= Time.deltaTime;
        m_fBeamAttackCDTimer -= Time.deltaTime;
    }

    // ----------------------------------------------------------------------------------------------
    // Conditions

    public ENodeResult CondPlayerGrounded()
    {
        if (m_playerController.IsGrounded())
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondGlobalAttackCD()
    {
        if (m_fTimeSinceGlobalAttack <= 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondNotGlobalAttackCD()
    {
        if (m_fTimeSinceGlobalAttack > 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondWithinSlamDistance()
    {
        if ((m_player.transform.position - transform.position).sqrMagnitude <= m_fSlamDistance * m_fSlamDistance)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondSlamCD()
    {
        if (m_fPortalPunchCDTimer <= 0.0f)
        {
            m_fPortalPunchCDTimer = m_fPortalPunchCD;
            return ENodeResult.NODE_SUCCESS;
        }
        return ENodeResult.NODE_FAILURE;
    }

    // Meteor
    public ENodeResult CondMeteorCD()
    {
        if (m_fMeteorCDTimer <= 0.0f)
        {
            m_fMeteorCDTimer = m_fMeteorCD;
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondBeamCD ()
    {
        if(m_fBeamAttackCDTimer <= 0.0f)
        {
            m_fBeamAttackCDTimer = m_fBeamAttackCD;
            m_fBeamTime = m_fBeamDuration;
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondBossStuck()
    {
        if (m_bIsStuck)
        {
            m_animator.enabled = false;
            m_armour.tag = "PullObj";
            StartCoroutine(ResetStuck());
            return ENodeResult.NODE_SUCCESS;
        }
        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPortalNotActive()
    {
        if (m_portalScript.IsActive())
            return ENodeResult.NODE_FAILURE;

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult CondIsIdleAnimation()
    {
        if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondBeamActive()
    {
        m_fBeamTime -= Time.deltaTime;

        if(CondBeamCD() == ENodeResult.NODE_SUCCESS || m_fBeamTime > 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }
        else if(m_fBeamTime <= 0.0f)
        {
            // Beam attack is complete.
            m_beamLine.enabled = false;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondBeamNotActive()
    {
        if(CondBeamActive() == ENodeResult.NODE_SUCCESS)
        {
            return ENodeResult.NODE_FAILURE;
        }

        return ENodeResult.NODE_SUCCESS;
    }

    // ----------------------------------------------------------------------------------------------
    // Actions

    public ENodeResult ActPlayPortalPunchAnim()
    {
        Debug.Log("Portal Punch!");

        m_animator.SetInteger("AttackID", 1);
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActPlayMeteorAnim()
    {
        Debug.Log("Meteor Attack!");

        m_animator.SetInteger("AttackID", 2);
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        Vector3 pos = 
            new Vector3(transform.position.x, transform.position.y + 100, transform.position.z) + 
            new Vector3(Random.Range(-m_v3MeteorSummonExtents.x / 2, m_v3MeteorSummonExtents.x / 2), Random.Range(-m_v3MeteorSummonExtents.y / 2, m_v3MeteorSummonExtents.y / 2), Random.Range(-m_v3MeteorSummonExtents.z / 2, m_v3MeteorSummonExtents.z / 2));

        m_meteor.Summon(pos, m_player.transform.position);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActTrackPlayer()
    {
        Vector3 lookPos = m_player.transform.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 2);

        return ENodeResult.NODE_SUCCESS;
    }

    // Track the beam's aim. Even if the beam is not in use.
    public ENodeResult ActBeamTrack()
    {
        Vector3 v3PlayerDir = (m_player.transform.position - m_beamOrigin.position).normalized;

        Vector3 v3Target = m_beamOrigin.position + (v3PlayerDir * m_fBeamMaxRange);

        m_v3BeamDirection = (m_v3BeamEnd - m_player.transform.position).normalized;

        float fBeamProgress = 1.0f - (m_fBeamTime / m_fBeamDuration);

        // Move beam end towards target.
        if(m_beamLine.enabled)
            m_v3BeamEnd = Vector3.Lerp(m_v3BeamEnd, v3Target, Mathf.Clamp(fBeamProgress * m_fBeamTrackSpeed, 0.01f, m_fBeamTrackSpeed));
        else // Tracking whilst inactive.
            m_v3BeamEnd = Vector3.Lerp(m_v3BeamEnd, v3Target, 0.01f);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActUseBeam()
    {
        // Enable beam if it is disabled.
        if (!m_beamLine.enabled)
        {
            m_beamLine.enabled = true;
        }

        // Look at player.
        ActTrackPlayer();

        // Linerenderer points.
        Vector3[] beamLinePoints = new Vector3[2];
        beamLinePoints[0] = m_beamOrigin.position;

        Ray beamRay = new Ray(m_beamOrigin.position, m_v3BeamDirection);

        RaycastHit beamHit;
        if(Physics.SphereCast(beamRay, 0.2f, out beamHit, m_fBeamMaxRange))
        {
            // Deal damage to the player.
            if(beamHit.collider.gameObject == m_player)
            {
                m_playerStats.DealDamage(m_fBeamDPS * Time.deltaTime);

                beamLinePoints[1] = m_v3BeamEnd;
            }
            else // Use ray hit point as the end point if the beam ray hits something other than the player.
                beamLinePoints[1] = beamHit.point;
        }
        else // Otherwise just use the existing beam end point.
            beamLinePoints[1] = m_v3BeamEnd;


        m_beamLine.SetPositions(beamLinePoints);

        return ENodeResult.NODE_SUCCESS;
    }

    // ----------------------------------------------------------------------------------------------
    // Misc

    public void ResetAnimToIdle()
    {
        m_animator.SetInteger("AttackID", 0);
    }

    IEnumerator ResetStuck()
    {
        yield return new WaitForSeconds(m_fStuckTime);
        m_bIsStuck = false;
        m_animator.enabled = true;
        m_armour.tag = "Untagged";
    }

    public void SummonPortal()
    {
        // Do nothing if the portal is already active.
        if (m_portalScript.IsActive())
            return;

        Vector3 v3PortalOffset = Vector3.up * 10.0f;
        //float fRemainingMag = 1.0f;

        // Create random unit vector.
        //v3RandomVec.x = Random.Range(-1.0f, 1.0f);
        //v3RandomVec.y = Random.Range(0.0f, 1.0f);
        //v3RandomVec.z = Random.Range(-1.0f, 1.0f);

        //v3RandomVec.Normalize();

        m_portal.SetActive(true);
        m_portal.transform.position = m_player.transform.position + m_playerController.GetVelocity() + v3PortalOffset;

        Vector3 v3PlayerDir = (m_player.transform.position - m_portal.transform.position).normalized;
        m_portal.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.up);

        return;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Stuck")
        {
            Destroy(other.gameObject);
            m_bIsStuck = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(new Vector3(transform.position.x, transform.position.y + 100, transform.position.z), m_v3MeteorSummonExtents);
    }

}
