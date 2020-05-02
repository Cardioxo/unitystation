﻿using System.Collections.Generic;
using Health;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
	public GameObject toxinAlert;
	public GameObject heatAlert;
	public GameObject coldAlert;
	public UI_PressureAlert pressureAlert;
	public GameObject oxygenAlert;
	public UI_TemperatureAlert temperatureAlert;
	public GameObject hungerAlert;
	public Button oxygenButton;
	public UI_HeartMonitor heartMonitor;
	public List<DamageMonitorListener> bodyPartListeners = new List<DamageMonitorListener>();
	public GameObject baseBody;
	public GameObject alertsBox;

	public bool humanUI;

	void Awake()
	{
		DisableAll();
	}

	private void DisableAll()
	{
		Transform[] childrenList = GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < childrenList.Length; i++)
		{
			var children = childrenList[i].gameObject;
			if(children == gameObject)
			{
				continue;
			}
			children.SetActive(false);
		}
		humanUI = false;
	}

	private void EnableAlwaysVisible()
	{
		oxygenButton.gameObject.SetActive(true);
		heartMonitor.gameObject.SetActive(true);
		for (int i = 0; i < bodyPartListeners.Count; i++)
		{
			bodyPartListeners[i].gameObject.SetActive(true);
		}
		baseBody.SetActive(true);
		alertsBox.SetActive(true);
		humanUI = true;
	}

	void SetSpecificVisibility(bool value, GameObject UIelement)
	{
		if(UIelement.activeInHierarchy != value)
		{
			UIelement.SetActive(value);
		}
	}

	void Update()
	{
		if (PlayerManager.LocalPlayer == null)
		{
			return;
		}

		if (humanUI && !oxygenButton.gameObject.activeInHierarchy)
		{
			EnableAlwaysVisible();
		}

		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			if(humanUI)
			{
				DisableAll();
			}
			return;
		}


		if(!PlayerManager.LocalPlayerScript.IsGhost && !humanUI)
		{
			EnableAlwaysVisible();
		}

		float temperature = PlayerManager.LocalPlayerScript.OrganicHealthSystem.respiratorySystem.temperature;
		float pressure = PlayerManager.LocalPlayerScript.OrganicHealthSystem.respiratorySystem.pressure;

		if (temperature < 110)
		{
			SetSpecificVisibility(true, coldAlert);
		}
		else
		{
			SetSpecificVisibility(false, coldAlert);
		}

		if (temperature > 510)
		{
			SetSpecificVisibility(true, heatAlert);
		}
		else
		{
			SetSpecificVisibility(false, heatAlert);
		}


		if(temperature > 260 && temperature < 360)
		{
			SetSpecificVisibility(false, temperatureAlert.gameObject);
		}
		else
		{
			SetSpecificVisibility(true, temperatureAlert.gameObject);
			temperatureAlert.SetTemperatureSprite(temperature);
		}

		if (pressure > 50 && pressure < 325)
		{
			SetSpecificVisibility(false, pressureAlert.gameObject);
		}
		else
		{
			SetSpecificVisibility(true, pressureAlert.gameObject);
			pressureAlert.SetPressureSprite(pressure);
		}

		SetSpecificVisibility(PlayerManager.LocalPlayerScript.OrganicHealthSystem.respiratorySystem.IsSuffocating, oxygenAlert);

		SetSpecificVisibility(false, toxinAlert);
		SetSpecificVisibility(PlayerManager.LocalPlayerScript.OrganicHealthSystem.Metabolism.IsHungry, hungerAlert);

		if (PlayerManager.Equipment.HasInternalsEquipped() && !oxygenButton.IsInteractable())
		{
			oxygenButton.interactable = true;
		}

		if (!PlayerManager.Equipment.HasInternalsEquipped() && oxygenButton.IsInteractable())
		{
			EventManager.Broadcast(EVENT.DisableInternals);
			oxygenButton.interactable = false;
		}
	}

	/// <summary>
	/// Update the PlayerHealth body part hud icon
	/// </summary>
	/// <param name="bodyPart"> Body part that requires updating </param>
	public void SetBodyTypeOverlay(BodyPartBehaviour bodyPart)
	{
		for (int i = 0; i < bodyPartListeners.Count; i++)
		{
			if (bodyPartListeners[i].bodyPartType != bodyPart.Type)
			{
				continue;
			}
			Sprite sprite;
			switch (bodyPart.Severity)
			{
				case DamageSeverity.None:
					sprite = bodyPart.BlueDamageMonitorIcon;
					break;
				case DamageSeverity.Light:
					sprite = bodyPart.GreenDamageMonitorIcon;
					break;
				case DamageSeverity.LightModerate:
					sprite = bodyPart.YellowDamageMonitorIcon;
					break;
				case DamageSeverity.Moderate:
					sprite = bodyPart.OrangeDamageMonitorIcon;
					break;
				case DamageSeverity.Bad:
					sprite = bodyPart.DarkOrangeDamageMonitorIcon;
					break;
				case DamageSeverity.Critical:
					sprite = bodyPart.RedDamageMonitorIcon;
					break;
				case DamageSeverity.Max:
				default:
					sprite = bodyPart.GrayDamageMonitorIcon;
					break;
			}
			if (sprite != null && bodyPartListeners[i] != null && bodyPartListeners[i].image != null)
			{
				bodyPartListeners[i].image.sprite = sprite;
			}
		}
	}

	/// <summary>
	/// Overload of previous method so we can keep retro compatibility until we finish refactoring
	/// </summary>
	/// <param name="bodyPart"></param>
	public void SetBodyTypeOverlay(BodyPart bodyPart)
	{
		foreach (var damageListener in bodyPartListeners)
		{
			if (damageListener.bodyPartType != bodyPart.bodyPartData.bodyPartType)
			{
				continue;
			}

			Sprite sprite;
			switch (bodyPart.DamageSeverity)
			{
				case DamageSeverity.None:
					sprite = bodyPart.bodyPartData.blueDamageMonitorIcon;
					break;
				case DamageSeverity.Light:
					sprite = bodyPart.bodyPartData.greenDamageMonitorIcon;
					break;
				case DamageSeverity.LightModerate:
					sprite = bodyPart.bodyPartData.yellowDamageMonitorIcon;
					break;
				case DamageSeverity.Moderate:
					sprite = bodyPart.bodyPartData.orangeDamageMonitorIcon;
					break;
				case DamageSeverity.Bad:
					sprite = bodyPart.bodyPartData.darkOrangeDamageMonitorIcon;
					break;
				case DamageSeverity.Critical:
					sprite = bodyPart.bodyPartData.redDamageMonitorIcon;
					break;
				case DamageSeverity.Max:
				default:
					sprite = bodyPart.bodyPartData.grayDamageMonitorIcon;
					break;
			}

			if (sprite != null && damageListener != null && damageListener.image != null)
			{
				damageListener.image.sprite = sprite;
			}
		}
	}
}