using UnityEngine;

namespace Rebound
{
    public sealed class ReboundAttack : MonoBehaviour
    {
        public GameManager game;
        public FallEnergySystem energy;
        public SpriteRenderer wave;
        float flashUntil;
        public bool TryFire()
        {
            if (game.IsGameOver || !energy.TryConsume()) return false;
            Vector2 size = game.settings.reboundSize;
            Vector2 center = (Vector2)transform.position + Vector2.right * (size.x * 0.5f + 0.3f);
            // Snapshot: damage can remove enemies from the registry during iteration.
            EnemyController[] enemies = EnemyController.Active.ToArray();
            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null || enemy.Game != game) continue;
                Bounds bounds = enemy.GetComponent<Collider2D>().bounds;
                if (bounds.max.x >= center.x - size.x / 2f && bounds.min.x <= center.x + size.x / 2f &&
                    bounds.max.y >= center.y - size.y / 2f && bounds.min.y <= center.y + size.y / 2f)
                    enemy.TakeDamage(game.settings.reboundDamage);
            }
            wave.transform.position = center;
            wave.transform.localScale = size;
            flashUntil = Time.time + 0.2f;
            wave.enabled = true;
            return true;
        }
        void Update()
        {
            wave.enabled = Time.time < flashUntil;
            wave.color = new Color(0.4f, 0.85f, 1f, Mathf.Clamp01((flashUntil - Time.time) / 0.2f) * 0.35f);
        }
    }
}
