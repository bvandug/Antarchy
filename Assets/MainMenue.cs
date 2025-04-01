using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenue : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void PlayGame(){
        Time.timeScale = 1f;
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        SceneManager.LoadScene("GameScene");
        
    }
    public void StartTutorial(){
        SceneManager.LoadScene("Tutorial");
    }

    public void QuitGame(){
        Debug.Log("QUIT");
        Application.Quit();
    }
}
