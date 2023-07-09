using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class UI : MonoBehaviour
{
    public static UI instance;
    
    public TMPro.TextMeshProUGUI appState;
    public TMPro.TextMeshProUGUI boardState;
    public TMPro.TextMeshProUGUI movingCount;

    public TMPro.TextMeshProUGUI score;
    
    
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        appState.text = "App state: " + AppManager.instance.state.ToString();
        boardState.text = "Board state: " + Board.instance.state.ToString();

        score.text = "Score: " + AppManager.instance.score.ToString();
    }
}
