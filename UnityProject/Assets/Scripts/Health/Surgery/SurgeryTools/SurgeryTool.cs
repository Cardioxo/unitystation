using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//This component allows an item to become a surgery tool.
public class SurgeryTool : MonoBehaviour
{
    private SurgeryToolType toolType = SurgeryToolType.None;

    public SurgeryToolType ToolType
    {
        get => toolType;
    }
    [SerializeField]
    private int efficiency;

    /// <summary>
    /// Efficiency %. The higher the efficiency, the higher the surgery speed and the higher the success rate.
    /// </summary>
    public float Efficiency
    {
        get => efficiency;
    }
}
