using UnityEngine;

namespace Watermelon
{
    public sealed class BuildingMission : Mission, IUnlockingMission
    {
        public override MissionUICase.Type MissionUIType => MissionUICase.Type.Build;

        [BoxGroup("Building Mission Special", "Building Mission Special")]
        [SerializeField] BuildingComplexBehavior buildingComplex;
        public BuildingComplexBehavior BuildingComplex => buildingComplex;

        // while the building is hidden under a locked tile the mission asks to open that tile first
        public IUnlockableComplex LinkedBuilding => IsBlockedByTile ? blockingTile : buildingComplex;

        public override string DisplayTitle => IsBlockedByTile ? "Clear the land" : base.DisplayTitle;

        private GroundTileComplexBehavior blockingTile;
        private bool IsBlockedByTile => blockingTile != null && !blockingTile.IsOpen;

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

            // the building is placed on a locked tile that no mission opens - guide the player to that tile first
            blockingTile = FindBlockingTile();
            if (IsBlockedByTile)
            {
                StartMission();

                blockingTile.SubscribeOnResourcePlaced(OnResourcePlaced);
            }

            // the target may not be initialised yet (e.g. it was just enabled by the previous mission),
            // until then "can't be purchased or constructed" would wrongly look like "already built"
            buildingComplex.InvokeOrSubscribe(OnTargetInitialised);

            buildingComplex.SubscribeOnFullyUnlocked(OnUnlocked);
            buildingComplex.SubscribeOnResourcePlaced(OnResourcePlaced);
        }

        private void OnTargetInitialised()
        {
            if (MissionsController.ActiveMission != this || missionStage == Stage.Finished || missionStage == Stage.Collected)
                return;

            isDirty = true;

            bool wasBlocked = blockingTile != null;
            if (wasBlocked)
            {
                blockingTile.UnsubscribeOnResourcePlaced(OnResourcePlaced);
                blockingTile = null;
            }

            if (buildingComplex.CanBePurchased || buildingComplex.CanBeConstructed)
            {
                if (missionStage != Stage.Active)
                {
                    StartMission();
                }
                else if (wasBlocked)
                {
                    // switch the missions panel from the tile to the building
                    MissionsController.RefreshActiveMissionUI();
                }
            }
            else
            {
                FinishMission();
            }
        }

        private GroundTileComplexBehavior FindBlockingTile()
        {
            GroundTileBehavior tile = GroundTileBehavior.GetTile(buildingComplex);
            if (tile == null || tile.IsOpen)
                return null;

            return tile.GetComponentInParent<GroundTileComplexBehavior>();
        }

        private void OnUnlocked()
        {
            if (!buildingComplex.CanBePurchased && !buildingComplex.CanBeConstructed)
            {
                FinishMission();
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();

            buildingComplex.UnsubscribeOnFullyUnlocked(OnUnlocked);
            buildingComplex.UnsubscribeOnResourcePlaced(OnResourcePlaced);

            if (blockingTile != null)
                blockingTile.UnsubscribeOnResourcePlaced(OnResourcePlaced);
        }

        private void OnResourcePlaced()
        {
            isDirty = true;
        }

        public override string GetFormattedProgress()
        {
            return "";
        }

        public override float GetProgress()
        {
            return IsBlockedByTile || buildingComplex.CanBePurchased ? 0.0f : 1.0f;
        }

        public override Vector3 GetDefaultPreviewPosition()
        {
            return IsBlockedByTile ? blockingTile.Position : buildingComplex.transform.position;
        }

        [System.Serializable]
        public class Save : MissionSave
        {

        }
    }
}