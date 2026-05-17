using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TaskProgressUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject nextTaskButton;


    private void Start()
    {
        Main.taskManager.OnTaskChanged += TaskChangedHandler;
        Main.taskManager.OnTaskCompleted += TaskCompletedHandler;
        nextTaskButton.SetActive(false);
    }

    private void OnDisable()
    {
        Main.taskManager.OnTaskCompleted += TaskCompletedHandler;
        Main.taskManager.OnTaskChanged -= TaskChangedHandler;
    }

    public void OnNextButtonClick()
    {
        Main.taskManager.SelectNext();
    }

    private void TaskChangedHandler(TaskId id)
    {
        nextTaskButton.SetActive(false);
        progressText.text = "задание:" + id.ToCustomString();
    }
    private void TaskCompletedHandler(TaskId id)
    {
        progressText.text = "Задание выполнено! Можно перейти к следующему.";
        nextTaskButton.SetActive(true);
    }
}