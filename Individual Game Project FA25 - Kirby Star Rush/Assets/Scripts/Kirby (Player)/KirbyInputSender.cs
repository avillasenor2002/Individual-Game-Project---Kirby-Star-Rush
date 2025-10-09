using UnityEngine;
using UnityEngine.InputSystem;

public class KirbyInputSender : MonoBehaviour
{
    [Header("Kirby References")]
    [SerializeField] private GameObject kirbyObject;

    private KirbyController2D_InputSystem controllerComp;
    private KirbyInhale inhaleComp;

    [Header("Input Action References")]
    [SerializeField] private InputActionReference movementAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference specialAction;

    private Vector2 currentMoveInput = Vector2.zero;

    private void Awake()
    {
        if (kirbyObject == null) kirbyObject = gameObject;
        CacheComponents();
    }

    private void OnValidate() => CacheComponents();

    private void CacheComponents()
    {
        if (kirbyObject != null)
        {
            controllerComp = kirbyObject.GetComponent<KirbyController2D_InputSystem>();
            inhaleComp = kirbyObject.GetComponent<KirbyInhale>();
        }
    }

    private void OnEnable()
    {
        if (movementAction != null)
        {
            movementAction.action.performed += OnMovementPerformed;
            movementAction.action.canceled += OnMovementCanceled;
        }

        if (jumpAction != null) jumpAction.action.performed += OnJumpPerformed;

        if (specialAction != null)
        {
            specialAction.action.started += OnSpecialStarted;
            specialAction.action.canceled += OnSpecialCanceled;
        }
    }

    private void OnDisable()
    {
        if (movementAction != null)
        {
            movementAction.action.performed -= OnMovementPerformed;
            movementAction.action.canceled -= OnMovementCanceled;
        }

        if (jumpAction != null) jumpAction.action.performed -= OnJumpPerformed;

        if (specialAction != null)
        {
            specialAction.action.started -= OnSpecialStarted;
            specialAction.action.canceled -= OnSpecialCanceled;
        }
    }

    private void Update()
    {
        // Continuously send movement input to controller
        if (controllerComp != null)
            controllerComp.onMovement(currentMoveInput);
    }

    #region Movement
    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        currentMoveInput = context.ReadValue<Vector2>();
    }

    private void OnMovementCanceled(InputAction.CallbackContext context)
    {
        currentMoveInput = Vector2.zero; // Stop movement when input released
    }
    #endregion

    #region Jump
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (controllerComp != null)
            controllerComp.onJump();
    }
    #endregion

    #region Special Action (Inhale / Float)
    private void OnSpecialStarted(InputAction.CallbackContext context)
    {
        // Start inhale if not floating
        if (inhaleComp != null)
            inhaleComp.StartInhaleIfNotFloating();

        // Cancel float if active
        if (controllerComp != null)
            controllerComp.onSpecialAction();
    }

    private void OnSpecialCanceled(InputAction.CallbackContext context)
    {
        // Stop inhale on button release
        if (inhaleComp != null)
            inhaleComp.StopInhalePublic();
    }
    #endregion
}
