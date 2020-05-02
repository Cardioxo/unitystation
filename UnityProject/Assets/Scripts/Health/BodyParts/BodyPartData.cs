using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Health
{
	[CreateAssetMenu(fileName = "BodyPartData", menuName = "ScriptableObjects/Health")]
	public class BodyPartData : ScriptableObject
	{
		[Tooltip("What archetype of body part is this?")]
		public BodyPartType bodyPartType = BodyPartType.Chest;

		[Tooltip("What's the max amount of damage this limb can resist")]
		public float maxDamage = 200;

		[Tooltip("If this limb hp ever reach this amount, it will be mangled")]
		public float mangledThreshold = 50;

		[Tooltip("Armor values for this body part")]
		public Armor armor = new Armor();

		[Tooltip("Should this limb be able to be dismembered?")]
		public bool dismemberable = true;

		[Tooltip("Damage threshold to roll a dismember event for this limb")]
		public float dismemberThreshold = 30;

		[Tooltip("Chances of this limb to be dismembered when the damage threshold is passed")][Range(0, 100)]
		public int dismemberChance = 0;

		[Tooltip("In game object representation of this limb")]
		public GameObject inGameLimb;

		[Tooltip("Does this limb contain inner organs?")]
		public bool containOrgans = false;

		[ConditionalField(nameof(containOrgans))] [Tooltip("List of organs this limb contains")]
		public List<string> organs; //TODO make organs data to fill this

		[BoxGroup("In game sprite")] [Tooltip("What does this body part look like in the mob's body")]
		public SpriteSheetAndData mobSprite;

		[BoxGroup("Doll health state")] [Tooltip("Sprite for blue state")]
		public Sprite blueDamageMonitorIcon;

		[BoxGroup("Doll health state")] [Tooltip("Sprite for green state")]
		public Sprite greenDamageMonitorIcon;

		[BoxGroup("Doll health state")] [Tooltip("Sprite for yellow state")]
		public Sprite yellowDamageMonitorIcon;

		[BoxGroup("Doll health state")] [Tooltip("Sprite for orange state")]
		public Sprite orangeDamageMonitorIcon;

		[BoxGroup("Doll health state")] [Tooltip("Sprite for dark orange state")]
		public Sprite darkOrangeDamageMonitorIcon;

		[BoxGroup("Doll health state")] [Tooltip("Sprite for red state")]
		public Sprite redDamageMonitorIcon;

		[BoxGroup("Doll health state")] [Tooltip("Sprite for gray state")]
		public Sprite grayDamageMonitorIcon;
	}
}