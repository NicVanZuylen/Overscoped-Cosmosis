using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTree;

public class BossBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject m_player = null;
    private CompositeNode m_behaviourRoot;
    private Animator m_animator;

    public float m_fMeleeRange;
    public float m_fThrowRange;
    public float m_fMoveSpeed;

    // Use this for initialization
    void Awake ()
    {
        m_behaviourRoot = BTreeEditor.BTreeEditor.LoadTree(Application.dataPath + "/BossTree.xml", this);

        m_animator = GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        m_animator.enabled = false;
        m_animator.SetBool("swinging", false);
        m_animator.SetBool("throwing", false);

        if (m_behaviourRoot.Run() == ENodeResult.NODE_FAILURE)
        {
            Debug.Log("Fail!");
        }
        else
            m_animator.enabled = true;
    }

    public ENodeResult CondPlayerNotBehind()
    {
        // Get direction to player and convert to Bector2.
        Vector3 v3PlayerDir = m_player.transform.position - transform.position;
        Vector2 v2PlayerDir = new Vector2(v3PlayerDir.x, v3PlayerDir.z);
        v2PlayerDir.Normalize();

        // Get Vector2 look direction.
        Vector2 v2LookDir = new Vector2(transform.forward.x, transform.forward.z);

        // Dot product.
        if (Vector2.Dot(v2LookDir, v2PlayerDir) > 0.97f)
        {
            return ENodeResult.NODE_SUCCESS;
        }

        Debug.Log("Player is behind the boss!");

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondWithinMeleeRange()
    {
        // Get distance to player...
        float fDistToPlayer = (m_player.transform.position - transform.position).magnitude;

        if (fDistToPlayer <= m_fMeleeRange)
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondOutOfMeleeRange()
    {
        if (CondWithinMeleeRange() == ENodeResult.NODE_FAILURE)
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult CondWithinThrowRange()
    {
        // Get distance to player...
        float fDistToPlayer = (m_player.transform.position - transform.position).magnitude;

        if (fDistToPlayer <= m_fThrowRange)
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }
    public ENodeResult CondOutOfThrowRange()
    {
        if (CondWithinThrowRange() == ENodeResult.NODE_FAILURE)
            return ENodeResult.NODE_SUCCESS;

        return ENodeResult.NODE_FAILURE;
    }

    public ENodeResult ActMeleeAttack()
    {
        m_animator.SetBool("swinging", true);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActThrowAttack()
    {
        m_animator.SetBool("throwing", true);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActFacePlayer()
    {
        Vector3 v3DirToPlayer = m_player.transform.position - transform.position;
        v3DirToPlayer.y = 0.0f;
        Quaternion targetRot = Quaternion.LookRotation(v3DirToPlayer, Vector3.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.01f);

        return ENodeResult.NODE_SUCCESS;
    }

    public ENodeResult ActMoveCloser()
    {
        // Get normalized direction vector to the player.
        Vector3 v3DirToPlayer = m_player.transform.position - transform.position;
        v3DirToPlayer.Normalize();
        v3DirToPlayer.y = 0.0f;

        // Translate close to the player.
        transform.position = transform.position + (v3DirToPlayer * m_fMoveSpeed * Time.deltaTime);

        return ENodeResult.NODE_SUCCESS;
    }
}
