using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenuControler : MonoBehaviour
{
    public GameObject TelaCreditos;
    
    public void OnStartClick()
    {
        SceneManager.LoadScene("J2 Scene");
    }

    public void OnExitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    

}
