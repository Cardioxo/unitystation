using System.Collections.Generic;
using UnityEngine;

namespace Health
{
    /// <summary>
    /// This component allows for a living object to receive surgery
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class Operable : MonoBehaviour, ICheckedInteractable<HandApply>
    {
        protected static readonly StandardProgressActionConfig ProgressConfig
            = new StandardProgressActionConfig(StandardProgressActionType.Restrain);
    
        private RegisterTile registerTile = null;
        //The initial surgery initiation page
        public NetTabType NetTabType = NetTabType.Surgery;
        private HealthSystem healthSystem = null;
    
        //Contains all the available surgery options on each body part. 
        [SerializeField]
        private SurgeryOptions surgeryOptions = null;
    
        /// <summary>
        /// Dictionary with the active surgery operations on a bodypart.
        /// </summary>
        private Dictionary<BodyPartType, SurgeryOperation> activeSurgeries = new Dictionary<BodyPartType, SurgeryOperation>();

        public Dictionary<BodyPartType, SurgeryOperation> ActiveSurgeries
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
            if (activeSurgeries.Count > 0 && activeSurgeries.ContainsKey(interaction.TargetBodyPart))
            {
                //Checks if it's the right tool used at the right step.
                return activeSurgeries[interaction.TargetBodyPart].ValidateInteraction(interaction);
            }
        
            //Performer has drape in hand
            if (interaction.HandObject.GetComponent<SurgeryTool>().SurgeryToolProperties[0].ToolType !=
                SurgeryToolType.Drapes)
            {
                return false;
            }
            return true;
        }

        public void ServerPerformInteraction(HandApply interaction)
        {
            //If drape in hand, show surgery UI
            if (interaction.HandObject.GetComponent<SurgeryTool>().SurgeryToolProperties[0].ToolType !=
                SurgeryToolType.Drapes)
            {
                TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType, TabAction.Open );
            }
            
            
            else
            {
                void ProgressFinishAction()
                {
                    if (RollSuccess())
                    {
                        activeSurgeries[interaction.TargetBodyPart].PerformInteractionSuccess(interaction); 
                    }
                    else
                    {
                        activeSurgeries[interaction.TargetBodyPart].PerformInteractionFailure(interaction); 
                    }
                }

                var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
                    .ServerStartProgress(registerTile, 10, interaction.Performer);
            }

        }

        /// <summary>
        /// Rolls the success of the surgery step depending on the circumstances. How efficient is the tool used?
        /// Is the surgery done on the floor or on a table? Etc.
        /// </summary>
        /// <returns></returns>
        private bool RollSuccess()
        {
            //TODO implement the roll
            return true;
        }
    }
}


//UI creates a new instance of a surgery type being performed on a certain limb