using IslandEscape.GlobalUpgrades;

namespace IslandEscape
{
    // Forge of World 2: smelts iron like any converter, and its upgrade button also sells sword levels
    public class BlacksmithForgeBehavior : ResourceConverterBuildingBehavior
    {
        private bool swordRegistered;

        protected override void Init()
        {
            base.Init();

            if (swordRegistered || upgradesTrigger == null)
                return;

            SwordUpgrade sword = GlobalUpgradesController.GetUpgrade<SwordUpgrade>(GlobalUpgradeType.Sword);
            if (sword != null)
            {
                upgradesTrigger.RegisterUpgrade(sword);
                swordRegistered = true;
            }
        }
    }
}
