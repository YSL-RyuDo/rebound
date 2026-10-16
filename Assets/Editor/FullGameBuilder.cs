using System;
using System.IO;
using System.IO.Compression;
using Rebound.Full;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FullGameBuilder
{
    public const string MainScene="Assets/Rebound/Scenes/REBOUND.unity";
    public const string WorkshopScene="Assets/Rebound/Scenes/ModWorkshop.unity";
    [MenuItem("REBOUND/Open MOD Workshop")]
    public static void OpenWorkshop()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!File.Exists(WorkshopScene))Generate();EditorSceneManager.OpenScene(WorkshopScene);
    }
    [MenuItem("REBOUND/Open Full Game")]
    public static void Open()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!File.Exists(MainScene))Generate();else EditorSceneManager.OpenScene(MainScene);
    }
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/Rebound/Resources");
        string dataPath="Assets/Rebound/Resources/base-content.json";
        if(!File.Exists(dataPath))File.WriteAllText(dataPath,JsonUtility.ToJson(BuiltinContent.Create(),true));
        Samples();AssetDatabase.Refresh();
        if(!File.Exists(MainScene))
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(0,0,-10);
            new GameObject("GameBootstrap - full content, menus and world").AddComponent<GameSession>();
            EditorSceneManager.SaveScene(scene,MainScene);
        }
        if(!File.Exists(WorkshopScene))
        {
            var workshop=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            new GameObject("MOD Workshop").AddComponent<ModWorkshop>();EditorSceneManager.SaveScene(workshop,WorkshopScene);
        }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MainScene,true),new EditorBuildSettingsScene(WorkshopScene,true),new EditorBuildSettingsScene("Assets/Rebound/Scenes/Game.unity",true)};
        PlayerSettings.productName="REBOUND";PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.bundleVersion="1.1.0";PlayerSettings.runInBackground=true;
        AssetDatabase.SaveAssets();Debug.Log("FULL GAME GENERATED");
    }
    static void Samples()
    {
        Directory.CreateDirectory("Assets/StreamingAssets/SampleMods");
        string baseDir="SampleMods";Directory.CreateDirectory(baseDir);
        var light=new ContentPack {flight=new[]{new FlightData {id="re01",name="LIGHT FRAME",replace=true,gravity=4.2f,thrust=5.2f,speed=6,maxFall=5,charge=23,skin="light"}},
            upgrades=new[]{new UpgradeData {id="light_vector",name="Feather vector",description="+6% thrust and a lighter flight profile",effect="thrust",amount=.06f,maxLevel=3,weight=2}},
            assets=new[]{new AssetData{id="light",name="Light chassis",kind="sprite",path="assets/sprites/light.png"}}};
        var heavy=new ContentPack {flight=new[]{new FlightData {id="re01",name="OVERCHARGE",replace=true,gravity=8.5f,thrust=5.8f,maxFall=7,charge=34,rebound=24,weapon="electric"}},
            weapons=new[]{new WeaponData {id="electric",name="Arc emitter",damage=3.5f,interval=.27f,sprite="electric",crit=.1f}},
            upgrades=new[]{new UpgradeData {id="electric_relay",name="Overvoltage relay",description="Rebound chains to an additional enemy",effect="chain",amount=1,maxLevel=3,weight=2}},
            assets=new[]{new AssetData {id="electric",name="Electric bolt",kind="sprite",path="assets/sprites/electric.png"}}};
        Package(baseDir,"light-frame","LIGHT FRAME",light,"light","lightframe");Package(baseDir,"overcharge","OVERCHARGE",heavy,"electric","spark");
    }
    static void Package(string root,string id,string name,ContentPack pack,string assetId,string shape)
    {
        string dir=Path.Combine(root,id);Directory.CreateDirectory(Path.Combine(dir,"data"));Directory.CreateDirectory(Path.Combine(dir,"assets/sprites"));
        File.WriteAllText(Path.Combine(dir,"manifest.json"),JsonUtility.ToJson(new ModManifest {schemaVersion=1,modId=id,name=name,author="REBOUND project",version="1.0.0",targetGameVersion="1.0.0",description=name=="LIGHT FRAME"?"Low gravity, nimble thrust, custom chassis and flight upgrade.":"Heavy gravity, rapid charge, powerful rebound and electric weapon.",license="CC0-1.0",content=new[]{"data/content.json"}},true));
        File.WriteAllText(Path.Combine(dir,"data/content.json"),JsonUtility.ToJson(pack,true));
        var art=new ArtFactory();Sprite s=art.Get(shape);File.WriteAllBytes(Path.Combine(dir,"assets/sprites/"+assetId+".png"),s.texture.EncodeToPNG());
        // Editor batch mode does not support deferred destruction; these small authoring textures are released at process exit.
        string zip=Path.Combine("Assets/StreamingAssets/SampleMods",id+".zip");
        bool exists=File.Exists(zip);
        using(var archive=ZipFile.Open(zip,exists?ZipArchiveMode.Update:ZipArchiveMode.Create))
        {
            foreach(var file in Directory.GetFiles(dir,"*",SearchOption.AllDirectories))
            {
                string relative=file.Substring(dir.Length+1).Replace('\\','/');if(exists)archive.GetEntry(relative)?.Delete();archive.CreateEntryFromFile(file,relative);
            }
        }
    }
    [MenuItem("REBOUND/Build Full Windows Game")]
    public static void Build()
    {
        Generate();Directory.CreateDirectory("Builds/FullWindows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{MainScene,WorkshopScene},locationPathName="Builds/FullWindows/REBOUND.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Full build failed: "+report.summary.result);
        Debug.Log("FULL WINDOWS BUILD SUCCEEDED");
    }
}
