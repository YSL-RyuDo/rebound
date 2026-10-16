using System.Collections.Generic;
using UnityEngine;

namespace Rebound.Full
{
    public static class AugmentRules
    {
        public static readonly string[] Codes={"damage","haste","multi","pierce","crit","range","rebound","chain","charge","thrust","fall","guard","repair"};
        public static readonly string[] Tiers={"silver","gold","platinum","prismatic"};
        public static IEnumerable<UpgradeEffect> Effects(UpgradeData u)
        {
            if(u.effects!=null&&u.effects.Length>0) return u.effects;
            return new[]{new UpgradeEffect {effect=u.effect,amount=u.amount}};
        }
        public static float TierWeight(string tier)=>tier=="gold"?30:tier=="platinum"?12:tier=="prismatic"?3:55;
        public static Color TierColor(string tier)=>tier=="gold"?new Color(1,.78f,.3f):tier=="platinum"?new Color(.35f,1,.85f):tier=="prismatic"?new Color(.85f,.55f,1):new Color(.75f,.82f,.9f);
        public static string Difference(PlayerStats a,PlayerStats b)
        {
            var changes=new List<string>();
            Add(changes,"Damage",a.Damage,b.Damage);Add(changes,"Shot interval (s)",a.Interval,b.Interval);
            Add(changes,"Projectiles",a.Count,b.Count);Add(changes,"Pierce",a.Pierce,b.Pierce);
            Add(changes,"Critical %",a.Crit*100,b.Crit*100);Add(changes,"Rebound reach",a.Range,b.Range);
            Add(changes,"Rebound damage",a.Rebound,b.Rebound);Add(changes,"Chains",a.Chains,b.Chains);
            Add(changes,"Fall charge",a.Charge,b.Charge);Add(changes,"Rise speed",a.Thrust,b.Thrust);
            Add(changes,"Max fall speed",a.MaxFall,b.MaxFall);Add(changes,"Max HP",a.MaxHp,b.MaxHp);Add(changes,"Shield capacity",a.MaxShield,b.MaxShield);
            return string.Join("\n",changes);
        }
        static void Add(List<string> lines,string name,float before,float after)
        {if(!Mathf.Approximately(before,after))lines.Add($"{name}: {before:0.###} → {after:0.###}");}
    }
}
