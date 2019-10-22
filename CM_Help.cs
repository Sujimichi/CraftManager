using UnityEngine;

namespace CraftManager
{
    public class HelpUI : CMUI
    {
        float rhs_width = 600;
        float lhs_width = 150;
        float content_height = 400f;
        Vector2 scroll_pos = new Vector2();
        bool return_to_top = false;
        string active_content = "intro";

        private void Start(){
            float inner_width = lhs_width + rhs_width + 10;
            window_title = "Craft Manager Help";
            if(CraftManager.main_ui != null){                
                window_pos = new Rect(CraftManager.main_ui.window_pos.x + CraftManager.main_ui.window_pos.width / 2 - inner_width / 2, CraftManager.main_ui.window_pos.y + 100, inner_width, 5);
            } else{
                window_pos = new Rect(Screen.width/2 - inner_width/2, Screen.height/3, inner_width, 5);
            }
            CraftManager.help_ui = this;
        }

        protected override void WindowContent(int win_id){ 
            section((w) =>{
                v_section(lhs_width, content_height, "dialog.section", (w2) =>{
                    content_button("Intro", "intro");
                    content_button("Keyboard Shortcuts", "keyboard_shortcuts");
                    content_button("Tags", "tags");
                    content_button("Craft List", "craft_list");
                    content_button("Craft Details", "craft_details");
                    content_button("Share on KerbalX", "upload_mode");
                    content_button("Download from KerbalX", "kerbalx_craft");
                    content_button("Compact Mode", "compact_mode");
                    content_button("Quick Tag", "quick_tag");

                });
                v_section(10, content_height, (w2)=>{
                    label("");
                });
                v_section(rhs_width, content_height, "dialog.section", (w2) =>{
                    if(return_to_top){
                        scroll_pos.y = 0; //setting y=0 in switch_content wasn't working, so a flag (return_to_top) is set and triggers the change here.
                        return_to_top = false;
                    }
                    scroll_pos = scroll(scroll_pos, w2-8, content_height-6f, (inner_width)=>{
                        show_content(active_content, inner_width-80);
                    });
                });
            });
            section(() =>{
                fspace();
                button("close", "button.large", close);
            });
        }

        private void content_button(string title, string content_name){
            button(title, "button" + (active_content==content_name ? ".down" : ""), ()=>{
                switch_content(content_name);
            });
        }

        public void switch_content(string content_name){
            active_content = content_name;
            return_to_top = true;
        }

        private void show_content(string content_name, float inner_width){
            switch(content_name){
                case "intro" :              intro_content(inner_width); break;
                case "keyboard_shortcuts" : keyboard_shortcuts(inner_width); break;
                case "tags" :               tags_content(inner_width); break;
                case "craft_list" :         craft_list_content(inner_width); break;
                case "craft_details" :      craft_details_content(inner_width); break;
                case "upload_mode" :        upload_interface_content(inner_width); break;
                case "kerbalx_craft":       kerbalx_craft_content(inner_width);break;
                case "compact_mode" :       compact_mode_content(inner_width);break;
                case "quick_tag":           quick_tag_content(inner_width);break;
            }
        }

        private void intro_content(float content_width){
            label("Craft Manager Basics", "h2");
            label("CraftManager enables you to search, sort, group your craft with tags, move/copy/load craft from other saves and post your creations on KerbalX.");
            label(
                "The top left of the interface lets you switch between SPH/VAB and Subassembly craft.\n" +
                "(You can hold CTRL while clicking to select them together or press 'All'.)"
            );
            label("In the top right, the 'Include Stock Craft' toggle lets you show/hide the stock craft that come with KSP");
            label(
                "On the left is the main search field, which allows you to search for craft by name.\n" + 
                "To the right of the search is the 'view craft from' dropdown menu. This lets you view craft from different saves or switch to viewing your craft on KerbalX.\n" +
                "(this menu won't be shown if you only have 1 save and KerbalX integration is disabled)."
            );
            label(
                "The main section of the inteface is divided into 3 panels. On the left are your tags, in the middle is the craft list and the right will show details about the selected craft.\n" + 
                "(You can turn on 'compact mode' to hide the left and right panels)."
            );
            label("Tip: You can right click on Craft and Tags for more actions.", "bold");

            section(() =>{
                label("see more about ");
                button("tags,", "hyperlink.inline", ()=>{switch_content("tags");});
                button("the craft list,", "hyperlink.inline", ()=>{switch_content("craft_list");});
                button("craft details panel", "hyperlink.inline", ()=>{switch_content("craft_details");});
                label("and");
                button("uploading to KerbalX", "hyperlink.inline", ()=>{switch_content("upload_mode");});
            });
        }

