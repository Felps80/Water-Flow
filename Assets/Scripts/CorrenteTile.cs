using UnityEngine;

public enum DirecaoCorrente
{
    Esquerda,
    Direita,
    Cima,
    Baixo
}

public class CorrenteTile : MonoBehaviour
{
    [Header("Configuração da Corrente")]
    public DirecaoCorrente direcao;      // Direção definida no Inspector
    public float velocidade = 5f;        // Velocidade com que a corrente empurra o player

    

    private Vector2 correnteDirection;

    private void Start()
    {
        // Define o vetor de direção com base na escolha feita no Inspector.
        switch (direcao)
        {
            case DirecaoCorrente.Esquerda:
                correnteDirection = Vector2.left;
                break;
            case DirecaoCorrente.Direita:
                correnteDirection = Vector2.right;
                break;
            case DirecaoCorrente.Cima:
                correnteDirection = Vector2.up;
                break;
            case DirecaoCorrente.Baixo:
                correnteDirection = Vector2.down;
                break;
        }
    }

    /// <summary>
    /// Retorna o vetor direção da corrente.
    /// </summary>
    public Vector2 ObterDirecao()
    {
        return correnteDirection;
    }

    /// <summary>
    /// Retorna a posição do pivot do tile.
    /// Se pivotTile não foi atribuído, retorna o próprio transform.position.
    /// </summary>
    public Vector2 ObterPosicaoPivot()
    {
        return (Vector2)transform.position;
    }
}
