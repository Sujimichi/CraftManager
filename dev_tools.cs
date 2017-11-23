using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using KSP;
using KSP.UI;

using KatLib;

namespace CraftManager
{
  
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class JumpStart : MonoBehaviour
    {
        public bool autostart = true;
        public string save_name = "kx_dev";
//        public string mode = "spacecenter";
        public string mode = "editor";
        public string craft_name = "testy";

        public void Start(){

            if(autostart){
                HighLogic.SaveFolder = save_name;
                DebugToolbar.toolbarShown = true;

                if(mode == "editor"){
                    var editor = EditorFacility.SPH;
                    GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
                    if(craft_name != null || craft_name != ""){                                           
                        string path = Paths.joined(KSPUtil.ApplicationRootPath, "saves", save_name, "Ships", "SPH", craft_name + ".craft");
                        EditorDriver.StartAndLoadVessel(path, editor);
                    } else{
                        EditorDriver.StartEditor(editor);
                    }
                } else if(mode == "spacecenter"){
                    HighLogic.LoadScene(GameScenes.SPACECENTER);
                }else if(mode == "flight"){
                    FlightDriver.StartAndFocusVessel("quicksave", 1);
                }

            }
        }
    }

    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class KerbalXConsole : DryUI
    {
        private void Start(){
            window_title = "CM::Konsole";
            window_pos = new Rect(0, 0, 310, 5);
            prevent_click_through = true;
            visible = true;
        }
        
        
        protected override void WindowContent(int win_id){
//            label("sup bitches");


            if(GUILayout.Button("reset cache")){
                CraftData.cache = null;                

            }
                
        }

    }

}

