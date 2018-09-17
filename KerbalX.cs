using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using KatLib;
using SimpleJSON;
using KXAPI;

namespace CraftManager
{

    public class KerbalX
    {

        internal static KerbalXAPI api = new KerbalXAPI("CraftManager", CraftManager.version);

        internal static bool enabled {
            get{ 
                return CraftManager.kerbalx_integration_enabled;
            }
        }
        internal static string loaded_craft_type = "";
        internal static List<Version> versions = new List<Version>();
        internal static Dictionary<Version, bool> v_toggle = new Dictionary<Version, bool>();
        internal static List<Version> selected_versions{
            get{
                return versions.FindAll(v => v_toggle[v]);
            }
        }
        internal static int download_queue_size = 0;

        internal static List<string> craft_styles = new List<string>(){
            "Ship", "Aircraft", "Spaceplane", "Lander", "Satellite", "Station", "Base", "Probe", "Rover", "Lifter" 
        };

        private static Dictionary<string, bool> selected_types_prev_state = null;
        internal static string bulk_download_log = "";
        internal static Vector2 log_scroll = new Vector2();

        internal static List<string> versions_list{
            get{ 
                List<string> version_list = new List<string>();
                for(int i = 0; i < versions.Count; i++){
                    version_list.Add(versions[i].ToString());
                }
                return version_list;
            }
        }
        internal static List<string> selected_versions_list{
            get{ 
                List<string> selected_version_list = new List<string>();
                for(int i = 0; i < selected_versions.Count; i++){
                    selected_version_list.Add(selected_versions[i].ToString());
                }
                return selected_version_list;
            }
        }

        internal delegate void DownloadCallback(ConfigNode craft_file);
        internal delegate void RemoteCraftMatcher();
        internal delegate void CraftByVersionCallback(Dictionary<string, Dictionary<int, Dictionary<string, string>>> cids_by_version, List<Version> versions);
        internal delegate void PartLookupCallback(Dictionary<string, string> identified_parts);


        internal static void select_all_versions(){
            foreach(Version v in versions){
                v_toggle[v] = true;
            }
            CraftManager.main_ui.filter_craft();
        }
        internal static void select_recent_versions(){
            for(int i = 0; i < versions.Count; i++){
                v_toggle[versions[i]] = i < 2;
            }
            CraftManager.main_ui.filter_craft();
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
            select_all_versions();

            CraftManager.status_info = "";
            CraftManager.main_ui.kerbalx_mode = true;
            if(selected_types_prev_state == null){
                selected_types_prev_state = new Dictionary<string, bool>(CraftManager.main_ui.selected_types);
            }
            CraftManager.main_ui.type_select_all(true);
            CraftManager.main_ui.filter_craft();
            CraftManager.main_ui.scroll_pos["main"] = new UnityEngine.Vector2(0,0);                        
        }


        internal static void ensure_ship_folders_exist(string save_dir){
            string save_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir);
            CraftManager.log("checking save folders in " + save_path);
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
            
        }

