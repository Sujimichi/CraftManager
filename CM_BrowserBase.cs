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
    //CMBrowserBase contains all the variables for the main CMBrowser class and the CMBrowserDialogs class (which is also inherited by CMBrowser
    //It also contains all the 'worker' methods which are called by the view elements of CMBrowser and CMBrowserDialogs.
    public class CMBrowserBase : CMUI
    {
        
        internal const string all_saves_ref = "<all_saves>";

        protected float main_section_height = Screen.height - 420f;
        protected float window_width  = 1000f;
        protected float[] col_widths_default = new float[]{0.2f,0.55f,0.25f};
        protected float[] col_widths_current = new float[]{0.2f,0.55f,0.25f};


        internal string current_save_dir = HighLogic.SaveFolder; //The dir of save which is currently being played
        internal string active_save_dir = HighLogic.SaveFolder;   //The dir of save which has been selected in UI

        internal delegate void DialogAction();
        protected DropdownMenuData inline_tag_menu;
        protected DropdownMenuData save_menu_options  = new DropdownMenuData();
        protected DropdownMenuData tags_menu_content  = new DropdownMenuData();
        protected DropdownMenuData toggle_tags_menu   = new DropdownMenuData();
        protected DropdownMenuData tag_sort_options   = new DropdownMenuData(new Dictionary<string, string> { {"name", "Name"}, {"craft_count", "Craft Count"} });
        protected DropdownMenuData tag_filter_modes   = new DropdownMenuData(new List<string> { "AND", "OR" });
        protected DropdownMenuData upload_image_mode  = new DropdownMenuData(new List<string> { "thumb", "list" });
        protected DropdownMenuData more_menu          = new DropdownMenuData(new Dictionary<string, string> { {"help", "Help"}, {"settings", "Settings"}, {"compact_mode", "Compact Mode"} });
        protected DropdownMenuData version_menu       = new DropdownMenuData();
        protected DropdownMenuData kerbalx_categories= new DropdownMenuData(new Dictionary<string, string> {
            {"users", "Your Craft"}, {"favourites", "Favourites"}, {"past_downloads", "Past Downloads"}, {"download_queue", "Download Queue"}
        });
        protected DropdownMenuData sort_options       = new DropdownMenuData(new Dictionary<string, string>{
            {"name", "Name"}, {"cost", "Cost"}, {"crew_capacity", "Crew Capacity"}, {"mass", "Mass"}, {"part_count", "Part Count"}, {"stage_count", "Stages"}, {"date_created", "Created"}, {"date_updated", "Updated"}
        });

        protected struct MenuOptions{
            public string text;
            public string action;
            public float width;
            public DropdownMenuData menu;
        }
        protected Dictionary<string, MenuOptions> load_menu = new Dictionary<string, MenuOptions>{
            {"default", new MenuOptions{text = "Load", action = "load", width = 120f, menu = new DropdownMenuData(new Dictionary<string, string> { { "merge", "Merge" }, { "subload", "Load as Subassembly" } })}},
            {"submode", new MenuOptions{text = "Load Subassembly", action = "subload", width = 300f, menu = new DropdownMenuData(new Dictionary<string, string> { { "merge", "Merge" }, { "load", "Load as Craft" } })}},
            {"download",new MenuOptions{text = "Download", action = "download", width = 200f, menu = new DropdownMenuData(new Dictionary<string, string> { {"dl_load", "Download & Load"}, { "dl_load_no_save", "Load without saving" } })}},
            {"redownload",new MenuOptions{text = "Update", action = "update", width = 200f, menu = new DropdownMenuData(new Dictionary<string, string> { {"update_load", "Update & Load"}, { "dl_load_no_save", "Load Remote version without saving" }, { "load", "Load local version" } })}},
            {"upload",  new MenuOptions{text = "Upload", action = "upload", width = 150f, menu = null}}
        };
        protected string load_menu_mode = "default";


        internal Dictionary<string, bool> selected_types = new Dictionary<string, bool>() { { "SPH", false }, { "VAB", false }, { "Subassemblies", false } };
        internal int selected_type_count = 1;
        internal List<string> selected_type_keys = new List<string>(){"SPH", "VAB", "Subassemblies"};


        protected string sort_opt = CraftManager.settings.get("craft_sort");
        protected bool reverse_sort = bool.Parse(CraftManager.settings.get("craft_sort_reverse"));
        protected string tag_filter_mode = CraftManager.settings.get("tag_filter_mode"); //"AND"; // OR
        internal string tag_sort_by = CraftManager.settings.get("sort_tags_by");
        internal bool exclude_stock_craft = bool.Parse(CraftManager.settings.get("exclude_stock_craft"));

        protected string search_string = "";
        protected string last_search = "";

        protected float save_menu_width = 0; //autoset based on content width
        protected float sort_menu_width = 0;

        protected string auto_focus_field = null;
        protected CraftData auto_focus_craft = null;
        protected int auto_focus_countdown = 0;
        internal bool stock_craft_loaded = false;
        protected bool edit_tags = false;
        protected bool expand_details = false;
        protected bool tag_prev_state = false;
        protected bool tag_state = false;
        protected bool ctrl_key_down = false;
        protected bool archived_tag = false;
        protected bool open_tag_menu = false;
        protected bool compact_mode = false;
        protected float tag_content_height = 0;
        protected float last_tag_content_height = 0;
        protected float tag_margin_offset = 0;
        protected float tag_scroll_height = 0;
        protected float section_header_height = 38f;
        protected float item_last_height = 0;

        internal bool show_transfer_indicator = false;
        internal bool transfer_is_upload = true;

        protected int prog_pos = 0;
        protected long prog_timer = 0;
        protected int prog_interval = 200;

        protected bool craft_list_overflow = false;
        protected bool craft_list_drag_active = false;
        protected Vector2 craft_list_drag_force = new Vector2();


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
        protected float adjusted_section_width = 0;


        //collection of Vector2 objects to track scroll positions
        public Dictionary<string, Vector2> scroll_pos = new Dictionary<string, Vector2>(){
            {"lhs", new Vector2()}, {"rhs", new Vector2()}, {"main", new Vector2()}, {"images", new Vector2()}
        };
        protected Rect scroll_relative_pos = new Rect(0, 0, 0, 0); //used to track the position of scroll sections. needed to render dropdown menus inside scroll section.




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
                        CraftManager.main_ui.load_craft_confirm_dialog(() =>{
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
                        CraftManager.main_ui.load_craft_confirm_dialog(() =>{
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
        internal void download(){download(true, false, null);}
        internal void download(bool save = true, bool force_overwrite = false, DialogAction action = null){
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
                        CraftManager.main_ui.download_confirm_dialog(action);
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
        protected void drag_scroll(Rect craft_scroll_section){
            if(Input.GetMouseButtonUp(0)){
                craft_list_drag_active = false;
            }else if(craft_scroll_section.Contains(Event.current.mousePosition) && Event.current.button == 0 && Event.current.type == EventType.MouseDrag){
                craft_list_drag_active = true;
            }
            if(craft_list_drag_active){
                craft_list_drag_force = Event.current.delta;
                scroll_pos["main"] -= craft_list_drag_force;
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
                CraftManager.main_ui.create_tag_dialog(false, craft);
            }else{
                if(craft.tag_names().Contains(resp)){
                    Tags.untag_craft(craft, resp);
                }else{                                    
                    Tags.tag_craft(craft, resp);
                }
            }
        }

        protected void toggle_compact_mode(){
            compact_mode = !compact_mode;
            if(compact_mode){
                window_width = 500f;
            } else{
                window_width = 1000f;
            }
            window_pos = get_window_position();
            more_menu.items["compact_mode"] = compact_mode ? "Full View" : "Compact Mode";
        }

        protected Rect get_window_position(){
            return new Rect((Screen.width/2) - (window_width/2) + 100, 80, window_width, main_section_height);
        }

        //load/reload craft from the active_save_dir and apply any active filters
        public void refresh(){            
            CraftData.load_craft_from_files(active_save_dir==all_saves_ref ? null : active_save_dir);
            filter_craft();
        }

        protected void update_remote_craft_info(){
            if(KerbalX.enabled){
                KerbalX.fetch_existing_craft_info();
            }
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

        protected void open_upload_interface(){
            show_upload_interface = true;
            close_dialog();
        }

        public void close_upload_interface(){
            show_upload_interface = false;
            auto_focus_on(CraftData.selected_craft);
        }

        //Transitions the interface between regular browsing mode and KerbalX upload mode. shrinks/expands the left and main columns
        protected void handle_upload_interface_transition(){
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
                    CraftManager.main_ui.create_tag_dialog();
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
    }
}

