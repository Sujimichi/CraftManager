using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using KatLib;
using SimpleJSON;

namespace CraftManager
{

    public class KerbalX
    {

        public static bool enabled {
            get{ 
                return CraftManager.kerbalx_integration_enabled;
            }
        }
        public static string loaded_craft_type = "";
        public static List<Version> versions = new List<Version>();
        public static Dictionary<Version, bool> v_toggle = new Dictionary<Version, bool>();
        public static List<Version> selected_versions{
            get{
                return versions.FindAll(v => v_toggle[v]);
            }
        }
        public static int download_queue_size = 0;

        public static List<string> craft_styles = new List<string>(){
            "Ship", "Aircraft", "Spaceplane", "Lander", "Satellite", "Station", "Base", "Probe", "Rover", "Lifter" 
        };

        private static Dictionary<string, bool> selected_types_prev_state = null;
        public static string bulk_download_log = "";
        public static Vector2 log_scroll = new Vector2();


        public delegate void DownloadCallback(ConfigNode craft_file);
        public delegate void ActionCallback();
        public delegate void RemoteCraftMatcher();
        public delegate void CraftByVersionCallback(Dictionary<string, List<int>> cids_by_version, List<Version> versions);


        internal static void login(){
            login(CraftManager.login_ui.username, CraftManager.login_ui.password);
        }
        internal static void login(string username, string password){
            CraftManager.log("logging in....");
            CraftManager.login_ui.enable_login = false; //disable interface while logging in to prevent multiple login clicks
            CraftManager.login_ui.login_failed = false;
            CraftManager.login_ui.login_indicator = null;
            KerbalXAPI.login(username, password, (resp, code) =>{
                if(code == 200){
                    var resp_data = JSON.Parse(resp);
                    CraftManager.log("Logged in");
                    CraftManager.login_ui.login_successful = true;
                    CraftManager.login_ui.after_login_action();
                    CraftManager.login_ui.show_upgrade_available_message(resp_data["update_available"]); //triggers display of update available message if the passed string is not empty
                } else{
                    CraftManager.log("NOT Logged in");
                    CraftManager.login_ui.login_failed = true;
                    CraftManager.login_ui.enable_login = true;
                }
                CraftManager.login_ui.enable_login = true;
                CraftManager.login_ui.autoheight();
                CraftManager.login_ui.password = "";

            });
        }

        //Check if Token file exists and if so authenticate it with KerbalX. Otherwise instruct login window to display login fields.
        internal static void load_and_authenticate_token(){
            CraftManager.log("logging in....");
            CraftManager.login_ui.enable_login = false;
            CraftManager.login_ui.login_indicator = null;
            KerbalXAPI.load_and_authenticate_token((resp, code) =>{
                if(code == 200){                    
                    var resp_data = JSON.Parse(resp);
                    CraftManager.log("Logged in");
                    CraftManager.login_ui.after_login_action();
                    CraftManager.login_ui.show_upgrade_available_message(resp_data["update_available"]); //triggers display of update available message if the passed string is not empty
                }else{
                    CraftManager.log("NOT Logged in");
                }
                CraftManager.login_ui.enable_login = true;
                CraftManager.login_ui.initial_token_check_complete = true;
                CraftManager.login_ui.autoheight();
            });
        }

        internal static void logout(){
            KerbalXAPI.logout((resp, code) =>{
                CraftManager.login_ui.enable_login = true;
                CraftManager.login_ui.login_successful = false;
                CraftManager.login_ui.username = "";
                CraftManager.login_ui.password = "";
                CraftManager.log("Logged out of KerbalX");
            });
        }


        public static void select_all_versions(){
            foreach(Version v in versions){
                v_toggle[v] = true;
            }
            CraftManager.main_ui.filter_craft();
        }
        public static void select_default_versions(){
            for(int i = 0; i < versions.Count; i++){
                v_toggle[versions[i]] = i < 2;
            }
            CraftManager.main_ui.filter_craft();
        }

        private static void if_logged_in_do(ActionCallback callback){            
            if(KerbalXAPI.logged_in()){
                callback();
            } else{
                CraftManager.main_ui.show_must_be_logged_in(() =>{
                    callback();
                    close_login_dialog();
                });
            }         
        }

