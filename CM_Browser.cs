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
    public class CMBrowser : CMUI
    {
        public const string all_saves_ref = "<all_saves>";

        private float main_section_height = Screen.height - 400f;
        private float window_width  = 1000f;
        private float[] col_widths = new float[]{0.2f,0.55f,0.25f};


        private string current_save_dir = HighLogic.SaveFolder;
        private string active_save_dir;

        private DropdownMenuData save_menu_options = new DropdownMenuData();
        private DropdownMenuData tags_menu_content = new DropdownMenuData();
        private DropdownMenuData load_menu_options_default = new DropdownMenuData(new Dictionary<string, string> { { "merge", "Merge" }, { "subload", "Load as Subassembly" } });
        private DropdownMenuData load_menu_options_submode = new DropdownMenuData(new Dictionary<string, string> { { "merge", "Merge" }, { "load", "Load as Craft" } });
        private DropdownMenuData sort_options = new DropdownMenuData(new Dictionary<string, string>{
            {"name", "Name"}, {"cost", "Cost"}, {"crew_capacity", "Crew Capacity"}, {"mass", "Mass"}, {"part_count", "Part Count"}, {"stage_count", "Stages"}, {"date_created", "Created"}, {"date_updated", "Updated"}
        });
        private DropdownMenuData tag_sort_options = new DropdownMenuData(new Dictionary<string, string> { {"name", "Name"}, {"craft_count", "Craft Count"} });
        private DropdownMenuData tag_filter_modes = new DropdownMenuData(new List<string> { "AND", "OR" });


        private Dictionary<string, bool> selected_types = new Dictionary<string, bool>(){
            {"SPH",EditorDriver.editorFacility.CompareTo(EditorFacility.SPH)==0},
            {"VAB",EditorDriver.editorFacility.CompareTo(EditorFacility.VAB)==0},
            {"Subassemblies",false} 
        };
        private int selected_type_count = 1;
        protected List<string> selected_type_keys = new List<string>(){"SPH", "VAB", "Subassemblies"};


        private string sort_opt = CraftManager.settings.get("craft_sort");
        private bool reverse_sort = bool.Parse(CraftManager.settings.get("craft_sort_reverse"));
        public bool exclude_stock_craft = bool.Parse(CraftManager.settings.get("exclude_stock_craft"));
        public string tag_sort_by = CraftManager.settings.get("sort_tags_by");
        public string tag_filter_mode = CraftManager.settings.get("tag_filter_mode"); //"AND"; // OR

        string load_button_text = "Load";
        string load_button_action = "load";

        private string search_string = "";
        private string last_search = "";


        float load_button_width = 120f;
        private float save_menu_width = 0; //autoset based on content width
        private float sort_menu_width = 0;

        private string auto_focus_on = null;
        public bool stock_craft_loaded = false;
        private bool edit_tags = false;
        private bool expand_details = false;
        private bool tag_prev_state = false;
        private bool tag_state = false;
        private float tag_content_height = 0;
        private float last_tag_content_height = 0;
        private float tag_margin_offset = 0;
        private float tag_scroll_height = 0;
        private bool ctrl_key_down = false;


        //collection of Vector2 objects to track scroll positions
        private Dictionary<string, Vector2> scroll_pos = new Dictionary<string, Vector2>(){
            {"lhs", new Vector2()}, {"rhs", new Vector2()}, {"main", new Vector2()}
        };
        protected Rect scroll_relative_pos = new Rect(0, 0, 0, 0); //used to track the position of scroll sections. needed to render dropdown menus inside scroll section.



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
            draggable = false;
            footer = false;
            prevent_click_through = false; //disable the standard click through prevention. show and hide will add control locks which are not based on mouse pos.

            EditorLogic.fetch.saveBtn.onClick.AddListener(on_save_click); //settup click event on the stock save button.
            //override existing ations on stock load button and replace with call to toggle CM's UI.
            if(CraftManager.replace_editor_load_button){
                UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent(); 
                c.AddListener(on_load_click);
                EditorLogic.fetch.loadBtn.onClick = c;
            }

            //Initialize list of Save directories, used in save select menus.
            active_save_dir = HighLogic.SaveFolder;
            save_menu_options.items.Add(active_save_dir, "Current Save (" + active_save_dir + ")");
            foreach(string dir_name in CraftData.save_names()){
                if(dir_name != active_save_dir){
                    save_menu_options.items.Add(dir_name, dir_name);
                }
            }
            save_menu_options.items.Add(all_saves_ref, "All");

            Tags.load(active_save_dir);
            CraftManager.log(CraftManager.settings.get("craft_sort"));

            tags_menu_content.remote_data = new DataSource(() => Tags.names);
            tags_menu_content.special_items.Add("new_tag", "New Tag");
//            show();
        }

        protected override void on_show(){            
            stock_craft_loaded = false;
            refresh();
            auto_focus_on = "main_search_field";
            InputLockManager.SetControlLock(window_id.ToString());
            interface_locked = true;

        }

        protected override void on_hide(){
            close_dialog(); //incase any dialogs have been left open
//            InputLockManager.RemoveControlLock(window_id.ToString());
        }


        //load/reload craft from the active_save_dir and apply any active filters
        public void refresh(){
            CraftManager.log("Refreshing data");
            CraftData.load_craft_from_files(active_save_dir==all_saves_ref ? null : active_save_dir);
            filter_craft();
        }

        protected void clear_search(){
            search_string = "";
            filter_craft();
        }

        protected void select_sort_option(string option){
            sort_opt = option;
            sort_menu_width = GUI.skin.button.CalcSize(new GUIContent("Sort: " + sort_options.items[sort_opt])).x; //recalculate the sort menu button width
            filter_craft();
            CraftManager.settings.set("craft_sort", sort_opt);
        }

        protected void toggle_reverse_sort(){
            reverse_sort = !reverse_sort;
            filter_craft();
            CraftManager.settings.set("craft_sort_reverse", reverse_sort.ToString());
        }

        protected void change_save(string save_name){
            active_save_dir = save_name;
            save_menu_width = GUI.skin.button.CalcSize(new GUIContent("Save: " + active_save_dir)).x;
            Tags.load(active_save_dir);
            refresh();
        }

        protected void change_tag_sort(string sort_by){
            tag_sort_by = sort_by;
            Tags.sort_tag_list();
            CraftManager.settings.set("sort_tags_by", tag_sort_by);
        }

        protected void change_tag_filter_mode(string mode){
            tag_filter_mode = mode;
            filter_craft();
            CraftManager.settings.set("tag_filter_mode", tag_filter_mode);
        }

        //Collect any currently active filters into a Dictionary<string, object> which can then be passed to 
        //filter_craft on CraftData (which does the actual filtering work).
        private void filter_craft(){
            if(!exclude_stock_craft && !stock_craft_loaded){
                CraftData.load_stock_craft_from_files();
            }

            Dictionary<string, object> search_criteria = new Dictionary<string, object>();
            search_criteria.Add("search", search_string);
            search_criteria.Add("type", selected_types);
            List<string> s_tags = Tags.selected_tags();
            if(s_tags.Count > 0){
                search_criteria.Add("tags", s_tags);
                search_criteria.Add("tag_filter_mode", tag_filter_mode);
            }
            search_criteria.Add("sort", sort_opt);
            search_criteria.Add("reverse_sort", reverse_sort);
            search_criteria.Add("exclude_stock", exclude_stock_craft);

            CraftData.filter_craft(search_criteria);
            Tags.sort_tag_list();
        }



        //Main GUI draw method (called by onGUI, see DryUI in KatLib).  Broken up into smaller sections to ease digestion and help prevent heart burn.
        //The GUI is main in 5 sections, top and bottom sections span the full width, while the LHS, RHS and main sections are columns.
        protected override void WindowContent(int win_id){
            key_event_handler();
            v_section(()=>{                
                draw_top_section(window_width);     
                GUILayout.Space(10);
                scroll_relative_pos = GUILayoutUtility.GetLastRect();
                section(window_width, inner_width =>{
                    draw_left_hand_section(inner_width * col_widths[0]); //Tag list section
                    draw_main_section(inner_width * col_widths[1]);      //Main craft list
                    draw_right_hand_section(inner_width * col_widths[2]);//Craft details section
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
                save_menu_options.selected_item = active_save_dir;
                dropdown("Save: " + (active_save_dir==all_saves_ref ? "All Saves" : active_save_dir), "save_menu", save_menu_options, this, save_menu_width, change_save);
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
                        CraftManager.settings.set("exclude_stock_craft", exclude_stock_craft.ToString());
                    }
                });
            });
        }


        //The Main craft list
        float item_last_height = 0;
        Rect craft_scroll_section = new Rect();
        protected void draw_main_section(float section_width){
            v_section(section_width, (inner_width)=>{

                //sort menu
                section((w)=>{
                    fspace();
                    if(sort_menu_width == 0){ //calculate the initial sort menu button width. should only happen on the first pass
                        sort_menu_width = GUI.skin.button.CalcSize(new GUIContent("Sort: " + sort_options.items[sort_opt])).x;
                    }
                    //render dropdown menu of sort options
                    sort_options.selected_item = sort_opt;
                    dropdown("Sort: " + sort_options.items[sort_opt], "sort_menu", sort_options, this, sort_menu_width, "button.tight", select_sort_option);
                    button((reverse_sort ? "/\\" : "\\/"), "button.tight.right_margin", 22f, toggle_reverse_sort); //TODO replace with proper icons.
                });

                //Main craft list scrolling section
                scroll_pos["main"] = scroll(scroll_pos["main"], "craft.list_container", inner_width, main_section_height, craft_list_width => {
                    item_last_height = 0;
                    foreach(CraftData craft in CraftData.filtered){
                        draw_craft_list_item(craft, craft_list_width); //render each craft

                        //this is used to get the top offset position of each item in the craft list and that is stored on the CraftData object
                        //facilitates maintaining focus on list items when using the up/down arrow keys to scroll.
                        if(Event.current.type == EventType.Repaint){
                            craft.list_position = item_last_height;
                            item_last_height += GUILayoutUtility.GetLastRect().height + 5; //+5 for margin
                        }
                    }
                });

                //an attempt at enabling the user to drag the craft list up and down. it works, but can be a bit laggy (but then so is the stock list when dragged)
                //and I can't make it so the user can drag it fast and let go and have it keep sliding. 
                craft_scroll_section = GUILayoutUtility.GetLastRect();
                if(craft_scroll_section.Contains(Event.current.mousePosition)){
                    if(Event.current.button == 0 && Event.current.type == EventType.MouseDrag){
                        scroll_pos["main"] -= Event.current.delta;
                        Event.current.Use();
                    }
                }
            });            
        }


        //Individual Craft Content
        protected void draw_craft_list_item(CraftData craft, float section_width){
            section(section_width-(30f), "craft.list_item" + (craft.selected ? ".selected" : ""), (inner_width)=>{ //subtractions from width to account for margins and scrollbar
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
                            label("This craft has locked parts", "craft.locked_parts");
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
                    GUIUtility.keyboardControl = 0;
                    CraftData.toggle_selected(craft);  
                }
                if(evt.double_click){
                    CraftData.select_craft(craft);
                    load_craft( craft.construction_type=="Subassembly" ? "subload" : "load");
                }
            });
        }


        //Left Hand Section: Tags
        protected void draw_left_hand_section(float section_width){
            tag_scroll_height = main_section_height-40f;    

            //adjustments to tag list width depending on if the edit buttons are shown and if the scroll bar is shown.
            tag_content_height = 0;
            tag_margin_offset = (edit_tags ? 82f : 30f);
            if(last_tag_content_height > tag_scroll_height){
                tag_margin_offset+=20f;
            }
            
            v_section(section_width, (inner_width) =>{
                section(()=>{
                    label("Tags", "h2");
                    fspace();
                    tag_sort_options.selected_item = tag_sort_by;
                    dropdown("Sort", "tag_sort_menu", tag_sort_options, this, 50f, change_tag_sort);
                });

                v_section(inner_width, "tags.list_outer", (tag_list_width) => {                    
                    scroll_pos["lhs"] = scroll(scroll_pos["lhs"], "side_panel.scroll.tags", inner_width, tag_scroll_height, scroll_width => {
                        for(int i=0; i < Tags.names.Count; i++){
                            string tag_name = Tags.names[i];
                            style_override = "tag.section";
                            section((sec_w)=>{
                                tag_state = Tags.is_selected(tag_name);
                                tag_prev_state = tag_state;

                                string s = "(" + Tags.craft_count_for(tag_name,(tag_filter_mode=="AND" ? "filtered" : "raw_count")) + ")";
                                float w = skin.button.CalcSize(new GUIContent(s)).x;
                                
                                tag_state = GUILayout.Toggle(tag_state, "", "tag.toggle.light");
                                button(tag_name, "tag.toggle.label", scroll_width - w - tag_margin_offset, ()=>{tag_state = !tag_state;});
                                button(s, "tag.toggle.count", w, ()=>{tag_state = !tag_state;});
                                
                                if(tag_prev_state != tag_state){
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

                            if(Event.current.type == EventType.Repaint){                                
                                tag_content_height += GUILayoutUtility.GetLastRect().height+5; //+5 for margin
                            }

                        }
                    });
                    section(tag_list_width, 40f, ()=>{       
                        tag_filter_modes.selected_item = tag_filter_mode;
                        dropdown("Mode", "tag_filter_mode_menu", tag_filter_modes, this, 50f, change_tag_filter_mode);
                        fspace();
                        edit_tags = GUILayout.Toggle(edit_tags, "edit", "button", width(40f) );
                        button("+", 30f, create_tag_dialog);
                    });
                });


            });
            if(tag_content_height != last_tag_content_height && tag_content_height != 0){
                last_tag_content_height = tag_content_height-2;
            }
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
                            scroll_relative_pos.x += (window_pos.width * (col_widths[0]+col_widths[1])) - 5f;
                            scroll_relative_pos.y += 45f - scroll_pos["rhs"].y;
                            List<string> craft_tags = craft.tags();
                            tags_menu_content.selected_items = craft_tags;
                            dropdown("Add Tag", "add_tag_menu", tags_menu_content, this, scroll_relative_pos, 70f, "Button", "menu.background", "menu.item.small", resp => {
                                if(resp == "new_tag"){
                                    create_tag_dialog(craft);
                                }else{
                                    if(craft_tags.Contains(resp)){
                                        Tags.untag_craft(craft, resp);
                                    }else{                                    
                                        Tags.tag_craft(craft, resp);
                                    }
                                }
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
            
            section((w) =>{
                fspace();

                gui_state(CraftData.selected_craft != null, ()=>{                    
                    load_button_text = "Load";
                    load_button_action = "load";
                    load_button_width = 120f;
                    if(CraftData.selected_craft != null && CraftData.selected_craft.construction_type == "Subassembly"){                        
                        load_button_text = "Load Subassembly";
                        load_button_action = "subload";
                        load_button_width = 300f;
                    }
                    
                    button(load_button_text, "button.load", load_button_width, ()=>{ load_craft(load_button_action);});
                    dropdown("\\/", "load_menu", (load_button_action=="subload" ? load_menu_options_submode : load_menu_options_default), this, 30f, "button.load", "menu.background", "menu.item", resp => {
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
            if(!ctrl_key_down){
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

        protected void create_tag_dialog(){
            create_tag_dialog(null);
        }

        protected void create_tag_dialog(CraftData add_to_tag = null){
            float dialog_width = 400f;
            float top = scroll_relative_pos.y + main_section_height;
            float left = scroll_relative_pos.x + window_width * col_widths[0];
            string resp = "";
            string new_tag_name = "";
            string save_dir_for_tag = current_save_dir;
            if(add_to_tag != null){
                save_dir_for_tag = add_to_tag.save_dir;
            }
            show_dialog("Create Tag", "", top, left, dialog_width, true, d =>{
                GUI.SetNextControlName("dialog_focus_field");
                new_tag_name = GUILayout.TextField(new_tag_name);
                section((w)=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Save", ()=>{
                        return Tags.create(new_tag_name, save_dir_for_tag, add_to_tag);

                    });
                });
                return resp;
            });
        }

        protected void delete_tag_dialog(string tag_name){            
            string resp = "";
            int craft_count = Tags.craft_count_for(tag_name, all_saves_ref);
            float top = Event.current.mousePosition.y + window_pos.y + 140;
            float left = Event.current.mousePosition.x + window_pos.x;
            show_dialog("Delete Tag", "Are you sure you want to delete this tag?", top, left, 400f, true, d =>{
                
                if(craft_count > 0){
                    GUILayout.Label("This tag is used for " + craft_count + " craft.");
                    label("deleting tags will not delete any craft");
                }
                if(active_save_dir == all_saves_ref){
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
                if(active_save_dir == all_saves_ref){
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

            bool switch_to_editor = false;
            Dictionary<string, EditorFacility> lookup = new Dictionary<string, EditorFacility>{
                {"SPH", EditorFacility.SPH}, {"VAB",EditorFacility.VAB},{"Subassembly",EditorFacility.None}
            };
            int selected_transfer_option = -1;
            List<string> transfer_opts = new List<string>();
            if(craft.construction_type != "SPH"){transfer_opts.Add("SPH");}
            if(craft.construction_type != "VAB"){transfer_opts.Add("VAB");}
            if(craft.construction_type != "Subassembly"){transfer_opts.Add("Subassembly");}
            string[] transfer_options = transfer_opts.ToArray();
            string opt = null;  

            show_dialog("Transfer Craft", "Transfer this craft to:", d =>{
                section(()=>{
                    selected_transfer_option = GUILayout.SelectionGrid(selected_transfer_option, transfer_options, 2, "button.large");
                });

                GUILayout.Space(10);
                if(selected_transfer_option == -1){
                    label("Select one of the above options and then click confirm");
                }else{
                    opt = transfer_options[selected_transfer_option];
                    label("Click Confirm to make this craft a " + opt + (opt=="Subassembly" ? "." : " craft."), "h2");
                    if(opt != "Subassembly"){
                        section(()=>{
                            switch_to_editor = GUILayout.Toggle(switch_to_editor, "");
                            button("Switch to " + opt + " and load craft", "bold", ()=>{
                                switch_to_editor = !switch_to_editor;    
                            });
                        });
                    }
                }
                gui_state(selected_transfer_option != -1, ()=>{
                    resp = submit("Confirm", "button.large", ()=>{                        
                        string response =  craft.transfer_to(lookup[opt]);
                        if(switch_to_editor){
                            EditorDriver.StartAndLoadVessel(craft.path, lookup[opt]);
                        }
                        return response;
                    });
                });
                GUILayout.Space(10);
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

            DropdownMenuData move_copy_save_menu = new DropdownMenuData(new Dictionary<string, string>(save_menu_options.items));
            List<string> keys = new List<string>(move_copy_save_menu.items.Keys);
            string key = keys.Find(k => (k.Equals(craft.save_dir) || k.Equals("Current Save (" + craft.save_dir + ")")));
            move_copy_save_menu.items.Remove(key);
            move_copy_save_menu.items.Remove(all_saves_ref);

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
            string response = "";
            string err_msg = "";
            int focus_count = 5;
            //wrapper for the given content which adds some of the common functionality
            DialogContent dc = new DialogContent(d =>{               
                //close on escape key press
                Event e = Event.current;
                if (e.type == EventType.keyDown && e.keyCode == KeyCode.Escape) {
                    close_dialog();
                }
                    
                //main dialog
                style_override = "dialog.section";
                v_section(()=>{          
                    if(!String.IsNullOrEmpty(heading)){
                        label(heading, "h2");
                    }
                    if(!String.IsNullOrEmpty(err_msg)){label(err_msg, "error");}    //display error message if any
                    response = content(d);                                          //render main dialog content which will return a response string.
                    if(!String.IsNullOrEmpty(response) && response != "200"){       //collect any error message returned (as next pass response will be empty again).
                        err_msg = response;
                    }
                });

                //autofocus on textfield/area - the reason for the focous_count countdown is because we only want to focus on the field just after creating the dialog
                //but one the first (few) passes it doesn't appear to be ready, so this slightly hacky solution keeps setting the focus for first 5 passes.  
                if(focus_count > 0){
                    auto_focus_on = "dialog_focus_field";
                    focus_count--;
                }
                //close dialog on OK response
                if(response == "200"){
                    close_dialog();
                }
            });

            if(modal){                
                ModalDialog dialog = gameObject.AddOrGetComponent<ModalDialog>();
                dialog.dialog_pos = new Rect(left, top, dialog_width, 80f);
                dialog.window_title = title;
                dialog.content = dc;
                dialog.skin = CraftManager.skin;
                return dialog;

            } else{
                DryDialog dialog = gameObject.AddOrGetComponent<DryDialog>();
                dialog.window_pos = new Rect(left, top, dialog_width, 80f);
                dialog.window_title = title;
                dialog.content = dc;
                dialog.skin = CraftManager.skin;
                return dialog;

            }
        }


        //Submit Button Helper
        //string response = submit("text", "style", ()=>{ return shit_that_happens_on_submit })
        //creates a button which performs whatever actions are describe in the delegate passed to the function
        //and also sets up detection of enter key press which will call the same actions.
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
            if (GUI.GetNameOfFocusedControl() == "dialog_focus_field" && e.type == EventType.keyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) {
                submit_clicked = true;
                e.Use();
            }
   
            if(submit_clicked){
                return submit_action();
            } else{
                return "";
            }
        }




        protected void key_event_handler(){
            if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)){
                ctrl_key_down = true;
            } else{
                ctrl_key_down = false;
            }

            Event e = Event.current;

            if(e.type == EventType.keyDown){
                if(e.type == EventType.keyDown && e.keyCode == KeyCode.Escape) {
                    e.Use();
                    this.hide();
                }else if(GUI.GetNameOfFocusedControl() != "main_search_field" && ctrl_key_down && e.keyCode == KeyCode.F){
                    GUI.FocusControl("main_search_field");
                    e.Use();
                }else if(GUI.GetNameOfFocusedControl() != "main_search_field" && ctrl_key_down && e.keyCode == KeyCode.T){
                    create_tag_dialog();
                    e.Use();
                } else if(e.keyCode == KeyCode.UpArrow){
                    jump_to_craft(CraftData.filtered.IndexOf(CraftData.selected_craft) - 1);
                    e.Use();
                } else if(e.keyCode == KeyCode.DownArrow){
                    jump_to_craft(CraftData.filtered.IndexOf(CraftData.selected_craft) + 1);
                    e.Use();

                }else if(GUI.GetNameOfFocusedControl() != "main_search_field" && CraftData.selected_craft != null){
                    if(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter){                       
                        load_craft(CraftData.selected_craft.construction_type == "Subassembly" ? "subload" : "load");
                    }                
                } else if(GUI.GetNameOfFocusedControl() == "main_search_field" && e.keyCode == KeyCode.Tab){
                    jump_to_craft(0);
                    e.Use();            
                }
            }
        }

        protected void jump_to_craft(int index){   
            GUIUtility.keyboardControl = 0;
            if(index < 0){
                index = 0;
            } else if(index > CraftData.filtered.Count-1){
                index = CraftData.filtered.Count - 1;
            }
            CraftData.select_craft(CraftData.filtered[index]);
            scroll_pos["main"] = new Vector2(scroll_pos["main"].x, CraftData.selected_craft.list_position - (main_section_height*0.4f));
        }

    }
}
