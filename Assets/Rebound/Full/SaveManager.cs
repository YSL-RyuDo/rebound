using System;
using System.IO;
using UnityEngine;

namespace Rebound.Full
{
    [Serializable] public class SaveData
    {
        public int version=1, width=1920, height=1080, bestScore, clears;
        public float master=.7f, music=.35f, effects=.65f, bestTime;
        public bool mute, fullscreen, shake=true;
        public string left="A",right="D",rise="Space";
        public string[] enabledMods=new string[0];
    }
    public sealed class SaveManager
    {
        readonly string path; public SaveData Data {get; private set;} public string Error {get; private set;}
        public SaveManager(string directory)
        {
            Directory.CreateDirectory(directory); path=Path.Combine(directory,"settings.json"); Data=new SaveData();
            try
            {
                if(!File.Exists(path)) return;
                var data=JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if(data==null || data.version!=1 || data.enabledMods==null || data.enabledMods.Length>128) throw new FormatException("Invalid save version/data");
                if(!ValidKey(data.left)||!ValidKey(data.right)||!ValidKey(data.rise)||data.left==data.right||data.left==data.rise||data.right==data.rise) throw new FormatException("Invalid input bindings");
                if(float.IsNaN(data.master)||float.IsNaN(data.music)||float.IsNaN(data.effects)||float.IsInfinity(data.master)||float.IsInfinity(data.music)||float.IsInfinity(data.effects)) throw new FormatException("Invalid volume");
                data.master=Mathf.Clamp01(data.master); data.music=Mathf.Clamp01(data.music); data.effects=Mathf.Clamp01(data.effects);
                data.width=Mathf.Clamp(data.width,960,3840); data.height=Mathf.Clamp(data.height,540,2160); Data=data;
            }
            catch(Exception ex) {Error="Settings reset: "+ex.Message; try {File.Copy(path,path+".corrupt",true);} catch(IOException) {} }
        }
        public static bool ValidKey(string key) => Enum.TryParse(key,out KeyCode c) && c!=KeyCode.None && c!=KeyCode.Escape && (int)c<(int)KeyCode.Mouse0;
        public void Save()
        {
            try
            {
                File.WriteAllText(path+".tmp",JsonUtility.ToJson(Data,true));
                if(File.Exists(path)) File.Replace(path+".tmp",path,path+".bak"); else File.Move(path+".tmp",path);
                Error=null;
            } catch(Exception ex) {Error="Save failed: "+ex.Message; Debug.LogWarning(Error);}
        }
        public KeyCode Left => (KeyCode)Enum.Parse(typeof(KeyCode),Data.left);
        public KeyCode Right => (KeyCode)Enum.Parse(typeof(KeyCode),Data.right);
        public KeyCode Rise => (KeyCode)Enum.Parse(typeof(KeyCode),Data.rise);
    }
}
