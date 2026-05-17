using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillStarter : MonoBehaviour
{
    public static event Action<DrillTaskAction> DrillStarted;


    void Update()
    {
        if (SimulatorPracticePanel.IsTypingInPracticeInput)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            print("starter space");
            DrillStarted?.Invoke(DrillTaskAction.DrillStarted);
        }
    }

    public static void print(string s)
    {
        Debug.Log(s);
    }
}
