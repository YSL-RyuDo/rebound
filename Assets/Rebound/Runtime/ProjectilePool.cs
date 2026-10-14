using System.Collections.Generic;
using UnityEngine;

namespace Rebound
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        public Projectile prefab;
        public GameManager game;
        readonly Queue<Projectile> available = new Queue<Projectile>();
        public int CreatedCount { get; private set; }
        public int AvailableCount => available.Count;
        void Awake()
        {
            for (int i = 0; i < game.settings.poolCapacity; i++)
            {
                Projectile projectile = Instantiate(prefab, transform);
                projectile.gameObject.SetActive(false);
                available.Enqueue(projectile);
                CreatedCount++;
            }
        }
        public Projectile Fire(Vector2 position)
        {
            if (available.Count == 0 || game.IsGameOver) return null;
            Projectile projectile = available.Dequeue();
            projectile.Launch(this, game, position);
            return projectile;
        }
        public void Return(Projectile projectile) { available.Enqueue(projectile); }
    }
}
