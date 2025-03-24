using UnityEngine;
using UnityEngine.SceneManagement;

public class Settings : MonoBehaviour
{

    private bool isPaused = false;

    public GameObject settingsPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void ToggleSettingsPanel(){
        isPaused = !isPaused;
        settingsPanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log("Toggled Settings Panel. Paused: " + isPaused + ", TimeScale: " + Time.timeScale);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ResetTheGame(){
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    public void QuitGame(){
        Debug.Log("QUIT");
        Application.Quit();
    }
}
