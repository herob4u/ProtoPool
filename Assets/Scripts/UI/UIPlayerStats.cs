using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerStats : MonoBehaviour
{
    public Text ScoreNumericText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(PoolGameDirector.Instance && ScoreNumericText)
        {
            PoolGamePlayer localPoolPlayer = PoolGameDirector.Instance.GetLocalPoolPlayer();
            if(localPoolPlayer != null)
            {
                ScoreNumericText.text = localPoolPlayer.Score.ToString();   
            }
        }
    }
}
