using BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Description: Contains AI behaviour for the boss.
 * Author: Nic Van Zuylen, Lachlan Mesman
*/

public struct AttackRating
{
    public int m_nPrefScore;
    public bool m_bAvailable;
}

[RequireComponent(typeof(Animator))]

public class BossBehaviour : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------
    [Header("References")]

    [Tooltip("Player object reference.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Meteor object references.")]
    [SerializeField]
    private Meteor[] m_meteors = null;

    [SerializeField]
    private GameObject m_portal = null;

    [Tooltip("Origin point of the beam attack.")]
    [SerializeField]
    private Transform m_beamOrigin = null;

    [Header("Stages")]
    [SerializeField]
    private string[] m_stageNames = null;

    // -------------------------------------------------------------------------------------------------
    [Header("Attacks")]

    [Tooltip("Distance in which the boss will attempt a slam attack.")]
    [SerializeField]
    private float m_fSlamDistance = 10.0f;

    [Header("Meteor")]
    [Tooltip("Amount of time before meteor attack can be used again.")]
    [SerializeField]
    private float m_fMeteorCD = 10.0f;

    [Header("Portal Punch")]
    [Tooltip("Amount of time before portal punch attack can be used again.")]
    [SerializeField]
    private float m_fPortalPunchCD = 4.0f;

    // -------------------------------------------------------------------------------------------------
    [Header("Beam")]

    [Tooltip("Amount of time before the beam attac k can be used again.")]
    [SerializeField]
    private float m_fBeamAttackCD = 7.0f;

    [Tooltip("Maximum range of the beam attack.")]
    [SerializeField]
    private float m_fBeamMaxRange = 100.0f;

    [Tooltip("Damage per second of the beam attack.")]
    [SerializeField]
    private float m_fBeamDPS = 10.0f;

    [Tooltip("Duration of the beam attack.")]
    [SerializeField]
    private float m_fBeamDuration = 5.0f;

    [Tooltip("Minimum beam attack tracking speed.")]
    [SerializeField]
    private float m_fMinBeamTrackSpeed = 50.0f;

    [Tooltip("Maximum beam attack tracking speed.")]
    [SerializeField]
    private float m_fMaxBeamTrackSpeed = 200.0f;

    [Tooltip("Volumetric beam particle systems.")]
    [SerializeField]
    private ParticleSystem[] m_beamParticles = null;

    // -------------------------------------------------------------------------------------------------
    [Header("VFX")]

    [Tooltip("Reference to an existing particlesystem to use as a meteor summon effect.")]
    [SerializeField]
    private ParticleSystem m_meteorSummonEffect = null;

    // -------------------------------------------------------------------------------------------------
    [Header("Audio")]

    // -------------------------------------------------------------------------------------------------
    [Header("Misc")]

    [Tooltip("Minimum delay before any kind of attack is performed.")]
    [SerializeField]
    private float m_fTimeBetweenAttacks = 5.0f;

    private PlayerController m_playerController;
    private PlayerStats m_playerStats;
    private GrappleHook m_grappleScript;
    private Animator m_animator;
    private float m_fTimeSinceGlobalAttack = 0.0f;
    private GameObject end_Portal;

    // Stages
    private BehaviourNode[] m_bossTreeStages;
    private int m_nStageIndex;

    // Attack decision making
    private AttackRating[] m_attackRatings;
    private int m_nAttackIndex;
    private int m_nChosenAttackIndex;

    // Portal punch
    private Portal m_portalScript;
    private float m_fPortalPunchCDTimer;

    // Beam attack
    private Vector3 m_v3BeamEnd;
    private Vector3 m_v3BeamDirection;
    private ParticleSystemRenderer[] m_beamParticleRenderers;
    private ParticleSystem.Particle[] m_beamSegmentParticles;
    private const int m_nMaxBeamParticles = 512;
    private float m_fBeamAttackCDTimer;
    private float m_fBeamTime;
    private bool m_bBeamActive;

    // Meteor attack
    private Queue<MeteorTarget> m_availableTargets;
    private float m_fMeteorCDTimer;

    // Attack delegates
    delegate ENodeResult AttackFunc();

    private AttackFunc[] m_attacks;

    void Awake()
    {
        m_playerController = m_player.GetComponent<PlayerController>();
        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_grappleScript = m_player.GetComponent<GrappleHook>();
        m_animator = GetComponent<Animator>();
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        end_Portal = GameObject.FindGameObjectWithTag("EndPortal");
        end_Portal.SetActive(false);

        // Initial attack cooldowns.
        m_fPortalPunchCDTimer = m_fPortalPunchCD;
        m_fMeteorCDTimer = m_fMeteorCD;
        m_fBeamAttackCDTimer = m_fBeamAttackCD;

        // Beam
        m_fBeamTime = m_fBeamDuration;
        m_v3BeamEnd = m_player.transform.position;

        // Initialize beam effect buffers.
        m_beamParticleRenderers = new ParticleSystemRenderer[m_beamParticles.Length];

        for(int i = 0; i < m_beamParticles.Length; ++i)
        {
            m_beamParticleRenderers[i] = m_beamParticles[i].GetComponent<ParticleSystemRenderer>();
            m_beamParticleRenderers[i].enabled = false;
        }

        m_beamSegmentParticles = new ParticleSystem.Particle[m_nMaxBeamParticles];

        // Meteor
        GameObject[] allMeteorSpawns = GameObject.FindGameObjectsWithTag("MeteorSpawn");
        
        m_availableTargets = new Queue<MeteorTarget>();

        // Add meteor spawns to spawn object pool and initialize them.
        for (int i = 0; i < allMeteorSpawns.Length; ++i)
        {
            MeteorTarget targetScript = allMeteorSpawns[i].GetComponent<MeteorTarget>();
            targetScript.Init(m_player, m_availableTargets);

            m_availableTargets.Enqueue(targetScript);
        }

        for (int i = 0; i < m_meteors.Length; ++i)
            m_meteors[i].Init(m_player);

        // Portal punch
        m_portalScript = m_portal.GetComponent<Portal>();
        m_portal.tag = "NoGrapple";
        m_portal.SetActive(false);

        // Attack functions...
        m_attacks = new AttackFunc[3];
        m_attacks[0] = ActPlayMeteorAnim;
        m_attacks[1] = ActPlayPortalPunchAnim;
        m_attacks[2] = ActInitializeBeam;

#if (UNITY_EDITOR)
        string treePath = Application.dataPath + "/Code/BossBehaviours/";
#else
        string treePath = Application.dataPath + "/";
#endif

        m_bossTreeStages = new BehaviourNode[m_stageNames.Length];

        // Load stage behaviour trees.
        for (int i = 0; i < m_stageNames.Length; ++i)
        {
            m_bossTreeStages[i] = BTreeEditor.NodeData.LoadTree(treePath + m_stageNames[i], this);
        }

        m_attackRatings = new AttackRating[3];
        m_nAttackIndex = 0;
    }

    void Update()
    {
        if (CondIsIdleAnimation() == ENodeResult.NODE_FAILURE)
            m_animator.SetInteger("AttackID", 0);

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
            ProgressStage();
#endif

        // Reset attack descision making data.
        m_nAttackIndex = 0;

        for (int i = 0; i < m_attackRatings.Length; ++i)
        {
            m_attackRatings[i].m_nPrefScore = 0;
            m_attackRatings[i].m_bAvailable = false;
        }

        m_bossTreeStages[m_nStageIndex].Run();  

        m_fTimeSinceGlobalAttack -= Time.deltaTime;

        m_fPortalPunchCDTimer -= Time.deltaTime;
        m_fMeteorCDTimer -= Time.deltaTime;
        m_fBeamAttackCDTimer -= Time.deltaTime;
    }

    public void ProgressStage()
    {
        // Progress stage.
        ++m_nStageIndex;
    }

    public void BossDead()
    {
        //enable end portal
        Debug.Log("Boss Dead");
        gameObject.SetActive(false);
        end_Portal.SetActive(true);
    }

    // ----------------------------------------------------------------------------------------------
    // Conditions

    public ENodeResult CondPlayerGrounded()
    {
        if (m_playerController.IsGrounded())
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPlayerNotGrounded()
    {
        if (!m_playerController.IsGrounded())
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

    public ENodeResult CondMeteorAvailable()
    {
        if (m_fMeteorCDTimer <= 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondMeteorCD()
    {
        if (m_fMeteorCDTimer <= 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondBeamCD()
    {
        if (m_fBeamAttackCDTimer <= 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
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
    
        if (CondBeamCD() == ENodeResult.NODE_SUCCESS || m_fBeamTime > 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }
        else if (m_fBeamTime <= 0.0f)
        {
            // Beam attack is complete.

            // Disable beam effects.
            for (int i = 0; i < m_beamParticleRenderers.Length; ++i)
                m_beamParticleRenderers[i].enabled = false;
        }
    
        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondBeamNotActive()
    {
        if (m_fBeamTime > 0.0f)
        {
            return ENodeResult.NODE_FAILURE;
        }

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult CondNearEnergyPillar()
    {
        if(m_fBeamTime > 0.0f)
        {
            return ENodeResult.NODE_SUCCESS;
        }
        if (EnergyPillar.PlayerWithinVicinity() == true)
        {
            return ENodeResult.NODE_FAILURE;
        }
        
        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult CondBarrierBroken()
    {
        Debug.Log("Incomplete Node Function!");
        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPlayerNearCrystal()
    {
        Debug.Log("Incomplete Node Function!");
        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPlayerGrappling()
    {
        if (m_playerController.IsOverridden())
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPlayerSprinting()
    {
        if (m_playerController.IsSprinting())
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPortalNotActive()
    {
        if (!m_portalScript.IsActive())
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    // ----------------------------------------------------------------------------------------------
    // Actions

    public ENodeResult ActAddPrefPoint()
    {
        ++m_attackRatings[m_nAttackIndex].m_nPrefScore;

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActProgressAtckIndex()
    {
        ++m_nAttackIndex;

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActAddAvailable()
    {
        m_attackRatings[m_nAttackIndex].m_bAvailable = true;

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActChooseAttack()
    {
        int nHighestScore = -1;
        int nAvailableCount = 0;
        int m_nChosenAttackIndex = -1;

        for(int i = 0; i < m_attackRatings.Length; ++i)
        {
            AttackRating rating = m_attackRatings[i];

            // Skip if attack is unavailable.
            if (!rating.m_bAvailable)
                continue;

            ++nAvailableCount;

            // Set preferred attack if it has the greatest score.
            if (rating.m_nPrefScore > nHighestScore)
            {
                nHighestScore = rating.m_nPrefScore;

                m_nChosenAttackIndex = i;
            }
            else if(rating.m_nPrefScore == nHighestScore && Random.Range(0.0f, 100.0f) >= 50.0f)
            {
                // 50/50 chance to prefer the attack if it has an equal rating.
                m_nChosenAttackIndex = i;
            }
        }

        // Report result.
        if (m_nChosenAttackIndex > -1)
        {
            Debug.Log("Attack Index: " + m_nChosenAttackIndex + ", " + nAvailableCount + " Attacks available.");

            // Perform attack.
            m_attacks[1]();
        }
        else
            Debug.Log("No attacks available!");

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActPlayPortalPunchAnim()
    {
        Debug.Log("Portal Punch!");

        m_animator.SetInteger("AttackID", 1);

        // Set cooldown timers.
        m_fPortalPunchCDTimer = m_fPortalPunchCD;
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActPlayMeteorAnim()
    {
        Debug.Log("Meteor Attack!");

        // Set cooldown timers
        m_fMeteorCDTimer = m_fMeteorCD;
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        // Sets the animator to the meteor animation
        m_animator.SetInteger("AttackID", 2);

        // Play summon VFX
        if(m_meteorSummonEffect)
            m_meteorSummonEffect.Play();

        // Gets a random amount of meteors to spawn
        int nMeteorAmount = Random.Range(1, m_meteors.Length + 1);

        // Summon meteors.
        for(int i = 0; i < nMeteorAmount; ++i)
        {
            if (m_availableTargets.Count == 0)
                break;

            MeteorTarget newTarget = m_availableTargets.Dequeue();

            newTarget.SummonMeteor(m_meteors[i], transform.position + new Vector3(0.0f, 100.0f, 0.0f));
        }
        
        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActTrackPlayer()
    {
        Vector3 lookPos = m_player.transform.position - transform.position;
        lookPos.y = 0;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 2);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActInitializeBeam()
    {
        // Enable beam renderers.
        if (!m_bBeamActive)
        {
            for (int i = 0; i < m_beamParticleRenderers.Length; ++i)
                m_beamParticleRenderers[i].enabled = true;

            // Set timers and flag the beam as active.
            m_fBeamAttackCDTimer = m_fBeamAttackCD;
            m_fBeamTime = m_fBeamDuration;
            m_bBeamActive = true;
        }

        return ENodeResult.NODE_SUCCESS;
    }

    public Vector3 PointOnSphere(Vector3 v3Point, Vector3 v3SpherePos, float fSphereRadius)
    {
        Vector3 v3Dir = (v3Point - v3SpherePos).normalized;

        return v3SpherePos + (v3Dir * fSphereRadius);
    }

    /*
    Description: Allow beam raycasting and damage dealing to the player.
    Return Type: ENodeResult
    */
    public ENodeResult ActUseBeam()
    {
        // Ensure global attack cooldown remains for the duration of the beam attack.
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        Debug.Log("Beam!");

        Ray beamRay = new Ray(m_beamOrigin.position, m_v3BeamDirection);
        RaycastHit beamHit;

        // Rotate beam effects...
        m_beamOrigin.rotation = Quaternion.LookRotation(m_v3BeamDirection, Vector3.up);

        // Raycast to get hit information.
        if (Physics.SphereCast(beamRay, 0.2f, out beamHit, m_fBeamMaxRange, int.MaxValue, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(m_beamOrigin.position, m_v3BeamEnd, Color.white);

            PlayerBeam.UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_beamSegmentParticles, m_beamOrigin.position, beamHit.distance, 2.0f, true);

            if (beamHit.collider.gameObject == m_player)
            {
                m_playerStats.DealDamage(m_fBeamDPS * Time.deltaTime);
            }
        }
        else
        {
            PlayerBeam.UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_beamSegmentParticles, m_beamOrigin.position, m_nMaxBeamParticles, 2.0f, false);
        }

        return ENodeResult.NODE_SUCCESS;
    }

    // Track the beam's aim. Even if the beam is not in use.
    public ENodeResult ActBeamTrack()
    {
        float fSphereMag = (m_player.transform.position - m_beamOrigin.position).magnitude;

        Vector3 v3EndOnRadius = PointOnSphere(m_v3BeamEnd, m_beamOrigin.position, fSphereMag);

        if (m_beamParticleRenderers[0].enabled)
        {
            float fBeamProgress = 1.0f - (m_fBeamTime / m_fBeamDuration);

            float fTrackSpeed = m_fMinBeamTrackSpeed + (fBeamProgress * (m_fMaxBeamTrackSpeed - m_fMinBeamTrackSpeed));

            Vector3 v3PlayerDir = (m_player.transform.position - m_beamOrigin.transform.position).normalized;
            m_v3BeamDirection = (m_v3BeamEnd - m_beamOrigin.transform.position).normalized;

            // Keep beam within a tight cone of the player's position.
            if (Vector3.Dot(m_v3BeamDirection, v3PlayerDir) >= 0.95f)
            {
                m_v3BeamEnd = Vector3.MoveTowards(v3EndOnRadius, m_player.transform.position, fTrackSpeed * Time.deltaTime);
            }
            else
            {
                m_v3BeamEnd = Vector3.MoveTowards(v3EndOnRadius, m_player.transform.position, 500.0f * Time.deltaTime);
            }

        }
        else
        {
            m_v3BeamEnd = PointOnSphere(m_player.transform.position + new Vector3(10,0,0), m_beamOrigin.position, fSphereMag);
        }

        // Run beam attack effects when beam is enabled.
        if (m_bBeamActive)
        {
            ActUseBeam();

            // Count down beam timer and deactivate once expired.
            m_fBeamTime -= Time.deltaTime;

            if (m_fBeamTime <= 0.0f)
            {
                for (int i = 0; i < m_beamParticleRenderers.Length; ++i)
                    m_beamParticleRenderers[i].enabled = false;

                m_bBeamActive = false;
            }
        }

        return ENodeResult.NODE_SUCCESS;
    }

    // ----------------------------------------------------------------------------------------------
    // Misc

    public void ResetAnimToIdle()
    {
        m_animator.SetInteger("AttackID", 0);
    }

    public void AdvancePunchAnim()
    {
        m_animator.SetBool("PortalPunchComplete", true);
    }

    public void SetArmEnterStage()
    {
        if (m_portalScript.IsActive())
            m_portalScript.SetArmEnterStage();
    }

    public void SetArmExitStage()
    {
        if (m_portalScript.IsActive())
            m_portalScript.SetArmExitStage();
    }

    public void SetPortalCloseStage()
    {
        if (m_portalScript.IsActive())
            m_portalScript.SetPortalCloseStage();
    }

    public void SummonPortal()
    {
        // Do nothing if the portal is already active.
        if (m_portalScript.IsActive())
            return;

        // Get the player's flat forward vector.
        Vector3 v3PlayerForward = m_playerController.LookForward();
        Vector3 v3PlayerRight = m_playerController.LookRight();
            
        // Create random unit vector.
        Vector3 v3PortalOffset = v3PlayerForward;

        float fHorizontalOff = Random.Range(-1.0f, 1.0f);
        v3PortalOffset.x += v3PlayerRight.x * fHorizontalOff;
        v3PortalOffset.y += v3PlayerRight.y * fHorizontalOff;
        v3PortalOffset.z += v3PlayerRight.z * fHorizontalOff;

        v3PortalOffset += Vector3.up * Random.Range(0.5f, 1.0f);
        v3PortalOffset += v3PlayerRight * Random.Range(-1.0f, 1.0f);

        v3PortalOffset.Normalize();

        m_portal.transform.position = m_player.transform.position + m_playerController.GetVelocity() + (v3PortalOffset * 50.0f);

        Vector3 v3PlayerDir = (m_player.transform.position - m_portal.transform.position).normalized;
        m_portal.transform.rotation = Quaternion.LookRotation(-v3PortalOffset, Vector3.up);

        m_portal.SetActive(true);
        m_portalScript.SetPunchDirection(-v3PortalOffset);

        return;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(m_v3BeamEnd, 1.0f);
    }

}
