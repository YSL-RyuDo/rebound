using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Rebound.Full
{
    public sealed class GameUI : MonoBehaviour
    {
        GameSession game;Canvas canvas;RectTransform root;Font font;
        Text hp,time,level,energyText,notice,bossText,upgrades;Image energy,xp,bossBar,bossTrack;Action<KeyCode> binding;
        readonly Color ink=new Color(.035f,.065f,.11f,.97f),line=new Color(.1f,.25f,.33f),white=new Color(.85f,.95f,1);
        public bool Rebinding=>binding!=null;
        public void Initialize(GameSession session)
        {
            game=session;font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},20);
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvas=new GameObject("REBOUND Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            if(!UnityEngine.Object.FindObjectOfType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x*1.5f,-y*1.5f);r.sizeDelta=new Vector2(w*1.5f,h*1.5f);return r;
        }
        Image Box(Transform parent,float x,float y,float w,float h,Color c)
        {var image=Rect("Panel",parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=c;return image;}
        Text Label(Transform parent,string value,float x,float y,float w,float h,int size=20,Color? color=null)
        {
            var t=Rect("Text",parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=Mathf.RoundToInt(size*1.5f);t.color=color??white;t.supportRichText=false;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        Button Button(Transform parent,string value,float x,float y,float w,float h,Action action)
        {
            var image=Box(parent,x,y,w,h,line);var button=image.gameObject.AddComponent<Button>();
            var colors=button.colors;colors.highlightedColor=new Color(.4f,.85f,.95f);colors.pressedColor=new Color(.25f,.5f,.65f);button.colors=colors;
            var text=Label(image.transform,value,12,8,w-24,h-12,20);text.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=>{game.Audio.Play("button",.3f);action();});return button;
        }
        public void Render()
        {
            if(!canvas)return;if(root){root.gameObject.SetActive(false);Destroy(root.gameObject);}binding=null;
            root=Rect("Screen",canvas.transform,0,0,1280,720);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            hp=time=level=energyText=notice=bossText=upgrades=null;energy=xp=bossBar=null;
            if(game.State==GameState.Playing||game.State==GameState.Pause||game.State==GameState.LevelUp)Hud();
            if(game.State==GameState.Playing)return;
            Box(root,0,0,1280,720,new Color(.015f,.03f,.06f,game.State==GameState.LevelUp||game.State==GameState.Pause?.9f:.95f));
            switch(game.State)
            {
                case GameState.Menu:Menu();break;
                case GameState.Story:Header("EMERGENCY DISPATCH","RE-01 / RECOVERY UNIT");Label(root,game.Content.Line("intro"),160,200,960,250,25);Label(root,Controls(),160,480,960,70,21,game.Art.Cyan);Button(root,"LAUNCH",830,600,270,60,game.StartRun);Back(()=>game.SetState(GameState.Menu));break;
                case GameState.Pause:Header("FLIGHT SUSPENDED","PAUSE");Label(root,game.Progress.Summary(),150,220,980,220,23);Button(root,"RESUME",450,440,380,55,()=>game.SetState(GameState.Playing));Button(root,"SETTINGS",450,505,380,55,game.ShowSettings);Button(root,"MAIN MENU",450,570,380,55,game.MainMenu);break;
                case GameState.LevelUp:LevelUp();break;
                case GameState.GameOver:case GameState.Clear:Result();break;
                case GameState.Settings:Settings();break;
                case GameState.Mods:ModScreen();break;
                case GameState.Credits:Header("CREDITS","AN ORIGINAL SMALL GAME");Label(root,"RE:BOUND\nDesign & implementation: project owner + Codex\n\nOriginal procedural spacecraft, reactor panels and combat effects.\nOriginal synthesized sound effects and four procedural music loops.\nNo commercial game resources used.\n\nEngine: Unity 2022.3 LTS / Unity UI / Unity Test Framework\nSee Docs/RESOURCES.md for resource and license details.",140,190,1000,360,23);Back(()=>game.SetState(GameState.Menu));break;
                default:Header("RE:BOUND","INITIALIZING GRAVITY GRID");break;
            }
        }
        void Header(string title,string subtitle){Label(root,subtitle,80,50,1100,40,18,game.Art.Cyan);Label(root,title,80,96,1120,80,48);Box(root,80,175,1120,2,line);}
        void Back(Action action){Button(root,"BACK",80,630,190,50,action);}
        string Controls()=>game.Save.Data.left+" / "+game.Save.Data.right+"  MOVE     "+game.Save.Data.rise+"  RISE     ESC  PAUSE\nFALL TO CHARGE  /  65+ ENERGY + RISE = REBOUND";
        void Menu()
        {
            Label(root,"GRAVITY RECOVERY / 01",100,75,1000,40,20,game.Art.Cyan);Label(root,"RE:BOUND",95,125,1000,130,86);
            Label(root,"FALL. CHARGE. STRIKE BACK.",105,265,900,50,25);
            Button(root,"START MISSION",100,365,380,60,()=>game.SetState(GameState.Story));
            Button(root,"MOD MANAGER",100,438,380,52,()=>{game.Mods.Scan(game.Save.Data.enabledMods);game.SetState(GameState.Mods);});
            Button(root,"SETTINGS",100,500,180,52,game.ShowSettings);Button(root,"CREDITS",295,500,185,52,()=>game.SetState(GameState.Credits));
            Button(root,"QUIT",100,565,380,52,()=>Application.Quit());
            Button(root,"FRAME: "+game.Content.Flight[game.FlightId].name,650,365,510,60,()=>{var ids=game.Content.Flight.Keys.OrderBy(s=>s).ToArray();game.FlightId=ids[(Array.IndexOf(ids,game.FlightId)+1)%ids.Length];Render();});
            Button(root,"STAGE: "+game.Content.Stages[game.StageId].name,650,438,510,60,()=>{var ids=game.Content.Stages.Keys.OrderBy(s=>s).ToArray();game.StageId=ids[(Array.IndexOf(ids,game.StageId)+1)%ids.Length];Render();});
            Button(root,WorkshopPlaytest.Content!=null?"RETURN TO WORKSHOP":"MOD WORKSHOP",650,510,510,52,game.OpenWorkshop);
            Label(root,$"BEST SCORE {game.Save.Data.bestScore}  /  LONGEST FLIGHT {game.Save.Data.bestTime:0}s",650,585,510,60,18,game.Art.Cyan);
            Label(root,"v1.1.0  /  MOD Workshop + Augment tiers  /  Original art & audio",100,665,1100,28,16);
        }
        Image Bar(float x,float y,float w,float h,Color color){Box(root,x,y,w,h,line);return Box(root,x,y,0,h,color);}
        void Hud()
        {
            Box(root,0,0,1280,86,ink);hp=Label(root,"",22,15,230,32,23);level=Label(root,"",270,10,220,30,18);
            xp=Bar(270,48,220,10,game.Art.Cyan);energyText=Label(root,"",530,10,340,30,18);energy=Bar(530,48,300,15,game.Art.Gold);Box(root,725,44,2,24,white);
            time=Label(root,"",980,12,290,55,20);upgrades=Label(root,"",24,91,1200,30,14,new Color(.65f,.78f,.85f));
            bossText=Label(root,"",370,115,620,30,20,game.Art.Red);bossTrack=Box(root,370,148,540,8,line);bossBar=Box(root,370,148,0,8,game.Art.Red);
            notice=Label(root,"",270,180,820,85,23,game.Art.Gold);notice.alignment=TextAnchor.MiddleCenter;
            Label(root,Controls().Replace("\n","    "),22,685,1240,30,13,new Color(.6f,.8f,.9f));
        }
        void LevelUp()
        {
            Header("UPGRADE AVAILABLE","LEVEL "+game.Progress.Level+" / SELECT ONE");
            for(int i=0;i<3;i++)
            {
                int index=i;int generation=game.Progress.Generation;var u=game.Progress.Choices[i];float x=80+i*385;
                Color tier=AugmentRules.TierColor(u.tier);
                Box(root,x,220,350,350,new Color(.055f,.11f,.17f));
                Box(root,x,220,350,4,tier);
                var icon=Box(root,x+25,245,48,48,tier);icon.sprite=game.Art.Get(u.icon);
                Label(root,u.tier.ToUpperInvariant(),x+90,257,240,35,18,tier);
                Label(root,u.name,x+24,307,305,65,25,tier);
                int rank=game.Progress.Ranks.TryGetValue(u.id,out int r)?r:0;
                var preview=Box(root,x+24,375,305,145,new Color(0,0,0,0));preview.gameObject.AddComponent<RectMask2D>();
                var scroll=preview.gameObject.AddComponent<ScrollRect>();string changes=game.Progress.Preview(u);
                var content=Rect("Effect changes",preview.transform,0,0,305,Mathf.Max(145,changes.Split('\n').Length*25));
                scroll.viewport=preview.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=25;
                Label(content,changes,0,0,305,Mathf.Max(145,changes.Split('\n').Length*25),17);
                Label(root,Progression.IsSupply(u)?"SUPPLY":$"RANK {rank} → {rank+1} / {u.maxLevel}",x+24,530,305,32,18,tier);
                Button(root,"SELECT  [ "+(i+1)+" ]",x,585,350,58,()=>game.Progress.Choose(index,generation));
            }
        }
        void Result()
        {
            bool clear=game.State==GameState.Clear;Header(clear?"GRID RESTORED":"SIGNAL LOST",clear?"MISSION COMPLETE":"RE-01 OFFLINE");
            Label(root,clear?game.Content.Line("ending"):"The grid is still unstable. Rebuild, adapt, return.",90,190,1100,90,25,clear?game.Art.Cyan:game.Art.Red);
            Label(root,$"TIME  {game.Elapsed:0.0}s       KILLS  {game.Kills}       LEVEL  {game.Progress.Level}       SCORE  {game.Score}\nWEAPON  {game.Stats.Weapon.name}",90,300,1100,100,23);
            Label(root,game.Progress.Summary(),90,420,1100,150,20);
            Button(root,"RETRY",700,625,220,60,game.StartRun);Button(root,"MAIN MENU",950,625,240,60,game.MainMenu);
        }
        void Settings()
        {
            Header("SETTINGS","SAVED LOCALLY");
            Slider("MASTER",210,game.Save.Data.master,v=>game.Save.Data.master=v);Slider("MUSIC",275,game.Save.Data.music,v=>game.Save.Data.music=v);Slider("EFFECTS",340,game.Save.Data.effects,v=>game.Save.Data.effects=v);
            Button(root,"MUTE: "+game.Save.Data.mute,80,420,310,50,()=>{game.Save.Data.mute=!game.Save.Data.mute;Render();});
            Button(root,"SHAKE: "+game.Save.Data.shake,410,420,310,50,()=>{game.Save.Data.shake=!game.Save.Data.shake;Render();});
            Button(root,game.Save.Data.fullscreen?"FULLSCREEN":"WINDOWED",80,485,310,50,()=>{game.Save.Data.fullscreen=!game.Save.Data.fullscreen;Render();});
            Button(root,$"{game.Save.Data.width} x {game.Save.Data.height}",410,485,310,50,()=>{int[] widths={1280,1600,1920,2560};int index=Array.IndexOf(widths,game.Save.Data.width);game.Save.Data.width=widths[(index+1)%widths.Length];game.Save.Data.height=game.Save.Data.width*9/16;Render();});
            Rebind("LEFT",game.Save.Data.left,800,220,key=>game.Save.Data.left=key);Rebind("RIGHT",game.Save.Data.right,800,300,key=>game.Save.Data.right=key);Rebind("RISE",game.Save.Data.rise,800,380,key=>game.Save.Data.rise=key);
            Label(root,"Select a control, then press a keyboard key.\nESC cancels. Duplicate keys are rejected.",800,480,390,100,18);
            Back(game.CloseSettings);Label(root,game.Save.Error??"Changes are applied when leaving this screen.",300,642,850,50,17);
        }
        void Rebind(string name,string current,float x,float y,Action<string> set)
        {
            Button button=null;button=Button(root,name+": "+current,x,y,380,55,()=>
            {
                button.GetComponentInChildren<Text>().text="PRESS KEY...";
                binding=key=>{string k=key.ToString();if(k!=current&&(k==game.Save.Data.left||k==game.Save.Data.right||k==game.Save.Data.rise))return;set(k);binding=null;Render();};
            });
        }
        void Slider(string name,float y,float value,Action<float> changed)
        {
            Label(root,name,80,y,190,40,21);var bg=Box(root,280,y,440,30,line);var slider=bg.gameObject.AddComponent<Slider>();
            var handle=Box(bg.transform,0,0,18,30,game.Art.Cyan);slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
        void ModScreen()
        {
            Header("MOD MANAGER","ZIP PACKAGES / CONTENT CHANGES APPLY TO THE NEXT RUN");
            Button(root,"OPEN MOD FOLDER",80,195,270,45,()=>Application.OpenURL(new Uri(game.Mods.Root).AbsoluteUri));
            Button(root,"INSTALL INBOX ZIPs",365,195,290,45,()=>Install(false));
            Button(root,"INSTALL SAMPLES",670,195,265,45,()=>Install(true));
            Button(root,"REFRESH",950,195,250,45,()=>{game.ReloadContent();Render();});
            var viewport=Box(root,80,260,1120,295,new Color(.025f,.055f,.085f));viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();var content=Rect("Mods",viewport.transform,0,0,1120,Mathf.Max(295,game.Mods.Installed.Count*125));
            scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            int row=0;
            foreach(var mod in game.Mods.Installed)
            {
                var m=mod;float y=row++*125;
                Label(content,m.Manifest==null?Path.GetFileName(m.Folder):m.Manifest.name+"  v"+m.Manifest.version+"  /  "+m.Manifest.author,15,y+6,780,35,21,game.Art.Cyan);
                Label(content,m.Error??m.Manifest?.description??"Invalid package",15,y+45,780,70,17,m.Error!=null?game.Art.Red:white);
                if(m.Manifest!=null)Button(content,m.Enabled?"DISABLE":"ENABLE",840,y+30,245,55,()=>
                {
                    var list=game.Save.Data.enabledMods.ToList();if(m.Enabled)list.Remove(m.Manifest.modId);else if(!list.Contains(m.Manifest.modId))list.Add(m.Manifest.modId);
                    game.Save.Data.enabledMods=list.OrderBy(s=>s,StringComparer.Ordinal).ToArray();game.Save.Save();game.ReloadContent();Render();
                });
            }
            if(row==0)Label(content,"No installed packages. Install the samples, or put a ZIP in Mods/Inbox.",15,25,1050,90,23);
            Label(root,game.Notice,80,565,1110,60,16,game.Art.Gold);
            Back(()=>{game.ReloadContent();game.SetState(GameState.Menu);});
            Label(root,"Packages are applied by modId order. Conflicting packages are rejected.\nDisable all packages to restore the base game.",310,637,880,60,17);
        }
        void Install(bool samples)
        {
            string dir=samples?Path.Combine(Application.streamingAssetsPath,"SampleMods"):Path.Combine(game.Mods.Root,"Inbox");int success=0;var errors=new System.Collections.Generic.List<string>();
            if(Directory.Exists(dir)) foreach(var file in Directory.GetFiles(dir,"*.zip"))try{game.Mods.Install(file);success++;}catch(Exception ex){errors.Add(Path.GetFileName(file)+": "+ex.Message);}
            game.ReloadContent();game.Notice=$"Installed {success} package(s). "+string.Join(" / ",errors);Render();
        }
        void Update()
        {
            // Match the limiting dimension so the complete 16:9 UI fits ultrawide and tall windows.
            if(canvas)canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=(float)Screen.width/Mathf.Max(1,Screen.height)>=16f/9f?1:0;
            if(binding!=null&&Input.anyKeyDown)
            {
                if(Input.GetKeyDown(KeyCode.Escape)){binding=null;Render();return;}
                foreach(KeyCode key in Enum.GetValues(typeof(KeyCode)))if(SaveManager.ValidKey(key.ToString())&&Input.GetKeyDown(key)){binding(key);break;}
            }
            if(game.State==GameState.LevelUp){if(Input.GetKeyDown(KeyCode.Alpha1))game.Progress.Choose(0);else if(Input.GetKeyDown(KeyCode.Alpha2))game.Progress.Choose(1);else if(Input.GetKeyDown(KeyCode.Alpha3))game.Progress.Choose(2);}
            if(!hp||!game.Player)return;
            hp.text=$"HP {game.Player.Hp}/{game.Stats.MaxHp}  SH {game.Player.Shields}";
            level.text=$"LV {game.Progress.Level}   XP {game.Progress.Xp}/{game.Progress.Required}";
            xp.rectTransform.sizeDelta=new Vector2(330*Mathf.Clamp01((float)game.Progress.Xp/game.Progress.Required),15);
            energyText.text=$"FALL ENERGY {game.Player.Energy:0} / 100"+(game.Player.Energy>=65?"   READY":"");
            energy.color=game.Player.Energy>=65?game.Art.Gold:game.Art.Cyan;energy.rectTransform.sizeDelta=new Vector2(450*game.Player.Energy/100,22.5f);
            time.text=$"{(int)game.Elapsed/60:00}:{(int)game.Elapsed%60:00}   SCORE {game.Score}\nKILLS {game.Kills}";
            upgrades.text=game.Progress.Summary();notice.text=Time.unscaledTime<game.NoticeUntil?game.Notice:game.Stage.GravityDisturbed?"GRAVITY DISTURBANCE — THRUST EARLY":"";
            var boss=game.Stage.Boss;bossText.text=boss!=null&&!boss.Dead?$"GRAVITY WARDEN / PHASE {boss.Phase}":"";
            bossTrack.enabled=boss!=null&&!boss.Dead;
            bossBar.rectTransform.sizeDelta=new Vector2(boss!=null&&!boss.Dead?810*Mathf.Clamp01(boss.Hp/boss.MaxHp):0,12);
        }
        void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
    }
}
