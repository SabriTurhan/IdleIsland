using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Watermelon.GlobalUpgrades;

namespace Watermelon
{
    // Usint it as a marker for now
    public abstract class AbstractLocalUpgrade: MonoBehaviour, IUpgrade
    {
        [SerializeField] protected string title;
        public string Title => title;

        [SerializeField] protected Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] string descriptionFormat;
        public string DescriptionFormat => descriptionFormat;

        public event SimpleCallback OnUpgraded;

        private SimpleIntSave save;

        public int UpgradeIndex
        {
            get => save.Value;
            protected set
            {
                save.Value = value;
                OnUpgraded?.Invoke();
            }
        }

        public int UpgradeLevel => UpgradeIndex + 1;

        // save is linked when the owning building initialises
        public bool IsInitialised => save != null;

        public abstract int UpgradesCount { get; }
        public bool IsMaxedOut => UpgradeIndex + 1 >= UpgradesCount;

        // locked upgrades are hidden from the upgrades page and can't be bought
        public virtual bool IsLocked => false;

        public UpgradeType Type => UpgradeType.Local;

        public abstract Resource GetUpgradeCost(int level);
        public abstract void UpgradeStage();
        public abstract string GetUpgradeDescription(int stageId);

        public bool IsHighlighted { get; set; }

        public void Init(string saveName)
        {
            save = SaveController.GetSaveObject<SimpleIntSave>(saveName);
        }
    }
}