        private void keyboard_shortcuts(float content_width){
            label("Keyboard Shortcuts", "h2");
            label("Editor shortcuts - these can be disabled in settings");
            section(() =>{
                v_section(content_width*0.3f, (w)=>{
                    label("ctrl +  o", "line");
                    label("ctrl +  n", "line");
                    label("ctrl +  s", "line");
                });
                v_section(content_width*0.7f, (w)=>{
                    label("Opens CraftManager", "line");
                    label("Creates a new craft/clears editor", "line");
                    label("saves current craft", "line");
                });
            });
            label("Craft Manager Interface shortcuts");
            section(() =>{
                v_section(content_width*0.3f, (w)=>{
                    label("up arrow", "line");
                    label("down arrow", "line");
                    label("[enter]", "line");
                    label("ctrl +  t", "line");
                    label("ctrl +  f", "line");
                    label("[tab]", "line");
                    label("esc", "line");
                    label("ctrl + a", "line");
                    label("ctrl + click", "line");
                    label("shift + click", "line");
                });
                v_section(content_width*0.7f, (w)=>{
                    label("scroll up craft list", "line");
                    label("scroll down craft list", "line");
                    label("load selected craft", "line");
                    label("create new tag OR open Quick Tag (when CM is closed)", "line");
                    label("Focus cursor on search field", "line");
                    label("move cursor from search field to list");
                    label("Close Craft Manager interface", "line");
                    label("(on craft list) select all craft", "line");
                    label("(on craft list) select multiple craft", "line");
                    label("(on craft list) if a craft is already selected, holding shift and clicking on another craft will select all craft between them as well", "line");

                });
            });
        }

        private void tags_content(float content_width){
            label("Tags", "h2");
            label(
                "Click a tag to filter the craft list to just craft with that tag.\n" + 
                "Click on multiple tags to filter the list to craft that have all selected tags.\n" +
                "Click the '+' at the bottom to create a new tag.\n" +
                "Click on craft and then use the 'add tag' dropdown in the craft details section to add/remove tags. Or right click on craft in the craft list and select 'add tag'.\n" + 
                "Right click on a tag to edit, delete or Exclude it. (excluding a tag hides its craft from the craft list)."
            );

            label("Tags are save specific and are stored in a 'craft.tags' file in each save folder.  If you view craft from multiple saves then the tags shown will be combined and any tags with the same name will merge. " +
                "If you have a tag in one save, then view craft from multiple saves and tag a craft in a different save with that tag, the tag will also be added to the other save.\n"
            );

            label("Auto tags", "h3");
            label(
                "Tags can be given a 'rule' and will then automatically tag any craft which match that rule.\n" + 
                "Rules are made up of an attribute, a comparitor and a value, for example;\n" + 
                "'mass', >, 42 - would match any craft with a mass greater than 42 tons.\n" + 
                "'crew capacity', =, 3 - would match craft that take exactly 3 crew.\n" + 
                "'name', includes, \"falcon\" - would match any craft with the word \"falcon\" in its name.\n" + 
                "'name', starts_with, \"sat\" - would match craft with names that start with \"sat\".\n" + 
                "'stock', =, True - matches the preset craft which come with KSP\n" + 
                "When creating/editing a tag, click 'Use Auto Tag rule', and then select the attribute, comparitor and value. The available comparitors change according to the datatype of the attribute you select.\n"
            );

            label("Excluding Tags", "h3");
            label(
                "If you right click on tag and select 'Exclude' then any craft with that tag will be filtered out.\n" +
                "You can use this in conjuction with rule based tags, ie: create a tag with the rule 'name'-includes-'test' and then set it to be excluded and now all your rough test craft (assuming you called them 'test-something' or 'something-test') will be filtered out.\n"
            );

            label("Tag Filtering Modes", "h3");
            label(
                "By default the filtering mode is set to AND. This means if you'd selected two tags, it will show you craft that have both the first tag AND the second tag\n" + 
                "If you change the mode to OR it would show craft that have the first tag OR the second tag."
            );
            label("Each tag has a number which shows the number of craft that tag would return, given any other tags/filters, and tag mode that is in effect");

        }

        private void craft_list_content(float content_width){
            label("Craft List", "h2");
            label("The craft list can be filtered by using tags and searching. You can sort the list by various attributes using the drop down 'Sort' menu at the top");
            label(
                "Clicking on a craft will show you details about the craft in the right section.\n" + 
                "Double Clicking will load the craft.\n" +
                "You can also right click on craft for quick access to actions like renaming, adding tags etc"
            );
            label("The list can be scrolled using the mousewheel, by dragging it with the mouse and by pressing the up/down keys");
        }

