using UnityEditor;
using UnityEngine;

namespace Health
{
    [CreateAssetMenu(fileName = "ProceedStep", menuName = "ScriptableObjects/Surgery/SurgeryEffects/ProceedStep")]
    public class ProceedStep : SurgeryEffect
    {
        public override void ApplyEffect(HandApply interaction)
        {
            BodyPartType targetedLimb = interaction.TargetBodyPart;
            // interaction.TargetObject.GetComponent<Operable>().ActiveSurgeries[targetedLimb]
            // currentStep++;
        }
    }
}
