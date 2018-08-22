using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using ExtensionMethods;
using KatLib;

namespace CraftManager
{

    //Inheritance Chain:
    //  |                     CraftManager                      |       KatLib        |     Unity    |
    //  CMBrowser <- CMBrowserDialogs < - CMBrowserBase <- CMUI <- DryUI <- DryUIBase <- MonoBehaviour

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CMBrowser : CMBrowserDialogs
    {

        //**MonoBehaviour Calls**//

        //register events used to keep track of the save state of the craft.
        //These events are unregistered by onGameSceneLoadRequested Event.
        private void Awake(){
            GameEvents.onEditorShipModified.Add(on_ship_modified);
            GameEvents.onEditorRestart.Add(on_editor_restart);
        }

        private void Start(){     
            CraftManager.log("Starting Main UI");
            CraftManager.main_ui = this;

            alt_window_style = new GUIStyle(HighLogic.Skin.window);
            alt_window_style.padding.top = 8; //remove excess padding to hide titlebar

            window_title = "";
//            window_pos = get_window_position();
            set_window_position();
            visible = false;
            draggable = false;
            footer = false;
            prevent_click_through = false; //disable the standard click through prevention. show and hide will add control locks which are not based on mouse pos.

            height_scale = float.Parse(CraftManager.settings.get("main_ui_height_scale"));
            CraftManager.log("height_scale:  " + height_scale);

            toggle_compact_mode(bool.Parse(CraftManager.settings.get("compact_mode")), false);

            //load the cache (& if cache has not been created, ie after initial install, then generate cache data for craft)     
            StartCoroutine(CraftDataCache.load_cache());

            if(KerbalX.enabled){
                enable_request_handler();
                if(KerbalXAPI.logged_in() && bool.Parse(CraftManager.settings.get("ask_to_populate_new_save"))){
                    if(Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "saves", current_save_dir), "*.craft", SearchOption.AllDirectories).Length == 0){
                        populate_new_save_dialog();
                    }
                }
                KerbalX.fetch_existing_craft_info();
                KerbalX.check_download_queue();
            }


            //Settup click event on the stock save button.
            EditorLogic.fetch.saveBtn.onClick.AddListener(on_save_click); 

            //override existing ations on stock load button and replace with call to toggle CM's UI.
            if(CraftManager.replace_editor_load_button){
                UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent(); 
                c.AddListener(on_load_click);
                EditorLogic.fetch.loadBtn.onClick = c;
            }

            //Add on focus event to trigger checking download queue 
            GameEvents.OnAppFocus.Add(on_app_focus);


            //Initialize Tags
            Tags.load(active_save_dir);

            //Initialize menus which use dynamically loaded content
            save_select_menu.remote_data = save_menu_data;
            tags_menu_content.remote_data = tags_menu_data;
            tags_menu_content.special_items.Add("new_tag", "New Tag");
            toggle_tags_menu.remote_data = toggle_tags_menu_data; //used in compact mode to select which tags are active
            toggle_tags_menu.special_items.Add("new_tag", "New Tag");
            version_menu.remote_data = versions_menu_data; //drop down used in compact mode to select which remote craft versions are shown

            //Set selected type (SPH or VAB) based on which editor we're in.
            type_select(EditorDriver.editorFacility.ToString(), true);  


            saves_count = CraftData.save_names.Count;
            CraftData.save_state = 0;

