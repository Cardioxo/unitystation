using System;
using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///		Health Monitoring component for all Living entities
///     Monitors the state of the entities health on the server and acts accordingly
/// </summary>
public class HealthStateMonitor : ManagedNetworkBehaviour
{
	//Cached members
	float overallHealthCache;
	ConsciousState consciousStateCache;
	bool isSuffocatingCache;
	float temperatureCache;
	float pressureCache;
	int heartRateCache;
	float bloodLevelCache;
	float oxygenDamageCache;
	float toxinLevelCache;
	bool isHuskCache;
	int brainDamageCache;

	private HealthSystem healthSystem;
	float tickRate = 1f;
	float tick = 0f;
	bool init = false;

	/// ---------------------------
	/// INIT FUNCTIONS
	/// ---------------------------
	void Awake()
	{
		healthSystem = GetComponent<HealthSystem>();
	}

	public override void OnStartServer()
	{
		InitServerCache();
		base.OnStartServer();
	}

	void InitServerCache()
	{
		overallHealthCache = healthSystem.OverallHealth;
		consciousStateCache = healthSystem.ConsciousState;
		isSuffocatingCache = healthSystem.respiratorySystem.IsSuffocating;
		temperatureCache = healthSystem.respiratorySystem.temperature;
		pressureCache = healthSystem.respiratorySystem.pressure;
		UpdateBloodCaches();
		if (healthSystem.brainSystem != null)
		{
			isHuskCache = healthSystem.brainSystem.IsHuskServer;
			brainDamageCache = healthSystem.brainSystem.BrainDamageAmt;
		}
		init = true;
	}

	void UpdateBloodCaches()
	{
		heartRateCache = healthSystem.bloodSystem.HeartRate;
		bloodLevelCache = healthSystem.bloodSystem.BloodLevel;
		oxygenDamageCache = healthSystem.bloodSystem.OxygenDamage;
		toxinLevelCache = healthSystem.bloodSystem.ToxinLevel;
	}

	/// ---------------------------
	/// SYSTEM MONITOR
	/// ---------------------------
	public override void UpdateMe()
	{
		if (isServer && init)
		{
			MonitorCrucialStats();
			tick += Time.deltaTime;
			if (tick > tickRate)
			{
				tick = 0f;
				MonitorNonCrucialStats();
			}
		}
	}

	// Monitoring stats that need to be updated straight away on client if there is any change
	[Server]
	void MonitorCrucialStats()
	{
		CheckOverallHealth();
		CheckRespiratoryHealth();
		CheckTemperature();
		CheckPressure();
		CheckCruicialBloodHealth();
		CheckConsciousState();
	}

	// Monitoring stats that don't need to be updated straight away on clients
	// (changes are updated at 1 second intervals)
	[Server]
	void MonitorNonCrucialStats()
	{
		CheckNonCrucialBloodHealth();
		if (healthSystem.brainSystem != null)
		{
			CheckNonCrucialBrainHealth();
		}
	}

	void CheckConsciousState()
	{
		if (consciousStateCache != healthSystem.ConsciousState)
		{
			consciousStateCache = healthSystem.ConsciousState;
			SendConsciousUpdate();
		}
	}

	void CheckOverallHealth()
	{
		if (overallHealthCache != healthSystem.OverallHealth)
		{
			overallHealthCache = healthSystem.OverallHealth;
			SendOverallUpdate();
		}
	}

	void CheckRespiratoryHealth()
	{
		if (isSuffocatingCache != healthSystem.respiratorySystem.IsSuffocating)
		{
			isSuffocatingCache = healthSystem.respiratorySystem.IsSuffocating;
			SendRespiratoryUpdate();
		}
	}

	void CheckTemperature()
	{
		if (temperatureCache != healthSystem.respiratorySystem.temperature)
		{
			temperatureCache = healthSystem.respiratorySystem.temperature;
			SendTemperatureUpdate();
		}
	}

	void CheckPressure()
	{
		if (pressureCache != healthSystem.respiratorySystem.pressure)
		{
			pressureCache = healthSystem.respiratorySystem.pressure;
			SendPressureUpdate();
		}
	}

	void CheckCruicialBloodHealth()
	{
		if (toxinLevelCache != healthSystem.bloodSystem.ToxinLevel ||
			heartRateCache != healthSystem.bloodSystem.HeartRate)
		{
			UpdateBloodCaches();
			SendBloodUpdate();
		}
	}

