using UnityEngine;

public class MobileUIController : MonoBehaviour
{
    public GameObject mobileCanvas; // Arraste o Canvas Mobile no Inspector.

    void Start()
    {
        // Verifica se est� rodando em Android ou iOS
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            mobileCanvas.SetActive(true);  // Ativa o Canvas Mobile
        }
        else
        {
           mobileCanvas.SetActive(false); // Desativa o Canvas Mobile
        }
    }
}
