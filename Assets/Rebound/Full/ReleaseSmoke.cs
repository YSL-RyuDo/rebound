using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Rebound.Full
{
    // Opt-in integration harness for the actual Windows player. Never runs during a normal launch.
    public sealed class ReleaseSmoke : MonoBehaviour
    {
        GameSession game;string output;bool finished;float started;
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;game=GetComponent<GameSession>();game.TestMode=true;
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--smoke-output");
            output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"Smoke");Directory.CreateDirectory(output);
            Application.logMessageReceived+=OnLog;
            game.Save=new SaveManager(Path.Combine(output,"save"));game.Save.Data.width=1280;game.Save.Data.height=720;game.ApplyDisplay();
            game.Mods=new ModManager(Path.Combine(output,"mods"));
            yield return new WaitForSecondsRealtime(1);yield return Capture("01-menu.png");
            foreach(string id in new[]{"light-frame","overcharge"})
            {
                if(!Directory.Exists(Path.Combine(game.Mods.Root,id)))game.Mods.Install(Path.Combine(Application.streamingAssetsPath,"SampleMods",id+".zip"));
                game.Save.Data.enabledMods=new[]{id};game.StartRun();game.Player.ReadInput=false;
                Check(Mathf.Abs(game.Stats.Gravity-(id=="light-frame"?4.2f:8.5f))<.01f,"Windows ZIP applied: "+id);
                for(int i=0;i<120;i++){if(game.State==GameState.LevelUp)game.Progress.Choose(0);if(game.Player.Position.y<-.8f)game.Player.Rise();yield return new WaitForSecondsRealtime(.025f);}
            }
            game.Save.Data.enabledMods=new string[0];game.StartRun();game.Player.ReadInput=false;Check(game.Stats.Gravity==6,"Base content restored");
            for(int i=0;i<4;i++)game.World.Spawn(game.Content.Enemies[new[]{"scout","hunter","shield","gunner"}[i]],new Vector2(2+i*1.6f,-2+i*1.5f));
            yield return new WaitForSeconds(.2f);yield return Capture("02-game.png");
            game.Progress.AddXp(20);game.Progress.OpenNext();yield return Capture("03-upgrades.png");game.Progress.Choose(0);
            game.Elapsed=240;game.Stage.Tick(0);var boss=game.Stage.Boss;boss.Position=new Vector2(6,0);boss.Hp=boss.MaxHp*.2f;
            yield return new WaitForSeconds(.15f);Check(boss.Phase==3,"Boss phase 3");yield return Capture("04-boss.png");
            game.World.Hit(boss,100000,true);Check(game.State==GameState.Clear,"Victory transition");yield return Capture("05-clear.png");
            game.StartRun();Check(game.Player.Hp==3&&game.Progress.Level==1,"Retry resets run");
            game.Player.Damage(1,true);game.Player.Damage(1,true);game.Player.Damage(1,true);Check(game.State==GameState.GameOver,"Game over transition");
            finished=true;File.AppendAllText(Path.Combine(output,"results.txt"),"SMOKE PASSED\n");Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            // Render explicitly: Windows may skip swapchain rendering for a hidden test window.
            yield return null;
            var canvas=UnityEngine.Object.FindObjectOfType<Canvas>();var mode=canvas.renderMode;
            var texture=new RenderTexture(1280,720,24);var previous=RenderTexture.active;var rect=game.Camera.rect;
            game.Camera.targetTexture=texture;game.Camera.rect=new Rect(0,0,1,1);game.Camera.aspect=16f/9f;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.Camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();game.Camera.Render();RenderTexture.active=texture;
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());
            canvas.renderMode=mode;game.Camera.targetTexture=null;game.Camera.rect=rect;game.Camera.ResetAspect();RenderTexture.active=previous;
            texture.Release();Destroy(texture);Destroy(pixels);
            yield return new WaitForSecondsRealtime(.1f);
        }
        void Check(bool condition,string label){if(!condition)throw new Exception("SMOKE FAILED: "+label);File.AppendAllText(Path.Combine(output,"results.txt"),"PASS "+label+"\n");}
        void OnLog(string message,string stack,LogType type)
        {
            if(!finished&&(type==LogType.Exception||type==LogType.Error)){finished=true;File.AppendAllText(Path.Combine(output,"results.txt"),"ERROR "+message+"\n"+stack);Application.Quit(1);}
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        void Update(){if(!finished&&output!=null&&Time.realtimeSinceStartup-started>60){finished=true;File.AppendAllText(Path.Combine(output,"results.txt"),"SMOKE TIMEOUT\n");Application.Quit(2);}}
    }
}
