using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rebound.Full
{
    public sealed class PlayerStats
    {
        public float Damage,Interval,BulletSpeed,BulletSize,Crit,Gravity,Thrust,Speed,MaxFall,RiseInterval,Charge,Rebound,Range;
        public int Count,Pierce,Chains,MaxHp,MaxShield; public WeaponData Weapon;
        public void Rebuild(FlightData f,WeaponData w,Dictionary<string,int> ranks,ContentRegistry content)
        {
            Weapon=w;Damage=w.damage;Interval=w.interval;BulletSpeed=w.speed;BulletSize=w.size;Crit=w.crit;Count=w.count;Pierce=w.pierce;
            Gravity=f.gravity;Thrust=f.thrust;Speed=f.speed;MaxFall=f.maxFall;RiseInterval=f.interval;Charge=f.charge;Rebound=f.rebound;Range=f.range;MaxHp=f.hp;MaxShield=0;Chains=0;
            float haste=0;
            foreach(var pair in ranks)
            {
                var u=content.Upgrades[pair.Key];
                foreach(var effect in AugmentRules.Effects(u))
                {
                float amount=effect.amount*pair.Value;
                switch(effect.effect)
                {
                    case "damage":Damage+=w.damage*amount;break;
                    case "haste":haste+=amount;break;
                    case "multi":Count+=(int)amount;break;
                    case "pierce":Pierce+=(int)amount;break;
                    case "crit":Crit+=amount;break;
                    case "range":Range+=f.range*amount;break;
                    case "rebound":Rebound+=f.rebound*amount;break;
                    case "chain":Chains+=(int)amount;break;
                    case "charge":Charge+=f.charge*amount;break;
                    case "thrust":Thrust+=f.thrust*amount;break;
                    case "fall":MaxFall-=f.maxFall*amount;break;
                    case "guard":MaxShield+=(int)amount;break;
                    case "repair":MaxHp+=(int)amount;break;
                }
                }
            }
            Interval=Mathf.Max(.06f,w.interval/(1+haste));Count=Mathf.Clamp(Count,1,12);Pierce=Mathf.Clamp(Pierce,0,16);Crit=Mathf.Clamp01(Crit);
            Thrust=Mathf.Clamp(Thrust,1,10);MaxFall=Mathf.Max(2,MaxFall);Range=Mathf.Clamp(Range,1,13);MaxHp=Mathf.Clamp(MaxHp,1,20);MaxShield=Mathf.Clamp(MaxShield,0,5);
        }
    }
    public sealed class Progression
    {
        readonly GameSession game;public readonly Dictionary<string,int> Ranks=new Dictionary<string,int>();
        public readonly List<UpgradeData> Choices=new List<UpgradeData>();
        public int Level {get;private set;}=1; public int Xp {get;private set;} public int Pending {get;private set;}
        public int Required => 12+(Level-1)*7;
        public int Generation {get;private set;}
        public Progression(GameSession session){game=session;}
        public void AddXp(int amount)
        {
            Xp+=Mathf.Max(0,amount);
            while(Xp>=Required&&Level<100){Xp-=Required;Level++;Pending++;}
        }
        public void OpenNext()
        {
            if(Pending<=0)return;
            Choices.Clear();Generation++;
            var available=game.Content.Upgrades.Values.Where(CanOffer).OrderBy(u=>u.id).ToList();
            while(Choices.Count<3 && available.Count>0)
            {
                var tiers=available.Select(u=>u.tier).Distinct().ToList();
                float tierRoll=Random.value*tiers.Sum(AugmentRules.TierWeight);string tier=tiers.Last();
                foreach(var t in tiers){tierRoll-=AugmentRules.TierWeight(t);if(tierRoll<=0){tier=t;break;}}
                var pool=available.Where(u=>u.tier==tier).ToList();
                float roll=Random.value*pool.Sum(u=>u.weight);var chosen=pool.Last();
                foreach(var u in pool){roll-=u.weight;if(roll<=0){chosen=u;break;}}
                Choices.Add(chosen);available.Remove(chosen);
            }
            while(Choices.Count<3)
            {
                int slot=Choices.Count;
                // Score supplies always have an observable effect, even at full HP/energy and all caps.
                Choices.Add(new UpgradeData {id="__supply_"+slot,name="Salvage cache",description="Gain 100 score (all remaining upgrades capped)",effect="reward2",maxLevel=1});
            }
            game.SetState(GameState.LevelUp);game.Audio.Play("level");
        }
        public bool CanOffer(UpgradeData u)
        {
            int rank=Ranks.TryGetValue(u.id,out var r)?r:0;
            return Level>=u.minLevel&&rank<u.maxLevel&&Preview(u).Length>0;
        }
        public string Preview(UpgradeData u)
        {
            if(IsSupply(u))return "+100 score";
            var ranks=new Dictionary<string,int>(Ranks);ranks[u.id]=ranks.TryGetValue(u.id,out var r)?r+1:1;
            var f=game.Content.Flight[game.FlightId];var next=new PlayerStats();next.Rebuild(f,game.Content.Weapons[f.weapon],ranks,game.Content);
            return AugmentRules.Difference(game.Stats,next);
        }
        // Internal supplies use IDs forbidden in MOD definitions; legacy effect text cannot impersonate one.
        public static bool IsSupply(UpgradeData u)=>u.id!=null&&u.id.StartsWith("__supply_",System.StringComparison.Ordinal);
        public bool Choose(int index,int generation=-1)
        {
            if(game.State!=GameState.LevelUp||index<0||index>=Choices.Count||Pending<=0||(generation>=0&&generation!=Generation))return false;
            var u=Choices[index];
            if(!IsSupply(u)&&!CanOffer(u)){OpenNext();return false;}
            if(IsSupply(u))game.Score+=100;
            else
            {
                Ranks[u.id]=Ranks.TryGetValue(u.id,out int rank)?rank+1:1;game.RebuildStats();
                foreach(var effect in AugmentRules.Effects(u))
                {
                    if(effect.effect=="repair")game.Player.Heal((int)effect.amount);
                    if(effect.effect=="guard")game.Player.Shields=game.Stats.MaxShield;
                }
            }
            Pending--;game.Audio.Play("select");game.SetState(GameState.Playing);
            if(Pending>0)OpenNext();return true;
        }
        public string Summary() => Ranks.Count==0?"No upgrades":string.Join("  /  ",Ranks.Select(p=>game.Content.Upgrades[p.Key].name+" "+p.Value));
    }
}
