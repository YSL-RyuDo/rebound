using UnityEngine;

namespace Rebound
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        public GameManager game;
        public EnemyController prefab;
        float nextSpawn;
        void Start() { nextSpawn = Time.time + 0.7f; }
        void FixedUpdate()
        {
            if (game.IsGameOver || Time.fixedTime < nextSpawn) return;
            // Mostly near the player's lane, with variation to reward altitude control.
            float y = Mathf.Clamp(game.player.transform.position.y + Random.Range(-1.2f, 1.2f), -4f, game.TopY);
            SpawnAt(new Vector2(GameManager.HalfWidth + 0.6f, y));
            nextSpawn = Time.fixedTime + game.settings.spawnInterval;
        }
        public EnemyController SpawnAt(Vector2 position)
        {
            EnemyController enemy = Instantiate(prefab, position, Quaternion.identity, transform);
            enemy.Initialize(game);
            return enemy;
        }
    }
}
