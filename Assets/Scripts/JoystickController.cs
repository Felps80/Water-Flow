
using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    private Vector2 inputVector;

    [SerializeField]
    private float deadZone = 0.1f; // Evita movimentações falsas

    [SerializeField]
    private float returnSpeed = 10f; // Velocidade de retorno suave

    public float Horizontal => ProcessedDirection.x;
    public float Vertical => ProcessedDirection.y;
    public Vector2 Direction => new Vector2(Horizontal, Vertical);

    private Vector2 ProcessedDirection
    {
        get
        {
            return inputVector.magnitude < deadZone ? Vector2.zero : inputVector;
        }
    }

    private void Start()
    {
        joystickBackground = GetComponent<RectTransform>();
        if (joystickHandle == null)
        {
            Debug.LogError("Joystick Handle não atribuído no Inspector!");
        }
        else
        {
            Debug.Log("Joystick Handle atribuído: " + joystickHandle.name);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out pos))
        {
            // Normaliza para -1 a 1
            pos.x = (pos.x / joystickBackground.sizeDelta.x) * 2;
            pos.y = (pos.y / joystickBackground.sizeDelta.y) * 2;

            inputVector = pos.magnitude > 1.0f ? pos.normalized : pos;

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
        if (joystickHandle == null)
        {
            Debug.LogWarning("Joystick Handle não está atribuído.");
            return;
        }

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
