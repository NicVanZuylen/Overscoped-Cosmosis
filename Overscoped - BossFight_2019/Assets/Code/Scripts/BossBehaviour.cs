using BehaviourTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(LineRenderer))]

public class BossBehaviour : MonoBehaviour
{
    [Header("References")]

    [Tooltip("Player object reference.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Meteor object reference.")]
    [SerializeField]
    private Meteor m_meteor = null;

    [Tooltip("Armour object reference.")]
    [SerializeField]
    private GameObject[] m_armorPeices = null;

    [Tooltip("Armor pulse material reference.")]
    [SerializeField]
    private Material m_armorMaterial = null;

    [SerializeField]
    private GameObject m_portal = null;

    [Tooltip("Origin point of the beam attack.")]
    [SerializeField]
    private Transform m_beamOrigin = null;

    [Header("Stages")]
    [SerializeField]
    private string[] m_stageNames = null;

    [Header("Attacks")]

    [Tooltip("Distance in which the boss will attempt a slam attack.")]
    [SerializeField]
    private float m_fSlamDistance = 10.0f;

    [Header("Meteor")]
    [Tooltip("Amount of time before meteor attack can be used again.")]
    [SerializeField]
    private float m_fMeteorCD = 10.0f;

    [Tooltip("Random chance of a meteor attack striking a random location.")]
    [SerializeField]
    private float m_fRandMeteorChance = 50.0f;

    [Header("Portal Punch")]
    [Tooltip("Amount of time before portal punch attack can be used again.")]
    [SerializeField]
    private float m_fPortalPunchCD = 4.0f;

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
    private float m_fTimeSinceGlobalAttack = 0.0f;
    private bool m_bIsStuck;

    // Stages
    private BehaviourNode[] m_bossTreeStages;
    private int m_nStageIndex;

    // Portal punch
    private Portal m_portalScript;
    private float m_fPortalPunchCDTimer;

    // Beam attack
    private LineRenderer m_beamLine;
    private Vector3 m_v3BeamEnd;
    private Vector3 m_v3BeamDirection;
    private EnergyPillar[] m_energyPillars;
    private float m_fBeamAttackCDTimer;
    private float m_fBeamTime;
    public float factor = 0.5f;
    public GameObject mesh;

    // Meteor attack
    private List<GameObject> m_availableMeteorSpawns;
    private float m_fMeteorCDTimer;
    private bool m_bRandomMeteor;

    // Armor
    private PullObject[] m_armorPullScripts;
    private Material[] m_armorMaterials;

    private static BoxCollider m_meteorSpawnVol;

    public string treePath;

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

        // Beam
        m_fBeamTime = m_fBeamDuration;
        m_v3BeamEnd = m_player.transform.position;

        // Meteor
        GameObject[] allMeteorSpawns = GameObject.FindGameObjectsWithTag("MeteorSpawn");

        m_availableMeteorSpawns = new List<GameObject>();

        // Add meteor spawns to spawn object pool.
        for (int i = 0; i < allMeteorSpawns.Length; ++i)
            m_availableMeteorSpawns.Add(allMeteorSpawns[i]);

        // Initialize meteor.
        m_meteor.Init(m_availableMeteorSpawns);

        m_portalScript = m_portal.GetComponent<Portal>();
        m_portal.SetActive(false);

        // Armor components
        m_armorPullScripts = new PullObject[m_armorPeices.Length];
        m_armorMaterials = new Material[m_armorPeices.Length];

        for(int i = 0; i < m_armorPeices.Length; ++i)
        {
            m_armorMaterials[i] = m_armorPeices[i].GetComponent<MeshRenderer>().material;

            m_armorPullScripts[i] = m_armorPeices[i].GetComponent<PullObject>();

            if (m_armorPullScripts[i] == null)
                Debug.LogError("Invalid armor object!");
        }

#if (UNITY_EDITOR)
        treePath = Application.dataPath + "/Code/BossBehaviours/";
#else
        treePath = Application.dataPath + "/";
#endif

        m_bossTreeStages = new BehaviourNode[m_stageNames.Length];

