using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IslandEscape.GlobalUpgrades;

namespace IslandEscape
{
    public abstract class UpgradesTrigger : MonoBehaviour
    {
        protected List<IUpgrade> upgrades;

        // if there where registered upgrades - then it's trigger for local upgrades
        // otherwise - open global upgrades page
        protected bool IsLocalTrigger => !upgrades.IsNullOrEmpty();

        // local trigger with every upgrade locked by missions has nothing to offer
        protected bool HasAvailableUpgrades => !IsLocalTrigger || upgrades.Exists(upgrade => !(upgrade is AbstractLocalUpgrade localUpgrade && localUpgrade.IsLocked));

        protected virtual void Awake()
        {
            upgrades = new List<IUpgrade>();
        }

        public virtual void RegisterUpgrade(IUpgrade upgrade)
        {
            upgrades.Add(upgrade);
        }

        public virtual void RegisterUpgrades(List<IUpgrade> newUpgrade)
        {
            upgrades.AddRange(newUpgrade);
        }

        public abstract void ShowUpgradesPanel();
    }
}