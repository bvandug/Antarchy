using UnityEngine;
using UnityEngine.UI;
[ExecuteInEditMode()]

public class ProgressBar : MonoBehaviour
{

    public int maximum;
    public int current;
    public Image mask;
    public Image fillImage;
    public Color lowColor = Color.red;
    public Color normalColor = Color.blue;
    public float lowThreshold = 0.40f;

    public int minimum;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        current = maximum;
        GetCurrentFill();

        
    }

    // Update is called once per frame
    void Update()
    {
        GetCurrentFill();

    }

    void GetCurrentFill(){

        float currentOffset = current - minimum;
        float maximumOffset = maximum - minimum;
        float fillAmount = currentOffset/maximumOffset;
        mask.fillAmount = fillAmount;

        if (fillImage != null){
            fillImage.color = (fillAmount <= lowThreshold ? lowColor : normalColor);
        }
    }

    public void SetProgress(int value){
        current = Mathf.Clamp(value, minimum, maximum);
    }
}
