using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{


    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color warningColor = Color.red;
    float elapsedTime;
    float levelTime = 300;
    float remainingTime;

    // Update is called once per frame
    void Update()
    {
        elapsedTime  += Time.deltaTime;
        remainingTime = levelTime - elapsedTime; 

        if (remainingTime <= 0){
            ResetTimer();
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);


        
    }

    private void ResetTimer(){
        elapsedTime = 0;
        remainingTime = levelTime;
    }
}
