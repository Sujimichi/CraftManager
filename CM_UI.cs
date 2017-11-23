using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using KatLib;

namespace CraftManager
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CM_UI : DryUI
    {

        private float window_height = Screen.height - 400f;
        private float window_width  = 1000f;

        private string current_save_dir;

        //collection of Vector2 objects to track scroll positions
        private Dictionary<string, Vector2> scroll_pos = new Dictionary<string, Vector2>(){
            {"lhs", new Vector2()}, {"rhs", new Vector2()}, {"main", new Vector2()}
        };


        private void Start(){     
            CraftManager.log("Starting Main UI");
            CraftManager.main_ui = this;
            current_save_dir = HighLogic.SaveFolder;

            window_title = "Craft Manager";
            window_pos = new Rect((Screen.width/2) - (window_width/2) + 100, 80, window_width, window_height);
            visible = false;
            draggable = false;
            footer = false;

            Tags.load();
            show();
        }

        protected override void on_show(){            
            refresh();
            auto_focus_on = "main_search_field";
        }

        private void refresh(){
            CraftData.load_craft();
            filter_craft();
        }

        private void filter_craft(){
            Dictionary<string, object> search_criteria = new Dictionary<string, object>();
            search_criteria.Add("save_dir", current_save_dir);
            search_criteria.Add("search", search_string);
            search_criteria.Add("type", selected_types);
            List<string> s_tags = Tags.selected_tags();
            if(s_tags.Count > 0){
                search_criteria.Add("tags", s_tags);
                search_criteria.Add("tag_mode_reduce", tag_mode_reduce);
            }
            search_criteria.Add("sort", sort_opt);
            search_criteria.Add("reverse_sort", reverse_sort);
            CraftData.filter_craft(search_criteria);
        }


        //GUI state holders
        private string auto_focus_on = null;
        private string search_string = "";
        private string last_search = "";
        private string sort_opt = "name";
        private string[] sort_options = new string[]{"name", "part_count", "mass", "stage_count", "date_created", "date_updated"};
        private bool reverse_sort = false;

        private string new_tag_name = "";
        private bool edit_tags = false;
        private bool add_to_tag = false;
        private bool tag_mode_reduce = true;

        private Dictionary<string, bool> selected_types = new Dictionary<string, bool>(){
            {"SPH",true},{"VAB",false},{"Subassemblies",false} //TODO select SPH or VAB based on current editor
        };
        private int selected_type_count = 1;

        private bool expand_details = false;

        //Main GUI draw method (called by onGUI, see DryUI in KatLib).  Broken up into easier to digest sections to help prevent heart burn.
        protected override void WindowContent(int win_id){
            v_section(()=>{
                draw_top_section(window_width);          
                section(window_width, inner_width =>{
                    draw_left_hand_section(inner_width); //Tag list section
                    draw_main_section(inner_width);      //Main craft list
                    draw_right_hand_section(inner_width);//Craft details section
                });
                
                section(window_width,(inner_width) =>{
                    fspace();
                    gui_state(CraftData.selected_craft() != null, ()=>{
                        if(GUILayout.Button("Load", "button.load", width(inner_width*0.2f) )){
                            EditorLogic.LoadShipFromFile(CraftData.selected_craft().path);
                            this.hide();
                        }                        
                    });

                });
            });
            if(!String.IsNullOrEmpty(auto_focus_on)){
                GUI.FocusControl(auto_focus_on);
                auto_focus_on = null;
            }

        }

        protected override void FooterContent(int window_id){
            GUILayout.Label("hello, this is footer");
        }


        protected void draw_top_section(float section_width){
            section(() =>{
                //SPH, VAB, Subs select buttons
                section(400, () =>{
                    foreach(string opt in selected_types.Keys){
                        if(GUILayout.Button(opt, "craft_type_sel" + (selected_types[opt] ? ".active" : ""))){
                            type_select(opt, !selected_types[opt]);            
                        }
                    }
                    if(GUILayout.Button("All", "craft_type_sel", width(30f))){
                        type_select_all();
                    }
                });
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("refresh")){
                    refresh();
                }
            });
            section(() =>{
                label("Search Craft:", "h2");
                GUI.SetNextControlName("main_search_field");
                search_string = GUILayout.TextField(search_string, width(section_width/2));
                if(last_search != search_string){
                    filter_craft();
                }  
                if(GUILayout.Button("<[x]", width(40f))){
                    search_string = "";
                    filter_craft();
                }
            });
        }

        //The Main craft list
        protected void draw_main_section(float section_width){
            v_section(section_width*0.55f, (inner_width)=>{
                last_search = search_string;
                section(()=>{
                    fspace();
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
                scroll_pos["main"] = scroll(scroll_pos["main"], inner_width, window_height, craft_list_width => {
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
            });            
        }


        protected void draw_craft_list_item(CraftData craft, float section_width){
            section(section_width-(12f+18f), "craft.list_item" + (craft.selected ? ".selected" : ""), (inner_width)=>{ //subtractions from width to account for margins and scrollbar
                section(inner_width-80f,()=>{
                    v_section(()=>{
                        section(()=>{
                            label(craft.name, "craft.name");
                            if(craft.name != craft.alt_name){
                                label("(" + craft.alt_name + ")", "craft.alt_name");
                            }
                            if(selected_type_count > 1){
                                label(craft.construction_type, "bold");
                            }
                            if(craft.save_dir != current_save_dir){
                                fspace();
                                label("in save: " + craft.save_dir);
                            }
                        });

                        section((w) => {
                            GUILayout.Label(craft.part_count + " parts in " + craft.stage_count + " stage" + (craft.stage_count==1 ? "" : "s"), "craft.info", width(w/4f));
                            label("cost: " + humanize(craft.cost["total"]), "craft.cost");
                        });
                        if(craft.locked_parts){
                            label("craft has part which hasn't been unlocked yet", "craft.locked_parts");
                        }
                        if(craft.missing_parts){
                            label("some parts are missing", "craft.missing_parts");
                        }
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





        protected void draw_left_hand_section(float section_width){
            v_section(section_width*0.2f, (inner_width) =>{
                section((w)=>{
                    GUILayout.Label("Tags", "h2");
                    //                    tag_mode_reduce = GUILayout.Toggle(tag_mode_reduce, "reduce", "Button", width(60f));
                    //                    tag_mode_reduce = !GUILayout.Toggle(!tag_mode_reduce, "extend", "Button", width(60f));

                    GUILayout.FlexibleSpace();
                    edit_tags = GUILayout.Toggle(edit_tags, "edit", "Button", width(40f) );
                });

                scroll_pos["lhs"] = scroll(scroll_pos["lhs"], inner_width, window_height, scroll_width => {
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



        protected void draw_right_hand_section(float section_width){
            scroll_pos["rhs"] = scroll(scroll_pos["rhs"], section_width*0.25f, window_height, scroll_width => {
                if(CraftData.selected_craft() != null){
                    CraftData craft = CraftData.selected_craft();

                    label("Craft Details", "h2");
                    section(()=>{
                        label("Cost", "bold.compact");
                        label(humanize(craft.cost["total"]), "compact");
                    });
                    section(()=> {
                        label("Mass", "bold.compact");
                        label(humanize(craft.mass["total"]), "compact");
                        fspace();                       
                        expand_details = GUILayout.Toggle(expand_details, "expand", "hyperlink.bold");
                    });

                    if(expand_details){
                        float inner_width = scroll_width - 30;
                        GUILayoutOption grid_width = width(inner_width*0.4f);
                        section(()=>{                        
                            GUILayout.Label("", width(inner_width*0.2f));
                            GUILayout.Label("Dry", "bold.compact", grid_width);
                            GUILayout.Label("Fuel", "bold.compact", grid_width);
                        });
                        section(()=>{                        
                            GUILayout.Label("Cost", "bold.compact", width(inner_width*0.2f));
                            GUILayout.Label(humanize(craft.cost["dry"]), "small.compact", grid_width);
                            GUILayout.Label(humanize(craft.cost["fuel"]), "small.compact", grid_width);
                        });
                        section(()=>{                        
                            GUILayout.Label("Mass", "bold.compact", width(inner_width*0.2f));
                            GUILayout.Label(humanize(craft.mass["dry"]), "small.compact", grid_width);
                            GUILayout.Label(humanize(craft.mass["fuel"]), "small.compact", grid_width);
                        });
                    }
                    GUILayout.Space(15);

                    section(() =>{
                        label("Tags", "h2");
                        GUILayout.FlexibleSpace();
                        add_to_tag = GUILayout.Toggle(add_to_tag, "add tags", "Button", width(70f));
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


                }else{

                    label("Select a craft to see info about it", "h1.centered");
                };
            });
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
                selected_types["SPH"] = false;
                selected_types["VAB"] = false;
                selected_types["Subassemblies"] = false;
            }
            selected_types[key] = val;

            //ensure that at least one of the options is selected (if none are selected, select the one just clicked).
            selected_type_count = 0;
            foreach(bool v in selected_types.Values){if(v){selected_type_count++;}}
            if(selected_type_count==0){
                selected_types[key] = true;
                selected_type_count = 1;
            }

            filter_craft();
        }
        private void type_select_all(){
            selected_types["SPH"] = true;
            selected_types["VAB"] = true;
            selected_types["Subassemblies"] = true;
            selected_type_count = 3;
            filter_craft();
        }


    }

}

