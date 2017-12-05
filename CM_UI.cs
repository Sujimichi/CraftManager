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
    }
}

