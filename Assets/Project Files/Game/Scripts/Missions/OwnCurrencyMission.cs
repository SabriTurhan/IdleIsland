using UnityEngine;

namespace IslandEscape
{
    // Have at least N of a currency at the same time (e.g. all 4 map pieces), items owned before the start count too
    public sealed class OwnCurrencyMission : Mission
    {
        [BoxGroup("Own Mission Special", "Own Mission Special")]
        [SerializeField] CurrencyType currencyType = CurrencyType.MapPiece;
        public CurrencyType CurrencyType => currencyType;

        [BoxGroup("Own Mission Special")]
        [SerializeField] int amount = 4;
        public int Amount => amount;

        private Save save;

        public override void Initialise()
        {
            base.Initialise();

            save = SaveController.GetSaveObject<Save>(GetSaveString());
            save.LinkMission(this);

            missionStage = save.MissionStage;
        }

        public override void Activate()
        {
            base.Activate();

            isDirty = true;

            StartMission();
            CheckStage();

            CurrencyController.SubscribeGlobalCallback(OnCurrencyChanged);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            CurrencyController.UnsubscribeGlobalCallback(OnCurrencyChanged);
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            if (currency.CurrencyType != currencyType)
                return;

            isDirty = true;
            CheckStage();
        }

        private int Owned => CurrencyController.GetCurrency(currencyType).Amount;

        private void CheckStage()
        {
            if (missionStage == Stage.Active && Owned >= amount)
                FinishMission();
        }

        public override string GetFormattedProgress()
        {
            return string.Format("{0}/{1} <sprite name={2}>", Mathf.Min(Owned, amount), amount, currencyType);
        }

        public override float GetProgress()
        {
            switch (missionStage)
            {
                case Stage.Active:
                    return Mathf.InverseLerp(0, amount, Owned);
                case Stage.Finished:
                case Stage.Collected:
                    return 1.0f;
            }

            return 0.0f;
        }

        [System.Serializable]
        public class Save : MissionSave
        {
        }
    }
}
