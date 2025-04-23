using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Variáveis de Movimento
    [SerializeField] private float velh = 5f;                 // Velocidade máxima horizontal
    [SerializeField] private float velv = 8f;                 // Força do pulo
    [SerializeField] private float acceleration = 5f;         // Taxa de aceleração
    [SerializeField] private float decceleration = 2f;        // Taxa de desaceleração

    [SerializeField] private float velPower = 2f; // Ajustável no editor Unity


    [SerializeField] private int totalPulos = 1;              // Número máximo de pulos
    private int pulos;

    private Rigidbody2D meuRB;
    private Animator meuAnim;
    private BoxCollider2D playerCollider;

    [SerializeField] private LayerMask groundLayer;           // Máscara para identificar o chão
    [SerializeField] private float raycastDistance = 0.1f;    // Distância do raycast
    private bool noChao = false;
    private float moveInput;
    #endregion

    #region Variáveis de Dash
    [SerializeField] private float dashSpeed = 10f;           // Velocidade do dash
    [SerializeField] private float dashDuration = 0.2f;       // Duração do dash
    [SerializeField] private float dashCooldown = 1f;         // Cooldown do dash
    private float lastDashTime = -Mathf.Infinity;             // Último tempo que o dash foi usado
    private bool isDashing = false;
    private Vector2 dashDirection;

    private bool puloMobile = false;
    private bool dashMobile = false;
    public JoystickController joystick;
    #endregion

    #region Variáveis de Correnteza
    [SerializeField] private float forcaEmpurraoHorizontal = 3f;
    [SerializeField] private float forcaEmpurraoVertical = 3f;

    private bool emAwaHorizontal = false;
    private bool emAwaVertical = false;
    private int direcaoEmpurraoHorizontal = -1;
    private int direcaoEmpurraoVertical = 1;

    private bool awaHDir = false;
    private bool awaHEs = false;
    private bool awaVSub = false;
    private bool awaVBai = false;

    private float tempoDesaceleracaoVertical = 0.3f;
    private float desaceleracaoTimer = 0f;
    private bool desacelerandoVertical = false;
    #endregion

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
            ControleMovimento();
            Pulando();
            CheckDash();
        }

        RaycastCheckGround();
        GerenciarCorrentezas();
    }

    void FixedUpdate()
    {
        if (isDashing || emAwaHorizontal || emAwaVertical || awaHDir || awaHEs || awaVSub || awaVBai)
            return;

        MovimentoSuave();
        AtualizarGravidade();
    }

    #region Movimento Suave
private void MovimentoSuave()
{
    float targetSpeed = moveInput * velh;
    float currentSpeed = meuRB.velocity.x;

    // Adiciona proteção para valores inválidos de velPower
    if (velPower <= 0)
    {
        Debug.LogError("O valor de velPower deve ser maior que zero.");
        velPower = 2f; // Define um valor padrão seguro
    }

    float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
    float speedDifference = targetSpeed - currentSpeed;

    // Calcula newSpeed apenas se velPower for válido
    float newSpeed = Mathf.Pow(Mathf.Abs(speedDifference), velPower) * Mathf.Sign(speedDifference);

    // Interpola a velocidade ajustada para suavizar o movimento
    meuRB.velocity = new Vector2(
        Mathf.Lerp(currentSpeed, currentSpeed + newSpeed, accelRate * Time.fixedDeltaTime), 
        meuRB.velocity.y
    );

    meuAnim.SetBool("Movendo", Mathf.Abs(moveInput) > 0);
}
#endregion


    #region Gravidade e Correntezas
private void AtualizarGravidade()
{
    if (emAwaVertical)
    {
        meuRB.gravityScale = -forcaEmpurraoVertical * direcaoEmpurraoVertical; // Aplica a força de empurrão vertical
    }
    else if (awaVSub)
    {
        meuRB.gravityScale = -forcaEmpurraoVertical; // Força de subida controlada
    }
    else if (awaVBai)
    {
        meuRB.gravityScale = forcaEmpurraoVertical; // Força de descida controlada
    }
    else if (!noChao && meuRB.velocity.y < 0)
    {
        meuRB.gravityScale = 2f; // Gravidade aumentada durante queda
    }
    else
    {
        meuRB.gravityScale = 1f; // Gravidade normal
    }

    if (desacelerandoVertical)
    {
        desaceleracaoTimer += Time.deltaTime;
        float t = desaceleracaoTimer / tempoDesaceleracaoVertical;
        float novaVelY = Mathf.Lerp(meuRB.velocity.y, 0f, t);
        meuRB.velocity = new Vector2(meuRB.velocity.x, novaVelY);

        if (desaceleracaoTimer >= tempoDesaceleracaoVertical)
        {
            desacelerandoVertical = false;
            meuRB.gravityScale = 1f;
        }
    }
}

