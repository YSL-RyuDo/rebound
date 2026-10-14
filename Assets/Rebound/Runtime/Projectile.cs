using UnityEngine;

namespace Rebound
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        Rigidbody2D body;
        ProjectilePool pool;
        GameManager game;
        float expires;
        bool flying;
        void Awake() { body = GetComponent<Rigidbody2D>(); }
        public void Launch(ProjectilePool owner, GameManager manager, Vector2 position)
        {
            pool = owner;
            game = manager;
            gameObject.SetActive(true);
            body.position = position;
            body.velocity = Vector2.right * game.settings.projectileSpeed;
            expires = Time.time + game.settings.projectileLifetime;
            flying = true;
        }
        void FixedUpdate()
        {
            if (flying && (game.IsGameOver || Time.time >= expires || body.position.x > GameManager.HalfWidth + 1f)) Release();
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!flying || game.IsGameOver) return;
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy == null || enemy.Game != game) return;
            enemy.TakeDamage(game.settings.projectileDamage);
            Release();
        }
        public void Release()
        {
            if (!flying) return;
            flying = false;
            body.velocity = Vector2.zero;
            gameObject.SetActive(false);
            pool.Return(this);
        }
    }
}
