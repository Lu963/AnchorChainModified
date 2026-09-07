using HarmonyLib;
using SeaPower;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Debug = UnityEngine.Debug;

namespace AnchorChain
{
    public class AdvChainLoader
    {
        public static void Initiate()
        {
            currentModList = GetActiveMods(FileManager.Instance.Directories);
            ApplyList();
        }

        public static void UnloadAllMods()
        {
            foreach (var mod in currentModList)
            {
                mod.UnloadCall?.Invoke(null, new object[] { });
            }
            Harmony.UnpatchAll();
        }

        public static List<ACMod> GetActiveMods(TrulyObservableCollection<SearchDirectory> files)
        {
            List<ACMod> output = new List<ACMod>();
            foreach (SearchDirectory dir in files)
            {
                if (!dir.IsEnabled || !dir.IsChecked) continue;
                foreach (string item in Directory.GetFiles(dir.DirectoryInfo.FullName, "*.dll"))
                {
                    if (item.EndsWith("AnchorChain.dll"))
                    {
                        continue;
                    }
                    try
                    {
                        Assembly assembly = Assembly.LoadFile(item);
                        foreach (Type type in assembly.GetExportedTypes())
                        {
                            Type[] interfaces = type.GetInterfaces();
                            if (interfaces.Contains(typeof(IAnchorChainMod)))
                            {
                                ACPlugin acplugin = (ACPlugin)Attribute.GetCustomAttribute(type, typeof(ACPlugin));
                                if (acplugin != null)
                                {
                                    ACMod mod = new ACMod(dir, item, acplugin, type);
                                    MethodInfo[] methods = type.GetMethods();
                                    mod.AdvancedEntry = methods.FirstOrDefault(x => x.Name == "TriggerImprovedEntryPoint");
                                    mod.UnloadCall = methods.FirstOrDefault(x => x.Name == "TriggerModUnload");
                                    output.Add(mod);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in listing plugins at {item}");
                        Debug.LogError(e.ToString());
                    }
                }
            }
            output.Reverse();
            return output;
        }

        public static void ApplyList()
        {
            foreach (ACMod mod in currentModList)
            {
                try
                {
                    string fileName = mod.EntryType.Assembly.FullName;
                    IAnchorChainMod acMod = (IAnchorChainMod)Activator.CreateInstance(mod.EntryType);
                    if (mod.AdvancedEntry != null)
                    {
                        Debug.Log($"Loading plugin '{fileName}' of mod '{mod.Location.Name}' using Improved entry point");
                        mod.HarmonyInstance = (Harmony)mod.AdvancedEntry.Invoke(acMod, new object[] { });
                    }
                    else
                    {
                        Debug.Log($"Loading plugin '{fileName}' of mod '{mod.Location.Name}' using AnchorChain entry point");
                        acMod.TriggerEntryPoint();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in loading plugin at {mod.Location.DirectoryInfo.FullName}");
                    Debug.LogError(e.ToString());
                }
            }
        }

        public static bool CheckForModChanges(List<ACMod> newList)
        {
            if (currentModList.Count != newList.Count) { return true; }
            for (int i = 0; i < currentModList.Count; i++)
            {
                bool check = string.Equals(currentModList[i].FullPath, newList[i].FullPath);
                if (!check) { return true; }
            }
            return false;
        }

        private static List<ACMod> currentModList = new List<ACMod>();
    }

    public class ACMod
    {
        public ACMod(SearchDirectory directory, string path, ACPlugin plugin, Type entry)
        {
            Location = directory;
            BasicPluginData = plugin;
            EntryType = entry;
            FullPath = path;
        }

        public Type EntryType { get; }
        public SearchDirectory Location { get; }
        public string FullPath { get; }
        public MethodInfo AdvancedEntry { get; set; }
        public MethodInfo UnloadCall { get; set; }
        public ACPlugin BasicPluginData { get; set; }
        public Harmony HarmonyInstance { get; set; }
    }
}