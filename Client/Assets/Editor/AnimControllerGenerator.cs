using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 程序化生成Animator Controller + 程序化动画片段
/// 菜单: Jx3/Animations/Create Animation Controllers
/// </summary>
public class AnimControllerGenerator : EditorWindow
{
    private static readonly string AnimPath = "Assets/Art/Animations";
    private static readonly string CtrlPath = "Assets/Art/Animations/Controllers";

    [MenuItem("Jx3/Animations/Create Animation Controllers")]
    static void CreateAllControllers()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "../", AnimPath));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "../", CtrlPath));

        (int, string)[] heroes = {
            (1001, "LiWangSheng"), (1002, "XieYunLiu"), (1003, "YeYing"),
            (1004, "QuYun"), (1005, "YeWei"), (1006, "XuanZheng"),
            (1007, "XiaoSha"), (1008, "ASaXin"), (2001, "GongSunDaNiang"),
            (2002, "LiuJingTao"),
        };
        foreach (var h in heroes) CreateController(h.Item1, h.Item2);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnimControllerGenerator] 成功生成 " + heroes.Length + " 个Controller");
    }

    static void CreateController(int heroId, string name)
    {
        string ctrlPath = string.Format("{0}/AC_Hero_{1}_{2}.controller", CtrlPath, heroId, name);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var sm = controller.layers[0].stateMachine;

        string[] states = { "Idle", "Walk", "Run", "Attack", "Hit", "Death" };
        var stateMap = new Dictionary<string, AnimatorState>();

        foreach (string s in states)
        {
            AnimationClip clip = BuildClip(heroId, s);
            string clipPath = string.Format("{0}/Anim_{1}_{2}.anim", AnimPath, name, s);
            AssetDatabase.CreateAsset(clip, clipPath);

            var st = sm.AddState(s);
            st.motion = clip;
            stateMap[s] = st;
        }
        sm.defaultState = stateMap["Idle"];

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        // Idle <-> Walk <-> Run
        AddCond(stateMap["Idle"], stateMap["Walk"], false, 0, 0.1f, "Speed", AnimatorConditionMode.Greater, 0.1f);
        AddCond(stateMap["Walk"], stateMap["Idle"], false, 0, 0.1f, "Speed", AnimatorConditionMode.Less, 0.1f);
        AddCond(stateMap["Walk"], stateMap["Run"], false, 0, 0.15f, "Speed", AnimatorConditionMode.Greater, 0.8f);
        AddCond(stateMap["Run"], stateMap["Walk"], false, 0, 0.15f, "Speed", AnimatorConditionMode.Less, 0.8f);

        // Any -> Attack/Hit/Death
        AddAny(controller, stateMap["Attack"], "Attack", 0.05f);
        AddAny(controller, stateMap["Hit"], "Hit", 0.05f);
        AddAny(controller, stateMap["Death"], "Death", 0.1f);

        // Attack/Hit -> Idle (by exit time)
        AddEmpty(stateMap["Attack"], stateMap["Idle"], true, 0.8f, 0.1f);
        AddEmpty(stateMap["Hit"], stateMap["Idle"], true, 0.5f, 0.1f);

        EditorUtility.SetDirty(controller);
    }

    static AnimationClip BuildClip(int heroId, string state)
    {
        AnimationClip clip = new AnimationClip();
        clip.legacy = false; clip.frameRate = 30;
        clip.name = string.Format("Anim_{0}_{1}", heroId, state);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = state != "Death";
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        float dur = state switch { "Idle" => 2f, "Walk" => 1f, "Run" => 0.6f, "Attack" => 0.5f, "Hit" => 0.3f, "Death" => 1.5f, _ => 1f };

        if (state == "Idle")
        {
            AddCurve(clip, "Root", "m_LocalPosition.x", dur, new[] { 0f, 0f, dur*0.5f, 0.02f, dur, 0f });
            AddCurve(clip, "Body", "m_LocalPosition.y", dur, new[] { 0f, 1f, dur*0.5f, 1.03f, dur, 1f });
            AddCurve(clip, "Root", "m_LocalRotation.y", dur, new[] { 0f, 0f, dur*0.25f, 2f, dur*0.5f, 0f, dur*0.75f, -2f, dur, 0f });
        }
        else if (state == "Walk")
        {
            AddCurve(clip, "Root", "m_LocalPosition.z", dur, new[] { 0f, 0f, dur, 0.5f });
            AddCurve(clip, "Root", "m_LocalPosition.y", dur, new[] { 0f, 0f, dur*0.25f, 0.03f, dur*0.5f, 0f, dur*0.75f, 0.03f, dur, 0f });
        }
        else if (state == "Run")
        {
            AddCurve(clip, "Root", "m_LocalPosition.z", dur, new[] { 0f, 0f, dur, 1.0f });
            AddCurve(clip, "Root", "m_LocalPosition.y", dur, new[] { 0f, 0f, dur*0.25f, 0.05f, dur*0.5f, 0f, dur*0.75f, 0.05f, dur, 0f });
        }
        else if (state == "Attack")
        {
            AddCurve(clip, "Root", "m_LocalPosition.z", dur, new[] { 0f, 0f, dur*0.3f, 0.3f, dur, 0f });
            AddCurve(clip, "Root", "m_LocalRotation.x", dur, new[] { 0f, 0f, dur*0.3f, -15f, dur, 0f });
            AddCurve(clip, "Body", "m_LocalRotation.z", dur, new[] { 0f, 0f, dur*0.2f, 20f, dur*0.5f, -10f, dur, 0f });
        }
        else if (state == "Hit")
        {
            AddCurve(clip, "Root", "m_LocalPosition.z", dur, new[] { 0f, 0f, 0.05f, -0.15f, 0.15f, -0.08f, dur, 0f });
            AddCurve(clip, "Root", "m_LocalRotation.x", dur, new[] { 0f, 0f, 0.05f, 10f, dur, 0f });
        }
        else if (state == "Death")
        {
            AddCurve(clip, "Root", "m_LocalRotation.x", dur, new[] { 0f, 0f, dur*0.3f, 30f, dur, 90f });
            AddCurve(clip, "Root", "m_LocalPosition.y", dur, new[] { 0f, 0f, dur, -0.3f });
        }
        return clip;
    }

    static void AddCurve(AnimationClip clip, string path, string prop, float dur, float[] keys)
    {
        Keyframe[] kfs = new Keyframe[keys.Length / 2];
        for (int i = 0; i < kfs.Length; i++) kfs[i] = new Keyframe(keys[i*2], keys[i*2+1]);
        clip.SetCurve(path, typeof(Transform), prop, new AnimationCurve(kfs));
    }

    static void AddCond(AnimatorState from, AnimatorState to, bool exit, float exitTime, float dur,
        string param, AnimatorConditionMode mode, float threshold)
    {
        var t = from.AddTransition(to);
        t.duration = dur; t.hasExitTime = exit; t.exitTime = exitTime;
        t.AddCondition(mode, threshold, param);
    }

    static void AddEmpty(AnimatorState from, AnimatorState to, bool exit, float exitTime, float dur)
    {
        var t = from.AddTransition(to);
        t.duration = dur; t.hasExitTime = exit; t.exitTime = exitTime;
    }

    static void AddAny(AnimatorController ctrl, AnimatorState to, string param, float dur)
    {
        var t = ctrl.layers[0].stateMachine.AddAnyStateTransition(to);
        t.duration = dur; t.hasExitTime = false; t.exitTime = 0;
        t.AddCondition(AnimatorConditionMode.If, 0, param);
    }
}
