using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("Riferimenti UI automatici")]
    public GameObject panelTutorial;
    public TextMeshProUGUI titoloText;
    public TextMeshProUGUI paragrafoText;
    public Button nextButton;
    public Button prevButton;
    public Button startButton;

    [Header("Layout dinamico")]
    public float margineSuperiore = 30f;
    public float spazioTraTesti = 20f;
    public float larghezzaMargine = 40f;
    public float margineInferiore = 100f;

    private RectTransform panelRect;
    private RectTransform titoloRect;
    private RectTransform paragrafoRect;

    private int currentIndex = 0;

    // Titoli in inglese
    private string[] titoli =
    {
        "Welcome to ImmersaNet",
        "Basic Movement",
        "Network Nodes",
        "Explore Connections",
        "Visualizations",
        "Let’s Begin!"
    };

    // Paragrafi in inglese
    private string[] paragrafi =
    {
        "In this tutorial, you'll learn how to visually explore network traffic in virtual reality.",

        "Use the left controller joystick to move around the environment. Use the right joystick to look around.",

        "Press the Y button on the left controller to show or hide the network timeline. Each sphere represents a network device with its IP address. " +
        "Connections between nodes indicate network traffic.",

        "Point at the connection between two nodes and press the select button on the back of the controller to view the connection details.",

        "Press the A button on the right controller to show or hide the menu. You can launch any query by pressing its button with the select button on the left controller.",

        "You're ready to begin. Use the tutorial to learn how the system works, and when you're ready, press the button to use the full system."
    };

    void Start()
    {
        // Rilevamento automatico dei RectTransform
        panelRect = panelTutorial.GetComponent<RectTransform>();
        titoloRect = titoloText.GetComponent<RectTransform>();
        paragrafoRect = paragrafoText.GetComponent<RectTransform>();

        AggiornaLayoutTesti();
        UpdatePanel();

        nextButton.onClick.AddListener(NextPanel);
        prevButton.onClick.AddListener(PrevPanel);
        startButton.onClick.AddListener(StartMainScene);

        prevButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(false);
    }

    void AggiornaLayoutTesti()
    {
        if (panelRect == null || titoloRect == null || paragrafoRect == null)
        {
            Debug.LogError("TutorialManager: Layout non trovato.");
            return;
        }

        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;

        float altezzaTitolo = 80f;

        // Titolo centrato in alto
        titoloRect.anchorMin = new Vector2(0.5f, 1f);
        titoloRect.anchorMax = new Vector2(0.5f, 1f);
        titoloRect.pivot = new Vector2(0.5f, 1f);
        titoloRect.sizeDelta = new Vector2(panelWidth - 2 * larghezzaMargine, altezzaTitolo);
        titoloRect.anchoredPosition = new Vector2(0f, -margineSuperiore);

        // Altezza massima disponibile per il paragrafo
        float altezzaDisponibile = panelHeight - altezzaTitolo - margineSuperiore - spazioTraTesti - margineInferiore;

        // Paragrafo sotto al titolo
        paragrafoRect.anchorMin = new Vector2(0.5f, 1f);
        paragrafoRect.anchorMax = new Vector2(0.5f, 1f);
        paragrafoRect.pivot = new Vector2(0.5f, 1f);
        paragrafoRect.sizeDelta = new Vector2(panelWidth - 2 * larghezzaMargine, altezzaDisponibile);
        paragrafoRect.anchoredPosition = new Vector2(0f, -margineSuperiore - altezzaTitolo - spazioTraTesti);
    }

    void UpdatePanel()
    {
        titoloText.text = titoli[currentIndex];
        paragrafoText.text = paragrafi[currentIndex];

        prevButton.gameObject.SetActive(currentIndex > 0);
        nextButton.gameObject.SetActive(currentIndex < titoli.Length - 1);
        startButton.gameObject.SetActive(currentIndex == titoli.Length - 1);
    }

    public void NextPanel()
    {
        if (currentIndex < titoli.Length - 1)
        {
            currentIndex++;
            UpdatePanel();
        }
    }

    public void PrevPanel()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdatePanel();
        }
    }

    void StartMainScene()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
