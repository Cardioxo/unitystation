using System;
using Health;
using Mirror;
using UnityEngine;

namespace Health
{
	/// <summary>
	/// Object representation of a body part. Contains all data and methods needed
	/// to interact with body parts in any living creature (organics or non)
	/// </summary>
	public class BodyPart : NetworkBehaviour
	{
		[Tooltip("Scriptable object that contains this body part relevant data")]
		public BodyPartData bodyPartData = null;

		[SerializeField] [Tooltip("Body part data to load when this member has been dismembered")]
		private BodyPartData dismemberData = null;

		private float overallHealth = 0;
		private DamageSeverity damageSeverity = DamageSeverity.None;
		private float bruteDamage = 0;
		private float burnDamage = 0;
		private bool isBleeding = false;
		private bool isMangled = false;
		private bool isDismembered = false;

		public float OverallHealth => overallHealth;
		public float OverallHealthPercentage => (overallHealth / bodyPartData.maxDamage) * 100;
		public float OverallDamage => bodyPartData.maxDamage - overallHealth;
		public float OverallDamagePercentage => (100 - OverallHealthPercentage);
		public DamageSeverity DamageSeverity => damageSeverity;
		public bool IsBleeding => isBleeding;
		public float BruteDamage => bruteDamage;
		public float BurnDamage => burnDamage;
		public bool IsMangled => isMangled;
		public bool IsDismembered => isDismembered;
		public Armor Armor { get; set; } = new Armor();

		public event Action<BodyPart, bool> MangledStateChanged;
		public event Action<BodyPart, bool> BleedingStateChanged;
		public event Action<BodyPart, bool> DismemberStateChanged;
		public event Action<BodyPartData> BodyPartChanged;

		// public override void OnStartServer()
		// {
		// 	Init();
		// }

		private void OnEnable()
		{
			Init();
		}

		private void Init()
		{
			bruteDamage = 0;
			burnDamage = 0;
			isBleeding = false;
			isMangled = false;
			isDismembered = false;
			overallHealth = bodyPartData.maxDamage;


			//TODO call update for sprites!
		}

		public virtual void ReceiveDamage(DamageType damageType, float damage)
		{
			UpdateDamage(damage, damageType);
			Logger.LogTraceFormat("{0} received {1} {2} damage. Total {3}/{4}, limb condition is {5}",
				Category.Health, bodyPartData.bodyPartType, damage, damageType, damage, bodyPartData.maxDamage, damageSeverity);
		}

		private void UpdateDamage(float damage, DamageType type)
		{
			switch (type)
			{
				case DamageType.Brute:
					bruteDamage += damage;
					break;

				case DamageType.Burn:
					burnDamage += damage;
					break;
			}

			CalculateOverall();

			if (bodyPartData.canBleed)
			{
				CheckBleeding();
			}

			if (bodyPartData.canBeMangled)
			{
				CheckMangled();
			}

			if (bodyPartData.canBeDismembered && damage >= bodyPartData.dismemberThreshold )//TODO this is for testing
			{
				CheckDismember();
			}

			UpdateSeverity();
		}


		public virtual void HealDamage(float damage, DamageType type)
		{
			switch (type)
			{
				case DamageType.Brute:
					bruteDamage -= damage;
					break;

				case DamageType.Burn:
					burnDamage -= damage;
					break;
			}

			CalculateOverall();
			CheckBleeding();
			CheckMangled();
			UpdateSeverity();
		}

		private void CalculateOverall()
		{
			overallHealth -= (bruteDamage + burnDamage);
		}

		private void UpdateSeverity()
		{
			// update UI limbs depending on their severity of damage
			float severity = OverallDamagePercentage;
			foreach (DamageSeverity value in Enum.GetValues(typeof(DamageSeverity)))
			{
				if (severity >= (int) value)
				{
					continue;
				}

				damageSeverity = value;
				break;
			}
		}

		private void CheckDismember()
		{
			if (!DMMath.Prob(bodyPartData.dismemberChance))
			{
				return;
			}

			//Drop limb
			var limb = Spawn.ServerPrefab(bodyPartData.limbGameObject, gameObject.RegisterTile().WorldPositionServer);
			limb.GameObject.GetComponentInChildren<SpriteHandler>().gameObject.SetActive(true);

			bodyPartData = dismemberData;
			isDismembered = true;
			BodyPartChanged?.Invoke(bodyPartData);
			DismemberStateChanged?.Invoke(this, isDismembered);
		}

		private void CheckMangled()
		{
			bool newMangled = OverallHealthPercentage >= bodyPartData.mangledThreshold;

			if (isMangled == newMangled)
			{
				return;
			}

			isMangled = newMangled;
			MangledStateChanged?.Invoke(this, isMangled);
		}

		private void CheckBleeding()
		{
			bool newBleeding = ((bruteDamage / bodyPartData.maxDamage) * 100) >= bodyPartData.bleedThreshold;

			if (isBleeding == newBleeding)
			{
				return;
			}

			isBleeding = newBleeding;
			BleedingStateChanged?.Invoke(this, isBleeding);
		}

		[Server]
		public void ReplaceLimb(BodyPartData bodyPart)
		{
			if (isDismembered)
			{
				isDismembered = false;
				DismemberStateChanged?.Invoke(this, isDismembered);
			}

			bodyPartData = bodyPart;
			Init();
			BodyPartChanged?.Invoke(bodyPart);
		}

		[Server]
		public void RestoreDamage()
		{
			HealDamage(bruteDamage, DamageType.Brute);
			HealDamage(burnDamage, DamageType.Burn);
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

		private void UpdateIcons()
		{
			if (!IsLocalPlayer())
			{
				return;
			}

			UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
		}

		protected bool IsLocalPlayer()
		{
			//kinda crappy way to determine local player,
			//but otherwise UpdateIcons would have to be moved to healthsystem
			//-----------------------------------------------------------------
			// Maybe we should move updating icons to HealthSystem
			return PlayerManager.LocalPlayerScript == gameObject.GetComponentInParent<PlayerScript>();
		}

		public void UpdateClientBodyPartStat(float brute, float burn)
		{
			bruteDamage = brute;
			burnDamage = burn;
			UpdateSeverity();
		}
	}
}