        public static void close_login_dialog(){
            ModalDialog.close();
            if(CraftManager.login_ui != null){
                GameObject.Destroy(CraftManager.login_ui);
            }
        }


        private static void after_load_action(Dictionary<int, Dictionary<string, string>> craft_data){            
            CraftData.all_craft.Clear();
            versions.Clear(); v_toggle.Clear();
            foreach(KeyValuePair<int, Dictionary<string, string>> data in craft_data){
                Dictionary<string, string> craft = data.Value;
                new CraftData(data.Key, craft["url"], craft["name"], craft["type"], craft["version"], int.Parse(craft["part_count"]), int.Parse(craft["stages"]),
                    int.Parse(craft["crew_capacity"]), float.Parse(craft["cost"]), float.Parse(craft["mass"]), craft["created_at"], craft["updated_at"], craft["description"]
                );
                versions.AddUnique(new Version(craft["version"]));               
            }
            versions.Sort((x, y) => y.CompareTo(x));
            foreach(Version v in versions){
                v_toggle.Add(v, false);
            }
            select_default_versions();

            CraftManager.status_info = "";
            CraftManager.main_ui.kerbalx_mode = true;
            if(selected_types_prev_state == null){
                selected_types_prev_state = new Dictionary<string, bool>(CraftManager.main_ui.selected_types);
            }
            CraftManager.main_ui.type_select_all(true);
            CraftManager.main_ui.filter_craft();
            CraftManager.main_ui.scroll_pos["main"] = new UnityEngine.Vector2(0,0);                        
        }




        public static void bulk_download(List<int> ids, string save_dir, Callback callback){

            string save_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir);
            if(Directory.Exists(save_path)){
                if(!Directory.Exists(Paths.joined(save_path, "Subassemblies"))){
                    Directory.CreateDirectory(Paths.joined(save_path, "Subassemblies"));
                }
                if(!Directory.Exists(Paths.joined(save_path, "Ships"))){
                    Directory.CreateDirectory(Paths.joined(save_path, "Ships"));
                }
                if(!Directory.Exists(Paths.joined(save_path, "Ships", "VAB"))){
                    Directory.CreateDirectory(Paths.joined(save_path, "Ships", "VAB"));
                }
                if(!Directory.Exists(Paths.joined(save_path, "Ships", "SPH"))){
                    Directory.CreateDirectory(Paths.joined(save_path, "Ships", "SPH"));
                }

                if(ids.Count > 0){
                    int id = ids[0];                
                    ids.Remove(id);
                    var craft_ref = KerbalXAPI.user_craft[id];
                    string type = craft_ref["type"];
                    string path = "";
                    bulk_download_log += "\ndownloading [" + (type=="Subassembly" ? "Sub" : type) + "]" + craft_ref["name"] + "...";
                    if(type == "Subassembly"){
                        path = Paths.joined(save_path, "Subassemblies", craft_ref["name"] + ".craft");
                    } else{
                        path = Paths.joined(save_path, "Ships", type, craft_ref["name"] + ".craft");
                    }

                    KerbalXAPI.download_craft(id, (craft_file_string, code) =>{
                        if(code == 200){
                            ConfigNode craft = ConfigNode.Parse(craft_file_string);
                            craft.Save(path);
                            bulk_download_log += "Done";
                            log_scroll.y = 10000;
                            KerbalX.bulk_download(ids, save_dir, callback);
                        }
                    });
                } else{
                    callback();
                }
            }
        }

        public static void get_craft_ids_by_version(CraftByVersionCallback callback){
            if(KerbalXAPI.logged_in()){
                KerbalXAPI.fetch_existing_craft(() =>{                
                    Dictionary<string, List<int>> craft_ids_by_version = new Dictionary<string, List<int>>();
                    List<Version> version_list = new List<Version>();
                    foreach(KeyValuePair<int, Dictionary<string, string>> data in KerbalXAPI.user_craft){
                        string v = data.Value["version"];
                        if(!craft_ids_by_version.ContainsKey(v)){
                            craft_ids_by_version.Add(v, new List<int>());
                        }
                        version_list.AddUnique(new Version(v));
                        craft_ids_by_version[v].Add(int.Parse(data.Value["id"]));
                    }
                    version_list.Sort((x, y) => y.CompareTo(x));
                    callback(craft_ids_by_version, version_list);
                });
            }
        }

