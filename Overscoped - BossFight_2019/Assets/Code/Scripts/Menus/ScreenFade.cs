using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Description: Screen fade-to-black effects.
 * Author: Nic Van Zuylen
*/

public class ScreenFade : MonoBehaviour
{
    public enum EFadeMode
    {
        FADE_IN,
        FADE_OUT
    }

    [Tooltip("Image to fade to and from full opacity.")]
    [SerializeField]
    private Image m_fadeImage = null;

    [Tooltip("Rate in which the image will fade.")]
    [SerializeField]
    private float m_fFadeRate = 0.3f;

    private EFadeMode m_eMode;
    private float m_fFadeLevel; // Fade value between 1 and 0.

    public delegate void FadeCallback();

    private FadeCallback m_callback;

    private void Awake()
    {
        m_fadeImage.gameObject.SetActive(true);

        // Set opacity.
        Color newColor = m_fadeImage.color;
        newColor.a = 1.0f;

        m_fadeImage.color = newColor;

        // Initial fade level.
        m_fFadeLevel = 1.0f;

        // Begin fading to game.
        BeginFade(EFadeMode.FADE_OUT);
    }

    private void Update()
    {
        if (m_fadeImage == null)
            return;

        switch (m_eMode)
        {
            case EFadeMode.FADE_IN:
                m_fFadeLevel += m_fFadeRate * Time.unscaledDeltaTime;

                if(m_fFadeLevel >= 1.0f)
                {
                    m_fFadeLevel = 1.0f;
                    enabled = false; // Disable script as realtime updates are no longer needed.

                    if (m_callback != null)
                    {
                        m_callback(); // Run set callback.
                    }
                }
                break;

            case EFadeMode.FADE_OUT:
                m_fFadeLevel -= m_fFadeRate * Time.unscaledDeltaTime;

                if (m_fFadeLevel <= 0.0f)
                {
                    m_fFadeLevel = 0.0f;
                    enabled = false; // Disable script as realtime updates are no longer needed.

                    if (m_callback != null)
                    {
                        m_callback(); // Run set callback.
                    }
                }
                break;
        }

        // Set new opacity.
        Color newColor = m_fadeImage.color;
        newColor.a = m_fFadeLevel;

        m_fadeImage.color = newColor;
    }

    /*
    Description: Begin the fading in/out process.
    Param:
        EFadeMode: eMode: The mode specifying whether to fade in or out.
    */
    public void BeginFade(EFadeMode eMode)
    {
        m_eMode = eMode;

        enabled = true; // Enable script for updates.
    }

    /*
    Description: Set callback function to be called when fading in/out is complete.
    Param:
        FadeCallback callback: The callback function to call on completion.
    */
    public void SetCallback(FadeCallback callback)
    {
        m_callback = callback;
    }

    /*
    Description: Set the image fade rate.
    Param:
        float fRate: The rate in which the image will fade.
    */
    public void SetFadeRate(float fRate)
    {
        m_fFadeRate = fRate;
    }

    /*
    Description: Set the current image fade level.
    Param:
        float fLevel: The new current image fade level.
    */
    public void SetFade(float fLevel)
    {
        m_fFadeLevel = fLevel;
    }

}
