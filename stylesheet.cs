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

//        public static Dictionary<string, Texture> assets = new Dictionary<string, Texture>() { 
//            { "logo_small",               GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "KXlogo_small"), false) },     //166x30 
//            { "logo large",               GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "KXlogo"), false) },           //664x120 
//            { "image_placeholder",        GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "image_placeholder"), false) }, 
//            { "upload_toolbar_btn",       GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "button_upload"), false) }, 
//            { "upload_toolbar_btn_hover", GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "button_upload_hover"), false) }, 
//            { "dnload_toolbar_btn",       GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "button_download"), false) }, 
//            { "dnload_toolbar_btn_hover", GameDatabase.Instance.GetTexture(Paths.joined("KerbalX", "Assets", "button_download_hover"), false) } 
//        };

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

        public StyleSheet(GUISkin base_skin){

            set_texture("blue_background", new Color(0.4f, 0.5f, 0.9f, 1), TextureWrapMode.Repeat);
            set_texture("dark_background", new Color(0.12f, 0.12f, 0.12f, 0.7f));
            set_texture("pic_highlight", new Color(0.4f, 0.5f, 0.9f, 1));
            set_texture("green_background", new Color(0.2f, 0.6f, 0.2f, 1));
            set_texture("light_green_background", new Color(0.3f, 0.5f, 0.3f, 1));


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
            define_style("h2.centered", "h2", s =>{
                s.alignment = TextAnchor.LowerCenter;
            });
            define_style("small", base_skin.label, s =>{
                s.fontSize = 12;
            });

            define_style("hyperlink", base_skin.label, s =>{
                s.normal.textColor = new Color(0.4f, 0.5f, 0.9f, 1); //roughly KerbalX Blue - #6E91EB
                s.hover.textColor = Color.red; //can't seem to make this work            
            });
            define_style("hyperlink.h2", "hyperlink", s =>{
                s.fontSize = 20;
                s.fontStyle = FontStyle.Bold;
                s.alignment = TextAnchor.UpperCenter;
            });
            define_style("hyperlink.h3", "hyperlink", s =>{
                s.fontSize = 15;
            });
            define_style("hyperlink.footer", "hyperlink", s =>{
                s.alignment = TextAnchor.LowerLeft;
            });


            define_style("craft_type_sel", base_skin.button, s =>{
                s.fontSize = 20;
            });
            define_style("craft_type_sel.active", "craft_type_sel", s =>{
                s.normal.background = s.active.background;
                s.hover.background = s.focused.background;
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
                s.margin.top = 0;
            });

            define_style("craft.info", base_skin.label, s =>{
                s.fontSize = 15;
                s.normal.textColor = new Color(244, 244, 244, 1);
                s.padding = new RectOffset(0, 0, 0, 0);
            });
            define_style("craft.cost", "craft.info", s =>{
                s.fontStyle = FontStyle.Bold;
                s.normal.textColor = Color.green;
//                s.alignment = TextAnchor.MiddleRight;
            });

            define_style("craft.list_item", base_skin.button, s =>{
                s.padding = new RectOffset(0, 0, 0, 0);
                s.margin = new RectOffset(0, 0, 5, 5);
            });

            define_style("craft.list_item.selected", "craft.list_item", s =>{
                s.normal.background = s.active.background;
                s.hover.background = s.focused.background;
            });

            define_style("craft.list_container", base_skin.scrollView, s =>{
                s.padding = new RectOffset(5, 5, 0, 0);    
            });





            define_style("pic.hover", base_skin.label, s =>{
                s.normal.textColor = Color.black;
                s.normal.background = textures["blue_background"];
            });

            //set the custom styles onto the base_skin;
            skin = Instantiate(base_skin);
            GUIStyle[] temp = new GUIStyle[custom_styles.Count];
            custom_styles.Values.CopyTo(temp, 0);                
            skin.customStyles = temp;

            skin.window.padding.bottom = 2;
        }




//        public static void prepare(){
//
//            GUISkin base_skin = HighLogic.Skin;
//            //GUISkin base_skin = GUI.skin;
//            
//            GUIStyle remove_link = new GUIStyle(base_skin.label);
//            remove_link.name = "remove_link";
//            remove_link.padding = new RectOffset(0, 0, 0, 0);
//            remove_link.margin = new RectOffset(2, 0, 0, 0);
//            remove_link.normal.textColor = Color.red;
//            remove_link.alignment = TextAnchor.LowerCenter;
//            remove_link.fontSize = 15;
//            
//            GUIStyle alert = new GUIStyle(base_skin.label);
//            alert.normal.textColor = Color.red;
//            alert.name = "alert";
//            
//            GUIStyle alert_h2 = new GUIStyle(alert);
//            alert_h2.name = "alert.h2";
//            alert_h2.fontSize = 20;
//            

