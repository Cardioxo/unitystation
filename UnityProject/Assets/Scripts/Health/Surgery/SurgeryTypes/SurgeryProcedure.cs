using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SurgeryProcedure
{
    protected static readonly StandardProgressActionConfig ProgressConfig
        = new StandardProgressActionConfig(StandardProgressActionType.Restrain);
    
    private int currentStep = 1;

    public int CurrentStep
    {
        get => currentStep;
        set => currentStep = Mathf.Clamp(value, 1, stepAndTool.Count);
    }

    protected Dictionary<int, SurgeryToolType> stepAndTool = new Dictionary<int, SurgeryToolType>();

    public Dictionary<int, SurgeryToolType> StepAndTool
    {
        get => stepAndTool;
    }

    /// <summary>
    /// Used to check if the surgery procedure is available. Some procedures has special requirements
    /// that can vary. Will return true, if not overrided.
    /// </summary>
    /// <returns></returns>
    public virtual bool IsAvailable()
    {
        return true;
    }
    
    /// <summary>
    /// Used by Operable to determine if the right tool at the right step is used by a performer.
    /// </summary>
    public abstract bool ValidateInteraction(Interaction interaction);

    /// <summary>
    /// The action that should be performed when ValidateInteraction is true
    /// </summary>
    public abstract void PerformInteraction(Interaction interaction, RegisterTile targetLocation);

    /// <summary>
    /// Used for calculating the time it takes to perform one surgery step
    /// </summary>
    /// <param name="surgeryToolUsed">The SurgeryTool component of an item</param>
    /// <returns></returns>
    protected virtual float CalculateSurgeryStepTime(SurgeryTool surgeryToolUsed)
    {
        //Do time calculation
        
        //Return perform time
        return 10f;
    }

    /// <summary>
    /// Used for rolling the success chance of the surgery.
    /// </summary>
    /// <returns></returns>
    protected virtual bool RollSuccessChance()
    {
        //TODO: Need to check tool efficiency%, table efficiency%, chemical efficiency%
        return true;
    }
}

