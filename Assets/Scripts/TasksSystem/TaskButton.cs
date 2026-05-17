using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskButton : MonoBehaviour
{
    public TaskId id;

    private void Awake(){
        GetComponent<Button>().onClick.AddListener(SelectTask);
    }

    public void SelectTask()
    {
       Main.taskManager.Select(id);
    }
}
