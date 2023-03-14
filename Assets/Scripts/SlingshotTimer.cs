using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SlingshotTimer : MonoBehaviour
{
    [SerializeField]
    public float secondsToRelease = 5f;

    private float m_timeRemaining;

    // private TMPro.TextMeshProUGUI m_tmp;
    [SerializeField] private TextMeshProUGUI m_tmp;

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
        if(GameManager.GetState() == GameState.Slingshot)
        {
            if (m_timeRemaining > 0)
            {
                m_timeRemaining -= Time.deltaTime;
                // REF: https://forum.unity.com/threads/convert-float-to-a-string.28332/
                m_tmp.text = m_timeRemaining.ToString();
            } else {
                GameManager.UpdateGameState(GameState.Released);
                m_tmp.text = "Released!";
            }
        }
        
    }
}
