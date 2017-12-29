using System;
using UnityEngine;
using KatLib;

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


            if(KerbalXAPI.failed_to_connect){
                
                ModalDialog dialog = show_modal_dialog(d =>{
                    label("Unable to Connect to KerbalX.com!", "alert.h1");
                    label("Check your net connection and that you can reach KerbalX in a browser", "alert.h2");
                    section(() =>{
                        button("try again", () =>{
                            RequestHandler.instance.try_again();
                            ModalDialog.close();
                        });                    
                        button("cancel", () =>{
                            KerbalXAPI.failed_to_connect = false;
                            ModalDialog.close();
                        });
                    });
                });
                dialog.dialog_pos.width = 600;
                dialog.dialog_pos.x = Screen.width / 2 - (dialog.dialog_pos.width / 2);
                dialog.dialog_pos.y = Screen.height * 0.3f;
                dialog.window_title = "CraftManager Error";
            }

            if(true){
                
                if(gui_locked){
                    GUI.enabled = false;
                    GUI.color = new Color(1, 1, 1, 2); //This enables the GUI to be locked from input, but without changing it's appearance. 
                }
                WindowContent(window_id);   //oh hey, finally, actually drawing the window content. 
                GUI.enabled = true;
                GUI.color = Color.white;
            }


            //add common footer elements for all windows if footer==true
            if(footer){
                FooterContent(window_id);
            }

            //enable draggable window if draggable == true.
            if(draggable){
                GUI.DragWindow();
            }
        }

        //Essential for any window which needs to make web requests.  If a window is going to trigger web requests then it needs to call this method on its Start() method
        //The RequestHandler handles sending requests asynchronously (so delays in response time don't lag the interface).  In order to do that it uses Coroutines 
        //which are inherited from MonoBehaviour (and therefore can't be triggered by the static methods in KerbalXAPI).
        protected void enable_request_handler(){
            if(RequestHandler.instance == null){
                KerbalXAPI.log("starting web request handler");
                RequestHandler request_handler = gameObject.AddOrGetComponent<RequestHandler>();
                RequestHandler.instance = request_handler;
            }
        }


        public void show_must_be_logged_in(AfterLoginAction callback){           
            login_dialog("You need to login to KerbalX to use this feature", callback);
        }

        public void login_dialog(string message = null, AfterLoginAction callback = null){
            if(CraftManager.login_ui != null){
                GameObject.Destroy(CraftManager.login_ui);
            }
            CMKX_login login_dialog = gameObject.AddOrGetComponent<CMKX_login>();
            login_dialog.modal_dialog = true;
            login_dialog.show_cancel = true;
            login_dialog.login_required_message = message;
            login_dialog.after_login_action = callback;
        }


    }
}

