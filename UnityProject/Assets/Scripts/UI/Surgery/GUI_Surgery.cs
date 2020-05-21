using System.Collections;
using System.Collections.Generic;
using Health;
using UnityEngine;

public class GUI_Surgery : NetTab
{
    private bool isUpdating = false;
    
    private Operable operable;
    protected override void InitServer()
    {
        StartCoroutine(WaitForProvider());
    }

    private IEnumerator WaitForProvider()
    {
        while (Provider == null)
        {
            yield return WaitFor.EndOfFrame;
        }
        operable = Provider.GetComponentInChildren<Operable>();
    }
    
    
}
