﻿using UnityEngine;

namespace Health
{
	/// <summary>
	/// Controls the blood system for a Living Entity
	/// Only really updated on the Server!
	/// Do not derive this class from NetworkBehaviour
	/// </summary>
	public class BloodSystem : MonoBehaviour
	{
		public float MaxToxinDamage = 200;

		private float toxinDamage = 0;

		/// <summary>
		/// How much toxin is found in the blood.
		/// </summary>
		public float ToxinDamage
		{
			get { return Mathf.Clamp(toxinDamage, 0, MaxToxinDamage); }
			set { toxinDamage = Mathf.Clamp(value, 0, MaxToxinDamage); }
		}

		/// <summary>
		/// How much toxin is found in the blood. 0% to 100%
		/// </summary>

		/// <summary>
		/// The lack of oxygen levels found in the blood.
		/// </summary>
		public float OxygenDamage
		{
			get { return Mathf.Clamp(oxygenDamage, 0, 200); }
			set { oxygenDamage = Mathf.Clamp(value, 0, 200); }
		}
		/// <summary>
		/// The heart rate affects the rate at which blood is pumped around the body
		/// This is only relevant on the Server.
		/// HeartRate value can be requested by a client via a NetMsg
		/// </summary>
		/// <value>Measured in BPM</value>
		public int HeartRate { get; set; } = 55; //Resting is 55. 0 = dead
		/// <summary>
		/// Is the Heart Stopped. Performing CPR might start it again
		/// </summary>
		public bool HeartStopped => HeartRate == 0;

		public float oxygenDamage = 0;
		private HealthSystem healthSystem;
		private DNAandBloodType bloodType;
		public float BloodLevel = (int)BloodVolume.NORMAL;
		public bool IsBleeding { get; private set; }
		private float tick = 0f;

		private BloodSplatType bloodSplatColor;

		void Awake()
		{
			healthSystem = GetComponent<HealthSystem>();
		}

		void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		//Initial setting for blood type. Server only
		public void SetBloodType(DNAandBloodType dnaBloodType)
		{
			bloodType = dnaBloodType;
			bloodSplatColor = dnaBloodType.BloodColor;
		}

		//Handle by UpdateManager
		void UpdateMe()
		{
			//Server Only:
			if (CustomNetworkManager.Instance._isServer)
			{
				if (healthSystem.IsDead)
				{
					HeartRate = 0;
					return;
				}

				tick += Time.deltaTime;
				if (HeartRate == 0)
				{
					// TODO Add ability to start heart again via CPR
					// Player needs to be in respiratory arrest and not
					// have any injuries that are incompatible with life
					tick = 0;
					return;
				}

				if (tick >= 60f / (float)HeartRate) //Heart rate determines loop time
				{
					tick = 0f;
					PumpBlood();
				}
			}
		}

		/// <summary>
		/// Where the blood pumping action happens
		/// </summary>
		void PumpBlood()
		{
			if (IsBleeding)
			{
				float bleedVolume = 0;
				for (int i = 0; i < healthSystem.bodyParts.Count; i++)
				{
					BodyPart bp = healthSystem.bodyParts[i];
					if (bp.IsBleeding)
					{
						bleedVolume += (bp.BruteDamage * 0.013f);
					}
				}
				LoseBlood(bleedVolume);
			}

			//TODO things that could affect heart rate, like low blood, crit status etc
		}

		/// <summary>
		/// Subtract an amount of blood from the player. Server Only
		/// </summary>
		public void AddBloodLoss(int amount, BodyPart bodyPart)
		{
			if (amount <= 0)
			{
				return;
			}
			TryBleed(bodyPart);
		}

		private void TryBleed(BodyPart bodyPart)
		{
			//TODO set a way to set bleeding status
			// bodyPart.isBleeding = true;
			//don't start another coroutine when already bleeding
			if (!IsBleeding)
			{
				IsBleeding = true;
			}
		}

		/// <summary>
		/// Stops bleeding on the selected bodypart. The bloodsystem continues bleeding if there's another bodypart bleeding. Server Only.
		/// </summary>
		public void StopBleeding(BodyPartBehaviour bodyPart)
		{
			bodyPart.isBleeding = false;
			foreach (var BPB in healthSystem.bodyParts)
			{
				if(BPB.IsBleeding)
				{
					return;
				}
			}

			IsBleeding = false;
		}

		/// <summary>
		/// Stops bleeding on all bodyparts. Server Only.
		/// </summary>
		public void StopBleedingAll()
		{
			for (int i = 0; i < healthSystem.bodyParts.Count; i++)
			{
				BodyPart BPB = healthSystem.bodyParts[i];
				//TODO set a way to stop bleeding status
				// BPB.IsBleeding = false;
			}
			IsBleeding = false;
		}

		private void LoseBlood(float amount)
		{
			if (amount <= 0)
			{
				return;
			}
			Logger.LogTraceFormat("{0} lost blood: {1}->{2}", Category.Health, this.gameObject.name, BloodLevel, BloodLevel - amount);
			BloodLevel -= amount;
			BloodSplatSize scaleOfTragedy;
			if (amount > 0 && amount < 15)
			{
				scaleOfTragedy = BloodSplatSize.small;
			}
			else if (amount >= 15 && amount < 40)
			{
				scaleOfTragedy = BloodSplatSize.medium;
			}
			else
			{
				scaleOfTragedy = BloodSplatSize.large;
			}


			EffectsFactory.BloodSplat(transform.position, scaleOfTragedy, bloodSplatColor);
		}

		/// <summary>
		/// Restore blood level
		/// </summary>
		private void RestoreBlood()
		{
			BloodLevel = (int)BloodVolume.NORMAL;
		}

		private static float BleedFactor(DamageType damageType)
		{
			float random = Random.Range(-0.2f, 0.2f);
			switch (damageType)
			{
				case DamageType.Brute:
					return 0.6f + random;
				case DamageType.Burn:
					return 0.4f + random;
				case DamageType.Tox:
					return 0.2f + random;
			}
			return 0;
		}

		/// <summary>
		/// Determine if there is any blood damage (toxin, oxygen loss) or bleeding that needs to occur
		/// Server only!
		/// </summary>
		public void AffectBloodState(BodyPartType bodyPartType, DamageType damageType, float amount, bool isHeal = false)
		{
			BodyPart bodyPart = healthSystem.FindBodyPart(bodyPartType);

			if (isHeal)
			{
				CheckHealing(bodyPart, damageType, amount);
				return;
			}

			//Check if limb should start bleeding (Bleeding is only for Players, not animals)
			if (damageType == DamageType.Brute)
			{
				int bloodLoss = (int)(Mathf.Clamp(amount, 0f, 10f) * BleedFactor(damageType));
				// start bleeding if the limb is really damaged
				if(bodyPart.BruteDamage > 40){
					AddBloodLoss(bloodLoss, bodyPart);
				}
			}

			if (damageType == DamageType.Tox)
			{
				ToxinDamage += amount;
			}
		}

		//Do any healing stuff:
		private void CheckHealing(BodyPart bodyPart, DamageType damageType, float healAmt)
		{
			//TODO: PRIORITY! Do Blood healing!
			Logger.Log("Not implemented: Blood healing.", Category.Health);
		}

		// --------------------
		// UPDATES FROM SERVER
		// --------------------

		public void UpdateClientBloodStats(int heartRate, float bloodVolume, float _oxygenDamage, float _toxinDamage)
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				return;
			}

			HeartRate = heartRate;
			BloodLevel = bloodVolume;
			OxygenDamage = _oxygenDamage;
			toxinDamage = _toxinDamage;
		}
	}
}