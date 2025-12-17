using UnityEngine;
using System.Collections;

public class BaseSceneManager : MonoBehaviour
{
    public float fadeDuration = 0.5f;

    protected void ShowButtons(GameObject[] buttons)
    {
        foreach (GameObject button in buttons)
        {
            if (button != null)
            {
                button.SetActive(true);
                StartCoroutine(FadeCanvasGroup(button, 0f, 1f));
            }
        }

        Debug.Log("Botones activados con fade in.");
    }

    protected void HideButtons(GameObject[] buttons)
    {
        foreach (GameObject button in buttons)
        {
            if (button != null)
                StartCoroutine(FadeAndDisable(button));
        }

        Debug.Log("Botones ocultados con fade out.");
    }

    protected IEnumerator FadeAndDisable(GameObject obj)
    {
        yield return FadeCanvasGroup(obj, 1f, 0f);
        obj.SetActive(false);
    }

    protected IEnumerator FadeCanvasGroup(GameObject obj, float startAlpha, float endAlpha)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            float t = elapsedTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
}
