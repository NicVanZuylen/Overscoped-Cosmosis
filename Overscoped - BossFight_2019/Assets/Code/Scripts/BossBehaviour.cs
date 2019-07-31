using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTree;

[RequireComponent(typeof(Animator))]

public class BossBehaviour : MonoBehaviour
{
    [Tooltip("Player object reference.")]
    [SerializeField]
    private GameObject m_player = null;

    [Tooltip("Meteor object reference.")]
    [SerializeField]
    private GameObject m_meteor = null;

    [Tooltip("Armour object reference.")]
    [SerializeField]
    private GameObject m_armour = null;

    [SerializeField]
    private GameObject m_portal = null;
    //private List<GameObject> m_portalSpawns;

    [Tooltip("Distance in which the boss will attempt a slam attack.")]
    [SerializeField]
    private float m_fSlamDistance = 10.0f;

    public bool m_bIsStuck;

    [Tooltip("Amount of time before meteor attack can be used again.")]
    [SerializeField]
    private float m_fMeteorCD = 10f;
    private float m_fMeteorCDTimer;

    [Tooltip("Amount of time before portal punch attack can be used again.")]
    [SerializeField]
    private float m_fPortalPunchCD = 4f;
    private float m_fPortalPunchCDTimer;

    [Tooltip("Amount of time spent stuck.")]
    [SerializeField]
    private float m_fStuckTime;
    
    private float m_fTimeSinceGlobalAttack;
    [SerializeField]
    private float m_fTimeBetweenAttacks;

    [Tooltip("The length of the boss arm, used for portal punching.")]
    private float m_fArmLength = 10.0f;

    public Vector3 m_vSize;

    private PlayerController m_playerController;
    private Animator m_animator;
    private CompositeNode m_bossTree;

    private Portal m_portalScript;

    void Awake()
    {
        m_playerController = m_player.GetComponent<PlayerController>();
        m_animator = GetComponent<Animator>();
        m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

        m_portalScript = m_portal.GetComponent<Portal>();
        m_portal.SetActive(false);

        string treePath = Application.dataPath + "/Code/BossBehaviours/BossTreePhase1.xml";
        m_bossTree = BTreeEditor.BTreeEditor.LoadTree(treePath, this);
    }

    void Update()
    {
        ResetAnimToIdle();
        m_bossTree.Run();

        m_fTimeSinceGlobalAttack -= Time.deltaTime;

        m_fMeteorCDTimer -= Time.deltaTime;
        m_fPortalPunchCDTimer -= Time.deltaTime;
    }

    // ----------------------------------------------------------------------------------------------
    // Conditions

    public ENodeResult CondPlayerGrounded()
    {
        if (m_playerController.IsGrounded())
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondTimeSinceGlobalAttack()
    {
        if(m_fTimeSinceGlobalAttack <= 0)
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
        if (m_fPortalPunchCDTimer <= 0)
        {
            return ENodeResult.NODE_SUCCESS;
        }
        return ENodeResult.NODE_FAILURE;
    }

    // Meteor
    public ENodeResult CondMeteorCD()
    {
        if (m_fMeteorCDTimer <= 0)
        {
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


    // ----------------------------------------------------------------------------------------------
    // Actions

    public ENodeResult ActPlayPortalPunchAnim()
    {
        if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) // Only transition from idle.
        {
            m_animator.SetInteger("AttackID", 1);
            m_fPortalPunchCDTimer = m_fPortalPunchCD;
            m_fTimeSinceGlobalAttack = m_fTimeBetweenAttacks;

            if(!m_portalScript.IsActive())
                SummonPortal();

            return ENodeResult.NODE_SUCCESS;
        }
        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult ActPlayMeteorAnim()
    {
        if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) // Only transition from idle.
        {
            m_animator.SetInteger("AttackID", 2);
            m_fMeteorCDTimer = m_fMeteorCD;
            Vector3 pos = new Vector3(transform.position.x, transform.position.y + 100, transform.position.z) + new Vector3(Random.Range(-m_vSize.x / 2, m_vSize.x / 2), Random.Range(-m_vSize.y / 2, m_vSize.y / 2), Random.Range(-m_vSize.z / 2, m_vSize.z / 2));
            Instantiate(m_meteor, pos, m_meteor.transform.rotation);
        }
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
        Gizmos.DrawCube(new Vector3(transform.position.x, transform.position.y + 100, transform.position.z), m_vSize);
    }

    private void SummonPortal()
    {
        Vector3 v3PortalOffset = Vector3.up * m_fArmLength;
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
    }
}
