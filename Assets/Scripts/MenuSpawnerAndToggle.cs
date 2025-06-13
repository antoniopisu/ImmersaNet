using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class MenuSpawnerAndToggle : MonoBehaviour
{
    public InputActionProperty gripAction;
    public GameObject rightHandController;
    public QueryVisualizer queryVisualizer;
    private GameObject menuCanvas;
    private bool isVisible = false;

    private Dictionary<int, bool> queryStates = new();
    private Dictionary<int, Image> buttonImages = new();
    private TextMeshProUGUI descriptionText;

    void Start()
    {
        menuCanvas = new GameObject("MenuCanvas");
        var canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        menuCanvas.AddComponent<CanvasScaler>();
        menuCanvas.AddComponent<GraphicRaycaster>();
        menuCanvas.AddComponent<TrackedDeviceGraphicRaycaster>();

        int uiLayer = LayerMask.NameToLayer("UI");
        menuCanvas.layer = uiLayer;

        menuCanvas.transform.SetParent(rightHandController.transform);
        menuCanvas.transform.localPosition = new Vector3(0f, 0.15f, 0.5f);
        menuCanvas.transform.localRotation = Quaternion.identity;
        menuCanvas.transform.localScale = Vector3.one * 0.001f;

        var rt = menuCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 400);  // più stretto e alto

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

        GameObject label = new GameObject("Label");
        label.transform.SetParent(panel.transform, false);
        var text = label.AddComponent<TextMeshProUGUI>();
        var trt = label.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.05f, 0.9f);
        trt.anchorMax = new Vector2(0.95f, 0.98f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        text.text = "Menu Query";
        text.fontSize = 35;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        label.layer = uiLayer;

        GameObject descGO = new GameObject("DescriptionBox");
        descGO.transform.SetParent(panel.transform, false);
        RectTransform drt = descGO.AddComponent<RectTransform>();
        drt.sizeDelta = new Vector2(260, 150);  // puoi ridurre a 240 se serve più margine orizzontale
        drt.anchoredPosition = new Vector2(0f, -165f); //distanza da margine alto
        descGO.layer = uiLayer;

        // Aggiunta del componente Mask (opzionale)
        descGO.AddComponent<RectMask2D>();

        descriptionText = descGO.AddComponent<TextMeshProUGUI>();
        descriptionText.fontSize = 18;
        descriptionText.color = Color.white;
        descriptionText.alignment = TextAlignmentOptions.TopLeft;  // più leggibile
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Masking;  // oppure Ellipsis o Truncate
        descriptionText.text = "";

        // === Bottoni ===
        string[] queryNames = new string[]
        {
            "Top Talkers Histogram",
            "Traffic by Protocol",
            "Heatmap Tbyte"
        };

        string[] queryDescriptions = new string[]
        {
            "Shows a bar chart of the IPs that sent/received the most bytes.",
            "Displays floating bubbles for each protocol used in traffic.",
            "Creates a heatmap of total bytes exchanged between nodes."
        };

        float buttonWidth = 240;
        float buttonHeight = 55;
        float spacing = 15;
        float startY = 100;

        for (int i = 0; i < queryNames.Length; i++)
        {
            GameObject buttonGO = new GameObject($"QueryButton_{i + 1}");
            buttonGO.transform.SetParent(panel.transform, false);
            buttonGO.layer = uiLayer;

            RectTransform brt = buttonGO.AddComponent<RectTransform>();
            brt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            brt.anchoredPosition = new Vector2(0f, startY - i * (buttonHeight + spacing));

            Button button = buttonGO.AddComponent<Button>();
            Image buttonImage = buttonGO.AddComponent<Image>();

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(buttonGO.transform, false);
            txtGO.layer = uiLayer;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            RectTransform trt1 = txtGO.GetComponent<RectTransform>();
            trt1.anchorMin = Vector2.zero;
            trt1.anchorMax = Vector2.one;
            trt1.offsetMin = Vector2.zero;
            trt1.offsetMax = Vector2.zero;
            tmp.text = queryNames[i];
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;

            int index = i;
            queryStates[i] = false;
            buttonImages[i] = buttonImage;
            buttonImage.color = Color.red;

            button.onClick.AddListener(() =>
            {
                Debug.Log($"Esecuzione {queryNames[index]}");
                bool isActive = queryStates[index];

                if (isActive)
                {
                    switch (index)
                    {
                        case 0: queryVisualizer.CloseQuery(QueryType.Histogram); break;
                        case 1: queryVisualizer.CloseQuery(QueryType.ProtocolBubbles); break;
                        case 2: queryVisualizer.CloseQuery(QueryType.Heatmap); break;
                    }
                    buttonImages[index].color = Color.red;
                    queryStates[index] = false;
                }
                else
                {
                    switch (index)
                    {
                        case 0: queryVisualizer.GenerateHistogram(); break;
                        case 1: queryVisualizer.GenerateProtocolBubbles(); break;
                        case 2: queryVisualizer.GenerateHeatmap(); break;
                    }
                    buttonImages[index].color = Color.green;
                    queryStates[index] = true;
                }
            });

            var hoverHandler = buttonGO.AddComponent<MenuHoverHandler>();
            hoverHandler.Initialize(queryDescriptions[index], descriptionText);
        }

        foreach (Transform child in menuCanvas.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = uiLayer;

        menuCanvas.SetActive(false);
    }

    public void ResetAllButtons()
    {
        foreach (var index in queryStates.Keys.ToList())
        {
            queryStates[index] = false;
            if (buttonImages.TryGetValue(index, out var img))
            {
                img.color = Color.red;
            }
        }
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
