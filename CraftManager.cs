

//Built Against KSP 1.3.1
//build id = 01891
//2017-10-05_22-01-21
//Branch: master


using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using KSP.UI.Screens;

using KatLib;

namespace CraftManager
{

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class CraftManager : MonoBehaviour
    {

        private void Awake(){

        }

        //Trigger the creation of custom Skin (copy of default skin with various custom styles added to it, see stylesheet.cs)
        private void OnGUI(){
            if(DryUI.skin == null){
//                StyleSheet.prepare();
                DryUI.skin = new StyleSheet(HighLogic.Skin).skin;
            }
        }

        internal static void log(string msg){
            Debug.Log("[CM] " + msg);
        }
    }

    public class CraftData
    {
        public static List<CraftData> all_craft = new List<CraftData>();
        public static List<CraftData> filtered  = new List<CraftData>();

        public static string save_dir = Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder);

        public static void load_craft(){
            string[] craft_file_paths;
            craft_file_paths = Directory.GetFiles(save_dir, "*.craft", SearchOption.AllDirectories);

            all_craft.Clear();
            foreach(string path in craft_file_paths){
                all_craft.Add(new CraftData(path));
            }
        }

        public static void filter_craft(){
            filtered = all_craft;    
        }

        public static void select_craft(CraftData craft){
            foreach(CraftData list_craft in filtered){
                list_craft.selected = list_craft == craft;
            }
        }

        public string path;
        public string name;
        public float mass;
        public float craft_cost;
        public float fuel_cost;


        public int part_count;
        public bool selected = false;

        public CraftData(string full_path){
            path = full_path;

            ShipConstruct ship = new ShipConstruct();
            ship.LoadShip(ConfigNode.Load(path));
            name = ship.shipName;
            mass = ship.GetTotalMass();
            ship.GetShipCosts(out craft_cost, out fuel_cost);

            part_count = ship.parts.Count();

                

//            string[] split_plath = path.Split('/');
//            name = split_plath[split_plath.Length-1];

        }

    }


    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CM_UI : DryUI
    {

        private float window_height = Screen.height - 400f;
        private float window_width  = 800f;

        //collection of Vector2 objects to track scroll positions
        private Dictionary<string, Vector2> scroll_pos = new Dictionary<string, Vector2>(){
            {"lhs", new Vector2()}, {"rhs", new Vector2()}, {"main", new Vector2()}
        };


        private void Start(){     
            window_title = "Test";
            window_pos = new Rect((Screen.width/2) - (window_width/2) + 100, 80, window_width, window_height);
            require_login = true;
            visible = true;
//            draggable = false;
            CraftData.load_craft();
            CraftData.filter_craft();
            CraftManager.log("start called");
        }


        protected override void WindowContent(int win_id){

            GUILayout.Label("this will be the top of stuff");
            GUILayout.Label(CraftData.save_dir);

            section(() =>{
                if(GUILayout.Button("load", width(40f))){
                    CraftData.load_craft();
                }
                if(GUILayout.Button("filter", width(40f))){
                    CraftData.filter_craft();
                }
            });


            section(window_width, inner_width =>{

                //Left Hand Section
                v_section(inner_width*0.2f, w2 => {
                    GUILayout.Label("title shit");
                    scroll_pos["lhs"] = scroll(scroll_pos["lhs"], w2, window_height, w3 => {
                        GUILayout.Label("some shit");
                    });
                });


                //Main Craft Section
                style_override = "craft.list_container";
                scroll_pos["main"] = scroll(scroll_pos["main"], inner_width*0.6f, window_height, craft_list_width => {
                    foreach(CraftData craft in CraftData.filtered){
                        draw_craft_list_item(craft, craft_list_width);
                    }
                });

//                Rect scroller = GUILayoutUtility.GetLastRect();
//                if(scroller.Contains(Event.current.mousePosition)){
//                    if(Event.current.button == 1 && Event.current.type == EventType.MouseDrag){
//                        scroll_pos["main"] += Event.current.delta;
//                        Event.current.Use();
//                    }
//                }


                //Right Hand Section
                scroll_pos["rhs"] = scroll(scroll_pos["rhs"], inner_width*0.2f, window_height, w2 => {
                    GUILayout.Label("info shit");
                });

            });

        }

        protected void draw_craft_list_item(CraftData craft, float width){
            section(width-(12f+18f), "craft.list_item" + (craft.selected ? ".selected" : ""), (inner_width)=>{
                section(inner_width*0.6f,()=>{
                    v_section(()=>{
                        GUILayout.Label(craft.name, "craft.name");
                        section(() => {
                            label("mass: " + craft.mass.ToString(), "craft.info");
                            label("cost: " + craft.craft_cost + craft.fuel_cost, "craft.info");
                            label("parts: " + craft.part_count, "craft.info");
                        });
                    });
                    
                });
                section(inner_width*0.4f,()=>{
                    v_section(()=>{
                        GUILayout.Label("craft pic");
                    });
                });
                
            }, craft_area => {
                if(craft_area.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0 ){                    
                    CraftData.select_craft(craft);
                }
            });


   

        }

        protected override void FooterContent(int window_id){
            GUILayout.Label("hello, this is footer");
        }
    }

}