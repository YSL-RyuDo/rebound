using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Rebound.Full
{
    // Opt-in Windows integration test. Uses real UI callbacks, isolated drafts and no user MOD writes.
    public sealed class WorkshopSmoke : MonoBehaviour
    {
        public static bool Running;
        string output,previousDraft;bool finished;float started;
        void Awake(){Running=true;DontDestroyOnLoad(gameObject);started=Time.realtimeSinceStartup;previousDraft=PlayerPrefs.GetString("Workshop.LastDraft","");}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--smoke-output");
            output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.temporaryCachePath,"WorkshopSmoke");Directory.CreateDirectory(output);
            Application.logMessageReceived+=OnLog;Screen.SetResolution(1600,900,false);
            WorkshopPlaytest.DraftPath=Path.Combine(output,"draft");
            yield return SceneManager.LoadSceneAsync("ModWorkshop");yield return null;
            var workshop=FindObjectOfType<ModWorkshop>();Check(workshop!=null,"Workshop scene loaded");
            Check(workshop.Project.Root==Path.GetFullPath(Path.Combine(output,"draft")),"Smoke draft is isolated from user Workshop files");
            Click("straight  >");Click("APPLY");Check(workshop.Project.Validate().Weapons["pulse"].trajectory=="wave","UI changes trajectory to wave");
            yield return Capture("06-workshop-weapon.png");
            Click("UPGRADES");Click("Amplifier\ndamage");Click("CLONE / NEW");Click("silver  >");Click("gold  >");Click("platinum  >");Click("ADD EFFECT");Click("APPLY");
            var upgrade=workshop.Project.Draft.content.upgrades.Single();Check(upgrade.tier=="prismatic"&&upgrade.effects.Length==2,"UI creates prismatic composite augment");
            yield return Capture("07-workshop-augment.png");
            Click("FLIGHT");
            var art=new ArtFactory();string png=Path.Combine(output,"custom-ship.png");File.WriteAllBytes(png,art.Get("lightframe").texture.EncodeToPNG());art.Dispose();
            var input=FindObjectsOfType<InputField>().Single(i=>i.gameObject.name=="Import path");input.text=png;input.onEndEdit.Invoke(png);Click("IMPORT PNG");
            Check(workshop.Project.Validate().Flight["re01"].skin=="re01_skin","PNG assigned through UI");
            Click("EFFECTS");
            var colorInput=FindObjectsOfType<InputField>().Single(i=>i.text=="#FF4538");colorInput.text="#00FF80";colorInput.onEndEdit.Invoke(colorInput.text);Click("APPLY");
            Check(workshop.Project.Validate().Effects["explosion"].color=="#00FF80","Effect color edited through UI");yield return Capture("10-workshop-effects.png");
            string zip=workshop.Project.Export(Path.Combine(output,"exports"));var manager=new ModManager(Path.Combine(output,"mods"));string id=manager.Install(zip);manager.Scan(new[]{id});
            var baseText=Resources.Load<TextAsset>("base-content");var content=manager.Load(JsonUtility.FromJson<ContentPack>(baseText.text));
            Check(content.Weapons["pulse"].trajectory=="wave"&&content.Assets.ContainsKey("re01_skin"),"Exported ZIP installs with trajectory and PNG");
            Click("TEST PLAY");yield return null;yield return null;
            var game=FindObjectOfType<GameSession>();Check(game!=null&&WorkshopPlaytest.Content!=null,"Test play opens game with draft");game.StartRun();game.Player.ReadInput=false;
            Check(game.TestMode&&game.Stats.Weapon.trajectory=="wave","Draft active; records disabled");
            var enemy=game.World.Spawn(game.Content.Enemies["scout"],new Vector2(2,0));game.World.Hit(enemy,10000);
            Check(game.Effects.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.color.g>.99f&&r.color.r<.01f),"Edited explosion is rendered in combat");
            for(int i=0;i<80;i++){if(game.State==GameState.LevelUp)game.Progress.Choose(0);if(game.Player.Position.y<-.8f)game.Player.Rise();yield return new WaitForSecondsRealtime(.025f);}
            Check(game.World.Projectiles.Any(p=>p.Active&&p.MotionData?.trajectory=="wave"),"Draft bullets run through combat pool");yield return Capture("08-workshop-playtest.png");
            game.Content.Weapons["pulse"].crit=1;game.RebuildStats();Check(!game.Progress.CanOffer(game.Content.Upgrades["crit"]),"Capped critical augment excluded");
            game.Progress.AddXp(20);game.Progress.OpenNext();yield return Capture("09-augment-tiers.png");
            game.OpenWorkshop();yield return null;yield return null;Check(FindObjectOfType<ModWorkshop>()!=null&&WorkshopPlaytest.Content==null,"Return restores authoring draft");
            workshop=FindObjectOfType<ModWorkshop>();Click("WEAPONS");Click("B · 수식 방식 / FORMULA");
            Edit("formulaX","v*t");Edit("formulaY","a*sin(2*pi*f*t)+0.2*sin(2*pi*5*t)");Click("APPLY");
            Check(workshop.Project.Validate().Weapons["pulse"].motionMode=="formula","Formula mode and custom expression apply through UI");
            yield return Capture("14-formula-workshop.png");
            Click("A · 수치 방식 / PARAMETERS");Click("APPLY");
            Check(workshop.Project.Validate().Weapons["pulse"].motionMode=="parameters","Parameter version remains selectable");
            yield return Capture("15-parameter-workshop.png");
            Click("B · 수식 방식 / FORMULA");Click("APPLY");
            var formulas=new ModManager(Path.Combine(output,"formula-mods"));string formulaId=formulas.Install(workshop.Project.Export(Path.Combine(output,"formula-exports")));formulas.Scan(new[]{formulaId});
            Check(formulas.Load(JsonUtility.FromJson<ContentPack>(baseText.text)).Weapons["pulse"].formulaY.Contains("0.2*sin"),"Formula survives ZIP export and installation");
            Click("TEST PLAY");yield return null;yield return null;game=FindObjectOfType<GameSession>();game.StartRun();game.Player.ReadInput=false;
            for(int i=0;i<50;i++){if(game.State==GameState.LevelUp)game.Progress.Choose(0);if(game.Player.Position.y<-.8f)game.Player.Rise();yield return new WaitForSecondsRealtime(.025f);}
            Check(game.World.Projectiles.Any(p=>p.Active&&p.MotionData?.motionMode=="formula"&&!p.Motion.Invalid),"Formula bullets move in real Windows combat");
            yield return Capture("16-formula-combat.png");
            finished=true;File.AppendAllText(Path.Combine(output,"results.txt"),"WORKSHOP SMOKE PASSED\n");Restore();Application.Quit(0);
        }
        static void Edit(string field,string value)
        {var input=FindObjectsOfType<InputField>().Single(i=>i.gameObject.name=="Field:"+field);input.text=value;input.onEndEdit.Invoke(value);}
        static void Click(string text)
        {
            var button=FindObjectsOfType<Button>().FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text==text);
            if(!button)throw new Exception("UI button missing: "+text);button.onClick.Invoke();
        }
        IEnumerator Capture(string name)
        {
            yield return null;var camera=Camera.main;var canvas=FindObjectOfType<Canvas>();var mode=canvas.renderMode;
            var target=new RenderTexture(1600,900,24);var previous=RenderTexture.active;var rect=camera.rect;
            camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.aspect=16f/9f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
            canvas.renderMode=mode;camera.targetTexture=null;camera.rect=rect;camera.ResetAspect();RenderTexture.active=previous;target.Release();Destroy(target);Destroy(image);
        }
        void Check(bool condition,string text){if(!condition)throw new Exception("WORKSHOP SMOKE FAILED: "+text);File.AppendAllText(Path.Combine(output,"results.txt"),"PASS "+text+"\n");}
        void OnLog(string text,string stack,LogType type){if(!finished&&(type==LogType.Error||type==LogType.Exception)){finished=true;File.AppendAllText(Path.Combine(output,"results.txt"),text+"\n"+stack);Restore();Application.Quit(1);}}
        void Restore(){PlayerPrefs.SetString("Workshop.LastDraft",previousDraft);PlayerPrefs.Save();WorkshopPlaytest.Content=null;WorkshopPlaytest.DraftPath=null;}
        void Update(){if(!finished&&output!=null&&Time.realtimeSinceStartup-started>60){finished=true;File.AppendAllText(Path.Combine(output,"results.txt"),"TIMEOUT\n");Restore();Application.Quit(2);}}
        void OnDestroy(){Application.logMessageReceived-=OnLog;Running=false;}
    }
}
