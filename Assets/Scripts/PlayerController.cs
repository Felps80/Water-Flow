using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Velocidade de movimento horizontal e força do pulo vertical
    [SerializeField] private float velh = 5f;
    [SerializeField] private float velv = 8f;

    // Gravidade e multiplicadores usados para controlar a sensação do pulo e queda
    [SerializeField] private float gravidade = 2f;
    [SerializeField] private float multiplicadorDeQueda = 2f;
    [SerializeField] private float multiplicadorDePulo = 0.3f;

    // Quantidade de pulos disponíveis (pulo duplo, etc.)
    [SerializeField] private int totalPulos = 1;
    [SerializeField] private int pulos = 1;

    // Parâmetros do dash
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    // Força das correntezas horizontais e verticais
    [SerializeField] private float forcaEmpurraoHorizontal = 3f;
    [SerializeField] private float forcaEmpurraoVertical = 3f;

    // Controle de desaceleração ao sair das correntezas verticais
    private float tempoDesaceleracaoVertical = 0.3f;
    private float desaceleracaoTimer = 0f;
    private bool desacelerandoVertical = false;

    // Velocidade atual (para modificar se quiser power-ups etc.)
    float speedAtual;
    float speedVatual;

    // Referências aos componentes
    private Rigidbody2D meuRB;
    private Animator meuAnim;

    // Variáveis de controle do dash
    private float lastDashTime;
    private bool isDashing;
    private Vector2 dashDirection;

    // Verifica se está no chão
    private bool noChao = false;

    // Correntezas dinâmicas
    private bool emAwaHorizontal = false;
    private bool emAwaVertical = false;
    private int direcaoEmpurraoHorizontal = -1;
    private int direcaoEmpurraoVertical = 1;

    // Correntezas fixas
    private bool awaHDir = false;
    private bool awaHEs = false;
    private bool awaVSub = false;
    private bool awaVBai = false;

    void Start()
    {
        // Referências ao Rigidbody e Animator
        meuRB = GetComponent<Rigidbody2D>();
        meuAnim = GetComponent<Animator>();
        speedAtual = velh;
        speedVatual = velv;
    }

    void Update()
    {
        // Verifica se está em alguma correnteza
        bool emCorrentezaHorizontal = emAwaHorizontal || awaHDir || awaHEs;
        bool emCorrentezaVertical = emAwaVertical || awaVSub || awaVBai;

        // Só pode controlar se não estiver em correntezas nem dashing
        bool podeControlar = !isDashing && !emCorrentezaHorizontal && !emCorrentezaVertical;

        if (podeControlar)
        {
            Movendo();  // Movimento horizontal
            Pulando();  // Pulo
            CheckDash(); // Dash
        }

        // Correntezas horizontais dinâmicas e fixas
        if (emAwaHorizontal)
        {
            meuRB.velocity = new Vector2(forcaEmpurraoHorizontal * direcaoEmpurraoHorizontal, meuRB.velocity.y);
            meuRB.gravityScale = 0f;
        }
        else if (awaHDir)
        {
            meuRB.velocity = new Vector2(forcaEmpurraoHorizontal, meuRB.velocity.y);
            meuRB.gravityScale = 0f;
        }
        else if (awaHEs)
        {
            meuRB.velocity = new Vector2(-forcaEmpurraoHorizontal, meuRB.velocity.y);
            meuRB.gravityScale = 0f;
        }

        // Correntezas verticais
        if (emAwaVertical)
        {
            meuRB.gravityScale = -gravidade * direcaoEmpurraoVertical;
        }
        else if (awaVSub)
        {
            meuRB.gravityScale = -gravidade;
        }
        else if (awaVBai)
        {
            meuRB.gravityScale = gravidade;
        }

        // Gravidade normal fora das correntezas
        if (!emCorrentezaVertical && !emCorrentezaHorizontal && !noChao && !isDashing)
        {
            if (meuRB.velocity.y < 0)
                meuRB.gravityScale = gravidade * multiplicadorDeQueda; // Cai mais rápido
            else if (meuRB.velocity.y > 0 && !Input.GetButton("Jump"))
                meuRB.gravityScale = gravidade * multiplicadorDePulo; // Soltou o botão, sobe menos
            else
                meuRB.gravityScale = gravidade; // Gravidade normal subindo
        }
        else if (noChao)
        {
            meuRB.gravityScale = 0f; // Sem gravidade no chão
        }

        // Desaceleração suave ao sair de uma correnteza vertical
        if (desacelerandoVertical)
        {
            desaceleracaoTimer += Time.deltaTime;
            float t = desaceleracaoTimer / tempoDesaceleracaoVertical;
            float novaVelY = Mathf.Lerp(meuRB.velocity.y, 0f, t);
            meuRB.velocity = new Vector2(meuRB.velocity.x, novaVelY);

            if (desaceleracaoTimer >= tempoDesaceleracaoVertical)
            {
                desacelerandoVertical = false;
                meuRB.gravityScale = gravidade;
            }
        }
    }

    private void Movendo()
    {
        // Se estiver em correnteza, não move manualmente
        if (emAwaHorizontal || emAwaVertical || awaHDir || awaHEs || awaVSub || awaVBai) return;

        float movimento = Input.GetAxis("Horizontal") * speedAtual;
        meuRB.velocity = new Vector2(movimento, meuRB.velocity.y);

        // Vira o sprite na direção que está indo
        if (movimento != 0)
            transform.localScale = new Vector3(Mathf.Sign(movimento), 1f, 1f);

        // Animação de movimento
        meuAnim.SetBool("Movendo", movimento != 0);
    }

    private void Pulando()
    {
        // Impede de pular em correntezas verticais
        if (emAwaVertical || awaVSub || awaVBai) return;

        bool puloPressionado = Input.GetButtonDown("Jump");

        if (puloPressionado && pulos > 0)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, velv); // Aplica força do pulo
            pulos--; // Gasta um pulo
            noChao = false;
            meuAnim.SetBool("NoChao", false); // Atualiza animação
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Reseta os pulos ao tocar no chão
        if (collision.gameObject.CompareTag("chao"))
        {
            pulos = totalPulos;
            noChao = true;
            meuAnim.SetBool("NoChao", true);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Ativa correntezas
        if (other.CompareTag("awaHorizontal")) emAwaHorizontal = true;
        if (other.CompareTag("awaVertical")) emAwaVertical = true;

        if (other.CompareTag("awaHDir")) awaHDir = true;
        if (other.CompareTag("awaHEs")) awaHEs = true;
        if (other.CompareTag("awaVSub")) awaVSub = true;
        if (other.CompareTag("awaVBai")) awaVBai = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Saiu das correntezas
        if (other.CompareTag("awaHorizontal")) emAwaHorizontal = false;
        if (other.CompareTag("awaVertical"))
        {
            emAwaVertical = false;
            desacelerandoVertical = true;
            desaceleracaoTimer = 0f;
        }

        if (other.CompareTag("awaHDir")) awaHDir = false;
        if (other.CompareTag("awaHEs")) awaHEs = false;

        if (other.CompareTag("awaVSub") || other.CompareTag("awaVBai"))
        {
            if (other.CompareTag("awaVSub")) awaVSub = false;
            if (other.CompareTag("awaVBai")) awaVBai = false;

            desacelerandoVertical = true;
            desaceleracaoTimer = 0f;
        }
    }

    private void CheckDash()
    {
        // Não pode usar dash em correntezas
        if (emAwaVertical || emAwaHorizontal || awaHDir || awaHEs || awaVSub || awaVBai) return;

        if (Input.GetKey(KeyCode.LeftShift) && Time.time > lastDashTime + dashCooldown)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if (moveX != 0 || moveY != 0)
            {
                dashDirection = new Vector2(moveX, moveY).normalized;
                StartCoroutine(Dash());
            }
        }
    }

    private System.Collections.IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            meuRB.velocity = dashDirection * dashSpeed;
            yield return null;
        }

        meuRB.velocity = Vector2.zero;
        isDashing = false;

        // Inverte a direção das correntezas dinâmicas
        direcaoEmpurraoHorizontal *= -1;
        direcaoEmpurraoVertical *= -1;
    }
}
