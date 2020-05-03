using System.Collections;
using Health;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update blood health stats
/// </summary>
public class HealthBloodMessage : ServerMessage
{
	public uint EntityToUpdate;
	public int HeartRate;
	public float BloodLevel;
	public float OxygenDamage;
	public float ToxinDamage;

	public override void Process()
	{
		LoadNetworkObject(EntityToUpdate);
		NetworkObject.GetComponent<HealthSystem>().UpdateClientBloodStats(HeartRate, BloodLevel, OxygenDamage, ToxinDamage);
	}

	public static HealthBloodMessage Send(GameObject recipient, GameObject entityToUpdate, int heartRate, float bloodLevel,
		float oxygenDamage, float toxinLevel)
	{
		HealthBloodMessage msg = new HealthBloodMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinDamage = toxinLevel
		};
		msg.SendTo(recipient);
		return msg;
	}

	public static HealthBloodMessage SendToAll(GameObject entityToUpdate, int heartRate, float bloodLevel,
		float oxygenDamage, float toxinLevel)
	{
		HealthBloodMessage msg = new HealthBloodMessage
		{
			EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
				HeartRate = heartRate,
				BloodLevel = bloodLevel,
				OxygenDamage = oxygenDamage,
				ToxinDamage = toxinLevel
		};
		msg.SendToAll();
		return msg;
	}
}