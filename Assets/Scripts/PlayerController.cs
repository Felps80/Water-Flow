using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MeuJogo.Correntes;

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
     // Supondo que você já tenha uma variável para armazenar a corrente em que o player está
    private CorrenteInversora correnteAtual;

    private enum InversoraState { None, Aligning, Pushing }
    private InversoraState inversoraState = InversoraState.None;

    #endregion 


    //Diferentes Materiais
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

    } //Função chamada no começo

    void Update()
    {
        if (!isDashing)
        {
            ControleMovimento();
            Pulando();
            CheckDash();
            dashCooldownRestante = Mathf.Max(0f, (lastDashTime + dashCooldown) - Time.time);
            //Debug.Log("Cooldown do Dash: " + dashCooldownRestante.ToString("F2") + " segundos");
        }

        GerenciarCorrentezas();
        UpdateCurrentCorrenteTile();
        // Detecção do chão usando Raycast
        RaycastCheckGround();
    } //Função chamada a cada frame

    void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }

        // --- SISTEMA DE CORRENTE INVERSORA ---
        if (correnteAtual != null)
        {
            // Obtem a direção atual da corrente inversora e o centro do tile
            Vector2 invDir = correnteAtual.ObterDirecao();
            Vector2 tileCenter = correnteAtual.transform.position;

            // Se ainda não foi definido, inicia o estado de alinhamento
            if (inversoraState == InversoraState.None)
            {
                inversoraState = InversoraState.Aligning;
            }

            if (inversoraState == InversoraState.Aligning)
            {
                Vector2 pos = transform.position;
                // Se a corrente age horizontalmente, alinhe verticalmente; se verticalmente, alinhe horizontalmente.
                if (Mathf.Abs(invDir.x) > 0.1f)
                {
                    pos.y = Mathf.MoveTowards(pos.y, tileCenter.y, alignmentSpeed * Time.fixedDeltaTime);
                }
                else if (Mathf.Abs(invDir.y) > 0.1f)
                {
                    pos.x = Mathf.MoveTowards(pos.x, tileCenter.x, alignmentSpeed * Time.fixedDeltaTime);
                }
                transform.position = pos;

                // Quando estiver próximo o suficiente (baseado em alignmentThreshold), passa para a fase de empuxo.
                if ((Mathf.Abs(invDir.x) > 0.1f && Mathf.Abs(pos.y - tileCenter.y) < alignmentThreshold) ||
                    (Mathf.Abs(invDir.y) > 0.1f && Mathf.Abs(pos.x - tileCenter.x) < alignmentThreshold))
                {
                    inversoraState = InversoraState.Pushing;
                }
                return; // Não processa o restante enquanto alinha.
            }

            if (inversoraState == InversoraState.Pushing)
            {
                meuRB.gravityScale = 0f;
                meuRB.velocity = invDir * correnteAtual.velocidade;
                meuAnim.SetBool("Movendo", false);
                return;
            }
        }

        // --- SISTEMA DE CORRENTEZA 2.0 (Normal) ---
        if (isInCorrenteza && currentCorrenteTile != null)
        {
            if (correntezaState == CorrentezaState.Aligning)
            {
                Vector2 vel = meuRB.velocity;
                if (currentDirection.x != 0)
                    vel.y = 0;
                else if (currentDirection.y != 0)
                    vel.x = 0;
                meuRB.velocity = vel;

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

                if ((currentDirection.x != 0 && Mathf.Abs(pos.y - currentCorrenteTile.transform.position.y) < alignmentThreshold) ||
                    (currentDirection.y != 0 && Mathf.Abs(pos.x - currentCorrenteTile.transform.position.x) < alignmentThreshold))
                {
                    correntezaState = CorrentezaState.Pushing;
                }
                return;
            }
            if (correntezaState == CorrentezaState.Pushing)
            {
                meuRB.gravityScale = 0f;
                meuRB.velocity = currentDirection * currentSpeed;
                meuAnim.SetBool("Movendo", false);
                return;
            }
        }

        UpdatePhysicsMaterial();
        MovimentoSuave();
        AtualizarGravidade();
    } //Função chamada a cada alguns frames

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
    } //Muda o material dependendo da situação

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
    } //Movimentação do player

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
    } //Aumenta a gravidade no topo

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
    }  //Controle de movimento 

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
    } //Pulo do Player

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
    } //Checa para ver se pode dar dash


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
    } //Contador de temopo do dash

    public void PularMobile()
{
    Debug.Log("Pulo Mobile apertado!");
    puloMobile = true;
}

    public void DashMobile()
    {
        dashMobile = true;
    } //Dash para mobile


    private void OnTriggerEnter2D(Collider2D other)
    {
        // Coleta de ovo: Quando o player pega um ovo, todas as correntes inversoras invertem sua direção
        if (other.CompareTag("Egg"))
        {
            GameObject[] todasCorrentes = GameObject.FindGameObjectsWithTag("CorrenteInversora");
            foreach (GameObject obj in todasCorrentes)
            {

                CorrenteInversora corrente = obj.GetComponent<CorrenteInversora>();
                if (corrente != null)
                {
                    corrente.InverterDirecao();
                }
            }
            //AddEgg();
            Destroy(other.gameObject);
        }

        // Ao entrar em um tile de CorrenteInversora, atualiza a referência e reseta o estado de alinhamento
        if (other.CompareTag("CorrenteInversora"))
        {
            CorrenteInversora novaCorrente = other.GetComponent<CorrenteInversora>();
            if (novaCorrente != null)
            {
                correnteAtual = novaCorrente;
                inversoraState = InversoraState.None;  // Será definido como Aligning no FixedUpdate
            }
        }

        // Ao entrar em um tile de Correnteza (normal)
        if (other.CompareTag("Correnteza"))
        {
            CorrenteTile tileNova = other.GetComponent<CorrenteTile>();
            if (tileNova != null)
            {
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
        // Saída de tile de Correnteza (normal)
        if (other.CompareTag("Correnteza"))
        {
            isInCorrenteza = false;
            correntezaState = CorrentezaState.None;
            currentCorrenteTile = null;
            currentDirection = Vector2.zero;
            currentSpeed = 0f;
            meuRB.gravityScale = 1f;
        }

        // Saída de tile de CorrenteInversora: limpa a referência e reseta o estado de inversora
        if (other.CompareTag("CorrenteInversora"))
        {
            CorrenteInversora ci = other.GetComponent<CorrenteInversora>();
            if (ci != null && correnteAtual == ci)
            {
                correnteAtual = null;
                inversoraState = InversoraState.None;
            }
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
    } //Gerencia as correntezas (Antigas)

    
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
                //Debug.Log("Detectado chão: " + hit.collider.name + " - Pulos reiniciados: " + pulosDisponiveis);
            }
        }
        else
        {
            noChao = false;
            meuAnim.SetBool("NoChao", false);
        }

    } //Joga um raio para baixo para checar se estamos no chão

    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Vector2 rayOrigin = new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.min.y + 0.01f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * raycastDistance);
        }
    } //Cria um feedback visual do Raycast

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
    } //Mata o Player

    public void AddEgg()
    {
        Debug.Log("antes: " + eggCount);
        eggCount++;   

        if (eggCount >= 3)
        {
            Debug.Log("dentro do if: " + eggCount);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

    } //Adiciona ovos na contagem e pula para a proxima fase

    private void UpdateCurrentCorrenteTile()
    {
        // Este método processa o sistema de correnteza normal (2.0)
        Vector2 boxSize = new Vector2(0.5f, 0.5f);
        Collider2D[] results = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f, correntezaLayer);

        if (results.Length > 0)
        {
            CorrenteTile bestTile = null;
            float bestDistance = Mathf.Infinity;
            foreach (Collider2D col in results)
            {
                if (col.CompareTag("Correnteza"))
                {
                    CorrenteTile tile = col.GetComponent<CorrenteTile>();
                    if (tile != null)
                    {
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
                if (currentCorrenteTile != bestTile)
                {
                    currentCorrenteTile = bestTile;
                    correntezaState = CorrentezaState.Aligning;
                    currentDirection = bestTile.ObterDirecao();
                    currentSpeed = bestTile.velocidade;
                }
                isInCorrenteza = true;
                return;
            }
        }
        currentCorrenteTile = null;
        isInCorrenteza = false;
    }
}
