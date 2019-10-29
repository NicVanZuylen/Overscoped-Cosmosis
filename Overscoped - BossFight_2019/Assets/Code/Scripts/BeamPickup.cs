using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class BeamPickup : MonoBehaviour
{
    [Header("Bonus")]

    [Tooltip("Amount in which the player's beam charge will be increased upon pickup.")]
    [SerializeField]
    private float m_fChargeAmount = 10.0f;

    [Header("Effects")]

    [Tooltip("Audio source of pickup SFX.")]
    [SerializeField]
    private AudioSource m_pickupSFXSource = null;

    [Tooltip("Sound effect played upon pickup.")]
    [SerializeField]
    private AudioClip m_pickupSFX = null;

    [Tooltip("VFX played on pickup.")]
    [SerializeField]
    private ParticleObject m_pickupVFX = new ParticleObject();

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

        if (m_pickupVFX.m_particleSystems.Length > 0)
            m_pickupVFX.m_particleSystems[0].transform.parent = null;

        if(m_pickupSFXSource)
        {
            // Detach object.
            m_pickupSFXSource.gameObject.transform.parent = null;

            m_pickupSFXSource.spatialBlend = 1.0f;
            m_pickupSFXSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        // Stop VFX.
        m_pickupVFX.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        // If the collider is from the player increment the beam charge and destroy.
        if(other.tag == "Player")
        {
            // Increment beam charge...
            m_playerBeamScript.IncreaseCharge(m_fChargeAmount);

            // Play pickup particle effect.
            m_pickupVFX.Play();

            // Play pickup sound FX.
            if(m_pickupSFX && m_pickupSFXSource)
            {
                m_pickupSFXSource.PlayOneShot(m_pickupSFX);
            }

            // Add to respawn pickup object pool.
            PickupRespawner.EnqueuePickup(this);

            gameObject.SetActive(false);
        }
    }
}
