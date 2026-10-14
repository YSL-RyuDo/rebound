using System.Collections.Generic;
using UnityEngine;

namespace Rebound
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class EnemyController : MonoBehaviour
    {
        public static readonly List<EnemyController> Active = new List<EnemyController>();
        public GameManager Game { get; private set; }
        public int Hp { get; private set; }
        Rigidbody2D body;
        bool dead;
        void Awake() { body = GetComponent<Rigidbody2D>(); }
        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }
        public void Initialize(GameManager game) { Game = game; Hp = game.settings.enemyHp; }
        void FixedUpdate()
        {
            if (Game == null || Game.IsGameOver || dead) { body.velocity = Vector2.zero; return; }
            body.MovePosition(body.position + Vector2.left * (Game.settings.enemySpeed * Time.fixedDeltaTime));
            if (body.position.x < -GameManager.HalfWidth - 1f) Despawn();
        }
        public void TakeDamage(int amount)
        {
            if (dead || Game == null || Game.IsGameOver || amount <= 0) return;
            Hp -= amount;
            if (Hp <= 0) { Game.RegisterKill(); Despawn(); }
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (dead || Game == null || Game.IsGameOver) return;
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null) return;
            health.TakeDamage();
            Despawn();
        }
        void Despawn() { dead = true; gameObject.SetActive(false); Destroy(gameObject); }
    }
}