        private void craft_details_content(float content_width){
            label("Craft Details", "h2");
            label(
                "The right hand side panel shows details about the currently selected craft and provides a bunch of actions (rename, move, share etc).\n" +
                "(You can also access these actions by right clicking on a craft in the list)."
            );

            label("Transfer Craft", "h3");
            label("This lets you move a craft between the SPH and VAB or turn a regular craft into a subassembly (or vice versa).\n" +
                "When moving a craft to the VAB from SPH (or otherway around) you have the option to tranfer to the other editor."
            );

            label("Move/Copy Craft", "h3");
            label("This lets you move/copy craft between saves. Click move/copy and then select another save (in this KSP install) and then choose to either move or copy the craft.");
            label("To move/copy a craft from another save into your current save, use the save dropdown menu at the top to switch to viewing craft in your other save and then use move/copy to bring it into your current save.");

            label("Rename craft", "h3");
            label("...");

            label("Delete craft", "h3");
            label("also self explanatory, just more deadly.");

            label("Share on KerbalX", "h3");
            label(
                "This action will only be visible if KerbalX integration is enabled and will change the interface show the unload layout." +            
                "If the craft has already been posted then this will say 'update craft'"
            );
            button("see more about uploading craft", "hyperlink.inline", () =>{
                switch_content("upload_mode");
            });
        }

        private void upload_interface_content(float content_width){
            label("Uploading to KerbalX", "h2");
            label("To upload one of your craft to KerbalX, click on the craft in the main list and click 'Share on KerbalX' in the right hand panel, or right click on the craft and click share.");
            label(
                "This will slide the interface into upload mode.  Most things should be already setup; check the craft's details, make sure you're happy with the tags, craft type and description.\n" +
                "You can also set info about the action groups."
            );
            label("Then you need to select at least one picture.  Choose a picture from the list on the right, or click 'take new picture' and the interface will temporarily hide amd give you a grab screenshot button (after tkaing a screenshot it will return you to the upload interface");
            label(
                "Finally, click the Upload button at the bottom.  A progress indicator will appear in the bottom left and when the upload is complete a popup will appear to let you know.\n" +
                "(while a craft is uploading you can close Craft Manager or carry on using it to view/load craft as normal)"
            );

            label("Updating an existing craft", "h2");
            label(
                "If you've already uploaded a craft to KerbalX the 'Share' button will be replaced with an 'update' button.\n" +
                "(This assumes that the craft on KerbalX has the same name as your local craft.)"
            );
            label(
                "Clicking update will open a dialog that lets you make sure it's the correct craft on KerbalX that will be updated.\n" +
                "If you have multiple craft with the same name, you can select the correct one from a dropdown menu."
            );

            label("Updating a craft with a different name", "h3");
            label(
                "If you local craft's name doesn't match the name of one of you craft on KerbalX, Craft Manager will assume that you want to upload it as a separate craft.\n" +
                "But if you want to replace one of your existing craft, click share and then at the bottom click update existing craft and you'll be able to select which craft to update"
            );

        }

        private void kerbalx_craft_content(float content_width){
            label("Your KerbalX Craft", "h2");
            label("To view your craft on KerbalX, use the 'view craft from' dropdown menu and select KerbalX");
            label(
                "The tags section will be replaced with a list of the different groups of craft you can view (Your Craft, Your Favourites, Your Past Downloads and Your Download Queue).\n" +
                "It will also list the KSP versions of the craft in those groups so you can filter craft to specific KSP version."
            );
            label("If a craft hasn't been downloaded then the default action is to download it, if you already have a craft with the same name there is an option to 'update' which will replace the existing craft with a fresh download");
            label(
                "There is also an option to 'Load without saving' (click the dropdown next to the main button) which will download the craft from KerbalX and load it into the editor without saving it locally." +
                "This is useful if you want to view a KerbalX craft without replacing an existing craft."
            );

                
        }

        private void compact_mode_content(float content_width){
            label("Compact Mode", "h2");
            label(
                "Compact mode hides the left and right panels so you just have the craft list.  Some functionality is reduced, but you can still do most things via right clicking on a craft and " +
                "you can select tags to filter using a dropdown 'tags' menu. (but you can't edit tags or select tags to be excluded, you'll need to return to full view mode to do that)."
            );
        }


        private void quick_tag_content(float inner_width){
            label("Quick Tag", "h2");
            label(
                "Quick Tag lets you add/remove/create tags for the currently loaded craft without having to open the main Craft Manager interface.\n" +
                "Click the tags icon in the toolbar or press ctrl + T to open it (esc or ctrl+t again will close it).\n" +
                "Then add or create tags from the dropdown menu or click the x by tags to remove them.\n" +
                "The Quick Tag dialog will remember where you position it, you can reset it's position from settings."
            );
        }

        public static void open(GameObject go){           
            go.AddOrGetComponent<HelpUI>();
        }

        public void close(){            
            GameObject.Destroy(CraftManager.help_ui);            
        }

    }
}

