using UnityEngine;

namespace Health
{
    public class SurgeryOperation
    {
        public readonly int MaxSteps = 1;
        
        private int currentStep = 1;

        public int CurrentStep
        {
            get => currentStep;
            set => currentStep = Mathf.Clamp(currentStep, 1, MaxSteps);
        }

        public readonly SurgeryProcedure surgeryProcedure = null;

        public SurgeryOperation(SurgeryProcedure surgeryProcedure)
        {
            this.surgeryProcedure = surgeryProcedure;
            MaxSteps = surgeryProcedure.SurgerySteps.Count;
        }


        public bool ValidateInteraction(HandApply interaction)
        {
            SurgeryStep surgeryStep = ConfirmSurgeryStep(interaction);
            if (surgeryStep != null)
            {
                return true;
            }
            return false;
        }

        public void PerformInteractionSuccess(HandApply interaction)
        {
            SurgeryStep surgeryStep = ConfirmSurgeryStep(interaction);
            if (surgeryStep != null)
            {
                ApplyAllSuccessEffects(interaction, surgeryStep);
            }
        }

        public void PerformInteractionFailure(HandApply interaction)
        {
            SurgeryStep surgeryStep = ConfirmSurgeryStep(interaction);
            if (surgeryStep != null)
            {
                ApplyAllFailureEffects(interaction, surgeryStep);
            }
        }

        /// <summary>
        /// Checks if the surgery tool used has the right surgery tool property at the right step.
        /// </summary>
        /// <param name="interaction"></param>
        /// <returns></returns>
        private SurgeryStep ConfirmSurgeryStep(HandApply interaction)
        {
            SurgeryTool toolUsed = interaction.HandObject.GetComponent<SurgeryTool>();
            //Goes through every step in the surgery procedure to confirm the right tool and step
            foreach (SurgeryStep surgeryStep in surgeryProcedure.SurgerySteps)
            {
                //Each tool property in the surgery tool
                foreach (SurgeryToolProperty toolProperty in toolUsed.SurgeryToolProperties)
                {
                    //Checks if the surgery step and tool is the same as the procedure, applies all the effects
                    if (surgeryStep.StepNumber == currentStep && surgeryStep.ToolType == toolProperty.ToolType)
                    {
                        return surgeryStep;
                    }
                }
            }

            return null;
        }
        
        private void ApplyAllSuccessEffects(HandApply interaction, SurgeryStep surgeryStep)
        {
            foreach (SurgeryEffect surgeryEffect in surgeryStep.OnSuccessEffects)
            {
                surgeryEffect.ApplyEffect(interaction);
            }
        }

        private void ApplyAllFailureEffects(HandApply interaction, SurgeryStep surgeryStep)
        {
            foreach (SurgeryEffect surgeryEffect in surgeryStep.OnFailureEffects)
            {
                surgeryEffect.ApplyEffect(interaction);
            }
        }
    }
}
