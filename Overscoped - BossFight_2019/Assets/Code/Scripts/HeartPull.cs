using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartPull : PullObject
{
    [Tooltip("Amount of time until the object will dissolve.")]
    [SerializeField]
    private float m_fDissolveTime = 10.0f;

    [Tooltip("Explosion VFX for when the heart is pull out.")]
    [SerializeField]
    private ParticleObject m_explosion = new ParticleObject();

    private GameObject m_player; // Player object reference.
    private CameraEffects m_camEffects; // Camera effects script reference.
    private PlayerBeam m_playerBeamScript; // Player beam constroller script reference.
    private BossBehaviour m_bossScript; // Boss AI script reference.
    private Material m_material; // Heart material reference.

    new private void Awake()
    {
        base.Awake();

        m_player = GameObject.FindGameObjectWithTag("Player");
        m_camEffects = m_player.GetComponentInChildren<CameraEffects>();
        m_playerBeamScript = m_player.GetComponent<PlayerBeam>();
        m_bossScript = GetComponentInParent<BossBehaviour>();

        // Duplicate material.
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        m_material = meshRenderer.material;

        // Disable script to prevent dissolve updates.
        enabled = false;
    }

    new private void Update()
    {
        m_fDissolveTime = Mathf.Max(m_fDissolveTime - Time.deltaTime, 0.0f);

        m_material.SetFloat("_Dissolve", Mathf.Min(m_fDissolveTime, 1.0f));

        // Disable when fully dissolved.
        if (m_fDissolveTime <= 0.0f)
            gameObject.SetActive(false);
    }

    public override void Trigger(Vector3 playerDirection)
    {
        // Decouple.
        base.Trigger(playerDirection);

        // Progress boss stage.
        if(m_bossScript)
            m_bossScript.KillBoss();

        // Play explosion effect.
        if(!m_explosion.IsPlaying())
            m_explosion.Play();

        // Turn of fresnel.
        m_material.SetFloat("_FresnelOnOff", 0.0f);

        // Enable for dissolve updates.
        enabled = true;
    }
}
