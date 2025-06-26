using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TimelineManager : MonoBehaviour
{
    public LoadData loadData;
    public VisualizeNetwork visualizeNetwork;
    public TextMeshProUGUI timeDisplay;
    public Slider timelineSlider;
    public Button playButton;
    public Button pauseButton;
    public float updateSpeed = 1f;

    private DateTime currentTime;
    private DateTime minTime;
    private DateTime maxTime;
    private Coroutine timelineRoutine;
    private bool isRunning = false;
    private bool isDragging = false;

    void Start()
    {
        if (!gameObject.activeInHierarchy || !enabled) return;

        // Disabilita Raycast Target solo per l'orario, se è true i puntatori dei controller sono troppo corti per una corretta interazione
        if (timeDisplay != null)
            timeDisplay.raycastTarget = false;

        // Disattiva pulsanti finché non è pronto
        playButton.interactable = false;
        pauseButton.interactable = false;
        timelineSlider.interactable = false;

        StartCoroutine(InitializeTimeline());

        playButton.onClick.AddListener(StartTimeline);
        pauseButton.onClick.AddListener(StopTimeline);
        timelineSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    IEnumerator InitializeTimeline()
    {
        while (!loadData.isLoaded)
            yield return null;

        List<DateTime> timestamps = new List<DateTime>();
        foreach (var row in loadData.data)
        {
            if (row.TryGetValue("Timestamp", out string rawTime) &&
                DateTime.TryParse(rawTime, out DateTime t))
            {
                timestamps.Add(t);
            }
        }

        if (timestamps.Count == 0)
        {
            Debug.LogError("Nessun timestamp trovato!");
            yield break;
        }

        minTime = timestamps[0];
        maxTime = timestamps[0];

        foreach (var t in timestamps)
        {
            if (t < minTime) minTime = t;
            if (t > maxTime) maxTime = t;
        }

        currentTime = minTime;
        UpdateSlider();
        UpdateButtonStates();

        // Attiva pulsanti dopo caricamento
        playButton.interactable = true;
        pauseButton.interactable = true;
        timelineSlider.interactable = true;
    }

    void UpdateTimeUI()
    {
        Debug.Log($"[UpdateTimeUI] Updating time: {currentTime}");

        if (timeDisplay != null)
            timeDisplay.text = currentTime.ToString("dd/MM/yyyy HH:mm:ss");

        visualizeNetwork?.VisualizzaReteInTempo(currentTime);
    }

    void UpdateSlider()
    {
        float totalSec = (float)(maxTime - minTime).TotalSeconds;
        float elapsedSec = (float)(currentTime - minTime).TotalSeconds;

        if (timelineSlider != null && totalSec > 0)
            timelineSlider.value = elapsedSec / totalSec;
    }

    public void OnSliderDragStart()
    {
        Debug.Log("[TimelineManager] Slider drag started");
        isDragging = true;
    }

    public void OnSliderDragEnd()
    {
        Debug.Log("[TimelineManager] Slider drag ended");
        isDragging = false;
        ApplySliderTime();
    }

    public void OnSliderValueChanged(float value)
    {
        if (isDragging)
        {
            ApplySliderTime();
        }
    }

    void ApplySliderTime()
    {
        double totalSec = (maxTime - minTime).TotalSeconds;
        double targetSec = timelineSlider.value * totalSec;
        currentTime = minTime.AddSeconds(targetSec);
        UpdateTimeUI();
    }

    public void StartTimeline()
    {
        Debug.Log("[TimelineManager] StartTimeline called");

        if (timelineRoutine != null)
        {
            StopCoroutine(timelineRoutine);
        }

        if (currentTime >= maxTime)
        {
            Debug.Log("[TimelineManager] currentTime >= maxTime -> reset to minTime");
            currentTime = minTime;
            UpdateTimeUI();
            UpdateSlider();
        }

        isDragging = false;

        timelineRoutine = StartCoroutine(RunTimeline());
        isRunning = true;

        visualizeNetwork.visualizationEnabled = true;

        UpdateButtonStates();
    }

    public void StopTimeline()
    {
        if (isRunning)
        {
            StopCoroutine(timelineRoutine);
            isRunning = false;
            UpdateButtonStates();
        }
    }

    IEnumerator RunTimeline()
    {
        Debug.Log("[TimelineManager] RunTimeline started");

        while (true)
        {
            if (!isDragging)
            {
                currentTime = currentTime.AddSeconds(1);
                Debug.Log($"[RunTimeline] currentTime = {currentTime}");

                if (currentTime > maxTime)
                {
                    Debug.Log("[TimelineManager] Reached maxTime -> stopping");
                    StopTimeline();
                    yield break;
                }

                UpdateTimeUI();
                UpdateSlider();
            }

            yield return new WaitForSeconds(updateSpeed);
        }
    }

    void UpdateButtonStates()
    {
        if (playButton != null) playButton.gameObject.SetActive(!isRunning);
        if (pauseButton != null) pauseButton.gameObject.SetActive(isRunning);
    }
}
