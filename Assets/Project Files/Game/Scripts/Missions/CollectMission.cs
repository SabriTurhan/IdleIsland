using UnityEngine;

namespace Watermelon
{
    public sealed class CollectMission : Mission
    {
        [BoxGroup("Collect Mission Special", "Collect Mission Special")]
        [SerializeField] CurrencyType currencyType;
        public CurrencyType CurrencyType => currencyType;

        [BoxGroup("Collect Mission Special")]
        [SerializeField] int amount;

        private Save save;

        public override void Initialise()
        {
            base.Initialise();

            save = SaveController.GetSaveObject<Save>(GetSaveString());
            save.LinkMission(this);

            // Load mission stage
            missionStage = save.MissionStage;
        }

        public override void Activate()
        {
            base.Activate();

            isDirty = true;

            StartMission();

            CheckStage();

            CurrencyController.SubscribeGlobalCallback(OnCurrencyPicked);

            PlayerInventory inventory = GetPlayerInventory();
            if (inventory != null)
                inventory.CapacityChanged += OnInventoryCapacityChanged;
        }

        public override void Deactivate()
        {
            base.Deactivate();

            CurrencyController.UnsubscribeGlobalCallback(OnCurrencyPicked);

            PlayerInventory inventory = GetPlayerInventory();
            if (inventory != null)
                inventory.CapacityChanged -= OnInventoryCapacityChanged;
        }

        private void OnInventoryCapacityChanged()
        {
            // progress text shows a "bag is full" hint
            isDirty = true;
        }

        private static PlayerInventory GetPlayerInventory()
        {
            PlayerBehavior player = PlayerBehavior.GetBehavior();

            return player != null ? player.Inventory : null;
        }

        // the required resource goes to the bag and the bag is full - it can't be picked up until space is freed
        private bool IsBlockedByFullInventory()
        {
            Currency currency = CurrencyController.GetCurrency(currencyType);
            if (currency == null || !currency.Data.UseInventory)
                return false;

            PlayerInventory inventory = GetPlayerInventory();

            return inventory != null && inventory.IsFull();
        }

        private void OnCurrencyPicked(Currency currency, int amountDifference)
        {
            if (currencyType == currency.CurrencyType)
            {
                if (amountDifference > 0)
                {
                    isDirty = true;

                    save.CollectedItems += amountDifference;

                    CheckStage();
                }
            }
        }

        private void CheckStage()
        {
            if (missionStage == Stage.Active)
            {
                if (save.CollectedItems >= amount)
                {
                    FinishMission();
                }
            }
        }

        public override string GetFormattedProgress()
        {
            string progress = string.Format("{0}/{1} <sprite name={2}>", save.CollectedItems, amount, currencyType);

            if (missionStage == Stage.Active && IsBlockedByFullInventory())
                progress += " <color=#FF5A5A>BAG FULL!</color>";

            return progress;
        }

        public override float GetProgress()
        {
            switch (missionStage)
            {
                case Stage.Uninitialised:
                    return 0.0f;
                case Stage.Active:
                    return Mathf.InverseLerp(0, amount, save.CollectedItems);
                case Stage.Finished:
                    return 1.0f;
                case Stage.Collected:
                    return 1.0f;
            }

            return 0.0f;
        }

        [System.Serializable]
        public class Save : MissionSave
        {
            public int CollectedItems;
        }
    }
}