using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Rebound.Full;

public sealed class ModSecurityTests
{
    string root;
    [SetUp]public void Setup(){root=Path.Combine(Path.GetTempPath(),"rebound-security-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
    [TearDown]public void Cleanup(){Directory.Delete(root,true);}
    string Zip(Action<ZipArchive> fill){string path=Path.Combine(root,"test.zip");using(var zip=ZipFile.Open(path,ZipArchiveMode.Create))fill(zip);return path;}
    [Test]public void RejectExecutableAndDuplicateCasePaths()
    {
        string zip=Zip(a=>a.CreateEntry("evil.dll"));var mods=new ModManager(Path.Combine(root,"mods"));Assert.Throws<FormatException>(()=>mods.Install(zip));
        File.Delete(zip);zip=Zip(a=>{a.CreateEntry("data/a.json");a.CreateEntry("data/A.json");});Assert.Throws<FormatException>(()=>mods.Install(zip));
    }
    [Test]public void RejectSymlinkEntry()
    {
        string zip=Zip(a=>{var e=a.CreateEntry("data/link.json");e.ExternalAttributes=unchecked((int)0xA1FF0000);});
        Assert.Throws<FormatException>(()=>new ModManager(Path.Combine(root,"mods")).Install(zip));
    }
    [Test]public void RejectZipBombAndExcessiveEntryCount()
    {
        string zip=Zip(a=>{using(var s=a.CreateEntry("data/huge.json").Open()){byte[] chunk=new byte[1024*1024];for(int i=0;i<9;i++)s.Write(chunk,0,chunk.Length);}});
        var mods=new ModManager(Path.Combine(root,"mods"));Assert.Throws<FormatException>(()=>mods.Install(zip));
        File.Delete(zip);zip=Zip(a=>{for(int i=0;i<257;i++)a.CreateEntry("data/a"+i+".json");});Assert.Throws<FormatException>(()=>mods.Install(zip));
    }
    [Test]public void RejectUnknownVersionAndDuplicateContentIds()
    {
        Assert.Throws<FormatException>(()=>ModValidator.Manifest(new ModManifest {schemaVersion=99}));
        var p=BuiltinContent.Create();p.weapons=new[]{p.weapons[0],p.weapons[0]};Assert.Throws<FormatException>(()=>ModValidator.Pack(p,root));
    }
    [Test]public void SweptCollisionFindsFastBulletCrossing()
    {
        Assert.AreEqual(0,CombatWorld.SegmentDistance(UnityEngine.Vector2.zero,new UnityEngine.Vector2(-10,0),new UnityEngine.Vector2(10,0)),.001f);
        Assert.AreEqual(3,CombatWorld.SegmentDistance(new UnityEngine.Vector2(0,3),new UnityEngine.Vector2(-10,0),new UnityEngine.Vector2(10,0)),.001f);
    }
}
