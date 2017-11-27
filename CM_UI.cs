using System;
using System.IO;
using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;

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
        private Dictionary<string, string>sort_options = new Dictionary<string, string>{
            {"name", "Name"}, {"cost", "Cost"}, {"crew_capacity", "Crew Capacity"}, {"mass", "Mass"}, {"part_count", "Part Count"}, {"stage_count", "Stages"}, {"date_created", "Created"}, {"date_updated", "Updated"}
        };

        public Dictionary<string, string> load_menu_options = new Dictionary<string, string>();
        public Dictionary<string, string> load_menu_options_default = new Dictionary<string, string> { { "merge", "Merge" }, { "subload", "Load as Subassembly" } };
        public Dictionary<string, string> load_menu_options_submode = new Dictionary<string, string> { { "merge", "Merge" }, { "load", "Load as Craft" } };

        private float save_menu_width = 0;
        private float sort_menu_width = 0;

        private string sort_opt = "name";
        private bool reverse_sort = false;
        

        private string auto_focus_on = null;
        private string new_tag_name = "";
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


            EditorLogic.fetch.saveBtn.onClick.AddListener(on_save_click);
            UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent();
            c.AddListener(on_load_click);
            EditorLogic.fetch.loadBtn.onClick = c;
            

            active_save_dir = HighLogic.SaveFolder;
            save_menu_options.Add(active_save_dir, "Current Save (" + active_save_dir + ")");
            foreach(string dir in Directory.GetDirectories(Paths.joined(CraftManager.ksp_root, "saves"))){
                string dir_name = dir.Replace(Paths.joined(CraftManager.ksp_root, "saves"), "").Replace("/","");
                if(dir_name != "training" && dir_name != "scenarios" && dir_name != active_save_dir){
                    save_menu_options.Add(dir_name, dir_name);
                }
            }
            save_menu_options.Add("all", "All");

            Tags.load();
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
        private void refresh(){
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
        }


        //Main GUI draw method (called by onGUI, see DryUI in KatLib).  Broken up into smaller sections to ease digestion and help prevent heart burn.
        //The GUI is main in 5 sections, top and bottom sections span the full width, while the LHS, RHS and main sections are columns.
        protected override void WindowContent(int win_id){
            v_section(()=>{                
                draw_top_section(window_width);     
                GUILayout.Space(10);
                scroll_relative_pos = GUILayoutUtility.GetLastRect();
                section(window_width, inner_width =>{
                    draw_left_hand_section(inner_width); //Tag list section
                    draw_main_section(inner_width);      //Main craft list
                    draw_right_hand_section(inner_width);//Craft details section
                });

                draw_bottom_section(window_width);

            });

            if(!String.IsNullOrEmpty(auto_focus_on)){  //When the UI opens set focus on the main search text field
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
                    foreach(string opt in selected_type_keys){
                        button(opt, "craft_type_sel" + (selected_types[opt] ? ".active" : ""), ()=>{type_select(opt, !selected_types[opt]);});
                    }
                    button("All", "craft_type_Sel", 30f, type_select_all);
                });
                fspace();
                if(save_menu_width == 0){
                    save_menu_width = GUI.skin.button.CalcSize(new GUIContent("Save: " + active_save_dir)).x;
                }
                dropdown("Save: " + active_save_dir, "save_menu", save_menu_options, this, save_menu_width, (resp) => {
                    active_save_dir = resp;
                    save_menu_width = GUI.skin.button.CalcSize(new GUIContent("Save: " + active_save_dir)).x;
                    refresh();
                });
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
                    button("include Stock Craft", "Label", ()=>{exclude_stock_craft = !exclude_stock_craft;});
                    if(exclude_stock_craft != prev_exstcr){
                        filter_craft();
                    }
                });
            });
        }



        //The Main craft list
        protected void draw_main_section(float section_width){
            v_section(section_width*0.55f, (inner_width)=>{
                last_search = search_string;
                section(()=>{
                    fspace();

                    if(sort_menu_width == 0){
                        sort_menu_width = GUI.skin.button.CalcSize(new GUIContent("Sort: " + sort_options[sort_opt])).x;
                    }
                    dropdown("Sort: " + sort_options[sort_opt], "sort_menu", sort_options, this, sort_menu_width, (resp) => {
                        sort_opt = resp;
                        sort_menu_width = GUI.skin.button.CalcSize(new GUIContent("Sort: " + sort_options[sort_opt])).x;
                        filter_craft();
                    });
                    button((reverse_sort ? "/\\" : "\\/"), 22f, toggle_reverse_sort);
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
                        if((bool)craft.locked_parts){
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
                    label("Tags", "h2");
                    //                    tag_mode_reduce = GUILayout.Toggle(tag_mode_reduce, "reduce", "Button", width(60f));
                    //                    tag_mode_reduce = !GUILayout.Toggle(!tag_mode_reduce, "extend", "Button", width(60f));

                    fspace();
                    edit_tags = GUILayout.Toggle(edit_tags, "edit", "Button", width(40f) );
                });


//                float tag_sec_height = GUI.skin.GetStyle("tag.toggle.label").CalcSize(new GUIContent("foo")).x * Tags.all.Count;
//                label("list: " + tag_sec_height + " scroll: " + main_section_height + " item: " + GUI.skin.GetStyle("tag.toggle.label").CalcSize(new GUIContent("foo")).x);


                scroll_pos["lhs"] = scroll(scroll_pos["lhs"], "side_panel.scroll", inner_width, main_section_height, scroll_width => {
                    foreach(KeyValuePair<string, Tag> pair in Tags.all){
                        Tag tag = pair.Value;

                        style_override = "tag.section";
                        section((sec_w)=>{
                            bool prev_state = tag.selected;
                            tag.selected = GUILayout.Toggle(tag.selected, "", "tag.toggle.light");
                            tag.selected = GUILayout.Toggle(tag.selected, tag.name + " - (" + tag.craft_count("filtered") + "/" + tag.craft_count("all") + ")", 
                                "tag.toggle.label", width(scroll_width-(edit_tags ? 60f : 35f))
                            );
                            if(prev_state != tag.selected){
                                filter_craft();                                    
                            }
                            if(edit_tags){
                                if(GUILayout.Button("X", "tag.delete_button.x")){
                                    delete_tag_dialog(tag);
                                }
                            }
                        });
                    }
                });
            });
        }


        protected Rect scroll_relative_pos = new Rect(0, 0, 0, 0);




        protected void draw_right_hand_section(float section_width){
            v_section(section_width * 0.25f, (inner_width) =>{                
                label("Craft Details", "h2");

//                style_override = skin.scrollView;
//                v_section(inner_width, main_section_height, (scroll_width)=>{
                    
//                })
                scroll_pos["rhs"] = scroll(scroll_pos["rhs"], "side_panel.scroll", inner_width, main_section_height, scroll_width => {
                    if(CraftData.selected_craft != null){
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


                        GUILayout.Space(15);

                        section((w) => {
                            button("edit description", edit_description_dialog);
                        });
                        section((w) => {
                            button("transfer", transfer_craft_dialog);
                            button("move/copy", delete_craft_dialog);
                        });
                        section((w) => {
                            button("rename", rename_craft_dialog);
                            button("delete", delete_craft_dialog);
                        });

                        button("reload", ()=>{CraftData.selected_craft.initialize(CraftData.selected_craft.path);});

                        GUILayout.Space(15);

                        section((w) =>{
                            label("Tags", "h2");
                            fspace();
                            scroll_relative_pos.x += (window_pos.width * (0.55f+0.2f)) - 5f;
                            scroll_relative_pos.y += 45f - scroll_pos["rhs"].y;
                            dropdown("Add Tag", "add_tag_menu", Tags.names, this, scroll_relative_pos, 70f, "Button", "menu.background", "menu.item.small", resp => {
                                Tags.tag_craft(Tags.craft_reference_key(craft), resp);
                            });
                        });
                   
                        foreach(string tag in Tags.tags_for(Tags.craft_reference_key(craft))){
                            section(() =>{
                                label(tag);    
                                fspace();
                                button("x", "tag.delete_button.x", ()=>{
                                    Tags.untag_craft(Tags.craft_reference_key(craft), tag);                                    
                                });
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
            });
        }

        protected void draw_bottom_section(float section_width){
            section(section_width,(inner_width) =>{
                new_tag_name = GUILayout.TextField(new_tag_name, width(200f));

                button("Add", 40f, ()=>{                    
                    Tags.add(new_tag_name);
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





        protected void clear_search(){
            search_string = "";
            filter_craft();
        }
        protected void toggle_reverse_sort(){
            reverse_sort = !reverse_sort;
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
                        DryDialog dialog = show_dialog(d =>{
                            style_override = "dialog.section";
                            v_section(()=>{
                                label("The Current Craft has unsaved changes", "h2");
                                if(GUILayout.Button("Load Anyway")){
                                    load_craft(load_type, true);
                                    close_dialog();
                                }
                                GUILayout.Space(10);
                                if(GUILayout.Button("Save Current Craft first")){
                                    string path = ShipConstruction.GetSavePath(EditorLogic.fetch.ship.shipName);
                                    EditorLogic.fetch.ship.SaveShip().Save(path);
                                    load_craft(load_type, true);
                                    close_dialog();
                                }
                                GUILayout.Space(10);
                                if(GUILayout.Button("Cancel")){
                                    close_dialog();
                                }
                            });
                        });
                        dialog.window_title = "Confirm Load";
                        dialog.window_pos = new Rect(this.window_pos.x + (this.window_pos.width / 2) - (dialog.window_pos.width / 2), 
                            this.window_pos.y + (this.window_pos.height / 2), 400, 80
                        );
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

        protected void delete_tag_dialog(Tag tag){
            close_dialog();
            DryDialog dialog = show_dialog((d)=>{                
                style_override = "dialog.section";
                v_section(()=>{
                    if(tag.craft_count("all") > 0){
                        GUILayout.Label("This tag is used for " + tag.craft_count("all") + " craft.");
                    }
                    GUILayout.Label("Are you sure you want to delete this tag?");
                    section(()=>{
                        GUILayout.FlexibleSpace();
                        if(GUILayout.Button("Cancel")){close_dialog();}
                        if(GUILayout.Button("Delete", "tag.delete_button")){
                            Tags.remove(tag.name);close_dialog();
                        };
                    });
                });
            });
            dialog.window_pos.width = 400f;
//            dialog.window_pos.height = 120f;
            dialog.window_pos.x = Event.current.mousePosition.x + window_pos.x;
            dialog.window_pos.y = Event.current.mousePosition.y + window_pos.y + 140;
            dialog.window_title = "Confirm Tag Delete";       

        }

        protected void edit_description_dialog(){
            if(CraftData.selected_craft.description == null){
                CraftData.selected_craft.description = "";
            }
            string exception_message = null;
            int focus_count = 5;
            show_dialog(d =>{
                d.window_title = "Edit Description";
                style_override = "dialog.section";
                v_section(() =>{
                    label("Edit Description", "h2");
                    if(!String.IsNullOrEmpty(exception_message)){label(exception_message, "error");}
                    GUI.SetNextControlName("edit_description_field");
                    CraftData.selected_craft.description = GUILayout.TextArea(CraftData.selected_craft.description);
                    section(()=>{
                        fspace();
                        button("Cancel", close_dialog);
                        button("Save", ()=>{
                            string resp = CraftData.selected_craft.save_description();
                            exception_message = resp;
                            if(resp == "200"){
                                close_dialog();
                            }
                        });
                    });
                });
                if(focus_count > 0){
                    auto_focus_on = "edit_description_field";
                    focus_count--;
                }
            });
        }

        protected void rename_craft_dialog(){
            CraftData.selected_craft.new_name = CraftData.selected_craft.name;
            string exception_message = null;
            int focus_count = 5;
            show_dialog(d =>{
                d.window_title = "Rename Craft";
                style_override = "dialog.section";
                v_section(() =>{
                    label("rename: " + CraftData.selected_craft.name, "h2");
                    if(!String.IsNullOrEmpty(exception_message)){label(exception_message, "error");}
                    GUI.SetNextControlName("rename_craft_field");
                    CraftData.selected_craft.new_name = GUILayout.TextField(CraftData.selected_craft.new_name);
                    section(()=>{
                        fspace();
                        button("Cancel", close_dialog);
                        button("Rename", ()=>{
                            string resp = CraftData.selected_craft.rename();
                            exception_message = resp;
                            if(resp == "200"){
                                close_dialog();
                            }
                        });
                    });
                });
                if(focus_count > 0){
                    auto_focus_on = "rename_craft_field";
                    focus_count--;
                }
            });
        }

        protected void delete_craft_dialog(){
            string exception_message = null;
            show_dialog(d =>{
                d.window_title = "Delete Craft?";
                style_override = "dialog.section";
                v_section(() =>{
                    label("Delete " + CraftData.selected_craft.name + "?", "h2");
                    label("Are you sure you want to do this?", "h2");
                    if(!String.IsNullOrEmpty(exception_message)){label(exception_message, "error");}
                    section(()=>{
                        fspace();
                        button("Cancel", close_dialog);
                        button("Delete", ()=>{
                            string resp = CraftData.selected_craft.delete();
                            exception_message = resp;
                            if(resp == "200"){
                                close_dialog();
                                refresh();
                            }
                        });
                    });
                });
            });
        }

        protected void transfer_craft_dialog(){
            string resp = "";
            CraftData craft = CraftData.selected_craft;
            show_dialog(d =>{
                d.window_title = "Transfer Craft";
                style_override = "dialog.section";
                v_section(() =>{
                    label("Transfer this craft to:", "h2");
                    if(!String.IsNullOrEmpty(resp)){label(resp, "error");}
                    section(()=>{
                        if(craft.construction_type != "SPH"){
                            button("The SPH", ()=>{ resp = craft.transfer_to(EditorFacility.SPH); });
                        }
                        if(craft.construction_type != "VAB"){
                            button("The VAB", ()=>{ resp = craft.transfer_to(EditorFacility.VAB); });
                        }
                        if(craft.construction_type != "Subassembly"){
                            button("Subassemblies", ()=>{ resp = craft.transfer_to(EditorFacility.None); });
                        }
                        if(resp == "200"){
                            close_dialog();
                        }
                    });
                    section(()=>{
                        fspace();
                        button("Cancel", close_dialog);                    
                    });
                });
            });
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

