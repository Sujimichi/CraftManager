using System;
using System.Collections.Generic;
using UnityEngine;
using KatLib;

namespace CraftManager
{
    //StyleSheet defines a set of GUIStyle and assigns them as custom styles to a new skin which is Instantiated from the current base_skin
    //StyleSheet.prepare will be called from inside OnGUI on the base class KerbalXWindow but only on the first call to OnGUI.
    //That will Instantiate the new skin and set it to a static var on KerbalXWindow (KXskin), once it's set further calls to StyleSheet.prepare won't do anything
    //Essentially this is a one time process that sets up all the GUIStyles needed and makes them available as named styles on the base_skin (OnGUI in KerbalXWindow
    //will set base_skin to the KXskin and unset it at the end so as to not effect other windows
    //....it's like we need a sorta sheet of styles, maybe one that can cascade, a cascading style sheet if you will....

    public delegate void StyleConfig(GUIStyle style);

    public class StyleSheet : MonoBehaviour
    {

        public static Dictionary<string, Texture> assets = new Dictionary<string, Texture>() { 
            { "ui_toolbar_btn",         GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "cm_ui"), false) },
            { "ui_toolbar_btn_hover",   GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "cm_ui_hover"), false) },
            { "SPH_placeholder",        GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "SPH_placeholder"), false) },
            { "VAB_placeholder",        GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "VAB_placeholder"), false) },
            { "Subassembly_placeholder",GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "SUB_placeholder"), false) },
            { "caret-down",             GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "caret-down"), false) },
            { "caret-up",               GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "caret-up"), false) },
            { "caret-down-green",       GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "caret-down-green"), false) },
            { "caret-down-green-hover", GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "caret-down-green-hover"), false) },
            { "arrow-down",             GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "arrow-down"), false) },
            { "arrow-up",               GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "arrow-up"), false) },
            { "tags",                   GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "tags"), false) },
            { "logo_small",             GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "KXlogo_small"), false) },     //166x30 
            { "logo_large",             GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "KXlogo"), false) },           //664x120 
            { "image_placeholder",      GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "image_placeholder"), false) },
            { "camera",                 GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "camera"), false) },
            { "menu",                   GameDatabase.Instance.GetTexture(Paths.joined("CraftManager", "Assets", "menu"), false) }
        };

        public GUISkin skin;
        public Dictionary<string, GUIStyle> custom_styles = new Dictionary<string, GUIStyle>();
        public Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        public void define_style(string name, GUIStyle inherits_from, StyleConfig config){
            GUIStyle style = new GUIStyle(inherits_from);
            style.name = name;
            custom_styles.Add(name, style);
            config(style);
        }

        public void define_style(string name, string inherits_from_name, StyleConfig config){
            GUIStyle style = new GUIStyle(custom_styles[inherits_from_name]);
            style.name = name;
            custom_styles.Add(name, style);
            config(style);
        }

        public void set_texture(string name, Color colour){
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, colour);
            tex.Apply();
            textures.Add(name, tex);
        }
        public void set_texture(string name, Color colour, TextureWrapMode wrap_mode){
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, colour);
            tex.wrapMode = wrap_mode;
            tex.Apply();
            textures.Add(name, tex);
        }

        public Texture2D make_texture(int width, int height, Color col){
            Color[] pix = new Color[width * height];
            for( int i = 0; i < pix.Length; ++i ){
                pix[ i ] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public StyleSheet(GUISkin base_skin){

            set_texture("blue_background", new Color(0.4f, 0.5f, 0.9f, 1), TextureWrapMode.Repeat);           
            set_texture("light_blue_background", new Color(0.37f, 0.41f, 0.62f, 0.4f));           
            set_texture("lighter_blue_background", new Color(0.4f, 0.5f, 0.9f, 0.6f));           
            set_texture("dark_background", new Color(0.12f, 0.12f, 0.12f, 0.5f));
            set_texture("pic_highlight", new Color(0.4f, 0.5f, 0.9f, 1));
            set_texture("green_background", new Color(0.2f, 0.6f, 0.2f, 1));
            set_texture("light_green_background", new Color(0.3f, 0.5f, 0.3f, 1));
            set_texture("red_background", new Color(0.51f, 0.44f, 0.44f, 0.4f));
            set_texture("clear_background", new Color(0f, 0f, 0f, 0f));
            set_texture("grey_background", Color.gray);


            set_texture("logging_in", Color.yellow);
            set_texture("logged_out", Color.red);
            set_texture("logged_in", Color.green);



            define_style("h1", base_skin.label, s =>{
                s.fontStyle = FontStyle.Bold;
                s.fontSize = 30;                
            });
            define_style("h2", "h1", s =>{
                s.fontSize = 20;
            });
            define_style("h3", "h1", s =>{
                s.fontSize = 15;
            });
            define_style("h1.centered", "h1", s =>{
                s.alignment = TextAnchor.LowerCenter;
            });
            define_style("upload_header", "h1", s =>{
                s.margin = new RectOffset(0,0,0,0);
                s.padding = new RectOffset(10,0,10,0);
                s.fontSize = 60;
            });
            define_style("upload_header.logo", base_skin.label, s =>{
                s.margin = new RectOffset(0,0,0,0);
                s.padding = new RectOffset(0,0,0,0);
            });

            define_style("h2.centered", "h2", s =>{
                s.alignment = TextAnchor.LowerCenter;
            });
            define_style("h2.tight", "h2", s =>{
                s.margin.bottom = 0; 
            });
            define_style("bold", base_skin.label, s =>{
                s.fontStyle = FontStyle.Bold;
            });
            define_style("small", base_skin.label, s =>{
                s.fontSize = 12;
            });
            define_style("centered", base_skin.label, s =>{
                s.alignment = TextAnchor.LowerCenter;
            });

            define_style("compact", base_skin.label, s =>{
                s.margin.top = 0;
                s.margin.bottom = 2;
            });

            define_style("line", "compact", s =>{                
                s.margin.bottom = 0;
            });
            define_style("bold.compact", "compact", s =>{
                s.fontStyle = FontStyle.Bold;
            });
            define_style("small.compact", "compact", s =>{
                s.fontSize = 12;
            });


            define_style("error", base_skin.label, s =>{
                s.normal.textColor = Color.red;
            });
            define_style("error.bold", "error", s =>{
                s.fontStyle = FontStyle.Bold;
            });
            define_style("alert", base_skin.label, s =>{
                s.normal.textColor = new Color(0.8f,0.3f,0.2f,1);
            });
            define_style("alert.h3", "alert", s =>{               
                s.fontSize = 15;
            });
            define_style("alert.h2", "alert", s =>{               
                s.fontSize = 20;
                s.fontStyle = FontStyle.Bold;
            });
            define_style("alert.h1", "alert.h2", s =>{               
                s.fontSize = 30;
            });

            define_style("modal.title", base_skin.label, s =>{
                s.fontStyle = FontStyle.Bold;
                s.fontSize = 18;
                s.alignment = TextAnchor.MiddleCenter;
                s.padding.top = 10;

            });


            define_style("hyperlink", base_skin.button, s =>{
                s.normal.background = base_skin.label.normal.background;
                s.hover.background = make_texture(2,2, Color.clear);
                s.active.background = make_texture(2,2, Color.clear);
                s.focused.background = make_texture(2,2, Color.clear);


                s.fontStyle = FontStyle.Normal;
                s.normal.textColor = new Color(0.4f, 0.5f, 0.9f, 1); //roughly KerbalX Blue - #6E91EB
                s.hover.textColor = Color.blue;
            });
            define_style("hyperlink.bold", "hyperlink", s =>{
                s.fontStyle = FontStyle.Bold;
            });
            define_style("hyperlink.left", "hyperlink", s =>{
                s.alignment = TextAnchor.UpperLeft;
            });
            define_style("hyperlink.inline", "hyperlink", s =>{
                s.alignment = TextAnchor.UpperLeft;
                s.stretchWidth = false;
                s.padding = base_skin.label.padding;
                s.margin = base_skin.label.margin;                    
            });
            define_style("hyperlink.bold.compact", "hyperlink.bold", s =>{
                s.margin = new RectOffset(0,0,0,0);
            });
            define_style("hyperlink.update_url", "hyperlink.bold", s =>{
                s.fontSize = 20;
                s.wordWrap = true;
                s.margin = new RectOffset(0,0,0,0);
            });

            define_style("download_waiting", "hyperlink", s =>{
                s.normal.textColor = Color.green;
                s.hover.textColor = new Color(0.4f, 0.5f, 0.9f, 1); //roughly KerbalX Blue - #6E91EB
                s.fontStyle = FontStyle.Bold;
                s.padding = new RectOffset(0,0,0,0);
                s.margin = new RectOffset(0,20,0,0);
                s.alignment = TextAnchor.LowerCenter;
                s.fixedHeight = 20;
            });

            define_style("transfer_progres.text", base_skin.label, s =>{
                s.normal.textColor = new Color(0.4f, 0.5f, 0.9f, 1);
                s.fontSize = 20;
                s.fontStyle = FontStyle.Bold;
            });


            define_style("button", base_skin.button, s =>{
            });
            define_style("button.down", base_skin.button, s =>{                
                s.normal.background = s.active.background;
                s.hover.background = s.focused.background;
                s.normal.textColor = Color.green;
            });
            define_style("button.large", base_skin.button, s =>{
                s.fontSize = 20;
            });
            define_style("button.vlarge", base_skin.button, s =>{
                s.fontSize = 30;
            });
            define_style("button.small", base_skin.button, s =>{
                s.fontSize = 12;
            });
            define_style("button.tight", base_skin.button, s =>{
                s.margin.left = 0;
                s.margin.right = 0;
            });
            define_style("button.tight.right_margin", "button.tight", s =>{                
                s.margin.right = base_skin.button.margin.right;
            });
            define_style("button.text", base_skin.label, s =>{
                s.normal.textColor = base_skin.button.normal.textColor;
                s.fontStyle = FontStyle.Bold;
                s.margin.left = 10;
                s.margin.top = 0;
                s.margin.bottom = 0;
                s.padding = new RectOffset(0,0,0,0);
            });
            define_style("button.text.large", "button.text", s =>{
                s.fontSize = 25;
                s.fontStyle = FontStyle.Bold;
            });

            define_style("button.delete", base_skin.button, s =>{
                s.hover.textColor = Color.red;
                s.normal.textColor = new Color(0.4f,0.2f,0.1f,1);
            });

            define_style("button.load", "button.vlarge", s =>{                
                s.normal.textColor = new Color(0.2f,0.3f,0.3f,1);
                s.hover.textColor = new Color(0.6f,0.9f,0.3f,1);
                s.margin = new RectOffset(0,0,15,0);
            });
            define_style("button.close", "button.vlarge", s =>{                
                s.normal.textColor = new Color(0.4f,0.2f,0.1f,1);
                s.hover.textColor = new Color(0.8f,0.4f,0.2f,1);
                s.margin = new RectOffset(0,8,15,0);
            });
            define_style("button.close.top", "button.close", s =>{
                s.margin = base_skin.button.margin;
                s.fontSize = base_skin.button.fontSize;
                s.fixedHeight = 30;
            });

            define_style("button.inline_load", "button.load", s =>{
                s.margin = base_skin.button.margin;
                s.margin.right = 0;
            });
            define_style("button.inline_update", "button.vlarge", s =>{
                s.margin.left = 0;
            });


            define_style("button.continue_with_save", "button.large", s =>{
                s.normal.textColor = new Color(0.2f,0.3f,0.3f,1);
                s.hover.textColor = new Color(0.6f,0.9f,0.3f,1);
            });
            define_style("button.continue_no_save", "button.large", s =>{
                s.normal.textColor = new Color(0.4f,0.2f,0.1f,1);
                s.hover.textColor = new Color(0.8f,0.4f,0.2f,1);
            });
            define_style("button.cancel_load", "button.large", s =>{
                s.normal.textColor = new Color(0.2f, 0.3f, 0.5f, 1);
                s.hover.textColor = new Color(0.1f, 0.4f, 0.9f, 1);
            });


            define_style("image_selector.item", base_skin.button, s =>{
                s.normal.background = base_skin.label.normal.background;
                s.hover.background = textures["pic_highlight"];
                s.active.background = base_skin.label.normal.background;
                s.fontSize = 14;
                s.alignment = TextAnchor.MiddleLeft;
            });
            define_style("image_selector.item.selected", "image_selector.item", s =>{
                s.normal.background = textures["green_background"];
                s.hover.background = textures["light_green_background"];
            });
            define_style("image_selector.remove_item", "hyperlink", s =>{                
                s.normal.textColor = new Color(0.75f,0.25f,0.0f,1);
                s.hover.textColor = Color.red;

                s.fontSize = 12;
                s.margin = new RectOffset(0,0,0,0);
                s.padding = new RectOffset(0,0,0,0);
                s.fixedWidth = 100f;
                s.alignment = TextAnchor.UpperRight;
            });

 
            define_style("button.login", base_skin.button, s =>{
                s.fontSize = 15;
                s.fontStyle = FontStyle.Bold;
                s.padding = new RectOffset(0, 0, 3, 3);
            });
            define_style("button.login.toggle", base_skin.button, s =>{
                s.fixedWidth = 20f;
                s.fixedHeight = 100f;
                s.margin = new RectOffset(0,0,0,0);
            });

            define_style("login.container", base_skin.window, s =>{
                s.margin = new RectOffset(0,0,0,0);
                s.padding = new RectOffset(0,0,0,0);
//                s.normal.background = textures["grey_background"];
            });
            define_style("login.window", base_skin.box, s =>{
                s.margin = new RectOffset(0,0,0,0);
                s.padding = new RectOffset(0,0,0,0);
                s.border = new RectOffset(0,0,0,0);
                s.normal.background = textures["clear_background"];
            });


            define_style("login.logging_in", base_skin.box, s =>{
                s.normal.background = textures["logging_in"];
                s.margin = new RectOffset(4,5,0,8);
                s.fixedWidth = 10f;
                s.fixedHeight = 10f;
            });
            define_style("login.logged_in", "login.logging_in", s =>{
                s.normal.background = textures["logged_in"];
            });
            define_style("login.logged_out", "login.logging_in", s =>{
                s.normal.background = textures["logged_out"];
            });



            define_style("craft_type_sel", base_skin.button, s =>{
                s.fontSize = 20;
            });
            define_style("craft_type_sel.active", "craft_type_sel", s =>{
                s.normal.background = s.active.background;
                s.hover.background = s.focused.background;
                s.normal.textColor = Color.green;
            });

            define_style("craft.list_container", base_skin.scrollView, s =>{
                s.padding = new RectOffset(5, 5, 0, 0);    
            });            
            define_style("craft.list_item", base_skin.button, s =>{
                s.padding = new RectOffset(0, 0, 0, 0);
                s.margin = new RectOffset(0, 0, 5, 5);
                s.normal.background = textures["dark_background"];
                s.hover.background = textures["blue_background"];
            });
            define_style("craft.list_item.selected", "craft.list_item", s =>{
                s.normal.background = textures["light_blue_background"];
                s.hover.background = textures["lighter_blue_background"];
            });
            define_style("craft.list_item.hover", "craft.list_item", s =>{
                s.normal.background = textures["blue_background"];
            });

            define_style("craft.name", base_skin.label, s =>{
                s.fontStyle = FontStyle.Bold;
                s.fontSize = 20;
                s.normal.textColor = Color.yellow;
                s.padding = new RectOffset(0, 0, 0, 0);
                s.margin.top = 5;
                s.margin.bottom = 0;
            });
            define_style("craft.alt_name", "small", s =>{
                s.normal.textColor = Color.yellow;
                s.margin.top = 8;
            });
            define_style("craft.info", base_skin.label, s =>{
                s.fontSize = 15;
                s.normal.textColor = new Color(244, 244, 244, 1);
                s.padding = new RectOffset(0, 0, 0, 0);
            });
            define_style("craft.cost", "craft.info", s =>{
                s.fontStyle = FontStyle.Bold;
                s.normal.textColor = Color.green;
            });
            define_style("craft.tags", "craft.info", s =>{
                s.fontSize = 12;
                s.margin.top = 0;
            });
            define_style("craft.locked_parts", base_skin.label, s =>{
                s.normal.textColor = Color.yellow;
            });
            define_style("craft.missing_parts", base_skin.label, s =>{
                s.normal.textColor = Color.red;
            });

            define_style("side_panel.scroll", "craft.list_container", s =>{
            });
            define_style("side_panel.scroll.tags", "side_panel.scroll", s =>{
                s.normal.background = base_skin.label.normal.background;
                s.margin = new RectOffset(0,0,0,0);
            });

            define_style("tags.list_outer", base_skin.box, s =>{
                s.padding = new RectOffset(0,0,0,0);
            });

            define_style("tag.toggle.light", base_skin.toggle, s =>{
                s.padding = new RectOffset(0, 0, 0, 0);
                s.margin = new RectOffset(0, 10, 10, 0);
                s.fixedWidth = 10f;
                s.fixedHeight = 10f;
            });

//            define_style("tag.toggle.light.selected", "tag.toggle.light", s =>{
//                s.normal = base_skin.toggle.active;
//            });

            define_style("tag.toggle.label", base_skin.button, s =>{
                s.normal.background = base_skin.label.normal.background;
                s.hover.background = base_skin.label.normal.background;
                s.active.background = base_skin.label.normal.background;
                s.margin = new RectOffset(0,0,0,0);
                s.padding = base_skin.label.padding;
                s.fontStyle = base_skin.label.fontStyle;
                s.normal.textColor = base_skin.label.normal.textColor;
                s.normal.textColor = base_skin.label.normal.textColor;
                s.alignment = base_skin.label.alignment;     
                s.wordWrap = true;
            });

            define_style("tag.toggle.label.autotag", "tag.toggle.label", s =>{
                s.fontStyle = FontStyle.Bold;
                s.normal.textColor = new Color(0.92f, 0.86f, 0.43f, 1.0f);
            });

            define_style("tag.toggle.count", "tag.toggle.label", s =>{
                s.alignment = TextAnchor.MiddleCenter;
            });

            define_style("tag.section", "tag.toggle.label", s =>{
                s.margin = new RectOffset(0,0,5,5);
                s.normal.background = textures["dark_background"];
                s.hover.background = textures["blue_background"];
            });
            define_style("tag.section.hover", "tag.section", s =>{
                s.normal.background = textures["blue_background"];
            });

            define_style("tag.section.selected", "tag.section", s =>{
                s.normal.background = textures["light_blue_background"];
            });
            define_style("tag.section.archived", "tag.section", s =>{
                s.normal.background = textures["red_background"];
            });

            define_style("tag.delete_button.x", "button.delete", s =>{
                s.fixedWidth = 20f;
                s.fixedHeight = 20f;
            });
            define_style("tag.edit_button", base_skin.button, s =>{
                s.fixedWidth = 20f;
                s.fixedHeight = 20f;
            });



            define_style("menu.container", base_skin.box, s =>{
                s.normal.background = base_skin.label.normal.background;
                s.border = new RectOffset(0,0,0,0);
            });

            define_style("menu.background", base_skin.label, s =>{
                s.normal.background = textures["dark_background"];
            });
            define_style("menu.scroll", base_skin.scrollView, s =>{
                s.padding = new RectOffset(0,0,0,0);
                s.margin = new RectOffset(0,0,0,0);
                s.border = new RectOffset(0,0,0,0);
                s.normal.background = base_skin.label.normal.background;
            });

            define_style("menu.item", base_skin.button, s =>{
                s.normal.background = (Texture2D)textures["dark_background"];
                s.hover.background = (Texture2D)textures["blue_background"];
                s.active.background = (Texture2D)textures["green_background"];
            });
            define_style("menu.item.small", "menu.item", s =>{
                s.fontSize = 12;
            });

            define_style("menu.item.selected", "menu.item", s =>{
                s.normal.textColor = Color.green;
            });
            define_style("menu.item.small.selected", "menu.item.small", s =>{
                s.normal.textColor = Color.green;
            });

            define_style("menu.item.special", "menu.item", s =>{
                s.normal.textColor = Color.blue;
//                s.hover.textColor = Color.green;
                s.normal.background = (Texture2D)textures["light_green_background"];
            });
            define_style("menu.item.small.special", "menu.item.special", s =>{
                s.fontSize = 12;
            });

            define_style("menu.item.tag_menu", "menu.item.small", s =>{
            });
            define_style("menu.item.tag_menu.special", "menu.item.tag_menu", s =>{
                s.normal.textColor = Color.red;
                s.hover.textColor = Color.red;
            });

            define_style("menu.item.craft", "menu.item", s =>{                
            });
            define_style("menu.item.craft.special", "menu.item.craft", s =>{
                s.normal.textColor = Color.red;
                s.hover.textColor = Color.red;
            });


            define_style("dialog.section", base_skin.label, s =>{
                s.normal.background = textures["dark_background"];
                s.margin = new RectOffset(0,0,0,8);
            });

            define_style("bottom.section", base_skin.label, s =>{
                s.margin.top = 0;
                s.margin.bottom = 20;
                s.padding.top = 0;
            });
            define_style("thin.section", base_skin.label, s =>{
                s.margin.top = 0;
                s.padding.top = 0;
                s.fixedHeight = 5;
            });

            define_style("progbox", base_skin.label, s =>{
                s.fixedWidth = 20f;
                s.fixedHeight = 20f;
                s.margin = new RectOffset(10,5,0,0);
                s.normal.background = textures["dark_background"];
            });

            define_style("progbox.active", "progbox", s =>{
                s.normal.background = textures["blue_background"];
            });

            define_style("progbox.compact", "progbox", s =>{
                s.fixedWidth = 10f;
                s.fixedHeight = 10f;
                s.margin = new RectOffset(5,2,8,0);
            });

            define_style("progbox.active.compact", "progbox.compact", s =>{
                s.normal.background = textures["blue_background"];
            });

            define_style("save_state_indicator", base_skin.label, s =>{
                s.fixedWidth = 5f;
                s.fixedHeight = 5f;
                s.margin = new RectOffset(2,2,2,2);
            });
            define_style("save_state_indicator.saved", "save_state_indicator", s =>{
                s.normal.background = textures["green_background"];
            });
            define_style("save_state_indicator.unsaved", "save_state_indicator", s =>{
                s.normal.background = textures["red_background"];
            });

            define_style("close_section", base_skin.button, s =>{
                s.margin = new RectOffset(0,0,0,0);
                s.padding = new RectOffset(0,0,0,0);
            });

            define_style("top_controls.section", new GUIStyle(), s =>{
                s.margin = new RectOffset(0,0,0,0);
                s.padding = new RectOffset(0,0,0,0);
            });

            define_style("stock_craft_toggle", new GUIStyle(), s =>{
                s.padding.top += 6;
            });
            define_style("stock_craft_toggle_button", "bold", s =>{
                s.padding.top += 4;
            });

            define_style("craft_select_label", base_skin.label, s =>{
                s.fontStyle = FontStyle.Bold;
                s.fontSize = 20;
            });

            define_style("settings.scroll", base_skin.scrollView, s =>{
                s.padding = new RectOffset(0, 0, 0, 0);    
            });

            //set the custom styles onto the base_skin;
            skin = Instantiate(base_skin);
            GUIStyle[] temp = new GUIStyle[custom_styles.Count];
            custom_styles.Values.CopyTo(temp, 0);                
            skin.customStyles = temp;
            skin.window.padding.bottom = 2;
        }
    }
}