        public static void find_matching_remote_craft(CraftData craft){
            RemoteCraftMatcher rcm = new RemoteCraftMatcher(() =>{
                if(KerbalXAPI.user_craft != null){
                    List<int> list = new List<int>();
                    foreach(KeyValuePair<int, Dictionary<string, string>> pair in KerbalXAPI.user_craft){
                        if(pair.Value["name"] == craft.name && pair.Value["type"] == craft.construction_type){
                            list.Add(pair.Key);
                        }
                    }
                    list.Sort();
                    craft.matching_remote_ids = list;
                }

            });

            if(KerbalXAPI.logged_in() && KerbalXAPI.user_craft == null){
                KerbalXAPI.fetch_existing_craft(() =>{                
                    rcm();
                });                    
            } else{
                rcm();
            }

        }

        public static void download(int id, DownloadCallback callback){
            if_logged_in_do(() =>{
                CraftManager.status_info = "Downloading craft from KerbalX...";
                KerbalXAPI.download_craft(id, (craft_file_string, code) =>{
                    if(code == 200){
                        ConfigNode craft = ConfigNode.Parse(craft_file_string);
                        CraftManager.status_info = "";
                        callback(craft);
                    }
                });
            });
        }

        public static void fetch_existing_craft_info(){
            if(KerbalXAPI.logged_in()){
                CraftManager.status_info = "fetching craft info from KerbalX";
                for(int i = 0; i < CraftData.all_craft.Count; i++){
                    CraftData.all_craft[i].matching_remote_ids = null;
                }
                KerbalXAPI.fetch_existing_craft(() =>{
                    CraftManager.status_info = "";
                });
            }
        }

        public static void check_download_queue(){
            if(KerbalXAPI.logged_in()){
                CraftManager.status_info = "checking KerbalX download queue";
                KerbalXAPI.fetch_download_queue(craft_data =>{
                    download_queue_size = craft_data.Count;
                    CraftManager.status_info = "";
                });
            }
        }

        public static void load_remote_craft(){     
            if_logged_in_do(() =>{
                CraftManager.main_ui.select_sort_option("date_updated", false);
                load_users_craft();
            });
        }

        public static void load_users_craft(){
            if_logged_in_do(() =>{
                CraftManager.status_info = "fetching your craft from KerbalX";
                loaded_craft_type = "users";
                KerbalXAPI.fetch_existing_craft(() =>{                
                    after_load_action(KerbalXAPI.user_craft);
                });
            });
        }

        public static void load_past_dowloads(){
            if_logged_in_do(() =>{
                CraftManager.status_info = "fetching you past downloads from KerbalX";
                loaded_craft_type = "past_downloads";
                KerbalXAPI.fetch_past_downloads(craft_data =>{
                    after_load_action(craft_data);
                });
            });
        }

        public static void load_favourites(){
            if_logged_in_do(() =>{
                CraftManager.status_info = "fetching your favourites from KerbalX";
                loaded_craft_type = "favourites";
                KerbalXAPI.fetch_favoutite_craft(craft_data =>{
                    after_load_action(craft_data);
                });
            });
        }

        public static void load_download_queue(){
            if_logged_in_do(() =>{
                CraftManager.status_info = "fetching download queue from KerbalX";
                loaded_craft_type = "download_queue";
                KerbalXAPI.fetch_download_queue(craft_data =>{
                    after_load_action(craft_data);
                });
            });
        }



        public static void load_local(){
            CraftManager.main_ui.kerbalx_mode = false;
            CraftManager.main_ui.select_sort_option(CraftManager.settings.get("craft_sort"), false);
            CraftManager.main_ui.selected_types = selected_types_prev_state;
            selected_types_prev_state = null;
            CraftManager.main_ui.selected_type_count = 0;
            foreach(KeyValuePair<string, bool> pair in CraftManager.main_ui.selected_types){
                if(pair.Value == true){
                    CraftManager.main_ui.selected_type_count += 1;
                }
            }
            CraftManager.main_ui.refresh();
        }
    }
}

