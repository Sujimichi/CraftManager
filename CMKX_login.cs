using System;
using System.Collections.Generic;
using UnityEngine;
using KatLib;
using SimpleJSON;


namespace CraftManager
{
    public delegate void AfterLoginAction();

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class CMKX_login : CMUI
    {

        public string username = "";
        public string password = "";
        public bool enable_login     = true; //used to toggle enabled/disabled state on login fields and button
        public bool login_failed     = false;//if true, displays login failed message and link to recover password on the site
        public bool login_successful = false;//if true, hides login field and shows logged in as and a logout button
        public bool modal_dialog = false;
        public string login_required_message = "";
        public bool show_cancel = false;
        public bool dialog_open = false;
        public bool window_retract = true;
        public bool initial_token_check_complete = false;
        public GUIStyle login_indicator = null;

        public AfterLoginAction after_login_action = () => {};

        public int count = 5;



        private void Start(){
            if(KerbalX.enabled){                
                enable_request_handler();
                window_title = "KerbalX::Login";
                window_pos = new Rect(-420f, 50, 420, 5);
                CraftManager.login_ui = this;
                enable_request_handler();
                //try to load a token from file and if present authenticate it with KerbalX.  if token isn't present or token authentication fails then show login fields.
                if(KerbalXAPI.logged_out()){
                    KerbalX.load_and_authenticate_token();   
                }

            }
        }

        protected override void WindowContent(int win_id) {            
            if(modal_dialog){                
                if(!dialog_open){
                    dialog_open = true;
                    ModalDialog dialog = gameObject.AddOrGetComponent<ModalDialog>();
                    dialog.dialog_pos = new Rect(Screen.width / 2 - 400 / 2, Screen.height / 3, 400f, 5f);
                    dialog.window_title = window_title;
                    dialog.content = new DialogContent(d =>{
                        login_content();                    
                    });
                    dialog.skin = CraftManager.skin;
                }
            } else{                
                section(400f, (w2) =>{
                    alt_window_style = skin.GetStyle("login.window");                    
                    GUILayout.BeginVertical("Window", width(400f), height(80f), GUILayout.ExpandHeight(true));
                    login_content();
                    GUILayout.EndVertical();
                    v_section(50f,(w) =>{
                        button("test", "button.login.toggle", () =>{
                            if(window_pos.x < -5){
                                window_retract = false;
                            }else if(window_pos.x >= -5){
                                window_retract = true;
                            }
                            initial_token_check_complete = false;
                        });
                        if(initial_token_check_complete && KerbalXAPI.logged_out()){
                            window_retract = false;
                        }

                        if(login_indicator == null || !enable_login){
                            login_indicator = "login.logging_in";
                        } else if(KerbalXAPI.logged_in()){                            
                            login_indicator = "login.logged_in";
                        }else if(KerbalXAPI.logged_out()){
                            login_indicator = "login.logged_out";
                        }

                        v_section(10f, 10f, login_indicator, (w3) => {
                            
                        });
                    });
                });
                if(window_retract && window_pos.x > -420f){
                    window_pos.x -= 10;
                } else if(!window_retract && window_pos.x < -5){
                    window_pos.x += 10;
                }
            }
        }


        protected void login_content(){
            if(!modal_dialog){
                skin = CraftManager.alt_skin;
            }
            section(400f, () =>{
                v_section(400f, (inner_width) =>{
                    if(!String.IsNullOrEmpty(login_required_message)){
                        label(login_required_message, "h2");
                    }

                    if (KerbalXAPI.logged_out()) {                  
                        gui_state(enable_login, () =>{                    
                            GUILayout.Label("Enter your KerbalX username and password");
                            section(() => {
                                label("username", width(70f));
                                username = GUILayout.TextField(username, 255, width(inner_width-85f));
                            });
                            section(() => {
                                label("password", width(70f));
                                password = GUILayout.PasswordField(password, '*', 255, width(inner_width-85f));
                            });
                            Event e = Event.current;
                            if (e.type == EventType.keyDown && e.keyCode == KeyCode.Return && !String.IsNullOrEmpty(username) && !String.IsNullOrEmpty(password)) {
                                KerbalX.login(username, password);
                            }
                        });
                    }else if (KerbalXAPI.logged_in()) {
                        label("You are logged in as " + KerbalXAPI.logged_in_as());
                    }
                    if (login_successful) {
                        section(() => {
                            label("KerbalX.key saved in KSP root", width(inner_width - 50f));
                            button("?", 20f, ()=>{
                                DryDialog dialog = show_dialog(post_login_message);
                                dialog.window_title = "KerbalX Token File";
                                dialog.window_pos = new Rect(window_pos.x + window_pos.width + 10, window_pos.y, 350f, 5);                        
                            });
                        });
                    }

                    section(()=>{                        
                        if (KerbalXAPI.logged_out()) {                
                            gui_state(enable_login, () =>{
                                button("Login", KerbalX.login);
                            });
                        } else {
                            button("Logout", KerbalX.logout);
                        }
                        if(show_cancel){
                            button("Cancel", ()=>{
                                close_dialog();
                                GameObject.Destroy(CraftManager.login_ui);
                            });
                        }
                    });

                    GUI.enabled = true; //just in case

                    if (login_failed) {
                        v_section(() => {
                            label("Login failed, check your things", "alert");
                            button("Forgot your password? Go to KerbalX to reset it.", ()=>{
                                Application.OpenURL("https://kerbalx.com/users/password/new");                        
                            });
                        });
                    }
                });
            });

            if(count == 0){
                CraftManager.login_ui.autoheight();
            } 
            count -= 1;
        }

        private void post_login_message(DryUI d){
            string message = "The KerbalX.key is a token that is used to authenticate you with the site." +
                "\nIt will also persist your login, so next time you start KSP you won't need to login again." +
                "\nIf you want to login to KerbalX from multiple KSP installs, copy the KerbalX.key file into each install.";
            label(message);
            button("OK", close_dialog);
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

        internal static void login(){
            login(CraftManager.login_ui.username, CraftManager.login_ui.password);
        }
        internal static void login(string username, string password){
            CraftManager.login_ui.enable_login = false; //disable interface while logging in to prevent multiple login clicks
            CraftManager.login_ui.login_failed = false;
            CraftManager.login_ui.login_indicator = null;
            KerbalXAPI.login(username, password, (resp, code) =>{
                if(code == 200){
                    var resp_data = JSON.Parse(resp);
                    CraftManager.login_ui.login_successful = true;
                    CraftManager.login_ui.after_login_action();
//                    CraftManager.login_ui.show_upgrade_available_message(resp_data["update_available"]); //triggers display of update available message if the passed string is not empty
                } else{
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
                //                var resp_data = JSON.Parse(resp);
                if(code == 200){                    
                    CraftManager.log("Logged in");
                    CraftManager.login_ui.after_login_action();
                    //                    KerbalX.login_gui.show_upgrade_available_message(resp_data["update_available"]); //triggers display of update available message if the passed string is not empty
                }else{
                    CraftManager.log("NOT Logged");
                }
                CraftManager.log(resp);
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

        public delegate void DownloadCallback(ConfigNode craft_file);
        public delegate void ActionCallback();

        private static void remote_action_wrapper(ActionCallback callback){            
            if(KerbalXAPI.logged_in()){
                callback();
            } else{
                CraftManager.main_ui.show_must_be_logged_in(() =>{
                    callback();
                    DryDialog.close();
                    if(CraftManager.login_ui != null){
                        GameObject.Destroy(CraftManager.login_ui);
                    }
                });
            }         
        }


        public static void download(int id, DownloadCallback callback){
            remote_action_wrapper(() =>{
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




        public static void load_remote_craft(){     
            remote_action_wrapper(() =>{
                CraftManager.main_ui.select_sort_option("date_updated", false);
                load_users_craft();
            });
        }

        public static void load_users_craft(){
            remote_action_wrapper(() =>{
                CraftManager.status_info = "fetching craft info from KerbalX";
                loaded_craft_type = "users";
                KerbalXAPI.fetch_existing_craft(() =>{                
                    after_load_action(KerbalXAPI.user_craft);
                });
            });
        }

        public static void load_past_dowloads(){
            remote_action_wrapper(() =>{
                CraftManager.status_info = "fetching craft info from KerbalX";
                loaded_craft_type = "past_downloads";
                KerbalXAPI.fetch_past_downloads(craft_data =>{
                    after_load_action(craft_data);
                });
            });
        }

        public static void load_favourites(){
            remote_action_wrapper(() =>{
                CraftManager.status_info = "fetching craft info from KerbalX";
                loaded_craft_type = "favourites";
                KerbalXAPI.fetch_favoutite_craft(craft_data =>{
                    after_load_action(craft_data);
                });
            });
        }

        public static void load_download_queue(){
            remote_action_wrapper(() =>{
                CraftManager.status_info = "fetching craft info from KerbalX";
                loaded_craft_type = "download_queue";
                KerbalXAPI.fetch_download_queue(craft_data =>{
                    after_load_action(craft_data);
                });
            });
        }

        public static void load_local(){
            CraftManager.main_ui.kerbalx_mode = false;
            CraftManager.main_ui.select_sort_option(CraftManager.settings.get("craft_sort"), false);
            CraftManager.main_ui.refresh();
        }



        private static void after_load_action(Dictionary<int, Dictionary<string, string>> craft_data){
            CraftManager.log("after_load_action called");
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





//        public static void fetch_existing_craft(){
//            CraftManager.status_info = "fetching craft info from KerbalX";
//            KerbalXAPI.fetch_existing_craft(()=>{
//                CraftManager.status_info = "";
//                CraftManager.log("fetched existing craft");
//                foreach(KeyValuePair<int, Dictionary<string, string>> pair in KerbalXAPI.user_craft){
//                    CraftManager.log(String.Join(", ", new List<string>(pair.Value.Values).ToArray()));
//                }
//            });
//        }

    }

}

