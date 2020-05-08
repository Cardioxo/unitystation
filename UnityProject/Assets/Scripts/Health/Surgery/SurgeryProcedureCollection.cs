using System.Collections.Generic;
using UnityEngine;

namespace Health
{
    [CreateAssetMenu(fileName = "SurgeryProcedureCollection", menuName = "ScriptableObjects/Surgery/SurgeryProcedureCollection")]
    public class SurgeryProcedureCollection : ScriptableObject
    {
        public List<SurgeryProcedure> SurgeryProcedures = null;
    }
}
