//Built Against KSP 1.8.0
//build id = 02686
//2019-10-14 22:04:12 EDT

using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace CraftManager
{
    
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class CraftManager : MonoBehaviour
    {

        public static string version = "1.2.0";
        public static Version game_version = new Version(Versioning.GetVersionString());

        //Settings
        internal static CMSettings settings;
        //These will have values set when new CMsettings is called in Awake
        internal static bool kerbalx_integration_enabled;
        internal static bool replace_editor_load_button;
        internal static bool use_editor_key_shortcuts;

        internal static string screenshot_dir;

        //Interface Instances
        internal static CMBrowser main_ui = null;
//        internal static CMKX_login login_ui = null;
        internal static GrabImage camera = null;
        internal static SettingsUI settings_ui = null;
        internal static HelpUI help_ui = null;

        //Toolbar Buttons
        internal static ApplicationLauncherButton main_ui_toolbar_button   = null;
        internal static ApplicationLauncherButton quick_tag_toolbar_button = null;

        //StyleSheet (initialised on first call to OnGUI)
        internal static GUISkin skin = null;
        internal static GUISkin alt_skin = null;

        //other
        public static string ksp_root { get { return Directory.GetParent(KSPUtil.ApplicationRootPath).FullName; } }
        public static string status_info = "";
        

        private void Awake(){
            settings = new CMSettings();

            bool using_toolbar = false;

            if(bool.Parse(settings.get("use_stock_toolbar"))){
                GameEvents.onGUIApplicationLauncherReady.Add(add_main_icon_to_toolbar);
                using_toolbar = true;
            }
            if(bool.Parse(settings.get("show_quick_tag_on_toolbar"))){                
                GameEvents.onGUIApplicationLauncherReady.Add(add_quick_tag_icon_to_toolbar);
                using_toolbar = true;
            }
            if(using_toolbar){
                GameEvents.onGUIApplicationLauncherDestroyed.Add(remove_from_toolbar);                
            }

            GameEvents.onGameSceneLoadRequested.Add(scene_load_request);    
        }

        private void Start(){
            if(KerbalX.enabled){
                KerbalX.api.login();
            }
        }

        //Bind events to add buttons to the toolbar
        private void add_main_icon_to_toolbar(){
            ApplicationLauncher.Instance.AddOnHideCallback(this.toolbar_on_hide);     //bind events to close guis when toolbar hides
            CraftManager.log("Adding main icon to toolbar");
            if(!CraftManager.main_ui_toolbar_button){
                CraftManager.main_ui_toolbar_button = ApplicationLauncher.Instance.AddModApplication(
                    toggle_main_ui, toggle_main_ui, 
                    main_btn_hover_on, main_btn_hover_off, 
                    null, null, 
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
                    StyleSheet.assets["ui_toolbar_btn"]
                );
            }
        }

        private void add_quick_tag_icon_to_toolbar(){
            
            if(!CraftManager.quick_tag_toolbar_button){
                CraftManager.quick_tag_toolbar_button = ApplicationLauncher.Instance.AddModApplication(
                    toggle_quick_tag, toggle_quick_tag, 
                    quick_tag_bttn_hover_on, quick_tag_bttn_hover_off, 
                    null, null, 
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, 
                    StyleSheet.assets["tags_toolbar_icon"]
                );
            }
        }


        //remove any existing buttons from the toolbar
        private void remove_from_toolbar(){
            if(CraftManager.main_ui_toolbar_button || CraftManager.quick_tag_toolbar_button){
                CraftManager.log("Removing buttons from toolbar");
            }
            if(CraftManager.main_ui_toolbar_button){
                ApplicationLauncher.Instance.RemoveModApplication(CraftManager.main_ui_toolbar_button);
                CraftManager.main_ui_toolbar_button = null;
            }
            if(CraftManager.quick_tag_toolbar_button){
                ApplicationLauncher.Instance.RemoveModApplication(CraftManager.quick_tag_toolbar_button);
                CraftManager.quick_tag_toolbar_button = null;
            }
        }

        //triggered by scene load, calls removal of the buttons
        private void scene_load_request(GameScenes scene){
            remove_from_toolbar();
            CraftData.cache = null;
            CraftData.save_state = 0;

            if(CraftManager.main_ui){
                GameEvents.onEditorShipModified.Remove(CraftManager.main_ui.on_ship_modified);
                GameEvents.onEditorRestart.Remove(CraftManager.main_ui.on_editor_restart);
            }

        }

        //Button Actions

        //Action for main interface button
        private void toggle_main_ui(){
            if(CraftManager.main_ui){
                CraftManager.main_ui.toggle();
            } else{
                CraftManager.log("Main UI has not been started");
            }
        }
        private void toggle_quick_tag(){
            if(QuickTag.instance){
                QuickTag.close();
            } else{                
                CraftManager.main_ui.open_quick_tag();
            }
        }

        //triggered when the application launcher hides, used to teardown open GUIs
        private void toolbar_on_hide(){
            if(CraftManager.main_ui){
                GameObject.Destroy(CraftManager.main_ui);
            }
        }

        internal static void log(string msg){
            Debug.Log("[CM] " + msg);
        }


        //Button hover actions
        private void main_btn_hover_on(){
            CraftManager.main_ui_toolbar_button.SetTexture(StyleSheet.assets["ui_toolbar_btn_hover"]);
        }
        private void main_btn_hover_off(){
            CraftManager.main_ui_toolbar_button.SetTexture(StyleSheet.assets["ui_toolbar_btn"]);
        }
        private void quick_tag_bttn_hover_on(){
            CraftManager.quick_tag_toolbar_button.SetTexture(StyleSheet.assets["tags"]); //TODO Change these icons
        }
        private void quick_tag_bttn_hover_off(){
            CraftManager.quick_tag_toolbar_button.SetTexture(StyleSheet.assets["tags_toolbar_icon"]);
        }
    }


    //Adds keyboard shortcuts to the Editor ctrl+o = open craft manager window, ctrl+n = new craft, ctrl+s = save current craft
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CM_KeyShortCuts : MonoBehaviour
    {
        public void Update(){
            if(CraftManager.use_editor_key_shortcuts && !CraftManager.main_ui.visible){
                if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)){
                    if(Input.GetKeyDown(KeyCode.O)){
                        CraftManager.main_ui.show();
                    } else if(Input.GetKeyDown(KeyCode.S)){
                        if(EditorLogic.fetch.saveBtn.enabled){
                            EditorLogic.fetch.saveBtn.onClick.Invoke();
                            ScreenMessages.PostScreenMessage("Craft Saved!");
                        }
                    } else if(Input.GetKeyDown(KeyCode.N)){
                        EditorLogic.fetch.newBtn.onClick.Invoke();
                    } else if(Input.GetKeyDown(KeyCode.T)){
                        if(QuickTag.instance == null){
                            QuickTag.open(gameObject);
                        } else{
                            QuickTag.close();
                        }
                    }
                }
            }
        }
    }

    //Translate.this_string("some string")
    //if the given string contains #autoLOC then it will attempt to lookup the translation and return that, 
    //otherwise it will just return the given stirng.
    //Translate.this_string("some regular string") -> "some regular string"
    //Translate.this_string("#autoLOC_501174") -> "a string found in a language file that coresponds to #autoLOC_501174"
    internal class Translate
    {
        internal static string this_string(string look_up){
            if(look_up.Contains("#autoLOC")){
                string translated_name;
                if(KSP.Localization.Localizer.TryGetStringByTag(look_up, out translated_name)){
                    look_up = translated_name;
                }
            }
            return look_up;
        }
    }
}