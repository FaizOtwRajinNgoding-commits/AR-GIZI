using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ARMenu : MonoBehaviour
{
    public string BackMenu;
    public GameObject popupMenu;

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
}