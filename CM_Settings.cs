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
            settings.Add("replace_editor_load_button", "True");
            settings.Add("use_stock_toolbar", "True");
            settings.Add("use_editor_key_shortcuts", "True");
            settings.Add("screenshot_dir", "<ksp_install>/Screenshots");

            settings.Add("ask_to_populate_new_save", "True");

            settings.Add("exclude_stock_craft", "True");
            settings.Add("craft_sort", "name");
            settings.Add("craft_sort_reverse", "False");
            settings.Add("sort_tags_by", "name");
            settings.Add("tag_filter_mode", "AND");
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
            CraftManager.replace_editor_load_button = bool.Parse(get("replace_editor_load_button"));
            CraftManager.use_stock_toolbar = bool.Parse(get("use_stock_toolbar"));
            CraftManager.use_editor_key_shortcuts = bool.Parse(get("use_editor_key_shortcuts"));

            CraftManager.screenshot_dir = get("screenshot_dir");
            CraftManager.screenshot_dir = CraftManager.screenshot_dir.Replace("<ksp_install>", KSPUtil.ApplicationRootPath);

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

    public class SettingsUI : CMUI
    {
        bool opt = false;
        float lhs_width = 600;
        float rhs_width = 80;
        CMSettings settings = CraftManager.settings;
        string new_screenshot_location = "";
        string setting_error_open_opts = "";
        string setting_error_screenshot_dir = "";

        private void Start(){
            float inner_width = lhs_width + rhs_width;
            window_title = "Craft Manager Settings";
            if(CraftManager.main_ui != null){                
                window_pos = new Rect(CraftManager.main_ui.window_pos.x + CraftManager.main_ui.window_pos.width / 2 - inner_width / 2, CraftManager.main_ui.window_pos.y + 100, inner_width, 5);
            } else{
                window_pos = new Rect(Screen.width/2 - inner_width/2, Screen.height/3, inner_width, 5);
            }
            CraftManager.settings_ui = this;
            new_screenshot_location = settings.get("screenshot_dir");
        }

        protected override void WindowContent(int win_id) { 

            v_section("dialog.section", () =>{
                setting_section("KerbalX_integration_enabled", "KerbalX.com Integration", "Enabled", "Enable", 
                    "Enables you to upload and download craft from KerbalX.com", 
                    "(requires a KerbalX.com account & enables the mod to make requests to KerbalX.com)"
                );
            });
            v_section("dialog.section", () =>{
                setting_section("replace_editor_load_button", "Replace load button", 
                    "The open button in the editors will open Craft Manager instead of the stock craft browser.", 
                    "(you need to exit and re-enter the editors for this to take effect)"
                );
                setting_section("use_stock_toolbar", "Use the Stock Toolbar",
                    "Have an icon in the stock toolbar to open Craft Manager",
                    "(you need to restart KSP to see changes)"
                );
                setting_section("use_editor_key_shortcuts", "Enable Editor Keyboard shortcuts",
                    "ctrl+o - Open Craft Manager, ctrl+n - new craft/clear editor, ctrl+s - save current craft"
                );
            });
            if(!String.IsNullOrEmpty(setting_error_open_opts)){
                label(setting_error_open_opts, "error");
            }

            v_section("dialog.section", () =>{
                string prev_new_screenshot_location = new_screenshot_location;
                section(()=>{
                    label("Screenshot Folder:", "h2.tight");
                    new_screenshot_location = GUILayout.TextField(new_screenshot_location);                    
                });
                section(()=>{
                    label("enter the full path to your Screenshot folder,\nor use a relative path in this KSP install by starting with '<ksp_install>'");
                    fspace();
                    button("use default", ()=>{
                        new_screenshot_location = "<ksp_install>/Screenshots";
                    });
                });
                    
                if(new_screenshot_location != prev_new_screenshot_location){
                    setting_error_screenshot_dir = "";
                    string path = new_screenshot_location.Replace("<ksp_install>", KSPUtil.ApplicationRootPath);
                    if(Directory.Exists(path)){
                        settings.set("screenshot_dir", new_screenshot_location);
                        CraftManager.settings = new CMSettings(); //re-initialize settings so updated values are set on CraftManager static variables
                    }else{
                        setting_error_screenshot_dir = "unable to find directory: "+ path;
                    }
                }
            });
            if(!String.IsNullOrEmpty(setting_error_screenshot_dir)){
                label(setting_error_screenshot_dir, "error");                
            }

            section(() =>{
                fspace();
                button("close", close);
            });        
        }

        public void setting_section(string setting_name, string label_text, string note){
            setting_section(setting_name, label_text, "Yes", "Yes", note, "");
        }
        public void setting_section(string setting_name, string label_text, string note, string sub_note){
            setting_section(setting_name, label_text, "Yes", "Yes", note, sub_note);
        }
        public void setting_section(string setting_name, string label_text, string active_text, string inactive_text, string note = "", string sub_note = ""){
            section((w2) =>{
                v_section(lhs_width, (w) =>{
                    label(label_text, "h2.tight");
                    if(!String.IsNullOrEmpty(note)){                        
                        label(note, "compact");
                    }
                    if(!String.IsNullOrEmpty(sub_note)){                        
                        label(sub_note, "small.compact");
                    }
                });
                v_section(rhs_width, (w) =>{
                    opt = bool.Parse(settings.get(setting_name));
                    button(opt ? active_text : inactive_text, "button" + (opt ? ".down" : ""), () =>{
                        setting_error_open_opts = "";
                        settings.set(setting_name, (!opt).ToString());

                        if(bool.Parse(settings.get("replace_editor_load_button")) == false && bool.Parse(settings.get("use_stock_toolbar")) == false && bool.Parse(settings.get("use_editor_key_shortcuts")) == false){
                            settings.set("replace_editor_load_button", true.ToString());
                            setting_error_open_opts = "You need to have at least 1 of the above three options selected";
                        }
                        CraftManager.settings = new CMSettings(); //re-initialize settings so updated values are set on CraftManager static variables
                    });
                });
            });   
        }

        public static void open(GameObject go){           
            go.AddOrGetComponent<SettingsUI>();
        }

        public void close(){            
            GameObject.Destroy(CraftManager.settings_ui);            
        }
    }



}

