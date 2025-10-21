using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    public GameObject[] buttonsToShow;
    public float fadeDuration = 0.5f;

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu Scene");
    }
    
    public void GoToLobby()
    {
        SceneManager.LoadScene("Lobby Scene");
    }

    public void GoToGame()
    {
        SceneManager.LoadScene("Game Scene");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ShowButtons()
    {
        foreach (GameObject button in buttonsToShow)
        {
            if (button != null)
            {
                button.SetActive(true);
                StartCoroutine(FadeCanvasGroup(button, 0f, 1f));
            }
        }

        Debug.Log("Botones activados con fade in.");
    }

    public void HideButtons()
    {
        foreach (GameObject button in buttonsToShow)
        {
            if (button != null)
                StartCoroutine(FadeAndDisable(button));
        }

        Debug.Log("Botones ocultados con fade out.");
    }

    private IEnumerator FadeAndDisable(GameObject obj)
    {
        yield return FadeCanvasGroup(obj, 1f, 0f);
        obj.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(GameObject obj, float startAlpha, float endAlpha)
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