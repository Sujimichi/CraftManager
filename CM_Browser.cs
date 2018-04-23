using System;
using System.IO;
using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;
using ExtensionMethods;
using SimpleJSON;

using KatLib;

namespace CraftManager
{

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CMBrowser : CMUI
    {
        public static string ops_count = ""; //just a test variable

        public const string all_saves_ref = "<all_saves>";

        private float main_section_height = Screen.height - 400f;
        private float window_width  = 1000f;
        private float[] col_widths_default = new float[]{0.2f,0.55f,0.25f};
        private float[] col_widths_current = new float[]{0.2f,0.55f,0.25f};


        public string current_save_dir = HighLogic.SaveFolder; //The dir of save which is currently being played
        public string active_save_dir = HighLogic.SaveFolder;   //The dir of save which has been selected in UI

        public delegate void DialogAction();

        private DropdownMenuData save_menu_options= new DropdownMenuData();
        private DropdownMenuData tags_menu_content= new DropdownMenuData();
        private DropdownMenuData inline_tag_menu;
        private DropdownMenuData tag_sort_options = new DropdownMenuData(new Dictionary<string, string> { {"name", "Name"}, {"craft_count", "Craft Count"} });
        private DropdownMenuData tag_filter_modes = new DropdownMenuData(new List<string> { "AND", "OR" });
        private DropdownMenuData upload_image_mode= new DropdownMenuData(new List<string> { "thumb", "list" });
        private DropdownMenuData more_menu = new DropdownMenuData(new List<string> { "settings", "help" });
        private DropdownMenuData sort_options = new DropdownMenuData(new Dictionary<string, string>{
            {"name", "Name"}, {"cost", "Cost"}, {"crew_capacity", "Crew Capacity"}, {"mass", "Mass"}, {"part_count", "Part Count"}, {"stage_count", "Stages"}, {"date_created", "Created"}, {"date_updated", "Updated"}
        });


        private struct MenuOptions{
            public string text;
            public string action;
            public float width;
            public DropdownMenuData menu;
        }
        private Dictionary<string, MenuOptions> load_menu = new Dictionary<string, MenuOptions>{
            {"default", new MenuOptions{text = "Load", action = "load", width = 120f, menu = new DropdownMenuData(new Dictionary<string, string> { { "merge", "Merge" }, { "subload", "Load as Subassembly" } })}},
            {"submode", new MenuOptions{text = "Load Subassembly", action = "subload", width = 300f, menu = new DropdownMenuData(new Dictionary<string, string> { { "merge", "Merge" }, { "load", "Load as Craft" } })}},
            {"download",new MenuOptions{text = "Download", action = "download", width = 200f, menu = new DropdownMenuData(new Dictionary<string, string> { {"dl_load", "Download & Load"}, { "dl_load_no_save", "Load without saving" } })}},
            {"redownload",new MenuOptions{text = "Update", action = "update", width = 200f, menu = new DropdownMenuData(new Dictionary<string, string> { {"update_load", "Update & Load"}, { "dl_load_no_save", "Load Remote version without saving" }, { "load", "Load local version" } })}},
            {"upload",  new MenuOptions{text = "Upload", action = "upload", width = 150f, menu = null}}
        };
        string load_menu_mode = "default";


        internal Dictionary<string, bool> selected_types = new Dictionary<string, bool>() { { "SPH", false }, { "VAB", false }, { "Subassemblies", false } };
        internal int selected_type_count = 1;
        protected List<string> selected_type_keys = new List<string>(){"SPH", "VAB", "Subassemblies"};


        public string sort_opt = CraftManager.settings.get("craft_sort");
        private bool reverse_sort = bool.Parse(CraftManager.settings.get("craft_sort_reverse"));
        public bool exclude_stock_craft = bool.Parse(CraftManager.settings.get("exclude_stock_craft"));
        public string tag_sort_by = CraftManager.settings.get("sort_tags_by");
        public string tag_filter_mode = CraftManager.settings.get("tag_filter_mode"); //"AND"; // OR


        private string search_string = "";
        private string last_search = "";

        private float save_menu_width = 0; //autoset based on content width
        private float sort_menu_width = 0;

        private string auto_focus_field = null;
        private CraftData auto_focus_craft = null;
        private int auto_focus_countdown = 0;
        public bool stock_craft_loaded = false;
        private bool edit_tags = false;
        private bool expand_details = false;
        private bool tag_prev_state = false;
        private bool tag_state = false;
        private bool ctrl_key_down = false;
        private bool archived_tag = false;
        private bool open_tag_menu = false;
        private float tag_content_height = 0;
        private float last_tag_content_height = 0;
        private float tag_margin_offset = 0;
        private float tag_scroll_height = 0;
        private float section_header_height = 38f;
        private float item_last_height = 0;

        public bool show_transfer_indicator = false;
        public bool transfer_is_upload = true;

        protected int prog_pos = 0;
        protected long prog_timer = 0;
        protected int prog_interval = 200;

        private bool craft_list_overflow = false;
        private bool craft_list_drag_active = false;
        private Vector2 craft_list_drag_force = new Vector2();


        //KerbalX specific stuff
        public bool kerbalx_mode = false;  //displaying remote craft from kerbalx
        public bool show_upload_interface = false;   //when set to true starts the transition to the upload interface. 
        public bool exit_kerbalx_mode_after_close = false;
        protected bool upload_interface_ready = false;  //The transition is compelte when upload interface ready is set to true
        protected bool show_headers = true;             //'headers' of the sections which are collapsed by the transition are hidden as they don't play well with being srunk
        public ImageData image_data;

        protected DropdownMenuData craft_styles_menu = new DropdownMenuData(KerbalX.craft_styles);
        protected List<List<Image>> grouped_images = null;
        protected float upload_rhs_width = 420;
        protected string image_select_mode = "thumb";
        float adjusted_section_width = 0;




        //collection of Vector2 objects to track scroll positions
        public Dictionary<string, Vector2> scroll_pos = new Dictionary<string, Vector2>(){
            {"lhs", new Vector2()}, {"rhs", new Vector2()}, {"main", new Vector2()}, {"images", new Vector2()}
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
        
        //Called when the editor is loaded
        private void Start(){     
            CraftManager.log("Starting Main UI");
            CraftManager.main_ui = this;
            window_title = "";
            alt_window_style = new GUIStyle(CraftManager.skin.window);
            alt_window_style.padding.top = 8; //remove excess padding to hide titlebar

            window_pos = new Rect((Screen.width/2) - (window_width/2) + 100, 80, window_width, main_section_height);
            visible = false;
            draggable = false;
            footer = false;
            prevent_click_through = false; //disable the standard click through prevention. show and hide will add control locks which are not based on mouse pos.


            if(KerbalX.enabled){
                enable_request_handler();
                if(KerbalXAPI.logged_in() && bool.Parse(CraftManager.settings.get("ask_to_populate_new_save"))){
                    if(Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "saves", current_save_dir), "*.craft", SearchOption.AllDirectories).Length == 0){
                        populate_new_save_dialog();
                    }
                }
            }


            EditorLogic.fetch.saveBtn.onClick.AddListener(on_save_click); //settup click event on the stock save button.
            //override existing ations on stock load button and replace with call to toggle CM's UI.
            if(CraftManager.replace_editor_load_button){
                UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent(); 
                c.AddListener(on_load_click);
                EditorLogic.fetch.loadBtn.onClick = c;
            }

            //Initialize list of Save directories, used in save select menus.
            save_menu_options.items.Add(active_save_dir, "Current Save (" + active_save_dir + ")");
            foreach(string dir_name in CraftData.save_names()){
                if(dir_name != active_save_dir){
                    save_menu_options.items.Add(dir_name, dir_name);
                }
            }
            save_menu_options.items.Add(all_saves_ref, "All");

            //Initialize Tags
            Tags.load(active_save_dir);
            tags_menu_content.remote_data = new DataSource(() => { //tag menu on craft details fetches data as it opens, rather than constantly setting the data each pass.
                return Tags.names.FindAll(t => !Tags.instance.autotags_list.Contains(t));
            }); 
            tags_menu_content.special_items.Add("new_tag", "New Tag");

            type_select(EditorDriver.editorFacility.ToString(), true);  //set selected type (SPH or VAB) based on which editor we're in.

            GameEvents.OnAppFocus.Add(on_app_focus);

            if(!File.Exists(CraftDataCache.cache_path)){
                CraftManager.log("persistent cache file not found");
                CraftData.load_craft_from_files(active_save_dir==all_saves_ref ? null : active_save_dir);
            }

            if(DevTools.autostart){
                show();                
//                HelpUI.open(gameObject);
            }
            CraftManager.log("CMUI-Ready");
        }
                

        protected override void OnDestroy(){
            GameEvents.OnAppFocus.Remove(on_app_focus);
        }

        protected override void before_show(){
            
        }

        protected override void on_show(){            
            stock_craft_loaded = false;
            show_transfer_indicator = false;
            CraftManager.status_info = "";

            string cur_selected_name = null;
            string cur_selected_craft_path = null;
            if(CraftData.selected_craft != null){
                cur_selected_craft_path = CraftData.selected_craft.path;
            }else{
                cur_selected_name = EditorLogic.fetch.ship.shipName;
            }

            if(!kerbalx_mode){refresh();}

            //if a craft which matches the name and save_dir of the currently loaded craft is in the filtered results then mark it to be focused on when the UI opens
            if(cur_selected_craft_path != null){
                auto_focus_on(CraftData.filtered.Find(c => c.path == cur_selected_craft_path));
            } else if(cur_selected_name.ToLower() != "untitled space craft"){
                auto_focus_on(CraftData.filtered.Find(c => c.save_dir == current_save_dir && c.name == cur_selected_name));
            }

            if(KerbalX.enabled){
                update_remote_craft_info();
                KerbalX.check_download_queue();
            }

            grouped_images = null;
            image_data = null;

            auto_focus_field = "main_search_field";
            InputLockManager.SetControlLock(window_id.ToString());
            interface_locked = true; //will trigger unlock of interface (after slight delay) on window hide
        }

