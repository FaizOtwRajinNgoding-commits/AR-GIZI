using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ARMenu : MonoBehaviour
{
    public string BackMenu;
    public GameObject popupMenu;
    public GameObject buttonSwitch;
    public GameObject buttonInpo;

    public void ShowPopup()
    {
        popupMenu.SetActive(true);
    }

    public void CancelExit()
    {
        popupMenu.SetActive(false);
    }

    public void ConfirmExit()
    {
        SceneManager.LoadScene(BackMenu);
    }

    public void ShowButton()
    {
        buttonSwitch.SetActive(true);
        buttonInpo.SetActive(true);
    }

    public void HideButton()
    {
        buttonSwitch.SetActive(false);
        buttonInpo.SetActive(false);
    }
}