	void CheckNonCrucialBloodHealth()
	{
		if (bloodLevelCache != healthSystem.bloodSystem.BloodLevel ||
			oxygenDamageCache != healthSystem.bloodSystem.OxygenDamage)
		{
			UpdateBloodCaches();
			SendBloodUpdate();
		}
	}

	void CheckNonCrucialBrainHealth()
	{
		if (isHuskCache != healthSystem.brainSystem.IsHuskServer ||
			brainDamageCache != healthSystem.brainSystem.BrainDamageAmt)
		{
			isHuskCache = healthSystem.brainSystem.IsHuskServer;
			brainDamageCache = healthSystem.brainSystem.BrainDamageAmt;
			SendBrainUpdate();
		}
	}

	/// ---------------------------
	/// SEND TO ALL SERVER --> CLIENT
	/// ---------------------------

	void SendConsciousUpdate()
	{
		HealthConsciousMessage.SendToAll(gameObject, healthSystem.ConsciousState);
	}

	void SendOverallUpdate()
	{
		HealthOverallMessage.Send(gameObject, gameObject, healthSystem.OverallHealth);
	}

	void SendBloodUpdate()
	{
		HealthBloodMessage.Send(gameObject, gameObject, heartRateCache, bloodLevelCache,
			oxygenDamageCache, toxinLevelCache);
	}

	void SendBrainUpdate()
	{
		if (healthSystem.brainSystem != null)
		{
			HealthBrainMessage.SendToAll(gameObject, healthSystem.brainSystem.IsHuskServer,
				healthSystem.brainSystem.BrainDamageAmt);
		}
	}

	/// ---------------------------
	/// SEND TO INDIVIDUAL CLIENT
	/// ---------------------------

	void SendOverallUpdate(GameObject requestor)
	{
		HealthOverallMessage.Send(requestor, gameObject, healthSystem.OverallHealth);
	}

	void SendConsciousUpdate(GameObject requestor)
	{
		HealthConsciousMessage.Send(requestor, gameObject, healthSystem.ConsciousState);
	}

	void SendBloodUpdate(GameObject requestor)
	{
		HealthBloodMessage.Send(requestor, gameObject, heartRateCache, bloodLevelCache,
			oxygenDamageCache, toxinLevelCache);
	}

	void SendRespiratoryUpdate()
	{
		HealthRespiratoryMessage.Send(gameObject, isSuffocatingCache);
	}

	void SendTemperatureUpdate()
	{
		HealthTemperatureMessage.Send(gameObject, temperatureCache);
	}

	void SendPressureUpdate()
	{
		HealthPressureMessage.Send(gameObject, pressureCache);
	}

	void SendBrainUpdate(GameObject requestor)
	{
		if (healthSystem.brainSystem != null)
		{
			HealthBrainMessage.Send(requestor, gameObject, healthSystem.brainSystem.IsHuskServer,
				healthSystem.brainSystem.BrainDamageAmt);
		}
	}

	/// ---------------------------
	/// CLIENT REQUESTS
	/// ---------------------------

	public void ProcessClientUpdateRequest(GameObject requestor)
	{
		StartCoroutine(ControlledClientUpdate(requestor));
	//	Logger.Log("Server received a request for health update from: " + requestor.name + " for: " + gameObject.name);
	}

	/// <summary>
	/// This is mainly used to update new Clients on connect.
	/// So we do not spam too many net messages at once for a direct
	/// client update, control the rate of update slowly:
	/// </summary>
	IEnumerator ControlledClientUpdate(GameObject requestor)
	{
		SendConsciousUpdate(requestor);

		yield return WaitFor.Seconds(.1f);

		SendOverallUpdate(requestor);

		yield return WaitFor.Seconds(.1f);

		SendBloodUpdate(requestor);

		yield return WaitFor.Seconds(.1f);

		SendRespiratoryUpdate();

		yield return WaitFor.Seconds(.1f);

		SendTemperatureUpdate();

		yield return WaitFor.Seconds(.1f);

		SendPressureUpdate();

		yield return WaitFor.Seconds(.1f);

		if (healthSystem.brainSystem != null)
		{
			SendBrainUpdate(requestor);
			yield return WaitFor.Seconds(.1f);
		}

		for (int i = 0; i < healthSystem.BodyParts.Count; i++)
		{
			HealthBodyPartMessage.Send(requestor, gameObject,
				healthSystem.BodyParts[i].Type,
				healthSystem.BodyParts[i].BruteDamage,
				healthSystem.BodyParts[i].BurnDamage);
			yield return WaitFor.Seconds(.1f);
		}
	}
}