using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rebound.Full
{
    public static class WorkshopPlaytest
    {
        public static ContentRegistry Content;
        public static string DraftPath;
    }
    [Serializable] public class WorkshopDraft
    {
        public ModManifest manifest=new ModManifest {schemaVersion=1,modId="my-mod",name="My MOD",author="Creator",version="1.0.0",targetGameVersion="1.2.0",description="Made in REBOUND Workshop",license="All rights reserved",content=new[]{"data/content.json"}};
        public ContentPack content=new ContentPack();
    }
    // A draft holds only overrides/additions. The shipped baseline is never edited by this service.
    public sealed class WorkshopProject
    {
        public static readonly string[] Sections={"flight","weapons","enemies","stages","upgrades","effects"};
        public readonly string Root;
        public WorkshopDraft Draft;
        public readonly ContentPack Baseline;
        public WorkshopProject(string root,ContentPack baseline)
        {
            Root=Path.GetFullPath(root);Baseline=baseline;Directory.CreateDirectory(Root);
            string file=Path.Combine(Root,"draft.json");
            Draft=File.Exists(file)?JsonUtility.FromJson<WorkshopDraft>(File.ReadAllText(file)):new WorkshopDraft();
            if(Draft==null||Draft.manifest==null||Draft.content==null)throw new FormatException("Invalid draft.json");
        }
        static FieldInfo Field(string section)
        {
            if(!Sections.Contains(section)&&section!="assets")throw new FormatException("Unknown category");
            return typeof(ContentPack).GetField(section);
        }
        public static Definition[] Items(ContentPack pack,string section)=>((Array)Field(section).GetValue(pack)).Cast<Definition>().ToArray();
        public Definition[] Available(string section)
        {
            var values=Items(Baseline,section).ToDictionary(d=>d.id);
            foreach(var d in Items(Draft.content,section))values[d.id]=d;
            return values.Values.OrderBy(d=>d.id).ToArray();
        }
        public static Definition Copy(Definition d)=>(Definition)JsonUtility.FromJson(JsonUtility.ToJson(d),d.GetType());
        public bool IsChanged(string section,string id)=>Items(Draft.content,section).Any(d=>d.id==id);
        static void Set(ContentPack pack,string section,IEnumerable<Definition> definitions)
        {
            var field=Field(section);var list=definitions.ToArray();var array=Array.CreateInstance(field.FieldType.GetElementType(),list.Length);
            for(int i=0;i<list.Length;i++)array.SetValue(list[i],i);field.SetValue(pack,array);
        }
        public void Apply(string section,Definition definition)
        {
            var candidate=JsonUtility.FromJson<ContentPack>(JsonUtility.ToJson(Draft.content));
            var copy=Copy(definition);copy.replace=Items(Baseline,section).Any(d=>d.id==copy.id);
            Set(candidate,section,Items(candidate,section).Where(d=>d.id!=copy.id).Concat(new[]{copy}));
            Validate(candidate);Draft.content=candidate;
            if(candidate.weapons.Any(w=>w.motionMode=="formula"))Draft.manifest.targetGameVersion="1.2.0";
            Save();
        }
        public void Revert(string section,string id)
        {
            var candidate=JsonUtility.FromJson<ContentPack>(JsonUtility.ToJson(Draft.content));
            Set(candidate,section,Items(candidate,section).Where(d=>d.id!=id));
            Validate(candidate);Draft.content=candidate;Save();
        }
        public ContentRegistry Validate(ContentPack content=null)
        {
            content=content??Draft.content;ModValidator.Manifest(Draft.manifest);ModValidator.Pack(content,Root);
            var assetFiles=content.assets.Select(a=>a.path).Distinct().ToArray();
            long bytes=System.Text.Encoding.UTF8.GetByteCount(JsonUtility.ToJson(content))+System.Text.Encoding.UTF8.GetByteCount(JsonUtility.ToJson(Draft.manifest));
            foreach(var file in assetFiles){long size=new FileInfo(ModValidator.SafePath(Root,file)).Length;if(size>8*1024*1024)throw new FormatException("An asset exceeds 8 MiB");bytes+=size;}
            if(assetFiles.Length+2>256||bytes>32*1024*1024)throw new FormatException("Export exceeds 256 files or 32 MiB");
            var registry=new ContentRegistry();registry.Apply(Baseline,"",true,new HashSet<string>());
            registry.Apply(content,Root,false,new HashSet<string>());registry.CheckReferences();
            // Catch sprite typos while allowing the procedural palette and installed custom sprites.
            var builtin=new HashSet<string>(new[]{"player","lightframe","hunter","shield","gunner","boss","bullet","ring","spark","reactor","upgrade","drone"}.Concat(AugmentRules.Codes));
            Action<string> sprite=id=>{if(string.IsNullOrWhiteSpace(id)||(!builtin.Contains(id)&&(!registry.Assets.TryGetValue(id,out var a)||a.kind!="sprite")))throw new FormatException("Unknown sprite ID: "+id);};
            foreach(var f in registry.Flight.Values)sprite(f.skin);
            foreach(var w in registry.Weapons.Values)sprite(w.sprite);
            foreach(var e in registry.Enemies.Values)sprite(e.sprite);
            foreach(var s in registry.Stages.Values)sprite(s.background);
            foreach(var u in registry.Upgrades.Values)sprite(u.icon);
            foreach(var e in registry.Effects.Values)sprite(e.sprite);
            return registry;
        }
        public string ImportSprite(string source,string id)
        {
            source=source.Trim().Trim('"');
            var info=new FileInfo(source);
            if(!info.Exists||info.Length>8*1024*1024||!info.Extension.Equals(".png",StringComparison.OrdinalIgnoreCase))throw new FormatException("Choose a PNG file <=8 MiB, dimensions <=2048 x 2048");
            string relative="assets/sprites/"+Guid.NewGuid().ToString("N")+".png";
            string destination=ModValidator.SafePath(Root,relative);Directory.CreateDirectory(Path.GetDirectoryName(destination));
            // Validate the source before adding it to the draft. The copy stays inside this generated workspace.
            File.Copy(source,destination);
            var asset=new AssetData {id=id,name=id,kind="sprite",path=relative,replace=Items(Baseline,"assets").Any(a=>a.id==id)};
            ModValidator.Pack(new ContentPack {assets=new[]{asset}},Root);
            Set(Draft.content,"assets",Items(Draft.content,"assets").Where(a=>a.id!=id).Concat(new[]{asset}));Save();return id;
        }
        public void Save()
        {
            string path=Path.Combine(Root,"draft.json"),temp=path+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(Draft,true));
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        public string Export(string exportsRoot)
        {
            Validate();Save();Directory.CreateDirectory(exportsRoot);
            string path=Path.Combine(exportsRoot,Draft.manifest.modId+"-"+Draft.manifest.version+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".zip");
            using(var archive=ZipFile.Open(path,ZipArchiveMode.Create))
            {
                Write(archive,"manifest.json",JsonUtility.ToJson(Draft.manifest,true));
                Write(archive,"data/content.json",JsonUtility.ToJson(Draft.content,true));
                foreach(var asset in Draft.content.assets.GroupBy(a=>a.path).Select(g=>g.First()))archive.CreateEntryFromFile(ModValidator.SafePath(Root,asset.path),asset.path);
            }
            return path;
        }
        static void Write(ZipArchive zip,string name,string text){using(var writer=new StreamWriter(zip.CreateEntry(name).Open()))writer.Write(text);}
        public static WorkshopProject ImportPackage(string zip,string workspace,ContentPack baseline)
        {
            // Use the same safe extraction and limits as the game. Import into a new draft, never an installed MOD.
            var manager=new ModManager(Path.Combine(workspace,"Imported"));string id=manager.Install(zip.Trim().Trim('"'));
            string source=Path.Combine(manager.Root,id);var manifest=JsonUtility.FromJson<ModManifest>(File.ReadAllText(Path.Combine(source,"manifest.json")));
            var project=new WorkshopProject(Path.Combine(workspace,"Drafts",Guid.NewGuid().ToString("N")),baseline);project.Draft.manifest=manifest;
            foreach(var file in manifest.content)
            {
                var pack=ModManager.ReadPack(ModValidator.SafePath(source,file));
                foreach(var section in Sections.Concat(new[]{"assets"}))Set(project.Draft.content,section,Items(project.Draft.content,section).Concat(Items(pack,section)));
            }
            foreach(var asset in project.Draft.content.assets)
            {
                string target=ModValidator.SafePath(project.Root,asset.path);Directory.CreateDirectory(Path.GetDirectoryName(target));
                if(!File.Exists(target))File.Copy(ModValidator.SafePath(source,asset.path),target);
            }
            project.Draft.manifest.content=new[]{"data/content.json"};project.Validate();project.Save();return project;
        }
    }
}
