

//Built Against KSP 1.3.1
//build id = 01891
//2017-10-05_22-01-21
//Branch: master

/* TODO List
  - settings and settings UI
  - Tagders - Tags as folders, different mode which limits each craft to one tag and only one tag can be selected at at time.
  - KerbalX Integration

    TODO BUGS
    - description field does not save chars after new line. Newline used in game is some other char
    - Change to description doesn't update the editor description field
    FIXED:
    - can't set tags on stock craft
    - cant create tags when in all saves mode
    - fails to clear window locks when loading first craft
    - invalid operation exception when selecting tags. modifying data in foreach?
    - add tag dropdown menu gets wide right border if lots of tags (or maybe a long tag).
    - change blk click to check that it is the same craft that is selected on the second click (fixed in KatLib);
    - R and T shortcuts interfer with adding new tags, might remove those shortcuts (removed shortcuts)
    - when opened with ctrl+o click through prevention doesn't work until window is clicked
*/

using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using KSP.UI.Screens;

using KatLib;

namespace CraftManager
{

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class CraftManager : MonoBehaviour
    {
        //Settings
        internal static CMSettings settings;
        //These will have values set when new CMsettings is called in Awake
        internal static bool use_stock_toolbar;
        internal static bool replace_editor_load_button;
        internal static bool use_editor_key_shortcuts;


        //Interface Instances
        internal static CMBrowser main_ui = null;

        //Toolbar Buttons
        internal static ApplicationLauncherButton main_ui_toolbar_button   = null;

        //Helpers
        public static string ksp_root = Directory.GetParent(KSPUtil.ApplicationRootPath).FullName;

        //StyleSheet (initialised on first call to OnGUI)
        internal static GUISkin skin = null;


        private void Awake(){
            settings = new CMSettings();
            if(CraftManager.use_stock_toolbar){
                GameEvents.onGUIApplicationLauncherReady.Add(add_to_toolbar);
                GameEvents.onGUIApplicationLauncherDestroyed.Add(remove_from_toolbar);
            }
            GameEvents.onGameSceneLoadRequested.Add(scene_load_request);    
        }

        //Trigger the creation of custom Skin (copy of default skin with various custom styles added to it, see stylesheet.cs)
        private void OnGUI(){
            if(CraftManager.skin == null){
                CraftManager.skin = new StyleSheet(HighLogic.Skin).skin;
//                DryUI.skin = new StyleSheet(GUI.skin).skin; //works but isn't as clear.
            }
        }



        //Bind events to add buttons to the toolbar
        private void add_to_toolbar(){
            ApplicationLauncher.Instance.AddOnHideCallback(this.toolbar_on_hide);     //bind events to close guis when toolbar hides

            CraftManager.log("Adding buttons to toolbar");

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

        //remove any existing buttons from the toolbar
        private void remove_from_toolbar(){
            if(CraftManager.main_ui_toolbar_button){
                CraftManager.log("Removing buttons from toolbar");
                ApplicationLauncher.Instance.RemoveModApplication(CraftManager.main_ui_toolbar_button);
                CraftManager.main_ui_toolbar_button = null;
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
                    }
                }
            }
        }
    }
}