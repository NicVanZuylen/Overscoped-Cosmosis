using BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

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

    [Tooltip("VFX object played at the beam origin point.")]
    [SerializeField]
    private ParticleObject m_beamOriginVFX = new ParticleObject();

    [Tooltip("VFX object played at the beam origin point.")]
    [SerializeField]
    private ParticleObject m_beamDestinationVFX = new ParticleObject();

    [Tooltip("Spawn GPU particlesystem.")]
    [SerializeField]
    private VisualEffect m_spawnVFX = null;

    [Tooltip("Spawn/Death dissolve material")]
    [SerializeField]
    private Material m_dissolveMat = null;

    // -------------------------------------------------------------------------------------------------
    [Header("SpawnSFX")]

    [SerializeField]
    private AudioSelection m_spawnSFX = new AudioSelection();

    [Header("Hit SFX")]

    [SerializeField]
    private AudioSelection m_bossHitSFX = new AudioSelection();

    [SerializeField]
    private AudioSelection m_bossDeathSFX = new AudioSelection();

    // -------------------------------------------------------------------------------------------------
    [Header("Attack Noises SFX")]

    [SerializeField]
    private AudioSelection m_meteorVoiceSelection = new AudioSelection();

    [SerializeField]
    private AudioSelection m_beamVoiceSelection = new AudioSelection();

    [SerializeField]
    private AudioSelection m_punchVoiceSelection = new AudioSelection();

    // -------------------------------------------------------------------------------------------------
    [Header("Meteor SFX")]
    [SerializeField]
    private AudioClip m_meteorSummonSFX = null;

    [SerializeField]
    private AudioClip m_portalAmbientsSFX = null;

    private AudioLoop m_portalAmbientsAudioLoop;

    [Header("Beam SFX")]
    [SerializeField]
    private AudioClip m_beamChargeSFX = null;

    [SerializeField]                         
    private AudioClip m_beamFireLoopingSFX = null;  
                                             
    [SerializeField]                         
    private AudioClip m_beamImpactLoopingSFX = null;

    private AudioLoop m_beamFireAudioLoop;

    private AudioLoop m_beamImpactAudioLoop;

    [SerializeField]
    private AudioSource m_SFXSource = null;

    // -------------------------------------------------------------------------------------------------
    [Header("Misc")]

    [Tooltip("Minimum delay before any kind of attack is performed.")]
    [SerializeField]
    private float m_fTimeBetweenAttacks = 5.0f;

    [Tooltip("Length of the boss death effects.")]
    [SerializeField]
    private float m_fDeathTime = 5.0f;

    // Global
    private PlayerController m_playerController;
    private PlayerStats m_playerStats;
    private Transform m_cameraTransform;
    private GrappleHook m_grappleScript;
    private Animator m_animator;
    private GameObject m_endPortal;
    private float m_fTimeSinceGlobalAttack = 0.0f;
    private float m_fTimeSinceHit; // Time since the boss was hit by the beam.
    private float m_fSpawnTime;
    private bool m_bUnderAttack;

    // State delegate.
    public delegate void StateFunc();
    private StateFunc m_state;

    // Stages
    private BehaviourNode[] m_bossTreeStages;
    private int m_nStageIndex;

    // Spawn/Death
    private float m_fCurrentSpawnTime;
    private float m_fCurrentDeathTime;

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
    private AudioSelection[] m_attackVoices;
    [SerializeField]
    private float[] m_fAttackDelays;
    private float m_fCurrentAttackDelay;
    private bool m_bAttackPending;

    // Audio
    private static float m_fBossVolume = 10.0f;

    void Awake()
    {
        m_playerController = m_player.GetComponent<PlayerController>();
        m_playerStats = m_player.GetComponent<PlayerStats>();
        m_cameraTransform = m_player.GetComponentInChildren<Camera>().transform;
        m_grappleScript = m_player.GetComponent<GrappleHook>();
        m_animator = GetComponent<Animator>();
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        m_endPortal = GameObject.FindGameObjectWithTag("EndPortal");
        m_endPortal.SetActive(false);

        // Initial behaviour state.
        m_state = SpawnState;

        // Initial attack cooldowns.
        m_fPortalPunchCDTimer = m_fPortalPunchCD;
        m_fMeteorCDTimer = m_fMeteorCD;
        m_fBeamAttackCDTimer = m_fBeamAttackCD;

        // Beam
        m_fBeamTime = m_fBeamDuration;
        m_v3BeamEnd = m_player.transform.position;

        // VFX

        // Spawn/Death
        m_dissolveMat.SetFloat("_Dissolve", 0.0f);
        m_animator.SetBool("Spawned", false);

        m_fSpawnTime = m_spawnVFX.GetFloat("Lifetime Max");
        m_fCurrentSpawnTime = m_fSpawnTime;

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

        // Attack functions...
        m_attacks = new AttackFunc[3];
        m_attacks[0] = ActPlayMeteorAnim;
        m_attacks[1] = ActPlayPortalPunchAnim;
        m_attacks[2] = ActChargeBeam;

        m_attackVoices = new AudioSelection[3];
        m_attackVoices[0] = m_meteorVoiceSelection;
        m_attackVoices[1] = m_punchVoiceSelection;
        m_attackVoices[2] = m_beamVoiceSelection;

        //m_fAttackDelays = new float[3];
        //m_fAttackDelays[0] = 1.0f;
        //m_fAttackDelays[1] = 2.0f;
        //m_fAttackDelays[2] = 1.0f;
        m_fCurrentAttackDelay = 0.0f;
        m_bAttackPending = false;

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

        m_beamFireAudioLoop = new AudioLoop(m_beamFireLoopingSFX, gameObject, ESpacialMode.AUDIO_SPACE_NONE);

        m_portalAmbientsAudioLoop = new AudioLoop(m_portalAmbientsSFX, m_portal, ESpacialMode.AUDIO_SPACE_NONE);

        m_beamImpactAudioLoop = new AudioLoop(m_beamImpactLoopingSFX, m_beamDestinationVFX.m_particleSystems[0].gameObject, ESpacialMode.AUDIO_SPACE_WORLD);
    }

    public void SpawnState()
    {
        if (m_fCurrentSpawnTime > 0.0f)
        {
            // Find and set dissolve shader value...
            m_fCurrentSpawnTime = Mathf.Max(m_fCurrentSpawnTime - Time.deltaTime, 0.0f);
            float fDissolveLevel = Mathf.Min((1.0f - (m_fCurrentSpawnTime / m_fSpawnTime) * 2.0f), 1.0f);

            m_dissolveMat.SetFloat("_Dissolve", Mathf.Max(fDissolveLevel, 0.0f));

            // Begin spawn animation when dissolve level reaches zero.
            if (fDissolveLevel > 0.0f)
                m_animator.SetBool("Spawned", true);

            if(fDissolveLevel > -0.5f)
            {
                // Play spawn voice line.
                m_spawnSFX.PlayRandom(m_fBossVolume);
            }
        }
        
        // The idle animation state before the spawn animation is a different node, we only want to know if he is in the post-spawn idle state.
        if(CondIsIdleAnimation() == ENodeResult.NODE_SUCCESS) // Change state once spawn animation is complete.
            m_state = AliveState;
    }

    public void AliveState()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
            ProgressStage();

        if (Input.GetKeyDown(KeyCode.B))
            KillBoss();
