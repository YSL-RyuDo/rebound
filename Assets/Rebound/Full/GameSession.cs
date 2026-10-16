using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Rebound.Full
{
    public enum GameState {Boot,Menu,Story,Playing,Pause,LevelUp,GameOver,Clear,Settings,Mods,Credits}
    public sealed class GameSession : MonoBehaviour
    {
        public GameState State {get;private set;}=GameState.Boot;
        public SaveManager Save;public ModManager Mods;public ContentRegistry Content;public ArtFactory Art;
        public AudioDirector Audio;public VisualEffects Effects;public CombatWorld World;public PlayerFlight Player;public GameUI UI;
        public Progression Progress;public PlayerStats Stats=new PlayerStats();public StageDirector Stage;
        public Camera Camera;public float Elapsed;public int Score,Kills;public string FlightId="re01",StageId="powerplant";
        public string Notice="";public float NoticeUntil;public bool TestMode;
        public const float Left=-8.8889f,Right=8.8889f,Top=3.5f,Bottom=-5.2f;
        ContentPack baseline;Transform arena;float shake;GameState returnTo=GameState.Menu;
        public bool Running => State==GameState.Playing;
        void Awake()
        {
            Application.targetFrameRate=120;
            Camera=UnityEngine.Object.FindObjectOfType<Camera>();
            if(!Camera){Camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();Camera.tag="MainCamera";}
            Camera.orthographic=true;Camera.orthographicSize=5;Camera.transform.position=new Vector3(0,0,-10);Camera.backgroundColor=new Color(.015f,.026f,.055f);Camera.clearFlags=CameraClearFlags.SolidColor;
            Save=new SaveManager(Application.persistentDataPath);
            Mods=new ModManager(Path.Combine(Application.persistentDataPath,"Mods"));
            var text=Resources.Load<TextAsset>("base-content");baseline=text?JsonUtility.FromJson<ContentPack>(text.text):BuiltinContent.Create();
            Audio=gameObject.AddComponent<AudioDirector>();Audio.Initialize(Save);
            Art=new ArtFactory();Effects=new GameObject("Effects").AddComponent<VisualEffects>();Effects.Initialize(this);
            ReloadContent();
            UI=gameObject.AddComponent<GameUI>();UI.Initialize(this);
            SetState(GameState.Menu);ApplyDisplay();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--rebound-smoke")>=0)gameObject.AddComponent<ReleaseSmoke>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--rebound-video")>=0)gameObject.AddComponent<GameplayRecorder>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--workshop-smoke")>=0&&!WorkshopSmoke.Running)new GameObject("Workshop integration harness").AddComponent<WorkshopSmoke>();
        }
        public void ReloadContent()
        {
            Mods.Scan(Save.Data.enabledMods);Content=Mods.Load(baseline);
            if(WorkshopPlaytest.Content!=null){Content=WorkshopPlaytest.Content;TestMode=true;}
            // Recreate generated assets so disabling a MOD always restores base graphics and sounds.
            if(arena) Destroy(arena.gameObject);
            Art.Dispose();Art=new ArtFactory();Art.LoadMods(Content);
            Destroy(Audio);Audio=gameObject.AddComponent<AudioDirector>();Audio.Initialize(Save);Audio.LoadMods(Content);
            if(!Content.Flight.ContainsKey(FlightId))FlightId="re01";if(!Content.Stages.ContainsKey(StageId))StageId="powerplant";
            arena=new GameObject("Parallax reactor").transform;
            if(Content.Stages[StageId].backgroundMode=="stretch")
            {
                var background=Art.Draw("Background",Content.Stages[StageId].background,Vector2.zero,Vector2.one,Color.white,arena,-5);
                background.transform.localScale=new Vector2((Right-Left)/background.sprite.bounds.size.x,10/background.sprite.bounds.size.y);
            }
            else for(int i=0;i<30;i++)
            {
                float x=-10+(i%10)*2.1f,y=-4+(i/10)*3.8f;
                Art.Draw("Reactor panel",Content.Stages[StageId].background,new Vector2(x,y),new Vector2(1.9f,3.4f),new Color(.06f,.13f,.2f),arena,-5);
            }
            if(Mods.Errors.Count>0)Notice=string.Join("\n",Mods.Errors);
        }
        public void StartRun()
        {
            ClearRun();ReloadContent();Elapsed=0;Score=0;Kills=0;Progress=new Progression(this);RebuildStats();
            World=new GameObject("CombatWorld").AddComponent<CombatWorld>();World.Initialize(this);
            Player=new GameObject("RE-01").AddComponent<PlayerFlight>();Player.Initialize(this);
            Stage=new StageDirector(this,Content.Stages[StageId]);
            SetState(GameState.Playing);Announce(Content.Line("outer"));Audio.MusicTrack("battle");
        }
        public void RebuildStats(){var f=Content.Flight[FlightId];Stats.Rebuild(f,Content.Weapons[f.weapon],Progress.Ranks,Content);}
        public void SetState(GameState state)
        {
            State=state;Time.timeScale=(state==GameState.Pause||state==GameState.LevelUp||state==GameState.Settings)?0:1;
            if(state==GameState.Menu||state==GameState.Story||state==GameState.Mods||state==GameState.Credits)Audio.MusicTrack("menu");
            if(UI)UI.Render();
        }
        public void ShowSettings(){returnTo=State;SetState(GameState.Settings);}
        public void CloseSettings(){Save.Save();ApplyDisplay();SetState(returnTo);}
        public void MainMenu(){ClearRun();SetState(GameState.Menu);}
        public void OpenWorkshop(){ClearRun();WorkshopPlaytest.Content=null;UnityEngine.SceneManagement.SceneManager.LoadScene("ModWorkshop");}
        void ClearRun()
        {
            Time.timeScale=1;if(World)Destroy(World.gameObject);if(Player)Destroy(Player.gameObject);World=null;Player=null;Stage=null;Effects.Clear();
        }
        public void Finish(bool victory)
        {
            if(!Running)return;
            SetState(victory?GameState.Clear:GameState.GameOver);Audio.Play(victory?"win":"lose");if(victory)Audio.MusicTrack("clear");
            if(!TestMode){Save.Data.bestScore=Mathf.Max(Save.Data.bestScore,Score);Save.Data.bestTime=Mathf.Max(Save.Data.bestTime,Elapsed);if(victory)Save.Data.clears++;Save.Save();}
        }
        public void EnemyKilled(int xp,Vector2 pos,string effect="explosion")
        {
            if(!Running)return;Kills++;Score+=xp*10;Progress.AddXp(xp);Effects.Burst(pos,Art.Red,10,.16f,effect);Audio.Play("explode",.5f);
        }
        public void Announce(string text){Notice=text;NoticeUntil=Time.unscaledTime+4;}
        public void Shake(float amount){shake=Mathf.Max(shake,amount);}
        public void ApplyDisplay()
        {
            if(Application.isBatchMode)return;
            Screen.SetResolution(Save.Data.width,Save.Data.height,Save.Data.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        }
        void Update()
        {
            if(!UI)return;
            if(Input.GetKeyDown(KeyCode.Escape)&&!UI.Rebinding)
            {
                if(State==GameState.Playing)SetState(GameState.Pause);else if(State==GameState.Pause)SetState(GameState.Playing);else if(State==GameState.Settings)CloseSettings();
            }
            if(Running)
            {
                Elapsed+=Time.deltaTime;Stage.Tick(Time.deltaTime);
                if(Progress.Pending>0)Progress.OpenNext();
            }
            float ratio=(float)Screen.width/Mathf.Max(1,Screen.height)/(16f/9f);
            Camera.rect=ratio>=1?new Rect((1-1/ratio)*.5f,0,1/ratio,1):new Rect(0,(1-ratio)*.5f,1,ratio);
            shake=Mathf.Max(0,shake-Time.unscaledDeltaTime);
            Vector2 offset=Save.Data.shake&&Running?UnityEngine.Random.insideUnitCircle*shake*.12f:Vector2.zero;
            Camera.transform.position=new Vector3(offset.x,offset.y,-10);
            if(arena&&Running&&Content.Stages[StageId].backgroundMode=="tile") foreach(Transform panel in arena) {panel.position+=Vector3.left*Time.deltaTime*.15f;if(panel.position.x< -11)panel.position+=Vector3.right*21;}
        }
        void OnDestroy(){Time.timeScale=1;if(Art!=null)Art.Dispose();if(arena)Destroy(arena.gameObject);if(Effects)Destroy(Effects.gameObject);if(World)Destroy(World.gameObject);if(Player)Destroy(Player.gameObject);}
    }
}
