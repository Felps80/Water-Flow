using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para reiniciar a cena

public class SpikeScript : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Verifica se o jogador colidiu com os espinhos
        if (collision.collider.GetComponent<PlayerController>())
        {
            collision.collider.GetComponent<PlayerController>().Die();
        }
    }
}
