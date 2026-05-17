using UnityEngine;

public class DrillHeadTurn : MonoBehaviour
{
    [SerializeField] private KeyCode key = KeyCode.E;
    [SerializeField] private string boolParameterName = "turn";
    [SerializeField] private bool debugLogs = true;

    private Animator _animator;
    private bool _state;
    private int _paramHash;

    public bool turned {private set; get;}

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _paramHash = Animator.StringToHash(boolParameterName);
    }

    private void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (!_animator || !Input.GetKeyDown(key))
        {
           
            return;
        }
      

        _state = !_state;
        turned = _state;
        _animator.SetBool(_paramHash, _state);

        if (debugLogs)
        {
            bool animatorValue = _animator.GetBool(_paramHash);
            Debug.Log(
                $"[{nameof(DrillHeadTurn)}] Key={key} -> {_state}. Animator '{boolParameterName}'={animatorValue} on '{gameObject.name}'",
                this);
        }
    }
}

