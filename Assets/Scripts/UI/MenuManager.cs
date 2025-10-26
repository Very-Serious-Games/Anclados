using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;

public class MenuManager : MonoBehaviour
{
    public GameObject[] buttonsToShow;
    public float fadeDuration = 0.5f;

    private String ip;
    private String username;

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu Scene");
    }

    public void GoToHost()
    {
        GameManager.Instance.connectionType = ConnectionType.Host;
        SceneManager.LoadScene("Host Scene");
    }

    public void GoToJoin()
    {
        GameManager.Instance.connectionType = ConnectionType.Client;
        SceneManager.LoadScene("Join Scene");
    }

    public void GoToLobby()
    {
        switch (GameManager.Instance.connectionType)
        {
            case ConnectionType.Host:

                GameManager.Instance.CreateServer();
                GameManager.Instance.networkServer.Start(7777);

                break;
            case ConnectionType.Client:

                GameManager.Instance.StartClient();
                GameManager.Instance.networkClient.Connect(ip, 7777);

                break;
            default:
                break;
        }

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

    public void OnIpTextValueChanged(String text)
    {
        ip = text;
    }

    public void OnUsernameTextValueChanged(String text)
    {
        username = text;
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