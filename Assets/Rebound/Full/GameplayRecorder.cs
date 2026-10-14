using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebound.Full
{
    // Opt-in capture only. Drives the normal input/UI APIs; does not alter HP, XP, damage, enemies or stage time.
    public sealed class GameplayRecorder : MonoBehaviour
    {
        GameSession game;string output;System.Diagnostics.Process encoder;BinaryWriter audio;
        RenderTexture target;Texture2D pixels;Canvas canvas;Text caption;bool recording;int frame,held;
        IEnumerator Start()
        {
            game=GetComponent<GameSession>();game.TestMode=true;
            var args=Environment.GetCommandLineArgs();output=Argument(args,"--video-output");string ffmpeg=Argument(args,"--ffmpeg");Directory.CreateDirectory(output);
            game.Save=new SaveManager(Path.Combine(output,"settings"));game.Save.Data.width=1280;game.Save.Data.height=720;game.Save.Data.shake=false;game.ApplyDisplay();
            game.Mods=new ModManager(Path.Combine(output,"Mods"));game.ReloadContent();game.SetState(GameState.Menu);
            QualitySettings.vSyncCount=0;Time.captureFramerate=30;
            var config=AudioSettings.GetConfiguration();config.speakerMode=AudioSpeakerMode.Stereo;config.sampleRate=48000;AudioSettings.Reset(config);
            yield return null;
            if(!AudioRenderer.Start())throw new Exception("Audio capture could not start");
            audio=new BinaryWriter(File.Create(Path.Combine(output,"audio.f32")));
            var start=new System.Diagnostics.ProcessStartInfo(ffmpeg,"-y -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \""+Path.Combine(output,"video.mp4")+"\"")
            {UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardError=true};
            encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(Path.Combine(output,"encoder.log"),e.Data+"\n");};encoder.BeginErrorReadLine();
            target=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            canvas=FindObjectOfType<Canvas>();
            var panel=new GameObject("Recording caption",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);
            var r=panel.GetComponent<RectTransform>();r.anchorMin=new Vector2(.02f,.07f);r.anchorMax=new Vector2(.98f,.13f);r.offsetMin=r.offsetMax=Vector2.zero;panel.GetComponent<Image>().color=new Color(.015f,.03f,.06f,.94f);
            caption=new GameObject("Caption",typeof(RectTransform),typeof(Text)).GetComponent<Text>();caption.transform.SetParent(panel.transform,false);
            var cr=caption.rectTransform;cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=new Vector2(16,0);cr.offsetMax=new Vector2(-16,0);
            caption.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",26);caption.fontSize=26;caption.alignment=TextAnchor.MiddleLeft;caption.color=Color.white;
            Application.logMessageReceived+=OnLog;recording=true;
            for(frame=0;frame<5160;frame++)
            {
                Direct();
                yield return null;
                CaptureFrame();
                if(frame%300==0)File.WriteAllText(Path.Combine(output,"progress.txt"),$"{frame}/5160 frames; state={game.State}; kills={game.Kills}; hp={(game.Player?game.Player.Hp:0)}");
            }
            Finish();File.WriteAllText(Path.Combine(output,"complete.txt"),"5160 frames / 30 fps / 172 seconds\n"+AudioSettings.outputSampleRate+" Hz stereo float audio\n");Application.Quit(0);
        }
        static string Argument(string[] args,string key){int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length)throw new ArgumentException(key);return args[i+1];}
        void Click(string label,int index=0)
        {
            var buttons=FindObjectsOfType<Button>().Where(b=>b.gameObject.activeInHierarchy&&b.GetComponentInChildren<Text>()?.text==label).OrderByDescending(b=>b.transform.position.y).ToArray();
            if(index>=buttons.Length)throw new Exception("Recording UI button not found: "+label);buttons[index].onClick.Invoke();
        }
        void Direct()
        {
            if(frame==90)Click("START MISSION");if(frame==210)Click("LAUNCH");
            if(frame==1800)game.MainMenu();if(frame==1860)Click("MOD MANAGER");if(frame==1920)Click("INSTALL SAMPLES");
            if(frame==1980)Click("ENABLE",0);if(frame==2040)Click("BACK");if(frame==2100)Click("START MISSION");if(frame==2160)Click("LAUNCH");
            if(frame==3450)game.MainMenu();if(frame==3510)Click("MOD MANAGER");
            if(frame==3570){Click("DISABLE");Click("ENABLE",1);}if(frame==3690)Click("BACK");if(frame==3750)Click("START MISSION");if(frame==3810)Click("LAUNCH");
            if(frame==5100)game.MainMenu();
            string section=frame<1800?"01 기본 게임":frame<2160?"02 LIGHT FRAME 설치·활성화":frame<3450?"02 LIGHT FRAME 플레이":frame<3810?"03 OVERCHARGE 활성화":"03 OVERCHARGE 플레이";
            string stats=game.Player&&game.Running?$"  |  중력 {game.Stats.Gravity:0.0} · 충전 {game.Stats.Charge:0} · 반동 {game.Stats.Rebound:0}":"  |  실제 메뉴 조작 및 적용";
            caption.text=section+stats+"  /  자동 조작 녹화";
            caption.transform.parent.SetAsLastSibling();
            if(game.Player)game.Player.ReadInput=false;
            if(game.Running)
            {
                held=0;float desired=-5.4f+Mathf.Sin(game.Elapsed*.7f)*1.1f;game.Player.InputAxis(Mathf.Clamp((desired-game.Player.Position.x)*2,-1,1));
                if(game.Player.Position.y < -1.45f)game.Player.Rise();
            }
            else if(game.State==GameState.LevelUp)
            {
                if(++held>=75)
                {
                    int pick=game.Progress.Choices.FindIndex(c=>c.effect=="damage"||c.effect=="multi"||c.effect=="rebound");
                    game.Progress.Choose(pick<0?0:pick);held=0;
                }
            }
            else if(game.State==GameState.GameOver){if(++held>=90){Click("RETRY");held=0;}}
        }
        void CaptureFrame()
        {
            var previous=RenderTexture.active;var rect=game.Camera.rect;
            game.Camera.targetTexture=target;game.Camera.rect=new Rect(0,0,1,1);game.Camera.aspect=16f/9;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.Camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();game.Camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
            byte[] bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;game.Camera.targetTexture=null;game.Camera.rect=rect;game.Camera.ResetAspect();RenderTexture.active=previous;
            int count=AudioRenderer.GetSampleCountForCaptureFrame();
            if(count>0)using(var samples=new NativeArray<float>(count*2,Allocator.Temp)){if(!AudioRenderer.Render(samples))throw new Exception("Audio render failed");foreach(float v in samples)audio.Write(v);}
        }
        void Finish()
        {
            if(!recording)return;recording=false;AudioRenderer.Stop();Time.captureFramerate=0;audio.Dispose();encoder.StandardInput.Close();
            if(!encoder.WaitForExit(15000)||encoder.ExitCode!=0)Debug.LogError("Video encoder failed");
            target.Release();Destroy(target);Destroy(pixels);
        }
        void OnLog(string message,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error){File.AppendAllText(Path.Combine(output,"error.txt"),message+"\n"+stack);Finish();Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=OnLog;Finish();}
    }
}
