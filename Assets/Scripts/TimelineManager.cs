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
        UpdateTimeUI();
        UpdateSlider();
        UpdateButtonStates();
    }

    void UpdateTimeUI()
    {
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

    public void OnSliderDragStart() => isDragging = true;

    public void OnSliderDragEnd()
    {
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
        if (isRunning)
        {
            StopCoroutine(timelineRoutine);
            isRunning = false;
        }

        timelineRoutine = StartCoroutine(RunTimeline());
        isRunning = true;
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
        while (currentTime <= maxTime)
        {
            if (!isDragging)
            {
                UpdateTimeUI();
                UpdateSlider();
                currentTime = currentTime.AddSeconds(1);
            }

            yield return new WaitForSeconds(updateSpeed);
        }

        isRunning = false;
        UpdateButtonStates();
    }

    void UpdateButtonStates()
    {
        if (playButton != null) playButton.gameObject.SetActive(!isRunning);
        if (pauseButton != null) pauseButton.gameObject.SetActive(isRunning);
    }
}
