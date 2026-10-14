using System.Collections.Generic;
using UnityEngine;

namespace Rebound.Full
{
    public sealed class CombatEnemy
    {
        public EnemyData Data;public SpriteRenderer Visual,Shield,Warning;public Vector2 Position;
        public float Hp,MaxHp,Cooldown,WarnTime,Age;public bool Dead,Boss;public int Phase=1;
    }
    public sealed class CombatProjectile
    {
        public SpriteRenderer Visual;public Vector2 Position,Velocity;public float Damage,Life,Radius;
        public bool Active,Hostile,Critical;public int Pierce;public readonly HashSet<CombatEnemy> Hit=new HashSet<CombatEnemy>();
    }
    public sealed class CombatWorld : MonoBehaviour
    {
        GameSession game;public readonly List<CombatEnemy> Enemies=new List<CombatEnemy>();
        public readonly List<CombatProjectile> Projectiles=new List<CombatProjectile>();
        readonly Queue<CombatProjectile> pool=new Queue<CombatProjectile>();
        public int ActiveProjectiles=>Projectiles.Count-pool.Count;
        public void Initialize(GameSession session)
        {
            game=session;
            for(int i=0;i<384;i++)
            {
                var p=new CombatProjectile {Visual=game.Art.Draw("Pooled projectile","bullet",Vector2.zero,Vector2.one*.2f,Color.white,transform,6)};
                p.Visual.enabled=false;Projectiles.Add(p);pool.Enqueue(p);
            }
        }
        public CombatEnemy Spawn(EnemyData data,Vector2 position,float difficulty=1,bool boss=false)
        {
            var e=new CombatEnemy {Data=data,Position=position,Hp=data.hp*difficulty,MaxHp=data.hp*difficulty,Cooldown=2,Boss=boss};
            e.Visual=game.Art.Draw(data.name,data.sprite,position,Vector2.one*(boss?2.7f:.85f),boss?game.Art.Gold:game.Art.Red,transform,3);
            e.Shield=game.Art.Draw("Armor","ring",position,Vector2.one*(boss?3:1.05f),new Color(.6f,.7f,1,.65f),transform,4);
            e.Warning=game.Art.Draw("Warning","ring",position,Vector2.one*1.5f,game.Art.Gold,transform,4);e.Warning.enabled=false;
            Enemies.Add(e);return e;
        }
        public void FirePlayer(Vector2 origin,PlayerStats stats)
        {
            Vector2 direction=Vector2.right;float closest=float.MaxValue;
            foreach(var enemy in Enemies)
            {
                Vector2 delta=enemy.Position-origin;
                if(enemy.Dead||delta.x<=0||Vector2.Angle(Vector2.right,delta)>20||delta.sqrMagnitude>=closest)continue;
                closest=delta.sqrMagnitude;direction=delta.normalized;
            }
            for(int i=0;i<stats.Count;i++)
            {
                float angle=(i-(stats.Count-1)*.5f)*(stats.Weapon.pattern=="fan"?stats.Weapon.spread:6);
                Vector2 dir=Quaternion.Euler(0,0,angle)*direction;bool crit=Random.value<stats.Crit;
                Shoot(origin+Vector2.right*.5f,dir*stats.BulletSpeed,stats.Damage*(crit?2:1),stats.BulletSize,stats.Pierce,false,crit,stats.Weapon.sprite);
            }
            game.Audio.Play("shot",.15f);
        }
        public void Shoot(Vector2 position,Vector2 velocity,float damage,float size,int pierce,bool hostile,bool critical=false,string sprite="bullet")
        {
            if(pool.Count==0)return;
            var p=pool.Dequeue();p.Position=position;p.Velocity=velocity;p.Damage=damage;p.Radius=size*.5f;p.Pierce=pierce;p.Hostile=hostile;p.Critical=critical;p.Life=5;p.Active=true;p.Hit.Clear();
            p.Visual.sprite=game.Art.Get(sprite);p.Visual.enabled=true;p.Visual.color=hostile?game.Art.Red:critical?Color.white:game.Art.Gold;
            p.Visual.transform.position=position;p.Visual.transform.localScale=new Vector2(size*2,size);p.Visual.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
        }
        void Release(CombatProjectile p){if(!p.Active)return;p.Active=false;p.Visual.enabled=false;pool.Enqueue(p);}
        public void Hit(CombatEnemy enemy,float damage,bool rebound=false,bool critical=false)
        {
            if(enemy.Dead||!game.Running)return;
            float armor=enemy.Boss?(enemy.Phase==3&&enemy.Age%8<2?.8f:.35f):enemy.Data.armor;
            enemy.Hp-=damage*(rebound?1.5f:1-armor);
            game.Effects.Burst(enemy.Position,critical?Color.white:game.Art.Gold,critical?5:2,.09f);game.Audio.Play("hit",.08f);
            if(enemy.Hp>0)return;
            enemy.Dead=true;enemy.Visual.enabled=false;enemy.Shield.enabled=false;enemy.Warning.enabled=false;
            game.EnemyKilled(enemy.Data.xp,enemy.Position);
            if(enemy.Boss)game.Finish(true);
        }
        public void Rebound(Vector2 origin,PlayerStats stats)
        {
            var hit=new HashSet<CombatEnemy>();
            foreach(var e in Enemies)
            {
                Vector2 d=e.Position-origin;float radius=e.Boss?1.25f:.35f;
                if(!e.Dead&&d.x>=0&&d.x-radius<=stats.Range&&Mathf.Abs(d.y)<=1.6f+radius){Hit(e,stats.Rebound,true);hit.Add(e);}
            }
            for(int i=0;i<stats.Chains;i++)
            {
                CombatEnemy next=null;float distance=3.5f;
                foreach(var e in Enemies) if(!e.Dead&&!hit.Contains(e)) foreach(var prior in hit)
                {float d=Vector2.Distance(e.Position,prior.Position);if(d<distance){distance=d;next=e;}}
                if(next==null)break;Hit(next,stats.Rebound*.6f,true);hit.Add(next);game.Effects.Ring(next.Position,1.6f,game.Art.Cyan);
            }
            game.Effects.Ring(origin+Vector2.right*stats.Range*.5f,stats.Range,new Color(.25f,.9f,1,.8f));
            // Rebound also clears incoming bullets in its forward area.
            foreach(var p in Projectiles) if(p.Active&&p.Hostile&&p.Position.x>=origin.x&&p.Position.x<=origin.x+stats.Range&&Mathf.Abs(p.Position.y-origin.y)<1.6f)Release(p);
        }
        void FixedUpdate()
        {
            if(!game.Running)return;float dt=Time.fixedDeltaTime;
            foreach(var e in Enemies)
            {
                if(e.Dead)continue;e.Age+=dt;e.Cooldown-=dt;
                if(e.Boss)
                {
                    e.Position.x=Mathf.MoveTowards(e.Position.x,6.1f,dt*1.2f);e.Position.y=Mathf.Sin(e.Age*.7f)*2;
                    e.Phase=e.Hp/e.MaxHp<=.25f?3:e.Hp/e.MaxHp<=.6f?2:1;
                }
                else
                {
                    e.Position.x-=e.Data.speed*dt;
                    if(e.Data.pattern=="track")e.Position.y=Mathf.MoveTowards(e.Position.y,game.Player.Position.y,dt*1.6f);
                }
                bool shoots=e.Boss||e.Data.pattern=="shoot";
                if(shoots&&e.Cooldown<=.65f){e.Warning.enabled=true;e.Warning.transform.localScale=Vector2.one*(1.4f+Mathf.Sin(e.Age*20)*.12f);}
                if(shoots&&e.Cooldown<=0)
                {
                    Vector2 aim=(game.Player.Position-e.Position).normalized;
                    int count=e.Boss?(e.Phase==1?3:e.Phase==2?7:9):1;
                    for(int i=0;i<count;i++) {Vector2 dir=Quaternion.Euler(0,0,(i-(count-1)*.5f)*(e.Phase==3?13:10))*aim;Shoot(e.Position+Vector2.left*.6f,dir*(e.Boss?4+e.Phase:4),e.Data.damage,.22f,0,true);}
                    e.Cooldown=e.Boss?(e.Phase==1?2:e.Phase==2?1.5f:1.1f):e.Data.interval;e.Warning.enabled=false;game.Audio.Play("warning",.15f);
                }
                e.Visual.transform.position=e.Position;e.Shield.transform.position=e.Position;e.Warning.transform.position=e.Position;
                e.Visual.transform.localRotation=Quaternion.Euler(0,0,e.Boss?e.Age*12:Mathf.Sin(e.Age*3)*8);
                e.Shield.enabled=e.Boss||e.Data.armor>0;
                if(Vector2.Distance(e.Position,game.Player.Position)<(e.Boss?1.45f:.65f)){game.Player.Damage(e.Data.damage);if(!e.Boss)e.Dead=true;}
                if(e.Position.x<GameSession.Left-1)e.Dead=true;
            }
            foreach(var p in Projectiles)
            {
                if(!p.Active)continue;
                Vector2 old=p.Position;p.Position+=p.Velocity*dt;p.Life-=dt;
                if(p.Hostile)
                {
                    if(SegmentDistance(game.Player.Position,old,p.Position)<.28f+p.Radius){game.Player.Damage(Mathf.CeilToInt(p.Damage));Release(p);}
                }
                else foreach(var e in Enemies)
                {
                    if(e.Dead||p.Hit.Contains(e)||SegmentDistance(e.Position,old,p.Position)>(e.Boss?1.2f:.36f)+p.Radius)continue;
                    p.Hit.Add(e);Hit(e,p.Damage,false,p.Critical);if(p.Pierce--<=0){Release(p);break;}
                }
                if(p.Life<=0||Mathf.Abs(p.Position.x)>11||Mathf.Abs(p.Position.y)>6)Release(p);
                p.Visual.transform.position=p.Position;
            }
            for(int i=Enemies.Count-1;i>=0;i--) if(Enemies[i].Dead)
            {var e=Enemies[i];Destroy(e.Visual.gameObject);Destroy(e.Shield.gameObject);Destroy(e.Warning.gameObject);Enemies.RemoveAt(i);}
        }
        public static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){var ab=b-a;float t=ab.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude):0;return Vector2.Distance(p,a+ab*t);}
    }
    public sealed class StageDirector
    {
        readonly GameSession game;readonly StageData data;float nextSpawn;int section=-1;public CombatEnemy Boss;
        public bool GravityDisturbed=>Boss!=null&&!Boss.Dead&&Boss.Phase>=2&&Boss.Age%12>8;
        public StageDirector(GameSession session,StageData stage){game=session;data=stage;}
        public void Tick(float dt)
        {
            if(game.Elapsed>=data.bossTime)
            {
                if(Boss==null)
                {
                    Boss=game.World.Spawn(new EnemyData {id="warden",name="GRAVITY WARDEN",hp=data.bossHp,speed=0,xp=100,damage=1,sprite="boss"},new Vector2(10,0),1,true);
                    game.Announce(game.Content.Line("boss"));game.Audio.MusicTrack("boss");game.Audio.Play("warning");
                }
                return;
            }
            for(int i=0;i<data.waves.Length;i++)
            {
                var wave=data.waves[i];if(game.Elapsed<wave.start||game.Elapsed>=wave.end)continue;
                if(section!=i){section=i;game.Announce(game.Content.Line(new[]{"outer","security","anomaly","core","warning"}[Mathf.Min(i,4)]));}
                if(game.Elapsed<nextSpawn)return;
                for(int n=0;n<wave.count;n++)
                {
                    var enemy=game.Content.Enemies[wave.enemies[Random.Range(0,wave.enemies.Length)]];
                    float y=Mathf.Clamp(game.Player.Position.y+Random.Range(-1.5f,1.5f),wave.minY,wave.maxY);
                    game.World.Spawn(enemy,new Vector2(GameSession.Right+1+n*.8f,y),wave.difficulty);
                }
                nextSpawn=game.Elapsed+wave.interval;return;
            }
        }
    }
}
