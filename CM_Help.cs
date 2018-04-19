using System;
//using System.IO;
//using System.Collections.Generic;

using UnityEngine;

//using KatLib;


namespace CraftManager
{
    public class HelpUI : CMUI
    {
        float rhs_width = 600;
        float lhs_width = 150;
        Vector2 scroll_pos = new Vector2();

        private void Start(){
            float inner_width = lhs_width + rhs_width + 10;
            window_title = "Craft Manager Help";
            if(CraftManager.main_ui != null){                
                window_pos = new Rect(CraftManager.main_ui.window_pos.x + CraftManager.main_ui.window_pos.width / 2 - inner_width / 2, CraftManager.main_ui.window_pos.y + 100, inner_width, 5);
            } else{
                window_pos = new Rect(Screen.width/2 - inner_width/2, Screen.height/3, inner_width, 5);
            }
            CraftManager.help_ui = this;
            content = intro_content;
        }

        protected delegate void HelpContent(float content_width);
        HelpContent content;


        protected override void WindowContent(int win_id){ 
            section((w) =>{
                v_section(lhs_width, 300f, "dialog.section", (w2) =>{
                    button("Intro", ()=>{switch_content("intro");});
                    button("Keyboard Shortcuts", ()=>{switch_content("keyboard_shortcuts");});
                    button("Tags", ()=>{switch_content("tags");});
                });
                v_section(10, 300f, (w2)=>{
                    label("");
                });
                v_section(rhs_width, 300f, "dialog.section", (w2) =>{
                    scroll_pos = scroll(scroll_pos, w2-8, 294f, (inner_width)=>{
                        content(inner_width-80);
                    });
                });
            });
            section(() =>{
                fspace();
                button("close", close);
            });
        }

        public void switch_content(string content_name){
            switch(content_name){
                case "intro" :              content = intro_content;break;
                case "keyboard_shortcuts" : content = keyboard_shortcuts;break;
                case "tags" :               content = tags_content;break;
                default :                   content = intro_content;break;
            }
            scroll_pos.y = 0;
        }

        private void intro_content(float content_width){
            label("Craft Manager Basics", "h2");
            label("CraftManager enables you to search, sort, group your craft with tags, move/copy/load craft from other saves and post your creations on KerbalX.");
            label(
                "The top left of the interface lets you switch between SPH/VAB and Subassembly craft.\n" +
                "(You can hold CTRL while clicking to select them together or press 'All'.)"
            );

            label("In the top right, the 'Include Stock Craft' toggle lets you show/hide the stock craft that come with KSP");
            label("and to the right of that is a menu to access your settings and this help section....so you probablly already found that if you're here!", "small.compact");

            label(
                "On the left is the main search field which allows you to search for craft by name.\n" + 
                "To the right of the search field is the 'KerbalX Craft' button (only shown if KerbalX integration is enabled) that will switch you to browsing your craft on KerbalX\n." + 
                "And right of that is a dropdown menu which lets you switch between the different saves (in this KSP install) so you can view/load craft from other saves"
            );

            label(
                "The main section of the interace is divided into 3 panels. On the left are the tags, in the middle is the craft list and when you click on a craft, its details will be shown in the right hand panel"
            );
            section(() =>{
                label("see more about ");
                button("tags,", "hyperlink.left", ()=>{switch_content("tags");});
                button("the craft list,", "hyperlink.left", ()=>{switch_content("craft_list");});
                button("craft details panel", "hyperlink.left", ()=>{switch_content("craft_details");});
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
                });
                v_section(content_width*0.7f, (w)=>{
                    label("scroll up craft list", "line");
                    label("scroll down craft list", "line");
                    label("load selected craft", "line");
                    label("create new tag", "line");
                    label("Focus cursor on search field", "line");
                    label("move cursor from search field to list");
                    label("Close Craft Manager interface", "line");
                });
            });
        }

        private void tags_content(float content_width){
            label("Tags", "h2");
            label(
                "Click a tag to reduce the craft list to just craft with that tag.\n" + 
                "Click the '+' at the bottom to create a new tag.\n" +
                "Click on craft and then use the 'add tag' dropdown in the craft details section to add/remove tags.\n" + 
                "Right click on a tag to edit, delete or Exclude it. (excluding a tag hides its craft from the craft list).\n"
            );

            label("Rule based tags", "h3");
            label(
                "Tags can be given a 'rule' and will then automatically tag any craft which match that rule.\n" + 
                "Rules are made up of an attribute, a comparitor and a value, for example;\n" + 
                "'mass', >, 42 - would match any craft with a mass greater than 42 tons.\n" + 
                "'name', includes, \"test\" - would match any craft with the word \"test\" in its name.\n" + 
                "'name', starts_with, \"sat\" - would match craft with names that start with \"sat\".\n" + 
                "When creating/editing a tag, click 'Use Auto Tag rule', and then select the attribute, comparitor and value.\n"
            );

            label("Tag Filtering Modes", "h3");
            label(
                "By default the filtering mode is set to AND. This means if you'd selected two tags, it will show you craft that have both the first tag AND the second tag\n" + 
                "If you change the mode to OR it would show craft that have the first tag OR the second tag."
            );
            label("Each tag has a number which shows the number of craft that tag would return, given any other tags/filters, and tag mode that is in effect");

        }

        public static void open(GameObject go){           
            go.AddOrGetComponent<HelpUI>();
        }

        public void close(){            
            GameObject.Destroy(CraftManager.help_ui);            
        }

    }
}

