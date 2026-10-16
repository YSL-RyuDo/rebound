using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rebound.Full;
using UnityEngine;

public sealed class WorkshopTests
{
    string root;
    [SetUp]public void Setup(){root=Path.Combine(Path.GetTempPath(),"rebound-workshop-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
    [TearDown]public void Cleanup(){Directory.Delete(root,true);}
    [Test]public void DraftOverrideExportInstallAndReopenRoundTrip()
    {
        var baseline=BuiltinContent.Create();var p=new WorkshopProject(Path.Combine(root,"draft"),baseline);
        var w=(WeaponData)WorkshopProject.Copy(baseline.weapons[0]);w.trajectory="wave";w.amplitude=1.2f;w.frequency=3;
        p.Apply("weapons",w);Assert.AreEqual("straight",baseline.weapons[0].trajectory,"Editing must not mutate shipped data");
        var u=new UpgradeData{id="custom_combo",name="Custom combo",tier="prismatic",effects=new[]{new UpgradeEffect{effect="damage",amount=.5f},new UpgradeEffect{effect="multi",amount=1}}};p.Apply("upgrades",u);
        var reload=new WorkshopProject(p.Root,baseline);Assert.AreEqual(1.2f,reload.Validate().Weapons["pulse"].amplitude);
        string zip=p.Export(Path.Combine(root,"exports"));var mods=new ModManager(Path.Combine(root,"mods"));string id=mods.Install(zip);mods.Scan(new[]{id});var content=mods.Load(baseline);
        Assert.IsEmpty(mods.Errors);Assert.AreEqual("wave",content.Weapons["pulse"].trajectory);Assert.AreEqual(2,content.Upgrades["custom_combo"].effects.Length);
        var imported=WorkshopProject.ImportPackage(zip,Path.Combine(root,"import"),baseline);Assert.AreEqual("prismatic",imported.Validate().Upgrades["custom_combo"].tier);
    }
    [Test]public void SpriteImportAssignExportAndLoad()
    {
        var baseline=BuiltinContent.Create();var p=new WorkshopProject(Path.Combine(root,"draft"),baseline);
        var texture=new Texture2D(8,8);texture.SetPixels(Enumerable.Repeat(Color.cyan,64).ToArray());texture.Apply();string png=Path.Combine(root,"my.png");File.WriteAllBytes(png,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        string id=p.ImportSprite(png,"custom_ship");var f=(FlightData)WorkshopProject.Copy(baseline.flight[0]);f.skin=id;p.Apply("flight",f);
        string zip=p.Export(Path.Combine(root,"exports"));var mods=new ModManager(Path.Combine(root,"mods"));string mod=mods.Install(zip);mods.Scan(new[]{mod});var registry=mods.Load(baseline);
        Assert.AreEqual(id,registry.Flight["re01"].skin);Assert.IsTrue(File.Exists(ModValidator.SafePath(registry.AssetRoots[id],registry.Assets[id].path)));
    }
    [Test]public void InvalidEditsAreTransactionalAndReferencesCannotDangle()
    {
        var p=new WorkshopProject(Path.Combine(root,"draft"),BuiltinContent.Create());
        var w=new WeaponData{id="pulse",name="Bad",trajectory="wave",frequency=float.NaN};Assert.Throws<FormatException>(()=>p.Apply("weapons",w));Assert.IsEmpty(p.Draft.content.weapons);
        var f=new FlightData{id="re01",name="Missing",weapon="unknown"};Assert.Throws<FormatException>(()=>p.Apply("flight",f));Assert.IsEmpty(p.Draft.content.flight);
        var u=new UpgradeData{id="fraction",name="No-op",effect="multi",amount=.1f};Assert.Throws<FormatException>(()=>p.Apply("upgrades",u));
        u.amount=1;u.tier="invalid";Assert.Throws<FormatException>(()=>p.Apply("upgrades",u));
    }
    [Test]public void CompositeEffectsRebuildWithoutCompounding()
    {
        var p=BuiltinContent.Create();var u=new UpgradeData{id="combo",name="Combo",effects=new[]{new UpgradeEffect{effect="damage",amount=.5f},new UpgradeEffect{effect="multi",amount=1}}};
        p.upgrades=p.upgrades.Concat(new[]{u}).ToArray();var r=new ContentRegistry();r.Apply(p,"",true,new System.Collections.Generic.HashSet<string>());
        var stats=new PlayerStats();var ranks=new System.Collections.Generic.Dictionary<string,int>{{"combo",2}};
        stats.Rebuild(p.flight[0],p.weapons[0],ranks,r);Assert.AreEqual(6,stats.Damage);Assert.AreEqual(3,stats.Count);
        stats.Rebuild(p.flight[0],p.weapons[0],ranks,r);Assert.AreEqual(6,stats.Damage);
    }
    [Test]public void TrajectoriesHaveObservableDistinctMotionAndFrameStableWave()
    {
        var w=new WeaponData{trajectory="wave",amplitude=1,frequency=1};var a=new ProjectileMotion(Vector2.zero,Vector2.right*10);var b=a;
        for(int i=0;i<25;i++)a.Step(w,.01f);for(int i=0;i<5;i++)b.Step(w,.05f);
        Assert.AreEqual(2.5f,a.Position.x,.001f);Assert.AreEqual(1,a.Position.y,.001f);Assert.Less(Vector2.Distance(a.Position,b.Position),.001f);
        w.trajectory="arc";w.gravity=6;var arc=new ProjectileMotion(Vector2.zero,Vector2.right*10);for(int i=0;i<50;i++)arc.Step(w,.02f);Assert.Less(arc.Position.y,-2.9f);
        w.trajectory="homing";w.turnRate=90;var home=new ProjectileMotion(Vector2.zero,Vector2.right*10);for(int i=0;i<25;i++)home.Step(w,.02f,new Vector2(8,8));Assert.Greater(home.Position.y,1);
    }
    [Test]public void LegacySchemaStillGetsSafeNewDefaults()
    {
        var pack=JsonUtility.FromJson<ContentPack>("{\"weapons\":[{\"id\":\"legacy\",\"name\":\"Legacy\"}],\"upgrades\":[{\"id\":\"legacy\",\"name\":\"Legacy\",\"effect\":\"damage\",\"amount\":0.2}]}");
        ModValidator.Pack(pack,root);Assert.AreEqual("straight",pack.weapons[0].trajectory);Assert.AreEqual("silver",pack.upgrades[0].tier);
    }
}
