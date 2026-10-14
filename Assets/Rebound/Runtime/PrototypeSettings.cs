using UnityEngine;

namespace Rebound
{
    // Serializable data stays separate from simulation; future MOD JSON can populate these fields.
    [CreateAssetMenu(menuName = "REBOUND/Prototype Settings")]
    public sealed class PrototypeSettings : ScriptableObject
    {
        [Header("Movement (world units, seconds)")]
        [Min(0.01f)] public float gravity = 6f;
        [Min(0.01f)] public float riseSpeed = 4.8f;
        [Min(0f)] public float moveSpeed = 5f;
        [Min(0.01f)] public float maxFallSpeed = 6f;
        [Min(0.01f)] public float riseInterval = 0.25f;
        [Range(0.1f, 1f)] public float movementWidth = 0.4f;
        [Header("Health")]
        [Min(1)] public int maxHp = 3;
        [Min(0f)] public float invulnerability = 1.2f;
        [Header("Weapon")]
        [Min(0.02f)] public float shotInterval = 0.22f;
        [Min(0.1f)] public float projectileSpeed = 15f;
        [Min(0.1f)] public float projectileLifetime = 2f;
        [Min(1)] public int projectileDamage = 1;
        [Min(1)] public int poolCapacity = 32;
        [Header("Enemies")]
        [Min(0.1f)] public float spawnInterval = 1.1f;
        [Min(0.1f)] public float enemySpeed = 2.2f;
        [Min(1)] public int enemyHp = 3;
        [Header("Fall energy / rebound")]
        [Min(0.1f)] public float energyPerUnit = 22f;
        [Range(1f, 100f)] public float reboundThreshold = 65f;
        [Min(1)] public int reboundDamage = 5;
        public Vector2 reboundSize = new Vector2(6.5f, 3.2f);
    }
}
