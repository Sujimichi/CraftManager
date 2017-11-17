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

        public static void prepare(){

            GUISkin base_skin = HighLogic.Skin;
            //GUISkin base_skin = GUI.skin;

            
            //Textures
            Texture2D blue_background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            blue_background.SetPixel(0, 0, new Color(0.4f, 0.5f, 0.9f, 1));
            blue_background.wrapMode = TextureWrapMode.Repeat;
            blue_background.Apply();
            
            Texture2D dark_background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            dark_background.SetPixel(0, 0, new Color(0.12f, 0.12f, 0.12f, 0.7f));
            dark_background.Apply();
            
            Texture2D pic_highlight = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pic_highlight.SetPixel(0, 0, new Color(0.4f, 0.5f, 0.9f, 1));
            pic_highlight.Apply();
            
            Texture2D green_background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            green_background.SetPixel(0, 0, new Color(0.2f, 0.6f, 0.2f, 1));
            green_background.Apply();
            
            Texture2D light_green_background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            light_green_background.SetPixel(0, 0, new Color(0.3f, 0.5f, 0.3f, 1));
            light_green_background.Apply();
            
            //Label Styles
            GUIStyle h1 = new GUIStyle(base_skin.label);
            h1.fontStyle = FontStyle.Bold;
            h1.fontSize = 30;
            h1.name = "h1";
            
            GUIStyle h2 = new GUIStyle(h1);
            h2.fontSize = 20;
            h2.name = "h2";
            
            GUIStyle h3 = new GUIStyle(h1);
            h3.fontSize = 15;
            h3.name = "h3";
            
            GUIStyle h2_centered = new GUIStyle(h1);
            h2_centered.fontSize = 20;
            h2_centered.alignment = TextAnchor.LowerCenter;
            h2_centered.name = "h2.centered";


            GUIStyle craft_name = new GUIStyle(base_skin.label);
            craft_name.fontStyle = FontStyle.Bold;
            craft_name.fontSize = 20;
            craft_name.normal.textColor = Color.yellow;
            craft_name.padding = new RectOffset(0, 0, 0, 0);
            craft_name.name = "craft.name";

            GUIStyle craft_info = new GUIStyle(base_skin.label);
            craft_info.fontSize = 15;
            craft_info.normal.textColor = Color.black;
            craft_info.padding = new RectOffset(0, 0, 0, 0);
            craft_info.name = "craft.info";

            GUIStyle craft_list_item = new GUIStyle(base_skin.button);
            craft_list_item.padding = new RectOffset(0, 0, 0, 0);
            craft_list_item.margin = new RectOffset(0, 0, 5, 5);
            craft_list_item.name = "craft.list_item";

            GUIStyle craft_list_container = new GUIStyle(base_skin.scrollView);
            craft_list_container.padding = new RectOffset(5, 5, 0, 0);
            craft_list_container.name = "craft.list_container";




            GUIStyle hyperlink = new GUIStyle(base_skin.label);
            hyperlink.normal.textColor = new Color(0.4f, 0.5f, 0.9f, 1); //roughly KerbalX Blue - #6E91EB
            hyperlink.hover.textColor = Color.red; //can't seem to make this work
            hyperlink.name = "hyperlink";
            
            GUIStyle hyperlink_h2 = new GUIStyle(hyperlink);
            hyperlink_h2.fontSize = 20;
            hyperlink_h2.fontStyle = FontStyle.Bold;
            hyperlink_h2.alignment = TextAnchor.UpperCenter;
            hyperlink_h2.name = "hyperlink.h2";
            
            GUIStyle hyperlink_h3 = new GUIStyle(hyperlink);
            hyperlink_h3.fontSize = 15;
            hyperlink_h3.name = "hyperlink.h3";
            
            GUIStyle hyperlink_footer = new GUIStyle(hyperlink);
            hyperlink_footer.alignment = TextAnchor.LowerLeft;
            hyperlink_footer.name = "hyperlink.footer";
            
            GUIStyle remove_link = new GUIStyle(base_skin.label);
            remove_link.name = "remove_link";
            remove_link.padding = new RectOffset(0, 0, 0, 0);
            remove_link.margin = new RectOffset(2, 0, 0, 0);
            remove_link.normal.textColor = Color.red;
            remove_link.alignment = TextAnchor.LowerCenter;
            remove_link.fontSize = 15;
            
            GUIStyle alert = new GUIStyle(base_skin.label);
            alert.normal.textColor = Color.red;
            alert.name = "alert";
            
            GUIStyle alert_h2 = new GUIStyle(alert);
            alert_h2.name = "alert.h2";
            alert_h2.fontSize = 20;
            
            GUIStyle small = new GUIStyle(base_skin.label);
            small.name = "small";
            small.fontSize = 12;
            
            GUIStyle centered = new GUIStyle(base_skin.label);
            centered.name = "centered";
            centered.alignment = TextAnchor.UpperCenter;
            
            GUIStyle right_align = new GUIStyle(base_skin.label);
            right_align.name = "align.right";
            right_align.alignment = TextAnchor.UpperRight;
            
            
            GUIStyle no_style = new GUIStyle(base_skin.label);
            no_style.name = "no_style";
            no_style.margin = new RectOffset(0, 0, 0, 0);
            no_style.padding = new RectOffset(0, 0, 0, 0);
            
            GUIStyle pic_link = new GUIStyle(base_skin.label);
            pic_link.name = "pic.link";
            pic_link.padding = new RectOffset(5, 5, 5, 5);
            pic_link.margin = new RectOffset(0, 0, 0, 0);
            
            GUIStyle pic_hover = new GUIStyle(pic_link);
            pic_hover.name = "pic.hover";
            pic_hover.normal.textColor = Color.black;
            pic_hover.normal.background = blue_background;
            
            GUIStyle pic_selected = new GUIStyle(pic_link);
            pic_selected.name = "pic.selected";
            pic_selected.normal.textColor = Color.black;
            pic_selected.normal.background = green_background;
            
            GUIStyle pic_selected_highlight = new GUIStyle(pic_link);
            pic_selected_highlight.name = "pic.selected.highlighted";
            pic_selected_highlight.normal.textColor = Color.black;
            pic_selected_highlight.normal.background = light_green_background;
            


            //Button Styles
            GUIStyle login_button = new GUIStyle(base_skin.button);
            login_button.name = "button.login";
            login_button.fontSize = 15;
            login_button.fontStyle = FontStyle.Bold;
            login_button.padding = new RectOffset(0, 0, 3, 3);
            
            GUIStyle upload_button = new GUIStyle(base_skin.button);
            upload_button.name = "button.upload";
            upload_button.fontSize = 20;
            upload_button.fontStyle = FontStyle.Bold;
            upload_button.padding = new RectOffset(3, 3, 10, 10);
            upload_button.margin = new RectOffset(20, 20, 20, 5);
            
            GUIStyle large_button = new GUIStyle(base_skin.button);
            large_button.name = "button.large";
            large_button.fontSize = 15;
            large_button.padding = new RectOffset(3, 3, 10, 10);
            
            GUIStyle large_button_bold = new GUIStyle(large_button);
            large_button_bold.name = "button.large.bold";
            large_button_bold.fontStyle = FontStyle.Bold;
            
            GUIStyle wrapped_button = new GUIStyle(base_skin.button);
            wrapped_button.name = "button.wrapped";
            wrapped_button.wordWrap = true;
            
            GUIStyle bold_button = new GUIStyle(base_skin.button);
            bold_button.name = "button.bold";
            bold_button.fontStyle = FontStyle.Bold;
            bold_button.padding = new RectOffset(0, 0, 3, 3);
            bold_button.fontSize = 15;
            
            
            
            //Background (Box) Styles
            GUIStyle blue_box = new GUIStyle(base_skin.box);
            blue_box.normal.background = blue_background;
            blue_box.border = new RectOffset(3, 3, 3, 3);
            blue_box.name = "box.blue";
            
            GUIStyle dark_back = new GUIStyle();
            dark_back.name = "background.dark";
            dark_back.normal.background = dark_background;
            
            GUIStyle dark_back_offset = new GUIStyle(dark_back);
            dark_back_offset.name = "background.dark.margin";
            dark_back_offset.margin = new RectOffset(0, 0, 5, 0);
            
            
            //Combobox specific
            GUIStyle combo_field = new GUIStyle(base_skin.textField);
            combo_field.margin = new RectOffset(0, 0, 0, 0);
            combo_field.name = "combobox.filter_field";
            
            GUIStyle combo_bttn = new GUIStyle(base_skin.button);
            combo_bttn.margin.top = 0;
            combo_bttn.name = "combobox.bttn";
            
            GUIStyle combo_option = new GUIStyle(base_skin.label);
            combo_option.margin = new RectOffset(0, 0, 0, 0);
            combo_option.padding = new RectOffset(3, 3, 1, 1);
            combo_option.name = "combobox.option";
            
            GUIStyle combo_option_hover = new GUIStyle(combo_option);
            combo_option_hover.normal.background = blue_background;
            combo_option_hover.normal.textColor = Color.black;
            combo_option_hover.name = "combobox.option.hover";
            
            
            //DryUI.skin = Instantiate(GUI.skin);
            DryUI.skin = Instantiate(base_skin);
            
            DryUI.skin.customStyles = new GUIStyle[] { 
                h1, h2, h3, h2_centered, hyperlink, hyperlink_h2, hyperlink_h3, hyperlink_footer, remove_link, alert, alert_h2, small, centered, right_align, 
                pic_link, pic_hover, pic_selected, pic_selected_highlight, dark_back, dark_back_offset, blue_box, no_style, 
                login_button, upload_button, large_button, large_button_bold, wrapped_button, bold_button, 
                combo_field, combo_bttn, combo_option, combo_option_hover,
                craft_name, craft_info, craft_list_item, craft_list_container
            };
            
            DryUI.skin.window.padding.bottom = 2;


        }


    }

}