        protected override void on_hide(){
            if(exit_kerbalx_mode_after_close){
                kerbalx_mode = false;
                exit_kerbalx_mode_after_close = false;
            }
            close_dialog(); //incase any dialogs have been left open
        }

        protected override void on_error(){
            show_transfer_indicator = false;
            CraftManager.status_info = "";
        }

        public void on_app_focus(bool focus_on){
            if(focus_on){
                KerbalX.check_download_queue();
            }
        }

        protected void update_remote_craft_info(){
            if(KerbalX.enabled){
                KerbalX.fetch_existing_craft_info();
            }
        }


        //**Main GUI**//

        //Main GUI draw method (c\alled by onGUI, see DryUI in KatLib).  Broken up into smaller sections to ease digestion and help prevent heart burn.
        //The GUI is main in 5 sections, top and bottom sections span the full width, while the LHS, RHS and main sections are columns.
        protected override void WindowContent(int win_id){
            key_event_handler();
            v_section(()=>{       
                if(upload_interface_ready){
                    draw_kerbalx_header(window_width);
                }else{
                    draw_top_section(window_width);
                }

                GUILayout.Space(10);
                scroll_relative_pos = GUILayoutUtility.GetLastRect();
                section(window_width, inner_width =>{
                    if(!upload_interface_ready){
                        draw_left_hand_section(inner_width * col_widths_current[0]); //Tag list section
                        draw_main_section(inner_width * col_widths_current[1]);      //Main craft list
                    }
                    draw_right_hand_section(inner_width * col_widths_current[2]);//Craft details section
                    if(upload_interface_ready){
                        draw_kerbalx_upload_section((col_widths_default[0] + col_widths_default[1]) * inner_width);
                    }
                });
                draw_bottom_section(window_width);
            });
            handle_auto_focus_actions();
            handle_upload_interface_transition();
        }

        protected override void FooterContent(int window_id){
            GUILayout.Label("hello, this is footer");
        }


        //**GUI Sections**//

        //GUI Top section when in upload mode
        protected void draw_kerbalx_header(float section_width){
            v_section(()=>{
                GUILayout.Space(15f);
                section(section_width, 60f, ()=>{
                    fspace();
                    label("KerbalX Upload", "upload_header");
                    fspace();
                });
            });
        }

        //GUI Top Section
        protected void draw_top_section(float section_width){
            section((w) =>{
                //SPH, VAB, Subs select buttons
                section(400, (w2) =>{
                    foreach(string opt in selected_type_keys){
                        button(opt, "craft_type_sel" + (selected_types[opt] ? ".active" : ""), ()=>{type_select(opt, !selected_types[opt]);});
                    }
                    button("All", "craft_type_Sel", 30f, type_select_all);
                });
                fspace();
                if(!kerbalx_mode){
                    section("stock_craft_toggle", ()=>{                        
                        bool prev_exstcr = exclude_stock_craft;
                        exclude_stock_craft = !GUILayout.Toggle(!exclude_stock_craft, "");
                        button("include Stock Craft", "stock_craft_toggle_button", ()=>{exclude_stock_craft = !exclude_stock_craft;});
                        if(exclude_stock_craft != prev_exstcr){
                            filter_craft();
                            CraftManager.settings.set("exclude_stock_craft", exclude_stock_craft.ToString());
                        }
                    });
                }
                dropdown(StyleSheet.assets["menu"], "more_menu", more_menu, this, 30f, "button", "menu.background", "menu.item", (resp) => {
                    switch(resp){
                        case "settings" : SettingsUI.open(gameObject); break;
                        case "help" : HelpUI.open(gameObject); break;
                    }
                });
                button("X", "button.close.top", 30f, hide);

            });
            section(() =>{
                section(() =>{
                    label("Search Craft:", "h2");
                    GUI.SetNextControlName("main_search_field");
                    search_string = GUILayout.TextField(search_string, width(section_width/2));
                    if(last_search != search_string){
                        filter_craft();
                    }
                    last_search = search_string;
                    button("clear", 40f, clear_search);
                    
                });
                fspace();
                if(KerbalX.enabled){
                    button("KerbalX Craft", "button" + (kerbalx_mode ? ".down" : ""), KerbalX.load_remote_craft);
                }
                if(kerbalx_mode){
                    button("Local Craft", KerbalX.load_local);
                }else{
                    if(save_menu_width == 0){               
                        save_menu_width = GUI.skin.button.CalcSize(new GUIContent("Save: " + active_save_dir)).x;
                    }
                    save_menu_options.selected_item = active_save_dir;
                    dropdown("Save: " + (active_save_dir==all_saves_ref ? "All Saves" : active_save_dir), StyleSheet.assets["caret-down"], "save_menu", save_menu_options, this, save_menu_width, change_save);
                }
            });
        }

        //The Main craft list
        protected void draw_main_section(float section_width){
            v_section(section_width, main_section_height, false, (inner_width)=>{

                //sort menu
                if(show_headers){
                    section(inner_width, section_header_height, ()=>{
                        label("Showing " + CraftData.filtered.Count + " Craft", "h2");
                        fspace();
                        if(sort_menu_width == 0){ //calculate the initial sort menu button width. should only happen on the first pass
                            sort_menu_width = GUI.skin.button.CalcSize(new GUIContent("Sort: " + sort_options.items[sort_opt])).x;
                        }
                        //render dropdown menu of sort options
                        sort_options.selected_item = sort_opt;
                        dropdown("Sort: " + sort_options.items[sort_opt], StyleSheet.assets["caret-down"], "sort_menu", sort_options, this, sort_menu_width, select_sort_option);
                        button(StyleSheet.assets[reverse_sort ? "arrow-up" : "arrow-down"], "button.tight.right_margin", 28f, 28f, toggle_reverse_sort); 
                    });
                }else{
                    v_section(()=>{
                        GUILayout.Space(section_header_height);
                    });
                }

                //Main craft list scrolling section
                bool calculate_heights = false;
                if(CraftData.filtered.FindAll(c => c.list_height == 0).Count > 0){
                    calculate_heights = true;
                }
                scroll_pos["main"] = scroll(scroll_pos["main"], "craft.list_container", inner_width, main_section_height, craft_list_width => {                    
                    item_last_height = 0;
                    if(!craft_list_overflow){
                        craft_list_width += 20;
                    }
                    foreach(CraftData craft in CraftData.filtered){
                        draw_craft_list_item(craft, craft_list_width); //render each craft

                        //partially working approach to only drawing the craft which are in focus, it works, but mousewheel scrolling results in flicker and the You are pushing more GUIClips than you are popping error
//                        if(calculate_heights || (craft.list_position + craft.list_height > scroll_pos["main"].y && craft.list_position < scroll_pos["main"].y + main_section_height)){
//                            CraftManager.log(craft.name + " is visible");
//                            draw_craft_list_item(craft, craft_list_width); //render each craft
//                        }else{
//                            section(section_width-(30f), craft.list_height-5, "craft.list_item", (inner_width)=>{ //subtractions from width to account for margins and scrollbar
//                            });
//                        }

                        //this is used to get the top offset position of each item in the craft list and that is stored on the CraftData object
                        //facilitates maintaining focus on list items when using the up/down arrow keys to scroll.
                        if(calculate_heights && Event.current.type == EventType.Repaint){
                            craft.list_position = item_last_height;
                            craft.list_height = GUILayoutUtility.GetLastRect().height + 5; //+5 for margin
                            item_last_height += craft.list_height;
                        }
                    }
                });

                if(calculate_heights && Event.current.type == EventType.Repaint){
                    craft_list_overflow = item_last_height+10 >= main_section_height;
                }

                drag_scroll(GUILayoutUtility.GetLastRect());
  
            });            
        }