            CraftManager.log("CraftManagerUI-Ready");
        }

        protected override void OnDestroy(){
            GameEvents.OnAppFocus.Remove(on_app_focus);
        }



        //**Event Hooks**//

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

            if(!kerbalx_mode){
                refresh();
            }
            
            //autoselect loaded craft in list, or re-select previously selected craft
            if(cur_selected_craft_path != null){
                auto_focus_on(CraftData.filtered.Find(c => c.path == cur_selected_craft_path));
            } else if(cur_selected_name.ToLower() != "untitled space craft"){
                List<CraftData> matching_craft = CraftData.filtered.FindAll(c => c.name == cur_selected_name && c.file_name != autosave_craft_name);
                if(matching_craft.Count == 1){
                    auto_focus_on(matching_craft[0]);
                }
            }

            grouped_images = null;
            image_data = null;
            
            auto_focus_field = "main_search_field";
            InputLockManager.SetControlLock(window_id.ToString());
            interface_locked = true; //will trigger unlock of interface (after slight delay) on window hide
        }


        protected override void on_hide(){
            if(exit_kerbalx_mode_after_close){
                KerbalX.load_local();
                exit_kerbalx_mode_after_close = false;
            }
            close_dialog(); //incase any dialogs have been left open
        }

        protected override void on_error(){
            show_transfer_indicator = false;
            CraftManager.status_info = "";
        }

        private void on_app_focus(bool focus_on){
            if(focus_on){
                KerbalX.fetch_existing_craft_info();
                KerbalX.check_download_queue();
            }
        }

        //called by GameEvents.onEditorRestart which is triggered when loading a craft and creating a new one.
        //sets the save_state to 0. This is done inside a Coroutine call with a 500ms delay, the reason for this is
        //that onEditorShipModified is called once directly after loading by the stock game and some mods call it multiple times. The delay lets that happen before setting
        //the save_state to 0.
        public void on_editor_restart(){
            StartCoroutine(clear_save_state());
        }
        public IEnumerator clear_save_state(){
            yield return true;
            Thread.Sleep(int.Parse(CraftManager.settings.get("clear_save_state_delay")));
            CraftData.save_state = 0;
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
                        if(!compact_mode){
                            draw_left_hand_section(inner_width * col_widths_current[0]); //Tag list section
                        }
                        float main_section_width = inner_width * col_widths_current[1];
                        if(compact_mode){
                            main_section_width = inner_width;
                        }
                        draw_main_section(main_section_width);      //Main craft list
                    }
                    if(!compact_mode){
                        draw_right_hand_section(inner_width * col_widths_current[2]);//Craft details section
                    }
                    if(upload_interface_ready){
                        draw_kerbalx_upload_section((col_widths_default[0] + col_widths_default[1]) * inner_width);
                    }
                });
                draw_bottom_section(window_width);
            });
            handle_auto_focus_actions();
            handle_upload_interface_transition();
            if(open_tag_menu){
                open_tag_menu  = false;
                gameObject.AddOrGetComponent<Dropdown>().open(inline_tag_menu);
            }
        }

        protected override void FooterContent(int window_id){
            GUILayout.Label("hello, this is footer");
        }


        //**GUI Sections**// - complicated and poorly commented, approach with caution!

        //GUI Top section when in upload mode
        protected void draw_kerbalx_header(float section_width){
            v_section((w)=>{                
                section(section_width, ()=>{
                    fspace();
                    label(StyleSheet.assets["logo_large"], "upload_header.logo", 415f, 75f);
                    label("Upload", "upload_header");
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
                if(!compact_mode){
                    stock_craft_toggle_checkbox();
                }
                dropdown(StyleSheet.assets["menu"], "more_menu", more_menu, this, 30f, "button", "menu.background", "menu.item", (resp) => {
                    switch(resp){
                        case "settings" : SettingsUI.open(gameObject); break;
                        case "help" : HelpUI.open(gameObject); break;
                        case "compact_mode" : toggle_compact_mode(); break;
                    }
                });
                button("X", "button.close.top", 30f, hide);
            });
            section(() =>{
                section(() =>{
                    label("Search Craft:", "h2");
                    GUI.SetNextControlName("main_search_field");
                    search_string = GUILayout.TextField(search_string, width(compact_mode ? 345 : 500));
                    if(last_search != search_string){
                        filter_craft();
                    }
                    last_search = search_string;
                    if(compact_mode){fspace();}
                    button("clear", 40f, clear_search);
                });
                fspace();
                if(!compact_mode){
                    craft_display_buttons();
                }
            });
            if(compact_mode){
                section(() =>{
                    stock_craft_toggle_checkbox();
                    fspace();
                    craft_display_buttons();
                });
            }
        }

        //The Main craft list
        protected void draw_main_section(float section_width){
            v_section(section_width, main_section_height, false, (inner_width)=>{

                if(show_headers){
                    section(inner_width, section_header_height, ()=>{
                        if(compact_mode){
                            if(kerbalx_mode){
                                string kerbalx_categories_label = kerbalx_categories.items[KerbalX.loaded_craft_type];
                                float kx_cat_width = GUI.skin.button.CalcSize(new GUIContent(kerbalx_categories_label)).x;
                                dropdown(kerbalx_categories_label, StyleSheet.assets["caret-down"], "kerbalx_categories", kerbalx_categories, this, kx_cat_width, (resp) => {
                                    switch(resp){
                                        case "users"            : KerbalX.load_users_craft(); break;
                                        case "favourites"       : KerbalX.load_favourites(); break;
                                        case "past_downloads"   : KerbalX.load_past_dowloads(); break;
                                        case "download_queue"   : KerbalX.load_download_queue(); break;
                                    }
                                });
                                version_menu.selected_items = KerbalX.selected_versions_list;
                                dropdown(version_menu.selected_items.Count + " versions shown", StyleSheet.assets["caret-down"], "kerbalx_versions_menu", version_menu, this, 80f, (resp) => {
                                    Version v = new Version(resp);
                                    KerbalX.v_toggle[v] = !KerbalX.v_toggle[v];
                                    filter_craft();
                                });
                            }else{
                                toggle_tags_menu.selected_items = Tags.selected_tags();
                                dropdown("Tags", StyleSheet.assets["caret-down"], "tags_select_menu", toggle_tags_menu, this, 40f, (resp) => {
                                    if(resp == "new_tag"){
                                        create_tag_dialog(true);
                                    }else{
                                        Tags.toggle_active(resp);                                    
                                    }
                                });
                            }
                        }else{
                            label("Showing " + CraftData.filtered.Count + " Craft", "h2");
                        }
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
                bool craft_visible = false;

                scroll_pos["main"] = scroll(scroll_pos["main"], "craft.list_container", inner_width, main_section_height, craft_list_width => {                    
                    item_last_height = 0;
                    if(!craft_list_overflow){
                        craft_list_width += 20;
                    }
                    for(int i=0; i < CraftData.filtered.Count; i++){
                        CraftData craft = CraftData.filtered[i];

                        //determine if this craft is visible in the scroll view and set craft.draw to true if it is (or false if not) BUT only perform this check during a 
                        //Layout event, otherwise the switch between drawing the full detail and the placeholder causes GUI errors (of the pushing more than poping sort).
                        craft_visible = craft.list_position + craft.list_height > scroll_pos["main"].y-100 && craft.list_position < scroll_pos["main"].y + main_section_height+100;
                        if(Event.current.type == EventType.Layout){
                            craft.draw = (calculate_heights || craft_visible);
                        }
                        //either draw the full detail craft item or a placeholder depending on if it is in the scroll view.
                        if(craft.draw){
                            draw_craft_list_item(craft, craft_list_width); //render each craft
                        }else{
                            section(section_width-(30f), craft.list_height-5, "craft.list_item", (w)=>{ //subtractions from width to account for margins and scrollbar
                                label(craft.name);
                            });
                        }
                        if(!thumbnail_generating && craft_visible && craft.thumbnail == null){
                            thumbnail_generating = true;
                            StartCoroutine(craft.load_thumbnail_image());
                        }

                        //this is used to get the top offset position of each item in the craft list and that is stored on the CraftData object
                        //facilitates maintaining focus on list items when using the up/down arrow keys to scroll.
                        if(calculate_heights && Event.current.type == EventType.Repaint){
                            craft.list_position = item_last_height;
                            craft.list_height = GUILayoutUtility.GetLastRect().height + 5; //+5 for margin
                            item_last_height += craft.list_height;
                        }
                    }
                    if(CraftData.filtered.Count == 0){
                        show_no_matching_craft_message();
                    }
                });
                if(calculate_heights && Event.current.type == EventType.Repaint){
                    craft_list_overflow = item_last_height+10 >= main_section_height;
                }
                scroll_pos["main"] = drag_scroll(GUILayoutUtility.GetLastRect(), scroll_pos["main"]);
            });            
        }

        //Individual Craft Content for main list.
        protected void draw_craft_list_item(CraftData craft, float section_width){
            
            section(section_width-(30f), "craft.list_item" + (craft.group_selected ? ".group_selected" : craft.selected ? ".selected" : (craft.menu_open ? ".hover" : "")), (inner_width)=>{ //subtractions from width to account for margins and scrollbar                
                section(inner_width-85f,()=>{
                    v_section(()=>{
                        section(()=>{
                            label(craft.name, "craft.name");
                            if(craft.name != craft.file_name){                                
                                if(craft.file_name == autosave_craft_name){
                                    label("[" + craft.file_name + "]", "craft.autosaved_name");
                                }else{                                    
                                    label("(" + craft.file_name + ")", "craft.alt_name");
                                }
                            }
                            if(active_save_dir != current_save_dir){
                                fspace();
                                label("in save: " + craft.save_dir);
                            }
                        });
                        section(() => {
                            label(craft.part_count + " parts in " + craft.stage_count + " stage" + (craft.stage_count==1 ? "" : "s"), "craft.info", width(200f));
                            label("cost: " + humanize(craft.cost_total), "craft.cost", width(140f));
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

                section(80f, "section.thumbnail", (w)=>{
                    fspace();
                    GUILayout.Label(craft.thumbnail, "thumbnail", width(80), height(80));
                });
            }, evt => {
                if(evt.single_click){
                    GUIUtility.keyboardControl = 0;
                    CraftData.track_currently_selected();//remember which craft are selected before selecing another (used to restore selected after drag scrolling);
                    if(shift_key_down && CraftData.active_craft.Count >= 1){
                        CraftData.shift_select(craft);
                    }else if(ctrl_key_down){
                        CraftData.toggle_group_select(craft);
                    }else{
                        CraftData.toggle_selected(craft);  
                    }
                } else if(evt.double_click){
                    CraftData.select_craft(craft);
                    if(craft.remote){
                        load_craft("dl_load");
                    }else{
                        load_craft( craft.construction_type=="Subassembly" ? "subload" : "load");                        
                    }
                } else if(evt.right_click){

                    DropdownMenuData menu;
                    if(kerbalx_mode){
                        if(CraftData.selected_group.Count > 0){
                            menu = new DropdownMenuData(new Dictionary<string, string>{ {"bulk_download", "Download All"} });                            
                        }else{
                            menu = new DropdownMenuData(new Dictionary<string, string>{ {"view_remote", "View on KerbalX"}, {"download", "Download"} });                            
                        }
                    }else if(CraftData.selected_group.Count > 0){
                        menu = new DropdownMenuData(new Dictionary<string, string>{{"add_tag", "Add Tag"}, {"transfer", "Transfer"}});
                        if(saves_count > 1){menu.items.Add("move_copy", "Move/Copy");}
                        menu.special_items.Add("delete", "Delete");
                    }else {
                        CraftData.select_craft(craft); //ensure the right clicked craft is the focus craft
                        menu = new DropdownMenuData(new Dictionary<string, string>{{"add_tag", "Add Tag"}, {"rename", "Rename"}, {"transfer", "Transfer"}});
                        if(saves_count > 1){menu.items.Add("move_copy", "Move/Copy");}
                        if(!craft.stock_craft && KerbalXAPI.logged_in()){
                            if(craft.on_kerbalx()){
                                menu.items.Add("update", "Update on KerbalX");
                            }else{
                                menu.items.Add("share", "Share on KerbalX");                                
                            }
                        }
                        menu.special_items.Add("delete", "Delete");
                    }

                    menu.special_items_first = false;
                    menu.offset_menu = false;
                    Rect offset = new Rect(0,0,0,0);
                    Rect container = new Rect(Input.mousePosition.x-this.window_pos.x, Screen.height-Input.mousePosition.y-this.window_pos.y, 0,0);
                    
                    menu.set_attributes(container, offset, this, 0f, "menu.background", "menu.item.craft", (resp) => {
                        switch(resp){
                            case "add_tag"      : prepare_tag_menu(container); break;
                            case "rename"       : rename_craft_dialog(); break;
                            case "transfer"     : transfer_craft_dialog(); break;
                            case "move_copy"    : move_copy_craft_dialog(); break;
                            case "share"        : open_upload_interface(); break;
                            case "update"       : show_update_kerbalx_craft_dialog(); break;
                            case "delete"       : delete_craft_dialog(); break;
                            case "view_remote"  : Application.OpenURL(KerbalXAPI.url_to(craft.url)); break;
                            case "download"     : load_craft("dl_load"); break;
                            case "bulk_download": show_bulk_download_dialog(); break;
                        }                            
                    });
                    menu.on_menu_open = new Callback(()=>{
                        craft.menu_open = true;
                    });
                    menu.on_menu_close = new Callback(()=>{
                        craft.menu_open = false;
                    });
                    gameObject.AddOrGetComponent<Dropdown>().open(menu);
                }
            });
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
                            button("Recent 2", KerbalX.select_recent_versions);

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
                                if(focused_tag_name == tag_name){
                                    tag_style = "tag.section.hover";
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
                                        menu.on_menu_open = new Callback(()=>{focused_tag_name = tag_name;});
                                        menu.on_menu_close = new Callback(()=>{focused_tag_name = "";});

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
            scroll_relative_pos.x += (window_pos.width * (col_widths_current[0]+col_widths_current[1])) - 5f;
            scroll_relative_pos.y += 45f - scroll_pos["rhs"].y;

            v_section(section_width, (inner_width) =>{        
                section(inner_width, section_header_height, ()=>{                    
                    label("Craft Details", "h2");
                });
                scroll_pos["rhs"] = scroll(scroll_pos["rhs"], "side_panel.scroll", inner_width, main_section_height, scroll_width => {
                    if(CraftData.active_craft.Count == 0){
                        GUILayout.Space(25);
                        label("Select a craft to see info about it..", "h2.centered");
                        label("Right click on craft to access quick actions");
                        label("Hold CTRL to select multiple craft for group actions (ie tag multiple craft at once)");
                    }else if(CraftData.selected_group.Count > 0){
                        draw_right_hand_section_group_select(scroll_width);
                    }else{
                        draw_right_hand_section_single_select(scroll_width);
                    };
                });
            });
        }

        //Right Hand Section component: Craft Details for single craft
        protected void draw_right_hand_section_single_select(float scroll_width){
            GUILayout.Space(5);
            CraftData craft = CraftData.selected_craft;                        
            section(()=>{label(craft.name, "h2");});

            if(bool.Parse(CraftManager.settings.get("show_craft_icon_in_details"))){                
                section(() =>{
                    fspace();
                    GUILayout.Label(craft.thumbnail, "thumbnail", width(scroll_width*0.6f), height(scroll_width*0.6f));
                    fspace();
                });
            }

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
            if(CraftManager.game_version.ToString() != craft.ksp_version){
                section(() =>{
                    label("Built in KSP:", "bold.compact");
                    label(craft.ksp_version, "compact");
                });
            }
                
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
                if(KerbalX.loaded_craft_type == "download_queue"){
                    button("remove from downloads", ()=>{
                        KerbalX.remove_from_download_queue(craft);
                    });
                }

                button("View on KerbalX", "hyperlink.bold", ()=>{
                    Application.OpenURL(KerbalXAPI.url_to(craft.url));
                });
            }else{
                GUILayout.Space(15);
                gui_state(!upload_interface_ready, ()=>{
                    section(() => {                                    
                        button("transfer", transfer_craft_dialog);
                        if(saves_count > 1){
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
                                        button("Share on KerbalX", open_upload_interface);
                                    }
                                }
                            }else{
                                button("Login to KerbalX to share craft", "button.small", ()=>{
                                    show_must_be_logged_in(KerbalX.close_login_dialog);
                                });
                            }                                   
                        });
                        section((w) => {
                            button("Mod Lookup", ()=>{
                                mod_lookup_dialog(craft);
                            });
                        });
                    }
                });
                GUILayout.Space(15);
                section((w) =>{
                    label("Tags", "h2");
                    fspace();

                    tags_menu_content.selected_items = craft.tag_names();
                    gui_state(!upload_interface_ready, ()=>{
                        dropdown("Add Tag", StyleSheet.assets["caret-down"], "add_tag_menu", tags_menu_content, this, scroll_relative_pos, 70f, "Button", "menu.background", "menu.item.small", resp => {
                            respond_to_tag_menu(resp);
                        });
                    });
                });
                draw_tags_list();

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
        }

        //Right Hand Section component: Craft Details for multiple craft
        protected void draw_right_hand_section_group_select(float scroll_width){
            GUILayout.Space(5);
            section(()=>{label(CraftData.selected_group.Count + " Craft Selected", "h2");});

            float total_cost = 0;
            float total_mass = 0;
            int total_crew = 0;
            for(int i=0; i < CraftData.selected_group.Count; i++){
                total_cost += CraftData.selected_group[i].cost_total;
                total_mass += CraftData.selected_group[i].mass_total;
                total_crew += CraftData.selected_group[i].crew_capacity;
            }
            section(()=>{
                label("Total Cost", "bold.compact");
                label(humanize(total_cost), "compact");
            });
            section(()=> {
                label("Total Mass", "bold.compact");
                label(humanize(total_mass), "compact");
            });
            section(()=>{
                label("Total Crew Capacity", "bold.compact");
                label(total_crew.ToString(), "compact");
            });

            if(!kerbalx_mode){
                GUILayout.Space(15);
                section(() =>{                                    
                    button("transfer", transfer_craft_dialog);
                    if(saves_count > 1){
                        button("move/copy", move_copy_craft_dialog);
                    }
                });
                section(() =>{                                
                    button("delete", "button.delete", delete_craft_dialog);
                });
                GUILayout.Space(15);
                section(()=>{
                    label("Tags", "h2");
                    fspace();
                    tags_menu_content.selected_items = tags_for_active_craft;
                    dropdown("Add Tags", StyleSheet.assets["caret-down"], "add_tag_menu", tags_menu_content, this, scroll_relative_pos, 70f, "Button", "menu.background", "menu.item.small", resp => {
                        respond_to_tag_menu(resp);
                    });                                
                });
                label("Tags used by all selected craft");
                draw_tags_list();
            } else{                
                button("Download All", "button.load", show_bulk_download_dialog);
                label("note: bulk downloads will replace any existing craft with the same name as the ones being downloaded", "small");
            }
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
                                    craft.upload_data.craft_name = GUILayout.TextField(craft.upload_data.craft_name, width(inner_width-70f));
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
                                    craft.upload_data.hash_tags = GUILayout.TextField(craft.upload_data.hash_tags, width(inner_width-70f));
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
                                    if(image_data.images.Count == 0){
                                        label("You don't have any pictures in " + CraftManager.screenshot_dir);
                                        label("click 'take new picture' to grab a screenshot from in the editor");
                                        label("You can change the folder Craft Manager looks for images in, in the Craft Manager settings");
                                    }
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
                    }
                });                
            });
        }

        //Botton Section: Load buttons
        protected void draw_bottom_section(float section_width){
            section("thin.section", () =>{
                if(KerbalX.download_queue_size > 0){
                    button(KerbalX.download_queue_size + " craft waiting to download", "download_waiting", KerbalX.load_download_queue);
                }
            });
            if(compact_mode){
                v_section("bottom.section", () =>{
                    section(()=>{
                        fspace();                   
                        action_buttons();
                    });
                    if(CraftManager.status_info != "" || show_transfer_indicator){
                        section(()=>{
                            kerbalx_status_indicator();
                        });
                    }
//                    label("", "save_state_indicator" + (CraftData.loaded_craft_saved ? ".saved" : ".unsaved"));
                });
            } else{
                section("bottom.section", () =>{
                    v_section(()=>{
                        kerbalx_status_indicator();
//                        label("", "save_state_indicator" + (CraftData.loaded_craft_saved ? ".saved" : ".unsaved"));
                    });
                    fspace();                   
                    action_buttons();
                });
            }
        }



        //**GUI sub components**//

        //draws the primary 'action' button for Load/download/upload with dropdown modifier menu and the main close UI button
        private void action_buttons(){
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
        }

        //draws a progress indicator which is shown during all interaction with KerbalX
        private void kerbalx_status_indicator(){
            if(CraftManager.status_info != ""){
                label(CraftManager.status_info, (compact_mode ? "small" : "Label"));
            }
            if(show_transfer_indicator){
                label((transfer_is_upload ? "Uploading Craft...." : "Updating Craft...."), "transfer_progres.text");
            }
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
                            label("", "progbox.active" + (compact_mode ? ".compact" : ""));
                        }else{
                            label("", "progbox" + (compact_mode ? ".compact" : ""));
                        }
                    }
                }
            });
        }

        private void stock_craft_toggle_checkbox(){
            if(!kerbalx_mode){
                section("stock_craft_toggle", ()=>{                        
                    bool prev_state = exclude_stock_craft;
                    exclude_stock_craft = !GUILayout.Toggle(!exclude_stock_craft, "");
                    button("include Stock Craft", "stock_craft_toggle_button", ()=>{exclude_stock_craft = !exclude_stock_craft;});
                    if(exclude_stock_craft != prev_state){
                        filter_craft();
                        CraftManager.settings.set("exclude_stock_craft", exclude_stock_craft.ToString());
                    }
                });
            }
        }

        internal int draw_tags_list(bool include_auto_tags = true){
            int count = 0;
            foreach(string tag in tags_for_active_craft){
                section(() =>{
                    if(include_auto_tags || !Tags.instance.autotags_list.Contains(tag)){
                        label(tag, "compact");
                        count+=1;
                        fspace();
                        if(!Tags.instance.autotags_list.Contains(tag)){                                    
                            gui_state(!upload_interface_ready, ()=>{
                                button("x", "tag.delete_button.x", ()=>{Tags.untag_craft(CraftData.active_craft, tag);});
                            });
                        }
                    }
                });
            }
            return count;
        }

        private void craft_display_buttons(){
            if(save_menu_width == 0){               
                adjust_save_menu_width();
            }
            if(KerbalX.enabled || saves_count > 1){
                label("view craft from:", "craft_select_label");
                string dropdown_label = "";
                if(kerbalx_mode){
                    dropdown_label = "KerbalX";
                    save_select_menu.selected_item = "kerbalx_remote";
                } else{
                    dropdown_label = (active_save_dir == all_saves_ref ? "All Saves" : (active_save_dir == current_save_dir ? "Current Save" : active_save_dir));
                    save_select_menu.selected_item = active_save_dir;
                }
                if(dropdown_label.Length > 20){                    
                    System.Text.StringBuilder s = new System.Text.StringBuilder();
                    dropdown_label = s.Insert(0, dropdown_label.ToCharArray(0, 20)).ToString() + "...";
                }
                dropdown(dropdown_label, StyleSheet.assets["caret-down"], "save_menu", save_select_menu, this, save_menu_width, change_craft_source);
            }
        }

        protected void show_no_matching_craft_message(){
            GUILayout.Space(main_section_height*0.4f);
            string message = "";
            if(search_criteria.ContainsKey("search") && !String.IsNullOrEmpty((string)search_criteria["search"])){
                message += "matching \"" + search_criteria["search"] + "\" ";
            }
            if(KerbalX.loaded_craft_type == "" && Tags.selected_tags().Count > 0){
                message += "with tags: " + Tags.selected_tags().n_join(((string)search_criteria["tag_filter_mode"]).ToLower()) + " ";
            }

            if(!String.IsNullOrEmpty(message.Trim()) && search_criteria.ContainsKey("type")){
                Dictionary<string, bool> types = (Dictionary<string, bool>) search_criteria["type"];
                List<string> selected_types = new List<string>();
                foreach(KeyValuePair<string, bool> t in types){
                    if(t.Value){                        
                        selected_types.Add(t.Key);
                    }
                }
                message += "in " + selected_types.n_join("or");
            }


            if(!String.IsNullOrEmpty(message.Trim())){
                label("No Craft found " + message, "h2");                            
            }else{
                if(KerbalX.loaded_craft_type == ""){
                    label("You don't have any craft here!", "h2");                                
                }else{
                    switch(KerbalX.loaded_craft_type){
                        case "users" : 
                            label("You don't seem to have any craft on KerbalX", "h2");
                            label("quick, upload something!");
                            break;
                        case "past_downloads" : 
                            label("You don't have any past downloads", "h2");
                            label("craft that you've previously downloaded from KerbalX will be shown here");
                            break;
                        case "favourites" :
                            label("You don't have any craft favourited on KerbalX", "h2");
                            label("Go to KerbalX.com, find somthing awesome and click the star icon, then it will appear here");
                            break;
                        case "download_queue": 
                            label("There is nothing in your Download Queue", "h2");
                            label(
                                "The download queue will list craft which you have tagged for download on KerbalX\n" +
                                "you need to enable 'Deferred Downloads' in your KerbalX settings to use this feature"
                            );
                            label(
                                "With Deferred Downloads enabled when you click download on a craft on KerbalX it won't download in your browser, instead it will appear here.\n" +
                                "So you can use your mobile or another computer to browse the site, mark craft for download and have them delivered here."
                            );
                            break;
                    }
                }
            }

        }
    }
}
