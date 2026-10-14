using UnityEngine;

namespace Rebound
{
    // Dependency-free prototype UI. Replace this view without touching game simulation.
    public sealed class GameHud : MonoBehaviour
    {
        public GameManager game;
        GUIStyle title, label, small, button;
        void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
                label = new GUIStyle(GUI.skin.label) { fontSize = 23 };
                small = new GUIStyle(GUI.skin.label) { fontSize = 17 };
                button = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            }
            Rect view = game.gameCamera.pixelRect;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(view.x, Screen.height - view.yMax, 0), Quaternion.identity,
                new Vector3(view.width / 1280f, view.height / 720f, 1));
            GUI.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(0, 0, 1280, 90), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(25, 10, 230, 45), "REBOUND", title);
            GUI.Label(new Rect(26, 53, 220, 30), "PHASE 1 / PROTOTYPE", small);
            GUI.Label(new Rect(260, 18, 170, 36), $"HP  {game.health.CurrentHp} / {game.settings.maxHp}", label);
            GUI.Label(new Rect(450, 10, 420, 30), $"FALL ENERGY   {game.energy.Energy:0} / 100", small);
            GUI.color = new Color(0.14f, 0.2f, 0.27f);
            GUI.DrawTexture(new Rect(450, 46, 320, 18), Texture2D.whiteTexture);
            GUI.color = game.energy.IsReady ? new Color(1f, 0.8f, 0.25f) : new Color(0.25f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(450, 46, 320 * game.energy.Energy / 100f, 18), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(450 + 320 * game.settings.reboundThreshold / 100f, 43, 2, 24), Texture2D.whiteTexture);
            GUI.Label(new Rect(800, 28, 220, 40), game.energy.IsReady ? "REBOUND READY" : "FALL TO CHARGE", small);
            GUI.Label(new Rect(1050, 14, 210, 35), $"TIME  {game.SurvivalTime:0.0}s", label);
            GUI.Label(new Rect(1050, 51, 210, 30), $"KILLS  {game.Kills}", small);
            GUI.Label(new Rect(25, 682, 1230, 32), "A / D  MOVE     SPACE  RISE     FALL > CHARGE     65+ ENERGY + SPACE  REBOUND     AUTO FIRE >", small);
            if (game.IsGameOver)
            {
                GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
                GUI.DrawTexture(new Rect(390, 230, 500, 250), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(470, 254, 390, 50), "GAME OVER", title);
                GUI.Label(new Rect(470, 315, 390, 45), $"SURVIVED {game.SurvivalTime:0.0}s / {game.Kills} KILLS", label);
                if (GUI.Button(new Rect(475, 385, 330, 60), "RETRY  [ R ]", button)) game.Retry();
            }
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
        }
    }
}