#endif

        // Reset attack descision making data.
        m_nAttackIndex = 0;

        for (int i = 0; i < m_attackRatings.Length; ++i)
        {
            m_attackRatings[i].m_nPrefScore = 0;
            m_attackRatings[i].m_bAvailable = false;
        }

        if (!m_bUnderAttack) // Don't run AI when being hit.
            m_bossTreeStages[m_nStageIndex].Run();
        else
        {
            // Hit recovery...

            m_fTimeSinceHit += Time.deltaTime;

            if (m_fTimeSinceHit >= 0.5f)
                RecoverFromHit();
        }

        // Perform attack after delay.
        if (m_bAttackPending && m_fCurrentAttackDelay <= 0.0f)
        {
            m_attacks[m_nChosenAttackIndex]();

            m_bAttackPending = false;
        }

        ActBeamTrack();

        // Count down audio cooldowns.
        m_bossHitSFX.CountCooldown();

        // Count down attack cooldowns...
        m_fTimeSinceGlobalAttack -= Time.deltaTime;
        m_fCurrentAttackDelay -= Time.deltaTime;

        m_fPortalPunchCDTimer -= Time.deltaTime;
        m_fMeteorCDTimer -= Time.deltaTime;
        m_fBeamAttackCDTimer -= Time.deltaTime;
    }

    public void DeathState()
    {
        // Death dissolve effect.
        if (m_fCurrentDeathTime > 0.0f)
        {
            m_fCurrentDeathTime -= Time.deltaTime;

            float fDissolveLevel = m_fCurrentDeathTime / m_fDeathTime;

            m_dissolveMat.SetFloat("_Dissolve", fDissolveLevel);
        }
        else // Disable script once death state is complete.
            enabled = false;
    }

    void Update()
    {
        // Run current state function.
        m_state();
    }

    /*
    Description: Progress the boss AI stage.
    */
    public void ProgressStage()
    {
        // Progress stage.
        ++m_nStageIndex;

        // Recover from beam attack.
        RecoverFromHit();
    }

    /*
    Description: Cancel attacks and play the hit animation.
    */
    public void TakeHit()
    {
        m_fTimeSinceHit = 0.0f;
        m_bUnderAttack = true;

        m_animator.SetInteger("AttackID", 0);
        m_animator.SetBool("UnderAttack", true);
        m_animator.SetBool("PortalPunchComplete", true);

        m_bossHitSFX.PlayRandom(m_fBossVolume);

        DeactivateBeam();
        m_portalScript.SetPortalCloseStage();
    }

    /*
    Description: Stop hit animation and return normal AI.
    */
    public void RecoverFromHit()
    {
        m_bUnderAttack = false;

        m_animator.SetBool("UnderAttack", false);
    }

    /*
    Description: Get whether or not the boss is currently being struck by an attack.
    Return Type: bool
    */
    public bool IsUnderAttack()
    {
        return m_bUnderAttack;
    }

    /*
    Description: Boss death event function.
    */
    public void KillBoss()
    {
        // Enable end portal
        m_endPortal.SetActive(true);

        // Set state.
        m_state = DeathState;

        // Play animation.
        m_animator.SetBool("IsDead", true);

        // Activate death state.
        m_fCurrentDeathTime = m_fDeathTime;

        // Play voice line.
        m_bossDeathSFX.PlayRandom(m_fBossVolume);

        // Cancel attacks.
        m_animator.SetInteger("AttackID", 0);
        m_animator.SetBool("PortalPunchComplete", true);

        DeactivateBeam();
        m_portalScript.SetPortalCloseStage();
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
        if (m_fTimeSinceGlobalAttack > 0.0f && m_bAttackPending)
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
        if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") && m_animator.GetInteger("AttackID") == 0)
            return ENodeResult.NODE_SUCCESS;

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

    public ENodeResult CondBarrierBroken()
    {
        if (m_nStageIndex > 0) // Stage 2 is active while barrier is broken.
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPlayerNearCrystal()
    {
        if(m_playerStats.NearPickup())
        {
            return ENodeResult.NODE_SUCCESS;
        }

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

    public ENodeResult CondAttackNotPending()
    {
        if (!m_bAttackPending)
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
        m_nChosenAttackIndex = -1;

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

                Debug.Log("Score " + i + ":" + rating.m_nPrefScore);

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

            if(!m_bAttackPending)
            {
                // Set delay and flag as pending attack.
                m_fCurrentAttackDelay = m_fAttackDelays[m_nChosenAttackIndex];
                m_bAttackPending = true;

                // Play voice line.
                m_attackVoices[m_nChosenAttackIndex].PlayRandom(m_fBossVolume);
            }
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

        m_animator.SetInteger("AttackID", 2);

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

    public ENodeResult ActChargeBeam()
    {
        m_animator.SetInteger("AttackID", 3);

        if (m_beamChargeSFX)
            m_SFXSource.PlayOneShot(m_beamChargeSFX, m_fBossVolume);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActInitializeBeam()
    {
        Debug.Log("Beam!");

        // Enable beam renderers.
        if (!m_bBeamActive)
        {
            for (int i = 0; i < m_beamParticleRenderers.Length; ++i)
                m_beamParticleRenderers[i].enabled = true;

            // Set timers and flag the beam as active.
            m_fBeamAttackCDTimer = m_fBeamAttackCD;
            m_fBeamTime = m_fBeamDuration;
            m_bBeamActive = true;

            // Play VFX...
            m_beamOriginVFX.Play();
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
        // Ensure maintain cooldowns while the beam is active.
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;
        m_fBeamAttackCDTimer = m_fBeamAttackCD;

        Ray beamRay = new Ray(m_beamOrigin.position, m_v3BeamDirection);
        RaycastHit beamHit;

        // Rotate beam effects...
        m_beamOrigin.rotation = Quaternion.LookRotation(m_v3BeamDirection, Vector3.up);

        if (!m_beamFireAudioLoop.IsPlaying())
            m_beamFireAudioLoop.Play(m_fBossVolume);

        if (!m_beamImpactAudioLoop.IsPlaying())
            m_beamImpactAudioLoop.Play();

        // Raycast to get hit information.
        if (Physics.SphereCast(beamRay, 0.2f, out beamHit, m_fBeamMaxRange, int.MaxValue, QueryTriggerInteraction.Ignore))
        {
            PlayerBeam.UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_beamSegmentParticles, m_beamOrigin.position, beamHit.distance, 10.0f, true);

            if (beamHit.collider.gameObject == m_player)
            {
                m_playerStats.DealDamage(m_fBeamDPS * Time.deltaTime);
            }

            // Ensure impact VFX are playing and set position.
            if (!m_beamDestinationVFX.IsPlaying())
                m_beamDestinationVFX.Play();

            m_beamDestinationVFX.SetPosition(beamHit.point);
        }
        else
        {
            // Stop VFX if the beam no longer is impacting anything.
            if (m_beamDestinationVFX.IsPlaying())
                m_beamDestinationVFX.Stop();

            PlayerBeam.UpdateParticlePositions(m_beamParticles, m_beamParticleRenderers, m_beamSegmentParticles, m_beamOrigin.position, m_nMaxBeamParticles, 10.0f, false);
        }

        return ENodeResult.NODE_SUCCESS;
    }

    /*
    Description: Deactivate the beam attack.
    */
    public void DeactivateBeam()
    {
        for (int i = 0; i < m_beamParticleRenderers.Length; ++i)
            m_beamParticleRenderers[i].enabled = false;

        // Set end point to start.
        m_v3BeamEnd = m_beamOrigin.position;

        if (m_beamFireAudioLoop.IsPlaying())
            m_beamFireAudioLoop.Stop();

        if (m_beamImpactAudioLoop.IsPlaying())
            m_beamImpactAudioLoop.Stop();

        // Stop VFX.
        m_beamOriginVFX.Stop();

        if(m_beamDestinationVFX.IsPlaying())
            m_beamDestinationVFX.Stop();

        ResetAnimToIdle();
        m_bBeamActive = false;
    }

    /*
    Description: Track the beam's aim. Even if the beam is not in use.
    */
    public ENodeResult ActBeamTrack()
    {
        float fSphereMag = (m_player.transform.position - m_beamOrigin.position).magnitude;

        Vector3 v3EndOnRadius = PointOnSphere(m_v3BeamEnd, m_beamOrigin.position, fSphereMag);

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

        // Run beam attack effects when beam is enabled.
        if (m_bBeamActive)
        {
            ActUseBeam();

            // Count down beam timer and deactivate once expired.
            m_fBeamTime -= Time.deltaTime;

            if (m_fBeamTime <= 0.0f)
            {
                DeactivateBeam();
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

    public void EvAdvancePunchAnim()
    {
        m_animator.SetBool("PortalPunchComplete", true);
    }

    public void EvActivateBeam()
    {
        ActInitializeBeam();
    }

    public void EvSetArmEnterStage()
    {
        if (m_portalScript.IsActive())
            m_portalScript.SetArmEnterStage();
    }

    public void EvSetArmExitStage()
    {
        if (m_portalScript.IsActive())
            m_portalScript.SetArmExitStage();
    }

    public void EvSetPortalCloseStage()
    {
        ResetAnimToIdle();

        if (m_portalScript.IsActive())
        {
            m_portalScript.SetPortalCloseStage();

            if (m_portalAmbientsAudioLoop.IsPlaying())
                m_portalAmbientsAudioLoop.Stop();
        }
    }

    public void EvSummonPortal()
    {
        // Get the player's flat forward vector.
        Vector3 v3PlayerForward = m_cameraTransform.forward;
        Vector3 v3PlayerRight = m_cameraTransform.right;
            
        // Create random unit vector.
        Vector3 v3PortalOffset = v3PlayerForward;
    
        v3PortalOffset += v3PlayerRight * Random.Range(-0.2f, 0.2f);

        v3PortalOffset.Normalize();

        // Position the portal at the potential player position with a random offset.
        Vector3 v3PlayerTrackPos = m_player.transform.position + (m_playerController.GetVelocity() * m_portalScript.OpenTime());
        m_portal.transform.position = v3PlayerTrackPos + (v3PortalOffset * 50.0f);

        // Look at predicted player location.
        m_portal.transform.rotation = Quaternion.LookRotation(-v3PortalOffset, Vector3.up);

        m_portal.SetActive(true);
        m_portalScript.SetPunchDirection(-v3PortalOffset);
        m_portalScript.Activate();

        m_portalAmbientsAudioLoop.GetSource().spatialBlend = 1;
        m_portalAmbientsAudioLoop.GetSource().minDistance = 50;

        if (!m_portalAmbientsAudioLoop.IsPlaying())
            m_portalAmbientsAudioLoop.Play(m_fBossVolume);
            
        return;
    }

    public void EvSummonMeteors()
    {
        // Set cooldown timers
        m_fMeteorCDTimer = m_fMeteorCD;
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        // Play summon VFX
        if (m_meteorSummonEffect)
            m_meteorSummonEffect.Play();

        // Gets a random amount of meteors to spawn
        int nMeteorAmount = Random.Range(1, m_meteors.Length + 1);

        // Summon meteors.
        for (int i = 0; i < nMeteorAmount; ++i)
        {
            if (m_availableTargets.Count == 0)
                break;

            MeteorTarget newTarget = m_availableTargets.Dequeue();

            if(m_meteorSummonSFX)
                AudioSource.PlayClipAtPoint(m_meteorSummonSFX, newTarget.transform.GetChild(0).position);

            newTarget.SummonMeteor(m_meteors[i], newTarget.transform.GetChild(0).position);
        }
    }

    public void EvMeteorVoice()
    {
        //m_meteorVoiceSelection.PlayRandom();
    }

    public void EvBeamVoice()
    {
        //m_beamVoiceSelection.PlayRandom();
    }

    public void EvPunchVoice()
    {
        //m_punchVoiceSelection.PlayRandom();
    }

    public static void SetVolume(float fVolume, float master)
    {
        m_fBossVolume = fVolume * master;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(m_v3BeamEnd, 1.0f);
    }
}
