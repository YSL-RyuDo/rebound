using UnityEngine;

namespace Rebound.Full
{
    public sealed class PlayerFlight : MonoBehaviour
    {
        GameSession game;Rigidbody2D body;SpriteRenderer hull,engine,shield;
        float axis,vy,nextRise,nextShot,immune,regen;bool queued,ready;
        public float Energy;public int Hp,Shields;public bool ReadInput=true;
        public Vector2 Position=>body.position;public float VelocityY=>vy;
        public void Initialize(GameSession session)
        {
            game=session;body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            transform.position=new Vector2(-5.5f,1);body.position=transform.position;Hp=game.Stats.MaxHp;
            hull=game.Art.Draw("Hull",game.Content.Flight[game.FlightId].skin,body.position,new Vector2(.95f,.65f),game.Art.Cyan,transform,5);
            engine=game.Art.Draw("Thruster","spark",body.position+Vector2.left*.5f,new Vector2(.5f,.25f),game.Art.Gold,transform,4);
            shield=game.Art.Draw("Shield","ring",body.position,Vector2.one*1.15f,game.Art.Cyan,transform,5);
        }
        public void InputAxis(float value){axis=Mathf.Clamp(value,-1,1);}
        public void Rise(){if(game.Running)queued=true;}
        void Update()
        {
            if(!game)return;
            if(ReadInput){axis=(Input.GetKey(game.Save.Right)?1:0)-(Input.GetKey(game.Save.Left)?1:0);if(Input.GetKeyDown(game.Save.Rise))Rise();}
            hull.color=Time.time<immune?new Color(.2f,1,1,.35f+Mathf.PingPong(Time.time*8,.65f)):game.Art.Cyan;
            hull.transform.localRotation=Quaternion.Euler(0,0,Mathf.Clamp(vy*2,-12,12));
            engine.transform.localScale=new Vector2(.35f+Mathf.Max(0,vy)*.07f,.2f+Mathf.Sin(Time.time*30)*.04f);
            shield.enabled=Shields>0;
        }
        void FixedUpdate()
        {
            if(!game.Running){queued=false;return;}
            var s=game.Stats;float dt=Time.fixedDeltaTime;
            if(queued&&Time.time>=nextRise)
            {
                vy=s.Thrust;nextRise=Time.time+s.RiseInterval;game.Audio.Play("rise",.3f);
                if(Energy>=65){Energy=0;game.World.Rebound(Position,s);game.Audio.Play("rebound");game.Shake(.7f);}
            }
            queued=false;
            float gravity=game.Stage!=null&&game.Stage.GravityDisturbed?1.55f:1;
            vy=Mathf.Max(-s.MaxFall,vy-s.Gravity*gravity*dt);
            Vector2 old=body.position,next=old+new Vector2(axis*s.Speed,vy)*dt;
            next.x=Mathf.Clamp(next.x,GameSession.Left+.5f,GameSession.Left+(GameSession.Right-GameSession.Left)*.4f-.5f);
            if(next.y>GameSession.Top){next.y=GameSession.Top;vy=Mathf.Min(0,vy);}
            if(next.y<GameSession.Bottom)
            {
                Damage(1,true);body.position=new Vector2(-5.5f,1);vy=0;Energy=0;ready=false;return;
            }
            float descent=Mathf.Max(0,old.y-next.y);Energy=Mathf.Clamp(Energy+descent*s.Charge*(1+.15f*Mathf.Clamp01(-vy/s.MaxFall)),0,100);
            if(Energy>=65&&!ready)game.Audio.Play("charge",.45f);ready=Energy>=65;
            body.MovePosition(next);
            if(Time.time>=nextShot){game.World.FirePlayer(next,s);nextShot=Time.time+s.Interval;}
            if(Shields<s.MaxShield){regen+=dt;if(regen>=20){Shields++;regen=0;}}else regen=0;
        }
        public void Damage(int amount,bool fall=false)
        {
            if(!game.Running||(!fall&&Time.time<immune))return;
            if(!fall&&Shields>0){Shields--;immune=Time.time+.5f;game.Effects.Ring(Position,1.5f,game.Art.Cyan);return;}
            Hp=Mathf.Max(0,Hp-amount);immune=Time.time+1.2f;game.Audio.Play("hurt");game.Shake(.9f);game.Effects.Burst(Position,game.Art.Cyan);
            if(Hp==0){game.Effects.Burst(Position,game.Art.Cyan,24,.2f);hull.enabled=false;engine.enabled=false;game.Finish(false);}
        }
        public void Heal(int amount){Hp=Mathf.Min(game.Stats.MaxHp,Hp+amount);}
        public void Teleport(Vector2 position){body.position=position;vy=0;Energy=0;}
    }
}
