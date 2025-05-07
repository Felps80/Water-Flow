using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Variáveis de Movimento
    [SerializeField] private float velh = 5f;
    [SerializeField] private float velv = 8f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float decceleration = 2f;
    [SerializeField] private float velPower = 2f;
    [SerializeField] private int totalPulos = 1;

    private Rigidbody2D meuRB;
    private Animator meuAnim;
    private BoxCollider2D playerCollider;

    [SerializeField] private float tempoDesaceleracaoVertical = 0.3f;
    private bool noChao = false;
    private float moveInput;
    private int pulosDisponiveis;
    #endregion

    #region Variáveis de Dash
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private float lastDashTime = -Mathf.Infinity;
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

    private float desaceleracaoTimer = 0f;
    private bool desacelerandoVertical = false;
    #endregion

    void Start()
    {
        meuRB = GetComponent<Rigidbody2D>();
        meuAnim = GetComponent<Animator>();
        playerCollider = GetComponent<BoxCollider2D>();
        pulosDisponiveis = totalPulos;
    }

    void Update()
    {
        if (!isDashing)
        {
            ControleMovimento();
            Pulando();
            CheckDash();
        }

        GerenciarCorrentezas();
    }

    void FixedUpdate()
    {
        if (isDashing || emAwaHorizontal || emAwaVertical || awaHDir || awaHEs || awaVSub || awaVBai)
        {
            if (awaVSub || awaVBai || emAwaVertical)
                meuRB.velocity = new Vector2(0f, meuRB.velocity.y); // Zera velocidade horizontal
            return;
        }

        MovimentoSuave();
        AtualizarGravidade();
    }

    private void MovimentoSuave()
    {
        float targetSpeed = moveInput * velh;
        float currentSpeed = meuRB.velocity.x;

        if (velPower <= 0)
        {
            Debug.LogError("O valor de velPower deve ser maior que zero.");
            velPower = 2f;
        }

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
        float speedDifference = targetSpeed - currentSpeed;
        float newSpeed = Mathf.Pow(Mathf.Abs(speedDifference), velPower) * Mathf.Sign(speedDifference);

        meuRB.velocity = new Vector2(
            Mathf.Lerp(currentSpeed, currentSpeed + newSpeed, accelRate * Time.fixedDeltaTime),
            meuRB.velocity.y
        );

        meuAnim.SetBool("Movendo", Mathf.Abs(moveInput) > 0);
    }

    private void AtualizarGravidade()
    {
        if (emAwaVertical)
        {
            meuRB.gravityScale = -forcaEmpurraoVertical * direcaoEmpurraoVertical;
        }
        else if (awaVSub)
        {
            meuRB.gravityScale = -forcaEmpurraoVertical;
        }
        else if (awaVBai)
        {
            meuRB.gravityScale = forcaEmpurraoVertical;
        }
        else if (!noChao && meuRB.velocity.y < 0)
        {
            meuRB.gravityScale = 2f;
        }
        else
        {
            meuRB.gravityScale = 1f;
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

    private void ControleMovimento()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        if (joystick != null && Mathf.Abs(joystick.Horizontal) > 0.2f)
        {
            moveInput = joystick.Horizontal;
        }

        if (moveInput > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void Pulando()
    {
        bool puloPressionado = (Input.GetKeyDown(KeyCode.Space) || puloMobile);

        // Não permite pulo em correntezas
        if ((emAwaHorizontal || awaHDir || awaHEs || emAwaVertical || awaVSub || awaVBai)) return;

        if (puloPressionado && pulosDisponiveis > 0)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, velv);
            pulosDisponiveis--;
            noChao = false;
            meuAnim.SetBool("NoChao", false);
            puloMobile = false;
        }
    }

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

    public void PularMobile()
    {
        puloMobile = true;
        pulosDisponiveis = totalPulos;
    }

    public void DashMobile()
    {
        dashMobile = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Colidiu com: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Chao"))
        {
            Debug.Log("Está no chão!");
            noChao = true;
            pulosDisponiveis = totalPulos;
            meuAnim.SetBool("NoChao", true);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("Saiu da colisão com: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Chao"))
        {
            Debug.Log("Saiu do chão!");
            noChao = false;
            meuAnim.SetBool("NoChao", false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ovocelente"))
        {
            direcaoEmpurraoHorizontal *= -1;
            direcaoEmpurraoVertical *= -1;
            Destroy(other.gameObject);
        }

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

        if (other.CompareTag("awaVertical") || other.CompareTag("awaVSub") || other.CompareTag("awaVBai"))
        {
            desacelerandoVertical = true;
            desaceleracaoTimer = 0f;
        }
    }

    private void GerenciarCorrentezas()
    {
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

        if (emAwaVertical)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, forcaEmpurraoVertical * direcaoEmpurraoVertical);
        }
        else if (awaVSub)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, forcaEmpurraoVertical);
        }
        else if (awaVBai)
        {
            meuRB.velocity = new Vector2(meuRB.velocity.x, -forcaEmpurraoVertical);
        }
    }
}
