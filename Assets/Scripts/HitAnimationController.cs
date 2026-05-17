using UnityEngine;

// Пункт 13: анимация запускается по правой кнопке мыши через триггер "hit"
// В Animator должно быть:
//   - Empty State (default)
//   - Any State --[hit]--> AnimationState --[ExitTime]--> Empty State
public class HitAnimationController : MonoBehaviour
{
    private Animator _animator;
    private static readonly int Hit = Animator.StringToHash("hit");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
            Debug.LogError("[Hit] Animator не найден на объекте " + gameObject.name);
    }

    private void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (Input.GetMouseButtonDown(1))
            _animator.SetTrigger(Hit);
    }
}
