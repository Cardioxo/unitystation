using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using Light2D;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace Health
{
	/// <summary>
	/// The Required component for all living creatures
	/// Monitors and calculates health
	/// </summary>
	[RequireComponent(typeof(HealthStateMonitor))]
	public abstract class HealthSystem : NetworkBehaviour, IHealth, IFireExposable, IExaminable, IServerSpawn
	{
		private static readonly float GIB_THRESHOLD = 200f;
		//damage incurred per tick per fire stack
		private static readonly float DAMAGE_PER_FIRE_STACK = 0.08f;
		//volume and temp of hotspot exposed by this player when they are on fire
		private static readonly float BURNING_HOTSPOT_VOLUME = .005f;
		private static readonly float BURNING_HOTSPOT_TEMPERATURE = 700f;

		#region Inspector
		[Tooltip("Max amount of HP this creature has overall.")]
		public float maxHealth = 100;

		[SerializeField][Tooltip("Percentage of this creature's max hp to get into this state")]
		private float softCritPercentage = 50;

		[SerializeField][Tooltip("Percentage of this creature's max hp to get into this state")]
		private float critPercentage = 15;

		[SerializeField][Tooltip("Percentage of this creature's max hp to get into this state")]
		private float deadPercentage = 0;

		[Tooltip("For mobs that can breath in any atmos environment")]
		public bool canBreathAnywhere = false;//TODO take this out of main class, respiratory system instead

		[Tooltip("At what oxy damage amount will this creature pass out")]
		public float OxygenPassOut = 50; //TODO take this out of main class, use respiratory system instead

		[Tooltip("Damage to apply when cloning this creature")]
		public float cloningDamage = 0;

		[Tooltip("What color is this creature's blood?")]
		public BloodSplatType bloodColor; //TODO take this out of main class, use blood system intead

		[Tooltip("This creature's blood system")]
		public BloodSystem bloodSystem;

		[Tooltip("This creature's brain system")]
		public BrainSystem brainSystem;

		[Tooltip("This creature's respiratory system")]
		public RespiratorySystem respiratorySystem;

		/// <summary>
		/// If there are any body parts for this living thing, then add them to this list
		/// via the inspector. There needs to be at least 1 chest bodypart for a living animal
		/// </summary>
		[Header("Fill BodyPart fields in via Inspector:")]
		[Tooltip("This creature's default body parts. At least a chest is needed for the simplest of life forms")]
		//public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();//TODO this is old implementation, commenting for now
		public List<BodyPart> bodyParts = new List<BodyPart>();
		//For meat harvest (pete etc)
		public bool allowKnifeHarvest; //TODO eliminate this, use harvesteable component instead
		#endregion

		#region Public getters/setters
		/// <summary>
		/// Server side, each mob has a different one and never it never changes
		/// </summary>
		public int mobID { get; private set; }
		public float SoftCritThreshold => maxHealth * (softCritPercentage / 100);
		public float CritThreshold => maxHealth * (critPercentage / 100);
		public float DeadThreshold => maxHealth * (deadPercentage / 100);
		public float OverallHealth { get; protected set; }
		public float OverallHealthPercentage => (OverallHealth / maxHealth) * 100;
		public ConsciousState ConsciousState
		{
			get => consciousState;
			protected set
			{
				ConsciousState oldState = consciousState;
				if (value != oldState)
				{
					consciousState = value;
					if (isServer)
					{
						OnConsciousStateChangeServer.Invoke(oldState, value);
					}
				}
			}
		}

		/// <summary>
		/// Is the creature unconscious
		/// </summary>
		public bool IsCrit => ConsciousState == ConsciousState.UNCONSCIOUS;
		/// <summary>
		/// Is the creature barely conscious
		/// </summary>
		public bool IsSoftCrit => ConsciousState == ConsciousState.BARELY_CONSCIOUS;
		/// <summary>
		/// Is the creature dead
		/// </summary>
		public bool IsDead => ConsciousState == ConsciousState.DEAD;
		/// <summary>
		/// Has the heart stopped.
		/// </summary>
		public bool IsCardiacArrest => bloodSystem.HeartStopped;
		/// <summary>
		/// Implementation from IHealth. Used to determine what happens on matrix collision (We think)
		/// </summary>
		public float Resistance { get; } = 50;
		/// <summary>
		/// How on fire we are. Exists client side - synced with server.
		/// </summary>
		public float FireStacks => fireStacks;


		#endregion

		#region Events declaration
		/// <summary>
		/// Triggers when this creature have received damage
		/// </summary>
		public event Action<GameObject> ApplyDamageEvent;
		/// <summary>
		/// Triggers when this creature has died
		/// </summary>
		public event Action OnDeathNotifyEvent;
		/// <summary>
		/// Client side event which fires when this object's fire status changes
		/// (becoming on fire, extinguishing, etc...). Use this to update
		/// burning sprites.
		/// </summary>
		[NonSerialized] public FireStackEvent OnClientFireStacksChange = new FireStackEvent();
		/// <summary>
		/// Invoked when conscious state changes. Provides old state and new state as 1st and 2nd args.
		/// </summary>
		[NonSerialized] public ConsciousStateEvent OnConsciousStateChangeServer = new ConsciousStateEvent();
		#endregion

		#region private properties
		/// <summary>
		/// Serverside, used for gibbing bodies after certain amount of damage is received after death
		/// </summary>
		private float afterDeathDamage = 0f;
		protected DamageType LastDamageType;
		protected GameObject LastDamagedBy;

		// JSON string for blood types and DNA.
		[SyncVar(hook = nameof(DNASync))] //May remove this in the future and only provide DNA info on request
		private string DNABloodTypeJSON;

		//how on fire we are, sames as tg fire_stacks. 0 = not on fire.
		//It's called "stacks" but it's really just a floating point value that
		//can go up or down based on possible sources of being on fire. Max seems to be 20 in tg.
		[SyncVar(hook=nameof(SyncFireStacks))] private float fireStacks;

		// BloodType and DNA Data.
		private DNAandBloodType DNABloodType;
		private float tickRate = 1f;
		private float tick = 0;
		private RegisterTile registerTile;
		private ConsciousState consciousState;
		#endregion

		#region Init Methods
		public virtual void Awake()
		{
			EnsureInit();

		}

		void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void EnsureInit()
		{
			if (registerTile != null)
			{
				return;
			}

			registerTile = GetComponent<RegisterTile>();
			SubscribeInternalEvents();
			InitSubsystems();
		}

		private void SubscribeInternalEvents()
		{
			OnDeathNotifyEvent += OnDeath;
			ApplyDamageEvent += OnDamageReceived;

			foreach (var bodyPart in bodyParts)
			{
				bodyPart.BleedingStateChanged += OnBleedingStateChanged;
				bodyPart.DismemberStateChanged += OnDismemberStateChanged;
				bodyPart.MangledStateChanged += OnMangledStateChanged;
			}

			//TODO subscribe to subsystems (respiratory, blood, brain) events
		}

		private void UnsubscribeAll()
		{
			ApplyDamageEvent -= OnDamageReceived;

			foreach (var bodyPart in bodyParts)
			{
				bodyPart.BleedingStateChanged -= OnBleedingStateChanged;
				bodyPart.DismemberStateChanged -= OnDismemberStateChanged;
				bodyPart.MangledStateChanged -= OnMangledStateChanged;
			}

			//TODO unsubscribe subsystems (respiratory, blood, brain) events


			OnDeathNotifyEvent -= OnDeath;
		}

		#endregion

		// This is the DNA SyncVar hook
		private void DNASync(string oldDNA, string updatedDNA)
		{
			EnsureInit();
			DNABloodTypeJSON = updatedDNA;
			DNABloodType = JsonUtility.FromJson<DNAandBloodType>(updatedDNA);
		}

		#region Public functions
		/// <summary>
		///  Apply Damage to the whole body of this Living thing. Server only
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damage">Damage Amount. will be distributed evenly across all body parts</param>
		/// <param name="attackType">type of attack that is causing the damage</param>
		/// <param name="damageType">The Type of Damage</param>
		[Server]
		public void ApplyDamage( GameObject damagedBy, float damage, AttackType attackType, DamageType damageType )
		{
			foreach ( var bodyPart in bodyParts )
			{
				ApplyDamageToBodypart(
					damagedBy,
					damage/bodyParts.Count,
					attackType,
					damageType,
					bodyPart.bodyPartData.bodyPartType );
			}
		}

		/// <summary>
		///  Apply Damage to random body part of the Living thing. Server only
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damage">Damage Amount</param>
		/// <param name="attackType">type of attack that is causing the damage</param>
		/// <param name="damageType">The Type of Damage</param>
		[Server]
		public void ApplyDamageToBodypart( GameObject damagedBy, float damage,
			AttackType attackType, DamageType damageType )
		{
			ApplyDamageToBodypart( damagedBy, damage, attackType, damageType, BodyPartType.Chest.Randomize( 0 ) );
		}

		/// <summary>
		///  Apply Damage to the Living thing. Server only
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damage">Damage Amount</param>
		/// <param name="attackType">type of attack that is causing the damage</param>
		/// <param name="damageType">The Type of Damage</param>
		/// <param name="bodyPartAim">Body Part that is affected</param>
		[Server]
		public virtual void ApplyDamageToBodypart(
			GameObject damagedBy, float damage, AttackType attackType, DamageType damageType, BodyPartType bodyPartAim)
		{
			TryGib(damage);

			BodyPart bodyPart = GetBodyPart(damage, damageType, bodyPartAim);

			if(bodyPart == null)
			{
				return;
			}

			var prevHealth = OverallHealth;

			ApplyDamageEvent?.Invoke(damagedBy);

			LastDamageType = damageType;
			LastDamagedBy = damagedBy;
			bodyPart.ReceiveDamage(damageType, bodyPart.Armor.GetDamage(damage, attackType));
			HealthBodyPartMessage.Send(
				gameObject,
				gameObject,
				bodyPartAim,
				bodyPart.BruteDamage,
				bodyPart.BurnDamage);


			if (attackType == AttackType.Fire)
			{
				SyncFireStacks(fireStacks, fireStacks+1);
			}

			//For special effects spawning like blood:
			DetermineDamageEffects(damageType);

			Logger.LogTraceFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
				damage, prevHealth, OverallHealth, gameObject.name, damageType, bodyPartAim, damagedBy);
		}

		/// <summary>
		///  Apply healing to a living thing. Server Only
		/// </summary>
		/// <param name="healingItem">the item used for healing (bruise pack etc). Null if there is none</param>
		/// <param name="healAmt">Amount of healing to add</param>
		/// <param name="damageType">The Type of Damage To Heal</param>
		/// <param name="bodyPartAim">Body Part to heal</param>
		[Server]
		public virtual void HealDamage(GameObject healingItem, int healAmt,
			DamageType damageTypeToHeal, BodyPartType bodyPartAim)
		{
			BodyPart bodyPartBehaviour = GetBodyPart(healAmt, damageTypeToHeal, bodyPartAim);
			if (bodyPartBehaviour == null)
			{
				return;
			}
			bodyPartBehaviour.HealDamage(healAmt, damageTypeToHeal);
			HealthBodyPartMessage.Send(gameObject, gameObject, bodyPartAim, bodyPartBehaviour.BruteDamage, bodyPartBehaviour.BurnDamage);

			var prevHealth = OverallHealth;
			Logger.LogTraceFormat("{3} received {0} {4} healing from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
				healAmt, prevHealth, OverallHealth, gameObject.name, damageTypeToHeal, bodyPartAim, healingItem);
		}

		public virtual void InitSubsystems()
		{
			//TODO get subsystems from inner organs instead.
			// use this method to initialize at spawn and to reinitialize when an inner organ has changed

			//TODO this is old implementation, replace with reading from inner organs instead
			//Always include blood for living entities:
			bloodSystem = GetComponent<BloodSystem>();
			if (bloodSystem == null)
			{
				bloodSystem = gameObject.AddComponent<BloodSystem>();
			}

			//Always include respiratory for living entities:
			respiratorySystem = GetComponent<RespiratorySystem>();
			if (respiratorySystem == null)
			{
				respiratorySystem = gameObject.AddComponent<RespiratorySystem>();
			}

			respiratorySystem.canBreathAnywhere = canBreathAnywhere;

			var tryGetHead = FindBodyPart(BodyPartType.Head);
			if (tryGetHead != null && brainSystem == null)
			{
				if (tryGetHead.bodyPartData.bodyPartType != BodyPartType.Chest)
				{
					//Head exists, install a brain system
					brainSystem = gameObject.AddComponent<BrainSystem>();
				}
			}

			//Generate BloodType and DNA
			DNABloodType = new DNAandBloodType();
			DNABloodType.BloodColor = bloodColor;
			DNABloodTypeJSON = JsonUtility.ToJson(DNABloodType);
			bloodSystem.SetBloodType(DNABloodType);
		}

		public void Extinguish()
		{
			SyncFireStacks(fireStacks, 0);
		}

		public void ChangeFireStacks(float deltaValue)
		{
			SyncFireStacks(fireStacks, fireStacks + deltaValue);
		}

		#endregion

		private void SyncFireStacks(float oldValue, float newValue)
		{
			EnsureInit();
			this.fireStacks = Math.Max(0,newValue);
			OnClientFireStacksChange.Invoke(this.fireStacks);
		}

		private BodyPart GetBodyPart(float amount, DamageType damageType, BodyPartType bodyPartAim = BodyPartType.Chest)
		{
			if (amount <= 0 || IsDead)
			{
				return null;
			}

			//convert micro body part to macro
			//TODO make eyes damage eyes inner organ
			switch (bodyPartAim)
			{
				case BodyPartType.Groin:
					bodyPartAim = BodyPartType.Chest;
					break;
				case BodyPartType.Eyes:
					bodyPartAim = BodyPartType.Head;
					break;
				case BodyPartType.Mouth:
					bodyPartAim = BodyPartType.Head;
					break;
			}

			if (bodyParts.Count == 0)
			{
				Logger.LogError($"There are no body parts to apply a health change to for {gameObject.name}", Category.Health);
				return null;
			}

			//See if damage affects the state of the blood:
			// See if any of the healing applied affects blood state
			bloodSystem.AffectBloodState(bodyPartAim, damageType, amount);

			if (damageType != DamageType.Brute && damageType != DamageType.Burn)
			{
				return null;
			}

			BodyPart bodyPart = null;

			foreach (var bp in bodyParts)
			{
				if (bp.bodyPartData.bodyPartType != bodyPartAim)
				{
					continue;
				}

				bodyPart = bp;
				break;
			}

			//If the body part does not exist then try to find the chest instead
			if (bodyPart != null)
			{
				return bodyPart;
			}

			var getChestIndex = bodyParts.FindIndex(x => x.bodyPartData.bodyPartType == BodyPartType.Chest);
			if (getChestIndex != -1)
			{
				bodyPart = bodyParts[getChestIndex];
			}
			else
			{
				//If there is no default chest body part then do nothing
				Logger.LogError($"No chest body part found for {gameObject.name}", Category.Health);
				return null;
			}
			return bodyPart;
		}

		public BodyPart FindBodyPart(BodyPartType bodyPartAim)
		{
			int searchIndex = bodyParts.FindIndex(x => x.bodyPartData.bodyPartType == bodyPartAim);
			if (searchIndex != -1)
			{
				return bodyParts[searchIndex];
			}
			//If nothing is found then try to find a chest component
			// else nothing:
			searchIndex = bodyParts.FindIndex(x => x.bodyPartData.bodyPartType == BodyPartType.Chest);
			return searchIndex != -1 ? bodyParts[searchIndex] : null;
		}

		/// <summary>
		/// Reset all body part damage.
		/// </summary>
		[Server]
		private void ResetBodyParts()
		{
			foreach (BodyPart bodyPart in bodyParts)
			{
				bodyPart.RestoreDamage();
			}
		}

		#region Update loop
		//TODO take everything you can outside the update loop

		//Handled via UpdateManager
		protected virtual void UpdateMe()
		{
			//Server Only:
			if (!isServer || IsDead)
			{
				return;
			}

			tick += Time.deltaTime;
			if (tick < tickRate)
			{
				return;
			}

			tick = 0f;
			if (fireStacks > 0)
			{
				HandleFireDamage();
			}

			//TODO stop calculation every tick, calculate only when health changed
			// CalculateOverallHealth();
			// CheckHealthAndUpdateConsciousState();
		}

		protected void HandleFireDamage()
		{
			//TODO: Burn clothes (see species.dm handle_fire)
			ApplyDamageToBodypart(null, fireStacks * DAMAGE_PER_FIRE_STACK, AttackType.Fire, DamageType.Burn);
			//gradually deplete fire stacks
			SyncFireStacks(fireStacks, fireStacks - 0.1f);
			//instantly stop burning if there's no oxygen at this location
			MetaDataNode node = registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionClient);
			if (node.GasMix.GetMoles(Gas.Oxygen) < 1)
			{
				SyncFireStacks(fireStacks, 0);
			}

			registerTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(gameObject.TileWorldPosition(),
				BURNING_HOTSPOT_TEMPERATURE, BURNING_HOTSPOT_VOLUME);
		}
		#endregion

		#region Visual effects
		/// <Summary>
		/// Used to determine any special effects spawning cased by a damage type
		/// Server only
		/// </Summary>
		[Server]
		protected virtual void DetermineDamageEffects(DamageType damageType)
		{
			//Brute attacks
			if (damageType == DamageType.Brute)
			{
				//spawn blood
				EffectsFactory.BloodSplat(registerTile.WorldPositionServer, BloodSplatSize.medium, bloodColor);
			}
		}
		#endregion

		#region Health calculation
		/// <summary>
		/// Recalculates the overall player health and updates OverallHealth property. Server only
		/// </summary>
		[Server]
		protected virtual void CalculateOverallHealth()
		{
			float newHealth = maxHealth;
			newHealth -= CalculateOverallBodyPartDamage();
			newHealth -= CalculateOverallBloodLossDamage();
			newHealth -= bloodSystem.OxygenDamage;
			newHealth -= cloningDamage;
			OverallHealth = newHealth;
		}

		protected float CalculateOverallBodyPartDamage()
		{
			float bodyPartDmg = 0;
			foreach (var part in bodyParts)
			{
				bodyPartDmg += part.OverallDamage;
			}
			return bodyPartDmg;
		}

		public float GetTotalBruteDamage()
		{
			float bruteDmg = 0;
			foreach (var part in bodyParts)
			{
				bruteDmg += part.BruteDamage;
			}
			return bruteDmg;
		}

		public float GetTotalBurnDamage()
		{
			float burnDmg = 0;
			foreach (var part in bodyParts)
			{
				burnDmg += part.BurnDamage;
			}
			return burnDmg;
		}

		/// Blood Loss and Toxin damage:
		public int CalculateOverallBloodLossDamage()
		{
			float maxBloodDmg = Mathf.Abs(DeadThreshold) + maxHealth;
			float bloodDmg = 0f;
			if (bloodSystem.BloodLevel < (int)BloodVolume.SAFE)
			{
				bloodDmg = Mathf.Lerp(0f, maxBloodDmg, 1f - (bloodSystem.BloodLevel / (float)BloodVolume.NORMAL));
			}

			if (bloodSystem.ToxinDamage > 1f)
			{
				//TODO determine a way to handle toxin damage when toxins are implemented
				//There will need to be some kind of blood / toxin ratio and severity limits determined
			}

			return Mathf.RoundToInt(Mathf.Clamp(bloodDmg, 0f, maxBloodDmg));
		}

		#endregion

		#region Crit + death methods
		///Death from other causes
		public virtual void Death()
		{
			if (IsDead)
			{
				return;
			}

			OnDeathNotifyEvent?.Invoke();
			afterDeathDamage = 0;
			ConsciousState = ConsciousState.DEAD;
			bloodSystem.StopBleedingAll();
			//stop burning
			//TODO: When clothes/limb burning is implemented, probably should keep burning until clothes are burned up
			SyncFireStacks(fireStacks, 0);
		}

		private void Crit(bool allowCrawl = false)
		{
			var proposedState = allowCrawl ? ConsciousState.BARELY_CONSCIOUS : ConsciousState.UNCONSCIOUS;

			if (ConsciousState == proposedState || IsDead)
			{
				return;
			}

			ConsciousState = proposedState;
		}

		private void Uncrit()
		{
			var proposedState = ConsciousState.CONSCIOUS;
			if (ConsciousState == proposedState || IsDead)
			{
				return;
			}
			ConsciousState = proposedState;
		}

		/// <summary>
		/// Checks if the player's health has changed such that consciousstate needs to be changed,
		/// and changes consciousstate and invokes whatever needs to be invoked when the state changes
		/// </summary>
		protected virtual void CheckHealthAndUpdateConsciousState()
		{
			if (ShouldBeDead())
			{
				Death();
				return;
			}

			if (ShouldBeUnconscious())
			{
				Crit(!(OverallHealth <= CritThreshold));
				Logger.LogFormat(
					"{0} is in {1}",
					Category.Health,
					gameObject.name,
					OverallHealth <= CritThreshold ? "softcrit" : "crit");
				return;
			}

			if (!ShouldBeConscious())
			{
				return;
			}

			Logger.LogFormat( "{0}, back on your feet!", Category.Health, gameObject.name );
			Uncrit();
			return;
		}

		private bool ShouldBeConscious()
		{
			return ConsciousState != ConsciousState.CONSCIOUS
			       && bloodSystem.OxygenDamage < OxygenPassOut
			       && OverallHealth >= SoftCritThreshold;
		}

		private bool ShouldBeUnconscious()
		{
			return  OverallHealth <= SoftCritThreshold || bloodSystem.OxygenDamage > OxygenPassOut;
		}

		private bool ShouldBeDead()
		{
			return OverallHealth < DeadThreshold && !IsDead;
		}

		protected void RaiseDeathNotifyEvent()
		{
			OnDeathNotifyEvent?.Invoke();
		}
		#endregion

		#region Update client methods
		// Stats are separated so that the server only updates the area of concern when needed

		/// <summary>
		/// Updates the main health stats from the server via NetMsg
		/// </summary>
		public void UpdateClientHealthStats(float overallHealth)
		{
			OverallHealth = overallHealth;
			//	Logger.Log($"Update stats for {gameObject.name} OverallHealth: {overallHealth} ConsciousState: {consciousState.ToString()}", Category.Health);
		}

		/// <summary>
		/// Updates the conscious state from the server via NetMsg
		/// </summary>
		public void UpdateClientConsciousState(ConsciousState proposedState)
		{
			ConsciousState = proposedState;
		}

		/// <summary>
		/// Updates the respiratory health stats from the server via NetMsg
		/// </summary>
		public void UpdateClientRespiratoryStats(bool value)
		{
			respiratorySystem.IsSuffocating = value;
		}

		public void UpdateClientTemperatureStats(float value)
		{
			respiratorySystem.temperature = value;
		}

		public void UpdateClientPressureStats(float value)
		{
			respiratorySystem.pressure = value;
		}

		/// <summary>
		/// Updates the blood health stats from the server via NetMsg
		/// </summary>
		public void UpdateClientBloodStats(int heartRate, float bloodVolume, float oxygenDamage, float toxinDamage)
		{
			bloodSystem.UpdateClientBloodStats(heartRate, bloodVolume, oxygenDamage, toxinDamage);
		}

		/// <summary>
		/// Updates the brain health stats from the server via NetMsg
		/// </summary>
		public void UpdateClientBrainStats(bool isHusk, int brainDamage)
		{
			if (brainSystem != null)
			{
				brainSystem.UpdateClientBrainStats(isHusk, brainDamage);
			}
		}

		//TODO figure this shit out
		/// <summary>
		/// Updates the bodypart health stats from the server via NetMsg
		/// </summary>
		public void UpdateClientBodyPartStats(BodyPartType bodyPartType, float bruteDamage, float burnDamage)
		{
			var bodyPart = FindBodyPart(bodyPartType);
			if (bodyPart == null)
			{
				return;
			}

			Logger.Log(
				$"Update stats for {gameObject.name}" +
				$" body part {bodyPartType.ToString()}" +
				$" BruteDmg: {bruteDamage} " +
				$" BurnDamage: {burnDamage}",
				Category.Health);

			bodyPart.UpdateClientBodyPartStat(bruteDamage, burnDamage);
		}
		#endregion

		#region Event methods

		public override void OnStartServer()
		{
			EnsureInit();
			mobID = PlayerManager.Instance.GetMobID();
			ResetBodyParts();
			if (maxHealth <= 0)
			{
				Logger.LogWarning($"Max health ({maxHealth}) set to zero/below zero!", Category.Health);
				maxHealth = 1;
			}
		}

		public override void OnStartClient()
		{
			EnsureInit();
			StartCoroutine(WaitForClientLoad());
		}

		IEnumerator WaitForClientLoad()
		{
			//wait for DNA:
			while (string.IsNullOrEmpty(DNABloodTypeJSON))
			{
				yield return WaitFor.EndOfFrame;
			}
			yield return WaitFor.EndOfFrame;
			DNASync(DNABloodTypeJSON, DNABloodTypeJSON);
			SyncFireStacks(fireStacks, this.fireStacks);
		}

		public void OnExposed(FireExposure exposure)
		{
			Profiler.BeginSample("PlayerExpose");
			ApplyDamage(null, 1, AttackType.Fire, DamageType.Burn);
			Profiler.EndSample();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			ConsciousState = ConsciousState.CONSCIOUS;
			OverallHealth = maxHealth;
			ResetBodyParts();
			CalculateOverallHealth();
		}

		protected virtual void OnDeath()
		{
			UnsubscribeAll();
			//TODO Start husk/decomposition coroutine
		}

		protected virtual void OnDamageReceived(GameObject damagedBy)
		{
			CalculateOverallHealth();
			//TODO update clients UI from here.
			CheckHealthAndUpdateConsciousState();

			// throw new NotImplementedException();
		}

		protected virtual void OnBleedingStateChanged(BodyPart bodyPart, bool isBleeding)
		{
			//TODO start or stop blood loss in blood system from here
			// throw new NotImplementedException();
		}

		protected virtual void OnDismemberStateChanged(BodyPart bodyPart, bool isDismembered)
		{
			// throw new NotImplementedException();
		}

		private void OnMangledStateChanged(BodyPart bodyPart, bool isMangled)
		{
			// throw new NotImplementedException();
		}

		#endregion

		#region Misc functions
		[Server]
		public virtual void Gib()
		{
			EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
			//todo: actual gibs

			//never destroy players!
			Despawn.ServerSingle(gameObject);
		}

		private void TryGib(float damage)
		{
			if (!IsDead)
			{
				return;
			}

			afterDeathDamage += damage;
			if (afterDeathDamage >= GIB_THRESHOLD)
			{
				Gib(); //TODO add fancy gibs
			}
		}

		private void OnDrawGizmos()
		{
			if ( !Application.isPlaying )
			{
				return;
			}
			Gizmos.color = Color.blue.WithAlpha( 0.5f );
			Gizmos.DrawCube( registerTile.WorldPositionServer, Vector3.one );
		}

		//TODO finish this implementation. Move all of this to an interface
		/// <summary>
		/// This is just a simple initial implementation of IExaminable to health;
		/// can potentially be extended to return more details and let the server
		/// figure out what to pass to the client, based on many parameters such as
		/// role, medical skill (if they get implemented), equipped medical scanners,
		/// etc. In principle takes care of building the string from start to finish,
		/// so logic generating examine text can be completely separate from examine
		/// request or netmessage processing.
		/// </summary>
		public string Examine(Vector3 worldPos)
		{
			var healthString  = "";

			if (!IsDead)
			{
				if (OverallHealthPercentage < 20)
				{
					healthString = "heavily wounded.";
				}
				else if (OverallHealthPercentage < 60)
				{
					healthString = "wounded.";
				}
				else
				{
					healthString = "in good shape.";
				}

				// On fire?
				if (FireStacks > 0)
				{
					healthString = "on fire!";
				}

				healthString = ConsciousState.ToString().ToLower().Replace("_", " ") + " and " + healthString;
			}
			else
			{
				healthString = "limp and unresponsive. There are no signs of life...";
			}

			// Assume animal
			string pronoun = "It";
			var cs = GetComponentInParent<PlayerScript>()?.characterSettings;
			if (cs != null)
			{
				pronoun = cs.PersonalPronoun();
				pronoun = pronoun[0].ToString().ToUpper() + pronoun.Substring(1);
			}

			healthString = pronoun + " is " + healthString + (respiratorySystem.IsSuffocating && !IsDead ? " " + pronoun + " is having trouble breathing!" : "");
			return healthString;
		}
		#endregion
	}

	/// <summary>
	/// Event which fires when fire stack value changes.
	/// </summary>
	public class FireStackEvent : UnityEvent<float>
	{
	}

	/// <summary>
	/// Communicates fire status changes.
	/// </summary>
	public class FireStatus
	{
		//whether becoming on fire or extinguished
		public readonly bool IsOnFire;
		//whether we are engulfed by flames or just partially on fire
		public readonly bool IsEngulfed;

		public FireStatus(bool isOnFire, bool isEngulfed)
		{
			IsOnFire = isOnFire;
			IsEngulfed = isEngulfed;
		}
	}

	/// <summary>
	/// Event which fires when conscious state changes, provides the old state and the new state
	/// </summary>
	public class ConsciousStateEvent : UnityEvent<ConsciousState, ConsciousState>
	{
	}
}