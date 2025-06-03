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
    public Image nextButtonBackground;

    [Header("Layout dinamico")]
    public float margineSuperiore = 30f;
    public float spazioTraTesti = 20f;
    public float larghezzaMargine = 40f;
    public float margineInferiore = 100f;

    [Header("Wrapper VR Panel")]
    public Transform wrapperPanel;      // WrapperPanel nella scena
    public Transform playerCamera;      // Tipicamente Camera.main.transform
    public float distanceFromFace = 1.5f;
    public float verticalOffset = 0f;

    private RectTransform panelRect;
    private RectTransform titoloRect;
    private RectTransform paragrafoRect;

    private int currentIndex = 0;
    private Color coloreOriginale;

    private string[] titoli =
    {
        "Welcome to ImmersaNet",
        "Basic Movement",
        "Network Nodes",
        "Explore Connections",
        "Visualizations",
        "Let’s Begin!"
    };

    private string[] paragrafi =
    {
        "In this tutorial, you'll learn how to visually explore network traffic in virtual reality.",
        "Use the left controller joystick to move around the environment. Use the right joystick to look around.",
        "Press the Y button on the left controller to show or hide the network timeline. Start the timeline to view the network over time. " +
        "Each sphere represents a network device with its IP address. Connections between nodes indicate network traffic.",
        "Point at the connection between two nodes and press the select button on the back of the controller to view the connection details.",
        "Press the A button on the right controller to show or hide the menu. You can launch any query by pressing its button with the select button on the left controller.",
        "You're ready to begin. Use the tutorial to learn how the system works, and when you're ready, press the button to use the full system."
    };

    void Start()
    {
        panelRect = panelTutorial.GetComponent<RectTransform>();
        titoloRect = titoloText.GetComponent<RectTransform>();
        paragrafoRect = paragrafoText.GetComponent<RectTransform>();

        if (nextButtonBackground != null)
        {
            coloreOriginale = nextButtonBackground.color;
        }

        AggiornaLayoutTesti();
        UpdatePanel();

        nextButton.onClick.AddListener(HandleNextClick);
        prevButton.onClick.AddListener(PrevPanel);

        prevButton.gameObject.SetActive(false);
    }

    void AggiornaLayoutTesti()
    {
        if (panelRect == null || titoloRect == null || paragrafoRect == null)
        {
            Debug.LogError("TutorialManager: Layout not found.");
            return;
        }

        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;
        float altezzaTitolo = 80f;

        titoloRect.anchorMin = new Vector2(0.5f, 1f);
        titoloRect.anchorMax = new Vector2(0.5f, 1f);
        titoloRect.pivot = new Vector2(0.5f, 1f);
        titoloRect.sizeDelta = new Vector2(panelWidth - 2 * larghezzaMargine, altezzaTitolo);
        titoloRect.anchoredPosition = new Vector2(0f, -margineSuperiore);

        float altezzaDisponibile = panelHeight - altezzaTitolo - margineSuperiore - spazioTraTesti - margineInferiore;

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

        Debug.Log($"[Tutorial] UpdatePanel - Index: {currentIndex}, Titolo: {titoli[currentIndex]}");

        prevButton.gameObject.SetActive(currentIndex > 0);

        if (currentIndex == titoli.Length - 1)
        {
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
            if (nextButtonBackground != null)
                nextButtonBackground.color = Color.green;
        }
        else
        {
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next";
            if (nextButtonBackground != null)
                nextButtonBackground.color = coloreOriginale;
        }
    }

    public void HandleNextClick()
    {
        if (currentIndex < titoli.Length - 1)
        {
            currentIndex++;
            Debug.Log($"[Tutorial] NEXT clicked -> Now showing panel {currentIndex}: \"{titoli[currentIndex]}\"");
            UpdatePanel();
        }
        else
        {
            Debug.Log("[Tutorial] START button clicked – Loading main scene...");
            StartMainScene();
        }
    }

    public void PrevPanel()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            Debug.Log($"[Tutorial] PREV clicked -> Now showing panel {currentIndex}: \"{titoli[currentIndex]}\"");
            UpdatePanel();
        }
    }

    void StartMainScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    void LateUpdate()
    {
        if (wrapperPanel != null && playerCamera != null)
        {
            Vector3 currentPos = wrapperPanel.position;
            float newY = playerCamera.position.y + verticalOffset;

            wrapperPanel.position = new Vector3(currentPos.x, newY, currentPos.z);
        }
    }

}
