using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using Rebound.Full;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class FullGameTests
{
    GameSession game; string isolated;
    [UnitySetUp] public IEnumerator Setup()
    {
        Time.timeScale=1;yield return SceneManager.LoadSceneAsync("REBOUND");
        game=UnityEngine.Object.FindObjectOfType<GameSession>();game.TestMode=true;
        isolated=Path.Combine(Application.temporaryCachePath,"rebound-session-"+Guid.NewGuid().ToString("N"));
        game.Save=new SaveManager(isolated);game.Mods=new ModManager(Path.Combine(isolated,"Mods"));
        game.StartRun();game.Player.ReadInput=false;yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;if(Directory.Exists(isolated))Directory.Delete(isolated,true);yield return null;}
    [UnityTest] public IEnumerator FullMovementEnergyAndDamage()
    {
        game.Player.Teleport(new Vector2(-5,3));
        yield return new WaitForSeconds(1.1f);Assert.GreaterOrEqual(game.Player.Energy,65);
        game.Player.Rise();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        Assert.Less(game.Player.Energy,1);Assert.Greater(game.Player.VelocityY,4);
        int hp=game.Player.Hp;game.Player.Damage(1);game.Player.Damage(1);Assert.AreEqual(hp-1,game.Player.Hp);
        game.Player.Teleport(new Vector2(-5,-6));yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Assert.AreEqual(hp-2,game.Player.Hp);
    }
    [UnityTest] public IEnumerator LevelQueueUniqueChoicesAndStackRebuild()
    {
        game.Progress.AddXp(70);int pending=game.Progress.Pending;Assert.Greater(pending,1);game.Progress.OpenNext();
        Assert.AreEqual(GameState.LevelUp,game.State);Assert.AreEqual(0,Time.timeScale);
        while(game.Progress.Pending>0)
        {
            Assert.AreEqual(3,game.Progress.Choices.Select(c=>c.id).Distinct().Count());game.Progress.Choose(0);
        }
        Assert.AreEqual(GameState.Playing,game.State);Assert.AreEqual(1,Time.timeScale);
        game.Progress.Ranks["damage"]=2;game.RebuildStats();float damage=game.Stats.Damage;game.RebuildStats();Assert.AreEqual(damage,game.Stats.Damage);
        foreach(var u in game.Content.Upgrades.Values)game.Progress.Ranks[u.id]=u.maxLevel;
        game.Progress.AddXp(500);game.Progress.OpenNext();Assert.IsTrue(game.Progress.Choices.All(c=>c.effect.StartsWith("reward")));
        yield return null;
    }
    [UnityTest] public IEnumerator BossPhasesVictoryAndRetry()
    {
        game.Elapsed=240;game.Stage.Tick(0);var boss=game.Stage.Boss;Assert.NotNull(boss);
        boss.Hp=boss.MaxHp*.59f;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Assert.AreEqual(2,boss.Phase);
        boss.Hp=boss.MaxHp*.24f;boss.Age=9;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Assert.AreEqual(3,boss.Phase);Assert.IsTrue(game.Stage.GravityDisturbed);
        game.World.Hit(boss,100000,true);Assert.AreEqual(GameState.Clear,game.State);
        game.StartRun();yield return null;Assert.AreEqual(1,game.Progress.Level);Assert.AreEqual(3,game.Player.Hp);Assert.AreEqual(0,game.Score);
    }
    [UnityTest] public IEnumerator CappedAugmentsNeverOfferAndStaleChoiceCannotSpendAnotherLevel()
    {
        game.Content.Weapons["pulse"].crit=1;game.RebuildStats();
        Assert.IsFalse(game.Progress.CanOffer(game.Content.Upgrades["crit"]));
        game.Content.Flight["re01"].thrust=10;game.Content.Flight["re01"].maxFall=2;game.RebuildStats();
        Assert.IsFalse(game.Progress.CanOffer(game.Content.Upgrades["thrust"]));Assert.IsFalse(game.Progress.CanOffer(game.Content.Upgrades["fall"]));
        game.Progress.Ranks["damage"]=game.Content.Upgrades["damage"].maxLevel;game.RebuildStats();
        Assert.IsFalse(game.Progress.CanOffer(game.Content.Upgrades["damage"]));
        game.Progress.AddXp(200);game.Progress.OpenNext();int generation=game.Progress.Generation,pending=game.Progress.Pending;
        Assert.IsTrue(game.Progress.Choices.All(u=>u.id!="crit"&&u.id!="damage"));
        Assert.IsTrue(game.Progress.Choose(0,generation));Assert.AreEqual(pending-1,game.Progress.Pending);
        Assert.IsFalse(game.Progress.Choose(0,generation));Assert.AreEqual(pending-1,game.Progress.Pending);
        var combo=new UpgradeData{id="reward_0",name="Composite",effect="reward2",effects=new[]{new UpgradeEffect{effect="damage",amount=.5f}}};
        game.Content.Upgrades.Add(combo.id,combo);game.Progress.Choices[0]=combo;float damage=game.Stats.Damage;int score=game.Score;
        Assert.IsTrue(game.Progress.Choose(0,game.Progress.Generation));Assert.Greater(game.Stats.Damage,damage);Assert.AreEqual(score,game.Score,"Composite effect overrides legacy text; MOD IDs cannot impersonate supplies");
        yield return null;
    }
    [UnityTest] public IEnumerator AllCappedSupplyActuallyChangesScore()
    {
        foreach(var u in game.Content.Upgrades.Values)game.Progress.Ranks[u.id]=u.maxLevel;game.RebuildStats();
        game.Player.Heal(100);game.Player.Energy=100;game.Progress.AddXp(20);game.Progress.OpenNext();
        Assert.IsTrue(game.Progress.Choices.All(u=>u.effect=="reward2"));int before=game.Score;
        Assert.IsTrue(game.Progress.Choose(1));Assert.AreEqual(before+100,game.Score);yield return null;
    }
    [UnityTest] public IEnumerator InstalledAugmentCardKeepsCustomIconNameAndDescription()
    {
        var draft=new WorkshopProject(Path.Combine(isolated,"icon-draft"),BuiltinContent.Create());
        var texture=new Texture2D(8,8);texture.SetPixels(Enumerable.Repeat(Color.magenta,64).ToArray());texture.Apply();
        string png=Path.Combine(isolated,"icon.png");File.WriteAllBytes(png,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
        string iconId=draft.ImportSprite(png,"custom_augment_icon");
        var upgrade=new UpgradeData{id="custom_card",name="사용자 증강",description="직접 작성한 설명입니다.\n공격력 +35%",icon=iconId,tier="gold",effect="damage",amount=.35f};
        draft.Apply("upgrades",upgrade);string id=game.Mods.Install(draft.Export(Path.Combine(isolated,"exports")));
        game.Save.Data.enabledMods=new[]{id};game.ReloadContent();game.StartRun();game.Player.ReadInput=false;
        game.Progress.AddXp(12);game.Progress.OpenNext();game.Progress.Choices[0]=game.Content.Upgrades[upgrade.id];game.SetState(GameState.LevelUp);
        yield return null;
        var labels=UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
        Assert.IsTrue(labels.Any(t=>t.text==upgrade.name));
        var details=labels.Single(t=>t.text.StartsWith(upgrade.description));
        Assert.IsTrue(details.text.Contains(game.Progress.Preview(upgrade)),"Description must not replace computed effect values");
        var image=UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>().Single(i=>i.sprite==game.Art.Get(iconId));
        Assert.AreEqual(Color.white,image.color,"Rarity color must not tint an imported icon");Assert.IsTrue(image.preserveAspect);
        var scroll=details.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        Assert.GreaterOrEqual(scroll.content.rect.height,details.preferredHeight);
        float damage=game.Stats.Damage;game.Progress.Choose(0);Assert.Greater(game.Stats.Damage,damage);
    }
    [UnityTest] public IEnumerator WorkshopSceneAndPlaytestRoundTrip()
    {
        string old=PlayerPrefs.GetString("Workshop.LastDraft","");WorkshopPlaytest.DraftPath=Path.Combine(isolated,"draft");
        yield return SceneManager.LoadSceneAsync("ModWorkshop");yield return null;
        var workshop=UnityEngine.Object.FindObjectOfType<ModWorkshop>();Assert.NotNull(workshop);
        var w=new WeaponData{id="pulse",name="Wave test",trajectory="wave",amplitude=1};workshop.Project.Apply("weapons",w);
        WorkshopPlaytest.Content=workshop.Project.Validate();yield return SceneManager.LoadSceneAsync("REBOUND");yield return null;
        game=UnityEngine.Object.FindObjectOfType<GameSession>();game.StartRun();game.Player.ReadInput=false;
        Assert.AreEqual("wave",game.Stats.Weapon.trajectory);Assert.IsTrue(game.TestMode);
        game.World.FirePlayer(Vector2.zero,game.Stats);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        Assert.IsTrue(game.World.Projectiles.Any(p=>p.Active&&p.MotionData!=null&&p.MotionData.trajectory=="wave"&&p.Position.y!=0));
        WorkshopPlaytest.Content=null;WorkshopPlaytest.DraftPath=null;PlayerPrefs.SetString("Workshop.LastDraft",old);
    }
    [UnityTest] public IEnumerator SweptProjectilesDamageEnemiesAndPoolReturns()
    {
        game.Player.enabled=false;var target=game.World.Spawn(game.Content.Enemies["scout"],new Vector2(-2,1));
        game.World.Shoot(new Vector2(-5,1),Vector2.right*30,100,.2f,0,false);
        yield return new WaitForSeconds(.15f);Assert.IsTrue(target.Dead);Assert.GreaterOrEqual(game.Kills,1);
        yield return new WaitForSeconds(.5f);Assert.Less(game.World.ActiveProjectiles,10);
    }
    [UnityTest] public IEnumerator PauseFreezesAndSettingsResumeCorrectly()
    {
        float elapsed=game.Elapsed;game.SetState(GameState.Pause);game.ShowSettings();yield return new WaitForSecondsRealtime(.1f);
        Assert.AreEqual(elapsed,game.Elapsed);game.CloseSettings();Assert.AreEqual(GameState.Pause,game.State);
        game.SetState(GameState.Playing);yield return new WaitForSeconds(.1f);Assert.Greater(game.Elapsed,elapsed);
    }
    [UnityTest] public IEnumerator EnemyPatternsArmorAndReboundDirection()
    {
        game.Player.enabled=false;
        var hunter=game.World.Spawn(game.Content.Enemies["hunter"],new Vector2(4,-2));
        var gunner=game.World.Spawn(game.Content.Enemies["gunner"],new Vector2(6,2));gunner.Cooldown=.02f;
        yield return new WaitForSeconds(.08f);Assert.Greater(hunter.Position.y,-2);Assert.IsTrue(game.World.Projectiles.Any(p=>p.Active&&p.Hostile));
        var shield=game.World.Spawn(game.Content.Enemies["shield"],game.Player.Position+Vector2.right*2);
        float hp=shield.Hp;game.World.Hit(shield,10);Assert.AreEqual(hp-3.5f,shield.Hp,.01f);
        var behind=game.World.Spawn(game.Content.Enemies["scout"],game.Player.Position+Vector2.left*2);
        game.World.Rebound(game.Player.Position,game.Stats);Assert.IsTrue(shield.Dead);Assert.AreEqual(behind.MaxHp,behind.Hp);
    }
    [UnityTest] public IEnumerator FullTimelineAndBoundedProjectilePool()
    {
        game.Player.enabled=false;
        foreach(float stamp in new[]{2f,32,92,152,212})
        {int before=game.World.Enemies.Count;game.Elapsed=stamp;game.Stage.Tick(0);Assert.Greater(game.World.Enemies.Count,before);}
        game.Elapsed=301;game.Stage.Tick(0);Assert.NotNull(game.Stage.Boss);Assert.AreEqual(GameState.Playing,game.State,"Five minutes must not force an ending");
        for(int i=0;i<500;i++)game.World.Shoot(new Vector2(9,4),Vector2.right*30,1,.2f,0,false);
        Assert.AreEqual(384,game.World.ActiveProjectiles);
        yield return new WaitForSeconds(.2f);Assert.Less(game.World.ActiveProjectiles,5,"All stress-test shots must return; an initial player shot may still be alive.");
    }
    [UnityTest] public IEnumerator RetryDoesNotAccumulateAudioSources()
    {
        for(int i=0;i<3;i++){game.StartRun();game.Player.ReadInput=false;yield return null;yield return null;}
        Assert.AreEqual(2,game.GetComponents<AudioSource>().Length);
    }
    [Test] public void RealZipSamplesApplyDisableAndRejectConflict()
    {
        string root=Path.Combine(Application.temporaryCachePath,"rebound-test-"+Guid.NewGuid().ToString("N"));
        try
        {
            var manager=new ModManager(root);
            manager.Install(Path.Combine(Application.streamingAssetsPath,"SampleMods/light-frame.zip"));
            manager.Install(Path.Combine(Application.streamingAssetsPath,"SampleMods/overcharge.zip"));
            manager.Scan(new[]{"light-frame"});var light=manager.Load(BuiltinContent.Create());Assert.AreEqual(4.2f,light.Flight["re01"].gravity);
            manager.Scan(new[]{"overcharge"});var heavy=manager.Load(BuiltinContent.Create());Assert.AreEqual(8.5f,heavy.Flight["re01"].gravity);Assert.IsTrue(heavy.Weapons.ContainsKey("electric"));
            manager.Scan(new[]{"light-frame","overcharge"});var conflict=manager.Load(BuiltinContent.Create());Assert.AreEqual(4.2f,conflict.Flight["re01"].gravity);Assert.IsTrue(manager.Errors.Any(e=>e.Contains("conflict")));
            manager.Scan(new string[0]);var clean=manager.Load(BuiltinContent.Create());Assert.AreEqual(6,clean.Flight["re01"].gravity);Assert.IsFalse(clean.Weapons.ContainsKey("electric"));
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Test] public void MaliciousZipAndInvalidContentAreRejected()
    {
        string root=Path.Combine(Application.temporaryCachePath,"rebound-bad-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            string zip=Path.Combine(root,"bad.zip");using(var a=ZipFile.Open(zip,ZipArchiveMode.Create))using(var w=new StreamWriter(a.CreateEntry("../escape.json").Open()))w.Write("{}");
            var manager=new ModManager(Path.Combine(root,"mods"));Assert.Throws<FormatException>(()=>manager.Install(zip));Assert.IsFalse(File.Exists(Path.Combine(root,"escape.json")));
            Assert.Throws<FormatException>(()=>ModValidator.SafePath(root,"C:/bad.json"));Assert.Throws<FormatException>(()=>ModValidator.SafePath(root,"assets/../../bad.json"));
            var p=BuiltinContent.Create();p.weapons[0].interval=0;Assert.Throws<FormatException>(()=>ModValidator.Pack(p,root));
            p=BuiltinContent.Create();p.enemies[0].pattern="execute-dll";Assert.Throws<FormatException>(()=>ModValidator.Pack(p,root));
            p=BuiltinContent.Create();p.flight[0].gravity=float.NaN;Assert.Throws<FormatException>(()=>ModValidator.Pack(p,root));
        }finally{Directory.Delete(root,true);}
    }
    [Test] public void CorruptSaveRecoversAndValidSaveRoundTrips()
    {
        string root=Path.Combine(Application.temporaryCachePath,"rebound-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root,"settings.json"),"broken");var save=new SaveManager(root);Assert.NotNull(save.Error);Assert.AreEqual("Space",save.Data.rise);
            save.Data.bestScore=123;save.Save();var loaded=new SaveManager(root);Assert.AreEqual(123,loaded.Data.bestScore);
        }finally{Directory.Delete(root,true);}
    }
}
