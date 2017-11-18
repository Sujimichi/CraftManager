

//Built Against KSP 1.3.1
//build id = 01891
//2017-10-05_22-01-21
//Branch: master


using System;
using System.IO;
//using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
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
                DryUI.skin = new StyleSheet(HighLogic.Skin).skin;
            }
        }

        internal static void log(string msg){
            Debug.Log("[CM] " + msg);
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
            window_title = "Craft Manager";
            window_pos = new Rect((Screen.width/2) - (window_width/2) + 100, 80, window_width, window_height);
            require_login = true;
            visible = true;
//            draggable = false;
            CraftData.load_craft();
            CraftData.filter_craft();
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
                scroll_pos["rhs"] = scroll(scroll_pos["rhs"], inner_width*0.2f, window_height, rhs_section_width => {
                    draw_right_hand_section(rhs_section_width);
                });

            });

        }

        protected void draw_craft_list_item(CraftData craft, float section_width){
            section(section_width-(12f+18f), "craft.list_item" + (craft.selected ? ".selected" : ""), (inner_width)=>{ //subtractions from width to account for margin and scrollbar
                section(inner_width-80f,()=>{
                    v_section(()=>{
                        label(craft.name, "craft.name");
                        if(craft.name != craft.alt_name){
                            label("(" + craft.alt_name + ")", "craft.alt_name");
                        }

                        section((w) => {
                            GUILayout.Label(craft.part_count + " parts in " + craft.stage_count + " stages", "craft.info", width(w/4f));
                            label("cost: " + humanize(craft.cost["total"]), "craft.cost");
                        });
                        label("mass: " + humanize(craft.mass["total"]), "craft.info");
                    });
                    
                });
                section(80f,()=>{
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(craft.thumbnail, width(70), height(70));
                });
                
            }, craft_area => {
                if(craft_area.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.button == 0 ){                    
                    if(craft.selected){
                        craft.selected = false;
                    }else{
                        CraftData.select_craft(craft);
                    }
                    Event.current.Use();
                }
            });
        }

        protected void draw_right_hand_section(float width){
            GUILayout.Label("info shit");
            if(CraftData.selected_craft() != null){
                CraftData craft = CraftData.selected_craft();
                label("total mass: " + humanize(craft.mass["total"]));
                label("total cost: " + humanize(craft.cost["total"]));
                label("dry mass: " + humanize(craft.mass["dry"]));
                label("fuel mass: " + humanize(craft.mass["fuel"]));
                label("dry cost: " + humanize(craft.cost["fuel"]));
                label("fuel cost: " + humanize(craft.cost["fuel"]));
                label(craft.description);
            };
        }

        protected override void FooterContent(int window_id){
            GUILayout.Label("hello, this is footer");
        }
    }

}