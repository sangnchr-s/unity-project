using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public static TaskManager taskManager;

    private void Awake()
    {
        taskManager = new TaskManager();
        taskManager.SubscibeAll();
    }
}
