using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Пункт 13: Any State --[hit trigger]--> AnimationState --[ExitTime]--> Empty State
public static class HitAnimatorSetup
{
    [MenuItem("Tools/Setup Hit Animator")]
    public static void SetupAnimator()
    {
        const string controllerPath = "Assets/anim/Hit.controller";
        const string animPath = "Assets/anim/Hit.anim";

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        if (clip == null)
        {
            Debug.LogError("[Hit] Анимация не найдена: " + animPath +
                           "\nСначала создай анимацию Hit.anim в папке Assets/anim/");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);

        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        foreach (var cs in sm.states)
            sm.RemoveState(cs.state);

        // Триггер-переменная
        controller.AddParameter("hit", AnimatorControllerParameterType.Trigger);

        // Empty State — состояние покоя по умолчанию
        AnimatorState emptyState = sm.AddState("Empty State", new Vector3(100, 100, 0));
        emptyState.writeDefaultValues = true;
        sm.defaultState = emptyState;

        // Состояние анимации
        AnimatorState animState = sm.AddState("HitAnim", new Vector3(400, 100, 0));
        animState.motion = clip;
        animState.writeDefaultValues = true;

        // Any State --[hit]--> HitAnim
        AnimatorStateTransition anyToAnim = sm.AddAnyStateTransition(animState);
        anyToAnim.AddCondition(AnimatorConditionMode.If, 0, "hit");
        anyToAnim.hasExitTime = false;
        anyToAnim.duration = 0f;
        anyToAnim.canTransitionToSelf = false;

        // HitAnim --[ExitTime=1]--> Empty State
        AnimatorStateTransition animToEmpty = animState.AddTransition(emptyState);
        animToEmpty.hasExitTime = true;
        animToEmpty.exitTime = 1f;
        animToEmpty.duration = 0f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Hit] Аниматор настроен: Empty State → Any State[hit] → HitAnim → Empty State");
    }
}
