using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class SkeletonEnemyBehavior : BaseEnemyBehavior
    {
        [SerializeField] int attackDamage = 10;
        public int AttackDamage => attackDamage;

        // the skeleton notices the player at this distance and gives up the chase at the leash distance
        [SerializeField] float detectionDistance = 5f;
        public float DetectionDistance => detectionDistance;

        [SerializeField] float leashDistance = 10f;
        public float LeashDistance => leashDistance;

        protected override void Awake()
        {
            base.Awake();

            animationCallbacks.Add(EnemyAnimationEventType.SpawnEnded, OnSpawnAnimationEnded);
            animationCallbacks.Add(EnemyAnimationEventType.Hit, OnHit);
            animationCallbacks.Add(EnemyAnimationEventType.HitEnded, HitEnded);
            animationCallbacks.Add(EnemyAnimationEventType.PlaySpawnParticle, PlaySpawnParticle);
            animationCallbacks.Add(EnemyAnimationEventType.PlayDeathParticle, PlayDeathAnimation);
            animationCallbacks.Add(EnemyAnimationEventType.DropResourcesOnDeath, SpawnDrop);
        }

        public virtual void OnHit()
        {
            if (Vector3.Distance(transform.position, PlayerBehavior.Position) < AttackRange)
            {
                PlayerBehavior.GetBehavior().TakeDamage(new DamageSource(attackDamage, this), transform.position, true);
            }
        }
    }
}
