using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreditsScreen : MonoBehaviour
{
    [Tooltip("Transform of the scrolling component of the credits screen.")]
    [SerializeField]
    private RectTransform m_scrollHandle = null;

    [Tooltip("Text component for the press to escape text.")]
    [SerializeField]
    private Text m_escapeText = null;

    [Tooltip("Scolling speed.")]
    [SerializeField]
    private float m_fScrollSpeed = 200.0f;

    [Tooltip("Scroll Y value in which the credits screen will transition back to the main menu.")]
    [SerializeField]
    private float m_fEndValue = -3000;

    private PauseMenu m_pauseScript;
    private ScreenFade m_fadeScript;
    private bool m_bQuitting;

    void Awake()
    {
        m_fadeScript = transform.parent.gameObject.GetComponentInChildren<ScreenFade>();
        m_pauseScript = transform.parent.gameObject.GetComponentInChildren<PauseMenu>();

        m_bQuitting = false;
    }

    void Update()
    {
        m_scrollHandle.anchoredPosition = m_scrollHandle.anchoredPosition + new Vector2(0.0f, m_fScrollSpeed * Time.unscaledDeltaTime);

        // Show skip text if any other button than escape is pressed.
        if(Input.anyKeyDown)
        {
            m_escapeText.enabled = true;
        }

        // Load main menu scene once end point has been reached or the user pressed escape to skip.
        if ((m_scrollHandle.anchoredPosition.y >= m_fEndValue || Input.GetKeyDown(KeyCode.Escape)) && !m_bQuitting)
        {
            m_escapeText.enabled = false;
            m_bQuitting = true;

            m_fadeScript.SetCallback(m_pauseScript.FadeQuitTitleScreen);
            m_fadeScript.BeginFade(ScreenFade.EFadeMode.FADE_IN);
        }
    }
}
