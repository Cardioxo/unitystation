

//Currently a draft class to help see what is required in HealthSystem

namespace Health
{
    /// <summary>
    /// Provides central access to the cyborg player health
    /// </summary>
    public class SiliconHealthSystem : HealthSystem
    {
    
        protected override void CalculateOverallHealth()
        {
            float newHealth = 100;
            newHealth -= CalculateOverallBodyPartDamage();
            OverallHealth = newHealth;
        }

        protected override void UpdateMe()
        {
        
            //Handle fire damage, cyborgs don't really take damage from fire unless extreme
            //temperatures. Plasmafire for example is not enough to cause burn damage.
            CalculateOverallHealth();
            CheckHealthAndUpdateConsciousState();
        }

        protected override void CheckHealthAndUpdateConsciousState()
        {
            //Pseudo code
            //Check if there's a battery, if not put the cyborg into unconscious state

        
            //Cyborgs have 200 HP, cyborg death after a total of 200 damage from brute and burn

            Death();
        }

        public override void Death()
        {
        

            RaiseDeathNotifyEvent();
            ConsciousState = ConsciousState.DEAD;
        }
    
        protected override void OnDeathActions()
        {
            throw new System.NotImplementedException();
        }
    }
}
