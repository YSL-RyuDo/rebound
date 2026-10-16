using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebound.Full
{
    // Opt-in documentary capture. UI callbacks and normal movement inputs only: no HP/XP/enemy/time cheats.
    public sealed class WorkshopRecorder : MonoBehaviour
    {
        public static bool Active;
        const int Fps=30,Width=1600,Height=900;
        GameSession game;string output,artPath,previousDraft,exported;int frame,cueStart;string chapter="",caption="";
        bool recording,failed;System.Diagnostics.Process encoder;BinaryWriter audio;
        RenderTexture target;Texture2D pixels;Canvas overlay;Image cursor,highlight;ArtFactory pointerArt;
        readonly List<string> cues=new List<string>();readonly List<string> chapters=new List<string>();
        void Awake(){Active=true;DontDestroyOnLoad(gameObject);previousDraft=PlayerPrefs.GetString("Workshop.LastDraft","");}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();output=Argument(args,"--video-output");artPath=Argument(args,"--demo-art");Directory.CreateDirectory(output);
            Application.logMessageReceived+=OnLog;ScopeGame();
            QualitySettings.vSyncCount=0;Time.captureFramerate=Fps;
            var config=AudioSettings.GetConfiguration();config.speakerMode=AudioSpeakerMode.Stereo;config.sampleRate=48000;AudioSettings.Reset(config);yield return null;
            if(!AudioRenderer.Start())throw new Exception("Audio capture could not start");
            audio=new BinaryWriter(File.Create(Path.Combine(output,"audio.f32")));
            var start=new System.Diagnostics.ProcessStartInfo(Argument(args,"--ffmpeg"),"-y -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1600x900 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p \""+Path.Combine(output,"video.mp4")+"\"")
            {UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardError=true};
            encoder=System.Diagnostics.Process.Start(start);encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(Path.Combine(output,"encoder.log"),e.Data+"\n");};encoder.BeginErrorReadLine();
            target=new RenderTexture(Width,Height,24);pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);CreatePointer();recording=true;
            StartCoroutine(CaptureLoop());yield return Workflow();
            if(failed)yield break;FlushCue();Finish();
            File.WriteAllText(Path.Combine(output,"subtitles.srt"),string.Join("\n",cues));File.WriteAllLines(Path.Combine(output,"chapters.tsv"),chapters);
            File.WriteAllText(Path.Combine(output,"complete.json"),"{\"frames\":"+frame+",\"fps\":30,\"sampleRate\":"+AudioSettings.outputSampleRate+",\"customAugmentRank\":"+(game.Progress.Ranks.TryGetValue("neon_core",out var rank)?rank:0)+"}");
            Restore();Application.Quit(0);
        }
        IEnumerator Workflow()
        {
            Chapter("01 기본 플레이","실제 Windows 빌드의 제작 → 설치 → 플레이 과정입니다.\n메뉴·입력 자동 조작 녹화이며 HP·경험치·게임 시간은 변경하지 않습니다.");yield return Hold(6);
            yield return Click("START MISSION");yield return Click("LAUNCH");yield return Play(26,false);
            Caption("기본 게임은 직선 탄환 1발을 자동 발사합니다.\n이제 직접 만든 이미지와 파동 궤적을 적용하겠습니다.");yield return Hold(3);
            game.SetState(GameState.Pause);yield return Click("MAIN MENU");
            WorkshopPlaytest.DraftPath=Path.Combine(output,"draft");yield return Click("MOD WORKSHOP");yield return Hold(1);
            Chapter("02 MOD 초안 만들기","MOD WORKSHOP에서 새 초안을 편집합니다.\n원본 콘텐츠와 설치된 MOD는 그대로 보존됩니다.");yield return Hold(3);
            yield return Edit("Meta:MOD ID","neon-wave-lab");yield return Edit("Meta:NAME","네온 웨이브");yield return Edit("Meta:AUTHOR","REBOUND Demo");yield return Edit("Meta:LICENSE","AI-assisted demo resources");
            Caption("MOD ID·이름·제작자·리소스 정보를 입력했습니다.\n배경·기체·적은 이번 시연용 AI 생성 원본, 탄환·효과는 자체 제작입니다.");yield return Hold(3);
            yield return Click("FLIGHT");
            Chapter("03 이미지 교체","캐릭터를 선택하고 PNG 경로를 붙여넣습니다.\nIMPORT PNG를 누르면 초안에 복사하고 캐릭터 이미지에 연결합니다.");
            yield return Import("player.png");yield return Hold(3);
            Caption("배경도 같은 방법으로 교체합니다.\n가져온 배경은 화면 전체에 표시하는 stretch 방식으로 설정됩니다.");
            yield return Click("STAGES");yield return Import("background.png");yield return Hold(3);
            Caption("몬스터는 scout를 선택해 새 이미지로 바꿉니다.\nHP나 이동 패턴은 그대로 두고 외형만 교체합니다.");
            yield return Click("ENEMIES");yield return Click("Scout\nscout");yield return Import("enemy.png");yield return Hold(3);
            Caption("정찰·추적·방어·포격 적을 각각 다른 PNG로 교체합니다.\n한 종류만 바꾸는 것이 아니라 각 적의 외형을 개별 지정할 수 있습니다.");
            yield return Click("Hunter\nhunter");yield return Import("hunter.png");yield return Click("Bulwark\nshield");yield return Import("shield.png");yield return Click("Artillery\ngunner");yield return Import("gunner.png");yield return Hold(3);
            yield return Click("WEAPONS");
            Chapter("04 탄환 궤적 제작","투사체 PNG를 바꾸고 발사 수와 공격력을 수정합니다.\n기본 1발·공격력 3에서 2발·공격력 4로 변경합니다.");
            yield return Import("bullet.png");yield return Edit("Field:damage","4");yield return Edit("Field:count","2");yield return ClickField("pattern");
            Caption("trajectory를 straight에서 wave로 변경합니다.\n진폭은 위아래 폭, frequency는 초당 파동 횟수입니다.");
            yield return ClickField("trajectory");yield return Edit("Field:amplitude","0.9");yield return Edit("Field:frequency","2");yield return Edit("Field:speed","15");yield return Click("APPLY");
            yield return FocusField("trajectory");yield return Hold(5);
            Caption("오른쪽 미리보기에서 파동 궤적을 확인합니다.\n미리보기와 실제 전투는 같은 탄환 이동 계산을 사용합니다.");yield return Hold(4);
            yield return Click("EFFECTS");
            Chapter("05 이펙트 편집","적 처치 이펙트 explosion을 편집합니다.\nPNG·색상·크기·수명·입자 수를 직접 지정할 수 있습니다.");
            yield return Import("effect.png");yield return Edit("Field:color","#4DFFB8");yield return Edit("Field:size","0.32");yield return Edit("Field:lifetime","0.65");yield return Edit("Field:count","12");yield return Click("APPLY");yield return Hold(4);
            yield return Click("UPGRADES");
            Chapter("06 기존 증강 이미지·문구 수정","기존 Rapid cycling의 이름을 ‘냉각 회로’로 바꿉니다.\n이름과 설명은 제작자가 직접 입력할 수 있습니다.");
            yield return Click("Rapid cycling\nhaste");yield return Edit("Field:name","냉각 회로");yield return Edit("Field:description","발사 속도 +25%");
            Caption("발사 속도 증가량을 18% → 25%, 최대 단계를 5 → 3으로 바꿉니다.\n설명 문구와 실제 효과 값을 함께 수정합니다.");
            yield return Edit("Field:amount","0.25");yield return Edit("Field:maxLevel","3");yield return Click("APPLY");
            Caption("기존 증강의 아이콘도 PNG로 교체합니다.\n원본 이미지 색상과 투명 배경을 유지하며 미리보기에 즉시 반영됩니다.");
            yield return Import("haste.png");yield return FocusField("name");yield return Hold(5);
            Chapter("07 새로운 복합 증강","Amplifier를 CLONE / NEW로 복제해 새 ID를 만듭니다.\n새 증강 이름은 ‘네온 과충전’, 최대 단계는 2입니다.");
            yield return Click("Amplifier\ndamage");yield return Click("CLONE / NEW");yield return Edit("Field:id","neon_core");yield return Edit("Field:name","네온 과충전");yield return Edit("Field:description","공격력 +35%, 탄환 +1");yield return Edit("Field:maxLevel","2");
            Caption("새 증강 ‘네온 과충전’에도 전용 PNG 아이콘을 넣습니다.\n아이콘·한글 이름·설명은 ZIP에 저장되어 실제 레벨업 카드에 표시됩니다.");
            yield return Import("augment.png");yield return FocusField("name");yield return Hold(5);
            Caption("희귀도는 실버·골드·플래티넘·프리즘을 선택할 수 있습니다.\n이 시연의 새 증강은 골드로 설정합니다.");
            for(int i=0;i<5;i++)yield return ClickField("tier");
            yield return Edit("Field:weight","100");
            Caption("같은 골드 등급 안에서 자주 보이도록 weight를 100으로 설정합니다.\n등급 자체의 추첨 확률은 바꾸지 않습니다.");yield return Hold(3);
            yield return Edit("Field:amount","0.35");yield return Click("ADD EFFECT");
            Caption("ADD EFFECT로 두 번째 효과를 추가합니다.\n첫 효과는 공격력 +35%, 두 번째는 multi = 탄환 +1입니다.");
            yield return ClickField("effect",1);yield return ClickField("effect",1);yield return Edit("Field:amount","1",1);yield return Click("APPLY");yield return FocusField("effect",1);yield return Hold(5);
            yield return Click("SAVE");
            Chapter("08 저장과 시험 실행","SAVE로 초안을 저장합니다.\nTEST PLAY는 설치 전에 현재 초안만 적용해 시험하는 기능입니다.");yield return Hold(3);
            yield return Click("TEST PLAY");yield return Hold(1);ScopeGame();yield return Click("START MISSION");yield return Click("LAUNCH");yield return Play(12,true);
            game.SetState(GameState.Pause);yield return Click("MAIN MENU");yield return Click("RETURN TO WORKSHOP");yield return Hold(1);
            Caption("제작 씬으로 돌아와 저장된 초안을 다시 불러옵니다.\n내용을 이어서 편집하거나 ZIP으로 내보낼 수 있습니다.");
            yield return Edit("Import path",Workshop().Project.Root);yield return Click("LOAD DRAFT");yield return Hold(3);
            Chapter("09 ZIP 내보내기와 설치","EXPORT ZIP으로 배포용 MOD 패키지를 만듭니다.\nmanifest·콘텐츠 JSON·사용한 PNG가 ZIP에 함께 들어갑니다.");
            yield return Click("EXPORT ZIP");exported=Input("Import path").text;if(!File.Exists(exported))throw new Exception("Export ZIP was not produced");yield return Hold(4);
            File.Copy(exported,Path.Combine(output,"neon-wave-lab.zip"),true);
            yield return Click("GAME MENU");yield return Hold(1);ScopeGame();
            Caption("생성된 ZIP을 MOD 폴더의 Inbox에 복사합니다.\n이 파일 복사 단계는 녹화 도구가 수행합니다.");
            File.Copy(exported,Path.Combine(game.Mods.Root,"Inbox","neon-wave-lab.zip"));yield return Click("MOD MANAGER");yield return Hold(3);
            Caption("INSTALL INBOX ZIPs로 설치한 뒤 ENABLE로 활성화합니다.\n설치만으로는 적용되지 않으며, 활성화 후 새 게임에 반영됩니다.");
            yield return Click("INSTALL INBOX ZIPs");yield return Hold(3);yield return Click("ENABLE");yield return Hold(3);yield return Click("BACK");
            if(!game.Content.Assets.ContainsKey("re01_skin")||game.Content.Weapons["pulse"].trajectory!="wave"||WorkshopPlaytest.Content!=null)throw new Exception("Installed MOD did not activate independently of the draft");
            Chapter("10 설치한 MOD로 플레이","초안 시험 모드를 종료하고 설치된 MOD로 새 게임을 시작합니다.\n캐릭터·배경·몬스터·탄환·처치 이펙트가 함께 변경됩니다.");
            yield return Click("START MISSION");yield return Click("LAUNCH");yield return Play(185,true);
            if(!game.Progress.Ranks.ContainsKey("neon_core"))yield return Play(60,true);
            if(!game.Progress.Ranks.ContainsKey("neon_core"))throw new Exception("New augment did not appear naturally in this take");
            game.SetState(GameState.Pause);
            Chapter("11 제작 결과","직접 만든 MOD의 이미지·파동 탄환·복합 증강을 확인했습니다.\n완성 영상과 자막, 동일한 MOD ZIP을 함께 제공합니다.");yield return Hold(6);
            File.WriteAllText(Path.Combine(output,"evidence.txt"),$"Installed package: neon-wave-lab\nDraft override active: {WorkshopPlaytest.Content!=null}\nKills: {game.Kills}\nLevel: {game.Progress.Level}\nCustom augment rank: {game.Progress.Ranks["neon_core"]}\nWeapon: {game.Stats.Weapon.trajectory}\nCount: {game.Stats.Count}\nDamage: {game.Stats.Damage}\n");
        }
        void ScopeGame()
        {
            game=FindObjectOfType<GameSession>();if(!game)throw new Exception("Game scene missing");game.TestMode=true;
            game.Save=new SaveManager(Path.Combine(output,"settings"));game.Save.Data.width=Width;game.Save.Data.height=Height;game.Save.Data.fullscreen=false;game.Save.Data.shake=false;game.Save.Data.music=.25f;game.Save.Data.effects=.7f;
            game.Mods=new ModManager(Path.Combine(output,"Mods"));game.ReloadContent();game.SetState(GameState.Menu);game.ApplyDisplay();
        }
        IEnumerator Play(float seconds,bool mod)
        {
            cursor.enabled=highlight.enabled=false;int held=0;GameState last=GameState.Boot;
            for(int i=0;i<seconds*Fps;i++)
            {
                if(game.Player)game.Player.ReadInput=false;
                if(game.Running)
                {
                    held=0;float desired=-5.4f+Mathf.Sin(game.Elapsed*.7f)*1.1f;game.Player.InputAxis(Mathf.Clamp((desired-game.Player.Position.x)*2,-1,1));if(game.Player.Position.y< -1.45f)game.Player.Rise();
                    if(i%300==0)Caption(mod?$"{(WorkshopPlaytest.Content!=null?"초안 시험 모드":"설치한 MOD")}에서 파동 탄환이 실제 발사됩니다.\n현재 공격력 {game.Stats.Damage:0.##} · 동시 발사 {game.Stats.Count}발 · 처치 {game.Kills}":"A/D 이동과 Space 상승 입력을 자동 조작합니다.\n실제 하강으로 충전하고 에너지 65 이상에서 반동 공격을 사용합니다.");
                }
                else if(game.State==GameState.LevelUp)
                {
                    int pick=game.Progress.Choices.FindIndex(u=>u.id=="neon_core");if(pick<0)pick=game.Progress.Choices.FindIndex(u=>u.effect=="damage"||u.effect=="multi"||u.effect=="rebound");if(pick<0)pick=0;
                    if(last!=GameState.LevelUp)Caption(game.Progress.Choices[pick].id=="neon_core"?"직접 만든 ‘네온 과충전’이 정상 레벨업 후보로 등장했습니다.\n카드에 표시된 공격력 증가와 탄환 추가 효과를 선택합니다.":"적을 처치해 자연스럽게 레벨업했습니다.\n등급·현재 단계·실제 적용 전후 수치를 확인하고 선택합니다.");
                    if(++held>=90){yield return Click("SELECT  [ "+(pick+1)+" ]");held=0;cursor.enabled=highlight.enabled=false;}
                }
                else if(game.State==GameState.GameOver)
                {
                    if(last!=GameState.GameOver)Caption("피격/추락으로 게임이 끝나면 실제 결과 화면이 표시됩니다.\nHP를 조작하지 않고 Retry로 다시 시작합니다.");
                    if(++held>=90){yield return Click("RETRY");held=0;cursor.enabled=highlight.enabled=false;}
                }
                last=game.State;yield return null;
            }
        }
        IEnumerator Import(string name){yield return Edit("Import path",Path.Combine(artPath,name));yield return Click("IMPORT PNG");}
        ModWorkshop Workshop()=>FindObjectOfType<ModWorkshop>();
        Canvas MainCanvas()=>FindObjectsOfType<Canvas>().FirstOrDefault(c=>c!=overlay&&c.gameObject.activeInHierarchy);
        InputField Input(string name,int index=0)=>MainCanvas().GetComponentsInChildren<InputField>().Where(i=>i.gameObject.name==name).ElementAt(index);
        Button FieldButton(string name,int index=0)=>MainCanvas().GetComponentsInChildren<Button>().Where(b=>b.gameObject.name=="Field:"+name).ElementAt(index);
        IEnumerator FocusField(string name,int index=0){var buttons=MainCanvas().GetComponentsInChildren<Button>().Where(b=>b.gameObject.name=="Field:"+name).ToArray();var r=buttons.Length>index?(RectTransform)buttons[index].transform:(RectTransform)Input("Field:"+name,index).transform;yield return Point(r);}
        IEnumerator ClickField(string name,int index=0){var button=FieldButton(name,index);yield return Point((RectTransform)button.transform);button.onClick.Invoke();yield return Hold(.9f);}
        IEnumerator Click(string label)
        {
            var button=MainCanvas().GetComponentsInChildren<Button>().FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text==label);if(!button)throw new Exception("Missing button: "+label);
            yield return Point((RectTransform)button.transform);button.onClick.Invoke();File.AppendAllText(Path.Combine(output,"actions.tsv"),frame+"\tCLICK\t"+label.Replace('\n',' ')+"\n");yield return Hold(1);
        }
        IEnumerator Edit(string name,string value,int index=0)
        {
            var input=Input(name,index);yield return Point((RectTransform)input.transform);input.ActivateInputField();yield return Hold(.2f);input.text="";
            if(value.Length>35){input.text=value;yield return Hold(.5f);}else foreach(char c in value){input.text+=c;yield return Hold(.055f);}
            input.onEndEdit.Invoke(value);input.DeactivateInputField();File.AppendAllText(Path.Combine(output,"actions.tsv"),frame+"\tEDIT\t"+name+"\t"+value+"\n");yield return Hold(.8f);
        }
        IEnumerator Point(RectTransform rect)
        {
            var scroll=rect.GetComponentInParent<ScrollRect>();
            if(scroll)
            {
                Canvas.ForceUpdateCanvases();float center=-scroll.content.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                float y=Mathf.Clamp(center-scroll.viewport.rect.height*.5f,0,Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height));var from=scroll.content.anchoredPosition;
                for(int i=1;i<=12;i++){scroll.content.anchoredPosition=Vector2.Lerp(from,new Vector2(from.x,y),i/12f);yield return null;}
            }
            Canvas.ForceUpdateCanvases();Vector3[] corners=new Vector3[4];rect.GetWorldCorners(corners);
            Vector2 a=RectTransformUtility.WorldToScreenPoint(null,corners[0]),b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            var position=new Vector2((a.x+b.x)*.5f/Screen.width*Width,(a.y+b.y)*.5f/Screen.height*Height);Vector2 old=cursor.rectTransform.anchoredPosition;
            cursor.enabled=true;highlight.enabled=false;
            for(int i=1;i<=10;i++){cursor.rectTransform.anchoredPosition=Vector2.Lerp(old,position,i/10f);yield return null;}
            highlight.rectTransform.anchoredPosition=position;highlight.rectTransform.sizeDelta=new Vector2((b.x-a.x)/Screen.width*Width+8,(b.y-a.y)/Screen.height*Height+8);highlight.enabled=true;yield return Hold(.35f);
        }
        void CreatePointer()
        {
            overlay=new GameObject("Recording pointer",typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();DontDestroyOnLoad(overlay.gameObject);overlay.renderMode=RenderMode.ScreenSpaceOverlay;overlay.sortingOrder=100;
            var scale=overlay.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(Width,Height);scale.matchWidthOrHeight=.5f;
            highlight=OverlayImage("Target",new Vector2(80,40));highlight.color=new Color(1,.8f,.2f,.07f);var outline=highlight.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.85f,.2f);outline.effectDistance=new Vector2(2,-2);
            cursor=OverlayImage("Pointer",new Vector2(30,30));pointerArt=new ArtFactory();cursor.sprite=pointerArt.Get("ring");cursor.color=new Color(1,.85f,.2f);cursor.enabled=highlight.enabled=false;
        }
        Image OverlayImage(string name,Vector2 size){var image=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(overlay.transform,false);image.rectTransform.anchorMin=image.rectTransform.anchorMax=Vector2.zero;image.rectTransform.pivot=Vector2.one*.5f;image.rectTransform.sizeDelta=size;image.raycastTarget=false;return image;}
        IEnumerator CaptureLoop(){while(recording){yield return null;if(recording)Capture();}}
        void Capture()
        {
            var camera=Camera.main;if(!camera)return;var canvases=FindObjectsOfType<Canvas>().Where(c=>c.isRootCanvas&&c.gameObject.activeInHierarchy).ToArray();var modes=canvases.Select(c=>c.renderMode).ToArray();
            var previous=RenderTexture.active;var rect=camera.rect;camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.aspect=16f/9f;
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply();var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            for(int i=0;i<canvases.Length;i++)canvases[i].renderMode=modes[i];camera.targetTexture=null;camera.rect=rect;camera.ResetAspect();RenderTexture.active=previous;
            int count=AudioRenderer.GetSampleCountForCaptureFrame();if(count>0)using(var data=new NativeArray<float>(count*2,Allocator.Temp)){if(!AudioRenderer.Render(data))throw new Exception("Audio capture failed");foreach(float v in data)audio.Write(v);}
            frame++;if(frame%300==0){File.WriteAllText(Path.Combine(output,"progress.txt"),frame+" frames / "+(frame/Fps)+" sec / "+chapter);File.WriteAllBytes(Path.Combine(output,"latest.png"),pixels.EncodeToPNG());}
            if(frame>20000)throw new Exception("Recording exceeded limit");
        }
        IEnumerator Hold(float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*Fps);i++)yield return null;}
        void Chapter(string name,string text){FlushCue();chapter=name;chapters.Add(((float)frame/Fps).ToString(System.Globalization.CultureInfo.InvariantCulture)+"\t"+name);caption=text;cueStart=frame;}
        void Caption(string text){FlushCue();caption=text;cueStart=frame;}
        void FlushCue(){if(!string.IsNullOrEmpty(caption)&&frame>cueStart)cues.Add((cues.Count+1)+"\n"+Stamp(cueStart)+" --> "+Stamp(frame)+"\n"+chapter+"\n"+caption+"\n");}
        static string Stamp(int f){var t=TimeSpan.FromSeconds((double)f/Fps);return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}";}
        static string Argument(string[] args,string key){int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length)throw new ArgumentException(key);return args[i+1];}
        void Finish()
        {
            if(!recording)return;recording=false;AudioRenderer.Stop();Time.captureFramerate=0;audio?.Dispose();encoder?.StandardInput.Close();
            if(encoder!=null&&(!encoder.WaitForExit(15000)||encoder.ExitCode!=0))File.AppendAllText(Path.Combine(output,"error.txt"),"Encoder failed\n");
            target?.Release();if(pixels)Destroy(pixels);pointerArt?.Dispose();if(overlay)Destroy(overlay.gameObject);
        }
        void Restore(){PlayerPrefs.SetString("Workshop.LastDraft",previousDraft);PlayerPrefs.Save();WorkshopPlaytest.Content=null;WorkshopPlaytest.DraftPath=null;}
        void OnLog(string message,string stack,LogType type){if(!failed&&(type==LogType.Error||type==LogType.Exception||(type==LogType.Warning&&message.StartsWith("Workshop:")))){failed=true;File.AppendAllText(Path.Combine(output,"error.txt"),message+"\n"+stack);Finish();Restore();Application.Quit(1);}}
        void OnDestroy(){Application.logMessageReceived-=OnLog;Finish();Active=false;}
    }
}
