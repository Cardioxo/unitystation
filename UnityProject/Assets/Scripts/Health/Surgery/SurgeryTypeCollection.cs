using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurgeryTypeCollection", menuName = "ScriptableObjects/Surgery/SurgeryTypeCollection")]
public class SurgeryTypeCollection : ScriptableObject
{
    public List<SurgeryProcedure> SurgeryProcedures = null;
}
