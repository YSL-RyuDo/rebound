using System.Collections.Generic;
using UnityEngine;

namespace Rebound.Full
{
    public sealed class ArtFactory
    {
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly HashSet<string> imported=new HashSet<string>();
        public Color SpriteColor(string id,Color tint)=>imported.Contains(id)?new Color(1,1,1,tint.a):tint;
        public readonly Color Cyan=new Color(.18f,.94f,.94f), Red=new Color(1,.27f,.22f), Gold=new Color(1,.8f,.28f);
        public Sprite Get(string id)
        {
            if(sprites.TryGetValue(id,out var sprite)) return sprite;
            int n=64; var t=new Texture2D(n,n,TextureFormat.RGBA32,false); t.filterMode=FilterMode.Bilinear;
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1; float r=Mathf.Sqrt(u*u+v*v); bool ink;
                switch(id)
                {
                    case "player": ink=(Mathf.Abs(v)<.38f*(1-u)&&u>-.65f&&u<.95f)||(u<-.3f&&u>-.8f&&Mathf.Abs(v)<.82f&&Mathf.Abs(v)>.45f); break;
                    case "lightframe": ink=(Mathf.Abs(v)<.18f*(1-u)&&u>-.85f&&u<.95f)||(Mathf.Abs(u+.45f)<.09f&&Mathf.Abs(v)<.75f);break;
                    case "damage": ink=Mathf.Abs(u)<.16f&&Mathf.Abs(v)<.8f||Mathf.Abs(v)<.16f&&Mathf.Abs(u)<.8f;break;
                    case "haste": ink=Mathf.Abs(Mathf.Abs(v)-u*.6f-.2f)<.1f&&Mathf.Abs(v)<.75f;break;
                    case "multi": ink=Mathf.Abs(u)<.7f&&(Mathf.Abs(v)<.1f||Mathf.Abs(Mathf.Abs(v)-.5f)<.1f);break;
                    case "pierce": ink=Mathf.Abs(v)<.1f&&Mathf.Abs(u)<.8f||Mathf.Abs(Mathf.Abs(v)+u-.75f)<.13f&&u>.1f;break;
                    case "crit": ink=r<.85f&&(Mathf.Abs(u*v)<.055f||Mathf.Abs(Mathf.Abs(u)-Mathf.Abs(v))<.07f);break;
                    case "range": ink=r>.62f&&r<.8f||Mathf.Abs(u*v)<.025f&&r<.9f;break;
                    case "rebound": ink=(Mathf.Abs(r-.3f)<.08f||Mathf.Abs(r-.65f)<.08f)&&u>-.2f;break;
                    case "chain": ink=Mathf.Abs(Mathf.Sqrt((u-.3f)*(u-.3f)+v*v)-.35f)<.09f||Mathf.Abs(Mathf.Sqrt((u+.3f)*(u+.3f)+v*v)-.35f)<.09f;break;
                    case "charge": ink=Mathf.Abs(u+v*.45f-(v>0?.15f:-.15f))<.18f&&Mathf.Abs(v)<.85f;break;
                    case "thrust": ink=Mathf.Abs(u)<.12f&&Mathf.Abs(v)<.7f||Mathf.Abs(Mathf.Abs(u)+v-.7f)<.13f&&v>.1f;break;
                    case "fall": ink=Mathf.Abs(u)<.12f&&Mathf.Abs(v)<.7f||Mathf.Abs(Mathf.Abs(u)-v-.7f)<.13f&&v<-.1f;break;
                    case "guard": ink=Mathf.Abs(u)<.65f&&v<.7f&&v>Mathf.Abs(u)-.9f&&(Mathf.Abs(u)>.45f||v>.5f||v<Mathf.Abs(u)-.7f);break;
                    case "repair": ink=Mathf.Abs(u)<.2f&&Mathf.Abs(v)<.65f||Mathf.Abs(v)<.2f&&Mathf.Abs(u)<.65f;break;
                    case "hunter": ink=Mathf.Abs(v)<.85f && Mathf.Abs(u+Mathf.Abs(v)*.7f)<.22f; break;
                    case "shield": ink=(Mathf.Abs(u)+Mathf.Abs(v)<.9f)||(u<-.65f&&Mathf.Abs(v)<.8f);break;
                    case "gunner": ink=Mathf.Max(Mathf.Abs(u),Mathf.Abs(v))<.65f && !(u<-.2f&&Mathf.Abs(v)<.18f);break;
                    case "boss": ink=(r<.65f&&r>.38f)||(Mathf.Abs(u)<.2f&&Mathf.Abs(v)<.95f)||(Mathf.Abs(u)<.9f&&Mathf.Abs(v)<.15f);break;
                    case "bullet": ink=Mathf.Abs(u)+Mathf.Abs(v)*2<.95f;break;
                    case "ring": ink=r>.8f&&r<.95f;break;
                    case "spark": ink=Mathf.Abs(u)*Mathf.Abs(v)<.045f&&r<.9f;break;
                    case "reactor": ink=Mathf.Abs(u)<.92f&&Mathf.Abs(v)<.92f&&(Mathf.Abs(u)>.85f||Mathf.Abs(v)>.85f||Mathf.Abs(u-v)<.04f);break;
                    case "upgrade": ink=Mathf.Abs(u)+Mathf.Abs(v)<.8f&&(Mathf.Abs(u)<.2f||Mathf.Abs(v)<.2f);break;
                    default: ink=(Mathf.Abs(u)+Mathf.Abs(v)<.9f)||(Mathf.Abs(u)<.75f&&Mathf.Abs(v)<.2f);break;
                }
                float light=.65f+.35f*(v+1)*.5f;
                if(Mathf.Abs(u)<.12f&&Mathf.Abs(v)<.15f&&id!="ring") light=1;
                t.SetPixel(x,y,ink?new Color(light,light,light,1):Color.clear);
            }
            t.Apply(); sprite=Sprite.Create(t,new Rect(0,0,n,n),Vector2.one*.5f,n); sprites[id]=sprite;return sprite;
        }
        public void LoadMods(ContentRegistry registry)
        {
            foreach(var a in registry.Assets.Values) if(a.kind=="sprite")
            {
                var s=ModAssetLoader.Sprite(ModValidator.SafePath(registry.AssetRoots[a.id],a.path));
                if(sprites.TryGetValue(a.id,out var old)) {Object.Destroy(old.texture);Object.Destroy(old);}
                sprites[a.id]=s;
                imported.Add(a.id);
            }
        }
        public SpriteRenderer Draw(string name,string sprite,Vector2 position,Vector2 scale,Color color,Transform parent,int order=1)
        {
            var renderer=new GameObject(name).AddComponent<SpriteRenderer>(); renderer.sprite=Get(sprite);renderer.color=order<7?SpriteColor(sprite,color):color;renderer.sortingOrder=order;
            renderer.transform.SetParent(parent,false); renderer.transform.position=position;renderer.transform.localScale=scale;return renderer;
        }
        public void Dispose() {foreach(var s in sprites.Values) {Object.Destroy(s.texture);Object.Destroy(s);} sprites.Clear();}
    }
    public sealed class VisualEffects : MonoBehaviour
    {
        struct Particle {public SpriteRenderer sprite;public Vector2 velocity;public float life,max;}
        readonly List<Particle> particles=new List<Particle>(); GameSession game;
        public void Initialize(GameSession session) {game=session;}
        public void Burst(Vector2 position,Color color,int count=8,float scale=.12f,string effect="impact")
        {
            game.Content.Effects.TryGetValue(effect,out var data);
            if(data!=null){count=data.count;scale=data.size;ColorUtility.TryParseHtmlString(data.color,out color);}
            for(int i=0;i<count&&particles.Count<180;i++)
            {
                var sprite=game.Art.Draw("Spark",data?.sprite??"spark",position,Vector2.one*scale,color,transform,8);
                particles.Add(new Particle {sprite=sprite,velocity=Random.insideUnitCircle*(data?.speed??3),life=data?.lifetime??.45f,max=data?.lifetime??.45f});
            }
        }
        public void Ring(Vector2 position,float size,Color color)
        {
            if(particles.Count>=180)return;
            game.Content.Effects.TryGetValue(game.Content.Flight[game.FlightId].reboundEffect,out var data);
            if(data!=null)ColorUtility.TryParseHtmlString(data.color,out color);
            for(int i=0;i<(data?.count??1)&&particles.Count<180;i++)
            {
                var sprite=game.Art.Draw("Shock ring",data?.sprite??"ring",position,Vector2.one*size*(data?.size??1),color,transform,7);
                particles.Add(new Particle {sprite=sprite,velocity=Random.insideUnitCircle*(data?.speed??0),life=data?.lifetime??.3f,max=data?.lifetime??.3f});
            }
        }
        void Update()
        {
            if(game==null||game.State==GameState.Pause||game.State==GameState.LevelUp) return;
            for(int i=particles.Count-1;i>=0;i--)
            {
                var p=particles[i];p.life-=Time.deltaTime;
                if(p.life<=0){Destroy(p.sprite.gameObject);particles.RemoveAt(i);continue;}
                p.sprite.transform.position+=(Vector3)p.velocity*Time.deltaTime;
                var c=p.sprite.color;c.a=p.life/p.max;p.sprite.color=c;p.sprite.transform.localScale*=1+Time.deltaTime*1.8f;particles[i]=p;
            }
        }
        public void Clear() {foreach(var p in particles) if(p.sprite) Destroy(p.sprite.gameObject); particles.Clear();}
    }
}
