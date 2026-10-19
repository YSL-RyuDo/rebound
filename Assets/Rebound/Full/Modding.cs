using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Rebound.Full
{
    [Serializable] public class ModManifest
    {
        public int schemaVersion; public string modId,name,author,version,targetGameVersion,description,license;
        public string[] content;
    }
    public class InstalledMod { public string Folder, Error; public ModManifest Manifest; public bool Enabled; }
    // A real remote provider can implement this once an API contract and authentication are supplied.
    public interface IModCatalogClient { System.Threading.Tasks.Task<string[]> ListPackages(); System.Threading.Tasks.Task<string> Download(string id); }
    public static class ModValidator
    {
        static readonly Regex Id = new Regex("^[a-z][a-z0-9_-]{0,63}$");
        public static string SafePath(string root,string relative)
        {
            if(string.IsNullOrWhiteSpace(relative) || relative.Contains("\\") || relative.Contains(":") || Path.IsPathRooted(relative)) throw new FormatException("Invalid relative path");
            var parts=relative.Split('/');
            if(parts.Any(p=>p==".." || p=="." || p=="" || p.EndsWith(".") || p.EndsWith(" ") || p.IndexOfAny(Path.GetInvalidFileNameChars())>=0)) throw new FormatException("Unsafe path: "+relative);
            string basePath=Path.GetFullPath(root)+Path.DirectorySeparatorChar;
            string full=Path.GetFullPath(Path.Combine(root,relative));
            if(!full.StartsWith(basePath,StringComparison.OrdinalIgnoreCase)) throw new FormatException("Path escapes package");
            return full;
        }
        public static void Manifest(ModManifest m)
        {
            if(m==null || m.schemaVersion!=1 || !Id.IsMatch(m.modId??"") || (m.targetGameVersion!="1.0.0"&&m.targetGameVersion!="1.1.0"&&m.targetGameVersion!="1.2.0") ||
                string.IsNullOrWhiteSpace(m.name) || string.IsNullOrWhiteSpace(m.author) || string.IsNullOrWhiteSpace(m.description) ||
                string.IsNullOrWhiteSpace(m.license) || !Version.TryParse(m.version,out _) || m.content==null || m.content.Length<1 || m.content.Length>32)
                throw new FormatException("Invalid manifest or incompatible schema/game version");
            if(m.content.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=m.content.Length) throw new FormatException("Duplicate content file");
            foreach(var file in m.content) if(!file.StartsWith("data/") || Path.GetExtension(file)!=".json") throw new FormatException("Content must be data/*.json");
        }
        static void Range(float v,float min,float max) { if(float.IsNaN(v)||float.IsInfinity(v)||v<min||v>max) throw new FormatException("Numeric value outside supported range"); }
        static void Definitions<T>(T[] list) where T:Definition
        {
            if(list==null || list.Length>128) throw new FormatException("Missing or oversized definition list");
            var ids=new HashSet<string>();
            foreach(var d in list) if(d==null || !Id.IsMatch(d.id??"") || !ids.Add(d.id) || string.IsNullOrWhiteSpace(d.name) || d.name.Length>100 || (d.description??"").Length>1000) throw new FormatException("Invalid or duplicate content ID/name");
        }
        public static void Pack(ContentPack p,string root)
        {
            if(p==null) throw new FormatException("Missing content");
            Definitions(p.flight); Definitions(p.weapons); Definitions(p.enemies); Definitions(p.upgrades); Definitions(p.stages); Definitions(p.assets);
            Definitions(p.effects??new EffectData[0]);
            if(p.story==null) throw new FormatException("Missing story array");
            foreach(var f in p.flight) { Range(f.gravity,.5f,20); Range(f.thrust,1,10); Range(f.speed,1,10); Range(f.maxFall,2,12); Range(f.interval,.12f,1); Range(f.charge,1,100); Range(f.hp,1,10); Range(f.rebound,1,100); Range(f.range,1,12); }
            foreach(var w in p.weapons) { Range(w.damage,.1f,100); Range(w.interval,.08f,3); Range(w.speed,3,30); Range(w.size,.05f,.6f); Range(w.crit,0,1); Range(w.count,1,7); Range(w.pierce,0,8); Range(w.spread,0,25); if(w.pattern!="line"&&w.pattern!="fan") throw new FormatException("Unsupported weapon pattern"); }
            foreach(var e in p.enemies) { Range(e.hp,1,1000); Range(e.speed,.2f,8); Range(e.interval,.5f,10); Range(e.armor,0,.85f); Range(e.xp,1,100); Range(e.damage,1,3); if(!new[]{"straight","track","shield","shoot"}.Contains(e.pattern)) throw new FormatException("Unsupported enemy pattern"); }
            foreach(var w in p.weapons)
            {
                if(!new[]{"straight","wave","arc","homing"}.Contains(w.trajectory))throw new FormatException("Unknown trajectory");
                Range(w.amplitude,0,3);Range(w.frequency,0,8);Range(w.gravity,-20,20);Range(w.acceleration,-10,10);
                Range(w.turnRate,0,360);Range(w.launchAngle,-90,90);Range(w.lifetime,.1f,10);
                if(w.motionMode!="parameters"&&w.motionMode!="formula")throw new FormatException("Unknown motion mode");
                if(w.motionMode=="formula")TrajectoryFormula.Validate(w);
            }
            foreach(var u in p.upgrades)
            {
                Range(u.weight,.01f,100);Range(u.maxLevel,1,10);Range(u.minLevel,1,50);
                if(!AugmentRules.Tiers.Contains(u.tier)||(u.effects!=null&&u.effects.Length>8))throw new FormatException("Invalid augment tier/effect count");
                foreach(var e in AugmentRules.Effects(u))
                {
                    if(e==null||!AugmentRules.Codes.Contains(e.effect))throw new FormatException("Unsupported upgrade effect");
                    Range(e.amount,.01f,5);
                    if(new[]{"multi","pierce","chain","guard","repair"}.Contains(e.effect)&&e.amount!=Mathf.Floor(e.amount))throw new FormatException(e.effect+" requires a whole number; fractional ranks can have no effect");
                }
            }
            foreach(var e in p.effects??new EffectData[0])
            {
                Range(e.size,.02f,4);Range(e.lifetime,.05f,3);Range(e.speed,0,12);Range(e.count,1,32);
                if(!ColorUtility.TryParseHtmlString(e.color,out _))throw new FormatException("Effect color must be a valid HTML color");
            }
            foreach(var s in p.stages)
            {
                Range(s.bossTime,30,1200); Range(s.bossHp,100,10000);
                if(s.backgroundMode!="tile"&&s.backgroundMode!="stretch")throw new FormatException("Background mode must be tile or stretch");
                if(s.waves==null || s.waves.Length<1 || s.waves.Length>64) throw new FormatException("Invalid waves");
                foreach(var w in s.waves) { Range(w.start,0,s.bossTime); Range(w.end,w.start+.1f,s.bossTime); Range(w.interval,.25f,10); Range(w.count,1,6); Range(w.difficulty,.5f,5); Range(w.minY,-4,3.3f); Range(w.maxY,w.minY,3.3f); if(w.enemies==null || w.enemies.Length<1 || w.enemies.Length>16) throw new FormatException("Invalid wave enemy list"); }
            }
            foreach(var a in p.assets)
            {
                string path=SafePath(root,a.path);
                if(!File.Exists(path) || !a.path.StartsWith("assets/") || (a.kind!="sprite"&&a.kind!="audio") ||
                    (a.kind=="sprite"&&Path.GetExtension(path)!=".png") || (a.kind=="audio"&&Path.GetExtension(path)!=".wav")) throw new FormatException("Invalid asset");
                var b=File.ReadAllBytes(path);
                if(a.kind=="sprite")
                {
                    if(b.Length<24 || b[0]!=137 || b[1]!=80 || b[2]!=78 || b[3]!=71) throw new FormatException("Invalid PNG");
                    uint width=BigEndian(b,16),height=BigEndian(b,20);
                    if(width<1 || height<1 || width>2048 || height>2048) throw new FormatException("PNG dimensions exceed 2048");
                    var texture=new Texture2D(2,2);
                    bool decoded=texture.LoadImage(b);
                    UnityEngine.Object.DestroyImmediate(texture);
                    if(!decoded) throw new FormatException("PNG decode failed");
                }
                else ModAssetLoader.ValidateWave(b);
            }
        }
        static uint BigEndian(byte[] b,int i) => ((uint)b[i]<<24)|((uint)b[i+1]<<16)|((uint)b[i+2]<<8)|b[i+3];
    }
    public sealed class ModManager
    {
        public readonly string Root;
        public readonly List<InstalledMod> Installed = new List<InstalledMod>();
        public readonly List<string> Errors = new List<string>();
        public ModManager(string root) { Root=root; Directory.CreateDirectory(root); Directory.CreateDirectory(Path.Combine(root,"Inbox")); }
        public static ContentPack ReadPack(string path) => JsonUtility.FromJson<ContentPack>(File.ReadAllText(path));
        public void Scan(IEnumerable<string> enabled)
        {
            Installed.Clear(); Errors.Clear(); var ids=new HashSet<string>();
            foreach(var dir in Directory.GetDirectories(Root).OrderBy(x=>x,StringComparer.Ordinal))
            {
                if(Path.GetFileName(dir)=="Inbox" || Path.GetFileName(dir).StartsWith(".install-")) continue;
                var item=new InstalledMod {Folder=dir};
                try
                {
                    ValidateFolder(dir);
                    item.Manifest=JsonUtility.FromJson<ModManifest>(File.ReadAllText(Path.Combine(dir,"manifest.json")));
                    ModValidator.Manifest(item.Manifest);
                    if(!ids.Add(item.Manifest.modId)) throw new FormatException("Duplicate mod ID");
                    item.Enabled=enabled.Contains(item.Manifest.modId);
                }
                catch(Exception ex) {item.Error=ex.Message; Record(ex.Message);}
                Installed.Add(item);
            }
        }
        static void ValidateFolder(string root)
        {
            int count=0; long total=0;
            var pending=new Stack<string>(); pending.Push(root);
            while(pending.Count>0)
            {
                string dir=pending.Pop();
                if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0) throw new FormatException("Symbolic links are forbidden");
                foreach(var sub in Directory.GetDirectories(dir)) pending.Push(sub);
                foreach(var f in Directory.GetFiles(dir))
                {
                    var info=new FileInfo(f);
                    if((info.Attributes&FileAttributes.ReparsePoint)!=0 || !new[]{".json",".png",".wav"}.Contains(info.Extension.ToLowerInvariant())) throw new FormatException("Forbidden file");
                    if(++count>256 || info.Length>8*1024*1024 || (total+=info.Length)>32*1024*1024) throw new FormatException("Package size limit exceeded");
                }
            }
        }
        public string Install(string zipPath)
        {
            string temp=Path.Combine(Root,".install-"+Guid.NewGuid().ToString("N"));
            try
            {
                if(new FileInfo(zipPath).Length>32*1024*1024) throw new FormatException("ZIP exceeds 32 MB");
                Directory.CreateDirectory(temp);
                using(var zip=ZipFile.OpenRead(zipPath))
                {
                    if(zip.Entries.Count>256) throw new FormatException("Too many ZIP entries");
                    long total=0; var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach(var e in zip.Entries)
                    {
                        string name=e.FullName;
                        if(name.EndsWith("/")) { ModValidator.SafePath(temp,name.TrimEnd('/')); continue; }
                        string path=ModValidator.SafePath(temp,name);
                        if(!names.Add(name) || ((e.ExternalAttributes>>16)&0xF000)==0xA000 || !new[]{".json",".png",".wav"}.Contains(Path.GetExtension(name))) throw new FormatException("Duplicate/forbidden ZIP entry");
                        if(e.Length>8*1024*1024 || (total+=e.Length)>32*1024*1024) throw new FormatException("Expanded ZIP too large");
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using(var input=e.Open()) using(var output=File.Create(path))
                        {
                            byte[] buffer=new byte[8192]; int n; long written=0;
                            while((n=input.Read(buffer,0,buffer.Length))>0) { written+=n; if(written>e.Length || written>8*1024*1024) throw new FormatException("ZIP size mismatch"); output.Write(buffer,0,n); }
                        }
                    }
                }
                ValidateFolder(temp);
                var m=JsonUtility.FromJson<ModManifest>(File.ReadAllText(Path.Combine(temp,"manifest.json"))); ModValidator.Manifest(m);
                foreach(var file in m.content) ModValidator.Pack(ReadPack(ModValidator.SafePath(temp,file)),temp);
                string destination=ModValidator.SafePath(Root,m.modId);
                if(Directory.Exists(destination)) throw new FormatException("Already installed. Remove the existing package folder before replacing it.");
                Directory.Move(temp,destination);
                return m.modId;
            }
            catch(Exception ex) {Record(ex.Message); throw;}
            finally { if(Directory.Exists(temp)) Directory.Delete(temp,true); }
        }
        public ContentRegistry Load(ContentPack baseline)
        {
            ContentRegistry registry=Base(baseline);
            var accepted=new List<InstalledMod>();
            foreach(var mod in Installed.Where(m=>m.Enabled && m.Manifest!=null && m.Error==null).OrderBy(m=>m.Manifest.modId,StringComparer.Ordinal))
            {
                try
                {
                    // Transactional rebuild ensures a bad package cannot partially change active content.
                    var candidate=Base(baseline); var changes=new HashSet<string>();
                    foreach(var m in accepted.Concat(new[]{mod})) foreach(var file in m.Manifest.content)
                    { var p=ReadPack(ModValidator.SafePath(m.Folder,file)); ModValidator.Pack(p,m.Folder); candidate.Apply(p,m.Folder,false,changes); }
                    candidate.CheckReferences(); registry=candidate; accepted.Add(mod);
                }
                catch(Exception ex) {mod.Error=ex.Message; Record(mod.Manifest.modId+": "+ex.Message);}
            }
            return registry;
        }
        static ContentRegistry Base(ContentPack baseline) {var r=new ContentRegistry(); r.Apply(baseline,"",true,new HashSet<string>()); r.CheckReferences(); return r;}
        void Record(string message) {Errors.Add(message); try {File.AppendAllText(Path.Combine(Root,"mod-errors.log"),DateTime.UtcNow.ToString("o")+" "+message+"\n");} catch(IOException) {} }
    }
    public static class ModAssetLoader
    {
        public static Sprite Sprite(string path)
        {
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false); if(!t.LoadImage(File.ReadAllBytes(path))) {UnityEngine.Object.Destroy(t); throw new FormatException("PNG decode failed");}
            return UnityEngine.Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,Mathf.Max(t.width,t.height));
        }
        public static void ValidateWave(byte[] bytes) { ParseWave(bytes,out _,out _,out _,out _); }
        static void ParseWave(byte[] b,out int channels,out int rate,out int start,out int size)
        {
            channels=0;rate=0;start=0;size=0;
            if(b.Length<44 || System.Text.Encoding.ASCII.GetString(b,0,4)!="RIFF" || System.Text.Encoding.ASCII.GetString(b,8,4)!="WAVE") throw new FormatException("Invalid WAV header");
            int bits=0,format=0;
            for(int i=12;i+8<=b.Length;)
            {
                string id=System.Text.Encoding.ASCII.GetString(b,i,4); int len=BitConverter.ToInt32(b,i+4);
                if(len<0 || (long)i+8+len>b.Length) throw new FormatException("Invalid WAV chunk");
                if(id=="fmt " && len>=16) {format=BitConverter.ToUInt16(b,i+8); channels=BitConverter.ToUInt16(b,i+10); rate=BitConverter.ToInt32(b,i+12); bits=BitConverter.ToUInt16(b,i+22);}
                if(id=="data") {start=i+8;size=len;}
                i+=8+len+(len%2);
            }
            if(format!=1 || bits!=16 || channels<1 || channels>2 || rate<8000 || rate>48000 || size<=0 || size%(channels*2)!=0 || size/(rate*channels*2f)>60) throw new FormatException("Use PCM16 WAV, mono/stereo, 8-48kHz, <=60 seconds");
        }
        public static AudioClip Wave(string path)
        {
            byte[] b=File.ReadAllBytes(path); ParseWave(b,out int channels,out int rate,out int start,out int size);
            float[] data=new float[size/2]; for(int i=0;i<data.Length;i++) data[i]=BitConverter.ToInt16(b,start+i*2)/32768f;
            var clip=AudioClip.Create(Path.GetFileName(path),data.Length/channels,channels,rate,false); clip.SetData(data,0); return clip;
        }
    }
}
