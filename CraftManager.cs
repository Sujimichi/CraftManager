

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
        private float window_width  = 1000f;

        //collection of Vector2 objects to track scroll positions
        private Dictionary<string, Vector2> scroll_pos = new Dictionary<string, Vector2>(){
            {"lhs", new Vector2()}, {"rhs", new Vector2()}, {"main", new Vector2()}
        };


        private void Start(){     
            window_title = "Craft Manager";
            window_pos = new Rect((Screen.width/2) - (window_width/2) + 100, 80, window_width, window_height);
            visible = true;
//            draggable = false;
            Tags.load();
            CraftData.load_craft();
            filter_craft();
        }


        //GUI state holders
        private string search_string = "";
        private string last_search = "";
        private string new_tag_name = "";
        private string sort_opt = "name";
        private string[] sort_options = new string[]{"name", "part_count", "mass", "stage_count", "date_created", "date_updated"};
        private bool reverse_sort = false;

        private bool edit_tags = false;
        private bool add_to_tag = false;
        private bool tag_mode_reduce = true;


        private Dictionary<string, bool> toggles = new Dictionary<string, bool>(){
            {"SPH",true},{"VAB",false},{"Subassemblies",false} //TODO select SPH or VAB based on current editor
        };



        private void filter_craft(){
            Dictionary<string, object> search_criteria = new Dictionary<string, object>();
            search_criteria.Add("search", search_string);
            search_criteria.Add("type", toggles);
            List<string> s_tags = Tags.selected_tags();
            if(s_tags.Count > 0){
                search_criteria.Add("tags", s_tags);
                search_criteria.Add("tag_mode_reduce", tag_mode_reduce);
            }
            search_criteria.Add("sort", sort_opt);
            search_criteria.Add("reverse_sort", reverse_sort);
            CraftData.filter_craft(search_criteria);
        }

        protected override void WindowContent(int win_id){

            GUILayout.Label("this will be the top of stuff");


            //SPH, VAB, Subs select buttons
            section((w) =>{
                section(400, (w2) =>{
                    foreach(string opt in toggles.Keys){
                        if(GUILayout.Button(opt, "craft_type_sel" + (toggles[opt] ? ".active" : ""))){
                            type_select(opt, !toggles[opt]);            
                        }
                    }
                    if(GUILayout.Button("All", "craft_type_sel", width(30f))){
                        toggles["SPH"]=true;toggles["VAB"]=true;toggles["Subassemblies"]=true;
                        filter_craft();
                    }
                });
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("refresh")){
                    CraftData.all_craft.Clear();CraftData.load_craft();filter_craft();
                }
            });


            section(window_width, inner_width =>{

                //Left Hand Section
                draw_left_hand_section(inner_width);

                

                //Main Craft Section
                v_section(()=>{
                    last_search = search_string;
                    section(()=>{
                        search_string = GUILayout.TextField(search_string);
                        if(last_search != search_string){
                            filter_craft();
                        }
                        if(GUILayout.Button("sort: " + sort_opt.Replace("_"," "), width(150f))){
                            int i = sort_options.IndexOf(sort_opt) + 1;
                            if(i > sort_options.Length-1){
                                i=0;
                            }
                            sort_opt = sort_options[i];
                            filter_craft();

                        }

                        if(GUILayout.Button((reverse_sort ? "/\\" : "\\/"), width(22f))){
                            reverse_sort = !reverse_sort;
                            filter_craft();
                        }
                        
                    });

                    style_override = "craft.list_container";
                    scroll_pos["main"] = scroll(scroll_pos["main"], inner_width*0.6f, window_height, craft_list_width => {
                        foreach(CraftData craft in CraftData.filtered){
                            draw_craft_list_item(craft, craft_list_width);
                        }
                    });
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


        protected void draw_left_hand_section(float section_width){
            v_section(section_width*0.2f, (inner_width) =>{
                section((w)=>{
                    GUILayout.Label("Tags");
//                    tag_mode_reduce = GUILayout.Toggle(tag_mode_reduce, "reduce", "Button", width(60f));
//                    tag_mode_reduce = !GUILayout.Toggle(!tag_mode_reduce, "extend", "Button", width(60f));

                    GUILayout.FlexibleSpace();
                    edit_tags = GUILayout.Toggle(edit_tags, "edit", "Button", width(40f) );
                });

                scroll_pos["lhs"] = scroll(scroll_pos["lhs"], inner_width, window_height, lhs_section_width => {
                    foreach(KeyValuePair<string, Tag> pair in Tags.all){
                        Tag tag = pair.Value;
                        
                        style_override = "tag.section";
                        section((sec_w)=>{
                            bool prev_state = tag.selected;
                            tag.selected = GUILayout.Toggle(tag.selected, "", "tag.toggle.light");
                            tag.selected = GUILayout.Toggle(tag.selected, tag.name + " - (" + tag.craft_count("filtered") + "/" + tag.craft_count("all") + ")", 
                                "tag.toggle.label", width(inner_width-(edit_tags ? 65f : 40f)) 
                            );
                            if(prev_state != tag.selected){
                                if(add_to_tag){
                                    tag.selected = prev_state;
                                    Tags.tag_craft(Tags.craft_reference_key(CraftData.selected_craft()), tag.name);
                                }else{
                                    filter_craft();                                    
                                }

                            }
                            if(edit_tags){
                                if(GUILayout.Button("X", "tag.delete_button.x")){
                                    delete_tag_dialog(tag);
                                }
                            }
                        });
                    }
                });

                section((w)=>{
                    new_tag_name = GUILayout.TextField(new_tag_name);
                    if(GUILayout.Button("Add", width(40f) )){
                        Tags.add(new_tag_name);
                        new_tag_name = "";
                    }
                });



            });
        }



        protected void draw_craft_list_item(CraftData craft, float section_width){
            section(section_width-(12f+18f), "craft.list_item" + (craft.selected ? ".selected" : ""), (inner_width)=>{ //subtractions from width to account for margins and scrollbar
                section(inner_width-80f,()=>{
                    v_section(()=>{
                        label(craft.name, "craft.name");
                        if(craft.name != craft.alt_name){
                            label("(" + craft.alt_name + ")", "craft.alt_name");
                        }

                        section((w) => {
                            GUILayout.Label(craft.part_count + " parts in " + craft.stage_count + " stage" + (craft.stage_count==1 ? "" : "s"), "craft.info", width(w/4f));
                            label("cost: " + humanize(craft.cost["total"]), "craft.cost");
                        });
//                        label("mass: " + humanize(craft.mass["total"]), "craft.info");
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

        protected void draw_right_hand_section(float section_width){
            if(CraftData.selected_craft() != null){
                CraftData craft = CraftData.selected_craft();
                label("total mass: " + humanize(craft.mass["total"]));
                label("total cost: " + humanize(craft.cost["total"]));
                label("dry mass: " + humanize(craft.mass["dry"]));
                label("fuel mass: " + humanize(craft.mass["fuel"]));
                label("dry cost: " + humanize(craft.cost["dry"]));
                label("fuel cost: " + humanize(craft.cost["fuel"]));



                section(() =>{
                    label("Tags", "h2");
                    add_to_tag = GUILayout.Toggle(add_to_tag, "add tags", "Button");
                });
                if(add_to_tag){
                    label("click tags on the left to add them to this craft");
                    add_to_tag = !GUILayout.Toggle(!add_to_tag, "done", "Button");
                }
                foreach(string tag in Tags.tags_for(Tags.craft_reference_key(craft))){
                    section(() =>{
                        label(tag);    
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("x", "tag.delete_button.x")){
                            Tags.untag_craft(Tags.craft_reference_key(craft), tag);
                        }
                    });
                }                  

//                label("time: " + craft.create_time);
//                label(DateTime.FromBinary(long.Parse(craft.create_time)).ToShortDateString());
//                label(DateTime.FromBinary(long.Parse(craft.create_time)).ToShortTimeString());


                label(craft.description);


            };
        }

        protected void delete_tag_dialog(Tag tag){
            close_dialog();
            DryDialog dialog = show_dialog((d)=>{
                if(tag.craft_count("all") > 0){
                    GUILayout.Label("This tag is used on " + tag.craft_count("all") + " craft.");
                }
                GUILayout.Label("Are you sure you want to delete it?");
                section(()=>{
                    GUILayout.FlexibleSpace();
                    if(GUILayout.Button("Cancel")){
                        close_dialog();
                    };
                    if(GUILayout.Button("Delete", "tag.delete_button")){
                        Tags.remove(tag.name);close_dialog();
                    };
                });
            });
            dialog.window_pos.width = 400f;
            dialog.window_pos.height = 100f;
            dialog.window_title = "Confirm Tag Delete";            
        }



        //called when clicking on the craft 'type' (VAB,SPH etc) buttons. unselects the other buttons unless ctrl is being held (enabling multiple select)
        //and ensures that at least one button is selected.
        private void type_select(string key, bool val){
            GUIUtility.keyboardControl = 0; //take focus away from text fields so that ctrl hold can be detected
            if(!Input.GetKey(KeyCode.LeftControl)){
                toggles["SPH"] = false;
                toggles["VAB"] = false;
                toggles["Subassemblies"] = false;
            }
            toggles[key] = val;

            //ensure that at least one of the options is selected (if none are selected, select the one just clicked).
            int set_count = 0;
            foreach(bool v in toggles.Values){if(v){set_count++;}}
            if(set_count==0){toggles[key] = true;}

            filter_craft();
        }

        protected override void FooterContent(int window_id){
            GUILayout.Label("hello, this is footer");
        }
    }

}