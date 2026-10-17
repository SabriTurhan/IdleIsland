using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandEscape
{
    [System.Serializable]
    public class EnemyData
    {
        [SerializeField] EnemyType enemyType;
        public EnemyType EnemyType => enemyType;

        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;
    }
}
