using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Health
{
    [CreateAssetMenu(fileName = "SurgeryProcedure", menuName = "ScriptableObjects/Surgery/SurgeryProcedure")]
    public class SurgeryProcedure : ScriptableObject
    {
        [SerializeField]
        private List<SurgeryStep> surgerySteps = null;
    
        public List<SurgeryStep> SurgerySteps
        {
            get => surgerySteps;
        }
    }

    [System.Serializable]
    public class SurgeryStep
    {
        /// <summary>
        /// The step number in the surgery procedure. 1 is the first step after a procedure has been chosen.
        /// </summary>
        [SerializeField]
        private int stepNumber = 1;
        public int StepNumber => stepNumber;
    
        /// <summary>
        /// The tool that can be used in this single step.
        /// </summary>
        [SerializeField]
        private SurgeryToolType toolType = SurgeryToolType.None;
        public SurgeryToolType ToolType
        {
            get => toolType;
        }
        /// <summary>
        /// The effects that should happen when the progress bar finishes and the step is successful
        /// </summary>
        [SerializeField]
        private List<SurgeryEffect> onSuccessEffects = null;

        public List<SurgeryEffect> OnSuccessEffects => onSuccessEffects;
    
        /// <summary>
        /// The effects that should happen when the progress bar finishes and the step is failing
        /// </summary>
        [SerializeField] 
        private List<SurgeryEffect> onFailureEffects = null;
    
        public List<SurgeryEffect> OnFailureEffects => onFailureEffects;
    }
}