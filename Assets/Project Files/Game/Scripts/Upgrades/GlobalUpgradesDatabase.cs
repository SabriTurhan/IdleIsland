using UnityEngine;

namespace Watermelon.GlobalUpgrades
{
    [CreateAssetMenu(fileName = "Upgrades Database", menuName = "Data/Upgrades/Upgrades Database")]
    public class GlobalUpgradesDatabase : ScriptableObject
    {
        [SerializeField] AbstactGlobalUpgrade[] upgrades;
        public AbstactGlobalUpgrade[] Upgrades => upgrades;
    }
}