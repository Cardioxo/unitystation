using System.Collections;
using Health;
using UnityEngine;

public class GibMessage : ServerMessage
{
	public override void Process()
	{
		foreach (HealthSystem living in Object.FindObjectsOfType<HealthSystem>())
		{
			living.Death();
		}
	}

	public static GibMessage Send()
	{
		GibMessage msg = new GibMessage();
		msg.SendToAll();
		return msg;
	}
}