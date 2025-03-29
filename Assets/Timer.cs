using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{


    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color warningColor = Color.red;
    float elapsedTime;
    float levelTime = 20;
    float remainingTime;
    public GameObject timeUpPanel;
    private bool isPaused = false;

    //Here is the AttackManager
    private AttackManager attackManager;

    // This runs at game start
    void Start()
    {
        attackManager = FindFirstObjectByType<AttackManager>(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (isPaused) return;
        elapsedTime  += Time.deltaTime;
        remainingTime = levelTime - elapsedTime; 

        if (remainingTime <= 0){
            remainingTime = 0;
            attackManager.ExecuteAttack();
            if (attackManager.gameOver == false){
                ShowTimeUpPanel();
            }
            
            
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);


        
    }

    private void ResetTimer(){
        elapsedTime = 0;
        remainingTime = levelTime;
    }
    private void ShowTimeUpPanel(){
        isPaused = true;
        timeUpPanel.SetActive(true);
        Time.timeScale = 0f;

    }

    public void OnTimeUpPanelOk(){

        ResetTimer();
        isPaused = false;
        timeUpPanel.SetActive(false);
        Time.timeScale = 1f; // Resume if you paused it
}


}
