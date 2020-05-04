﻿using System;
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

		private float overallDamage = 0;
		private DamageSeverity damageSeverity = DamageSeverity.None;
		private bool isBleeding = false;
		private float bruteDamage = 0;
		private float burnDamage = 0;
		private bool mangled = false;

		public float OverallDamage => overallDamage;
		public DamageSeverity DamageSeverity => damageSeverity;
		public bool IsBleeding => isBleeding;
		public float BruteDamage => bruteDamage;
		public float BurnDamage => burnDamage;
		public bool Mangled => mangled;

		public event Action<bool> MangledStateChanged;
		public event Action<bool> BleedingStateChanged;
		public event Action<BodyPartData> BodyPartChanged;

		public override void OnStartServer()
		{
			Init();
		}

		private void Init()
		{
			overallDamage = bodyPartData.maxDamage;
			isBleeding = false;
			bruteDamage = 0;
			burnDamage = 0;
			mangled = false;

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

			if (damage >= bodyPartData.dismemberThreshold)
			{
				CheckDismember();
			}

			CheckBleeding();
			CheckMangled();
			UpdateSeverity();
		}

		public virtual void HealDamage(int damage, DamageType type)
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
			CheckBleeding();
			CheckMangled();
			UpdateSeverity();
		}

		private void UpdateSeverity()
		{
			// update UI limbs depending on their severity of damage
			float severity = (float) (overallDamage / bodyPartData.maxDamage) * 100;
			foreach (DamageSeverity _severity in Enum.GetValues(typeof(DamageSeverity)))
			{
				if (severity >= (int) _severity)
				{
					continue;
				}

				damageSeverity = _severity;
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
			Spawn.ServerPrefab(bodyPartData.inGameLimb, gameObject.RegisterTile().WorldPositionServer);
			bodyPartData = dismemberData;
			BodyPartChanged?.Invoke(bodyPartData);
		}

		private void CheckMangled()
		{
			bool newMangled = overallDamage < bodyPartData.mangledThreshold;

			if (mangled == newMangled)
			{
				return;
			}

			mangled = newMangled;
			MangledStateChanged?.Invoke(mangled);
		}

		private void CheckBleeding()
		{
			bool newBleeding = bruteDamage < 20;

			if (isBleeding == newBleeding)
			{
				return;
			}

			isBleeding = newBleeding;
			BleedingStateChanged?.Invoke(isBleeding);
		}

		[Server]
		public void ReplaceLimb(BodyPartData bodyPart)
		{
			bodyPartData = bodyPart;
			Init();
			BodyPartChanged?.Invoke(bodyPart);
		}

		public float GetDamageValue(DamageType damageType)//TODO this looks like unnecessary. Find usages on old class and purge
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
	}
}