using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using KatLib;


namespace CraftManager
{
    public class CMSettings
    {
        protected string settings_path = Paths.joined(CraftManager.ksp_root, "GameData", "CraftManager", "settings.cfg");
        protected Dictionary<string, string> settings = new Dictionary<string, string>();

        public string craft_sort;

        public CMSettings(){

            //default settings. These will populate settings.cfg if the file doesn't exist and also provides
            //a reference of which values to try and fetch from the confignode.
            settings.Add("KerbalX_integration_enabled", "True");
            settings.Add("craft_sort", "name");
            settings.Add("craft_sort_reverse", "False");
            settings.Add("sort_tags_by", "name");
            settings.Add("tag_filter_mode", "AND");
            settings.Add("exclude_stock_craft", "True");

            settings.Add("use_stock_toolbar", "True");
            settings.Add("replace_editor_load_button", "True");
            settings.Add("use_editor_key_shortcuts", "True");
            settings.Add("tag_states", "");

            if(File.Exists(settings_path)){
                ConfigNode settings_raw = ConfigNode.Load(settings_path);
                ConfigNode settings_data = settings_raw.GetNode("SETTINGS");
                List<string> keys = new List<string>(settings.Keys);
                foreach(string key in keys){
                    settings[key] = settings_data.GetValue(key);
                }
            } else{
                save();
            }

            CraftManager.kerbalx_integration_enabled = bool.Parse(get("KerbalX_integration_enabled"));
            CraftManager.use_stock_toolbar = bool.Parse(get("use_stock_toolbar"));
            CraftManager.replace_editor_load_button = bool.Parse(get("replace_editor_load_button"));
            CraftManager.use_editor_key_shortcuts = bool.Parse(get("use_editor_key_shortcuts"));


        }

        public string get(string key){
            if(settings.ContainsKey(key)){
                return settings[key];
            } else{
                return "";
            }
        }

        public void set(string key, string value){
            if(settings.ContainsKey(key)){
                settings.Remove(key);
            }
            settings.Add(key, value);
            save();
                
        }

        protected void save(){
            ConfigNode settings_data = new ConfigNode();
            ConfigNode settings_node = new ConfigNode();
            settings_data.AddNode("SETTINGS", settings_node);
                
            List<string> keys = new List<string>(settings.Keys);
            foreach(string key in keys){
                settings_node.AddValue(key, settings[key]);
            }
            settings_data.Save(settings_path);
        }

    }
}

