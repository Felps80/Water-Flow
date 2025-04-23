using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para reiniciar a cena

public class TestPlayerControler : MonoBehaviour
{
    #region Variáveis

    // Variáveis de movimento
    [SerializeField] private float velh = 5f;                 // Velocidade máxima horizontal
    [SerializeField] private float velv = 8f;                 // Força do pulo
    [SerializeField] private float acceleration = 5f;         // Taxa de aceleração
    [SerializeField] private float decceleration = 2f;        // Taxa de desaceleração
    [SerializeField] private float velPower = 2f;             // Controle da curva de aceleração

    // Variáveis de pulo
    [SerializeField] private int totalPulos = 1;              // Número máximo de pulos
    private int pulos;

    // Variáveis do raycast para detecção do chão
    [SerializeField] private LayerMask groundLayer;           // Máscara para identificar o chão
    [SerializeField] private float raycastDistance = 0.1f;      // Distância do raycast
    private bool noChao = false;

    // Variáveis de dash
    [SerializeField] private float dashSpeed = 10f;           // Velocidade do dash
    [SerializeField] private float dashDuration = 0.2f;         // Duração do dash
    [SerializeField] private float dashCooldown = 1f;           // Cooldown do dash
    private float lastDashTime = -Mathf.Infinity;             // Último tempo que o dash foi usado
    private bool isDashing = false;
    private Vector2 dashDirection;

    // Componentes e inputs
    private Rigidbody2D meuRB;
    private Animator meuAnim;
    private BoxCollider2D playerCollider;
    private float moveInput;

    // Variáveis para controle mobile
    private bool puloMobile = false;
    private bool dashMobile = false;
    public JoystickController joystick;

    #endregion Variáveis

    #region Métodos Unity

    void Start()
    {
        meuRB = GetComponent<Rigidbody2D>();
        meuAnim = GetComponent<Animator>();
        playerCollider = GetComponent<BoxCollider2D>();
        pulos = totalPulos;
    }

    void Update()
    {
        if (!isDashing)
        {
            // Captura o input horizontal
            moveInput = Input.GetAxisRaw("Horizontal");
            if (joystick != null && Mathf.Abs(joystick.Horizontal) > 0.2f)
            {
                moveInput = joystick.Horizontal;
            }
            // Impede conflito de inputs (quando A e D são pressionados simultaneamente)
            if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D))
            {
                moveInput = 0;
            }

            // Atualiza a escala do player conforme a direção do movimento
            if (moveInput > 0)
                transform.localScale = new Vector3(1f, 1f, 1f);
            else if (moveInput < 0)
                transform.localScale = new Vector3(-1f, 1f, 1f);

            // Checa pulo e dash
            Pulando();
            CheckDash();
        }

        // Checa se o player está no chão via Raycast
        RaycastCheckGround();
    }

    void FixedUpdate()
    {
        if (isDashing)
            return;

        // Movimento horizontal suave
        float targetSpeed = moveInput * velh;
        float currentSpeed = meuRB.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
        float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);
        meuRB.velocity = new Vector2(newSpeed, meuRB.velocity.y);

        // Atualiza animação de movimento
        meuAnim.SetBool("Movendo", Mathf.Abs(moveInput) > 0);

        // Ajuste da gravidade: aumenta quando o player está caindo e não está no chão
        if (!noChao && meuRB.velocity.y < 0)
            meuRB.gravityScale = 2f;
        else
            meuRB.gravityScale = 1f;
    }

    #endregion Métodos Unity

    #region Métodos de Movimento (Pulo)

    private void Pulando()
    {
        // Detecta input de pulo: botão "Jump", joystick ou botões mobile
        bool puloPressionado = Input.GetButtonDown("Jump")
                               || (joystick != null && joystick.Vertical > 0.5f)
                               || puloMobile;

        if (puloPressionado && pulos > 0)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, velv);
            pulos--; // Decrementa os pulos disponíveis
            noChao = false;
            meuAnim.SetBool("NoChao", false);
            puloMobile = false;
            Debug.Log("Pulou! Pulos restantes: " + pulos);
        }
    }

    #endregion Métodos de Movimento (Pulo)

    #region Métodos de Dash

    private void CheckDash()
    {
        // Permite dash se a tecla LeftShift (ou input mobile) for pressionada e o cooldown tiver expirado
        if ((Input.GetKey(KeyCode.LeftShift) || dashMobile) && Time.time > lastDashTime + dashCooldown)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if (joystick != null)
            {
                if (Mathf.Abs(joystick.Horizontal) > 0.2f || Mathf.Abs(joystick.Vertical) > 0.2f)
                {
                    moveX = joystick.Horizontal;
                    moveY = joystick.Vertical;
                }
            }

            if (moveX != 0 || moveY != 0)
            {
                dashDirection = new Vector2(moveX, moveY).normalized;
                dashMobile = false;
                StartCoroutine(Dash());
            }
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            // Durante o dash, a velocidade é forçada na direção de dash
            meuRB.velocity = dashDirection * dashSpeed;
            yield return null;
        }

        // Ao final do dash, zera a velocidade e retorna o controle normal
        meuRB.velocity = Vector2.zero;
        isDashing = false;
        yield break;
    }

    #endregion Métodos de Dash

    #region Raycast para Detecção do Chão

    private void RaycastCheckGround()
    {
        // Define a origem do raycast na parte inferior do BoxCollider2D
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - playerCollider.bounds.extents.y + 0.01f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, raycastDistance, groundLayer);

        // Se o raycast detectar o chão e o player estiver caindo ou parado, reinicia os pulos
        if (hit.collider != null && meuRB.velocity.y <= 0)
        {
            if (!noChao)
            {
                pulos = totalPulos;
                noChao = true;
                meuAnim.SetBool("NoChao", true);
                Debug.Log("Detectado chão: " + hit.collider.name + " - Pulos reiniciados: " + pulos);
            }
        }
        else
        {
            noChao = false;
            meuAnim.SetBool("NoChao", false);
        }
    }

    #endregion Raycast para Detecção do Chão

    #region Gizmos (Para Debug Visual)

    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - playerCollider.bounds.extents.y + 0.01f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * raycastDistance);
        }
    }

    #endregion Gizmos

    #region Controles Mobile

    public void PularMobile()
    {
        puloMobile = true;
    }

    public void DashMobile()
    {
        dashMobile = true;
    }

    #endregion Controles Mobile

    #region Detecção de Espinhos e Morte

    // Quando o player entra em contato com um objeto com tag "espinho", reinicia a fase.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("espinho"))
        {
            Debug.Log("Espinho detectado! Reiniciando a fase...");
            RestartLevel();
        }
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion Detecção de Espinhos e Morte
}