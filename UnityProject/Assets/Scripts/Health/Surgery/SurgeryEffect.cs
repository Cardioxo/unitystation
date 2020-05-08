using UnityEngine;

namespace Health
{
    [CreateAssetMenu(fileName = "SurgeryEffect", menuName = "ScriptableObjects/Surgery/SurgeryEffect")]
    public abstract class SurgeryEffect : ScriptableObject
    {
        public abstract void ApplyEffect(HandApply interaction);
    }
}
