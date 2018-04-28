using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

using KatLib;

namespace CraftManager
{

    public class CraftData
    {
        //**Class Methods/Variables**//

        public static List<CraftData> all_craft = new List<CraftData>();  //will hold all the craft loaded from disk
        public static List<CraftData> filtered  = new List<CraftData>();  //will hold the results of search/filtering to be shown in the UI.
        public static CraftDataCache cache = null;

        public static int file_load_count = 0;
        public static int cache_load_count= 0;

        public static bool loading_craft = false;
        public static int save_state = 0;
        public static bool craft_saved {
            get{
                return save_state <= 0;
            }
        }


        public static void load_craft_from_files(string save_dir = null){
            if(cache == null){
                cache = new CraftDataCache();                
            }
            file_load_count = 0;
            cache_load_count = 0;

            string[] craft_file_paths;
            if(save_dir == null){
                craft_file_paths = Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "saves"), "*.craft", SearchOption.AllDirectories);
            } else{
                craft_file_paths = Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "saves", save_dir), "*.craft", SearchOption.AllDirectories);
            }

            all_craft.Clear();
            foreach(string path in craft_file_paths){                
                new CraftData(path);
            }

            if(CraftManager.main_ui && !CraftManager.main_ui.exclude_stock_craft){
                load_stock_craft_from_files();
            }
            if(cache_load_count > 0){
                CraftManager.log("Loaded " + cache_load_count + " craft from cache");
            }
            if(file_load_count > 0){
                CraftManager.log("Loaded " + file_load_count + " craft from file");
            }
        }

        public static void load_stock_craft_from_files(){            
            string[] craft_file_paths = Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "Ships"), "*.craft", SearchOption.AllDirectories);
            foreach(string path in craft_file_paths){
                new CraftData(path, true);
            }
            if(CraftManager.main_ui){
                CraftManager.main_ui.stock_craft_loaded = true;
            };
        }


        public static void filter_craft(Dictionary<string, object> criteria){
            if(CraftData.cache != null){
                CraftData.cache.tag_craft_count_store.Clear();
            }
            filtered = all_craft;

            if((bool)criteria["exclude_stock"]){
                filtered = filtered.FindAll(craft => !craft.stock_craft);
            }
            if(criteria.ContainsKey("search")){
                filtered = filtered.FindAll(craft => craft.name.ToLower().Contains(((string)criteria["search"]).ToLower()));
            }
            if(criteria.ContainsKey("type")){
                Dictionary<string, bool> types = (Dictionary<string, bool>) criteria["type"];
                List<string> selected_types = new List<string>();
                foreach(KeyValuePair<string, bool> t in types){
                    if(t.Value){                        
                        selected_types.Add(t.Key=="Subassemblies" ? "Subassembly" : t.Key);
                    }
                }
                filtered = filtered.FindAll(craft => selected_types.Contains(craft.construction_type));
            }

            if(criteria.ContainsKey("archived_tags")){
                List<string> a_tags = (List<string>)criteria["archived_tags"];
                filtered = filtered.FindAll(craft =>{
                    bool sel = true;
                    foreach(string tag in craft.tags()){
                        if(a_tags.Contains(tag)){
                            sel = false;
                        }
                    }
                    return sel;
                });
            }

            if(criteria.ContainsKey("tags")){
                List<string> s_tags = (List<string>)criteria["tags"];
                string tag_filter_mode = (string)criteria["tag_filter_mode"];
                if(tag_filter_mode == "OR"){
                    filtered = filtered.FindAll(craft =>{
                        bool sel = false;
                        foreach(string tag in craft.tags()){
                            if(s_tags.Contains(tag)){
                                sel = true;
                            }
                        }
                        return sel;
                    });
                } else{
                    foreach(string tag in s_tags){
                        filtered = filtered.FindAll(craft => craft.tags().Contains(tag));
                    }
                }
            }
            if(criteria.ContainsKey("versions")){
                List<Version> vers = (List<Version>)criteria["versions"];
                filtered = filtered.FindAll(craft =>{                    
                    return vers.Contains(new Version(craft.ksp_version));
                });
            }
            if(criteria.ContainsKey("sort")){
                string sort_by = (string)criteria["sort"];
                filtered.Sort((x,y) => {
                    if(sort_by == "name"){
                        return x.name.CompareTo(y.name);
                    }else if(sort_by == "part_count"){
                        return y.part_count.CompareTo(x.part_count);
                    }else if(sort_by == "stage_count"){
                        return y.stage_count.CompareTo(x.stage_count);
                    }else if(sort_by == "mass"){
                        return y.mass_total.CompareTo(x.mass_total);
                    }else if(sort_by == "cost"){
                        return y.cost_total.CompareTo(x.cost_total);
                    }else if(sort_by == "crew_capacity"){
                        return y.crew_capacity.CompareTo(x.crew_capacity);
                    }else if(sort_by == "date_created"){
                        return y.create_time.CompareTo(x.create_time);
                    }else if(sort_by == "date_updated"){
                        return y.last_updated_time.CompareTo(x.last_updated_time);
                    }else{
                        return x.name.CompareTo(y.name);
                    }
                });
                if(criteria.ContainsKey("reverse_sort") && (bool)criteria["reverse_sort"]){
                    filtered.Reverse();
                }
            }

            //reset the list_height values on filtered craft so they are recalculated when the list is first drawn
            for(int i = 0; i < filtered.Count; i++){
                filtered[i].list_height = 0;
            }
        }
            
        public static void select_craft(CraftData craft){
            for(int i = 0; i < CraftData.all_craft.Count; i++){
                CraftData.all_craft[i].selected = false;
            }
            craft.selected = true;
        }

        public static void toggle_selected(CraftData craft){
            if(craft.selected){
                craft.selected = false;
            }else{
                CraftData.select_craft(craft);
            }
        }

        public static CraftData selected_craft { 
            get { 
                return filtered.Find(c => c.selected == true);
            } 
        }

        public static List<string> save_names{
            get{
                List<string> dirs = new List<string>();
                foreach(string dir in Directory.GetDirectories(Paths.joined(CraftManager.ksp_root, "saves"))){
                    string dir_name = dir.Replace(Paths.joined(CraftManager.ksp_root, "saves"), "").Replace("/", "").Replace("\\", "");
                    if(dir_name != "training" && dir_name != "scenarios"){
                        dirs.Add(dir_name);
                    }
                }
                return dirs;
            }

        }


        //**Instance Methods/Variables**//

        //craft attributes - attributes with getters will automatically be stored in the in memory cache and written do persistent store cache
        //if they also have a setter then they can be restored from the cache.
        public string name { get; set; }
        public string alt_name { get; set; }
        public string path { get; set; }
        public string checksum { get; set; }
        public string part_sig { get; set; }
        public string description { get; set; }
        public string construction_type { get; set; }
        public string ksp_version{ get; set; }
        public int stage_count { get; set; }
        public int part_count { get; set; }
        public int crew_capacity{ get; set; }
        public float cost_dry { get; set; }
        public float cost_fuel { get; set; }
        public float cost_total { get; set; }
        public float mass_dry { get; set; }
        public float mass_fuel { get; set; }
        public float mass_total { get; set; }
        public bool stock_craft { get; set; }
        public bool missing_parts { get; set; }
        public bool locked_parts { get; set; }  //This is an exception. locked_parts will be cached to the in-memory cache but not to the persistent cache.


        //part_name_list holds a unique list of the craft parts, but uses part_names as the getter and setter for it
        //this enables part_names to be cached and reloaded from the cache and the getter and setters handle conversion between ',' sep string and List<string>
        public List<string> part_name_list = new List<string>();
        public string part_names {
            get{
                return String.Join(", ", part_name_list.ToArray());
            }
            set{ 
                part_name_list.Clear();
                foreach(string v in value.Split(',')){
                    part_name_list.AddUnique(v.Trim());
                }                    
            }
        }


        //Attribues which are always set from craft file/path, not loaded from cache
        public Texture2D thumbnail;
        public string create_time;
        public string last_updated_time;
        public string save_dir{ 
            get { 
                //double nested ternary ahead, approach with caution. also, strangly written so it's not one massive line.
                //this returns the save dir name for craft, but enables stock craft to fake being in whichever save is currently being viewed.
                return stock_craft ? 
                    (CraftManager.main_ui.active_save_dir==CMBrowser.all_saves_ref ? HighLogic.SaveFolder : CraftManager.main_ui.active_save_dir) 
                    : 
                    path.Replace(Paths.joined(CraftManager.ksp_root, "saves", ""), "").Split('/')[0];
            }
        }

        //Attributes which are set during object's lifetime and not cached.
        public bool selected = false;
        public bool locked_parts_checked = false;
        public List<string> tag_name_cache = null;

        public string new_name = "";
        public float list_position = 0;
        public float list_height = 0;
        public bool draw = true;



        //Attributes specific to KerbalX Craft
        public int remote_id;
        public bool remote = false;
        public string author = "";
        public string url = "";
        public bool exists_locally = false;


        public List<int> matching_remote_ids = null;
        public bool on_kerbalx(){
            if(matching_remote_ids == null){
                matching_remote_ids = new List<int>();
                KerbalX.find_matching_remote_craft(this);
            }
            return matching_remote_ids.Count > 0;            
        }

        public KerbalXUploadData upload_data;



        //Initialize a new CraftData object. Takes a path to a .craft file and either populates it from attributes from the craft file
        //or loads information from the CraftDataCache. Main logic moved to initialize() so it can be call again (reinitialized) on existing object      
        public CraftData(string full_path, bool stock = false){
            initialize(full_path, stock);
            CraftData.all_craft.Add(this);
        }
            
        //Initialize a new CraftData object from remote (KerbalX). Remote craft are not cached.
        public CraftData(int id, string kx_url, string craft_name, string type, string version, int p_count, int stages, int crew, float c_cost, float c_mass, string created_at, string updated_at, string desc){    
            remote = true; stock_craft = false;
            name = craft_name; alt_name = craft_name; description = desc;
            remote_id = id; url = kx_url; construction_type = type; ksp_version = version;
            stage_count = stages; part_count = p_count; crew_capacity = crew; cost_total = c_cost; mass_total = c_mass;
            create_time = DateTime.Parse(created_at).ToUniversalTime().ToBinary().ToString();                
            last_updated_time = DateTime.Parse(updated_at).ToUniversalTime().ToBinary().ToString();                
            author = url.Split('/')[1];
            if(construction_type == "Subassembly"){
                path = Paths.joined(CraftManager.ksp_root, "saves", CraftManager.main_ui.current_save_dir, "Subassemblies", name + ".craft");
            } else{
                path = Paths.joined(CraftManager.ksp_root, "saves", CraftManager.main_ui.current_save_dir, "Ships", construction_type, name + ".craft");
            }
            exists_locally = File.Exists(path);

            part_sig = checksum = "";
            cost_dry = cost_fuel = mass_dry = mass_fuel = 0;
            missing_parts = locked_parts = false;           

            CraftData.all_craft.Add(this);
        }

        public void initialize(string full_path, bool stock = false){        
            path = full_path.Replace("\\", "/");
            checksum = Checksum.digest(File.ReadAllText(path));
            stock_craft = stock;
            locked_parts_checked = false;

            bool cache_after_load = false;

            //attempt to load craft data from the cache. If unable to fetch from cache then load 
            //craft data from the .craft file and cache the loaded info.
            if(!cache.try_fetch(this)){
                read_craft_info_from_file();
                check_locked_parts();
                part_sig = cache.installed_part_sig;
                cache_after_load = true;
            }
                
            if(!locked_parts_checked){
                check_locked_parts();
                cache_after_load = true;
            }

            if(cache_after_load){
                cache.write(this);
            }

            //set timestamp data from the craft file
            create_time = System.IO.File.GetCreationTime(path).ToUniversalTime().ToBinary().ToString();
//            create_time = System.IO.File.GetCreationTime(path).ToBinary().ToString();
            last_updated_time = System.IO.File.GetLastWriteTime(path).ToUniversalTime().ToBinary().ToString();

            //prepare the thumbnail texture
            load_thumbnail_image();
        }

        //return the path to the craft's thumbnail based on craft properties. Overload method provides means to generate a 
        //different thumbnail path without changing the crafts properties.
        public string thumbnail_path(){
            return thumbnail_path(stock_craft ? "stock" : save_dir, construction_type, name);
        }
        public string thumbnail_path(string save_folder, string construct_type, string craft_name){
            if(save_folder == "stock"){
                return Paths.joined(CraftManager.ksp_root, "Ships", "@thumbs", construct_type,  craft_name + ".png");
            } else{
                return Paths.joined(CraftManager.ksp_root, "thumbs", save_folder + "_" + construct_type + "_" + craft_name + ".png");
            }
        }

        public void load_thumbnail_image(){
            string thumbnail_path = this.thumbnail_path();
            if(File.Exists(thumbnail_path)){
                thumbnail = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                byte[] pic_data = File.ReadAllBytes(thumbnail_path);  //read image file
                thumbnail.LoadImage(pic_data);          
            } else{
                thumbnail = (Texture2D)StyleSheet.assets[construction_type + "_placeholder"];                
            }
        }

        //Parse .craft file and read info
        private void read_craft_info_from_file(){
            name = Path.GetFileNameWithoutExtension(path);
            CraftData.file_load_count += 1; //increment count of craft loaded from file

            ConfigNode data = ConfigNode.Load(path);
            ConfigNode[] parts = data.GetNodes();
            AvailablePart matched_part;

            alt_name = data.GetValue("ship");
            description = data.GetValue("description");
            construction_type = data.GetValue("type");
            ksp_version = data.GetValue("version");
            if(!(construction_type == "SPH" || construction_type == "VAB")){
                construction_type = "Subassembly";
            }
            part_count = parts.Length;
            stage_count = 0;
            missing_parts = false;

            cost_dry = 0;cost_fuel = 0;cost_total = 0;mass_dry = 0;mass_fuel = 0;mass_total = 0;
            crew_capacity = 0;

            //interim variables used to collect values from GetPartCostsAndMass (defined outside of loop as a garbage reduction measure)
            float dry_mass = 0;
            float fuel_mass = 0;
            float dry_cost = 0;
            float fuel_cost = 0;
            string stage;
            string part_name;

            foreach(ConfigNode part in parts){

                //Set the number of stages in the craft
                stage = part.GetValue("istg");
                if(!String.IsNullOrEmpty(stage)){
                    int stage_number = int.Parse(stage);
                    if(stage_number > stage_count){
                        stage_count = stage_number;
                    }
                }

                //locate part in game_parts and read part cost/mass information.
                part_name = get_part_name(part);
                part_name_list.AddUnique(part_name);
                matched_part = cache.fetch_part(part_name);
                if(matched_part != null){
                    ShipConstruction.GetPartCostsAndMass(part, matched_part, out dry_cost, out fuel_cost, out dry_mass, out fuel_mass);
                    if(matched_part.partConfig.HasValue("CrewCapacity")){
                        crew_capacity += int.Parse(matched_part.partConfig.GetValue("CrewCapacity"));
                    }
                    cost_dry += dry_cost;
                    cost_fuel += fuel_cost;
                    mass_dry += dry_mass;
                    mass_fuel += fuel_mass;
                } else{
                    missing_parts = true;
                }
            }

            stage_count += 1; //this might not be right
            cost_total = cost_dry + cost_fuel;
            mass_total = mass_dry + mass_fuel;
        }


        //get the part name from a PART config node.
        private string get_part_name(ConfigNode part){
            string part_name = part.GetValue("part");
            if(!String.IsNullOrEmpty(part_name)){
                part_name = part_name.Split('_')[0];
            } else{
                part_name = "";
            }
            return part_name;
        }

        public List<string> tags(){
            return Tags.for_craft(this);
        }

        public List<string> tag_names(){
            if(tag_name_cache == null){
                tag_name_cache = this.tags();
                tag_name_cache.Sort();
            }
            return tag_name_cache;
        }

        //Check to see if any of the crafts parts match the names of parts listed as locked (in the cache)
        //which parts are locked can change during game play, but can't change while in the editors.  So the value for locked_parts
        //is only cached in the in-memory cache (which is dropped between scene changes), so it will be rechecked when first loading
        //craft after entering the editor, and its value will be cached in the in-memory cache so it doesn't need rechecking until the
        //next scene change.
        public void check_locked_parts() {
            locked_parts = false;
            if(HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX){
                foreach(string p_name in part_name_list){
                    if(cache.locked_parts.Contains(p_name)){
                        locked_parts = true;
                    }
                }
            }
            locked_parts_checked = true;
        }


        //Rename the craft (both name in the craft file and the file itself). Does checks before attempting rename to ensure valid name.
        //the new name should be set on the craft object first;
        //craft.new_name = "I am Jeff";
        //craft.rename();
        //returns various strings depending on outcome, a "200" string means all went ok, yes it's an HTTP status code, I'm a web dev, deal with it.
        public string rename(){
            List<string> invalid = new List<string>();
            if(!String.IsNullOrEmpty(new_name) && new_name != name){
                foreach(char c in Path.GetInvalidFileNameChars()){                    
                    if(new_name.Contains(c.ToString())){
                        invalid.Add(c.ToString());
                    }
                }
                if(invalid.Count == 0){
                    string new_path = path.Replace(name + ".craft", new_name + ".craft");
                    if(File.Exists(new_path)){
                        return "Another craft already has this name";
                    }
                    FileInfo file = new FileInfo(path);
                    if(file.Exists){                                            
                        try{
                            file.MoveTo(new_path);
                        }
                        catch(Exception e){
                            return "Unable to rename file\n" + e.Message;
                        }
                        FileInfo thumbnail_file = new FileInfo(thumbnail_path());
                        List<string> tags = Tags.untag_craft(this); //remove old name from tags (returns any tag names it was in).
                        ConfigNode nodes = ConfigNode.Load(new_path);
                        nodes.SetValue("ship", new_name);
                        nodes.Save(new_path);
                        initialize(new_path, stock_craft);  //reprocess the craft file
                        Tags.tag_craft(this, tags); //add updated craft to the tags it was previously in.
                        if(thumbnail_file.Exists){
                            thumbnail_file.MoveTo(thumbnail_path());
                            load_thumbnail_image();
                        }
                        return "200";
                    } else{                    
                        return "error 404 - file not found";
                    }
                } else{
                    return "new name has invalid letters; " + String.Join(",", invalid.ToArray());
                }
            } else{
                if(String.IsNullOrEmpty(new_name)){
                    return "name can not be blank";                    
                }else{
                    return "200"; //do nothing if name is unchanged.                           
                }
            }
        }

        public string delete(){
            if(File.Exists(path)){
                File.Delete(path);
                FileInfo thumbnail_file = new FileInfo(thumbnail_path());
                if(thumbnail_file.Exists){
                    thumbnail_file.Delete();
                }
                Tags.untag_craft(this);
                if(CraftManager.main_ui){CraftManager.main_ui.refresh();}
                return "200";
            } else{
                return "error 404 - file not found";
            }
        }

        public string save_description(){
            try{
                ConfigNode nodes = ConfigNode.Load(path);
                nodes.SetValue("description", description.Replace("\n","¨"));
                nodes.Save(path);
                initialize(path, stock_craft);  //reprocess the craft file
                return "200";
            }
            catch(Exception e){
                return "Unable to update description; " + e.Message;
            }
        }

        //Transfer craft to VAB/SPH/Subassemblies
        public string transfer_to(EditorFacility facility){
            string new_path = "";
            if(facility == EditorFacility.SPH){
                new_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir, "Ships", "SPH", name + ".craft");
            } else if(facility == EditorFacility.VAB){
                new_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir, "Ships", "VAB", name + ".craft");
            } else if(facility == EditorFacility.None){
                new_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir, "Subassemblies", name + ".craft");
            }
            if(String.IsNullOrEmpty(new_path)){
                return "Unexpected error";
            }
            if(File.Exists(new_path)){
                string msg = "A craft with this name already exists in " + (facility == EditorFacility.None ? "Subassemblies" : "the " + facility.ToString());
                return msg;
            } else{
                try{
                    ConfigNode nodes = ConfigNode.Load(path);
                    nodes.SetValue("type", facility.ToString());
                    nodes.Save(new_path);
                }
                catch(Exception e){
                    return "Unable to move craft; " + e.Message;
                }
                List<string> tags = Tags.untag_craft(this);
                FileInfo thumbnail_file = new FileInfo(thumbnail_path());

                File.Delete(path);
                initialize(new_path, stock_craft);
                Tags.tag_craft(this, tags);
                if(thumbnail_file.Exists){
                    thumbnail_file.MoveTo(thumbnail_path());
                    load_thumbnail_image();
                }
                if(CraftManager.main_ui){CraftManager.main_ui.refresh();}
                return "200";
            }
        }


        //move or copy craft to another save in the same install.
        public string move_copy_to(string new_save_dir, bool move = false){
            string new_path = "";
            List<string> existing_saves = save_names;

            if(String.IsNullOrEmpty(new_save_dir)){
                return "You must select a save";
            }
            if(existing_saves.Contains(new_save_dir)){
                if(this.construction_type == "Subassembly"){
                    new_path = Paths.joined(CraftManager.ksp_root, "saves", new_save_dir, "Subassemblies", name + ".craft");
                } else{                
                    new_path = Paths.joined(CraftManager.ksp_root, "saves", new_save_dir, "Ships", this.construction_type, name + ".craft");
                }
                if(File.Exists(new_path)){
                    return "A Craft with this name alread exists in " + new_save_dir;
                } else{
                    FileInfo file = new FileInfo(path);
                    FileInfo thumbnail_file = new FileInfo(thumbnail_path());
                    try{                        
                        if(move){
                            file.MoveTo(new_path);
                            thumbnail_file.MoveTo(thumbnail_path(new_save_dir, this.construction_type, this.name));
                        }else{                        
                            file.CopyTo(new_path);
                            thumbnail_file.CopyTo(thumbnail_path(new_save_dir, this.construction_type, this.name));
                        }
                        if(CraftManager.main_ui){CraftManager.main_ui.refresh();}
                        return "200";
                    }
                    catch(Exception e){
                        return "Unable to " + (move ? "move" : "copy") + " craft; " + e.Message;
                    }
                }
            } else{
                return "'" + new_save_dir + "' does not exist";
            }

        }


    }

}

