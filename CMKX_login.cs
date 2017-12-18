using System;
using System.Collections.Generic;

//using KatLib;

namespace CraftManager
{

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class CMKX_login : CMUI
    {

        private void Start(){
            if(CraftManager.kerbalx_integration_enabled){
            enable_request_handler();

                //try to load a token from file and if present authenticate it with KerbalX.  if token isn't present or token authentication fails then show login fields.
                if(KerbalXAPI.logged_out()){
                    CraftManager.load_and_authenticate_token();   
                }
            }
        }


    }

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

        public static void load_remote_craft(){
            CraftManager.main_ui.select_sort_option("date_updated", false);
            load_users_craft();
        }

        public static void load_users_craft(){
            CraftManager.status_info = "fetching craft info from KerbalX";
            loaded_craft_type = "users";
            KerbalXAPI.fetch_existing_craft(() =>{                
                after_load_action(KerbalXAPI.user_craft);
            });
        }

        public static void load_past_dowloads(){
            CraftManager.status_info = "fetching craft info from KerbalX";
            loaded_craft_type = "past_downloads";
            KerbalXAPI.fetch_past_downloads(craft_data =>{
                after_load_action(craft_data);
            });
        }

        public static void load_favourites(){
            CraftManager.status_info = "fetching craft info from KerbalX";
            loaded_craft_type = "favourites";
            KerbalXAPI.fetch_favoutite_craft(craft_data =>{
                after_load_action(craft_data);
            });
        }

        public static void load_download_queue(){
            CraftManager.status_info = "fetching craft info from KerbalX";
            loaded_craft_type = "download_queue";
            KerbalXAPI.fetch_download_queue(craft_data =>{
                after_load_action(craft_data);
            });
        }

        private static void after_load_action(Dictionary<int, Dictionary<string, string>> craft_data){
            CraftData.all_craft.Clear();
            versions.Clear(); v_toggle.Clear();
            foreach(KeyValuePair<int, Dictionary<string, string>> data in craft_data){
                Dictionary<string, string> craft = data.Value;
                new CraftData(data.Key, craft["url"], craft["name"], craft["type"], craft["version"], int.Parse(craft["part_count"]), int.Parse(craft["stages"]),
                    int.Parse(craft["crew_capacity"]), float.Parse(craft["cost"]), float.Parse(craft["mass"]), craft["created_at"], craft["updated_at"]
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
            CraftManager.main_ui.filter_craft();
            CraftManager.main_ui.scroll_pos["main"] = new UnityEngine.Vector2(0,0);                        
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

        public static void load_local(){
            CraftManager.main_ui.kerbalx_mode = false;
            CraftManager.main_ui.select_sort_option(CraftManager.settings.get("craft_sort"), false);
            CraftManager.main_ui.refresh();
        }

        public static void fetch_existing_craft(){
            CraftManager.status_info = "fetching craft info from KerbalX";
            KerbalXAPI.fetch_existing_craft(()=>{
                CraftManager.status_info = "";
                CraftManager.log("fetched existing craft");
                foreach(KeyValuePair<int, Dictionary<string, string>> pair in KerbalXAPI.user_craft){
                    CraftManager.log(String.Join(", ", new List<string>(pair.Value.Values).ToArray()));
                }
            });
        }

    }

}

