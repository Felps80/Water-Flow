using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referências do Joystick")]
    [SerializeField] private RectTransform joystickBackground;  // Arraste aqui o fundo do joystick
    [SerializeField] private RectTransform joystickHandle;      // Arraste aqui a alça do joystick

    private Vector2 inputVector;

    // Getters públicos para acesso externo
    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => inputVector;

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

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out pos))
        {
            // Normaliza a posição dentro do raio do joystick
            pos.x = pos.x / (joystickBackground.sizeDelta.x / 2);
            pos.y = pos.y / (joystickBackground.sizeDelta.y / 2);

            inputVector = new Vector2(pos.x, pos.y);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Move a alça do joystick
            joystickHandle.anchoredPosition = new Vector2(
                inputVector.x * (joystickBackground.sizeDelta.x / 2),
                inputVector.y * (joystickBackground.sizeDelta.y / 2)
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
    }
}
