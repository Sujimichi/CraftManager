using System;
using System.IO;
using System.Collections.Generic;

using KatLib;

namespace CraftManager
{
    public class CraftDataCache
    {
        internal static string cache_path = Paths.joined(CraftManager.ksp_root, "GameData", "CraftManager", "PluginData", "craft_data.cache");
        internal static string installed_part_sig; //checksum signature of the installed parts, used to determine if the installed parts have changed since last time

        internal static Dictionary<string, AvailablePart> part_data = new Dictionary<string, AvailablePart>();  //name->part lookup for available parts
        internal static List<string> locked_parts = new List<string>();

        private Dictionary<string, ConfigNode> craft_data = new Dictionary<string, ConfigNode>();
        private List<string> ignore_fields = new List<string>{"selected_craft", "selected_group", "active_craft", "loaded_craft_saved", "craft_saved", "save_names"};

        internal Dictionary<string, int> tag_craft_count_store = new Dictionary<string, int>();

        internal int tag_craft_count_for(string lookup, string modifiyer = ""){            
            if(!tag_craft_count_store.ContainsKey(lookup + "_" + modifiyer)){
                tag_craft_count_store.Add(lookup + "_" + modifiyer, Tags.craft_count_for(lookup, modifiyer));
            }
            return tag_craft_count_store[lookup + "_" + modifiyer];
        }

        internal static void cache_game_parts(){
            CraftManager.log("caching game parts");
            locked_parts.Clear();
            List<string> part_names = new List<string>();
            foreach(AvailablePart part in PartLoader.LoadedPartsList){
                if(!part_data.ContainsKey(part.name)){
                    part_data.Add(part.name, part);
                }
                part_names.AddUnique(part.name);                        
                if(!ResearchAndDevelopment.PartTechAvailable(part)){
                    locked_parts.AddUnique(part.name);
                }
            }
            //Make a string containing all the installed parts and the data from LoaderInfo in the save file and 
            //then generate a checksum from it.  This is used as a signature of the installed setup which will change
            //if the installed mods are changed enabling craft to disregard cached data after a change in mod setup.
            part_names.Sort();
            string s = String.Join("", part_names.ToArray());
            string lf = "";
            try{
                ConfigNode save_data = ConfigNode.Load(Paths.joined(CraftManager.ksp_root, "saves", HighLogic.SaveFolder, "persistent.sfs"));                   
                ConfigNode loader_info = save_data.GetNode("GAME").GetNode("LoaderInfo");
                lf = loader_info.ToString();
            }
            catch(Exception e){
                CraftManager.log("Failed to read loaderinfo " + e.Message);
            }
            installed_part_sig = Checksum.digest(s+lf);
        }



        internal CraftDataCache(){
            CraftManager.log("Initializing Cache");
            CraftManager.settings.ensure_plugin_data_dir_exists();
            if(File.Exists(cache_path)){
                CraftManager.log("loading persistent cache from file");
                try{
                    load(); 
                }
                catch(Exception e){
                    CraftManager.log("Failed to load cache " + e.Message);
                }
            }

            if(part_data.Count == 0){
                cache_game_parts();                
            }
            CraftManager.log("Cache Ready");
        }

        internal AvailablePart fetch_part(string part_name){
            if(part_data.ContainsKey(part_name)){
                return part_data[part_name];
            }
            return null;
        }

        private string sanitize_path(string path){
            return path.Replace(@"\","/");
        }

        //takes a CraftData craft and creates a ConfigNode that contains all of it's public properties, ConfigNodes is held in 
        //a <string, ConfigNode> dict with the full path as the key. 
        internal void write(CraftData craft){
            ConfigNode node = new ConfigNode();
            foreach(var prop in craft.GetType().GetProperties()){
                if(!ignore_fields.Contains(prop.Name)){
                    var value = prop.GetValue(craft, null);
                    if(value != null){
                        node.AddValue(prop.Name, value);
                    }
                }
            }
            string path = sanitize_path(craft.path);
            if(craft_data.ContainsKey(path)){
                craft_data[path] = node;
            }else{
                craft_data.Add(path, node);
            }
            save();
        }

        //Takes a CraftData craft object and if the cached data contains a matching path AND the checksum value matches
        //then the craft's properties are populated from the ConfigNode in the cache.  Returns true if matching data was
        //found, otherwise returns false, in which case the data will have to be interpreted from the .craft file.
        internal bool try_fetch(CraftData craft){
            string path = sanitize_path(craft.path);
            if(craft_data.ContainsKey(path) && craft_data[path].GetValue("checksum") == craft.checksum && craft_data[path].GetValue("part_sig") == installed_part_sig){
                try{
//                    CraftManager.log("loading from CACHE: " + Path.GetFileNameWithoutExtension(path));
                    CraftData.cache_load_count += 1; //increment count of craft loaded from cache
                    ConfigNode node = craft_data[path];                    
                    foreach(var prop in craft.GetType().GetProperties()){               
                        if(prop.CanWrite){                            
                            var node_value = node.GetValue(prop.Name);
                            if(!String.IsNullOrEmpty(node_value)){
                                var type = prop.GetValue(craft, null);
                                if(type is float){
                                    prop.SetValue(craft, float.Parse(node_value), null);                                
                                }else if(type is int){
                                    prop.SetValue(craft, int.Parse(node_value), null);                                
                                }else if(type is bool){                 
                                    prop.SetValue(craft, bool.Parse(node_value), null);                                
                                }else{
                                    prop.SetValue(craft, node_value, null);                                
                                }
                            }
                        }
                    }
                    if(node.HasValue("locked_parts")){
                        craft.locked_parts_checked = true;
                    }
                    return true;
                }
                catch(Exception e){
                    CraftManager.log("try_fetch failed: " + e.Message + "\n" + e.StackTrace);
                    return false;
                }
            } else{
                return false;
            }
        }

        private void save(){
            ConfigNode nodes = new ConfigNode();
            ConfigNode craft_nodes = new ConfigNode();
            nodes.AddValue("craft_manager_version", CraftManager.version);
            foreach(KeyValuePair<string, ConfigNode> pair in craft_data){
                ConfigNode node_to_save = pair.Value.CreateCopy();
                node_to_save.RemoveValue("locked_parts");
                craft_nodes.AddNode("CRAFT", node_to_save);
            }
            nodes.AddNode("CraftData", craft_nodes);
            nodes.Save(cache_path);
        }

        private void load(){
            craft_data.Clear();
            ConfigNode nodes = ConfigNode.Load(cache_path);
            if(nodes.GetValue("craft_manager_version") == CraftManager.version){
                ConfigNode craft_nodes = nodes.GetNode("CraftData");
                foreach(ConfigNode node in craft_nodes.nodes){
                    try{
                        string path = sanitize_path(node.GetValue("path"));
                        if(!craft_data.ContainsKey(path)){
                            craft_data.Add(node.GetValue("path"), node);                        
                        }
                    } catch(Exception e){
                        CraftManager.log("failed to add " + node.GetValue("path") + "\n" + e.Message);
                    }
                }
            } else{
                CraftManager.log("cache was generated by another version of CraftManager, resetting cache");
            }
        }

        internal static IEnumerator<bool> load_cache(){
            yield return true;
            if(!File.Exists(CraftDataCache.cache_path)){
                CraftManager.log("pre-generating craft data cache");
                CraftData.load_craft_from_files(CraftManager.main_ui.active_save_dir);
            }else if(CraftData.cache == null){
                CraftData.cache = new CraftDataCache();                
            }
        }
    }
}

