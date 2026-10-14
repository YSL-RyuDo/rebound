using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rebound.Full
{
    [Serializable] public class Definition { public string id, name, description; public bool replace; }
    [Serializable] public class FlightData : Definition
    {
        public float gravity = 6, thrust = 4.8f, speed = 5, maxFall = 6, interval = .25f, charge = 20, rebound = 14, range = 7;
        public int hp = 3; public string skin = "player", weapon = "pulse";
    }
    [Serializable] public class WeaponData : Definition
    {
        public float damage = 3, interval = .25f, speed = 17, size = .18f, crit = .05f, spread = 8;
        public int count = 1, pierce; public string pattern = "line", sprite = "bullet";
    }
    [Serializable] public class EnemyData : Definition
    {
        public float hp = 9, speed = 2, interval = 3, armor;
        public int xp = 5, damage = 1; public string pattern = "straight", sprite = "drone";
    }
    [Serializable] public class UpgradeData : Definition
    {
        public string effect, icon = "upgrade"; public int maxLevel = 5, minLevel = 1;
        public float amount = 1, weight = 1;
    }
    [Serializable] public class WaveData
    {
        public float start, end, interval = 1.2f, difficulty = 1, minY = -3.8f, maxY = 3.2f;
        public int count = 1; public string[] enemies;
    }
    [Serializable] public class StageData : Definition
    {
        public float bossTime = 240, bossHp = 1400; public string background = "reactor"; public WaveData[] waves;
    }
    [Serializable] public class StoryData { public string id, text; }
    [Serializable] public class AssetData : Definition { public string path, kind; }
    [Serializable] public class ContentPack
    {
        public FlightData[] flight = new FlightData[0]; public WeaponData[] weapons = new WeaponData[0];
        public EnemyData[] enemies = new EnemyData[0]; public UpgradeData[] upgrades = new UpgradeData[0];
        public StageData[] stages = new StageData[0]; public AssetData[] assets = new AssetData[0];
        public StoryData[] story = new StoryData[0];
    }
    public static class BuiltinContent
    {
        public static ContentPack Create()
        {
            var p = new ContentPack
            {
                flight = new[] { new FlightData { id="re01", name="RE-01" } },
                weapons = new[] { new WeaponData { id="pulse", name="Pulse emitter" } },
                enemies = new[] {
                    new EnemyData {id="scout", name="Scout", sprite="drone"},
                    new EnemyData {id="hunter", name="Hunter", hp=14, speed=2.8f, xp=8, pattern="track", sprite="hunter"},
                    new EnemyData {id="shield", name="Bulwark", hp=24, speed=1.4f, xp=13, armor=.65f, pattern="shield", sprite="shield"},
                    new EnemyData {id="gunner", name="Artillery", hp=18, speed=1.2f, xp=12, pattern="shoot", sprite="gunner"}
                },
                stages = new[] {new StageData {id="powerplant", name="The broken sky", waves=new[] {
                    Wave(0,30,1.5f,1,1,"scout"), Wave(30,90,1.2f,1,1.1f,"scout","hunter"),
                    Wave(90,150,1f,2,1.2f,"scout","hunter","shield"),
                    Wave(150,210,.9f,2,1.35f,"hunter","shield","gunner"),
                    Wave(210,240,.8f,2,1.5f,"scout","shield","gunner") } } },
                story = new[] {
                    new StoryData {id="intro", text="GRAVITY GRID FAILURE\n\nThe floating city is falling. Its defense machines reject all access.\nRE-01, enter the powerplant and shut down the GRAVITY WARDEN.\n\nYour engine recovers energy as you fall. Turn descent into a weapon."},
                    new StoryData {id="outer", text="01 / OUTER GRID  —  Recover energy. Keep flying."},
                    new StoryData {id="security", text="02 / SECURITY BREACH  —  Tracking drones inbound."},
                    new StoryData {id="anomaly", text="03 / GRAVITY ANOMALY  —  Rebound breaks armor."},
                    new StoryData {id="core", text="04 / CENTRAL SHAFT  —  Artillery signals detected."},
                    new StoryData {id="warning", text="05 / CORE ACCESS  —  WARDEN activation imminent."},
                    new StoryData {id="boss", text="GRAVITY WARDEN\nUNAUTHORIZED MASS DETECTED"},
                    new StoryData {id="ending", text="WARDEN OFFLINE\nGravity grid restored. The city holds.\nRE-01: recovery mission complete."}
                }
            };
            p.upgrades = new[] {
                U("damage","Amplifier","+25% base weapon damage","damage",.25f,5),
                U("haste","Rapid cycling","+18% firing rate","haste",.18f,5),
                U("multi","Split emitter","+1 projectile per volley","multi",1,3),
                U("pierce","Phase needle","+1 target penetration","pierce",1,3),
                U("crit","Precision core","+10% critical chance; critical hits deal double damage","crit",.1f,5),
                U("range","Resonance field","+20% rebound reach","range",.2f,4),
                U("rebound","Impact capacitor","+35% rebound damage","rebound",.35f,5),
                U("chain","Arc relay","Rebound arcs to one extra nearby target per rank","chain",1,3),
                U("charge","Energy recycler","+25% fall energy efficiency","charge",.25f,4),
                U("thrust","Vector booster","+8% rise thrust","thrust",.08f,3),
                U("fall","Air brakes","Reduce maximum fall speed by 10%","fall",.1f,3),
                U("guard","Aegis cell","Gain a shield charge; one charge regenerates every 20s","guard",1,3),
                U("repair","Repair kit","Restore 1 HP and increase maximum HP by 1","repair",1,3)
            };
            return p;
        }
        static UpgradeData U(string id,string name,string desc,string effect,float amount,int max) => new UpgradeData {id=id,name=name,description=desc,effect=effect,icon=id,amount=amount,maxLevel=max};
        static WaveData Wave(float start,float end,float interval,int count,float difficulty,params string[] enemies) => new WaveData {start=start,end=end,interval=interval,count=count,difficulty=difficulty,enemies=enemies};
    }
    public sealed class ContentRegistry
    {
        public readonly Dictionary<string,FlightData> Flight = new Dictionary<string,FlightData>();
        public readonly Dictionary<string,WeaponData> Weapons = new Dictionary<string,WeaponData>();
        public readonly Dictionary<string,EnemyData> Enemies = new Dictionary<string,EnemyData>();
        public readonly Dictionary<string,UpgradeData> Upgrades = new Dictionary<string,UpgradeData>();
        public readonly Dictionary<string,StageData> Stages = new Dictionary<string,StageData>();
        public readonly Dictionary<string,AssetData> Assets = new Dictionary<string,AssetData>();
        public readonly Dictionary<string,string> AssetRoots = new Dictionary<string,string>();
        public readonly Dictionary<string,string> Story = new Dictionary<string,string>();
        public void Apply(ContentPack p, string root, bool baseline, HashSet<string> changed)
        {
            Add(Flight,p.flight,baseline,changed); Add(Weapons,p.weapons,baseline,changed); Add(Enemies,p.enemies,baseline,changed);
            Add(Upgrades,p.upgrades,baseline,changed); Add(Stages,p.stages,baseline,changed); Add(Assets,p.assets,baseline,changed);
            foreach(var a in p.assets) AssetRoots[a.id]=root;
            foreach(var s in p.story) { if (!baseline) throw new FormatException("Story overrides are not supported in schema 1."); Story[s.id]=s.text; }
        }
        static void Add<T>(Dictionary<string,T> target,T[] list,bool baseline,HashSet<string> changed) where T:Definition
        {
            foreach(var item in list)
            {
                string key=typeof(T).Name+":"+item.id;
                if (!baseline && (!changed.Add(key) || target.ContainsKey(item.id)!=item.replace))
                    throw new FormatException("Content conflict / invalid replace flag: "+key);
                target[item.id]=item;
            }
        }
        public string Line(string id) => Story.TryGetValue(id,out var line) ? line : id;
        public void CheckReferences()
        {
            if (!Flight.ContainsKey("re01") || !Stages.ContainsKey("powerplant")) throw new FormatException("Required defaults missing");
            foreach(var f in Flight.Values) if (!Weapons.ContainsKey(f.weapon)) throw new FormatException("Unknown weapon: "+f.weapon);
            foreach(var s in Stages.Values) foreach(var w in s.waves) foreach(var id in w.enemies)
                if(!Enemies.ContainsKey(id)) throw new FormatException("Unknown enemy: "+id);
        }
    }
}