private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("awaHorizontal")) emAwaHorizontal = true;
    if (other.CompareTag("awaVertical")) emAwaVertical = true;

    if (other.CompareTag("awaHDir")) awaHDir = true;
    if (other.CompareTag("awaHEs")) awaHEs = true;
    if (other.CompareTag("awaVSub")) awaVSub = true;
    if (other.CompareTag("awaVBai")) awaVBai = true;
}

private void OnTriggerExit2D(Collider2D other)
{
    if (other.CompareTag("awaHorizontal")) emAwaHorizontal = false;
    if (other.CompareTag("awaVertical")) emAwaVertical = false;

    if (other.CompareTag("awaHDir")) awaHDir = false;
    if (other.CompareTag("awaHEs")) awaHEs = false;
    if (other.CompareTag("awaVSub")) awaVSub = false;
    if (other.CompareTag("awaVBai")) awaVBai = false;
}


private void GerenciarCorrentezas()
{
    // Correntezas Horizontais
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

    // Correntezas Verticais
    if (emAwaVertical)
    {
        meuRB.velocity = new Vector2(meuRB.velocity.x, forcaEmpurraoVertical * direcaoEmpurraoVertical); // Velocidade vertical ajustada
    }
    else if (awaVSub)
    {
        meuRB.velocity = new Vector2(meuRB.velocity.x, forcaEmpurraoVertical); // Empurrando para cima
    }
    else if (awaVBai)
    {
        meuRB.velocity = new Vector2(meuRB.velocity.x, -forcaEmpurraoVertical); // Empurrando para baixo
    }
}
#endregion


    #region Controle Movimento e Input
    private void ControleMovimento()
{
    moveInput = Input.GetAxisRaw("Horizontal");
    if (joystick != null && Mathf.Abs(joystick.Horizontal) > 0.2f)
    {
        moveInput = joystick.Horizontal;
    }

    // Atualiza a direção sem alterar o tamanho
    if (moveInput > 0)
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    else if (moveInput < 0)
        transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
}


    private void Pulando()
    {
        bool puloPressionado = Input.GetButtonDown("Jump") || puloMobile;

        if (puloPressionado && pulos > 0)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, velv);
            pulos--;
            noChao = false;
            meuAnim.SetBool("NoChao", false);
            puloMobile = false;
        }
    }
    #endregion

    #region Dash
    private void CheckDash()
    {
        if ((Input.GetKey(KeyCode.LeftShift) || dashMobile) && Time.time > lastDashTime + dashCooldown)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if (joystick != null && (Mathf.Abs(joystick.Horizontal) > 0.2f || Mathf.Abs(joystick.Vertical) > 0.2f))
            {
                moveX = joystick.Horizontal;
                moveY = joystick.Vertical;
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
            meuRB.velocity = dashDirection * dashSpeed;
            yield return null;
        }

        meuRB.velocity = Vector2.zero;
        isDashing = false;
    }
    #endregion

    #region Controles Mobile
    public void PularMobile()
    {
        puloMobile = true;
        pulos = totalPulos; // Reseta o número de pulos
    }

    public void DashMobile()
    {
        dashMobile = true;
    }
    #endregion

    private void RaycastCheckGround()
{
    // Define a origem do raycast na parte inferior do BoxCollider2D
    Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - playerCollider.bounds.extents.y + 0.01f);
    RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, raycastDistance, groundLayer);

    // Verifica se o raycast detecta o chão
    if (hit.collider != null && meuRB.velocity.y <= 0)
    {
        if (!noChao)
        {
            pulos = totalPulos; // Reseta os pulos disponíveis
            noChao = true;
            meuAnim.SetBool("NoChao", true);
        }
    }
    else
    {
        noChao = false;
        meuAnim.SetBool("NoChao", false);
    }
}

}
