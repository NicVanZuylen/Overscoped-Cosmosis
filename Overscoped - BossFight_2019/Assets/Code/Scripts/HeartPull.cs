using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartPull : PullObject
{
    [Tooltip("Amount of time until the object will dissolve.")]
    [SerializeField]
    private float m_fDissolveTime = 10.0f;

    //[Tooltip("Rate in which the armor piece will dissolve.")]
    //[SerializeField]
    //private float m_fDissolveRate = 0.05f;

    private GameObject m_player;
    private PlayerBeam m_playerBeamScript;
    private BossBehaviour m_bossScript;
    private Material m_material;

    new private void Awake()
    {
        base.Awake();

        m_player = GameObject.FindGameObjectWithTag("Player");
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
        m_bossScript.BossDead();

        // Turn of fresnel.
        m_material.SetFloat("_FresnelOnOff", 0.0f);

        // Enable for dissolve updates.
        enabled = true;
    }
}
