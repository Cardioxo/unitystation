﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Health
{
    [CreateAssetMenu(fileName = "SurgeryOptions", menuName = "ScriptableObjects/Surgery/SurgeryOptions")]
    public class SurgeryOptions : ScriptableObject
    {
        public DictionaryBodyPartTypeToSurgeryTypeList SurgeryTypesUnderBodyPart = null;
    }

    [Serializable]
    public class DictionaryBodyPartTypeToSurgeryTypeList : SerializableDictionary<BodyPartType, SurgeryProcedureCollection>
    {
    }
}