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
            if(this.skin == null){
                this.skin = CraftManager.skin;
            }
            GUI.skin = skin;
            base.OnGUI();
            GUI.skin = null;
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

    }
}

