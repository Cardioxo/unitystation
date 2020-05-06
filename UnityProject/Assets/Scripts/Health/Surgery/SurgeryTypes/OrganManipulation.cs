using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganManipulation : SurgeryProcedure
{
    void OnEnable()
    {
        stepAndTool = new Dictionary<int, SurgeryToolType>()
        {
            {1, SurgeryToolType.Scalpel},
            {2, SurgeryToolType.Retractor},
            {3, SurgeryToolType.CircularSaw},
            {4, SurgeryToolType.Hemostat},
            {5, SurgeryToolType.Scalpel},
            {6, SurgeryToolType.Hemostat},
            {7, SurgeryToolType.Cautery}
        };
    }
    public override bool ValidateInteraction(Interaction interaction)
    {
        SurgeryToolType surgeryToolType = interaction.UsedObject.GetComponent<SurgeryTool>().ToolType;
        //6th step is the step where organs are pulled out
        if (CurrentStep == 6) return true;
        //Right tool at x step
        else if (stepAndTool[CurrentStep] == surgeryToolType) return true;
        else return false;
    }

    public override void PerformInteraction(Interaction interaction, RegisterTile targetLocation)
    {
        if (CurrentStep == 6) 
        { 
            //Show up UI to remove organs
            //TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType, TabAction.Open );
            return;
        }

        else
        {
            float surgeryTime = CalculateSurgeryStepTime(interaction.UsedObject.GetComponent<SurgeryTool>());

            void ProgressFinishAction()
            {
                bool isSuccess = RollSuccessChance();
                CurrentStep++;
            }

            var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
                .ServerStartProgress(targetLocation, surgeryTime, interaction.Performer);

        }
    }
}
