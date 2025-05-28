using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    #region Variáveis de Movimento
    [SerializeField] private float velh = 5f;
    [SerializeField] private float velv = 8f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float decceleration = 2f;
    [SerializeField] private float velPower = 2f;
    [SerializeField] private int totalPulos = 1;

    // Variáveis para Raycast
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastDistance = 0.1f;

    private Rigidbody2D meuRB;
    private Animator meuAnim;
    private CapsuleCollider2D playerCollider;

    [SerializeField] private float tempoDesaceleracaoVertical = 0.3f;
    private bool noChao = false;
    private float moveInput;
    private int pulosDisponiveis;
    #endregion

    #region Variáveis de Dash
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 10f;
    private float lastDashTime = -Mathf.Infinity;
    [SerializeField] private float dashCooldownRestante;
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

    #region Variaveis Correnteza 2.0

    // Estado da correnteza: para controlar se estamos alinhando ou já empurrando
    private enum CorrentezaState { None, Aligning, Pushing }
    private CorrentezaState correntezaState = CorrentezaState.None;

    // Referência para o tile de correnteza ativo
    private CorrenteTile currentCorrenteTile = null;

    // Flag para indicar que estamos sob efeito de correnteza
    private bool isInCorrenteza = false;

    // Variáveis que armazenam a direção e velocidade vindas do tile de correnteza
    private Vector2 currentDirection = Vector2.zero;
    private float currentSpeed = 0f;

    // Parâmetros para o efeito de alinhamento (sinta-se livre para ajustar)
    [SerializeField] private float alignmentSpeed = 5f;      // Velocidade de alinhamento
    [SerializeField] private float alignmentThreshold = 0.2f;  // Distância mínima para considerar alinhado

    [SerializeField] private LayerMask correntezaLayer;  

    #endregion 


    // At the top with your other serialized variables:
    [SerializeField] private PhysicsMaterial2D groundMaterial;  // Assign a material with some friction in the Inspector
    [SerializeField] private PhysicsMaterial2D airMaterial;     // Assign your frictionless material here


    // Variáveis para adiar a aplicação do fall multiplier
    [SerializeField] private float fallDelay = 0.05f; // tempo (em segundos) que espera antes de aumentar a gravidade
    private float fallTimer = 0f;

    //Variavel da quantidade de ovos
    public int eggCount = 0;

    //Posição inicial do player
    Vector2 startPosition;

    void Start()
    {
        meuRB = GetComponent<Rigidbody2D>();
        meuAnim = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider2D>();
        pulosDisponiveis = totalPulos;
        startPosition = transform.position;

    }

    void Update()
    {
        if (!isDashing)
        {
            ControleMovimento();
            Pulando();
            CheckDash();
            dashCooldownRestante = Mathf.Max(0f, (lastDashTime + dashCooldown) - Time.time);
            Debug.Log("Cooldown do Dash: " + dashCooldownRestante.ToString("F2") + " segundos");
        }

        GerenciarCorrentezas();

        // Detecção do chão usando Raycast
        RaycastCheckGround();
    }

    void FixedUpdate()
    {
        if (isDashing || emAwaHorizontal || emAwaVertical || awaHDir || awaHEs || awaVSub || awaVBai)
        {
            if (awaVSub || awaVBai || emAwaVertical)
                meuRB.velocity = new Vector2(0f, meuRB.velocity.y); // Zera velocidade horizontal
            return;
        }

        // Atualiza o tile ativo da correnteza
        UpdateCurrentCorrenteTile();

        // Se estiver sob efeito de correnteza...
        if (isInCorrenteza && currentCorrenteTile != null)
        {
            // Fase de alinhamento
            if (correntezaState == CorrentezaState.Aligning)
            {
                // Limpa a velocidade no eixo que atrapalha o alinhamento
                Vector2 vel = meuRB.velocity;
                if (currentDirection.x != 0)  // Corrente horizontal, alinhamos verticalmente
                {
                    vel.y = 0;
                }
                else if (currentDirection.y != 0)  // Corrente vertical, alinhamos horizontalmente
                {
                    vel.x = 0;
                }
                meuRB.velocity = vel;

                // Alinha o jogador para o centro do tile
                Vector2 pos = transform.position;
                if (currentDirection == Vector2.right || currentDirection == Vector2.left)
                {
                    float targetY = currentCorrenteTile.transform.position.y;
                    pos.y = Mathf.MoveTowards(pos.y, targetY, alignmentSpeed * Time.fixedDeltaTime);
                }
                else if (currentDirection == Vector2.up || currentDirection == Vector2.down)
                {
                    float targetX = currentCorrenteTile.transform.position.x;
                    pos.x = Mathf.MoveTowards(pos.x, targetX, alignmentSpeed * Time.fixedDeltaTime);
                }
                transform.position = pos;

                // Verifica se o alinhamento foi concluído
                if (
                    (currentDirection.x != 0 && Mathf.Abs(pos.y - currentCorrenteTile.transform.position.y) < alignmentThreshold) ||
                    (currentDirection.y != 0 && Mathf.Abs(pos.x - currentCorrenteTile.transform.position.x) < alignmentThreshold)
                )
                {
                    correntezaState = CorrentezaState.Pushing;
                }

                // Enquanto estiver alinhando, não executa a movimentação manual
                return;
            }

            // Fase de empuxo (Pushing)
            if (correntezaState == CorrentezaState.Pushing)
            {
                // Desativa a gravidade e empurra o jogador na direção e velocidade definidas
                meuRB.gravityScale = 0f;
                meuRB.velocity = currentDirection * currentSpeed;
                meuAnim.SetBool("Movendo", false);
                return;
            }
        }

        UpdatePhysicsMaterial();
        MovimentoSuave();
        AtualizarGravidade();
    }

    private void UpdatePhysicsMaterial()
    {
        if (noChao)
        {
            if (playerCollider.sharedMaterial != groundMaterial)
                playerCollider.sharedMaterial = groundMaterial;
        }
        else
        {
            if (playerCollider.sharedMaterial != airMaterial)
                playerCollider.sharedMaterial = airMaterial;
        }
    }

    private void MovimentoSuave()
    {
        // Se estiver na correnteza, ignora o controle do jogador
        if (isInCorrenteza)
        {
            // Garante que a gravidade não o faça cair
            meuRB.gravityScale = 0f;
            // Aplica o movimento da corrente
            meuRB.velocity = currentDirection * currentSpeed;
            // Opcional: Atualiza a animação para um estado inerte, se desejar.
            meuAnim.SetBool("Movendo", false);
            return;
        }

        // Caso contrário, executa o movimento normal
        float targetSpeed = moveInput * velh;
        float currentSpeedLocal = meuRB.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
        float speedDifference = targetSpeed - currentSpeedLocal;
        float newSpeed = Mathf.Pow(Mathf.Abs(speedDifference), velPower) * Mathf.Sign(speedDifference);

        meuRB.velocity = new Vector2(
            Mathf.Lerp(currentSpeedLocal, currentSpeedLocal + newSpeed, accelRate * Time.fixedDeltaTime),
            meuRB.velocity.y
        );
        meuAnim.SetBool("Movendo", Mathf.Abs(moveInput) > 0);
    }

    private void AtualizarGravidade()
    {
        if (emAwaVertical)
        {
            meuRB.gravityScale = -forcaEmpurraoVertical * direcaoEmpurraoVertical;
            fallTimer = 0f;
        }
        else if (awaVSub)
        {
            meuRB.gravityScale = -forcaEmpurraoVertical;
            fallTimer = 0f;
        }
        else if (awaVBai)
        {
            meuRB.gravityScale = forcaEmpurraoVertical;
            fallTimer = 0f;
        }
        else if (!noChao)
        {
            // Se estiver caindo, acumula o tempo antes de aplicar o fall multiplier
            if (meuRB.velocity.y < 0)
            {
                fallTimer += Time.deltaTime;
                if (fallTimer >= fallDelay)
                {
                    meuRB.gravityScale = 2f;
                }
                else
                {
                    meuRB.gravityScale = 0.8f;
                }
            }
            else
            {
                // Se estiver subindo, reinicia o timer e mantém a gravidade 1
                fallTimer = 0f;
                meuRB.gravityScale = 1f;
            }
        }
        else
        {
            fallTimer = 0f;
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
        if (emAwaHorizontal || awaHDir || awaHEs || emAwaVertical || awaVSub || awaVBai)
            return;

        if (puloPressionado && pulosDisponiveis > 0)
        {
            meuRB.velocity += new Vector2(0, velv * 2f);
            pulosDisponiveis--;
            noChao = false;
            meuAnim.SetBool("NoChao", false);
            puloMobile = false;
        }
    }

    private void CheckDash()
    {
        // Se estiver na correnteza, não permite dash
        if (isInCorrenteza)
            return;

        if (emAwaHorizontal || emAwaVertical || awaHDir || awaHEs || awaVSub || awaVBai)
        {
            return;
        }


        if ((Input.GetKey(KeyCode.LeftShift) || dashMobile) && Time.time > lastDashTime + dashCooldown)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if (joystick != null && (Mathf.Abs(joystick.Horizontal) > 0.2f || Mathf.Abs(joystick.Vertical) > 0.2f))
            {
                moveX = joystick.Horizontal;
                moveY = joystick.Vertical;
            }

            // Limita os valores a apenas três direções: esquerda, direita e cima
            if (moveY > 0.5f)
            {
                dashDirection = Vector2.up;
            }
            else if (moveX > 0.5f)
            {
                dashDirection = Vector2.right;
            }
            else if (moveX < -0.5f)
            {
                dashDirection = Vector2.left;
            }
            else
            {
                return; // Nenhuma direção válida pressionada
            }

            dashMobile = false;
            StartCoroutine(Dash());
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

    // Removemos a detecção do chão via colisão para usar somente o Raycast.
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

        if (other.CompareTag("Correnteza"))
        {
            CorrenteTile tileNova = other.GetComponent<CorrenteTile>();
            if (tileNova != null)
            {
                // Se já estiver em uma corrente, mas essa nova tem direção diferente,
                // reinicia o alinhamento para a nova corrente.
                if (currentCorrenteTile != null && currentCorrenteTile != tileNova &&
                    tileNova.ObterDirecao() != currentDirection)
                {
                    currentCorrenteTile = tileNova;
                    correntezaState = CorrentezaState.Aligning;
                    currentDirection = tileNova.ObterDirecao();
                    currentSpeed = tileNova.velocidade;
                }
                else if (currentCorrenteTile == null)
                {
                    currentCorrenteTile = tileNova;
                    isInCorrenteza = true;
                    correntezaState = CorrentezaState.Aligning;
                    currentDirection = tileNova.ObterDirecao();
                    currentSpeed = tileNova.velocidade;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Correnteza"))
        {
            // Quando sair da área de correnteza, desativa o efeito
            isInCorrenteza = false;
            correntezaState = CorrentezaState.None;
            currentCorrenteTile = null;
            currentDirection = Vector2.zero;
            currentSpeed = 0f;
            meuRB.gravityScale = 1f; // Restaura a gravidade padrão
        }

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

    // Método que utiliza Raycast para detectar se o personagem está no chão.
    private void RaycastCheckGround()
    {
        // Define a origem utilizando a borda inferior central do Collider
        Vector2 rayOrigin = new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.min.y + 0.01f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, raycastDistance, groundLayer);

        if (hit.collider != null && meuRB.velocity.y <= 0)
        {
            if (!noChao)
            {
                pulosDisponiveis = totalPulos;
                noChao = true;
                meuAnim.SetBool("NoChao", true);
                Debug.Log("Detectado chão: " + hit.collider.name + " - Pulos reiniciados: " + pulosDisponiveis);
            }
        }
        else
        {
            noChao = false;
            meuAnim.SetBool("NoChao", false);
        }

    }


    //Cria um feedback visual do Raycast
    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Vector2 rayOrigin = new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.min.y + 0.01f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * raycastDistance);
        }
    }

    public void Die()
    {
        transform.position = startPosition;
        eggCount = 0; // Reset player's egg count
        Debug.Log("Player died. Egg count reset.");

        // Tell EggManager to respawn the eggs.
        if (EggManager.instance != null)
        {
            EggManager.instance.RespawnEggs();
        }
    }

    public void AddEgg()
    {
        eggCount++;   

        if (eggCount >= 3)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

    }

    #region Correnteza 2.0

    private void UpdateCurrentCorrenteTile()
    {
        // Defina o tamanho da área de checagem – ajuste esse valor conforme o tamanho do seu player/tile.
        Vector2 boxSize = new Vector2(0.5f, 0.5f);

        // Procura por todos os colliders na área ao redor do player, filtrando pelo layer de correnteza
        Collider2D[] results = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f, correntezaLayer);

        if (results.Length > 0)
        {
            CorrenteTile bestTile = null;
            float bestDistance = Mathf.Infinity;

            // Itera entre todos os colliders encontrados
            foreach (Collider2D col in results)
            {
                // Verifica se o objeto tem a tag "Correnteza" (ou você pode verificar se possui o componente CorrenteTile)
                if (col.CompareTag("Correnteza"))
                {
                    CorrenteTile tile = col.GetComponent<CorrenteTile>();
                    if (tile != null)
                    {
                        // Mede a distância entre o player e o centro do tile (usando transform.position do tile)
                        float d = Vector2.Distance(transform.position, tile.transform.position);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            bestTile = tile;
                        }
                    }
                }
            }

            if (bestTile != null)
            {
                // Se o tile atual for diferente do tile previamente selecionado,
                // reinicia o estado de alinhamento para a nova correnteza.
                if (currentCorrenteTile != bestTile)
                {
                    currentCorrenteTile = bestTile;
                    correntezaState = CorrentezaState.Aligning;
                    currentDirection = bestTile.ObterDirecao(); // supondo que essa função já esteja definida
                    currentSpeed = bestTile.velocidade;
                }
                isInCorrenteza = true;
                return;
            }
        }

        // Se nenhum tile for encontrado
        currentCorrenteTile = null;
        isInCorrenteza = false;
    }





    #endregion

}
