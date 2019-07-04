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

    [Tooltip("Distance in which the boss will attempt a slam attack.")]
    [SerializeField]
    private float m_fSlamDistance = 10.0f;

    private PlayerController m_playerController;
    private Animator m_animator;
    private CompositeNode m_bossTree;

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
    }

    // ----------------------------------------------------------------------------------------------
    // Conditions

    public ENodeResult CondWithinSlamDistance()
    {
        if ((m_player.transform.position - transform.position).sqrMagnitude <= m_fSlamDistance * m_fSlamDistance)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondPlayerGrounded()
    {
        if (m_playerController.IsGrounded())
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    // ----------------------------------------------------------------------------------------------
    // Actions

    public ENodeResult ActPlaySlamAnim()
    {
        if(m_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) // Only transition from idle.
            m_animator.SetInteger("AttackID", 1);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActPlayMeteorAnim()
    {
        if (m_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) // Only transition from idle.
            m_animator.SetInteger("AttackID", 2);

        return ENodeResult.NODE_SUCCESS;
    }

    // ----------------------------------------------------------------------------------------------
    // Misc

    public void ResetAnimToIdle()
    {
        m_animator.SetInteger("AttackID", 0);
    }
}
