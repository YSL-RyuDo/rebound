using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Rebound.Full
{
    public sealed class ModWorkshop : MonoBehaviour
    {
        public WorkshopProject Project {get;private set;}
        public const string SceneName="ModWorkshop";
        Canvas canvas;RectTransform root;Font font;ArtFactory art;ContentRegistry registry;
        string section="weapons",message="Select an item, edit values, then APPLY. Draft files are separate from installed MODs.",path="";
        Definition selected;bool isNew,previewRejected;Text status;Image moving;
        readonly List<Action> readFields=new List<Action>();
        readonly List<Image> previewEffects=new List<Image>();
        readonly Color ink=new Color(.025f,.045f,.08f),panel=new Color(.045f,.085f,.13f),accent=new Color(.25f,.9f,.87f),white=new Color(.88f,.93f,.98f);
        string Workspace=>Path.Combine(Application.persistentDataPath,"Workshop");
        ContentPack baseline;
        void Awake()
        {
            Time.timeScale=1;WorkshopPlaytest.Content=null;
            var text=Resources.Load<TextAsset>("base-content");baseline=text?JsonUtility.FromJson<ContentPack>(text.text):BuiltinContent.Create();
            font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},18);
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvas=new GameObject("Workshop Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);
            if(!FindObjectOfType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            string resume=WorkshopPlaytest.DraftPath??PlayerPrefs.GetString("Workshop.LastDraft","");
            string draftPath=!string.IsNullOrEmpty(WorkshopPlaytest.DraftPath)?WorkshopPlaytest.DraftPath:Directory.Exists(resume)?resume:NewPath();
            try {Project=new WorkshopProject(draftPath,baseline);RefreshArt();}
            catch(Exception ex){Project=new WorkshopProject(NewPath(),baseline);message="Could not load last draft: "+ex.Message;RefreshArt();}
            Remember();SelectFirst();Render();
        }
        string NewPath()=>Path.Combine(Workspace,"Drafts",Guid.NewGuid().ToString("N"));
        void Remember(){WorkshopPlaytest.DraftPath=Project.Root;PlayerPrefs.SetString("Workshop.LastDraft",Project.Root);PlayerPrefs.Save();}
        void SelectFirst(){selected=WorkshopProject.Copy(Project.Available(section).First());isNew=false;}
        void RefreshArt(){registry=Project.Validate();art?.Dispose();art=new ArtFactory();art.LoadMods(registry);}
        public void ApplySelected(){ReadFields();Project.Apply(section,selected);isNew=false;RefreshArt();message="Applied to draft. Preview and TEST PLAY now use these values.";Render();}
        void ReadFields(){foreach(var read in readFields)read();}
        void Guard(Action action){try{action();}catch(Exception ex){message=ex.Message;if(status)status.text=message;Debug.LogWarning("Workshop: "+ex.Message);}}
        RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        Image Box(Transform parent,float x,float y,float w,float h,Color color)
        {var i=Rect(parent,"Panel",x,y,w,h).gameObject.AddComponent<Image>();i.color=color;return i;}
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size=18,Color? color=null)
        {var t=Rect(parent,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=color??white;t.supportRichText=false;t.raycastTarget=false;return t;}
        Button Button(Transform parent,string text,float x,float y,float w,float h,Action action,Color? color=null)
        {
            var i=Box(parent,x,y,w,h,color??new Color(.09f,.21f,.28f));var b=i.gameObject.AddComponent<Button>();var t=Label(i.transform,text,8,4,w-16,h-8,16);t.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(()=>Guard(action));return b;
        }
        InputField InputBox(Transform parent,string value,float x,float y,float w,float h,Action<string> set,bool collect=true)
        {
            var i=Box(parent,x,y,w,h,new Color(.018f,.033f,.055f));var input=i.gameObject.AddComponent<InputField>();
            var t=Label(i.transform,value,8,4,w-16,h-8,16);t.supportRichText=false;input.textComponent=t;input.text=value??"";input.characterLimit=1000;
            input.onEndEdit.AddListener(v=>Guard(()=>set(v)));if(collect)readFields.Add(()=>set(input.text));return input;
        }
        RectTransform Scroll(Transform parent,float x,float y,float w,float h,float contentHeight)
        {
            var box=Box(parent,x,y,w,h,panel);box.gameObject.AddComponent<RectMask2D>();var scroll=box.gameObject.AddComponent<ScrollRect>();
            var content=Rect(box.transform,"Scroll content",0,0,w-12,contentHeight);scroll.viewport=box.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            return content;
        }
        void Render()
        {
            if(root){root.gameObject.SetActive(false);Destroy(root.gameObject);}readFields.Clear();previewEffects.Clear();moving=null;previewRejected=false;
            root=Rect(canvas.transform,"Workshop",0,0,1600,900);root.anchorMin=root.anchorMax=root.pivot=Vector2.one*.5f;root.anchoredPosition=Vector2.zero;
            Box(root,0,0,1600,900,ink);
            Label(root,"RE:BOUND / MOD WORKSHOP",24,20,620,45,30,accent);
            Button(root,"NEW DRAFT",690,22,135,40,()=>{Project.Save();Project=new WorkshopProject(NewPath(),baseline);Remember();RefreshArt();SelectFirst();Render();});
            Button(root,"SAVE",835,22,110,40,()=>{ReadFields();Project.Apply(section,selected);Project.Save();message="Saved: "+Project.Root;Render();});
            Button(root,"EXPORT ZIP",955,22,150,40,()=>{ApplySelected();path=Project.Export(Path.Combine(Workspace,"Exports"));message="Exported: "+path;Render();});
            Button(root,"TEST PLAY",1115,22,150,40,()=>{ApplySelected();WorkshopPlaytest.Content=Project.Validate();Remember();SceneManager.LoadScene("REBOUND");});
            Button(root,"GAME MENU",1275,22,145,40,()=>{Project.Save();WorkshopPlaytest.Content=null;SceneManager.LoadScene("REBOUND");});
            Button(root,"FOLDER",1430,22,145,40,()=>Application.OpenURL(new Uri(Workspace).AbsoluteUri));
            var m=Project.Draft.manifest;
            Meta("MOD ID",m.modId,24,230,v=>m.modId=v);Meta("NAME",m.name,270,300,v=>m.name=v);Meta("AUTHOR",m.author,586,250,v=>m.author=v);Meta("VERSION",m.version,852,150,v=>m.version=v);Meta("LICENSE",m.license,1018,250,v=>m.license=v);
            Label(root,"DESCRIPTION",1284,82,290,24,14,accent);InputBox(root,m.description,1284,107,290,38,v=>m.description=v).gameObject.name="Meta:DESCRIPTION";
            Label(root,"1. SELECT",24,170,410,30,18,accent);Label(root,"2. EDIT / APPLY",455,170,560,30,18,accent);Label(root,"3. PREVIEW / TEST",1040,170,535,30,18,accent);
            int tab=0;
            foreach(var s in WorkshopProject.Sections)
            {
                string category=s;Button(root,s.ToUpperInvariant(),24,215+tab++*54,155,44,()=>{section=category;SelectFirst();Render();},s==section?new Color(.15f,.4f,.43f):(Color?)null);
            }
            Label(root,"* = in draft\nMouse wheel: scroll\n\nAPPLY validates.\nTEST uses this draft.\n\nOriginal files are kept.",24,558,155,160,15);
            var records=Project.Available(section);var list=Scroll(root,195,210,240,515,Mathf.Max(515,records.Length*62));int row=0;
            foreach(var record in records)
            {
                var item=record;Button(list,(Project.IsChanged(section,item.id)?"* ":"")+item.name+"\n"+item.id,4,row++*62,220,56,()=>{selected=WorkshopProject.Copy(item);isNew=false;Render();});
            }
            Button(root,"CLONE / NEW",455,210,170,38,()=>{ReadFields();selected=WorkshopProject.Copy(selected);selected.id="custom_"+Guid.NewGuid().ToString("N").Substring(0,8);selected.name="New "+section;selected.replace=false;isNew=true;Render();});
            Button(root,"REVERT / REMOVE",635,210,195,38,()=>{Project.Revert(section,selected.id);RefreshArt();SelectFirst();Render();});
            Button(root,"APPLY",840,210,175,38,ApplySelected,new Color(.12f,.4f,.36f));
            float formTop=262;
            if(selected is WeaponData editedWeapon)
            {
                Button(root,"A · 수치 방식 / PARAMETERS",455,262,275,36,()=>{ReadFields();editedWeapon.motionMode="parameters";Render();},editedWeapon.motionMode=="parameters"?new Color(.12f,.4f,.36f):(Color?)null);
                Button(root,"B · 수식 방식 / FORMULA",740,262,275,36,()=>{ReadFields();editedWeapon.motionMode="formula";Render();},editedWeapon.motionMode=="formula"?new Color(.12f,.4f,.36f):(Color?)null);
                formTop=310;
                if(editedWeapon.motionMode=="formula")
                {
                    Button(root,"직선 예제",455,307,180,32,()=>FormulaPreset("straight"));
                    Button(root,"파동 예제",645,307,180,32,()=>FormulaPreset("wave"));
                    Button(root,"포물선 예제",835,307,180,32,()=>FormulaPreset("arc"));formTop=350;
                }
            }
            var form=Scroll(root,455,formTop,560,725-formTop,1800);float fy=8;
            BuildFields(form,selected,ref fy);
            if(selected is UpgradeData upgrade)
            {
                var effects=upgrade.effects??new UpgradeEffect[0];
                Label(form,"COMBINED EFFECTS (up to 8)",12,fy,510,30,18,accent);fy+=34;
                for(int i=0;i<effects.Length;i++)
                {
                    int n=i;Label(form,"Effect "+(i+1),12,fy,300,30,16);Button(form,"REMOVE",380,fy,140,30,()=>{ReadFields();upgrade.effects=upgrade.effects.Where((e,j)=>j!=n).ToArray();Render();});fy+=34;
                    BuildFields(form,effects[i],ref fy);
                }
                Button(form,"ADD EFFECT",12,fy,250,34,()=>{ReadFields();var e=AugmentRules.Effects(upgrade).Select(a=>new UpgradeEffect{effect=a.effect,amount=a.amount}).ToList();if(e.Count>=8)throw new FormatException("Maximum 8 effects");e.Add(new UpgradeEffect());upgrade.effects=e.ToArray();Render();});fy+=44;
                Label(form,"Combined effects replace the single effect.\nAmounts: ratios for damage/haste/etc.; whole numbers\nfor multi, pierce, chain, guard, repair. Crit: 0.1 = 10%.",12,fy,510,90,15);fy+=100;
            }
            if(selected is StageData stage)
            {
                var waves=stage.waves??new WaveData[0];
                for(int i=0;i<waves.Length;i++)
                {
                    int n=i;Label(form,"WAVE "+(i+1),12,fy,300,30,18,accent);Button(form,"REMOVE",380,fy,140,30,()=>{ReadFields();stage.waves=stage.waves.Where((w,j)=>j!=n).ToArray();Render();});fy+=36;BuildFields(form,waves[i],ref fy);
                }
                Button(form,"ADD WAVE",12,fy,250,34,()=>{ReadFields();stage.waves=(stage.waves??new WaveData[0]).Concat(new[]{new WaveData{start=0,end=30,enemies=new[]{"scout"}}}).ToArray();Render();});fy+=44;
            }
            form.sizeDelta=new Vector2(form.sizeDelta.x,Mathf.Max(725-formTop,fy+12));
            Preview();
            Label(root,"PNG / ZIP / draft folder path (paste from Explorer)",24,744,900,26,16,accent);
            InputBox(root,path,24,775,985,40,v=>path=v,false).gameObject.name="Import path";
            Button(root,"IMPORT PNG",1024,775,175,40,ImportPng);
            Button(root,"IMPORT ZIP",1210,775,175,40,()=>{Project=WorkshopProject.ImportPackage(path,Path.Combine(Workspace,Guid.NewGuid().ToString("N")),baseline);Remember();RefreshArt();SelectFirst();message="ZIP imported into a separate editable draft.";Render();});
            Button(root,"LOAD DRAFT",1395,775,180,40,()=>{string folder=path.Trim().Trim('"');if(!File.Exists(Path.Combine(folder,"draft.json")))throw new FormatException("Select a folder containing draft.json");var candidate=new WorkshopProject(folder,baseline);candidate.Validate();Project=candidate;Remember();RefreshArt();SelectFirst();Render();});
            status=Label(root,message,24,833,1550,34,14,accent);
            Label(root,"Draft: "+Project.Root,24,873,1550,22,12);
        }
        void Meta(string label,string value,float x,float width,Action<string> set){Label(root,label,x,82,width,24,14,accent);InputBox(root,value,x,107,width,38,set).gameObject.name="Meta:"+label;}
        void FormulaPreset(string kind)
        {
            ReadFields();var weapon=(WeaponData)selected;weapon.formulaX="v*t";
            weapon.formulaY=kind=="wave"?"a*sin(2*pi*f*t)":kind=="arc"?"-0.5*g*t^2":"0";
            if(kind=="arc"&&weapon.gravity==0)weapon.gravity=6;
            message="예제 수식을 불러왔습니다. x(t), y(t)를 편집하고 APPLY를 누르세요.";Render();
        }
        void BuildFields(Transform form,object obj,ref float y)
        {
            foreach(var field in obj.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance).OrderBy(f=>f.Name=="id"?0:f.Name=="name"?1:f.Name=="description"?2:f.Name=="formulaX"?3:f.Name=="formulaY"?4:5))
            {
                if(field.Name=="replace"||field.Name=="effects"||field.Name=="waves")continue;
                if(obj is WeaponData w&&(field.Name=="motionMode"||
                    (w.motionMode!="formula"&&(field.Name=="formulaX"||field.Name=="formulaY"))||
                    (w.motionMode=="formula"&&(field.Name=="trajectory"||field.Name=="turnRate"))))continue;
                if(obj is UpgradeData u&&u.effects!=null&&u.effects.Length>0&&(field.Name=="effect"||field.Name=="amount"))continue;
                string name=field.Name;object current=field.GetValue(obj);
                Label(form,FieldLabel(name),12,y+5,190,30,16);
                if(name=="id"&&!isNew){Label(form,current?.ToString(),210,y+5,315,30,15,accent);y+=40;continue;}
                var choices=Options(obj,name);
                if(choices!=null)
                {
                    string value=current?.ToString();Button(form,value+"  >",210,y,315,34,()=>{ReadFields();int n=Array.IndexOf(choices,value);field.SetValue(obj,choices[(n+1)%choices.Length]);Render();}).gameObject.name="Field:"+name;
                }
                else
                {
                bool expression=name=="formulaX"||name=="formulaY";
                if(expression)y+=36;
                var input=InputBox(form,current is string[] array?string.Join(",",array):Convert.ToString(current,CultureInfo.InvariantCulture),expression?12:210,y,expression?518:315,34,value=>
                {
                    if(field.FieldType==typeof(float)){if(!float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var f)||float.IsNaN(f)||float.IsInfinity(f))throw new FormatException(name+": enter a finite number using a decimal point");field.SetValue(obj,f);}
                    else if(field.FieldType==typeof(int)){if(!int.TryParse(value,out var i))throw new FormatException(name+": enter a whole number");field.SetValue(obj,i);}
                    else if(field.FieldType==typeof(string[]))field.SetValue(obj,value.Split(',').Select(v=>v.Trim()).Where(v=>v.Length>0).ToArray());
                    else field.SetValue(obj,value);
                });input.gameObject.name="Field:"+name;
                if(expression)input.characterLimit=256;
                }
                y+=40;
            }
        }
        static string FieldLabel(string name)
        {
            if(name=="formulaX")return "x(t) · 전방 변위";if(name=="formulaY")return "y(t) · 수직 변위";
            switch(name){case "amplitude":return "Wave amplitude (0–3)";case "frequency":return "Wave Hz (0–8)";case "gravity":return "Gravity (units/s²)";case "acceleration":return "Acceleration (±10)";case "turnRate":return "Turn rate (°/s, 0–360)";case "launchAngle":return "Launch angle (±90°)";case "lifetime":return "Lifetime (seconds)";case "tier":return "Rarity tier";case "maxLevel":return "Max rank (1–10)";case "minLevel":return "Unlock level (1–50)";case "enemies":return "Enemy IDs (comma)";default:return name;}
        }
        static string[] Options(object obj,string field)
        {
            if(field=="tier")return AugmentRules.Tiers;if(field=="effect")return AugmentRules.Codes;
            if(field=="trajectory")return new[]{"straight","wave","arc","homing"};
            if(field=="backgroundMode")return new[]{"tile","stretch"};
            if(field=="pattern")return obj is WeaponData?new[]{"line","fan"}:new[]{"straight","track","shield","shoot"};return null;
        }
        void ImportPng()
        {
            ReadFields();string field=selected is FlightData?"skin":selected is StageData?"background":selected is UpgradeData?"icon":"sprite";
            string id=selected.id+"_"+field;Project.ImportSprite(path,id);selected.GetType().GetField(field).SetValue(selected,id);
            if(selected is StageData stage)stage.backgroundMode="stretch";
            Project.Apply(section,selected);RefreshArt();message="PNG imported and assigned to "+selected.id+" / "+field;Render();
        }
        void Preview()
        {
            var p=Box(root,1040,210,535,360,new Color(.015f,.03f,.05f));p.gameObject.AddComponent<RectMask2D>();
            string bg=selected is StageData s?s.background:registry.Stages["powerplant"].background;
            var background=Box(p.transform,0,0,535,360,Color.white);background.sprite=art.Get(bg);background.color=registry.Assets.ContainsKey(bg)?Color.white:new Color(.08f,.2f,.25f);
            var flight=selected as FlightData??registry.Flight["re01"];
            var player=Box(p.transform,35,160,65,65,Color.white);player.sprite=art.Get(flight.skin);player.preserveAspect=true;
            var enemy=selected as EnemyData??registry.Enemies["scout"];
            var target=Box(p.transform,420,155,65,65,Color.white);target.sprite=art.Get(enemy.sprite);target.preserveAspect=true;
            var weapon=PreviewWeapon();
            var motion=new ProjectileMotion(Vector2.zero,Quaternion.Euler(0,0,weapon.launchAngle)*Vector2.right*weapon.speed);
            for(int i=0;i<60;i++)
            {
                Vector2 point=motion.Step(weapon,.025f,new Vector2(12,1));if(motion.Invalid)break;
                var dot=Box(p.transform,90+point.x*27,190-point.y*27,3,3,new Color(.3f,.75f,.8f,.5f));dot.raycastTarget=false;
            }
            moving=Box(p.transform,90,190,Mathf.Max(8,weapon.size*80),Mathf.Max(5,weapon.size*40),Color.white);moving.sprite=art.Get(weapon.sprite);moving.raycastTarget=false;
            if(selected is EffectData effect)
            {
                ColorUtility.TryParseHtmlString(effect.color,out var color);
                for(int i=0;i<effect.count;i++){var fx=Box(p.transform,260,175,effect.size*65,effect.size*65,color);fx.sprite=art.Get(effect.sprite);fx.rectTransform.pivot=Vector2.one*.5f;previewEffects.Add(fx);}
            }
            if(selected is UpgradeData upgrade)
            {
                var icon=Box(p.transform,210,80,100,100,art.SpriteColor(upgrade.icon,AugmentRules.TierColor(upgrade.tier)));icon.sprite=art.Get(upgrade.icon);icon.preserveAspect=true;
                Label(p.transform,upgrade.tier.ToUpperInvariant()+" / "+upgrade.name,24,252,490,45,20,AugmentRules.TierColor(upgrade.tier));
                Label(p.transform,upgrade.description,24,297,490,60,16);
            }
            string help="Preview: 27 px = 1 world unit; target at (12, 1).\nAPPLY refreshes artwork. TEST PLAY uses this draft.\nReturn through Main Menu > Workshop.\n\nPNG import assigns the selected item's image.\nNew enemies: add their ID to a stage wave.\nNew weapons: set a flight's weapon ID.";
            if(selected is WeaponData)
            {
                Label(p.transform,weapon.motionMode=="formula"?"B · 수식 방식  /  x(t), y(t)":"A · 수치 방식  /  "+weapon.trajectory,14,12,500,30,18,accent);
                if(weapon.motionMode=="formula")
                {
                    help="t: 발사 후 시간(초), v: 발사 속도\na: 진폭, f: 주파수(Hz), g: 중력, acc: 가속도\nx: 발사 방향, y: 발사 방향 기준 위쪽 변위\n+ - * / ^  ·  sin cos abs sqrt min max clamp  ·  pi e\nx(0)=y(0)=0, 삼각함수는 라디안. APPLY로 검증.\n수식에 쓴 변수만 적용됩니다. 오류 발생 탄환은 회수합니다.";
                    try{TrajectoryFormula.Validate(weapon);}catch(FormatException ex){help=ex.Message+"\n수식을 수정하고 APPLY를 누르세요.";moving.enabled=false;previewRejected=true;}
                }
            }
            Label(root,help,1040,585,535,145,15);
        }
        WeaponData PreviewWeapon()=>selected as WeaponData??(selected is FlightData f&&registry.Weapons.TryGetValue(f.weapon,out var w)?w:registry.Weapons["pulse"]);
        void Update()
        {
            if(!canvas)return;canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=(float)Screen.width/Mathf.Max(1,Screen.height)>=16f/9f?1:0;
            if(!moving||previewRejected)return;var weapon=PreviewWeapon();float age=Mathf.Repeat(Time.unscaledTime,Mathf.Min(weapon.lifetime,1.5f));
            var motion=new ProjectileMotion(Vector2.zero,Quaternion.Euler(0,0,weapon.launchAngle)*Vector2.right*weapon.speed);
            while(age>0){float dt=Mathf.Min(age,.02f);motion.Step(weapon,dt,new Vector2(12,1));age-=dt;}
            moving.enabled=!motion.Invalid;
            moving.rectTransform.anchoredPosition=new Vector2(90+motion.Position.x*27,-190+motion.Position.y*27);
            if(selected is EffectData effect&&effect.lifetime>0)
            {
                float phase=Mathf.Repeat(Time.unscaledTime,effect.lifetime+.3f),ratio=Mathf.Clamp01(phase/effect.lifetime);ColorUtility.TryParseHtmlString(effect.color,out var color);color.a*=1-ratio;
                for(int i=0;i<previewEffects.Count;i++)
                {
                    var fx=previewEffects[i];float angle=i*Mathf.PI*2/previewEffects.Count;
                    fx.rectTransform.anchoredPosition=new Vector2(260+Mathf.Cos(angle)*effect.speed*phase*27,-175+Mathf.Sin(angle)*effect.speed*phase*27);
                    fx.rectTransform.sizeDelta=Vector2.one*effect.size*65*Mathf.Exp(1.8f*Mathf.Min(phase,effect.lifetime));fx.color=color;
                }
            }
        }
        void OnDestroy(){art?.Dispose();if(canvas)Destroy(canvas.gameObject);}
    }
}
