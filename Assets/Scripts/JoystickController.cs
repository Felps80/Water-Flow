using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referências do Joystick")]
    [SerializeField] private RectTransform joystickBackground;  // Arraste aqui o fundo do joystick
    [SerializeField] private RectTransform joystickHandle;      // Arraste aqui a alça do joystick

    private Vector2 inputVector;

<<<<<<< Updated upstream
    // Getters públicos para acesso externo
    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => inputVector;
=======
    [SerializeField]
    private float deadZone = 0.1f; // Evita movimentações falsas

    [SerializeField]
    private float returnSpeed = 10f; // Velocidade de retorno suave

    public float Horizontal => ProcessedDirection.x;
    public float Vertical => ProcessedDirection.y;
    public Vector2 Direction => new Vector2(Horizontal, Vertical);
>>>>>>> Stashed changes

    private Vector2 ProcessedDirection
    {
        get
        {
            return inputVector.magnitude < deadZone ? Vector2.zero : inputVector;
        }
    }

    private void Start()
    {
        if (joystickBackground == null || joystickHandle == null)
        {
            Debug.LogError("JoystickController: Referências não atribuídas no Inspector!");
        }
        else if (joystickBackground.sizeDelta == Vector2.zero)
        {
            Debug.LogWarning("JoystickController: 'sizeDelta' do background é zero, verifique o RectTransform.");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out pos))
        {
<<<<<<< Updated upstream
            // Normaliza a posição dentro do raio do joystick
            pos.x = pos.x / (joystickBackground.sizeDelta.x / 2);
            pos.y = pos.y / (joystickBackground.sizeDelta.y / 2);

            inputVector = new Vector2(pos.x, pos.y);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;
=======
            // Normaliza para -1 a 1
            pos.x = (pos.x / joystickBackground.sizeDelta.x) * 2;
            pos.y = (pos.y / joystickBackground.sizeDelta.y) * 2;

            inputVector = pos.magnitude > 1.0f ? pos.normalized : pos;
>>>>>>> Stashed changes

            UpdateHandlePosition();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        // Não zera a alça imediatamente, deixa o Update suavizar
    }

    private void Update()
    {
        if (inputVector != Vector2.zero)
        {
            UpdateHandlePosition();
        }
        else if (joystickHandle.anchoredPosition != Vector2.zero)
        {
            // Suaviza o retorno ao centro
            joystickHandle.anchoredPosition = Vector2.Lerp(
                joystickHandle.anchoredPosition,
                Vector2.zero,
                Time.deltaTime * returnSpeed
            );
        }
    }

    private void UpdateHandlePosition()
    {
        joystickHandle.anchoredPosition = new Vector2(
            inputVector.x * (joystickBackground.sizeDelta.x / 2),
            inputVector.y * (joystickBackground.sizeDelta.y / 2)
        );
    }
}
