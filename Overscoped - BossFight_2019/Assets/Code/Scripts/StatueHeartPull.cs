using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatueHeartPull : PullObject
{
    // Mesh renderers of all rocks to dissolve in.
    [SerializeField]
    private GameObject[] m_rocks = null;

    [SerializeField]
    private Collider[] m_rockColliders = null;

    [Tooltip("Rate in which the rocks will dissolve-in.")]
    [SerializeField]
    private float m_fDissolveRate = 1.0f;

    private Material m_rockMaterial; // Shared material for the rocks to dissolve in.
    private float m_fDissolve;

    new private void Awake()
    {
        // Call base initialization functionality.
        base.Awake();

        // Instantiate new dissolve material to share between rocks.
        m_rockMaterial = Instantiate(m_rocks[0].GetComponent<MeshRenderer>().material);

        // Use new shared material for all rocks and disable rock colliders.
        for (int i = 0; i < m_rocks.Length; ++i)
        {
            m_rocks[i].GetComponent<MeshRenderer>().sharedMaterial = m_rockMaterial;

            m_rockColliders[i].enabled = false;
        }

        // Set initial dissolve value.
        m_fDissolve = 0.0f;
        m_rockMaterial.SetFloat("_Dissolve", m_fDissolve);

        // Disable to stop overhead when not needed.
        enabled = false;
    }

    new private void Update()
    {
        if (m_fDissolve < 1.0f)
        {
            m_fDissolve = Mathf.Max(m_fDissolve + (Time.deltaTime * m_fDissolveRate), 0.0f);
            m_rockMaterial.SetFloat("_Dissolve", m_fDissolve);
        }
        else
            enabled = false; // Disable once dissolve is complete.
    }

    public override void Trigger(Vector3 v3PlayerDirection)
    {
        // Call base trigger behaviour.
        base.Trigger(v3PlayerDirection);

        // Enable rock colliders.
        for (int i = 0; i < m_rockColliders.Length; ++i)
            m_rockColliders[i].enabled = true;

        // Enable this script for updates, the updates will dissolve in the rocks.
        enabled = true;
    }
}
