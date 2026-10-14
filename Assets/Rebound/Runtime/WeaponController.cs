using UnityEngine;

namespace Rebound
{
    public sealed class WeaponController : MonoBehaviour
    {
        public GameManager game;
        public ProjectilePool pool;
        float nextShot;
        void FixedUpdate()
        {
            if (game.IsGameOver || Time.fixedTime < nextShot) return;
            pool.Fire((Vector2)transform.position + Vector2.right * 0.55f);
            nextShot = Time.fixedTime + game.settings.shotInterval;
        }
    }
}
