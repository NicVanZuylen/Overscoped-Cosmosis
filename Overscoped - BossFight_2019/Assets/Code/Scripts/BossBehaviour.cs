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
    private GameObject m_meteor;

    [Tooltip("Armour object reference.")]
    [SerializeField]
    private GameObject m_armour;

    [Tooltip("Distance in which the boss will attempt a slam attack.")]
    [SerializeField]
    private float m_fSlamDistance = 10.0f;

    public bool m_bIsStuck;

    [Tooltip("Amount of time before meteor attack can be used again.")]
    [SerializeField]
    private float m_fMeteorCD = 10f;
    private float m_fMeteorCDTimer;

    [Tooltip("Amount of time before slam attack can be used again.")]
    [SerializeField]
    private float m_fSlamCD = 4f;
    private float m_fSlamCDTimer;

    [Tooltip("Amount of time spent stuck.")]
    [SerializeField]
    private float m_fStuckTime;

    public Vector3 m_vSize;

    private PlayerController m_playerController;
    private Animator m_animator;
    private BehaviourNode m_bossTree;

    void Awake()
    {
        m_playerController = m_player.GetComponent<PlayerController>();
        m_animator = GetComponent<Animator>();

        string treePath = Application.dataPath + "/Code/BossBehaviours/BossTree.xml";
        m_bossTree = BTreeEditor.BTreeEditor.LoadTree(treePath, this);
    }

    void Update()
    {
        ResetAnimToIdle();
        m_bossTree.Run();

        m_fMeteorCDTimer -= Time.deltaTime;
        m_fSlamCDTimer -= Time.deltaTime;
    }

    // ----------------------------------------------------------------------------------------------
    // Conditions

    public ENodeResult CondPlayerGrounded()
    {
        if (m_playerController.IsGrounded())
            return ENodeResult.NODE_SUCCESS;

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
        if (m_fSlamCDTimer <= 0)
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

    public ENodeResult ActPlaySlamAnim()
    {
        if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) // Only transition from idle.
        {
            m_animator.SetInteger("AttackID", 1);
            m_fSlamCDTimer = m_fSlamCD;
        }
        return ENodeResult.NODE_SUCCESS;
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
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(new Vector3(transform.position.x, transform.position.y + 100, transform.position.z), m_vSize);
    }
}