        // Load stage behaviour trees.
        for (int i = 0; i < m_stageNames.Length; ++i)
        {
            m_bossTreeStages[i] = BTreeEditor.NodeData.LoadTree(treePath + m_stageNames[i], this);
        }
    }

    void Update()
    {
        if (CondIsIdleAnimation() == ENodeResult.NODE_FAILURE)
            m_animator.SetInteger("AttackID", 0);

        // Stuck state debug.
        if (Input.GetKeyDown(KeyCode.G))
            EnterStuckState();

        if (Input.GetKeyDown(KeyCode.P))
            ProgressStage();

        m_bossTreeStages[m_nStageIndex].Run();

        m_fTimeSinceGlobalAttack -= Time.deltaTime;

        // Only reduce portal punch cooldown whilse the portal is not active.
        if(!m_portalScript.IsActive())
            m_fPortalPunchCDTimer -= Time.deltaTime;

        // Only reduce meteor attack cooldown when the meteor is not active.
        if(!m_meteor.gameObject.activeInHierarchy)
            m_fMeteorCDTimer -= Time.deltaTime;

        // Only reduce beam cooldown when not in use.
        if(!m_beamLine.enabled)
            m_fBeamAttackCDTimer -= Time.deltaTime;
    }

    public void ProgressStage()
    {
        // Exit stuck state.
        ExitStuckState();

        // Progress stage.
        ++m_nStageIndex;
    }

    public void EnterStuckState()
    {
        m_bIsStuck = true;

        // Reset to idle state.
        m_animator.SetBool("isStunned", true);

        // Disable beam & reset attack.
        m_beamLine.enabled = false;
        m_fBeamTime = 0.0f;
        m_animator.SetInteger("AttackID", 0);
        m_fTimeSinceGlobalAttack = 0.0f;

        for (int i = 0; i < m_armorPeices.Length; ++i)
        {
            // Enable armor material glow.
            m_armorMaterials[i].SetFloat("_FresnelOnOff", 1.0f);

            // Tag armor peices as pullable objects.
            m_armorPeices[i].tag = "PullObj";
        }

        // Start stuck timer.
        StartCoroutine(ResetStuck());
    }

    public void ExitStuckState()
    {
        m_bIsStuck = false;

        // Exit stuck state in animation controller.
        m_animator.SetBool("isStunned", false);

        for (int i = 0; i < m_armorPeices.Length; ++i)
        {
            // Disable armor material glow.
            m_armorMaterials[i].SetFloat("_FresnelOnOff", 0.0f);

            // Untag armor peices.
            m_armorPeices[i].tag = "Untagged";
        }
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

    public ENodeResult CondMeteorAvailable()
    {
        if (m_fMeteorCDTimer <= 0.0f && m_meteor.Available() && m_availableMeteorSpawns.Count > 0)
        {
            m_bRandomMeteor = Random.Range(0.0f, 100.0f) <= m_fRandMeteorChance;

            // Ensure the player is grounded or this is a random stike.
            if(m_playerController.IsGrounded() && m_meteorSpawnVol != null)
            {
                // Make stikes when the player is in a volume not random.
                m_bRandomMeteor = false;

                return ENodeResult.NODE_SUCCESS;
            }
            else if(m_bRandomMeteor)
            {
                return ENodeResult.NODE_SUCCESS;
            }
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondMeteorCD()
    {
        if (m_fMeteorCDTimer <= 0.0f)
        {
            m_fMeteorCDTimer = m_fMeteorCD;

            // Determine whether or not this is a random strike.
            //m_bRandomMeteor = true;


            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondBeamCD()
    {
        if (m_fBeamAttackCDTimer <= 0.0f)
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
    
        if (CondBeamCD() == ENodeResult.NODE_SUCCESS || m_fBeamTime > 0.0f)
        {
            SetPos(m_beamOrigin.position, m_beamOrigin.position + (m_v3BeamDirection * m_fBeamMaxRange));
            return ENodeResult.NODE_SUCCESS;
        }
        else if (m_fBeamTime <= 0.0f)
        {
            // Beam attack is complete.
            mesh.SetActive(false);
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
        m_fMeteorCDTimer = m_fMeteorCD;

        m_animator.SetInteger("AttackID", 2);
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        if(m_meteorSpawnVol != null && !m_bRandomMeteor)
        {
            Debug.Log("Meteor Attack!");

            m_availableMeteorSpawns.Remove(m_meteorSpawnVol.transform.parent.gameObject);

            // Calculate random spawn point and summon meteor.
            Vector3 v3RandomSpawn = m_meteorSpawnVol.transform.position;

            v3RandomSpawn.x += m_meteorSpawnVol.center.x + Random.Range(m_meteorSpawnVol.size.x * 0.5f, m_meteorSpawnVol.size.x * -0.5f);
            v3RandomSpawn.y += m_meteorSpawnVol.center.y + Random.Range(m_meteorSpawnVol.size.y * 0.5f, m_meteorSpawnVol.size.y * -0.5f);
            v3RandomSpawn.z += m_meteorSpawnVol.center.z + Random.Range(m_meteorSpawnVol.size.z * 0.5f, m_meteorSpawnVol.size.z * -0.5f);

            m_meteor.Summon(v3RandomSpawn, m_meteorSpawnVol.transform.parent.gameObject);
        }
        else if(m_bRandomMeteor)
        {
            Debug.Log("Random Meteor Attack!");

            if (m_availableMeteorSpawns.Count <= 0)
                return ENodeResult.NODE_SUCCESS;

            int nRandomIndex = Random.Range(0, m_availableMeteorSpawns.Count);

            // Pick random spawn point object.
            GameObject spawnObj = m_availableMeteorSpawns[nRandomIndex].transform.GetChild(0).gameObject;
            m_availableMeteorSpawns.RemoveAt(nRandomIndex);

            // Get the spawn volume.
            BoxCollider spawnBox = spawnObj.GetComponent<BoxCollider>();

            // Calculate random spawn point and summon meteor.
            Vector3 v3RandomSpawn = spawnObj.transform.position;

            v3RandomSpawn.x += spawnBox.center.x + Random.Range(spawnBox.size.x * 0.5f, spawnBox.size.x * -0.5f);
            v3RandomSpawn.y += spawnBox.center.y + Random.Range(spawnBox.size.y * 0.5f, spawnBox.size.y * -0.5f);
            v3RandomSpawn.z += spawnBox.center.z + Random.Range(spawnBox.size.z * 0.5f, spawnBox.size.z * -0.5f);

            m_meteor.Summon(v3RandomSpawn, spawnObj.transform.parent.gameObject);

            m_bRandomMeteor = false;
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

    public Vector3 PointOnSphere(Vector3 v3Point, Vector3 v3SpherePos, float fSphereRadius)
    {
        Vector3 v3Dir = (v3Point - v3SpherePos).normalized;

        return v3SpherePos + (v3Dir * fSphereRadius);
    }

    // Track the beam's aim. Even if the beam is not in use.
    public ENodeResult ActBeamTrack()
    {
        float fSphereMag = (m_player.transform.position - m_beamOrigin.position).magnitude;

        Vector3 v3EndOnRadius = PointOnSphere(m_v3BeamEnd, m_beamOrigin.position, fSphereMag);

        if (mesh.activeInHierarchy)
        {
            float fBeamProgress = 1.0f - (m_fBeamTime / m_fBeamDuration);

            float fTrackSpeed = m_fMinBeamTrackSpeed + (fBeamProgress * (m_fMaxBeamTrackSpeed - m_fMinBeamTrackSpeed));

            Vector3 v3PlayerDir = (m_player.transform.position - m_beamOrigin.transform.position).normalized;
            m_v3BeamDirection = (m_v3BeamEnd - m_beamOrigin.transform.position).normalized;

            // Keep beam within a tight cone of the player's position.
            if (Vector3.Dot(m_v3BeamDirection, v3PlayerDir) >= 0.95f)
                m_v3BeamEnd = Vector3.MoveTowards(v3EndOnRadius, m_player.transform.position, fTrackSpeed * Time.deltaTime);
            else
            {
                m_v3BeamEnd = Vector3.MoveTowards(v3EndOnRadius, m_player.transform.position, 500.0f * Time.deltaTime);
            }

        }
        else
            m_v3BeamEnd = PointOnSphere(m_player.transform.position + new Vector3(10,0,0), m_beamOrigin.position, fSphereMag);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActUseBeam()
    {
        // Enable beam if it is disabled.
        if (!mesh.activeInHierarchy)
        {
            mesh.SetActive(false);
        }

        // Look at player.
        ActTrackPlayer();

        // Linerenderer points.
        Vector3[] beamLinePoints = new Vector3[2];
        beamLinePoints[0] = m_beamOrigin.position;
        beamLinePoints[1] = m_beamOrigin.position + (m_v3BeamDirection * m_fBeamMaxRange);

        Ray beamRay = new Ray(m_beamOrigin.position, m_v3BeamDirection);
        RaycastHit beamHit;
        if(Physics.SphereCast(beamRay, 0.2f, out beamHit, m_fBeamMaxRange, int.MaxValue, QueryTriggerInteraction.Ignore))
        {
            if (beamHit.collider.gameObject == m_player)
            {
                m_playerStats.DealDamage(m_fBeamDPS * Time.deltaTime);
            }
            else if (beamHit.collider.GetComponent<EnergyPillar>())
            {
                beamHit.collider.GetComponent<EnergyPillar>().Charge(this.transform.GetComponent<BossBehaviour>());
            }
            else
                beamLinePoints[1] = beamHit.point;
        }

        //m_beamLine.SetPositions(beamLinePoints);

        return ENodeResult.NODE_SUCCESS;
    }

    // ----------------------------------------------------------------------------------------------
    // Misc

    /*
    Description: Set the meteor spawn point for the meteor attack.
    Param:
        GameObject spawner: The spawner gameobject to use.
    */
    public static void SetMeteorSpawn(BoxCollider spawner)
    {
        m_meteorSpawnVol = spawner;
    }

    public void ResetAnimToIdle()
    {
        m_animator.SetInteger("AttackID", 0);
    }

    IEnumerator ResetStuck()
    {
        yield return new WaitForSeconds(m_fStuckTime);
        ExitStuckState();
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

        m_portal.SetActive(true);
        m_portal.transform.position = m_player.transform.position + m_playerController.GetVelocity() + (v3PortalOffset * 50.0f);

        Vector3 v3PlayerDir = (m_player.transform.position - m_portal.transform.position).normalized;
        m_portal.transform.rotation = Quaternion.LookRotation(-v3PortalOffset, Vector3.up);

        m_portalScript.SetPunchDirection(-v3PortalOffset);

        return;
    }

    void SetPos(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        Vector3 mid = (dir) / 2.0f + start;
        mesh.transform.position = mid;
        mesh.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        Vector3 scale = mesh.transform.localScale;
        scale.y = dir.magnitude * factor;
        mesh.transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(m_v3BeamEnd, 1.0f);
    }

}
