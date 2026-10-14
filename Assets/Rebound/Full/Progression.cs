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
                var u=content.Upgrades[pair.Key];float amount=u.amount*pair.Value;
                switch(u.effect)
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
        public Progression(GameSession session){game=session;}
        public void AddXp(int amount)
        {
            Xp+=Mathf.Max(0,amount);
            while(Xp>=Required&&Level<100){Xp-=Required;Level++;Pending++;}
        }
        public void OpenNext()
        {
            if(Pending<=0)return;
            Choices.Clear();
            var available=game.Content.Upgrades.Values.Where(u=>Level>=u.minLevel&&(!Ranks.ContainsKey(u.id)||Ranks[u.id]<u.maxLevel)).OrderBy(u=>u.id).ToList();
            while(Choices.Count<3 && available.Count>0)
            {
                float roll=Random.value*available.Sum(u=>u.weight);int index=available.Count-1;
                for(int i=0;i<available.Count;i++){roll-=available[i].weight;if(roll<=0){index=i;break;}}
                Choices.Add(available[index]);available.RemoveAt(index);
            }
            while(Choices.Count<3)
            {
                int slot=Choices.Count;
                Choices.Add(new UpgradeData {id="reward_"+slot,name=slot==0?"Emergency repair":slot==1?"Energy reserve":"Salvage cache",description=slot==0?"Restore one HP":slot==1?"Fill rebound energy":"Gain 100 score",effect="reward"+slot,maxLevel=1});
            }
            game.SetState(GameState.LevelUp);game.Audio.Play("level");
        }
        public bool Choose(int index)
        {
            if(game.State!=GameState.LevelUp||index<0||index>=Choices.Count||Pending<=0)return false;
            var u=Choices[index];
            if(u.effect=="reward0")game.Player.Heal(1);
            else if(u.effect=="reward1")game.Player.Energy=100;
            else if(u.effect=="reward2")game.Score+=100;
            else
            {
                Ranks[u.id]=Ranks.TryGetValue(u.id,out int rank)?rank+1:1;game.RebuildStats();
                if(u.effect=="repair")game.Player.Heal((int)u.amount);
                if(u.effect=="guard")game.Player.Shields=game.Stats.MaxShield;
            }
            Pending--;game.Audio.Play("select");game.SetState(GameState.Playing);
            if(Pending>0)OpenNext();return true;
        }
        public string Summary() => Ranks.Count==0?"No upgrades":string.Join("  /  ",Ranks.Select(p=>game.Content.Upgrades[p.Key].name+" "+p.Value));
    }
}
