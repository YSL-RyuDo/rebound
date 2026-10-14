using System.Collections.Generic;
using UnityEngine;

namespace Rebound.Full
{
    public sealed class AudioDirector : MonoBehaviour
    {
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        AudioSource music,fx; SaveManager save; string track;
        public void Initialize(SaveManager settings)
        {
            save=settings;music=gameObject.AddComponent<AudioSource>();fx=gameObject.AddComponent<AudioSource>();music.loop=true;
            string[] sounds={"shot","hit","explode","hurt","rise","charge","rebound","level","select","button","win","lose","warning"};
            for(int i=0;i<sounds.Length;i++) clips[sounds[i]]=Tone(sounds[i],160+i*65,.08f+(i%4)*.055f,i);
            clips["menu"]=Music("menu",110,0);clips["battle"]=Music("battle",146.83f,1);clips["boss"]=Music("boss",130.81f,2);clips["clear"]=Music("clear",174.61f,3);
        }
        static AudioClip Tone(string name,float hz,float length,int kind)
        {
            int rate=22050;float[] data=new float[(int)(rate*length)];var random=new System.Random(kind);
            for(int i=0;i<data.Length;i++) {float t=(float)i/rate,env=Mathf.Pow(1-(float)i/data.Length,2);data[i]=env*(Mathf.Sin(2*Mathf.PI*hz*t*(1-t*2))*.4f+(kind%3==0?(float)(random.NextDouble()-.5)*.2f:0));}
            var c=AudioClip.Create(name,data.Length,1,rate,false);c.SetData(data,0);return c;
        }
        static AudioClip Music(string name,float root,int mood)
        {
            const int rate=22050;float[] data=new float[rate*16];int[] notes={0,7,12,3,0,10,7,5,0,7,15,12,10,7,3,5};
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate,beat=t*(mood==2?4:2),phase=beat-Mathf.Floor(beat);
                float hz=root*Mathf.Pow(2,notes[((int)beat)%notes.Length]/12f);
                float synth=Mathf.Sin(2*Mathf.PI*hz*t)*Mathf.Exp(-phase*6)*.16f;
                float bass=Mathf.Sin(2*Mathf.PI*root*.5f*t)*.1f*(1-phase);
                float kick=Mathf.Sin(2*Mathf.PI*(52+60*Mathf.Exp(-phase*30))*phase)*Mathf.Exp(-phase*24)*.12f;
                data[i]=synth+bass+(mood==0?0:kick);
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void LoadMods(ContentRegistry registry)
        {
            foreach(var a in registry.Assets.Values) if(a.kind=="audio")
            {var c=ModAssetLoader.Wave(ModValidator.SafePath(registry.AssetRoots[a.id],a.path));if(clips.TryGetValue(a.id,out var old)) Destroy(old);clips[a.id]=c;}
            track=null;
        }
        public void Play(string id,float volume=1) {if(clips.TryGetValue(id,out var c)) fx.PlayOneShot(c,volume);}
        public void MusicTrack(string id) {if(track==id)return;track=id;if(clips.TryGetValue(id,out var c)){music.clip=c;music.Play();}}
        void Update() {if(save==null)return; float v=save.Data.mute?0:save.Data.master;music.volume=v*save.Data.music;fx.volume=v*save.Data.effects;}
        void OnDestroy() {if(music){music.Stop();Destroy(music);}if(fx){fx.Stop();Destroy(fx);}foreach(var c in clips.Values) Destroy(c);}
    }
}
