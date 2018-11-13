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
//using System.Diagnostics;
//using System.Collections.Generic;
//
//namespace CraftManager
//{
//
//    internal class Timer
//    {
//
//        internal static Stopwatch stopwatch = null;
//        internal static List<string> data = new List<string>();
//
//
//        internal static void start(){
//            data.Clear();
//            stopwatch = new Stopwatch();
//            stopwatch.Start();
//        }
//
//        internal static void split(string msg = ""){
//            stopwatch.Stop();
//            data.Add(stopwatch.Elapsed.ToString() + " " + msg);
//            stopwatch = new Stopwatch();
//            stopwatch.Start();
//        }
//
//        internal static void stop(){
//            stopwatch.Stop();
//        }
//
//        internal static void clear(){
//            data.Clear();
//        }
//
//        internal static void show(){
//            string output = "";
//            foreach(string t in data){
//                output += "\n" + t;
//            }
//            CraftManager.log(output);
//        }
//
//        internal delegate void TimerBlock();
//        internal static void time(string message, TimerBlock timer_block){
//            stopwatch = new Stopwatch();
//            stopwatch.Start();
//            timer_block();
//            stopwatch.Stop();
//            CraftManager.log(message + "\ntime: " + stopwatch.Elapsed.ToString());
//        }
//
//
//
//
//    }
//}

//[KSPAddon(KSPAddon.Startup.EditorAny, false)]
//public class GeoCacheTest : CMUI
//{
//    
//    private string search_string;
//    private int last_id;
//    private void Start(){
//        visible = true;    
//    }
//    
//    protected override void WindowContent(int win_id){
//        button("list_caches", () =>{
//            KerbalX.api.fetch_geo_cache_list((resp, code) => {
//                Debug.Log(resp);
//            });
//        });
//        
//        search_string = GUILayout.TextField(search_string);
//        button("search", () =>{
//            WWWForm search_params = new WWWForm();
//            search_params.AddField("name", search_string);
//            
//            KerbalX.api.search_geo_caches(search_params, (resp, code) => {
//                Debug.Log(resp);
//            }); 
//        });
//        
//        button("upload", () =>{
//            
//            ConfigNode geo_cache = new ConfigNode();
//            ConfigNode sub_node = new ConfigNode();
//            geo_cache.name = "GEO_CACHE";
//            geo_cache.AddValue("some attr", "some value");
//            geo_cache.AddValue("some other attr", "some other value");
//            geo_cache.AddNode("SUB_DATA", sub_node);
//            sub_node.AddValue("sub attr", "sub value");
//            
//            
//            WWWForm form = new WWWForm();
//            form.AddField("name", "foobar");
//            form.AddField("coordinates", "someplace");
//            form.AddField("file", geo_cache.ToString());
//            
//            KerbalX.api.upload_geo_cache(form, (resp, code) => {                    
//                if(code == 200){
//                    //example resp: "{\"created\":true,\"id\":8}"
//                    int database_id = int.Parse(JSON.Parse(resp)["id"]); ﻿   
//                }else if(code == 422){ //unable to save geo_cache (ie invalid data).
//                    //example resp: "{\"created\":false,\"errors\":\"unable to save because reasons\"}"
//                    Debug.Log(JSON.Parse(resp)["errors"]);
//                }else{
//                    Debug.Log("unknown error: " + resp);
//                }    
//            });
//        });
//        
//        
//        button("get latest id", () =>{
//            KerbalX.api.fetch_geo_cache_list((resp, code) => {
//                if(code == 200){
//                    var data = JSON.Parse(resp);
//                    Debug.Log("cache count: " + data.Count);
//                    last_id = int.Parse(data[data.Count-1]["id"]);                        
//                }
//            });                
//        });
//        label("last id: " + last_id);
//        
//        button("fetch latest", () =>{                
//            KerbalX.api.fetch_geo_cache(last_id, (geo_cache, status_code) => {
//                Debug.Log(geo_cache); 
//                ConfigNode geo = ConfigNode.Parse(geo_cache);
//            });            
//        });
//        
//        button("update latest", () =>{
//            ConfigNode geo_cache = new ConfigNode();
//            geo_cache.name = "GEO_CACHE";
//            geo_cache.AddValue("some attr", "some updated value");
//            geo_cache.AddValue("some other attr", "some other updated value");
//            
//            WWWForm form = new WWWForm();
//            form.AddField("name", "new foobar");
//            form.AddField("coordinates", "some new place");
//            form.AddField("file", geo_cache.ToString());
//            
//            KerbalX.api.update_geo_cache(last_id, form, (resp, code) => {
//                if(code == 200){
//                    //example response: "{\"updated\":true,\"id\":8}"
//                }else if(code == 422){
//                    //example resp: "{\"updated\":false,\"errors\":\"unable to update because reasons\"}"
//                }else{
//                    Debug.Log("unknown error: " + resp);
//                }
//            }); 
//        });
//        
//        button("delete latest", () =>{
//            KerbalX.api.destroy_geo_cache(last_id, (resp, code) => {
//                Debug.Log(resp);
//            });
//        });
//        
//        
//    }
//}
