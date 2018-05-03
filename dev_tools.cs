//using System;
//using System.Linq;
//using System.Globalization;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.IO;
//using UnityEngine;
//using KSP;
//using KSP.UI;
//
//using KatLib;
//
//namespace CraftManager
//{
//  
//    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
//    public class DevTools : MonoBehaviour
//    {
//        public static bool autostart = false;
//        public string save_name = "kx_dev";
////        public string mode = "spacecenter";
//        public string mode = "editor";
//        public string craft_name = "testy";
//
//        public void Start(){
//
//            if(autostart){
//                HighLogic.SaveFolder = save_name;
//
//                if(mode == "editor"){
//                    var editor = EditorFacility.SPH;
//                    GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
//                    if(craft_name != null || craft_name != ""){                                           
//                        string path = Paths.joined(KSPUtil.ApplicationRootPath, "saves", save_name, "Ships", "SPH", craft_name + ".craft");
//                        EditorDriver.StartAndLoadVessel(path, editor);
//                    } else{
//                        EditorDriver.StartEditor(editor);
//                    }
//                } else if(mode == "spacecenter"){
//                    HighLogic.LoadScene(GameScenes.SPACECENTER);
//                }else if(mode == "flight"){
//                    FlightDriver.StartAndFocusVessel("quicksave", 1);
//                }
//
//            }
//        }
//    }
//}
//
