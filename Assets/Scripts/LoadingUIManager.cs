using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingUIManager : MonoBehaviour
{
    public LoadData loadData;

    public GameObject textWait;
    public GameObject loadBars;
    public Transform progressBarFill;

    private float fullWidth = 1f;

    void Start()
    {
        textWait.SetActive(true);
        loadBars.SetActive(true);
        StartCoroutine(CheckLoading());
    }

    IEnumerator CheckLoading()
    {
        TextMeshProUGUI loadingText = textWait.GetComponent<TextMeshProUGUI>();

        while (!loadData.isLoaded)
        {
            loadingText.text = "Stirring the data soup... Almost tasty.";

            float progress = Mathf.Clamp01(loadData.progress);

            Vector3 scale = progressBarFill.localScale;
            scale.x = progress * fullWidth;
            progressBarFill.localScale = scale;

            Vector3 pos = progressBarFill.localPosition;
            pos.x = (progress - 1f) * fullWidth * 0.5f;
            progressBarFill.localPosition = pos;

            yield return null;
        }

        loadingText.text = "Data cooked to perfection. Bon appétit!";

        Vector3 finalScale = progressBarFill.localScale;
        finalScale.x = fullWidth;
        progressBarFill.localScale = finalScale;

        Vector3 finalPos = progressBarFill.localPosition;
        finalPos.x = 0f;
        progressBarFill.localPosition = finalPos;

        yield return new WaitForSeconds(2f);

        textWait.SetActive(false);
        loadBars.SetActive(false);
    }
}
