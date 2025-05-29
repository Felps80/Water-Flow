using UnityEngine;

public class EggScript : MonoBehaviour
{
    // Voc� pode criar eventos ou chamar m�todos espec�ficos aqui,
    // como alterar as configura��es de correnteza ou aumentar
    // a pontua��o, etc.

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se o que colidiu com o ovo � o Player (verificando a tag)
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();

            if (playerController != null)
            {
                Debug.Log("addEgg no eggscript");
                playerController.AddEgg();
            }

            else
            {
                Debug.LogWarning("PlayerController n�o encontrado no objeto Player!");
            }


            Destroy(gameObject);         
        }
    }
}
