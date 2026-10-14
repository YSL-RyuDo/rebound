using UnityEngine;

namespace Rebound
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        public GameManager game;
        public PlayerHealth health;
        public FallEnergySystem energy;
        public ReboundAttack rebound;
        public bool readKeyboard = true;
        Rigidbody2D body;
        float axis;
        bool riseQueued;
        float nextRise;
        public float VerticalSpeed => body.velocity.y;

        void Awake() { body = GetComponent<Rigidbody2D>(); }
        void Update()
        {
            if (!readKeyboard) return;
            SetMoveAxis((Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f));
            if (Input.GetKeyDown(KeyCode.Space)) RequestRise();
        }
        public void SetMoveAxis(float value) { axis = Mathf.Clamp(value, -1f, 1f); }
        public void RequestRise() { if (!game.IsGameOver) riseQueued = true; }

        void FixedUpdate()
        {
            if (game.IsGameOver) { body.velocity = Vector2.zero; riseQueued = false; return; }
            Vector2 position = body.position;
            if (position.y < game.FallY)
            {
                health.TakeDamage(true);
                ResetToSafePosition();
                return;
            }
            energy.ObserveHeight(position.y);
            float vy = Mathf.Max(body.velocity.y - game.settings.gravity * Time.fixedDeltaTime, -game.settings.maxFallSpeed);
            if (riseQueued && Time.fixedTime >= nextRise)
            {
                vy = game.settings.riseSpeed;
                nextRise = Time.fixedTime + game.settings.riseInterval;
                rebound.TryFire();
            }
            riseQueued = false; // A press inside cooldown is ignored, never converted into hold-to-fly.
            float vx = axis * game.settings.moveSpeed;
            float nextX = Mathf.Clamp(position.x + vx * Time.fixedDeltaTime, game.PlayerMinX, game.PlayerMaxX);
            vx = (nextX - position.x) / Time.fixedDeltaTime;
            if (position.y + vy * Time.fixedDeltaTime > game.TopY)
                vy = Mathf.Min(0f, (game.TopY - position.y) / Time.fixedDeltaTime);
            body.velocity = new Vector2(vx, vy);
        }

        public void ResetToSafePosition()
        {
            body.position = game.SafePosition;
            body.velocity = Vector2.zero;
            riseQueued = false;
            nextRise = Time.fixedTime;
            energy.ResetTracking(game.SafePosition.y, true);
        }
    }
}
