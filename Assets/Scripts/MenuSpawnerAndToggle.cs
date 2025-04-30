using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI; // per il raycaster XR

public class MenuSpawnerAndToggle : MonoBehaviour
{
    public InputActionProperty gripAction;
    public GameObject rightHandController;
    public QueryVisualizer queryVisualizer;
    private GameObject menuCanvas;
    private bool isVisible = false;

    void Start()
    {
        menuCanvas = new GameObject("MenuCanvas");
        var canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        menuCanvas.AddComponent<CanvasScaler>();
        menuCanvas.AddComponent<GraphicRaycaster>();
        menuCanvas.AddComponent<TrackedDeviceGraphicRaycaster>(); // XR support

        // Imposta layer UI
        int uiLayer = LayerMask.NameToLayer("UI");
        menuCanvas.layer = uiLayer;

        menuCanvas.transform.SetParent(rightHandController.transform);
        menuCanvas.transform.localPosition = new Vector3(0f, 0f, 0.25f);
        menuCanvas.transform.localRotation = Quaternion.identity;
        menuCanvas.transform.localScale = Vector3.one * 0.001f;

        var rt = menuCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 300);

        // Sfondo
        GameObject panel = new GameObject("Background");
        panel.transform.SetParent(menuCanvas.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.9f);
        panel.layer = uiLayer;

        // Titolo
        GameObject label = new GameObject("Label");
        label.transform.SetParent(panel.transform, false);
        var text = label.AddComponent<TextMeshProUGUI>();
        var trt = label.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.05f, 0.85f);
        trt.anchorMax = new Vector2(0.95f, 0.98f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        text.text = "Menu Query";
        text.fontSize = 40;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        label.layer = uiLayer;

        // Bottoni (2 per riga)
        int totalButtons = 6;
        int buttonsPerRow = 2;
        float buttonWidth = 180;
        float buttonHeight = 50;
        float spacing = 10;
        float startX = -((buttonsPerRow - 1) * (buttonWidth + spacing)) / 2f;
        float startY = 60;

        for (int i = 0; i < totalButtons; i++)
        {
            GameObject buttonGO = new GameObject($"QueryButton_{i + 1}");
            buttonGO.transform.SetParent(panel.transform, false);
            buttonGO.layer = uiLayer;

            RectTransform brt = buttonGO.AddComponent<RectTransform>();
            float x = startX + (i % buttonsPerRow) * (buttonWidth + spacing);
            float y = startY - (i / buttonsPerRow) * (buttonHeight + spacing);
            brt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            brt.anchoredPosition = new Vector2(x, y);

            Button button = buttonGO.AddComponent<Button>();
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.4f, 1f, 0.8f);

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(buttonGO.transform, false);
            txtGO.layer = uiLayer;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            RectTransform trt1 = txtGO.GetComponent<RectTransform>();
            trt1.anchorMin = Vector2.zero;
            trt1.anchorMax = Vector2.one;
            trt1.offsetMin = Vector2.zero;
            trt1.offsetMax = Vector2.zero;
            tmp.text = $"Query {i + 1}";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            int index = i;

            if (index == 0) // Query 1
            {
                button.onClick.AddListener(() =>
                {
                    Debug.Log("Esecuzione Query 1");
                    queryVisualizer.GenerateHistogram();
                });
            }
            else
            {
                button.onClick.AddListener(() => Debug.Log($"Hai cliccato Query {index + 1}"));
            }

        }

        foreach (Transform child in menuCanvas.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = uiLayer;
        }

        menuCanvas.SetActive(false);
    }

    void Update()
    {
        if (gripAction.action != null && gripAction.action.WasPressedThisFrame())
        {
            isVisible = !isVisible;
            menuCanvas.SetActive(isVisible);
        }
    }
}