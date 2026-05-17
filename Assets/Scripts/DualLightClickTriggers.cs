using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DualLightClickTriggers : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator leftLightAnimator;
    [SerializeField] private Animator rightLightAnimator;

    [Header("Trigger")]
    [SerializeField] private string triggerName = "hit";

    [Header("Mouse Buttons")]
    [SerializeField] private int leftMouseButton  = 0;
    [SerializeField] private int rightMouseButton = 1;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private int _triggerHash;

    private void Awake()
    {
        _triggerHash = Animator.StringToHash(triggerName);
    }

    private void Update()
    {
        if (IsMouseButtonDown(leftMouseButton))
            ActivateLeft();

        if (IsMouseButtonDown(rightMouseButton))
            ActivateRight();
    }

    private void ActivateLeft()
    {
        if (leftLightAnimator)
            leftLightAnimator.SetTrigger(_triggerHash);

        if (debugLogs)
            Debug.Log($"[DualLightClickTriggers] LEFT click -> trigger='{triggerName}', animator={(leftLightAnimator ? "ok" : "null")}", this);
    }

    private void ActivateRight()
    {
        if (rightLightAnimator)
            rightLightAnimator.SetTrigger(_triggerHash);

        if (debugLogs)
            Debug.Log($"[DualLightClickTriggers] RIGHT click -> trigger='{triggerName}', animator={(rightLightAnimator ? "ok" : "null")}", this);
    }

    private static bool IsMouseButtonDown(int button)
    {
        if (Input.GetMouseButtonDown(button))
            return true;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;
        return button switch
        {
            0 => Mouse.current.leftButton.wasPressedThisFrame,
            1 => Mouse.current.rightButton.wasPressedThisFrame,
            2 => Mouse.current.middleButton.wasPressedThisFrame,
            _ => false
        };
#else
        return false;
#endif
    }
}
