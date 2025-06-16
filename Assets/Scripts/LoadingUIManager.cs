using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingUIManager : MonoBehaviour
{
    public LoadData loadData;

    public GameObject textWait;         // GameObject che contiene il testo (TextWait)
    public GameObject loadBars;         // GameObject che contiene i quad (LoadBars)
    public Transform progressBarFill;   // Il quad verde che cresce

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
            loadingText.text = "Loading dataset... Please wait.";

            float progress = Mathf.Clamp01(loadData.progress);

            Vector3 scale = progressBarFill.localScale;
            scale.x = progress * fullWidth;
            progressBarFill.localScale = scale;

            Vector3 pos = progressBarFill.localPosition;
            pos.x = (progress - 1f) * fullWidth * 0.5f;
            progressBarFill.localPosition = pos;

            yield return null;
        }

        loadingText.text = "Dataset loaded! You can now press Play.";

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
