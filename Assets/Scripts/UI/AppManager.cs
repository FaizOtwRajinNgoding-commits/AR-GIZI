using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppManager : MonoBehaviour
{
	public string ARScene;

    public string GameScene;


    public void MulaiAR()
        {
            SceneManager.LoadScene(ARScene);
        }

    public void MulaiGame()
    {
        SceneManager.LoadScene(GameScene);
    }
	
	public void KeluarAplikasi()
	{
		Application.Quit();
	}

}
