using System;
using System.Collections.Generic;


public enum TaskId
{
    None,
    StartMachine,
    StartRotation,
    Run,
    ExtendDrill,
    ChangePower,
    ChangePowerAgain,
    //EnableLight,
    Finish
}

public static class TaskIdExtensions
{

    private static readonly IReadOnlyDictionary<TaskId, string> _toStringMap = new Dictionary<TaskId, string>
    {
        { TaskId.None, "все задания выполнены" },
        { TaskId.StartMachine, "запустите машину (клавиша пробел)" },
        { TaskId.StartRotation, "запустите вращение (клавиша R)" },
        { TaskId.Run, "доедьте до горной породы (клавиша F)" },
        { TaskId.ExtendDrill, "выдвиньте головку бура (клавиша E)" },
        { TaskId.ChangePower, "доедьте до гранитной породы (F). смените мощность (2)" },
        { TaskId.ChangePowerAgain, "проедьте до диорита (F). смените мощность (3)" },
        { TaskId.Finish, "закончите тоннель" }

    };

    private static readonly IReadOnlyDictionary<string, TaskId> _fromStringMap =
        BuildReverseMap(_toStringMap);

    private static IReadOnlyDictionary<string, TaskId> BuildReverseMap(IReadOnlyDictionary<TaskId, string> forward)
    {
        var dict = new Dictionary<string, TaskId>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in forward)
        {
            if (!dict.ContainsKey(kv.Value))
                dict[kv.Value] = kv.Key;
        }
        return dict;
    }

    public static string ToCustomString(this TaskId id)
    {
        if (_toStringMap.TryGetValue(id, out var s))
            return s;
        return id.ToString();
    }

    public static bool TryParseFromCustomString(this string s, out TaskId result)
    {
        if (string.IsNullOrEmpty(s))
        {
            result = TaskId.None;
            return false;
        }

        if (_fromStringMap.TryGetValue(s, out result))
            return true;

        if (Enum.TryParse<TaskId>(s, ignoreCase: true, out result))
            return true;

        result = TaskId.None;
        return false;
    }

    public static TaskId ParseFromCustomString(this string s)
    {
        if (s.TryParseFromCustomString(out var result))
            return result;
        throw new ArgumentException($"Unknown TaskId string: {s}");
    }

    public static TaskId Next(this TaskId current, bool loop = true)
    {

        var values = (TaskId[])Enum.GetValues(typeof(TaskId));
        int index = Array.IndexOf(values, current);
        if (index < 0) return current;

        int nextIndex = index + 1;
        if (nextIndex >= values.Length)
            nextIndex = loop ? 0 : values.Length - 1;

        return values[nextIndex];
    }
}

public enum DrillTaskAction
{
    DrillStarted,
    RotationStarted,
    MovementHitRock,
    DrillHeadExtended,
    OnPowerChanged1,
    OnPowerChanged2,
    AchieveFinish

}

[System.Serializable]
public class TaskManager
{
    public TaskId current = TaskId.None;
    private int _progress;
    public int Progress //делаю так потомц что может быть задание из 2 частей, тогда прибавлять по 0.5
    {
        get
        {
            return _progress;
        }
        private set
        {
            _progress = value;
            if (_progress >= 1)
            {
                OnTaskCompleted?.Invoke(current);
                _progress = 0;

            }
        }
    }

    public event Action<TaskId> OnTaskChanged;
    public event Action<TaskId> OnTaskCompleted;

    public void SubscibeAll()
    {
        Select(TaskId.StartMachine);
        DrillStarter.DrillStarted += TaskEventHandler;
        Movement.OnDrillHitRock += TaskEventHandler;
        Movement.OnPowerChanged += TaskEventHandler;
        Movement.OnFinish += TaskEventHandler;
        DrillHeadController.DrillRotateStarted += TaskEventHandler;
        DrillHeadController.DrillHeadExtended += TaskEventHandler;
    }

    private void TaskEventHandler(DrillTaskAction action)
    {
        switch (current)
        {
            case TaskId.StartMachine:
                if (action == DrillTaskAction.DrillStarted)
                    Progress = 1;

                break;
            case TaskId.StartRotation:
                if (action == DrillTaskAction.RotationStarted)
                    Progress = 1;
                break;
            case TaskId.Run:
                if (action == DrillTaskAction.MovementHitRock)
                    Progress = 1;
                break;

            case TaskId.ExtendDrill:
                if (action == DrillTaskAction.DrillHeadExtended)
                    Progress = 1;
                break;

            case TaskId.ChangePower:
                if (action == DrillTaskAction.OnPowerChanged1)
                    Progress = 1;
                    break;
            case TaskId.ChangePowerAgain:
                if (action == DrillTaskAction.OnPowerChanged2)
                    Progress = 1;
                    break;
            case TaskId.Finish:
                if (action == DrillTaskAction.AchieveFinish)
                    Progress = 1;
                    break;
            default:
                return;
        }
    }

    public void Select(TaskId t)
    {
        current = t;
        OnTaskChanged?.Invoke(current);
    }

    public void SelectNext()
    {
        current = current.Next();
        OnTaskChanged?.Invoke(current);
    }



}