using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float velh = 5f;
    [SerializeField] private float velv = 8f;

    [SerializeField] private float gravidade = 2f;
    [SerializeField] private float multiplicadorDeQueda = 2f;
    [SerializeField] private float multiplicadorDePulo = 0.3f;

    [SerializeField] private int totalPulos = 1;
    [SerializeField] private int pulos = 1;

    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [SerializeField] private float forcaEmpurraoHorizontal = 3f;
    [SerializeField] private float forcaEmpurraoVertical = 3f;

    private float tempoDesaceleracaoVertical = 0.3f;
    private float desaceleracaoTimer = 0f;
    private bool desacelerandoVertical = false;

    float speedAtual;
    float speedVatual;

    private Rigidbody2D meuRB;
    private Animator meuAnim;

    private float lastDashTime;
    private bool isDashing;
    private Vector2 dashDirection;

    private bool noChao = false;

    private bool emAwaHorizontal = false;
    private bool emAwaVertical = false;
    private int direcaoEmpurraoHorizontal = -1;
    private int direcaoEmpurraoVertical = 1;

    private bool awaHDir = false;
    private bool awaHEs = false;
    private bool awaVSub = false;
    private bool awaVBai = false;

    public JoystickController joystick;

    private bool puloMobile = false;
    private bool dashMobile = false;

    void Start()
    {
        meuRB = GetComponent<Rigidbody2D>();
        meuAnim = GetComponent<Animator>();
        speedAtual = velh;
        speedVatual = velv;
    }

    void Update()
    {
        bool emCorrentezaHorizontal = emAwaHorizontal || awaHDir || awaHEs;
        bool emCorrentezaVertical = emAwaVertical || awaVSub || awaVBai;
        bool podeControlar = !isDashing && !emCorrentezaHorizontal && !emCorrentezaVertical;

        if (podeControlar)
        {
            Movendo();
            Pulando();
            CheckDash();
        }

        // Correntezas horizontais
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

        // Gravidade normal
        if (!emCorrentezaVertical && !emCorrentezaHorizontal && !noChao && !isDashing)
        {
            if (meuRB.velocity.y < 0)
                meuRB.gravityScale = gravidade * multiplicadorDeQueda;
            else if (meuRB.velocity.y > 0 && !Input.GetButton("Jump"))
                meuRB.gravityScale = gravidade * multiplicadorDePulo;
            else
                meuRB.gravityScale = gravidade;
        }
        else if (noChao)
        {
            meuRB.gravityScale = 0f;
        }

        // Desaceleração vertical
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
        if (emAwaHorizontal || emAwaVertical || awaHDir || awaHEs || awaVSub || awaVBai) return;

        float horizontal = Input.GetAxis("Horizontal");

        if (joystick != null)
        {
            if (Mathf.Abs(joystick.Horizontal) > 0.2f)
            {
                horizontal = joystick.Horizontal;
            }
        }

        float movimento = horizontal * speedAtual;
        meuRB.velocity = new Vector2(movimento, meuRB.velocity.y);

        if (movimento != 0)
            transform.localScale = new Vector3(Mathf.Sign(movimento), 1f, 1f);

        meuAnim.SetBool("Movendo", movimento != 0);
    }

    private void Pulando()
    {
        if (emAwaVertical || awaVSub || awaVBai) return;

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

    private void CheckDash()
    {
        if (emAwaVertical || emAwaHorizontal || awaHDir || awaHEs || awaVSub || awaVBai) return;

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

        direcaoEmpurraoHorizontal *= -1;
        direcaoEmpurraoVertical *= -1;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("chao"))
        {
            pulos = totalPulos;
            noChao = true;
            meuAnim.SetBool("NoChao", true);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
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

    // Botões mobile
    public void PularMobile()
    {
        puloMobile = true;
    }

    public void DashMobile()
    {
        dashMobile = true;
    }
}
