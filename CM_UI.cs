using System;
using System.IO;
using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;
using ExtensionMethods;

using KatLib;

namespace CraftManager
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CM_UI : DryUI
    {

        private float main_section_height = Screen.height - 400f;
        private float window_width  = 1000f;

        private string current_save_dir = HighLogic.SaveFolder;
        private string active_save_dir;


        private string search_string = "";
        private string last_search = "";
        
        private Dictionary<string, string> save_menu_options = new Dictionary<string, string>();
        private Dictionary<string, string> load_menu_options = new Dictionary<string, string>();
        private Dictionary<string, string> load_menu_options_default = new Dictionary<string, string> { { "merge", "Merge" }, { "subload", "Load as Subassembly" } };
        private Dictionary<string, string> load_menu_options_submode = new Dictionary<string, string> { { "merge", "Merge" }, { "load", "Load as Craft" } };
        private Dictionary<string, string> sort_options = new Dictionary<string, string>{
            {"name", "Name"}, {"cost", "Cost"}, {"crew_capacity", "Crew Capacity"}, {"mass", "Mass"}, {"part_count", "Part Count"}, {"stage_count", "Stages"}, {"date_created", "Created"}, {"date_updated", "Updated"}
        };
        private Dictionary<string, string> tag_sort_options = new Dictionary<string, string> { {"name", "Name"}, {"craft_count", "Craft Count"} };

        private float save_menu_width = 0;
        private float sort_menu_width = 0;

        private string sort_opt = "name";
        private bool reverse_sort = false;
        

        private string auto_focus_on = null;
        private string new_tag_name = "";
        public string tag_sort_by = "craft_count";
        private bool edit_tags = false;
        private bool tag_mode_reduce = true;
        private bool expand_details = false;
        private bool exclude_stock_craft = true;

        string load_button_text = "Load";
        string load_button_action = "load";
        float load_button_width = 120f;

        private Dictionary<string, bool> selected_types = new Dictionary<string, bool>(){
            {"SPH",EditorDriver.editorFacility.CompareTo(EditorFacility.SPH)==0},
            {"VAB",EditorDriver.editorFacility.CompareTo(EditorFacility.VAB)==0},
            {"Subassemblies",false} 
        };
        private int selected_type_count = 1;
        protected List<string> selected_type_keys = new List<string>(){"SPH", "VAB", "Subassemblies"};


        //collection of Vector2 objects to track scroll positions
        private Dictionary<string, Vector2> scroll_pos = new Dictionary<string, Vector2>(){
            {"lhs", new Vector2()}, {"rhs", new Vector2()}, {"main", new Vector2()}
        };
        protected Rect scroll_relative_pos = new Rect(0, 0, 0, 0);




        //register events used to keep track of the save state of the craft.
        //These events are unregistered by onGameSceneLoadRequested Event.
        private void Awake(){
            GameEvents.onEditorShipModified.Add(on_ship_modified);
            GameEvents.onEditorRestart.Add(on_editor_restart);
        }

        //editor restart is triggered when loading a craft and creating a new one.  
        //reset the save_state count to 0 (after new) or -1 (after load).  Reason is that onEditorShipModified is called 
        //directly after loading so it ends up as 0.  The distinction between load and new is detected by loading_craft being true.
        public void on_editor_restart(){
            if(CraftData.loading_craft){
                CraftData.save_state = -1;
                CraftData.loading_craft = false;
            } else{
                CraftData.save_state = 0;
            }
        }
        //called by onEditorShipModified, increments count of save_state
        public void on_ship_modified(ShipConstruct ship){
            CraftData.save_state++;
        }

        //Actions which are hooked into click events on the main editor buttons (not done with GameEvents)
        //clicking save resets the save_state to 0. Although this isn't quite right; in the case of the dialog 
        //to overright existing craft being shown it could result in the wrong save_state being set.
        private UnityEngine.Events.UnityAction on_save_click = new UnityEngine.Events.UnityAction(()=>{            
            CraftData.save_state = 0;
        });

        //Replace the default load action
        private UnityEngine.Events.UnityAction on_load_click = new UnityEngine.Events.UnityAction(()=>{            
            CraftManager.main_ui.toggle();
        });
        

        private void Start(){     
            CraftManager.log("Starting Main UI");
            CraftManager.main_ui = this;
            window_title = "Craft Manager";
            window_pos = new Rect((Screen.width/2) - (window_width/2) + 100, 80, window_width, main_section_height);
            visible = false;
            //            draggable = false;
            footer = false;

            EditorLogic.fetch.saveBtn.onClick.AddListener(on_save_click); //settup click event on the stock save button.
            //override existing ations on stock load button and replace with call to toggle CM's UI.
            if(CraftManager.replace_editor_load_button){
                UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent(); 
                c.AddListener(on_load_click);
                EditorLogic.fetch.loadBtn.onClick = c;
            }

            //Initialize list of Save directories, used in save select menus.
            active_save_dir = HighLogic.SaveFolder;
            save_menu_options.Add(active_save_dir, "Current Save (" + active_save_dir + ")");
            foreach(string dir_name in CraftData.save_names()){
                if(dir_name != active_save_dir){
                    save_menu_options.Add(dir_name, dir_name);
                }
            }
            save_menu_options.Add("all", "All");

            Tags.load(active_save_dir);
//            show();
        }

        protected override void on_show(){            
            refresh();
            auto_focus_on = "main_search_field";
        }

        protected override void on_hide(){
            close_dialog(); //incase any dialogs have been left open
        }

        //load/reload craft from the active_save_dir and apply any active filters
        public void refresh(){
            CraftData.load_craft(active_save_dir=="all" ? null : active_save_dir);
            filter_craft();
        }


        //Collect any currently active filters into a Dictionary<string, object> which can then be passed to 
        //filter_craft on CraftData (which does the actual filtering work).
        private void filter_craft(){
            Dictionary<string, object> search_criteria = new Dictionary<string, object>();
            search_criteria.Add("search", search_string);
            search_criteria.Add("type", selected_types);
            List<string> s_tags = Tags.selected_tags();
            if(s_tags.Count > 0){
                search_criteria.Add("tags", s_tags);
                search_criteria.Add("tag_mode_reduce", tag_mode_reduce);
            }
            search_criteria.Add("sort", sort_opt);
            search_criteria.Add("reverse_sort", reverse_sort);
            if(exclude_stock_craft){
                search_criteria.Add("exclude_stock", true);
            }
            CraftData.filter_craft(search_criteria);
            Tags.sort_tag_list();
        }

        protected void clear_search(){
            search_string = "";
            filter_craft();
        }
        protected void toggle_reverse_sort(){
            reverse_sort = !reverse_sort;
            filter_craft();
        }
        protected void change_save(string save_name){
            active_save_dir = save_name;
            save_menu_width = GUI.skin.button.CalcSize(new GUIContent("Save: " + active_save_dir)).x;
//            new Tags();
            Tags.load(active_save_dir);
            refresh();
        }


        //Main GUI draw method (called by onGUI, see DryUI in KatLib).  Broken up into smaller sections to ease digestion and help prevent heart burn.
        //The GUI is main in 5 sections, top and bottom sections span the full width, while the LHS, RHS and main sections are columns.
        protected override void WindowContent(int win_id){
            v_section(()=>{                
                draw_top_section(window_width);     
                GUILayout.Space(10);
                scroll_relative_pos = GUILayoutUtility.GetLastRect();
                section(window_width, inner_width =>{
                    draw_left_hand_section(inner_width * 0.2f); //Tag list section
                    draw_main_section(inner_width * 0.55f);      //Main craft list
                    draw_right_hand_section(inner_width * 0.25f);//Craft details section
                });
                draw_bottom_section(window_width);
            });

            //When the UI opens set focus on the main search text field, but don't keep setting focus
            if(!String.IsNullOrEmpty(auto_focus_on)){  
                GUI.FocusControl(auto_focus_on);
                auto_focus_on = null;
            } 
        }

        protected override void FooterContent(int window_id){
            GUILayout.Label("hello, this is footer");
        }




        //**GUI Sections**//

        //GUI Top Section
        protected void draw_top_section(float section_width){
            section(() =>{
                //SPH, VAB, Subs select buttons
                section(400, () =>{
                    foreach(string opt in selected_type_keys){
                        button(opt, "craft_type_sel" + (selected_types[opt] ? ".active" : ""), ()=>{type_select(opt, !selected_types[opt]);});
                    }
                    button("All", "craft_type_Sel", 30f, type_select_all);
                });
                fspace();
                if(save_menu_width == 0){
                    save_menu_width = GUI.skin.button.CalcSize(new GUIContent("Save: " + active_save_dir)).x;
                }
                dropdown("Save: " + active_save_dir, "save_menu", save_menu_options, this, save_menu_width, change_save);
            });
            section(() =>{
                label("Search Craft:", "h2");
                GUI.SetNextControlName("main_search_field");
                search_string = GUILayout.TextField(search_string, width(section_width/2));
                if(last_search != search_string){
                    filter_craft();
                }
                button("clear", 40f, clear_search);
                    
                fspace();
                section(()=>{
                    bool prev_exstcr = exclude_stock_craft;
                    exclude_stock_craft = !GUILayout.Toggle(!exclude_stock_craft, "");
                    button("include Stock Craft", "bold", ()=>{exclude_stock_craft = !exclude_stock_craft;});
                    if(exclude_stock_craft != prev_exstcr){
                        filter_craft();
                    }
                });
            });
        }



        //The Main craft list
        protected void draw_main_section(float section_width){
            v_section(section_width, (inner_width)=>{
                last_search = search_string;
                section(()=>{
                    fspace();

                    if(sort_menu_width == 0){
                        sort_menu_width = GUI.skin.button.CalcSize(new GUIContent("Sort: " + sort_options[sort_opt])).x;
                    }
                    dropdown("Sort: " + sort_options[sort_opt], "sort_menu", sort_options, this, sort_menu_width, "button.tight", (resp) => {
                        sort_opt = resp;
                        sort_menu_width = GUI.skin.button.CalcSize(new GUIContent("Sort: " + sort_options[sort_opt])).x;
                        filter_craft();
                    });
                    button((reverse_sort ? "/\\" : "\\/"), "button.tight.right_margin", 22f, toggle_reverse_sort);
                });


                scroll_pos["main"] = scroll(scroll_pos["main"], "craft.list_container", inner_width, main_section_height, craft_list_width => {
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

        //Individual Craft Content
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
                            if(active_save_dir != current_save_dir){
                                fspace();
                                label("in save: " + craft.save_dir);
                            }
                        });

                        section((w) => {
                            label(craft.part_count + " parts in " + craft.stage_count + " stage" + (craft.stage_count==1 ? "" : "s"), "craft.info", width(w/4f));
                            label("cost: " + humanize(craft.cost_total), "craft.cost");
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
                    fspace();
                    GUILayout.Label(craft.thumbnail, width(70), height(70));
                });

            }, evt => {
                if(evt.single_click){
                    CraftData.toggle_selected(craft);  
                }
                if(evt.double_click){
                    CraftData.select_craft(craft);
                    load_craft( craft.construction_type=="Subassembly" ? "subload" : "load");
                }
            });
        }


        //Left Hand Section: Tags
        bool prev_state = false;
        bool state = false;
        protected void draw_left_hand_section(float section_width){
            v_section(section_width, (inner_width) =>{
                section((w)=>{
                    label("Tags", "h2");
//                    tag_mode_reduce = GUILayout.Toggle(tag_mode_reduce, "reduce", "Button", width(60f));
//                    tag_mode_reduce = !GUILayout.Toggle(!tag_mode_reduce, "extend", "Button", width(60f));
                    fspace();
                    edit_tags = GUILayout.Toggle(edit_tags, "edit", "button.tight", width(40f) );
                    dropdown("\\/", "tag_sort_menu", tag_sort_options, this, 20f, "button.tight.right_margin", resp => {
                        tag_sort_by = resp;
                        Tags.sort_tag_list();
                    });
                });


//                float tag_sec_height = GUI.skin.GetStyle("tag.toggle.label").CalcSize(new GUIContent("foo")).x * Tags.all.Count;
//                label("list: " + tag_sec_height + " scroll: " + main_section_height + " item: " + GUI.skin.GetStyle("tag.toggle.label").CalcSize(new GUIContent("foo")).x);


                scroll_pos["lhs"] = scroll(scroll_pos["lhs"], "side_panel.scroll", inner_width, main_section_height, scroll_width => {
                    foreach(string tag_name in Tags.names){
                        style_override = "tag.section";
                        section((sec_w)=>{
                            state = Tags.is_selected(tag_name);
                            prev_state = state;

                            state = GUILayout.Toggle(state, "", "tag.toggle.light");
                            state = GUILayout.Toggle(
                                state, tag_name + " - (" + Tags.craft_count_for(tag_name,"filtered") + ")", 
                                "tag.toggle.label", width(scroll_width-(edit_tags ? 82f : 35f))
                            );

                            if(prev_state != state){
                                Tags.toggle_tag(tag_name);
                                filter_craft();                                    
                            }
                            if(edit_tags){
                                button("e", "tag.edit_button", ()=>{
                                    edit_tag_dialog(tag_name);
                                });
                                button("X", "tag.delete_button.x", ()=>{
                                    delete_tag_dialog(tag_name);
                                });
                            }
                        });
                    }
                });
            });
        }


        //Right Hand Section: Craft Details
        protected void draw_right_hand_section(float section_width){
            v_section(section_width, (inner_width) =>{                
                label("Craft Details", "h2");

                scroll_pos["rhs"] = scroll(scroll_pos["rhs"], "side_panel.scroll", inner_width, main_section_height, scroll_width => {
                    if(CraftData.selected_craft == null){
                        label("Select a craft to see info about it", "h1.centered");
                    }else{
                        GUILayout.Space(6);
                        CraftData craft = CraftData.selected_craft;                        
                        section(()=>{
                            label("Cost", "bold.compact");
                            label(humanize(craft.cost_total), "compact");
                        });
                        section(()=> {
                            label("Mass", "bold.compact");
                            label(humanize(craft.mass_total), "compact");
                            fspace();                       
                            expand_details = GUILayout.Toggle(expand_details, "expand", "hyperlink.bold");
                        });

                        
                        if(expand_details){
                            float details_width = scroll_width - 50;
                            GUILayoutOption grid_width = width(details_width*0.4f);
                            section((w)=>{                        
                                label("", width(details_width*0.2f));
                                label("Dry", "bold.compact", grid_width);
                                label("Fuel", "bold.compact", grid_width);
                            });
                            section(()=>{                        
                                label("Cost", "bold.compact", width(details_width*0.2f));
                                label(humanize(craft.cost_dry), "small.compact", grid_width);
                                label(humanize(craft.cost_fuel), "small.compact", grid_width);
                            });
                            section(()=>{                        
                                label("Mass", "bold.compact", width(details_width*0.2f));
                                label(humanize(craft.mass_dry), "small.compact", grid_width);
                                label(humanize(craft.mass_fuel), "small.compact", grid_width);
                            });
                        }

                        section(()=>{
                            label("Crew Capacity", "bold.compact");
                            label(craft.crew_capacity.ToString(), "compact");
                        });

                        section(()=>{
                            DateTime date = DateTime.FromBinary(long.Parse(craft.last_updated_time));
                            label("Last Updated", "bold.compact");
                            label(date.time_ago(), "compact");
                        });

                        GUILayout.Space(15);

                        section((w) => {
                            button("transfer", transfer_craft_dialog);
                            button("move/copy", move_copy_craft_dialog);
                        });
                        section((w) => {
                            button("rename", rename_craft_dialog);
                            button("delete", "button.delete", delete_craft_dialog);
                        });

                        GUILayout.Space(15);

                        section((w) =>{
                            label("Tags", "h2");
                            fspace();
                            scroll_relative_pos.x += (window_pos.width * (0.55f+0.2f)) - 5f;
                            scroll_relative_pos.y += 45f - scroll_pos["rhs"].y;
                            dropdown("Add Tag", "add_tag_menu", Tags.names, this, scroll_relative_pos, 70f, "Button", "menu.background", "menu.item.small", resp => {
                                Tags.tag_craft(craft, resp);
                            });
                        });
                   
                        foreach(string tag in Tags.for_craft(craft)){
                            section(() =>{
                                label(tag);    
                                fspace();
                                button("x", "tag.delete_button.x", ()=>{Tags.untag_craft(craft, tag);});
                            });
                        }                  

                        section(() => {
                            label("Description", "h2");
                            fspace();
                            button((String.IsNullOrEmpty(craft.description) ? "Add" : "Edit"), edit_description_dialog);
                        });
                        section(() => {
                            label(craft.description);
                        });
                    };
                });
            });
        }

        //Botton Section: Load buttons
        protected void draw_bottom_section(float section_width){
            
            section(() =>{
                new_tag_name = GUILayout.TextField(new_tag_name, width(200f));
                button("Add", 40f, ()=>{                    
                    Tags.find_or_create_by(new_tag_name, active_save_dir);
                    new_tag_name = "";
                });

                fspace();

                gui_state(CraftData.selected_craft != null, ()=>{                    
                    load_button_text = "Load";
                    load_button_action = "load";
                    load_button_width = 120f;
                    load_menu_options = load_menu_options_default;
                    if(CraftData.selected_craft != null && CraftData.selected_craft.construction_type == "Subassembly"){                        
                        load_button_text = "Load Subassembly";
                        load_button_action = "subload";
                        load_button_width = 300f;
                        load_menu_options = load_menu_options_submode;
                    }
                    
                    button(load_button_text, "button.load", load_button_width, ()=>{ load_craft(load_button_action);});
                    dropdown("\\/", "load_menu", load_menu_options, this, 30f, "button.load", "menu.background", "menu.item", resp => {
                        load_craft(resp);
                    });
                });
                GUILayout.Space(8);
                button("Close", "button.close", 120f, this.hide);

            });
            GUILayout.Space(20);
        }



        //**Helpers**//

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

        //Handles loading the CraftData.selected_craft() into the editor. Takes a string which can either be "load", "merge" or "subload".
        //"load" performs a normal craft load (checks save state of existing & clears existing content before loading)
        //"merge" spawns a disconnected contruct of the craft along side an existing craft
        //"subload" loads like merge, but retains select on the loaded craft so it can be placed (same as stock subassembly load).
        protected void load_craft(string load_type, bool force = false){
            if(CraftData.selected_craft != null){

                if(load_type == "load"){                                       
                    if(CraftData.craft_saved || force){
                        CraftData.loading_craft = true;
                        EditorLogic.LoadShipFromFile(CraftData.selected_craft.path);
                        this.hide();
                    } else {
                        load_craft_confirm_dialog(load_type);
                    }
                } else if(load_type == "merge"){                    
                    ShipConstruct ship = new ShipConstruct();
                    ship.LoadShip(ConfigNode.Load(CraftData.selected_craft.path));
                    EditorLogic.fetch.SpawnConstruct(ship);
                    this.hide();
                } else if(load_type == "subload"){
                    ShipTemplate subassembly = new ShipTemplate();
                    subassembly.LoadShip(ConfigNode.Load(CraftData.selected_craft.path));
                    EditorLogic.fetch.SpawnTemplate(subassembly);
                    this.hide();
                }

            }

        }



        //**Dialogs**//

        //Various popup windows. All these dialogs use the 'show_dialog' method which is lurking below them
        //The show_dialog method takes care of all common aspects, leaving these dialog methods DRY and minimal
        //The delegate passed to show_dialog is expected to return a string (resp), This is used to pass back
        //error messages or a success status code ("200").  If "200" is returned the dialog will be closed, 
        //any other string will be shown as an error message to the user.

        protected void load_craft_confirm_dialog(string load_type){
            string resp = "";
            show_dialog("Confirm Load", "The Current Craft has unsaved changes", d =>{
                section(()=>{                    
                    button("Save Current Craft first", "button.continue_with_save", ()=>{
                        string path = ShipConstruction.GetSavePath(EditorLogic.fetch.ship.shipName);
                        EditorLogic.fetch.ship.SaveShip().Save(path);
                        load_craft(load_type, true); resp = "200";
                    });                    
                    button("Continue Without Saving", "button.continue_no_save", ()=>{
                        load_craft(load_type, true); resp = "200";
                    });
                });
                GUILayout.Space(10);
                button("Cancel", "button.cancel_load", close_dialog);
                return resp;
            });
        }

        protected void delete_tag_dialog(string tag_name){            
            string resp = "";
            int craft_count = Tags.craft_count_for(tag_name, "all");
            float top = Event.current.mousePosition.y + window_pos.y + 140;
            float left = Event.current.mousePosition.x + window_pos.x;
            show_dialog("Delete Tag", "Are you sure you want to delete this tag?", top, left, 400f, true, d =>{
                
                if(craft_count > 0){
                    GUILayout.Label("This tag is used for " + craft_count + " craft.");
                    label("deleting tags will not delete any craft");
                }
                if(active_save_dir == "all"){
                    label("You are viewing craft from all saves, this tag will be deleted in each of your saves.", "alert.h3");
                }
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Delete", "button.delete", ()=>{
                        return Tags.remove(tag_name, active_save_dir);
                    });
                });
                return resp;
            });
        }

        protected void edit_tag_dialog(string tag_name){
            string resp = "";
            string new_tag_name = tag_name;
            float top = Event.current.mousePosition.y + window_pos.y + 140;
            float left = Event.current.mousePosition.x + window_pos.x;
            show_dialog("Edit Tag", "Edit Tag: " + tag_name, top, left, 400f, true, d =>{
                if(active_save_dir == "all"){
                    label("You are viewing craft from all saves, this will rename this tag in each of your saves.", "alert.h3");
                }
                GUI.SetNextControlName("dialog_focus_field");
                new_tag_name = GUILayout.TextField(new_tag_name);
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Save", ()=>{
                        return Tags.rename(tag_name, new_tag_name, active_save_dir);
                    });
                });
                return resp;
            });
        }

        protected void edit_description_dialog(){
            if(CraftData.selected_craft.description == null){
                CraftData.selected_craft.description = "";
            }
            string resp = "";
            float area_height = 0;
            show_dialog("Edit Description", "Edit Description", d =>{
                GUI.SetNextControlName("dialog_focus_field");
                area_height = skin.textArea.CalcHeight(new GUIContent(CraftData.selected_craft.description), d.window_pos.width)+10;
                if(area_height < 150f){area_height=150f;}
                CraftData.selected_craft.description = GUILayout.TextArea(CraftData.selected_craft.description, height(area_height));
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Save", CraftData.selected_craft.save_description);
                });
                return resp;
            });
        }

        protected void rename_craft_dialog(){
            CraftData.selected_craft.new_name = CraftData.selected_craft.name;
            string resp = "";
            show_dialog("Rename Craft", "rename: " + CraftData.selected_craft.name, d =>{
                GUI.SetNextControlName("dialog_focus_field");
                CraftData.selected_craft.new_name = GUILayout.TextField(CraftData.selected_craft.new_name);
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Rename", CraftData.selected_craft.rename);
                });
                return resp;
            });
        }


        protected void delete_craft_dialog(){
            string resp = "";
            show_dialog("Delete Craft?", "Delete " + CraftData.selected_craft.name + "?\nAre you sure you want to do this?", d =>{                
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Delete", "button.delete", CraftData.selected_craft.delete);
                });
                return resp;
            });
        }

        protected void transfer_craft_dialog(){
            string resp = "";
            CraftData craft = CraftData.selected_craft;
            show_dialog("Transfer Craft", "Transfer this craft to:", d =>{
                section(()=>{
                    if(craft.construction_type != "SPH"){
                        button("The SPH", "button.large", ()=>{ resp = craft.transfer_to(EditorFacility.SPH); });
                    }
                    if(craft.construction_type != "VAB"){
                        button("The VAB", "button.large", ()=>{ resp = craft.transfer_to(EditorFacility.VAB); });
                    }
                    if(craft.construction_type != "Subassembly"){
                        button("Subassemblies", "button.large", ()=>{ resp = craft.transfer_to(EditorFacility.None); });
                    }
                });
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);                    
                });
                return resp;
            });
        }

        protected void move_copy_craft_dialog(){
            CraftData craft = CraftData.selected_craft;
            string resp = "";
            string selected_save = "";
            Dictionary<string, string> move_copy_save_menu = new Dictionary<string, string>(save_menu_options);
            List<string> keys = new List<string>(move_copy_save_menu.Keys);
            string key = keys.Find(k => (k.Equals(craft.save_dir) || k.Equals("Current Save (" + craft.save_dir + ")")));
            move_copy_save_menu.Remove(key);
            move_copy_save_menu.Remove("all");

            show_dialog("Move/Copy Craft", "Move or Copy this craft to another save:", false, d =>{
                section(500f, (inner_width)=>{
                    GUILayout.Space(inner_width*0.3f);
                    dropdown("Select Save", "copy_transfer_save_menu", move_copy_save_menu, d, inner_width*0.4f, "button.large", "menu.background", "menu.item", (selected_save_name) => {
                        resp = "";
                        selected_save = selected_save_name;
                    });           
                });
                section(()=>{
                    label("Selected Save: ", "h2");
                    label(selected_save, "h2");
                });
                section(()=>{
                    button("Move", "button.large", ()=>{resp = craft.move_copy_to(selected_save, true);});
                    button("Copy", "button.large", ()=>{resp = craft.move_copy_to(selected_save, false);});
                });
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);                    
                });
                return resp;
            });            
        }


        //Dialog Handler
        //All the above dialogs are created with this function and it handles all their common aspects and cam render them as modal dialogs.
        //It's a bit of a hacky solution. Its renders a full screen size window with GUI.ModalWindow which is skinned to appear as a box, 
        //and then renders a box which is skinned to look like a window in the middle. So the effect is a shaded out screen with a dialog
        //in the middle and only the dialog can be interacted with.

        public delegate string InnerDialogContent(DryUI dialog);

        protected DryDialog show_dialog(string title, string heading, InnerDialogContent content){
            return show_dialog(title, heading, Screen.height / 3, this.window_pos.x + (this.window_pos.width / 2) - (500 / 2), 500f, true, content);
        }
        protected DryDialog show_dialog(string title, string heading, bool modal, InnerDialogContent content){
            return show_dialog(title, heading, Screen.height / 3, this.window_pos.x + (this.window_pos.width / 2) - (500 / 2), 500f, modal, content);
        }
        protected DryDialog show_dialog(string title, string heading, float top, float left, float dialog_width, InnerDialogContent content){
            return show_dialog(title, heading, top, left, dialog_width, false, content);
        }

        protected DryDialog show_dialog(string title, string heading, float top, float left, float dialog_width, bool modal, InnerDialogContent content){
            close_dialog();
            string resp = "";
            int focus_count = 5;
            
            DialogContent dc = new DialogContent(d =>{
                style_override = "dialog.section";
                v_section(()=>{                    
                    label(heading, "h2");
                    if(!String.IsNullOrEmpty(resp)){label(resp, "error");}
                    resp = content(d);
                });
                if(focus_count > 0){
                    auto_focus_on = "dialog_focus_field";
                    focus_count--;
                }
                if(resp == "200"){
                    close_dialog();
                }
                Event e = Event.current;
                if (e.type == EventType.keyDown && e.keyCode == KeyCode.Escape) {
                    close_dialog();
                }
            });

            if(modal){                
                ModalDialog dialog = gameObject.AddOrGetComponent<ModalDialog>();
                dialog.dialog_pos = new Rect(left, top, dialog_width, 80f);
                dialog.window_title = title;
                dialog.content = dc;
                return dialog;

            } else{
                DryDialog dialog = gameObject.AddOrGetComponent<DryDialog>();
                dialog.window_pos = new Rect(left, top, dialog_width, 80f);
                dialog.window_title = title;
                dialog.content = dc;
                return dialog;

            }
        }

        bool submit_clicked = false;
        protected delegate string SubmitAction();
        protected string submit(string button_label, SubmitAction submit_action){
            return submit(button_label, "Button", submit_action);
        }
        protected string submit(string button_label, GUIStyle button_style, SubmitAction submit_action){
            submit_clicked = false;
            button(button_label, button_style, () =>{
                submit_clicked = true;    
            });
            Event e = Event.current;
            if (GUI.GetNameOfFocusedControl() == "dialog_focus_field" && e.type == EventType.keyDown && e.keyCode == KeyCode.Return) {
                submit_clicked = true;
                e.Use();
            }
            if(submit_clicked){
                return submit_action();
            } else{
                return "";
            }
        }

    }
}

