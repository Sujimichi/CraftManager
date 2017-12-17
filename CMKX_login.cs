using System;
using System.Collections.Generic;

//using KatLib;

namespace CraftManager
{

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class CMKX_login : CMUI
    {

        private void Start(){
            if(CraftManager.kerbalx_integration_enabled){
            enable_request_handler();

                //try to load a token from file and if present authenticate it with KerbalX.  if token isn't present or token authentication fails then show login fields.
                if(KerbalXAPI.logged_out()){
                    CraftManager.load_and_authenticate_token();   
                }
            }
        }


    }

    public class KerbalX
    {

        public static void fetch_existing_craft(){
            CraftManager.status_info = "fetching craft info from KerbalX";
            KerbalXAPI.fetch_existing_craft(()=>{
                CraftManager.status_info = "";
                CraftManager.log("fetched existing craft");
                foreach(KeyValuePair<int, Dictionary<string, string>> pair in KerbalXAPI.user_craft){
                    CraftManager.log(String.Join(", ", new List<string>(pair.Value.Values).ToArray()));
                }
            });
        }

    }

}

