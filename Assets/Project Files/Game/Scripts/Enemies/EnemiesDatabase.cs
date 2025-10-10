using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Enemies Database", menuName = "Data/Enemies Database")]
    public class EnemiesDatabase : ScriptableObject
    {
        [SerializeField] EnemyData[] enemies;
        public EnemyData[] Enemies => enemies;

        private Dictionary<EnemyType, PoolGeneric<BaseEnemyBehavior>> enemyPools;

        public void Init()
        {
            enemyPools = new Dictionary<EnemyType, PoolGeneric<BaseEnemyBehavior>>();

            for(int i = 0; i < enemies.Length; i++)
            {
                var type = enemies[i].EnemyType;
                var prefab = enemies[i].Prefab;

                var pool = new PoolGeneric<BaseEnemyBehavior>(prefab, $"Enemy_{enemies[i].EnemyType}");

                enemyPools.Add(type, pool);
            }
        }

        public BaseEnemyBehavior GetEnemyBehavior(EnemyType type)
        {
            if (enemyPools.ContainsKey(type))
            {
                return enemyPools[type].GetPooledComponent();
            }

            return null;
        }

        public EnemyData GetEnemyData(EnemyType type)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].EnemyType.Equals(type))
                    return enemies[i];
            }

            Debug.LogError("[Enemies Database] Enemy of type " + type + " + is not found!");
            return enemies[0];
        }

        public void Unload()
        {
            if(enemyPools != null)
            {
                foreach(PoolGeneric<BaseEnemyBehavior> pool in enemyPools.Values)
                {
                    PoolManager.DestroyPool(pool);
                }

                enemyPools.Clear();
            }
        }
    }
}
