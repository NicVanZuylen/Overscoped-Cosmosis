using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamPickup : MonoBehaviour
{
    [Tooltip("Amount in which the player's beam charge will be increased upon pickup.")]
    [SerializeField]
    private float m_fChargeAmount = 10.0f;

    private GameObject m_player;
    private PlayerBeam m_playerBeamScript;

    void Awake()
    {
        // Find player and PlayerStats script instance.
        m_player = GameObject.FindGameObjectWithTag("Player");
        m_playerBeamScript = m_player.GetComponent<PlayerBeam>();

        // Create player near detection radius.
        GameObject newObj = new GameObject("Beam_Pickup_Near_Radius");
        newObj.tag = "Pickup";
        newObj.transform.position = transform.position;
        newObj.AddComponent<SphereCollider>();
        newObj.GetComponent<SphereCollider>().radius = 30.0f;
        newObj.GetComponent<SphereCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // If the collider is from the player increment the beam charge and destroy.
        if(other.tag == "Player")
        {
            // Increment beam charge...
            m_playerBeamScript.IncreaseCharge(m_fChargeAmount);

            gameObject.SetActive(false);
        }
    }
}
