using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para reiniciar a cena

public class SpikeScript : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponent<PlayerController>();
        if (player != null)
        {
            // Agora passa a posição deste spike como fonte de dano
            player.Die(transform.position);
        }
    }
}
