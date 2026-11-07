using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    [SerializeField] private Slider fuelSlider;
    [SerializeField] private TMP_Text fuetText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    private int totalPoints = 0;
    public TextMeshProUGUI pointsText;
    public GameObject pausePanel;


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        ;
    }



    public void UpdateFuelSlider(float current, float max)
    {
        fuelSlider.maxValue = max;
        fuelSlider.value = Mathf.RoundToInt(current);
        fuetText.text = fuelSlider.value + " / " + fuelSlider.maxValue;
    }

    public void UpdateHealthSlider(float current, float max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = Mathf.RoundToInt(current);
        healthText.text = healthSlider.value + " / " + healthSlider.maxValue;
    }

    public void AddPoint()
    {
        totalPoints++;
        UpdatePointsText(totalPoints);
    }
 
    public void UpdatePointsText(int currentPoints)
    {
        pointsText.text = "Score:" + currentPoints.ToString();
    }
}
