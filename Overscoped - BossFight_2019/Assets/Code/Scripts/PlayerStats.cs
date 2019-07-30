using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public enum ERegenMode
    {
        REGEN_LINEAR,
        REGEN_LERP
    }

    // On inspector: 

    [Header("Health & Mana")]
    [Space(10)]

    [SerializeField]
    private float m_fMaxHealth = 100.0f;
    [SerializeField]
    private float m_fMaxMana = 100.0f;

    [Tooltip("Minimum mana to cast the grapple.")]
    [SerializeField]
    private float m_fMinManaCost = 15.0f;

    [Tooltip("Rate of player's mana loss when using the grapple.")]
    [SerializeField]
    private float m_fManaLossRate = 5.0f;

    [Tooltip("Rate of mana regeneration when not using the grapple.")]
    [SerializeField]
    private float m_fManaRegenRate = 20.0f;

    [Tooltip("Rate of mana regeneration when not using the grapple in lerp mode.")]
    [SerializeField]
    private float m_fManaRegenRateLerp = 0.1f;

    [Tooltip("Amount of time after using the grapple mana regen will begin.")]
    [SerializeField]
    private float m_fManaRegenDelay = 1.0f;

    [Tooltip("Regen mode for mana regen.")]
    [SerializeField]
    private ERegenMode m_manaRegenMode = ERegenMode.REGEN_LINEAR;

    [Space(10)]
    [Header("Health & Mana")]
    [Space(10)]

    [SerializeField]
    private RectTransform m_healthFillRect = null;

    [SerializeField]
    private RectTransform m_manaFillRect = null;

    [SerializeField]
    private Material m_manaMat = null;

    [SerializeField]
    private Material m_healthMat = null;

    // Private:

    private GrappleHook m_hookScript;
    private PlayerController m_controller;

    private float m_fHealth;
    private float m_fMana;
    private float m_fCurrentRegenDelay;

    void Awake()
    {
        m_hookScript = GetComponent<GrappleHook>();
        m_controller = GetComponent<PlayerController>();

        m_fHealth = m_fMaxHealth;
        m_fMana = m_fMaxMana;
        m_fCurrentRegenDelay = 1.0f;   
    }

    // Update is called once per frame
    void Update()
    {
        if(m_hookScript.IsActive())
        {
            // Reset mana regen delay.
            m_fCurrentRegenDelay = m_fManaRegenDelay;

            // Lose mana whilst the hook is active.
            m_fMana -= m_fManaLossRate * Time.deltaTime;
        }
        else
        {
            // Count down regen delay.
            m_fCurrentRegenDelay -= Time.deltaTime;

            if (m_fCurrentRegenDelay <= 0.0f)
            {
                // Regenerate mana.
                switch(m_manaRegenMode)
                {
                    case ERegenMode.REGEN_LINEAR:
                        m_fMana += m_fManaRegenRate * Time.deltaTime;
                        break;

                    case ERegenMode.REGEN_LERP:
                        m_fMana = Mathf.Lerp(m_fMana, m_fMaxMana, m_fManaRegenRateLerp);
                        break;
                }
            }
        }

        // Clamp health and mana.
        m_fHealth = Mathf.Clamp(m_fHealth, 0.0f, m_fMaxHealth);
        m_fMana = Mathf.Clamp(m_fMana, 0.0f, m_fMaxMana);

        m_healthMat.SetFloat("_Mana", 1.0f - (m_fHealth / m_fMaxHealth));
        m_manaMat.SetFloat("_Mana", 1.0f - (m_fMana / m_fMaxMana));

        m_healthFillRect.sizeDelta = new Vector2((m_fHealth / m_fMaxHealth) * 300.0f, m_healthFillRect.sizeDelta.y);
        m_manaFillRect.sizeDelta = new Vector3((m_fMana / m_fMaxMana) * 300.0f, m_manaFillRect.sizeDelta.y);
    }

    /*
    Description: Whether or not the player has enough mana to cast the grapple. 
    Return Type: bool
    */
    public bool EnoughMana()
    {
        return m_fMana > m_fMinManaCost;
    }

    /*
    Description: Restore the player's health to full, and reverse any death effects.
    */
    public void Resurrect()
    {
        m_fHealth = m_fMaxHealth;

        // Reverse death effects...
    }

    /*
    Description: Restore the player's health to full. 
    */
    public void RestoreHealth()
    {
        m_fHealth = m_fMaxHealth;
    }

    /*
    Description: Deal damage to the player's health, and play any related effects.
    Param:
        float fDamage: The amount of damage to deal to the player's health.
    */
    public void DealDamage(float fDamage)
    {
        m_fHealth -= fDamage;

        if(m_fHealth <= 0.0f)
        {
            m_fHealth = 0.0f;

            // Kill player...
        }
    }
}
