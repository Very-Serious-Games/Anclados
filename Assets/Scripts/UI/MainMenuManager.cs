using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : BaseSceneManager
{
    public GameObject[] buttonsToShow;

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

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ShowButtons()
    {
        ShowButtons(buttonsToShow);
    }

    public void HideButtons()
    {
        HideButtons(buttonsToShow);
    }
}
