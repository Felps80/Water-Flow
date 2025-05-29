using UnityEngine;

namespace MeuJogo.Correntes
{
    // Nosso enum de direção, exclusivo para as correntes do jogo.
    public enum DirecaoCorrente
    {
        Esquerda,
        Direita,
        Cima,
        Baixo
    }

    public class CorrenteInversora : MonoBehaviour
    {
        [Header("Configuração da Corrente Inversora")]
        public DirecaoCorrente direcao;   // Direção padrão definida no Inspector
        public float velocidade = 5f;     // Velocidade com que a corrente empurra o player

        private Vector2 correnteDirection;

        private void Start()
        {
            AtualizarDirecao();
        }

        // Atualiza o vetor de direção com base na direção definida
        private void AtualizarDirecao()
        {
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
        /// Retorna o vetor de direção atual da corrente.
        /// </summary>
        public Vector2 ObterDirecao()
        {
            return correnteDirection;
        }

        /// <summary>
        /// Inverte a direção da corrente.
        /// Por exemplo, se estiver para a direita, passa a ser para a esquerda e vice-versa.
        /// </summary>
        public void InverterDirecao()
        {
            // Inverter o vetor de direção
            correnteDirection = -correnteDirection;

            // Atualiza o enum para refletir a nova direção
            switch (direcao)
            {
                case DirecaoCorrente.Esquerda:
                    direcao = DirecaoCorrente.Direita;
                    break;
                case DirecaoCorrente.Direita:
                    direcao = DirecaoCorrente.Esquerda;
                    break;
                case DirecaoCorrente.Cima:
                    direcao = DirecaoCorrente.Baixo;
                    break;
                case DirecaoCorrente.Baixo:
                    direcao = DirecaoCorrente.Cima;
                    break;
            }
        }
    }
}