        internal static void bulk_download(Dictionary<int, Dictionary<string, string>> download_list, string save_dir, Callback callback){
            
            string save_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir);
            if(Directory.Exists(save_path)){
                ensure_ship_folders_exist(save_dir);

                if(download_list.Count > 0){
                    CraftManager.status_info = "Downloading craft from KerbalX...";
                    List<int> keys = new List<int>(download_list.Keys);
                    int id = keys[0];


                    Dictionary<string, string> craft_ref = download_list[id];
                    string type = craft_ref["type"];
                    string path = "";
                    if(type == "Subassembly"){
                        path = Paths.joined(save_path, "Subassemblies", craft_ref["name"] + ".craft");
                    } else{
                        path = Paths.joined(save_path, "Ships", type,   craft_ref["name"] + ".craft");
                    }
                    bulk_download_log += (String.IsNullOrEmpty(bulk_download_log) ? "" : "\n") + "downloading [" + (type=="Subassembly" ? "Sub" : type) + "]" + craft_ref["name"] + "...";

                    download_list.Remove(id); 

                    api.download_craft(id, (craft_file_string, code) =>{
                        if(code == 200){
                            ConfigNode craft = ConfigNode.Parse(craft_file_string);
                            craft.Save(path);
                            Thread.Sleep(1000); //This sleep is just to add a pause to reduce load on the site.
                            bulk_download_log += "Done";
                            log_scroll.y = 10000;

                            KerbalX.bulk_download(download_list, save_dir, callback);
                        }
                    });
                } else{
                    CraftManager.status_info = "";
                    callback();
                }
            }
        }

        internal static void get_craft_ids_by_version(CraftByVersionCallback callback){
            if(api.logged_in){
                KerbalX.api.fetch_existing_craft((resp, status_code) =>{                
                    if(status_code == 200){
                        Dictionary<string, Dictionary<int, Dictionary<string, string>>> craft_ids_by_version = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();
                        List<Version> version_list = new List<Version>();
                        foreach(KeyValuePair<int, Dictionary<string, string>> data in api.user_craft){
                            string v = data.Value["version"];
                            if(!craft_ids_by_version.ContainsKey(v)){
                                craft_ids_by_version.Add(v, new Dictionary<int, Dictionary<string, string>>());
                            }
                            version_list.AddUnique(new Version(v));

                            craft_ids_by_version[v].Add(int.Parse(data.Value["id"]), new Dictionary<string, string>{{"name", data.Value["name"]}, {"type", data.Value["type"] }});
                        }
                        version_list.Sort((x, y) => y.CompareTo(x));
                        callback(craft_ids_by_version, version_list);
                    }
                });
            }
        }

        internal static void find_matching_remote_craft(CraftData craft){
            RemoteCraftMatcher rcm = new RemoteCraftMatcher(() =>{
                if(api.user_craft != null){
                    List<int> list = new List<int>();
                    foreach(KeyValuePair<int, Dictionary<string, string>> pair in api.user_craft){
                        if(pair.Value["name"] == craft.name && pair.Value["type"] == craft.construction_type){
                            list.Add(pair.Key);
                        }
                    }
                    list.Sort();
                    craft.matching_remote_ids = list;
                }

            });

            if(api.logged_in && api.user_craft == null){
                api.fetch_existing_craft((resp, status_code) =>{                
                    if(status_code == 200){
                        rcm();
                    }
                });                    
            } else{
                rcm();
            }

        }

        internal static void lookup_parts(List<string> parts, PartLookupCallback callback){
            WWWForm part_data = new WWWForm();
            string part_json = "[\"" + String.Join("\",\"", parts.ToArray()) + "\"]";
            part_data.AddField("parts", part_json);
            Dictionary<string, string> identified_parts = new Dictionary<string, string>();
            CraftManager.status_info = "Looking up parts....";
            api.lookup_parts(part_data, (resp, code) => {
                if(code == 200){
                    JSONNode part_info = JSON.Parse(resp);
                    foreach(string part_name in parts){
                        if(!identified_parts.ContainsKey(part_name)){
                            identified_parts.Add(part_name, part_info[part_name]);
                        }
                    }
                    callback(identified_parts);                        
                }
                CraftManager.status_info = "";
            });
        }

        internal static void download(int id, DownloadCallback callback){            
            CraftManager.status_info = "Downloading craft from KerbalX...";
            api.download_craft(id, (craft_file_string, code) =>{
                if(code == 200){
                    ConfigNode craft = ConfigNode.Parse(craft_file_string);
                    check_download_queue();
                    callback(craft);
                }
                CraftManager.status_info = "";
            });
        }

        internal static void remove_from_download_queue(CraftData craft){
            KerbalX.api.remove_from_queue(craft.remote_id, (resp, code)=>{                    
                if(code==200){
                    CraftData.all_craft.Remove(craft);
                    CraftData.filtered.Remove(craft);
                    api.fetch_download_queue((craft_data, status_code) =>{
                        if(status_code == 200){
                            download_queue_size = craft_data.Count;                     
                        }
                        CraftManager.status_info = "";
                    });
                }
            });
        }

        internal static void fetch_existing_craft_info(){
            if(api.logged_in){
                CraftManager.status_info = "fetching craft info from KerbalX";
                for(int i = 0; i < CraftData.all_craft.Count; i++){
                    CraftData.all_craft[i].matching_remote_ids = null;
                }
                api.fetch_existing_craft((resp, status_code) =>{                                    
                    CraftManager.status_info = "";
                });
            }
        }

        internal static void check_download_queue(){
            if(api.logged_in){
                CraftManager.status_info = "checking KerbalX download queue";
                api.fetch_download_queue((craft_data, status_code) =>{
                    if(status_code == 200){
                        download_queue_size = craft_data.Count;
                    }
                    CraftManager.status_info = "";
                });
            }
        }

        internal static void load_remote_craft(){                 
            CraftManager.main_ui.select_sort_option("date_updated", false);
            load_users_craft();
        }

        internal static void load_users_craft(){
            CraftManager.status_info = "fetching your craft from KerbalX";
            loaded_craft_type = "users";
            api.fetch_existing_craft((resp, status_code) =>{                
                if(status_code == 200){
                    after_load_action(api.user_craft);
                }
                CraftManager.status_info = "";
            });
        }

        internal static void load_past_dowloads(){
            CraftManager.status_info = "fetching you past downloads from KerbalX";
            loaded_craft_type = "past_downloads";
            api.fetch_past_downloads((craft_data, status_code) =>{
                if(status_code == 200){
                    after_load_action(craft_data);
                }
                CraftManager.status_info = "";
            });
        }

        internal static void load_favourites(){
            CraftManager.status_info = "fetching your favourites from KerbalX";
            loaded_craft_type = "favourites";
            api.fetch_favoutite_craft((craft_data, status_code) =>{
                if(status_code == 200){
                    after_load_action(craft_data);
                }
                CraftManager.status_info = "";
            });
        }

        internal static void load_download_queue(){
            CraftManager.status_info = "fetching download queue from KerbalX";
            loaded_craft_type = "download_queue";
            api.fetch_download_queue((craft_data, status_code) =>{
                if(status_code == 200){
                    after_load_action(craft_data);
                }
                CraftManager.status_info = "";
            });
        }



        internal static void load_local(){
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
            loaded_craft_type = "";
            CraftManager.main_ui.refresh();
        }
    }
}

