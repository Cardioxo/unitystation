using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Health
{
	[CreateAssetMenu(fileName = "BodyPartSO", menuName = "ScriptableObjects/Health")]
	public class BodyPartSO : ScriptableObject
	{
		#region inspector values
		[Tooltip("What archetype of body part is this?")]
		public BodyPartType bodyPartType = BodyPartType.Chest;

		[Tooltip("What's the max amount of damage this limb can resist before falling apart")] [SerializeField]
		private float maxDamage = 200;

		[Tooltip("If this limb hp ever reach this amount, it will be mangled")] [SerializeField]
		private float mangledThreshold = 50;

		[Tooltip("Armor values for this body part")]
		public Armor armor = new Armor();

		[Tooltip("Should this limb be able to be dismembered?")][SerializeField]
		private bool dismemberable = true;

		[Tooltip("In game object representation of this limb")] [SerializeField]
		private GameObject inGameLimb;

		[Tooltip("Does this limb contain inner organs?")][SerializeField]
		private bool containOrgans = false;

		[ConditionalField(nameof(containOrgans))][Tooltip("List of organs this limb contains")][SerializeField]
		private List<string> organs;

		[BoxGroup("Doll health state")]
		[Tooltip("Sprite for blue state")]
		public Sprite blueDamageMonitorIcon;
		[BoxGroup("Doll health state")]
		[Tooltip("Sprite for green state")]
		public Sprite greenDamageMonitorIcon;
		[BoxGroup("Doll health state")]
		[Tooltip("Sprite for yellow state")]
		public Sprite yellowDamageMonitorIcon;
		[BoxGroup("Doll health state")]
		[Tooltip("Sprite for orange state")]
		public Sprite orangeDamageMonitorIcon;
		[BoxGroup("Doll health state")]
		[Tooltip("Sprite for dark orange state")]
		public Sprite darkOrangeDamageMonitorIcon;
		[BoxGroup("Doll health state")]
		[Tooltip("Sprite for red state")]
		public Sprite redDamageMonitorIcon;
		[BoxGroup("Doll health state")]
		[Tooltip("Sprite for gray state")]
		public Sprite grayDamageMonitorIcon;
		#endregion

		#region public states
		[HideInInspector] public float overallDamage = 0; //TODO initialize this value when the health system adds this limb
		public DamageSeverity Severity = DamageSeverity.None;
		[HideInInspector] public bool isBleeding = false;
		[HideInInspector] public float bruteDamage = 0;
		[HideInInspector] public float burnDamage = 0;
		[HideInInspector] public bool mangled = false;
		#endregion

		#region methods
		//Apply damages from here.
		public virtual void ReceiveDamage(DamageType damageType, float damage)
		{
			UpdateDamage(damage, damageType);
			Logger.LogTraceFormat("{0} received {1} {2} damage. Total {3}/{4}, limb condition is {5}",
				Category.Health, bodyPartType, damage, damageType, damage, maxDamage, Severity);
		}

		private void UpdateDamage(float damage, DamageType type)
		{
			if (overallDamage >= maxDamage)
			{
				return;
			}

			switch (type)
			{
				case DamageType.Brute:
					bruteDamage += damage;
					break;

				case DamageType.Burn:
					burnDamage += damage;
					break;
			}

			CheckMangled();
			UpdateSeverity();
		}

		private void CheckMangled()
		{
			if (overallDamage >= mangledThreshold)
			{
				mangled = true;//TODO mangle the limb, making it useless
			}
		}

		private void UpdateSeverity()
		{
			// update UI limbs depending on their severity of damage
			float severity = (float)overallDamage / maxDamage;
			// If the limb is uninjured
			if (severity <= 0)
			{
				Severity = DamageSeverity.None;
			}
			// If the limb is under 20% damage
			else if (severity < 0.2)
			{
				Severity = DamageSeverity.Light;
			}
			// If the limb is under 40% damage
			else if (severity < 0.4)
			{
				Severity = DamageSeverity.LightModerate;
			}
			// If the limb is under 60% damage
			else if (severity < 0.6)
			{
				Severity = DamageSeverity.Moderate;
			}
			// If the limb is under 80% damage
			else if (severity < 0.8)
			{
				Severity = DamageSeverity.Bad;
			}
			// If the limb is under 100% damage
			else if (severity < 1f)
			{
				Severity = DamageSeverity.Critical;
			}
			// If the limb is 100% damage or over
			else if (severity >= 1f)
			{
				Severity = DamageSeverity.Max;
			}

		}

		public virtual void HealDamage(int damage, DamageType type)
		{
			switch (type)
			{
				case DamageType.Brute:
					bruteDamage -= damage;
					// if(bruteDamage < 20){
					// 	healthSystem.bloodSystem.StopBleeding(this);//TODO handle stop bleeding
					// }
					break;

				case DamageType.Burn:
					burnDamage -= damage;
					break;
			}
			UpdateSeverity();
		}

		public float GetDamageValue(DamageType damageType)
		{
			switch (damageType)
			{
				case DamageType.Brute:
					return bruteDamage;
				case DamageType.Burn:
					return burnDamage;
				default:
					return 0;
			}
		}

		//TODO make halth system update icons of player

		// private void UpdateIcons()
		// {
		// 	if (!IsLocalPlayer())
		// 	{
		// 		return;
		// 	}
		// 	UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
		// }
		//
		// protected bool IsLocalPlayer()
		// {
		// 	//kinda crappy way to determine local player,
		// 	//but otherwise UpdateIcons would have to be moved to HumanHealthBehaviour
		// 	return PlayerManager.LocalPlayerScript == gameObject.GetComponentInParent<PlayerScript>();
		// }

		#endregion

	}
}