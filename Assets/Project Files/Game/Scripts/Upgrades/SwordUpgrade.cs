using UnityEngine;
using IslandEscape.GlobalUpgrades;

namespace IslandEscape
{
    // Sword level bought at the World 2 blacksmith: damage of every player hit on enemies
    [CreateAssetMenu(fileName = "Sword Upgrade", menuName = "Data/Upgrades/Sword Upgrade")]
    public class SwordUpgrade : GlobalUpgrade<SwordUpgrade.SwordStage>
    {
        public override void Initialise()
        {

        }

        public override string GetUpgradeDescription(int stageId)
        {
            try
            {
                var prevValue = GetStage(stageId).Damage;
                var value = GetStage(stageId + 1).Damage;

                return string.Format(DescriptionFormat, prevValue, value);
            }
            catch
            {
                return "";
            }
        }

        // damage of one player hit (1 before any sword upgrade, as in the template)
        public static int CurrentDamage
        {
            get
            {
                if (GlobalUpgradesController.ActiveUpgrades == null)
                    return 1;

                foreach (AbstactGlobalUpgrade upgrade in GlobalUpgradesController.ActiveUpgrades)
                {
                    if (upgrade is SwordUpgrade sword)
                        return Mathf.Max(1, sword.GetCurrentStage().Damage);
                }

                return 1;
            }
        }

        [System.Serializable]
        public class SwordStage : GlobalUpgradeStage
        {
            [SerializeField] int damage = 1;
            public int Damage => damage;
        }
    }
}
