using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandEscape
{
    public class ConverterRecipeUpgrade : LocalUpgrade<Recipe>
    {
        // Changing the recipe stops the old product. Keep it locked while a mission still needs that product,
        // otherwise the mission can never be completed (e.g. Big Jam bought before "Cook some Jam").
        public override bool IsLocked
        {
            get
            {
                if (!IsInitialised || NextStage == null)
                    return false;

                CurrencyType currentResult = CurrentStage.Value.ResultResourceType;
                if (NextStage.Value.ResultResourceType == currentResult)
                    return false;

                return MissionsController.IsCurrencyRequiredByPendingMission(currentResult);
            }
        }

        public override string GetUpgradeDescription(int stageId)
        {
            try
            {
                var value = GetStage(stageId).Value.ResultResourceType;

                return string.Format(DescriptionFormat, value);
            }
            catch
            {
                return "";
            }
        }
    }
}