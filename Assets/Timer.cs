using UnityEngine;
using TMPro;
using System.Collections;

public class Timer : MonoBehaviour
{


    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color warningColor = Color.red;
    float elapsedTime;
    float levelTime = 30;
    float remainingTime;
    public GameObject timeUpPanel;
    public bool isPaused = true;
    private Coroutine flashCoroutine;
    

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
            
            
        }else if (remainingTime < 30)
        {
            if (flashCoroutine == null) // Start flashing if not already started
            {
                flashCoroutine = StartCoroutine(FlashText());
            }
        }
        else
        {
            // Ensure text is normal when not in warning range
            timerText.color = normalColor;
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);


        
    }

    private void ResetTimer(){
        elapsedTime = 0;
        remainingTime = levelTime;
        timerText.color = normalColor;
        if (flashCoroutine != null)
            {
        StopCoroutine(flashCoroutine);
        flashCoroutine = null;}
    }
    private void ShowTimeUpPanel(){
        isPaused = true;
        timeUpPanel.SetActive(true);
        Time.timeScale = 0f;

    }

    public void OnTimeUpPanelOk(){
        Time.timeScale = 1f; // Resume time
        ResetTimer(); // Reset timer values
        isPaused = false; // Unpause the timer
        timeUpPanel.SetActive(false);
}

    private IEnumerator FlashText()
    {
        while (remainingTime > 0 && remainingTime < 30)
        {
            timerText.color = (timerText.color == normalColor) ? warningColor : normalColor;
            yield return new WaitForSeconds(0.5f);
        }
        flashCoroutine = null; // Reset coroutine reference when finished
    }


}
