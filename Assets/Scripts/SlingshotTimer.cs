using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class SlingshotTimer : MonoBehaviour
{
    public static SlingshotTimer Instance;
    private static float _slingshotTimeLeft;

    [SerializeField]
    public static float secondsToRelease = 5f;

    private float m_timeRemaining;

    // private TMPro.TextMeshProUGUI m_tmp;
    [SerializeField] private TextMeshProUGUI m_tmp;

    public static event Action OnSlingshotTimerEnd;

    private bool m_run = false;

    private void Awake()
    {
        Instance = this;
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_timeRemaining = secondsToRelease;
        // REF: https://forum.unity.com/threads/changing-textmeshpro-text-from-ui-via-script.462250/
        // m_tmp = GetComponent<TMPro.TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if(m_run)
        {
            if (m_timeRemaining > 0)
            {
                m_timeRemaining -= Time.deltaTime;
                // REF: https://forum.unity.com/threads/convert-float-to-a-string.28332/
                m_tmp.text = m_timeRemaining.ToString();
                _slingshotTimeLeft = m_timeRemaining;
            } else {
                GameManager.UpdateGameState(GameState.Released);
                m_tmp.text = "Released!";
                OnSlingshotTimerEnd?.Invoke();
                m_run = false;
            }
        }
        
    }

    private void OnGameStateChanged(GameState s)
    {
        m_run = s == GameState.Slingshot;
    }

    public static float GetSlingshotTimeLeft()
    {
        return _slingshotTimeLeft;
    }
}
