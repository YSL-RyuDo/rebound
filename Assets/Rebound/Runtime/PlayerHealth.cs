using UnityEngine;

namespace Rebound
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        public GameManager game;
        public SpriteRenderer visual;
        public int CurrentHp { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        float invulnerableUntil;
        void Awake() { CurrentHp = game.settings.maxHp; }
        public bool TakeDamage(bool isFall = false)
        {
            if (game.IsGameOver || (!isFall && IsInvulnerable)) return false;
            CurrentHp = Mathf.Max(0, CurrentHp - 1);
            invulnerableUntil = Time.time + game.settings.invulnerability;
            if (CurrentHp == 0) game.EndGame();
            return true;
        }
        void Update()
        {
            visual.color = IsInvulnerable && !game.IsGameOver
                ? new Color(0.3f, 0.95f, 1f, Mathf.PingPong(Time.time * 8f, 0.65f) + 0.25f)
                : new Color(0.3f, 0.95f, 1f);
        }
    }
}
