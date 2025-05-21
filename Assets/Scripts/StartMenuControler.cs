using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuControler : MonoBehaviour
{
    public void StartMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
