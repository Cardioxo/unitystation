using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
/// <summary>
/// Allows an object to be harvested by a player.
/// </summary>
public class Harvestable : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{
    [SerializeField] private static readonly StandardProgressActionConfig ProgressConfig
        = new StandardProgressActionConfig(StandardProgressActionType.Restrain);
    
    [SerializeField]
    private float butcherTime = 2.0f;

    [SerializeField] 
    private string initialButcherSound = "Desceration1";
    [SerializeField]
    private string butcherSound = "BladeSlice";
    
    public GameObject[] butcherResults;
    private HealthSystem healthSystemCache;

    void OnEnable()
    {
        healthSystemCache = GetComponent<HealthSystem>();
    }
    
    
    [Server]
    public void Harvest()
    {
        //If there's a healthsystem, it's a player
        if (healthSystemCache)
        {
            foreach (GameObject harvestPrefab in butcherResults)
            {
                Spawn.ServerPrefab(harvestPrefab, transform.position, parent: transform.parent);
            }
            
            healthSystemCache.Gib();
        }

        //If not, it's an object
        else
        {
            foreach (GameObject harvestPrefab in butcherResults)
            {
                Spawn.ServerPrefab(harvestPrefab, transform.position, parent: transform.parent);
            }
        }

        
    }

    public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
    {
        //Checks if within range
        if (!DefaultWillInteract.Default(interaction, side)) return false;
        //Checks if this gameobject is being targeted
        if (interaction.TargetObject != gameObject) return false;
        //Checks if the performer has a knife
        if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Knife)) return false;
        //Checks if we're targetting something alive.
        if (healthSystemCache && !healthSystemCache.IsDead) return false;
        
        return true;
    }
    public void ServerPerformInteraction(PositionalHandApply interaction)
    {
        var handObject = interaction.HandObject;
        
        RegisterTile targetLocation = GetComponent<RegisterTile>();
        //Checks if the harvested object is something alive with the right conditions to harvest
        if (healthSystemCache && healthSystemCache.IsDead && Validations.HasItemTrait(handObject, CommonTraits.Instance.Knife) 
            && interaction.Intent == Intent.Harm)
        {
            PerformButchering(interaction, targetLocation);
        }
        
        //The harvested object is an item
        else if (healthSystemCache == null && Validations.HasItemTrait(handObject, CommonTraits.Instance.Knife))
        {
            PerformButchering(interaction, targetLocation);
        }
    }

    private void PerformButchering(PositionalHandApply interaction, RegisterTile targetLocation)
    {
        GameObject performer = interaction.Performer;

        //Only play the initial butcher sound if the butcher time is longer than 0.5 seconds
        if (butcherTime > 0.5f)
        {
            SoundManager.PlayNetworkedAtPos(initialButcherSound, targetLocation.WorldPositionServer);
        }

        void ProgressFinishAction()
        {
            Harvest();
            SoundManager.PlayNetworkedAtPos(butcherSound, targetLocation.WorldPositionServer);
        }

        var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
            .ServerStartProgress(targetLocation, butcherTime, performer);
    }
}
