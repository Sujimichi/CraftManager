using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using KatLib;
using SimpleJSON;

namespace CraftManager
{

    public class KerbalXUploadData
    {

        public CraftData craft = null;

        internal string craft_name = "";
        internal string hash_tags = "";
        internal string craft_type = "Ship";
        internal List<Image> images = new List<Image>();
        internal Dictionary<string, string> action_groups = new Dictionary<string, string>() { 
            { "1", "" }, { "2", "" }, { "3", "" }, { "4", "" }, { "5", "" }, { "6", "" }, { "7", "" }, { "8", "" }, { "9", "" }, { "0", "" }, 
            { "stage", "" }, { "gears", "" }, { "lights", "" }, { "RCS", "" }, { "SAS", "" }, { "brakes", "" }, { "abort", "" } 
        };

        //keeps hold of created instances of KerbalXUploadData so they can be reloaded 
        internal static Dictionary<string, KerbalXUploadData> upload_data_store = new Dictionary<string, KerbalXUploadData>();

        //returns a new instance of KerbalXUploadData for the given craft or returns an existing instance if one has already been created.
        public static KerbalXUploadData prepare_for(CraftData for_craft){
            KerbalXUploadData data;
            if(upload_data_store.ContainsKey(for_craft.path)){
                CraftManager.log("loading upload data for: " + for_craft.name);
                data = upload_data_store[for_craft.path];
            } else{
                CraftManager.log("creating upload data for: " + for_craft.name);
                data = new KerbalXUploadData(for_craft);
                upload_data_store.Add(for_craft.path, data);
            }
            return data;
        }

        public KerbalXUploadData(CraftData for_craft){
            craft = for_craft;
            craft_name = craft.name;

            List<string> craft_tags = new List<string>();
            foreach(string tag in craft.tag_names()){               
                craft_tags.Add(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(tag.Trim()).Replace(" ", ""));
            }
            hash_tags = String.Join(", ", craft_tags.ToArray());
        }

        public List<string> errors = new List<string>();


        //returns true/false if images contains the given image, but unlike images.Contains(image), this checks by image path rather than instance of image object
        public bool has_image(Image image){
            return images.FindAll(c => c.path == image.path).Count > 0;
        }

        public void toggle_image(Image image){
            if(images.FindAll(c => c.path == image.path).Count > 0){
                images.RemoveAll(c => c.path == image.path);
            }else{
                if(images.Count > 2){
                    errors.Clear();
                    errors.Add("You can only add 3 images");
                } else{
                    images.Add(image);
                }
            }
        }

        public bool is_valid{
            get{
                errors.Clear();
                craft_name.Trim();
                hash_tags.Trim();
                if(!System.IO.File.Exists(craft.path)){
                    errors.Add("unable to find craft file");
                    return false;
                }
                if(String.IsNullOrEmpty(craft_name)){
                    errors.Add("The craft's name can't be blank");
                }
                if(craft_name.Length <= 2){
                    errors.Add("The craft's name must be at least 3 characters");
                }
                if(images.Count == 0){
                    errors.Add("You need to add at least 1 picture");
                }
                if(!KerbalX.craft_styles.Contains(craft_type)){
                    errors.Add("The craft's 'type' is not valid");
                }
                return errors.Count == 0;                
            }
        }

        public void post(){
            if(is_valid){
                CraftManager.main_ui.show_transfer_indicator = true;
                CraftManager.main_ui.transfer_is_upload = true;
                WWWForm craft_data = new WWWForm();
                craft_data.AddField("craft_name", craft_name);
                craft_data.AddField("craft_style", craft_type);
                craft_data.AddField("craft_file", System.IO.File.ReadAllText(craft.path));
                craft_data.AddField("part_data", JSONX.toJSON(part_info()));
                craft_data.AddField("action_groups", JSONX.toJSON(action_groups));
                craft_data.AddField("hash_tags", hash_tags);

                int pic_count = 0;
                foreach(Image image in images){
                    craft_data.AddField("images[image_" + pic_count++ + "]", Convert.ToBase64String(image.read_as_jpg()));
//                    craft_data.AddField("image_urls[url_" + pic_count++ + "]", "https://i.imgur.com/nSUkIe0.jpg");
                }

                KerbalXAPI.upload_craft(craft_data, (resp, code) =>{
                    //                        var resp_data = JSON.Parse(resp);
                    if(code == 200){
                        CraftManager.log("craft uploaded OK");
                        KerbalXAPI.fetch_existing_craft(()=>{   //refresh remote craft info 
                            craft.matching_remote_ids = null;
                            CraftManager.main_ui.close_upload_interface();
                        });
                    } else{                           
                        CraftManager.log("craft upload failed");
                    }

                    //return UI to craft list mode and show upload complete dialog.
                    CraftManager.main_ui.show_transfer_indicator = false;
                    //                        CraftManager.main_ui.unlock_ui();
                    CraftManager.main_ui.upload_complete_dialog(code, resp);

                });

            }
        }

        public void put(){            
            CraftManager.log("Updating remote craft ID: " + craft.update_to_id.ToString());
            CraftManager.main_ui.show_transfer_indicator = true;
            CraftManager.main_ui.transfer_is_upload = false;

            WWWForm craft_data = new WWWForm();
            craft_data.AddField("craft_name", craft_name);
            craft_data.AddField("craft_file", System.IO.File.ReadAllText(craft.path));
            craft_data.AddField("part_data", JSONX.toJSON(part_info()));

            KerbalXAPI.update_craft(craft.update_to_id, craft_data, (resp, code) =>{
                if(code == 200){
                    CraftManager.log("craft updated OK");
                    KerbalXAPI.fetch_existing_craft(()=>{                        
                        //                        craft.matching_remote_ids = null;
                        CraftManager.main_ui.close_upload_interface();
                    });
                }else{
                    CraftManager.log("craft update failed");
                }
                CraftManager.main_ui.show_transfer_indicator = false;
                //TODO show update complete dialog, maybe? or just close and move on.

            });
        }


        private Dictionary<string, object> part_info(){
            Dictionary<string, object> part_data = new Dictionary<string, object>();

            foreach(string part_name in craft.part_name_list){
                if(!part_data.ContainsKey(part_name)){
                    AvailablePart av_part = CraftData.cache.part_data[part_name];
                    if(av_part != null){
                        Part part = av_part.partPrefab;
                        Dictionary<string, object> part_detail = new Dictionary<string, object>();
                        part_detail.Add("mod", part.partInfo.partUrl.Split('/')[0]);
                        part_detail.Add("mass", part.mass);
                        part_detail.Add("cost", part.partInfo.cost);
                        part_detail.Add("category", part.partInfo.category.ToString());
                        if(part.CrewCapacity > 0){
                            part_detail.Add("CrewCapacity", part.CrewCapacity);
                        }
                        part_detail.Add("TechRequired", part.partInfo.TechRequired);
                        Dictionary<string, object> part_resources = new Dictionary<string, object>();
                        foreach(PartResource r in part.Resources){
                            part_resources.Add(r.resourceName, r.maxAmount);
                        }
                        part_detail.Add("resources", part_resources);
                        part_data.Add(part.name, part_detail);

                    }
                }
            }
            return part_data;
        }
    }

}

