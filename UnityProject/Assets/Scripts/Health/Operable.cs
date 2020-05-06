using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Health;
using UnityEngine.XR.WSA.Input;

/// <summary>
/// This component allows for a living object to receive surgery
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class Operable : MonoBehaviour, ICheckedInteractable<HandApply>
{
    private RegisterTile registerTile = null;
    //The initial surgery initiation page
    public NetTabType NetTabType = NetTabType.Surgery;
    private HealthSystem healthSystem = null;
    
    //Contains all the available surgery options on each body part. 
    [SerializeField]
    private SurgeryOptions surgeryOptions = null;
    
    private Dictionary<BodyPartType, SurgeryProcedure> activeSurgeries = new Dictionary<BodyPartType, SurgeryProcedure>();

    public Dictionary<BodyPartType, SurgeryProcedure> ActiveSurgeries
    {
        get => activeSurgeries;
    }
    
    
    void OnEnable()
    {
        EnsureInit();
    }

    private void EnsureInit()
    {
        healthSystem = GetComponent<HealthSystem>();
        registerTile = GetComponent<RegisterTile>();
    }

    public bool WillInteract(HandApply interaction, NetworkSide side)
    {
        //Within range
        if (!DefaultWillInteract.Default(interaction, side)) return false;
        //Performer is self
        if (interaction.Performer == interaction.TargetObject) return false;
        //Target is lying down
        if (!interaction.TargetObject.GetComponent<RegisterPlayer>().IsLayingDown) return false;

        //Checks if there is an active surgery going on at a bodypart
        if (activeSurgeries.ContainsKey(interaction.TargetBodyPart))
        {
            //Checks if it's the right tool used at the right step.
            if (activeSurgeries[interaction.TargetBodyPart].ValidateInteraction(interaction)) return true;
        }
        
        //Performer has drape in hand
        // if (!Validations.HasItemTrait(interaction.HandObject, )) 
        return true;
    }

    public void ServerPerformInteraction(HandApply interaction)
    {
        //TODO: Finish logic here
        //If drape in hand, show surgery UI
        // if(
        // TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType, TabAction.Open );
        
        Logger.Log("Starting surgery UI");
        //else 
        activeSurgeries[interaction.TargetBodyPart].PerformInteraction(interaction, registerTile);
        

    }
}

//UI creates a new instance of a surgery type being performed on a certain limb