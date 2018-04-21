using System;
using System.IO;
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

        internal string username = "";
        internal string password = "";
        internal bool enable_login     = true; //used to toggle enabled/disabled state on login fields and button
        internal bool login_failed     = false;//if true, displays login failed message and link to recover password on the site
        internal bool login_successful = false;//if true, hides login field and shows logged in as and a logout button
        internal bool modal_dialog = false;
        internal string login_required_message = "";
        internal bool show_cancel = false;
        internal bool initial_token_check_complete = false;
        internal GUIStyle login_indicator = null;
        private bool dialog_open = false;
        private bool window_retract = true;

        private float window_out_pos = -15f;
        private float window_in_pos = -420f;
            

        internal AfterLoginAction after_login_action = () => {};

        private int count = 5;


        private void Start(){
            window_title = null;
            window_pos = new Rect(window_in_pos, 50, 420, 5);
            CraftManager.login_ui = this;
            if(KerbalX.enabled){                
                enable_request_handler();
                enable_request_handler();
                //try to load a token from file and if present authenticate it with KerbalX.  if token isn't present or token authentication fails then show login fields.
                if(KerbalXAPI.logged_out()){
                    KerbalX.load_and_authenticate_token();   
                }
            } else{
                GameObject.Destroy(CraftManager.login_ui);
            }
        }

        protected override void WindowContent(int win_id) {            
            if(modal_dialog){                
                if(!dialog_open){
                    dialog_open = true;
                    ModalDialog dialog = gameObject.AddOrGetComponent<ModalDialog>();
                    dialog.dialog_pos = new Rect(Screen.width / 2 - 450f / 2, Screen.height / 3, 450f, 5f);
                    dialog.window_title = window_title;
                    dialog.content = new DialogContent(d =>{
                        login_content(450f);                    
                    });
                    dialog.skin = CraftManager.skin;
                }
            } else{                
                section(400f, 5f, "login.container", (inner_width) =>{
                    alt_window_style = skin.GetStyle("login.window");                    
                    GUILayout.BeginVertical("Window", width(400f), height(100f), GUILayout.ExpandHeight(true));
                    login_content(400f);
                    GUILayout.EndVertical();
                    v_section(20f, w =>{
                        fspace();
                        if(login_indicator == null || !enable_login){
                            login_indicator = "login.logging_in";
                        } else if(KerbalXAPI.logged_in()){                            
                            login_indicator = "login.logged_in";
                        }else if(KerbalXAPI.logged_out()){
                            login_indicator = "login.logged_out";
                        }
                        label("K\ne\nr\nb\na\nl\nX", "centered", 10f);
                        label("", login_indicator);
                    }, (evt) => {
                        if(evt.single_click){
                            if(window_pos.x < window_out_pos){
                                window_retract = false;
                            }else if(window_pos.x >= window_out_pos){
                                window_retract = true;
                            }
                            initial_token_check_complete = false;
                        }
                    });
                });

                if(initial_token_check_complete && KerbalXAPI.logged_out()){
                    window_retract = false;
                }
                if(window_retract && window_pos.x > window_in_pos){
                    window_pos.x -= 10;
                } else if(!window_retract && window_pos.x < window_out_pos){
                    window_pos.x += 10;
                }
            }
        }


        protected void login_content(float content_width){
            if(!modal_dialog){
                skin = CraftManager.alt_skin;
            }
            section(content_width, 110f, () =>{
                v_section(content_width, (inner_width) =>{

                    if(!String.IsNullOrEmpty(login_required_message)){
                        label(login_required_message, "h2");
                    }

                    if (KerbalXAPI.logged_out()) {                  
                        gui_state(enable_login, () =>{                    
                            label("CraftManager - KerbalX.com login");
                            section(() => {
                                label("username", width(70f));
                                GUI.SetNextControlName("username_field");
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
                        if(!enable_login){
                            label("Logging in....", "h2");
                        }
                    }else if (KerbalXAPI.logged_in()) {
                        label("CraftManager has logged you into KerbalX.com");
                        label("Welcome back " + KerbalXAPI.logged_in_as());
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

            if(count >= 0){
                GUI.FocusControl("username_field");
                count -= 1;
            } 
        }

        private void post_login_message(DryUI d){
            string message = "The KerbalX.key is a token that is used to authenticate you with the site." +
                "\nIt will also persist your login, so next time you start KSP you won't need to login again." +
                "\nIf you want to login to KerbalX from multiple KSP installs, copy the KerbalX.key file into each install.";
            label(message);
            button("OK", close_dialog);
        }

        //Shows an upgrade available message after login if the server provides a upload available message string
        internal void show_upgrade_available_message(string message) {
            if (!String.IsNullOrEmpty(message)) {
                
                DryDialog dialog = show_dialog((d) => {
                    v_section(w => {
                        label("A new version of CraftManager is available");
                        label(message);
                        section("dialog.section", ()=>{
                            button("visit KerbalX to download the latest version", "hyperlink", ()=>{
                                Application.OpenURL(KerbalXAPI.url_to("mod"));
                            });                            
                        });
                        section(w2 => {                           
                            button("Remind me later", close_dialog);
                            button("Don't notify me about this update", ()=>{
                                KerbalXAPI.dismiss_current_update_notification();
                                close_dialog();
                                
                            });
                        });

                    });
                });
                dialog.window_title = "CraftManager - Update Available";
                dialog.window_pos = new Rect(window_pos.x + window_pos.width + 10, window_pos.y, 400f, 5);
            }
        }
    }
}

