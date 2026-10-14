using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rebound
{
    public sealed class GameManager : MonoBehaviour
    {
        public PrototypeSettings settings;
        public Camera gameCamera;
        public PlayerController player;
        public PlayerHealth health;
        public FallEnergySystem energy;
        public EnemySpawner spawner;
        public bool IsGameOver { get; private set; }
        public float SurvivalTime { get; private set; }
        public int Kills { get; private set; }
        public const float HalfHeight = 5f;
        public const float HalfWidth = HalfHeight * 16f / 9f;
        public Vector2 SafePosition => new Vector2(-HalfWidth * 0.63f, 1f);
        public float PlayerMinX => -HalfWidth + 0.35f;
        public float PlayerMaxX => -HalfWidth + HalfWidth * 2f * settings.movementWidth - 0.35f;
        // Leave space for the HUD and the player's half-height.
        public float TopY => HalfHeight - 1.6f;
        public float FallY => -HalfHeight - 0.45f;

        void Update()
        {
            if (!IsGameOver) SurvivalTime += Time.deltaTime;
            else if (Input.GetKeyDown(KeyCode.R)) Retry();
            // Preserve a 16:9 playfield even in a differently sized Game view/window.
            float ratio = (float)Screen.width / Mathf.Max(1, Screen.height) / (16f / 9f);
            gameCamera.rect = ratio >= 1f
                ? new Rect((1f - 1f / ratio) / 2f, 0, 1f / ratio, 1)
                : new Rect(0, (1f - ratio) / 2f, 1, ratio);
        }

        public void RegisterKill() { Kills++; }
        public void EndGame() { IsGameOver = true; }
        public void Retry() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    }
}
