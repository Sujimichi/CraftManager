using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using ExtensionMethods;
using KatLib;

namespace CraftManager
{
    //CMBrowserDialogs is inherited by the main CMBrowser class and contains all the popup dialogs which are used by the main interface.
    public class CMBrowserDialogs : CMBrowserBase
    {
        
        //All these dialogs use the 'show_dialog' method which is lurking below them
        //The show_dialog method takes care of all common aspects, leaving these dialog methods DRY and minimal
        //The delegate passed to show_dialog is expected to return a string (resp), This is used to pass back
        //error messages or a success status code ("200").  If "200" is returned the dialog will be closed, 
        //any other string will be shown as an error message to the user.  
        //Dialogs can also use a submit(<args>) method which draws a button that calls the given delegate, but also binds
        //the delegate to be triggered by enter key press. the delagate must return a response string based on the success 
        //of the action which is then returned by submit.


        protected void rename_craft_dialog(){ rename_craft_dialog(CraftData.selected_craft); }
        protected void rename_craft_dialog(CraftData craft){            
            craft.new_name = craft.name;
            string resp = "";
            show_dialog("Rename Craft", "rename: " + craft.name, d =>{
                GUI.SetNextControlName("dialog_focus_field");
                craft.new_name = GUILayout.TextField(craft.new_name, width(d.window_pos.width-22));
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
            DropdownMenuData move_copy_save_menu = new DropdownMenuData();
            save_menu_data(move_copy_save_menu);
            move_copy_save_menu.items.Remove(craft.save_dir);
            move_copy_save_menu.items.Remove(all_saves_ref);
            move_copy_save_menu.items.Remove("kerbalx_remote");
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
                    List<int> counts = new List<int>{ keys.Count, craft.upload_data.action_groups.Count };
                    float label_width = 45f;
                    GUI.SetNextControlName("dialog_focus_field");
                    section(()=>{
                        for(int j=0; j < counts.Count; j++){
                            v_section(350f, (w)=>{
                                for(int i=0; i < counts[j]; i++){
                                    section(()=>{
                                        string key = keys[i];
                                        if(j==0 && key.Length <= 1 || j==1 && key.Length > 1){
                                            label(key, width(label_width));
                                            action_groups[key] = GUILayout.TextField(action_groups[key], width(w-label_width));
                                        }
                                    });
                                }
                            }); 
                        }
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

        //Show user option to save current craft or carry on loading
        internal void load_craft_confirm_dialog(DialogAction action){
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

        internal void load_craft_with_missing_parts_dialog(CraftData craft, string load_type){
            string resp = "";
            List<string> missing_parts_list = new List<string>();
            Dictionary<string, string> identified_parts_list = new Dictionary<string, string>();
            bool show_identified_parts = false;
            List<string> required_mods = new List<string>();
            Vector2 part_list_scroll = new Vector2();
            show_dialog("Missing Parts", "This Craft has missing parts", d =>{
                    
                section(()=>{
                    button("list missing parts", ()=>{
                        show_identified_parts = false;
                        missing_parts_list = craft.list_missing_parts();
                    });
                    if(KerbalX.enabled){
                        button("KerblaX Mod lookup", () => {
                            KerbalX.lookup_parts(craft.list_missing_parts(), identified_parts => {
                                identified_parts_list = identified_parts;
                                missing_parts_list.Clear();
                                part_list_scroll = new Vector2();
                                required_mods.Clear();
                                foreach(string mod_name in identified_parts_list.Values){
                                    if(mod_name != "Squad"){
                                        required_mods.AddUnique(mod_name);
                                    }
                                }
                                show_identified_parts = true;
                            });
                        });
                    }
                });
                if(KerbalX.enabled){
                    label("click \"KerbalX Mod Lookup\" and KerbalX will try to work out which mods you need for the missing parts");
                }else{
                    label("tip: if you enable KerbalX integration, Craft Manager can use KerbalX to try and work out which mods you need", "small");
                }


                if(missing_parts_list.Count > 0){
                    part_list_scroll= scroll(part_list_scroll, d.window_pos.width, 200, (w) => {
                        foreach(string part_name in missing_parts_list){
                            label(part_name);
                        }
                    });
                }

                if(show_identified_parts){
                    if(identified_parts_list.Count > 0){
                        part_list_scroll= scroll(part_list_scroll, d.window_pos.width, 200, (w) => {
                            foreach(KeyValuePair<string, string> pair in identified_parts_list){
                                section(()=>{
                                    label(pair.Key, "Label", w*0.3f);
                                    label(pair.Value, "Label", w*0.6f);                                    
                                });
                            }
                        }); 
                        if(identified_parts_list.ContainsValue("Squad")){
                            label("You seem to be missing some parts from the core game!", "alert");
                        }
                        if(required_mods.Count > 0){                            
                            label("This craft needs the following mods:\n" + required_mods.n_join("and"));
                        }

                    }else{
                        label("no part info found");
                    }
                }


                section(()=>{
                    button("Try to load anyway", ()=>{
                        load_craft(load_type + "_ignore_missing", false);
                        resp = "200";
                    });
                    button("Cancel", close_dialog);                    
                });
                return resp;
            });
        }

        //**Tag Dialogs**//

        //Call Create Tag Dialog (using tag_dialog_form)
        internal void create_tag_dialog(){create_tag_dialog(true, null, true);}
        internal void create_tag_dialog(bool show_rule_opts = true, CraftData auto_add_craft = null, bool autopos = false){
            float top = this.window_pos.y + (this.window_pos.height*0.4f);
            float left = this.window_pos.x + (this.window_pos.width/2) - (200f);
            if(autopos){
                top = this.window_pos.y + scroll_relative_pos.y + main_section_height - 130f;
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
                tag_name = GUILayout.TextField(tag_name, width(400f-22f));

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



        //**KerbalX Specific Dialogs**//


        internal void download_confirm_dialog(DialogAction action = null){
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

        protected void populate_new_save_dialog(){
            KerbalX.get_craft_ids_by_version((craft_by_version, versions) =>{
                string v1 = versions[0].ToString();
                string v2 = versions[1].ToString();
                string resp = "";
                Version ksp_version = new Version(Versioning.GetVersionString());
                KerbalX.log_scroll = new Vector2();
                KerbalX.bulk_download_log = "";

                show_dialog("Import Craft From KerbalX", "", d =>{
                    label("You don't have any craft in this save yet.", "h2");
                    label("Do you want to fetch your craft from KerbalX?", "h2");

                    if(versions[0] == ksp_version){
                        button("download " + craft_by_version[v1].Count + " craft built in KSP " + ksp_version, ()=>{
                            KerbalX.bulk_download(craft_by_version[v1], current_save_dir, ()=>{});
                        });
                    }else{
                        label("You don't have any craft made in this version of KSP");
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

        internal void close_update_kerbalx_craft_dialog(){
            close_dialog();
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

                foreach(string error in craft.upload_data.errors){
                    label(error, "error.bold");
                }

                GUILayout.Space(10f);
                gui_state(craft.upload_data.update_to_id > 0, ()=>{
                    button("Confirm Update", "button.large", ()=>{
                        craft.upload_data.put();
                    });
                });
                GUILayout.Space(4f);
                section(()=>{
                    button("Upload as a new Craft", "button.large", open_upload_interface);
                    button("Cancel", "button.large", close_dialog);                    
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

    }
}

