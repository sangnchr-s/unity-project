using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class DrillHeadAnimatorSetup
{
    [MenuItem("Tools/Setup DrillHead Animator")]
    public static void SetupAnimator()
    {
        const string controllerPath = "Assets/anim/DrillHead.controller";
        const string animPath = "Assets/anim/DrillHead_anim.anim";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("[DrillHead] Контроллер не найден: " + controllerPath);
            return;
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        if (clip == null)
        {
            Debug.LogError("[DrillHead] Анимация не найдена: " + animPath);
            return;
        }

        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);

        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        foreach (var cs in sm.states)
            sm.RemoveState(cs.state);

        // Bool-параметр
        controller.AddParameter("IsExtended", AnimatorControllerParameterType.Bool);

        // ── Состояния ──────────────────────────────────────────────────────────
        AnimatorState retracted = sm.AddState("Retracted", new Vector3(100, 100, 0));
        retracted.writeDefaultValues = false;

        AnimatorState extending = sm.AddState("Extending", new Vector3(350, 0, 0));
        extending.motion = clip;
        extending.speed = 1f;
        extending.writeDefaultValues = false;

        AnimatorState extended = sm.AddState("Extended", new Vector3(600, 100, 0));
        extended.writeDefaultValues = false;

        AnimatorState retracting = sm.AddState("Retracting", new Vector3(350, 200, 0));
        retracting.motion = clip;
        retracting.speed = -1f;
        retracting.cycleOffset = 1f;
        retracting.writeDefaultValues = false;

        sm.defaultState = retracted;

        // ── Переходы ───────────────────────────────────────────────────────────
        var t1 = retracted.AddTransition(extending);
        t1.AddCondition(AnimatorConditionMode.If, 0, "IsExtended");
        t1.hasExitTime = false;
        t1.duration = 0f;

        var t2 = extending.AddTransition(extended);
        t2.hasExitTime = true;
        t2.exitTime = 1f;
        t2.duration = 0f;

        var t3 = extended.AddTransition(retracting);
        t3.AddCondition(AnimatorConditionMode.IfNot, 0, "IsExtended");
        t3.hasExitTime = false;
        t3.duration = 0f;

        var t4 = retracting.AddTransition(retracted);
        t4.hasExitTime = true;
        t4.exitTime = 1f;
        t4.duration = 0f;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[DrillHead] Аниматор настроен: 4 состояния, bool IsExtended.");
    }
}
