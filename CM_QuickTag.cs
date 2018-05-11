using System;
using System.IO;
using System.Collections.Generic;
using ExtensionMethods;
using UnityEngine;

using KatLib;

namespace CraftManager
{
   
    public class QuickTag : CMBrowserBase
    {
        internal static QuickTag instance = null;

        internal static QuickTag open(GameObject go){           
            return go.AddOrGetComponent<QuickTag>();
        }

        internal static void close(){
            if(QuickTag.instance != null){
                QuickTag.instance.save_window_pos();
                GameObject.Destroy(QuickTag.instance);
            }
        }


        new private float window_width = 300;
        private float window_height = 5;
        private bool first_pass = true;
        private Rect last_window_pos; 
        private int last_tag_count = 0;
        private string last_editor_craft_name = "";


        private void Start(){
            instance = this;
            set_window_pos_from_settings();
            prevent_click_through = true;
            alt_window_style = new GUIStyle(HighLogic.Skin.window);
            alt_window_style.padding.top = 8; //remove excess padding to hide titlebar
            window_title = "";

            tags_menu_content.remote_data = tags_menu_data;
            tags_menu_content.special_items.Add("new_tag", "New Tag");
            get_current_craft();    
        }


        protected override void WindowContent(int win_id) {
            if(first_pass){
                GUI.FocusWindow(window_id);
                first_pass = false; 
            }

            if(EditorLogic.fetch.ship.shipName != last_editor_craft_name){
                get_current_craft();
            }

            if(CraftData.selected_craft == null){
                label("Can't find current craft, perhaps it's not saved yet?", "h2");
                get_current_craft();
            } else{
                label("Quick Tag: " + CraftData.selected_craft.name, "h2");

                tags_menu_content.selected_items = CraftData.selected_craft.tag_names();
                section(() =>{
                    fspace();
                    dropdown("Add Tag", StyleSheet.assets["caret-down"], "add_tag_menu", tags_menu_content, this, scroll_relative_pos, 70f, "Button", "menu.background", "menu.item.small", resp => {
                        respond_to_tag_menu(resp);
                    });
                });
                v_section("dialog.section", ()=>{
                    CraftManager.main_ui.draw_tags_list(false);
                });

                if(CraftData.selected_craft.tags().Count != last_tag_count){ //reset the window pos, which forces the height to be reset & so shrinking the window after a tag is removed.
                    save_window_pos();
                    set_window_pos_from_settings();
                    last_tag_count = CraftData.selected_craft.tags().Count;
                }
            }
            section(() =>{
                fspace();
                button("close", close);
            });
            key_event_handler();
        }

        private void get_current_craft(bool allow_retry = true){
            CraftData craft = CraftData.all_craft.Find(c => 
                c.save_dir == CraftManager.main_ui.current_save_dir && c.construction_type == EditorDriver.editorFacility.ToString() && c.name == EditorLogic.fetch.ship.shipName
            );
            if(craft != null){
                CraftData.select_craft(craft);
            } else{
                if(allow_retry){
                    refresh();
                    get_current_craft(false);
                } else{
                    CraftData.deselect_all();
                }
            }
            last_editor_craft_name = EditorLogic.fetch.ship.shipName;
        }

        private void save_window_pos(){
            if(window_pos.x != last_window_pos.x || window_pos.y != last_window_pos.y){
                CraftManager.settings.set("quick_tag_pos", window_pos.x + "," + window_pos.y);
            }
        }

        private void set_window_pos_from_settings(){
            try{
                string pos = CraftManager.settings.get("quick_tag_pos");
                if(pos == "auto"){
                    set_window_pos();
                }else{
                    int x = int.Parse(pos.Split(',')[0]);
                    int y = int.Parse(pos.Split(',')[1]);
                    set_window_pos(x, y);
                }
            }
            catch(Exception e){
                CraftManager.log("failed to read setting for quick tag position, " + e.Message + "\ndefaulting to auto position");
                set_window_pos();
                save_window_pos();
            }
        }

        private void set_window_pos(int x = -1, int y = -1){
            if(x == -1 && y == -1){
                window_pos = new Rect(Screen.width - window_width - 50, 50, window_width, window_height);
            } else{
                window_pos = new Rect(x, y, window_width, window_height);
            }
            last_window_pos = window_pos;
        }

        protected override void key_event_handler(){
            Event e = Event.current;          
            if(e.type == EventType.keyDown){
                //'esc' - close interface
                if(e.keyCode == KeyCode.Escape){
                    e.Use();
                    QuickTag.close();
                }
            }
        }

    }
}

