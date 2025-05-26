using UnityEngine;

public class EggScript : MonoBehaviour
{
    // Você pode criar eventos ou chamar métodos específicos aqui,
    // como alterar as configurações de correnteza ou aumentar
    // a pontuação, etc.

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se o que colidiu com o ovo é o Player (verificando a tag)
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();

            if (playerController != null)
            {
                playerController.AddEgg();
            }

            else
            {
                Debug.LogWarning("PlayerController não encontrado no objeto Player!");
            }


            Destroy(gameObject);         
        }
    }
}