//            
//            GUIStyle centered = new GUIStyle(base_skin.label);
//            centered.name = "centered";
//            centered.alignment = TextAnchor.UpperCenter;
//            
//            GUIStyle right_align = new GUIStyle(base_skin.label);
//            right_align.name = "align.right";
//            right_align.alignment = TextAnchor.UpperRight;
//            
//            
//            GUIStyle no_style = new GUIStyle(base_skin.label);
//            no_style.name = "no_style";
//            no_style.margin = new RectOffset(0, 0, 0, 0);
//            no_style.padding = new RectOffset(0, 0, 0, 0);
//            
//            GUIStyle pic_link = new GUIStyle(base_skin.label);
//            pic_link.name = "pic.link";
//            pic_link.padding = new RectOffset(5, 5, 5, 5);
//            pic_link.margin = new RectOffset(0, 0, 0, 0);
//            
//            GUIStyle pic_hover = new GUIStyle(pic_link);
//            pic_hover.name = "pic.hover";
//            pic_hover.normal.textColor = Color.black;
//            pic_hover.normal.background = blue_background;
//            
//            GUIStyle pic_selected = new GUIStyle(pic_link);
//            pic_selected.name = "pic.selected";
//            pic_selected.normal.textColor = Color.black;
//            pic_selected.normal.background = green_background;
//            
//            GUIStyle pic_selected_highlight = new GUIStyle(pic_link);
//            pic_selected_highlight.name = "pic.selected.highlighted";
//            pic_selected_highlight.normal.textColor = Color.black;
//            pic_selected_highlight.normal.background = light_green_background;
//            
//
//
//            //Button Styles
//            GUIStyle login_button = new GUIStyle(base_skin.button);
//            login_button.name = "button.login";
//            login_button.fontSize = 15;
//            login_button.fontStyle = FontStyle.Bold;
//            login_button.padding = new RectOffset(0, 0, 3, 3);
//            
//            GUIStyle upload_button = new GUIStyle(base_skin.button);
//            upload_button.name = "button.upload";
//            upload_button.fontSize = 20;
//            upload_button.fontStyle = FontStyle.Bold;
//            upload_button.padding = new RectOffset(3, 3, 10, 10);
//            upload_button.margin = new RectOffset(20, 20, 20, 5);
//            
//            GUIStyle large_button = new GUIStyle(base_skin.button);
//            large_button.name = "button.large";
//            large_button.fontSize = 15;
//            large_button.padding = new RectOffset(3, 3, 10, 10);
//            
//            GUIStyle large_button_bold = new GUIStyle(large_button);
//            large_button_bold.name = "button.large.bold";
//            large_button_bold.fontStyle = FontStyle.Bold;
//            
//            GUIStyle wrapped_button = new GUIStyle(base_skin.button);
//            wrapped_button.name = "button.wrapped";
//            wrapped_button.wordWrap = true;
//            
//            GUIStyle bold_button = new GUIStyle(base_skin.button);
//            bold_button.name = "button.bold";
//            bold_button.fontStyle = FontStyle.Bold;
//            bold_button.padding = new RectOffset(0, 0, 3, 3);
//            bold_button.fontSize = 15;
//            
//            
//            
//            //Background (Box) Styles
//            GUIStyle blue_box = new GUIStyle(base_skin.box);
//            blue_box.normal.background = blue_background;
//            blue_box.border = new RectOffset(3, 3, 3, 3);
//            blue_box.name = "box.blue";
//            
//            GUIStyle dark_back = new GUIStyle();
//            dark_back.name = "background.dark";
//            dark_back.normal.background = dark_background;
//            
//            GUIStyle dark_back_offset = new GUIStyle(dark_back);
//            dark_back_offset.name = "background.dark.margin";
//            dark_back_offset.margin = new RectOffset(0, 0, 5, 0);
//            
//            
//            //Combobox specific
//            GUIStyle combo_field = new GUIStyle(base_skin.textField);
//            combo_field.margin = new RectOffset(0, 0, 0, 0);
//            combo_field.name = "combobox.filter_field";
//            
//            GUIStyle combo_bttn = new GUIStyle(base_skin.button);
//            combo_bttn.margin.top = 0;
//            combo_bttn.name = "combobox.bttn";
//            
//            GUIStyle combo_option = new GUIStyle(base_skin.label);
//            combo_option.margin = new RectOffset(0, 0, 0, 0);
//            combo_option.padding = new RectOffset(3, 3, 1, 1);
//            combo_option.name = "combobox.option";
//            
//            GUIStyle combo_option_hover = new GUIStyle(combo_option);
//            combo_option_hover.normal.background = blue_background;
//            combo_option_hover.normal.textColor = Color.black;
//            combo_option_hover.name = "combobox.option.hover";
//            
//            
//            //DryUI.skin = Instantiate(GUI.skin);
//            DryUI.skin = Instantiate(base_skin);
//            
//            DryUI.skin.customStyles = new GUIStyle[] { 
//                h1, h2, h3, h2_centered, hyperlink, hyperlink_h2, hyperlink_h3, hyperlink_footer, remove_link, alert, alert_h2, small, centered, right_align, 
//                pic_link, pic_hover, pic_selected, pic_selected_highlight, dark_back, dark_back_offset, blue_box, no_style, 
//                login_button, upload_button, large_button, large_button_bold, wrapped_button, bold_button, 
//                combo_field, combo_bttn, combo_option, combo_option_hover,
//                craft_name, craft_info, craft_list_item, craft_list_container
//            };
//            DryUI.skin.window.padding.bottom = 2;
//        }


    }

}