        //Individual Craft Content for main list.
        protected void draw_craft_list_item(CraftData craft, float section_width){
            section(section_width-(30f), "craft.list_item" + (craft.selected ? ".selected" : ""), (inner_width)=>{ //subtractions from width to account for margins and scrollbar
                section(inner_width-80f,()=>{
                    v_section(()=>{
                        section(()=>{
                            label(craft.name, "craft.name");
                            if(craft.name != craft.alt_name){
                                label("(" + craft.alt_name + ")", "craft.alt_name");
                            }
                            if(active_save_dir != current_save_dir){
                                fspace();
                                label("in save: " + craft.save_dir);
                            }
                        });

                        section((w) => {
                            label(craft.part_count + " parts in " + craft.stage_count + " stage" + (craft.stage_count==1 ? "" : "s"), "craft.info", width(w/5f));
                            label("cost: " + humanize(craft.cost_total), "craft.cost", width(w/7f));
                            if(selected_type_count > 1){
                                fspace();
                                label(craft.construction_type=="Subassembly" ? "Sub" : craft.construction_type, "bold.compact");
                                fspace();
                            }
                        });

                        if(craft.remote){
                            section(()=>{                                
                                label("made in KSP: " + craft.ksp_version, "craft.tags");
                                if(craft.author != KerbalXAPI.kx_username){
                                    label(" by: " + craft.author, "craft.tags");
                                }
                            });
                        }else{
                            if(craft.tag_names().Count > 0){
                                label("#" + String.Join(", #", craft.tag_names().ToArray()), "craft.tags");
                            }    
                            if(craft.locked_parts){
                                label("This craft has locked parts", "craft.locked_parts");
                            }
                            if(craft.missing_parts){
                                label("some parts are missing", "craft.missing_parts");
                            }
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
                } else if(evt.double_click){
                    CraftData.select_craft(craft);
                    if(craft.remote){
                        load_craft("dl_load");
                    }else{
                        load_craft( craft.construction_type=="Subassembly" ? "subload" : "load");                        
                    }
                } else if(evt.right_click){
                    if(!craft.remote){
                        DropdownMenuData menu = new DropdownMenuData(new Dictionary<string, string>{{"add_tag", "Add Tag"}, {"rename", "Rename"}, {"transfer", "Transfer"}});
                        if(save_menu_options.items.Count > 2){menu.items.Add("move_copy", "Move/Copy");}
                        menu.special_items.Add("delete", "Delete");
                        menu.special_items_first = false;
                        menu.offset_menu = false;
                        Rect offset = new Rect(0,0,0,0);
                        Rect container = new Rect(Input.mousePosition.x-this.window_pos.x, Screen.height-Input.mousePosition.y-this.window_pos.y, 0,0);

                        menu.set_attributes(container, offset, this, 0f, "menu.background", "menu.item.craft", (resp) => {
                            switch(resp){
                                case "add_tag"  : prepare_tag_menu(craft, container); break;
                                case "rename"   : rename_craft_dialog(craft);break;
                                case "transfer" : transfer_craft_dialog(craft);break;
                                case "move_copy": move_copy_craft_dialog(craft);break;
                                case "delete"   : delete_craft_dialog(craft);break;
                            }                            
                        });
                        gameObject.AddOrGetComponent<Dropdown>().open(menu);
                    }
                }
            });
            if(open_tag_menu){
                open_tag_menu  = false;
                gameObject.AddOrGetComponent<Dropdown>().open(inline_tag_menu);
            }
        }

        //Left Hand Section: Tags
        protected void draw_left_hand_section(float section_width){
            tag_scroll_height = main_section_height-40f;    
            if(kerbalx_mode){
                tag_scroll_height = main_section_height-170f;                
            }

            //adjustments to tag list width depending on if the edit buttons are shown and if the scroll bar is shown.
            tag_content_height = 0;
            tag_margin_offset = (edit_tags ? 82f : 30f);
            if(last_tag_content_height > tag_scroll_height){
                tag_margin_offset+=20f;
            }

            v_section(section_width, main_section_height, false, (inner_width) =>{
                if(show_headers){
                    section(inner_width, section_header_height, ()=>{
                        if(kerbalx_mode){
                            label("KerbalX Options", "h2");
                        }else{
                            label("Tags", "h2");
                            label(StyleSheet.assets["tags"], 28f, 28f);
                            fspace();
                            tag_sort_options.selected_item = tag_sort_by;
                            dropdown("Sort", StyleSheet.assets["caret-down"], "tag_sort_menu", tag_sort_options, this, 50f, change_tag_sort);
                        }
                    });
                }else{
                    v_section(()=>{
                        GUILayout.Space(section_header_height);
                    });
                }

                v_section(inner_width, "tags.list_outer", (tag_list_width) => {    
                    if(kerbalx_mode){
                        button("Your Craft", "button" + (KerbalX.loaded_craft_type=="users" ? ".down" : ""), KerbalX.load_users_craft);
                        button("Favourites", "button" + (KerbalX.loaded_craft_type=="favourites" ? ".down" : ""), KerbalX.load_favourites);
                        button("Past Downloads", "button" + (KerbalX.loaded_craft_type=="past_downloads" ? ".down" : ""), KerbalX.load_past_dowloads);
                        button("Download Queue", "button" + (KerbalX.loaded_craft_type=="download_queue" ? ".down" : ""), KerbalX.load_download_queue);

                        scroll_pos["lhs"] = scroll(scroll_pos["lhs"], "side_panel.scroll.tags", inner_width-6, tag_scroll_height, scroll_width => {
                            for(int i=0; i < KerbalX.versions.Count; i++){
                                section("tag.section", ()=>{
                                    KerbalX.v_toggle[KerbalX.versions[i]] = GUILayout.Toggle(KerbalX.v_toggle[KerbalX.versions[i]], "", "tag.toggle.light");
                                    label(KerbalX.versions[i].ToString());
                                }, (evt) => {
                                    KerbalX.v_toggle[KerbalX.versions[i]] = !KerbalX.v_toggle[KerbalX.versions[i]];
                                    filter_craft();
                                });
                            }                           
                        });
                        section(tag_list_width, 38f, ()=>{
                            fspace();
                            button("All", KerbalX.select_all_versions);
                            button("Default", KerbalX.select_default_versions);

                        });

                    }else{                        
                        scroll_pos["lhs"] = scroll(scroll_pos["lhs"], "side_panel.scroll.tags", inner_width, tag_scroll_height, scroll_width => {                            
                            for(int i=0; i < Tags.names.Count; i++){
                                string tag_name = Tags.names[i];
                                
                                tag_state = Tags.is_active(tag_name);
                                archived_tag = Tags.is_archived(tag_name);
                                tag_prev_state = tag_state;
                                
                                string tag_style = "tag.section" + (tag_state ? ".selected" : "");
                                if(archived_tag){
                                    tag_style = "tag.section.archived";
                                } 
                                Rect tag_container = section(tag_style, ()=>{                                   
                                    int craft_count = CraftData.cache.tag_craft_count_for(tag_name, archived_tag ? "" : tag_filter_mode=="AND" ? "filtered" : "raw_count");
                                    string count_string = "(" + craft_count + ")";
                                    float count_width = skin.button.CalcSize(new GUIContent(count_string)).x;
                                    
                                    tag_state = GUILayout.Toggle(tag_state, "", "tag.toggle.light");
                                    label(tag_name, "tag.toggle.label" + (Tags.instance.autotags_list.Contains(tag_name) ? ".autotag" : ""), scroll_width - count_width - tag_margin_offset);
                                    label(count_string, "tag.toggle.count", count_width);
                                }, (evt) => {                                
                                    if(evt.single_click || evt.double_click){
                                        tag_state = !tag_state;
                                    }else if(evt.right_click){
                                        DropdownMenuData menu = new DropdownMenuData(new Dictionary<string, string>{
                                            {"select", tag_state ? "DeSelect" : "Select"}, {"archive", "Exclude"}, {"edit", "Edit"}
                                        });
                                        menu.special_items.Add("delete", "Delete");
                                        menu.special_items_first = false;
                                        menu.offset_menu = false;
                                        Rect offset = new Rect(0,0,0,0);
                                        Rect container = new Rect(Input.mousePosition.x-this.window_pos.x, Screen.height-Input.mousePosition.y-this.window_pos.y, 0,0);
                                        menu.set_attributes(container, offset, this, 0f, "menu.background", "menu.item.tag_menu", (resp) => {
                                            Vector2 pos = new Vector2(evt.contianer.x + window_pos.x + evt.contianer.width, evt.contianer.y + window_pos.y + scroll_relative_pos.y - scroll_pos["lhs"].y + 80);
                                            switch(resp){
                                                case "select" : Tags.toggle_active(tag_name);break;
                                                case "archive": Tags.toggle_archive(tag_name);break;
                                                case "edit"   : edit_tag_dialog(tag_name, pos.y, pos.x);break;
                                                case "delete" : delete_tag_dialog(tag_name, pos.y, pos.x);break;
                                            }                                            
                                        });
                                        gameObject.AddOrGetComponent<Dropdown>().open(menu);
                                    }
                                });                                
                                if(Event.current.type == EventType.Repaint){                                  
                                    tag_content_height += tag_container.height + 6;
                                }                                
                                if(tag_prev_state != tag_state){
                                    Tags.toggle_active(tag_name);
                                    filter_craft();
                                }
                            }                            
                        });
                        if(show_headers){
                            section(tag_list_width, 40f, ()=>{
                                tag_filter_modes.selected_item = tag_filter_mode;
                                dropdown("Mode", StyleSheet.assets["caret-down"], "tag_filter_mode_menu", tag_filter_modes, this, 50f, change_tag_filter_mode);
                                fspace();
                                button("+", 30f, create_tag_dialog);
                            });
                        }
                    }
                });

            });
            if(tag_content_height != last_tag_content_height && tag_content_height != 0){
                last_tag_content_height = tag_content_height-2;
            }
        }

        //Right Hand Section: Craft Details
        protected void draw_right_hand_section(float section_width){
            v_section(section_width, (inner_width) =>{        
                section(inner_width, section_header_height, ()=>{                    
                    label("Craft Details", "h2");
                });

                scroll_pos["rhs"] = scroll(scroll_pos["rhs"], "side_panel.scroll", inner_width, main_section_height, scroll_width => {
                    if(CraftData.selected_craft == null){
                        GUILayout.Space(25);
                        label("Select a craft to see info about it..", "h2.centered");
                    }else{
                        GUILayout.Space(5);
                        CraftData craft = CraftData.selected_craft;                        
                        section(()=>{label(craft.name, "h2");});

                        if(expand_details){
                            float details_width = scroll_width - 50;
                            GUILayoutOption grid_width = width(details_width*0.4f);
                            section(()=>{                        
                                label("", width(details_width*0.2f));
                                label("Mass", "bold.compact", grid_width);
                                label("Cost", "bold.compact", grid_width);
                            });
                            section(()=>{                        
                                label("Dry", "bold.compact", width(details_width*0.2f));
                                label(humanize(craft.mass_dry), "small.compact", grid_width);
                                label(humanize(craft.cost_dry), "small.compact", grid_width);
                            });
                            section(()=>{
                                label("Fuel", "bold.compact", width(details_width*0.2f));
                                label(humanize(craft.mass_fuel), "small.compact", grid_width);
                                label(humanize(craft.cost_fuel), "small.compact", grid_width);
                            });
                            section(()=>{
                                label("Total", "bold.compact", width(details_width*0.2f));
                                label(humanize(craft.mass_total), "small.compact", grid_width);
                                label(humanize(craft.cost_total), "small.compact", grid_width);
                            });
                            section(()=>{
                                if(!craft.remote){
                                    fspace();              
                                    button("collapse", "hyperlink.bold.compact", ()=>{expand_details = false;});
                                }
                            });
                        }else{
                            section(()=>{
                                label("Cost", "bold.compact");
                                label(humanize(craft.cost_total), "compact");
                            });
                            section(()=> {
                                label("Mass", "bold.compact");
                                label(humanize(craft.mass_total), "compact");
                                if(!craft.remote){
                                    fspace();              
                                    button("expand", "hyperlink.bold.compact", ()=>{expand_details = true;});
                                }
                            });
                        }

                        section(()=>{
                            label("Crew Capacity", "bold.compact");
                            label(craft.crew_capacity.ToString(), "compact");
                        });
                        section(()=>{
                            DateTime date = DateTime.FromBinary(long.Parse(craft.last_updated_time));
                            label("Last Edited", "bold.compact");
                            label(date.time_ago(), "compact");
                        });

                        if(craft.remote){
                            GUILayout.Space(5);
                            if(craft.exists_locally){
                                label("Already Downloaded", "h2");
                                section(()=>{                                    
                                    button("Load", "button.inline_load", load_craft);
                                    button("update", "button.inline_update", download);
                                });
                            }else{                                
                                button("Download", "button.load", download);
                            }
                        }else{
                            GUILayout.Space(15);
                            gui_state(!upload_interface_ready, ()=>{
                                section(() => {
                                    button("transfer", transfer_craft_dialog);
                                    if(save_menu_options.items.Count > 2){
                                        button("move/copy", move_copy_craft_dialog);
                                    }
                                });
                                section(() => {
                                    button("rename", rename_craft_dialog);
                                    button("delete", "button.delete", delete_craft_dialog);
                                });

                                if(KerbalX.enabled && !craft.stock_craft){
                                    section((w)=>{
                                        if(KerbalXAPI.logged_in()){
                                            if(upload_interface_ready == false){
                                                if(craft.on_kerbalx()){
                                                    button("Update craft on KerbalX", show_update_kerbalx_craft_dialog);
                                                }else{
                                                    button("Share on KerbalX", ()=>{
                                                        show_upload_interface = true;
                                                    });
                                                }
                                            }
                                        }else{
                                            button("Login to KerbalX to share craft", "button.small", ()=>{
                                                show_must_be_logged_in(KerbalX.close_login_dialog);
                                            });
                                        }                                   
                                    });
                                }
                            });

                            GUILayout.Space(15);
                            section((w) =>{
                                label("Tags", "h2");
                                fspace();
                                scroll_relative_pos.x += (window_pos.width * (col_widths_current[0]+col_widths_current[1])) - 5f;
                                scroll_relative_pos.y += 45f - scroll_pos["rhs"].y;
                                tags_menu_content.selected_items = craft.tag_names();
                                gui_state(!upload_interface_ready, ()=>{
                                    dropdown("Add Tag", StyleSheet.assets["caret-down"], "add_tag_menu", tags_menu_content, this, scroll_relative_pos, 70f, "Button", "menu.background", "menu.item.small", resp => {
                                        respond_to_tag_menu(craft, resp);
                                    });
                                });
                            });
                            
                            foreach(string tag in craft.tag_names()){
                                section(() =>{
                                    label(tag, "compact");
                                    fspace();
                                    if(!Tags.instance.autotags_list.Contains(tag)){                                    
                                        gui_state(!upload_interface_ready, ()=>{
                                            button("x", "tag.delete_button.x", ()=>{Tags.untag_craft(craft, tag);});
                                        });
                                    }
                                });
                            }                  
                        }

                        GUILayout.Space(15);
                        section(() => {
                            label("Description", "h2");
                            fspace();
                            if(!craft.remote){
                                gui_state(!upload_interface_ready, ()=>{
                                    button((String.IsNullOrEmpty(craft.description) ? "Add" : "Edit"), edit_description_dialog);
                                });
                            }
                        });
                        section(() => {
                            if(craft.description == null){
                                craft.description = "";
                            }
                            label(craft.description.Replace("¨","\n"));
                        });
                    };
                });
            });
        }

        //KerbalX upload section (replaces main and rhs sections)
        protected void draw_kerbalx_upload_section(float section_width){
            adjusted_section_width = section_width - 26; //account for close_section 'button' 

            v_section(() =>{
                GUILayout.Space(38f);
                v_section(20, main_section_height, "close_section", (w) =>{
                    fspace();label(">>");fspace();
                }, (evt) => {
                    if(evt.single_click){
                        close_upload_interface();
                    }
                });
            });

            v_section(() =>{
                label("Upload Details", "h2");
                GUILayout.Space(2f);
                v_section(adjusted_section_width, main_section_height, "craft.list_container", (w_outter) =>{
                    if(CraftData.selected_craft != null){
                        CraftData craft = CraftData.selected_craft;                        
                        if(craft.upload_data == null){ //create new instance of upload data if it's not already been set
                            craft.upload_data = KerbalXUploadData.prepare_for(craft);
                        }

                        section(adjusted_section_width, w =>{

                            v_section(w - upload_rhs_width, inner_width =>{

                                foreach(string error in craft.upload_data.errors){
                                    label(error, "error.bold");
                                }

                                label("Step 1: Set basic Craft details", "h2");
                                section(() =>{
                                    label("Name:", "h3", 50f);
                                    craft.upload_data.craft_name = GUILayout.TextField(craft.upload_data.craft_name);
                                });

                                section(() =>{
                                    label("Type:", "h3", 50f);
                                    fspace();
                                    float type_width = GUI.skin.button.CalcSize(new GUIContent(craft.upload_data.craft_type)).x + 40;
                                    section(type_width, () =>{
                                        dropdown(craft.upload_data.craft_type, StyleSheet.assets["caret-down"], "craft_upload_style_menu", craft_styles_menu, this, type_width, menu_resp =>{
                                            craft.upload_data.craft_type = menu_resp.ToString();
                                        });
                                    });
                                });

                                section(() =>{
                                    label("#tags:", "h3", 50f);
                                    craft.upload_data.hash_tags = GUILayout.TextField(craft.upload_data.hash_tags);
                                });
                                section(() =>{
                                    fspace();
                                    label("space or comma separated (optional)", "small");
                                });                                


                                GUILayout.Space(10f);
                                label("Step 2: Add some pictures (" + craft.upload_data.images.Count + "/3)", "h2");
                                List<List<Image>> grouped_images = ImageData.images_in_groups_of(craft.upload_data.images, 3);

                                if(craft.upload_data.images.Count == 0){
                                    label("You need to add at least 1 picture");
                                    label("Select some pictures -->", "bold.compact");
                                }
                                foreach(List<Image> grp in grouped_images){
                                    section(() =>{
                                        foreach(Image image in grp){
                                            v_section(() =>{
                                                label(image.texture, 90, 72);
                                                button("remove", "image_selector.remove_item", () =>{
                                                    craft.upload_data.toggle_image(image);
                                                });
                                            });
                                        }
                                    });
                                }

                                GUILayout.Space(10f);
                                label("Step 3: Set extra info", "h2.tight");
                                label("(optional, but recommended!)", "compact");
                                section(() =>{
                                    button("edit Description", edit_description_dialog);
                                    button("edit Action Group info", edit_action_group_dialog);                                
                                });

                                GUILayout.Space(10f);
                                label("OR", "h2");
                                button("Update one of your existing craft", show_update_kerbalx_craft_dialog);
                            });

                            v_section(upload_rhs_width, inner_width =>{

                                section(() =>{
                                    label("Select Pictures to add", "h2");
                                    fspace();
                                    button("take new picture", ()=>{
                                        hide();
                                        gameObject.AddOrGetComponent<GrabImage>();
                                    });
                                    dropdown("view", StyleSheet.assets["caret-down"], "upload_image_mode_menu", upload_image_mode, this, 70f, (resp) =>{
                                        image_select_mode = resp;    
                                    });    
                                });

                                if(grouped_images == null){
                                    grouped_images = image_data.get_grouped_images(3);
                                }
                                scroll_pos["lhs"] = scroll(scroll_pos["lhs"], "side_panel.scroll.tags", inner_width, main_section_height - 40, scroll_width =>{
                                    v_section(() =>{
                                        if(image_select_mode == "thumb"){
                                            for(int i = 0; i < grouped_images.Count; i++){
                                                bool grp_visible = false;
                                                List<Image> group = grouped_images[i];
                                                section(() =>{
                                                    if((100 * i) - scroll_pos["lhs"].y <= main_section_height){                                                   
                                                        grp_visible = true;
                                                    }
                                                    foreach(Image image in group){
                                                        if(grp_visible){
                                                            if(image.loaded == false && image_data.images_being_loaded_count < 3){                                                        
                                                                image_data.images_being_loaded_count += 1;
                                                                image.loaded = true;
                                                                StartCoroutine(image.load_image());
                                                            }                                                        
                                                        }

                                                        button(image.texture, (craft.upload_data.has_image(image) ? "image_selector.item.selected" : "image_selector.item"), 125f, 90f, () =>{
                                                            craft.upload_data.toggle_image(image);
                                                        });
                                                    }
                                                });
                                            }
                                        } else if(image_select_mode == "list"){
                                            foreach(Image image in image_data.images){
                                                button(image.file.Name, (craft.upload_data.has_image(image) ? "image_selector.item.selected" : "image_selector.item"), () =>{
                                                    craft.upload_data.toggle_image(image);
                                                });
                                            }
                                        }
                                        //TODO add in 'preview' mode (single image at a time, large).
                                    });

                                });

                            });
                        });
                    }else{
//                        close_upload_interface()
                            
                    }
                });                
            });
        }

        //Botton Section: Load buttons
        protected void draw_bottom_section(float section_width){
            section("thin.section", () =>{
//                GUILayout.Space(10);
                if(KerbalX.download_queue_size > 0){
                    button(KerbalX.download_queue_size + " craft waiting to download", "download_waiting", KerbalX.load_download_queue);
                }
            });
            section("bottom.section", () =>{
                v_section(()=>{
                    section(()=>{
                        label(CraftManager.status_info);
                        if(show_transfer_indicator){
                            if(transfer_is_upload){
                                label("Uploading Craft....", "transfer_progres.text");
                            }else{
                                label("Updating Craft....", "transfer_progres.text");
                            }
                        }
                    });

                    section(()=>{
                        if(CraftManager.status_info != "" || show_transfer_indicator){
                            if((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - prog_timer > prog_interval){
                                prog_timer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                                prog_pos += 1;
                                if(prog_pos > 4){
                                    prog_pos = 0;
                                }
                            }
                            for(int i = 0; i < 5; i++){
                                if(prog_pos == i){
                                    label("", "progbox.active");
                                }else{
                                    label("", "progbox");
                                }
                            }
                        }
                    });
                });
                fspace();                   
                gui_state(CraftData.selected_craft != null && show_transfer_indicator == false, ()=>{
                    load_menu_mode = "default";

                    if(CraftData.selected_craft != null){                        
                        if(upload_interface_ready){
                            load_menu_mode = "upload";
                        }else if(CraftData.selected_craft.remote && CraftData.selected_craft.exists_locally){
                            load_menu_mode = "redownload";
                        }else if(CraftData.selected_craft.remote && !CraftData.selected_craft.exists_locally){
                            load_menu_mode = "download";
                        }else if(CraftData.selected_craft.construction_type == "Subassembly"){                        
                            load_menu_mode = "submode";
                        }
                    }

                    button(load_menu[load_menu_mode].text, "button.load", load_menu[load_menu_mode].width, ()=>{ load_craft(load_menu[load_menu_mode].action);});

                    Texture dl_menu_option_texture = StyleSheet.assets["caret-down-green"];
                    if(anchors.ContainsKey("load_menu") && anchors["load_menu"].Contains(Event.current.mousePosition)){
                        dl_menu_option_texture = StyleSheet.assets["caret-down-green-hover"];
                    }
                    if(load_menu[load_menu_mode].menu !=null){
                        dropdown(dl_menu_option_texture, "load_menu", load_menu[load_menu_mode].menu, this, 42f, "button.load", "menu.background", "menu.item", resp => {                    
                            load_craft(resp);
                        });
                    }
                });
                GUILayout.Space(8);
                if(upload_interface_ready){
                    button("Back", "button.close", 120f, close_upload_interface);
                }else{                    
                    button("Close", "button.close", 120f, this.hide);
                }

            });
        }



        //**Helpers**//

        //Collect currently active filters into a Dictionary<string, object> which is then be passed to 
        //filter_craft on CraftData (which does the actual filtering work).
        public void filter_craft(){
            if(CraftData.cache != null && !exclude_stock_craft && !stock_craft_loaded){ //load stock craft if they've not yet been loaded and option to exclude stock is switched off.
                CraftData.load_stock_craft_from_files();
            }
            Dictionary<string, object> search_criteria = new Dictionary<string, object>();
            search_criteria.Add("search", search_string);
            search_criteria.Add("type", selected_types);
            if(kerbalx_mode){
                List<Version> s_vers = KerbalX.selected_versions;
                if(s_vers.Count > 0){
                    search_criteria.Add("versions", s_vers);
                }
            } else{
                List<string> s_tags = Tags.selected_tags();
                List<string> a_tags = Tags.archived_tags();
                if(s_tags.Count > 0){
                    search_criteria.Add("tags", s_tags);
                }
                if(a_tags.Count > 0){
                    search_criteria.Add("archived_tags", a_tags);
                }
            }
            search_criteria.Add("tag_filter_mode", tag_filter_mode);
            search_criteria.Add("sort", sort_opt);
            search_criteria.Add("reverse_sort", reverse_sort);
            search_criteria.Add("exclude_stock", exclude_stock_craft);
            CraftData.filter_craft(search_criteria); //pass options to filter logic
        }

        //Handles loading the CraftData.selected_craft() into the editor. Takes a string which can either be "load", "merge" or "subload".
        //"load" performs a normal craft load (checks save state of existing & clears existing content before loading)
        //"merge" spawns a disconnected contruct of the craft along side an existing craft
        //"subload" loads like merge, but retains select on the loaded craft so it can be placed (same as stock subassembly load).
        //"download" download craft from KerbalX, save it to users craft (with query about replacing an existing one)
        //"dl_load"  download craft from KerbalX, save it to users craft (with query about replacing an existing one) and load it.
        //"dl_load_no_save" download craft from KerbalX and load it without saving it to users craft
        //"update"   download craft from KerbalX, save it to users craft (without query about replacing existing one)
        //"update_load" download craft from KerbalX, save it to users craft (without query about replacing existing one) and load it.
        protected void load_craft(){ load_craft("load");}
        protected void load_craft(string load_type, bool force = false){            
            if(CraftData.selected_craft != null){
                CraftData craft = CraftData.selected_craft;
                if(load_type == "load"){                                       
                    if(CraftData.craft_saved || force){
                        CraftData.loading_craft = true;
                        EditorLogic.LoadShipFromFile(craft.path);
                        CraftManager.main_ui.hide();
                    } else{
                        load_craft_confirm_dialog(() =>{
                            load_craft(load_type, true);
                        });
                    }
                } else if(load_type == "merge"){                    
                    ShipConstruct ship = new ShipConstruct();
                    ship.LoadShip(ConfigNode.Load(craft.path));
                    EditorLogic.fetch.SpawnConstruct(ship);
                    CraftManager.main_ui.hide();
                } else if(load_type == "subload"){
                    ShipTemplate subassembly = new ShipTemplate();
                    subassembly.LoadShip(ConfigNode.Load(craft.path));
                    EditorLogic.fetch.SpawnTemplate(subassembly);
                    CraftManager.main_ui.hide();
                } else if(load_type == "download"){
                    download(true, false);
                } else if(load_type == "update"){
                    download(true, true);
                } else if(load_type == "dl_load"){
                    download(true, false, load_craft);
                }else if(load_type == "update_load"){
                    download(true, true, load_craft);
                } else if(load_type == "dl_load_no_save"){
                    if(CraftData.craft_saved || force){
                        download(false);
                        CraftManager.main_ui.hide();
                    } else{
                        load_craft_confirm_dialog(() =>{
                            load_craft(load_type, true);
                        });
                    }
                } else if(load_type == "upload"){                    
                    craft.upload_data.post();
                }
            }
        }
            
        //makes a request on the API to fetch a craft from KerbalX (which matches the remote_id of the selected craft).
        //if the first optional args is false then the downloaded craft isn't saved to the users craft, it's just loaded into the editor
        //if the first two arguments are true then the craft will forceable replace an existing craft with the same name.
        //third argument is used to pass through the action which follows the download (either load craft or not), but as this process can be interupted by a dialog to ask the user 
        //which action to take (replace/cancel) this action needs to be passed back into this method after the dialog closes.
        protected void download(){download(true, false, null);}
        protected void download(bool save = true, bool force_overwrite = false, DialogAction action = null){
            CraftData craft = CraftData.selected_craft;
            if(craft != null && craft.remote){                
                if(save){
                    if(!File.Exists(craft.path) || force_overwrite){
                        KerbalX.download(craft.remote_id, craft_file =>{
                            craft_file.Save(craft.path);
                            craft.exists_locally = true;
                            if(action != null){action();}
                        });
                    } else{
                        download_confirm_dialog(action);
                    }
                } else{
                    KerbalX.download(craft.remote_id, craft_file =>{                                
                        string temp_path = Paths.joined(CraftManager.ksp_root, "GameData", "CraftManager", "temp.craft");
                        craft_file.Save(temp_path);
                        EditorLogic.LoadShipFromFile(temp_path);
                        File.Delete(temp_path);
                    });
                }
            }
        }

        //an attempt at enabling the user to drag the craft list up and down. it works, but can be a bit laggy (but then so is the stock list when dragged)
        //and I can't make it so the user can drag it fast and let go and have it keep sliding. 
        private void drag_scroll(Rect craft_scroll_section){
            if(Input.GetMouseButtonUp(0)){
                craft_list_drag_active = false;
            }else if(craft_scroll_section.Contains(Event.current.mousePosition) && Event.current.button == 0 && Event.current.type == EventType.MouseDrag){
                craft_list_drag_active = true;
            }

            if(craft_list_drag_active){
                craft_list_drag_force = Event.current.delta;
                scroll_pos["main"] -= craft_list_drag_force;

                //                    Event.current.Use();
            }else if(craft_list_drag_force.y >= 1 || craft_list_drag_force.y <= -1){
                scroll_pos["main"] -= craft_list_drag_force;
                if(craft_list_drag_force.y > 0){
                    craft_list_drag_force.y -= 0.2f;
                }else if(craft_list_drag_force.y < 0){
                    craft_list_drag_force.y += 0.2f;
                }
            }            
        }

        //prepare to open a tag menu, triggered from another dropdown menu, which is why this is a bit odd. It has to be opened after the first menu has been closed (and destroyed)
        //so this method is called from the frist menu which sets up the tag menu and sets a flag (open_tag_menu).  After this call the first menu closes, but then on the next pass
        //the open_tag_menu flag triggers the opening of this second menu.
        protected void prepare_tag_menu(CraftData craft, Rect container){
            inline_tag_menu= new DropdownMenuData();
            inline_tag_menu.remote_data = new DataSource(() => { 
                return Tags.names.FindAll(t => !Tags.instance.autotags_list.Contains(t));
            }); 
            inline_tag_menu.special_items.Add("new_tag", "New Tag");
            inline_tag_menu.selected_items = craft.tag_names();
            inline_tag_menu.offset_menu = false;
            inline_tag_menu.set_attributes(container, new Rect(0,0,0,0), this, 0f, "menu.background", "menu.item.small", (resp) =>{
                respond_to_tag_menu(craft, resp);
            });
            open_tag_menu = true;
        }

        protected void respond_to_tag_menu(CraftData craft, string resp){
            if(resp == "new_tag"){
                create_tag_dialog(false, craft);
            }else{
                if(craft.tag_names().Contains(resp)){
                    Tags.untag_craft(craft, resp);
                }else{                                    
                    Tags.tag_craft(craft, resp);
                }
            }
        }

        //load/reload craft from the active_save_dir and apply any active filters
        public void refresh(){            
            CraftData.load_craft_from_files(active_save_dir==all_saves_ref ? null : active_save_dir);
            filter_craft();
        }

        protected void clear_search(){
            search_string = "";
            filter_craft();
        }

        public void select_sort_option(string option){select_sort_option(option, true);}
        public void select_sort_option(string option, bool save = true){
            sort_opt = option;
            sort_menu_width = 0; //trigger recalculate of sort menu button width
            filter_craft();
            if(save){
                CraftManager.settings.set("craft_sort", sort_opt);
            }
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
            stock_craft_loaded = false;
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

        //called when clicking on the craft 'type' (VAB,SPH etc) buttons. unselects the other buttons unless ctrl is being held (enabling multiple select)
        //and ensures that at least one button is selected.
        internal void type_select(string key, bool val){
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

        internal void type_select_all(){type_select_all(false);}
        internal void type_select_all(bool force = false){
            if(!force && selected_types["SPH"] && selected_types["VAB"] && selected_types["Subassemblies"]){               
                type_select(EditorDriver.editorFacility.ToString(), true);
            } else{
                selected_types["SPH"] = true;
                selected_types["VAB"] = true;
                selected_types["Subassemblies"] = true;
                selected_type_count = 3;
            }
            filter_craft();
        }

        //if an auto_focus_craft has been set then wait a few passes and then focus on the craft in the list
        //if no auto_focus_craft is set and an auto_focus_field has been set then focus UI control onto that field
        protected void handle_auto_focus_actions(){
            if(auto_focus_craft != null && auto_focus_countdown > 0){
                if(auto_focus_countdown > 1){
                    auto_focus_countdown -= 1;
                } else{
                    jump_to_craft(CraftData.filtered.IndexOf(auto_focus_craft));
                    auto_focus_craft = null;
                }
            } else{                
                //When the UI opens set focus on the main search text field, but don't keep setting focus
                if(!String.IsNullOrEmpty(auto_focus_field)){  
                    GUI.FocusControl(auto_focus_field);
                    auto_focus_field = null;
                }
            }
        }

        protected void auto_focus_on(CraftData craft){
            auto_focus_craft = craft;
            auto_focus_countdown = 10; //delay auto_focus by x passes, to give the list time to be drawn 
            //(not happy with this but attempting to autofocus right away selects the craft, but doesn't scroll the list to it
        }

        public void close_upload_interface(){
            show_upload_interface = false;
            auto_focus_on(CraftData.selected_craft);
        }

        //Transitions the interface between regular browsing mode and KerbalX upload mode. shrinks/expands the left and main columns
        private void handle_upload_interface_transition(){
            if(show_upload_interface){
                show_headers = false;
                if(col_widths_current[0] > 0){
                    col_widths_current[0] -= 0.02f;
                }
                if(col_widths_current[1] > 0){
                    col_widths_current[1] -= 0.02f;                    
                } else{
                    upload_interface_ready = true;
                    if(image_data == null){
                        image_data = new ImageData();
                    }
                }
            } else if(!show_upload_interface){
                upload_interface_ready = false;
                if(col_widths_current[0] < col_widths_default[0]){
                    col_widths_current[0] += 0.02f;
                }
                if(col_widths_current[1] < col_widths_default[1]){
                    col_widths_current[1] += 0.02f;                    
                } else{
                    show_headers = true;
                    col_widths_current[0] = col_widths_default[0];
                    col_widths_current[1] = col_widths_default[1];
                }
            }
        }

        //**Dialogs**//
        //Various popup windows. All these dialogs use the 'show_dialog' method which is lurking below them
        //The show_dialog method takes care of all common aspects, leaving these dialog methods DRY and minimal
        //The delegate passed to show_dialog is expected to return a string (resp), This is used to pass back
        //error messages or a success status code ("200").  If "200" is returned the dialog will be closed, 
        //any other string will be shown as an error message to the user.  
        //Dialogs can also use a submit(<args>) method which draws a button that calls the given delegate, but also binds
        //the delegate to be triggered by enter key press. the delagate must return a response string based on the success 
        //of the action which is then returned by submit.

        protected void download_confirm_dialog(DialogAction action = null){
            string resp = "";
            show_dialog("Overwrite", "A Craft with this name already exists", d =>{
                section(()=>{                    
                    button("Replace Existing Craft", "button.continue_with_save", ()=>{
                        download(true, true, action);
                        resp = "200";
                    });                    
                    button("Load Craft (without saving)", "button.continue_no_save", ()=>{
                        load_craft("dl_load_no_save");
                        resp = "200";
                    });
                });
                GUILayout.Space(10);
                button("Cancel", "button.cancel_load", close_dialog);
                return resp;
            });
        }

        public void upload_complete_dialog(int remote_status_code, string response){
//            string resp = "";
            var resp_data = JSON.Parse(response);
            show_dialog(remote_status_code == 200 ? "Upload Complete" : "Upload Error", "", d =>{
                if(remote_status_code == 200){
                    string craft_url = resp_data["url"];
                    string craft_full_url = KerbalXAPI.url_to(craft_url);
                    label("Your Craft has been uploaded!", "h2");
                    button("It is now available here:\n" + craft_full_url, "hyperlink.bold", ()=>{
                        Application.OpenURL(craft_full_url);
                        close_dialog();
                    });
                    button(StyleSheet.assets["logo_large"], "centered", 664, 120, ()=> {                       
                        Application.OpenURL(craft_full_url);
                        close_dialog();
                    });
                    section(()=>{
                        fspace();
                        label("click the url or the logo to see your craft on KerbalX", "compact");                        
                    });
                }else{
                    label("There was a problem uploading your craft. Error code " + remote_status_code, "h3");
                    label(response); //TODO: make this nicer.
                }
                section(()=>{
                    fspace();
                    button("close", close_dialog);
                });
                return "";
            });
        }

        protected void show_update_kerbalx_craft_dialog(){
            string resp = "";
            CraftData craft = CraftData.selected_craft;
            if(craft.upload_data == null){ //create new instance of upload data if it's not already been set
                craft.upload_data = KerbalXUploadData.prepare_for(craft);
            }
            craft.upload_data.update_to_id = craft.matching_remote_ids.Count == 0 ? 0 : craft.matching_remote_ids[0];

            string menu_label = "";
            string kx_url = "";
            bool show_craft_select_menu = craft.matching_remote_ids.Count != 1;
            Rect d_offset = new Rect();

            Dictionary<string, string> upload_select_data_matching = new Dictionary<string,  string>();
            Dictionary<string, string> upload_select_data_all = new Dictionary<string, string>();

            foreach(int id in craft.matching_remote_ids){
                upload_select_data_matching.Add(id.ToString(), KerbalXAPI.user_craft[id]["url"].Replace(("/" + KerbalXAPI.logged_in_as()+ "/"), ""));
            }
            foreach(KeyValuePair<int, Dictionary<string, string>> c in KerbalXAPI.user_craft){
                upload_select_data_all.Add(c.Key.ToString(), c.Value["url"].Replace(("/" + KerbalXAPI.logged_in_as()+ "/"), ""));
            }

            DropdownMenuData upload_select_menu = new DropdownMenuData();
            DropdownMenuData upload_select_menu_2 = new DropdownMenuData();
            if(craft.matching_remote_ids.Count == 0){
                upload_select_menu.set_data(upload_select_data_all);
            } else{
                upload_select_menu.set_data(upload_select_data_matching);                 
            }
            upload_select_menu_2.set_data(upload_select_data_all);

            show_dialog("Update KerbalX Craft", "", d =>{
                d_offset.x = -d.window_pos.x; d_offset.y = -d.window_pos.y;

                if(craft.upload_data.update_to_id != 0){
                    menu_label = KerbalXAPI.user_craft[craft.upload_data.update_to_id]["url"].Replace(("/" + KerbalXAPI.logged_in_as() + "/"), "");
                    kx_url = KerbalXAPI.url_to(KerbalXAPI.user_craft[craft.upload_data.update_to_id]["url"]);
                }else{
                    menu_label = "Select a craft";
                }

                if(show_craft_select_menu){
                    if(craft.matching_remote_ids.Count > 1){
                        label("There are " + craft.matching_remote_ids.Count + " craft in your account with the same name as this craft");
                    }
                    label("Select which craft you want to update");
                    dropdown(menu_label, StyleSheet.assets["caret-down"], "upload_select_menu", upload_select_menu, d, d_offset, d.window_pos.width*0.8f, (sel)=>{
                        craft.upload_data.update_to_id = int.Parse(sel);
                    });
                    if(upload_select_menu.items != upload_select_data_all){
                        section(()=>{
                            fspace();
                            button("show all your craft in menu", "hyperlink", ()=>{                    
                                upload_select_menu.set_data(upload_select_data_all);
                                open_dropdown(upload_select_menu);
                            });
                        });
                    }
                }

                if(!String.IsNullOrEmpty(kx_url)){                    
                    label("This will update the KerbalX craft:");
                    section((w)=>{
                        button(kx_url, "hyperlink.update_url", 500f, 800f, ()=>{ Application.OpenURL(kx_url); });
                        if(!show_craft_select_menu){
                            dropdown("edit", StyleSheet.assets["caret-down"], "upload_select_menu_2", upload_select_menu_2, d, d_offset, d.window_pos.width*0.8f, (sel)=>{
                                craft.upload_data.update_to_id = int.Parse(sel);
                            });
                        }                        
                    });
                    section(()=>{
                        fspace();
                        label("make sure this is the craft you want to update!", "small.compact");
                    });
                }

                GUILayout.Space(10f);
                gui_state(craft.upload_data.update_to_id > 0, ()=>{
                    button("Confirm Update", "button.large", ()=>{
                        craft.upload_data.put();
                        close_dialog();
                    });
                });
                GUILayout.Space(4f);
                section(()=>{
                    button("Upload as a new Craft", "button.large", ()=> {
                        show_upload_interface = true;
                        close_dialog();
                    });
                    button("Cancel", "button.large", close_dialog);                    
                });
                return resp;
            });
        }

        //Show user option to save current craft or carry on loading
        protected void load_craft_confirm_dialog(DialogAction action){
            string resp = "";
            show_dialog("Confirm Load", "The Current Craft has unsaved changes", d =>{
                section((w)=>{                    
                    button("Save Current Craft first", "button.continue_with_save", ()=>{
                        string path = ShipConstruction.GetSavePath(EditorLogic.fetch.ship.shipName);
                        EditorLogic.fetch.ship.SaveShip().Save(path);
                        action(); resp = "200";
                    });                    
                    button("Continue Without Saving", "button.continue_no_save", ()=>{
                        action(); resp = "200";
                    });
                });
                GUILayout.Space(10);
                button("Cancel", "button.cancel_load", close_dialog);
                return resp;
            });
        }

        protected void edit_description_dialog(){
            if(CraftData.selected_craft.description == null){
                CraftData.selected_craft.description = "";
            }
            string resp = "";
            float area_height = 0;
            string original_desc = new string(CraftData.selected_craft.description.ToCharArray());

            show_dialog("Edit Description", "Edit Description", d =>{
                GUI.SetNextControlName("dialog_focus_field");
                area_height = skin.textArea.CalcHeight(new GUIContent(CraftData.selected_craft.description), d.window_pos.width)+10;
                if(area_height < 150f){area_height=150f;}
                CraftData.selected_craft.description = GUILayout.TextArea(CraftData.selected_craft.description.Replace("¨", "\n"), height(area_height));
                section(()=>{
                    fspace();
                    button("Cancel", ()=>{
                        CraftData.selected_craft.description = original_desc;
                        close_dialog();
                    });
                    resp = submit("Save", CraftData.selected_craft.save_description);
                });
                return resp;
            });
        }

        protected void edit_action_group_dialog(){
            string resp = "";
//            float area_height = 0;
            show_dialog("Edit Action Group info", "Edit Action Group info", 700f, d =>{

                CraftData craft = CraftData.selected_craft;
                if(craft !=null){                    
                    Dictionary<string, string> action_groups = craft.upload_data.action_groups;
                    List<string> keys = new List<string>(action_groups.Keys);
                    float label_width = 45f;
                    GUI.SetNextControlName("dialog_focus_field");
                    section(()=>{                        
                        v_section(350f, (w)=>{
                            for(int i=0; i< keys.Count; i++){
                                section(()=>{
                                    string key = keys[i];
                                    if(key.Length <= 1){
                                        label(key, width(label_width));
                                        action_groups[key] = GUILayout.TextField(action_groups[key]);
                                    }
                                });
                            }
                        });
                        v_section(350f, (w)=>{
                            for(int i=0; i< craft.upload_data.action_groups.Count; i++){
                                section(()=>{
                                    string key = keys[i];
                                    if(key.Length > 1){
                                        label(key, width(label_width));
                                        action_groups[key] = GUILayout.TextField(action_groups[key]);
                                    }
                                });
                            }
                        });
                    });
                }else{
                    fspace();
                    label("No craft is selected", "alert.h2");
                    fspace();
                }
                section((w)=>{
                    fspace();
                    resp = submit("OK", "button.large", ()=>{ return "200";});
                });
                return resp;
            });
        }

        protected void rename_craft_dialog(){ rename_craft_dialog(CraftData.selected_craft); }
        protected void rename_craft_dialog(CraftData craft){            
            craft.new_name = craft.name;
            string resp = "";
            show_dialog("Rename Craft", "rename: " + craft.name, d =>{
                GUI.SetNextControlName("dialog_focus_field");
                craft.new_name = GUILayout.TextField(craft.new_name);
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Rename", craft.rename);
                });
                return resp;
            });
        }

        protected void delete_craft_dialog(){ delete_craft_dialog(CraftData.selected_craft); }
        protected void delete_craft_dialog(CraftData craft){
            string resp = "";
            show_dialog("Delete Craft?", "Delete " + craft.name + "?\nAre you sure you want to do this?", d =>{                
                section(()=>{
                    fspace();
                    button("Cancel", close_dialog);
                    resp = submit("Delete", "button.delete", craft.delete);
                });
                return resp;
            });
        }

        protected void transfer_craft_dialog(){ transfer_craft_dialog(CraftData.selected_craft); }
        protected void transfer_craft_dialog(CraftData craft){
            string resp = "";
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

        protected void move_copy_craft_dialog(){ move_copy_craft_dialog(CraftData.selected_craft); }
        protected void move_copy_craft_dialog(CraftData craft){            
            string resp = "";
            string selected_save = "";

            DropdownMenuData move_copy_save_menu = new DropdownMenuData(new Dictionary<string, string>(save_menu_options.items));
            List<string> keys = new List<string>(move_copy_save_menu.items.Keys);
            List<string> rem_keys = keys.FindAll(k => k.Contains(craft.save_dir) );
            foreach(string key in rem_keys){                
                move_copy_save_menu.items.Remove(key);
            }
            move_copy_save_menu.items.Remove(all_saves_ref);
            Rect d_offset = new Rect();

            show_dialog("Move/Copy Craft", "", d =>{
                section(500f, (inner_width)=>{
                    label("Move or Copy this craft to another save:", "h2");
                    d_offset.x = -d.window_pos.x; d_offset.y = -d.window_pos.y;
                    dropdown("Select Save", StyleSheet.assets["caret-down"], "copy_transfer_save_menu", move_copy_save_menu, d, d_offset, inner_width/2, "button.large", "menu.background", "menu.item", (selected_save_name) => {
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

        //Call Create Tag Dialog (using tag_dialog_form)
        protected void create_tag_dialog(){create_tag_dialog(true, null, true);}
        protected void create_tag_dialog(bool show_rule_opts = true, CraftData auto_add_craft = null, bool autopos = false){
            float top = this.window_pos.y + (this.window_pos.height*0.4f);
            float left = this.window_pos.x + (this.window_pos.width/2) - (200f);
            if(autopos){
                top = this.window_pos.y + scroll_relative_pos.y + main_section_height - 135f;
                left = this.window_pos.x + scroll_relative_pos.x + (col_widths_current[0] * window_width) - 400f;
            }

            string save_dir_for_tag = active_save_dir;
            if(save_dir_for_tag == all_saves_ref){
                save_dir_for_tag = current_save_dir;
            }
            if(auto_add_craft != null){
                save_dir_for_tag = auto_add_craft.save_dir;
            }
            tag_dialog_form("Create", show_rule_opts, "", save_dir_for_tag, false, "", "", "", top, left, auto_add_craft);
        }

        //Edit Tag Dialog (using tag_dialog_form)
        protected void edit_tag_dialog(string tag_name, float top, float left){
            Tag tag = Tags.find(tag_name, active_save_dir);
            tag_dialog_form("Edit", true, tag.name, active_save_dir, tag.rule_based, tag.rule_attribute, tag.rule_comparitor, tag.rule_value, top, left, null);
        }

        //The main dialog used for both editing and creating tags
        protected void tag_dialog_form(string mode, bool show_rule_opts, string tag_name, string save_dir, bool rule_based, string rule_attr, string rule_comparator, string rule_value, float top, float left, CraftData auto_add_craft = null){
            string initial_name = tag_name;
            string resp = "";
            string header = (mode == "Create" ? "Create Tag" : ("Edit Tag: " + tag_name));

            Rect d_offset = new Rect();
            List<Tag> tags = Tags.find_all(tag_name, save_dir);                
            DropdownMenuData rule_attrs = new DropdownMenuData(Tags.instance.rule_attributes);
            DropdownMenuData rule_comparators = new DropdownMenuData();
            DropdownMenuData bool_opts = new DropdownMenuData(new List<string>{"True", "False"});
            string prev_rule_attr = rule_attr;
            string sel_attr_type = "";
            bool first_pass = true;

            show_dialog("Tag Form", header, top, left, 400f, true, d =>{
                d_offset.x = -d.window_pos.x; d_offset.y = -d.window_pos.y;

                if(tags.Count > 1){
                    label("You are viewing craft from all saves, this will edit " + tags.Count + " tags called " + initial_name + " in each of your saves.", "alert.h3");
                }
                GUI.SetNextControlName("dialog_focus_field");
                tag_name = GUILayout.TextField(tag_name);

                if(show_rule_opts){
                    rule_based = GUILayout.Toggle(rule_based, "Use Auto Tag rule", "Button");
                }

                if(rule_based){
                    section(()=>{
                        prev_rule_attr = rule_attr;    
                        section(d.window_pos.width*0.35f, ()=>{
                            dropdown((String.IsNullOrEmpty(rule_attr) ? "Select an Attribute" : rule_attrs.items[rule_attr]), StyleSheet.assets["caret-down"], 
                                "tag_rule_attr_menu", rule_attrs, d, d_offset, d.window_pos.width*0.35f, 
                                (sel_attr)=>{
                                    rule_attr = sel_attr;
                                }
                            );
                        });

                        if(prev_rule_attr != rule_attr || first_pass){                        
                            if(!String.IsNullOrEmpty(rule_attr)){
                                sel_attr_type = typeof(CraftData).GetProperty(rule_attr).PropertyType.ToString();
                                if(sel_attr_type == "System.String"){
                                    rule_comparators.set_data(new Dictionary<string, string>(Tags.instance.rule_comparitors_string));
                                }else if(sel_attr_type == "System.Int32" || sel_attr_type == "System.Single"){
                                    rule_comparators.set_data(new Dictionary<string, string>(Tags.instance.rule_comparitors_numeric));
                                }
                            }
                        }

                        if(!rule_comparators.items.ContainsKey(rule_comparator)){                        
                            rule_comparator = "equal_to";
                        }
                        if(sel_attr_type == "System.Int32" || sel_attr_type == "System.Single"){
                            rule_value = System.Text.RegularExpressions.Regex.Replace(rule_value, "[^0-9]", "");                        
                        }

                        if(!String.IsNullOrEmpty(sel_attr_type)){                        
                            if(sel_attr_type == "System.Boolean"){
                                if(!bool_opts.items.ContainsKey(rule_value)){
                                    rule_value = "True";
                                }
                                rule_comparator = "equal_to";
                                label("==", "Button", width(d.window_pos.width*0.25f));
                                dropdown(rule_value, StyleSheet.assets["caret-down"], "tag_rule_bool_opt_menu", bool_opts, d, d_offset, d.window_pos.width*0.25f, (bool_val)=>{
                                    rule_value = bool_val;
                                });
                            }else{
                                section(d.window_pos.width*0.25f, ()=>{
                                    dropdown(rule_comparators.items[rule_comparator], StyleSheet.assets["caret-down"], "tag_rule_comp_menu", rule_comparators, d, d_offset, d.window_pos.width*0.25f, (sel_comparator)=>{
                                        rule_comparator = sel_comparator;
                                    });
                                });
                                rule_value = GUILayout.TextField(rule_value);
                            }
                        }
                    });
                }

                section((w)=>{
                    fspace();
                    button("Cancel", close_dialog);
                    if(mode == "Edit"){
                        resp = submit("Update", ()=>{
                            return Tags.update(initial_name, tag_name, save_dir, rule_based, rule_attr, rule_comparator, rule_value);
                        });                       
                    }else{
                        resp = submit("Create Tag", ()=>{
                            return Tags.create(tag_name, save_dir, rule_based, rule_attr, rule_comparator, rule_value, auto_add_craft);
                        });                       
                    }
                });
                return resp;
            });
        }

        protected void delete_tag_dialog(string tag_name, float top, float left){            
            string resp = "";
            int craft_count = CraftData.cache.tag_craft_count_for(tag_name);
            show_dialog("Delete Tag", "Are you sure you want to delete this tag?", top, left, 400f, true, d =>{

                if(active_save_dir == all_saves_ref){
                    label("You are viewing craft from all saves, this tag will be deleted in each of your saves.", "alert.h3");
                }
                if(craft_count > 0){
                    label("This tag is used by " + craft_count + " craft.");
                    label("deleting tags will not delete any craft", "compact");
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

        protected void populate_new_save_dialog(){
            KerbalX.get_craft_ids_by_version((craft_by_version, versions) =>{
                
                string v1 = versions[1].ToString();
                string v2 = versions[2].ToString();
                string resp = "";
                Version ksp_version = new Version(Versioning.GetVersionString());
                KerbalX.log_scroll = new Vector2();
                KerbalX.bulk_download_log = "";
                
                show_dialog("Import Craft From KerbalX", "", d =>{
                    label("You don't have any craft in this save yet.", "h2");
                    label("Do you want to fetch your craft from KerbalX?", "h2");

                    if(versions[0] == ksp_version){
                        button("download " + craft_by_version[versions[0].ToString()].Count + " craft built in KSP " + ksp_version, ()=>{
                            KerbalX.bulk_download(craft_by_version[versions[0].ToString()], current_save_dir, ()=>{});
                        });
                    }else{
                        label("You don't have any craft made in this version of KSP");
                    }

                    if(v1 != null || v2 != null){
                        label("get craft from previous versions:");
                        section(()=>{
                            if(v1 != null && craft_by_version[v1] != null ){
                                button("download " + craft_by_version[v1].Count + " craft built in KSP " + v1, ()=>{
                                    KerbalX.bulk_download(craft_by_version[v1], current_save_dir, ()=>{});
                                });
                            }
                            if(v2 != null && craft_by_version[v2] != null ){
                                button("download " + craft_by_version[v2].Count + " craft built in KSP " + v2, ()=>{
                                    KerbalX.bulk_download(craft_by_version[v2], current_save_dir, ()=>{});
                                });
                            }
                        });
                    }

                    if(!String.IsNullOrEmpty(KerbalX.bulk_download_log)){
                        KerbalX.log_scroll = scroll(KerbalX.log_scroll, d.window_pos.width, 80f, (w)=>{
                            label(KerbalX.bulk_download_log);
                        });
                    }

                    button("OR cherry pick the ones you want", ()=>{
                        close_dialog();
                        exit_kerbalx_mode_after_close = true;
                        CraftManager.main_ui.show();
                        KerbalX.load_remote_craft();
                    });
                    button("Close", close_dialog);

                    return resp;
                });
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
        protected DryDialog show_dialog(string title, string heading, float dialog_width, InnerDialogContent content){
            return show_dialog(title, heading, Screen.height / 3, this.window_pos.x + (this.window_pos.width / 2) - (dialog_width / 2), dialog_width, true, content);
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
                    auto_focus_field = "dialog_focus_field";
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


        //listen to key press actions
        protected void key_event_handler(){
            if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)){
                ctrl_key_down = true;
            } else{
                ctrl_key_down = false;
            }
            Event e = Event.current;

            if(e.type == EventType.keyDown){
                //'esc' - close interface
                if(e.type == EventType.keyDown && e.keyCode == KeyCode.Escape) {
                    e.Use();
                    this.hide();
                //'ctrl+f' - focus on main search field
                }else if(GUI.GetNameOfFocusedControl() != "main_search_field" && ctrl_key_down && e.keyCode == KeyCode.F){
                    GUI.FocusControl("main_search_field");
                    e.Use();
                //'ctrl+t' - create new tag
                }else if(GUI.GetNameOfFocusedControl() != "main_search_field" && ctrl_key_down && e.keyCode == KeyCode.T){
                    create_tag_dialog();
                    e.Use();
                //'up arrow' move up in craft list
                } else if(e.keyCode == KeyCode.UpArrow && !upload_interface_ready){
                    jump_to_craft(CraftData.filtered.IndexOf(CraftData.selected_craft) - 1);
                    e.Use();
                //'down arrow' move down in craft list  
                } else if(e.keyCode == KeyCode.DownArrow && !upload_interface_ready){
                    jump_to_craft(CraftData.filtered.IndexOf(CraftData.selected_craft) + 1);
                    e.Use();
                //'enter key' - load selected craft (if focus is not on search field)
                }else if(GUI.GetNameOfFocusedControl() != "main_search_field" && CraftData.selected_craft != null){
                    if(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter){                       
                        load_craft(CraftData.selected_craft.construction_type == "Subassembly" ? "subload" : "load");
                    }                
                //'tab' - move focus from search field to craft list.
                } else if(GUI.GetNameOfFocusedControl() == "main_search_field" && e.keyCode == KeyCode.Tab){
                    jump_to_craft(0);
                    e.Use();            
                }
            }
        }

        //select craft in crafts list by it's ID and adjust the scroll to focus on the selected craft
        protected void jump_to_craft(int index){   
            GUIUtility.keyboardControl = 0;
            if(index < 0){
                index = 0;
            } else if(index > CraftData.filtered.Count-1){
                index = CraftData.filtered.Count - 1;
            }
            CraftData craft = CraftData.filtered[index];
            if(craft != null){
                CraftData.select_craft(craft);
                if(craft.list_position < scroll_pos["main"].y){
                    scroll_pos["main"] = new Vector2(scroll_pos["main"].x, craft.list_position);
                } else if(craft.list_position + craft.list_height > scroll_pos["main"].y + main_section_height){
                    scroll_pos["main"] = new Vector2(scroll_pos["main"].x, craft.list_position - main_section_height + craft.list_height+5);
                }
            }
        }

    }
}
