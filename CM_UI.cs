using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KatLib;
using KXAPI;

namespace CraftManager
{

    public class CMUI : DryUI
    {
        //This is needed (although kinda hacky) so set the stylesheet for the base window class.  All windows will be created as CMUI instances
        //which inherit functionality from DryUI (part of KatLib)
        protected override void OnGUI(){
            //Trigger the creation of custom Skin (copy of default skin with various custom styles added to it, see stylesheet.cs)
            if(CraftManager.skin == null){
                CraftManager.skin = new StyleSheet(HighLogic.Skin).skin;
                CraftManager.alt_skin = new StyleSheet(GUI.skin).skin; //works but isn't as clear.
            }
            if(this.skin == null){
                this.skin = CraftManager.skin;
            }
            GUI.skin = skin;
            base.OnGUI();
            GUI.skin = null;
        }


        //Callback method which is passed to GUILayout.Window in OnGUI.  Calls WindowContent and performs common window actions
        protected override void DrawWindow(int window_id){
            if(prevent_click_through){
                prevent_ui_click_through();
            }
            if(KerbalX.api.has_errors){
                KerbalX.api.show_errors();
            }

            if(gui_locked){
                GUI.enabled = false;
                GUI.color = new Color(1, 1, 1, 2); //This enables the GUI to be locked from input, but without changing it's appearance. 
            }
            WindowContent(window_id);   //oh hey, finally, actually drawing the window content. 
            GUI.enabled = true;
            GUI.color = Color.white;

            //add common footer elements for all windows if footer==true
            if(footer){
                FooterContent(window_id);
            }

            //enable draggable window if draggable == true.
            if(draggable){
                GUI.DragWindow();
            }
        }
    }
}

