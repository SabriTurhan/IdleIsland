using System.Collections.Generic;
using UnityEngine;

namespace IslandEscape
{
    // Boss of World 2: a giant ghost skeleton. Melee like a skeleton, plus a telegraphed ground slam
    // and pirate skeleton summons at 66% and 33% health.
    public class GhostCaptainBehavior : SkeletonEnemyBehavior
    {
        [Header("Ghost Captain")]
        [SerializeField] string bossTitle = "GHOST CAPTAIN GRIMBEARD";
        public string BossTitle => bossTitle;

        [SerializeField] float slamInterval = 7f;
        [SerializeField] float slamTelegraph = 1.3f;
        [SerializeField] float slamRadius = 3.4f;
        [SerializeField] int slamDamage = 20;
        [SerializeField] Material slamMaterial;

        [SerializeField] float[] summonThresholds = { 0.66f, 0.33f };
        [SerializeField] EnemyType minionType = EnemyType.PirateSkeleton;
        [SerializeField] int minionsPerSummon = 2;

        private float nextSlamTime;
        private bool isSlamming;
        private int summonsDone;
        private GameObject slamRing;
        private readonly List<Transform> minionSpawnPoints = new List<Transform>();
        private readonly List<BaseEnemyBehavior> minions = new List<BaseEnemyBehavior>();

        public bool IsEngaged { get; private set; }

        public event SimpleCallback OnBossDefeated;

        // the fight restarts when the player dies or leaves the crypt
        public void Despawn()
        {
            if (slamRing != null) slamRing.SetActive(false);

            foreach (BaseEnemyBehavior minion in minions)
            {
                if (minion != null && minion.gameObject.activeSelf) minion.Unload();
            }
            minions.Clear();

            Unload();
        }

        public void ResetBoss()
        {
            summonsDone = 0;
            isSlamming = false;
            nextSlamTime = Time.time + slamInterval;
            IsEngaged = false;
            if (slamRing != null) slamRing.SetActive(false);
        }

        private void LateUpdate()
        {
            if (IsDead || PlayerBehavior.GetBehavior() == null)
                return;

            float distance = Vector3.Distance(transform.position, PlayerBehavior.Position);
            if (!IsEngaged && distance < DetectionDistance)
            {
                IsEngaged = true;
                nextSlamTime = Time.time + slamInterval * 0.6f;
            }

            if (!IsEngaged || PlayerBehavior.GetBehavior().IsDead)
                return;

            if (!isSlamming && Time.time >= nextSlamTime && distance < slamRadius * 2.5f)
                StartSlam();

            float healthPart = MaxHealth > 0 ? CurrentHealth / MaxHealth : 1f;
            if (summonsDone < summonThresholds.Length && healthPart <= summonThresholds[summonsDone])
            {
                summonsDone++;
                Summon();
            }
        }

        private void StartSlam()
        {
            isSlamming = true;

            if (slamRing == null) slamRing = CreateRing();

            Vector3 center = transform.position;
            slamRing.transform.position = center + Vector3.up * 0.06f;
            slamRing.transform.localScale = Vector3.one * 0.2f;
            slamRing.SetActive(true);

            Attack();

            slamRing.transform.DOScale(Vector3.one * slamRadius, slamTelegraph).SetEasing(Ease.Type.QuadOut).OnComplete(() =>
            {
                if (!IsDead && PlayerBehavior.GetBehavior() != null && Vector3.Distance(center, PlayerBehavior.Position) <= slamRadius)
                {
                    PlayerBehavior.GetBehavior().TakeDamage(new DamageSource(slamDamage, this), center, true);
                }

                if (CameraController.ActiveVirtualCamera != null)
                    CameraController.ActiveVirtualCamera.Shake(0.05f, 0.4f, 0.4f, 1.6f);

                Tween.DelayedCall(0.25f, () =>
                {
                    if (slamRing != null) slamRing.SetActive(false);
                    isSlamming = false;
                    nextSlamTime = Time.time + slamInterval;
                });
            });
        }

        private void Summon()
        {
            if (minionSpawnPoints.Count == 0)
            {
                for (int i = 0; i < minionsPerSummon; i++)
                {
                    Transform point = new GameObject("Minion Spawn " + i).transform;
                    point.SetParent(SpawnPoint != null ? SpawnPoint.parent : transform.parent, true);
                    minionSpawnPoints.Add(point);
                }
            }

            for (int i = 0; i < minionsPerSummon; i++)
            {
                float angle = (i + 0.5f) / minionsPerSummon * Mathf.PI * 2f + Random.Range(-0.4f, 0.4f);
                Vector3 position = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2.2f;
                if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    position = hit.position;

                minionSpawnPoints[i].position = position;
                minionSpawnPoints[i].rotation = Quaternion.LookRotation((PlayerBehavior.Position - position).SetY(0).normalized + Vector3.forward * 0.001f);

                BaseEnemyBehavior minion = GameController.Data.EnemiesDatabase.GetEnemyBehavior(minionType);
                if (minion == null) continue;

                minion.Spawn(minionSpawnPoints[i]);
                minions.Add(minion);
            }

            if (CameraController.ActiveVirtualCamera != null)
                CameraController.ActiveVirtualCamera.Shake(0.05f, 0.3f, 0.3f, 0.8f);
        }

        protected override void OnKilled()
        {
            if (slamRing != null) slamRing.SetActive(false);

            // the curse ends with the captain, his crew crumbles too
            foreach (BaseEnemyBehavior minion in minions)
            {
                if (minion != null && minion.gameObject.activeSelf && !minion.IsDead)
                    minion.TakeDamage(new DamageSource(9999, this), minion.transform.position);
            }
            minions.Clear();

            OnBossDefeated?.Invoke();
        }

        public override void OnHit()
        {
            base.OnHit();

            if (CameraController.ActiveVirtualCamera != null && Vector3.Distance(transform.position, PlayerBehavior.Position) < AttackRange)
                CameraController.ActiveVirtualCamera.Shake(0.03f, 0.2f, 0.2f, 0.6f);
        }

        // flat ring + disc on the ground showing where the slam lands
        private GameObject CreateRing()
        {
            GameObject ring = new GameObject("Slam Telegraph");
            ring.transform.SetParent(transform.parent, true);

            Mesh mesh = new Mesh { name = "Slam Ring" };
            const int SEGMENTS = 40;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int i = 0; i < SEGMENTS; i++)
            {
                float a0 = i / (float)SEGMENTS * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)SEGMENTS * Mathf.PI * 2f;
                Vector3 outer0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
                Vector3 outer1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                int start = vertices.Count;
                vertices.Add(outer0 * 0.86f);
                vertices.Add(outer0);
                vertices.Add(outer1);
                vertices.Add(outer1 * 0.86f);
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            Vector2[] uvs = new Vector2[vertices.Count];
            for (int i = 0; i < uvs.Length; i++) uvs[i] = new Vector2(0.05f, 1f - (10 * 32 + 16) / 1024f); // Curse band
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            ring.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = slamMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return ring;
        }
    }
}
