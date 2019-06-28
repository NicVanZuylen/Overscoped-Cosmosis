using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawn : MonoBehaviour
{
    public CharacterController m_CharacterController;
    public PlayerController m_PlayerController;
    public GameObject m_RespawnNode;
    public RaycastHit m_GroundCheck;
    Vector3 m_RespawnOffset;
    GameObject m_GroundedObject;

    public float m_GizmoRadius = 0.2f;
    public float m_RespawnHeight = 10;
    public bool m_IsGrounded = false;
    public bool m_IsDead = false;
    float m_DistanceToGround;

    void Start()
    {
        m_CharacterController = gameObject.GetComponent<CharacterController>();
        m_PlayerController = gameObject.GetComponent<PlayerController>();
        m_RespawnOffset = new Vector3(0, m_RespawnHeight, 0);

        m_DistanceToGround = m_CharacterController.height * 0.5f;
    }

    void Update()
    {
        if (Physics.Raycast(m_CharacterController.transform.position, -Vector3.up, out m_GroundCheck, m_DistanceToGround + 0.1f))
        {
            if (m_GroundCheck.collider.gameObject.tag != "Player")
            {
                m_IsGrounded = true;

                m_GroundedObject = m_GroundCheck.collider.gameObject;
                Debug.Log("Standing on " + m_GroundCheck.collider.gameObject.name);

                m_RespawnNode.transform.position = m_GroundedObject.transform.position + m_RespawnOffset;
            }
        }
    }

    private void PlayerRespawn()
    {
        if (m_IsDead)
        {
            m_CharacterController.enabled = false;
            transform.position = m_RespawnNode.transform.position;
            m_CharacterController.enabled = true;

            m_PlayerController.SetVelocity(new Vector3(0.0f, m_PlayerController.GetVelocity().y, 0.0f));

            Debug.Log("Respawn");
            m_IsDead = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Killbox")
        {
            m_IsDead = true;
            PlayerRespawn();
            
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        //Gizmos.DrawSphere(m_RespawnNode.gameObject.transform.position, m_GizmoRadius);
    }
}
