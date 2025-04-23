using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Necess�rio para reiniciar a cena

public class TestPlayerControler : MonoBehaviour
{
    #region Vari�veis

    // Vari�veis de movimento
    [SerializeField] private float velh = 5f;                 // Velocidade m�xima horizontal
    [SerializeField] private float velv = 8f;                 // For�a do pulo
    [SerializeField] private float acceleration = 5f;         // Taxa de acelera��o
    [SerializeField] private float decceleration = 2f;        // Taxa de desacelera��o
    [SerializeField] private float velPower = 2f;             // Controle da curva de acelera��o

    // Vari�veis de pulo
    [SerializeField] private int totalPulos = 1;              // N�mero m�ximo de pulos
    private int pulos;

    // Vari�veis do raycast para detec��o do ch�o
    [SerializeField] private LayerMask groundLayer;           // M�scara para identificar o ch�o
    [SerializeField] private float raycastDistance = 0.1f;      // Dist�ncia do raycast
    private bool noChao = false;

    // Vari�veis de dash
    [SerializeField] private float dashSpeed = 10f;           // Velocidade do dash
    [SerializeField] private float dashDuration = 0.2f;         // Dura��o do dash
    [SerializeField] private float dashCooldown = 1f;           // Cooldown do dash
    private float lastDashTime = -Mathf.Infinity;             // �ltimo tempo que o dash foi usado
    private bool isDashing = false;
    private Vector2 dashDirection;

    // Componentes e inputs
    private Rigidbody2D meuRB;
    private Animator meuAnim;
    private BoxCollider2D playerCollider;
    private float moveInput;

    // Vari�veis para controle mobile
    private bool puloMobile = false;
    private bool dashMobile = false;
    public JoystickController joystick;

    #endregion Vari�veis

    #region M�todos Unity

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
            // Impede conflito de inputs (quando A e D s�o pressionados simultaneamente)
            if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D))
            {
                moveInput = 0;
            }

            // Atualiza a escala do player conforme a dire��o do movimento
            if (moveInput > 0)
                transform.localScale = new Vector3(1f, 1f, 1f);
            else if (moveInput < 0)
                transform.localScale = new Vector3(-1f, 1f, 1f);

            // Checa pulo e dash
            Pulando();
            CheckDash();
        }

        // Checa se o player est� no ch�o via Raycast
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

        // Atualiza anima��o de movimento
        meuAnim.SetBool("Movendo", Mathf.Abs(moveInput) > 0);

        // Ajuste da gravidade: aumenta quando o player est� caindo e n�o est� no ch�o
        if (!noChao && meuRB.velocity.y < 0)
            meuRB.gravityScale = 2f;
        else
            meuRB.gravityScale = 1f;
    }

    #endregion M�todos Unity

    #region M�todos de Movimento (Pulo)

    private void Pulando()
    {
        // Detecta input de pulo: bot�o "Jump", joystick ou bot�es mobile
        bool puloPressionado = Input.GetButtonDown("Jump")
                               || (joystick != null && joystick.Vertical > 0.5f)
                               || puloMobile;

        if (puloPressionado && pulos > 0)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, velv);
            pulos--; // Decrementa os pulos dispon�veis
            noChao = false;
            meuAnim.SetBool("NoChao", false);
            puloMobile = false;
            Debug.Log("Pulou! Pulos restantes: " + pulos);
        }
    }

    #endregion M�todos de Movimento (Pulo)

    #region M�todos de Dash

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
            // Durante o dash, a velocidade � for�ada na dire��o de dash
            meuRB.velocity = dashDirection * dashSpeed;
            yield return null;
        }

        // Ao final do dash, zera a velocidade e retorna o controle normal
        meuRB.velocity = Vector2.zero;
        isDashing = false;
        yield break;
    }

    #endregion M�todos de Dash

    #region Raycast para Detec��o do Ch�o

    private void RaycastCheckGround()
    {
        // Define a origem do raycast na parte inferior do BoxCollider2D
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - playerCollider.bounds.extents.y + 0.01f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, raycastDistance, groundLayer);

        // Se o raycast detectar o ch�o e o player estiver caindo ou parado, reinicia os pulos
        if (hit.collider != null && meuRB.velocity.y <= 0)
        {
            if (!noChao)
            {
                pulos = totalPulos;
                noChao = true;
                meuAnim.SetBool("NoChao", true);
                Debug.Log("Detectado ch�o: " + hit.collider.name + " - Pulos reiniciados: " + pulos);
            }
        }
        else
        {
            noChao = false;
            meuAnim.SetBool("NoChao", false);
        }
    }

    #endregion Raycast para Detec��o do Ch�o

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

    #region Detec��o de Espinhos e Morte

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

    #endregion Detec��o de Espinhos�e�Morte
}