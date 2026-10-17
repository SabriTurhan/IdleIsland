using UnityEngine;

namespace Watermelon
{
    // Defeat N enemies (of one type, or any type) after the mission starts
    public sealed class KillEnemiesMission : Mission
    {
        [BoxGroup("Kill Mission Special", "Kill Mission Special")]
        [SerializeField] bool anyEnemyType = true;

        [BoxGroup("Kill Mission Special")]
        [SerializeField, HideIf("anyEnemyType")] EnemyType enemyType = EnemyType.PirateSkeleton;
        public EnemyType EnemyType => enemyType;
        public bool AnyEnemyType => anyEnemyType;

        [BoxGroup("Kill Mission Special")]
        [SerializeField] int amount = 3;
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

            BaseEnemyBehavior.OnEnemyKilled += OnEnemyKilled;
        }

        public override void Deactivate()
        {
            base.Deactivate();

            BaseEnemyBehavior.OnEnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemyKilled(BaseEnemyBehavior enemy)
        {
            if (!anyEnemyType && enemy.EnemyType != enemyType)
                return;

            save.Killed += 1;
            isDirty = true;

            CheckStage();
        }

        private void CheckStage()
        {
            if (missionStage == Stage.Active && save.Killed >= amount)
                FinishMission();
        }

        public override string GetFormattedProgress()
        {
            return string.Format("{0}/{1} <sprite name=Skull>", Mathf.Min(save.Killed, amount), amount);
        }

        public override float GetProgress()
        {
            switch (missionStage)
            {
                case Stage.Active:
                    return Mathf.InverseLerp(0, amount, save.Killed);
                case Stage.Finished:
                case Stage.Collected:
                    return 1.0f;
            }

            return 0.0f;
        }

        [System.Serializable]
        public class Save : MissionSave
        {
            public int Killed;
        }
    }
}
