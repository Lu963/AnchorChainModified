using AnchorChain.Preloader;
using System;
using UnityEngine;

namespace AnchorChain
{
    public class AnchorChainLoader : IPluginLoader
    {
        public void LoadPlugins()
        {
            Debug.Log("AdvancedChainLoader starting patch");
            AdvChainLoader.Initiate();
            Debug.Log("AdvancedChainLoader finished patch");
        }
    }
    public interface IAnchorChainMod
    {
        void TriggerEntryPoint();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ACPlugin : Attribute
    {
        // Token: 0x0600000A RID: 10 RVA: 0x000030D0 File Offset: 0x000012D0
        public ACPlugin(string guid, string name, string version, string[] before = null, string[] after = null)
        {
            GUID = guid;
            Name = name ?? "";
            Version = new Version(version ?? "1.0");
        }

        public string GUID { get; }
        public string Name { get; }
        public Version Version { get; }
    }
}