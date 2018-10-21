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

        public static int save_state = 0;
        public static bool loaded_craft_saved {
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
            for(int i=0; i < craft_file_paths.Length; i++){
                new CraftData(craft_file_paths[i]);
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
            CraftManager.log("loading stock craft");
            string[] craft_file_paths = Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "Ships"), "*.craft", SearchOption.AllDirectories);
            for(int i=0; i < craft_file_paths.Length; i++){
                new CraftData(craft_file_paths[i], true);
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

        }


        internal static List<string> previously_selected = new List<string>();
        internal static void track_currently_selected(){
            previously_selected.Clear();
            foreach(CraftData craft in CraftData.active_craft){
                previously_selected.Add(craft.path);
            }
        }
        internal static void restore_previously_selected(){
            CraftData.deselect_all();
            foreach(string path in previously_selected){
                CraftData craft = CraftData.all_craft.Find(c => c.path == path);
                if(craft != null){
                    craft.group_selected = true;
                }
            }
            if(active_craft.Count == 1){
                CraftData.select_craft(active_craft[0]);
            }
        }

        public static void deselect_all(){
            for(int i = 0; i < CraftData.all_craft.Count; i++){
                CraftData.all_craft[i].selected = false;
                CraftData.all_craft[i].group_selected = false;
            }
        }
            
        public static void select_craft(CraftData craft){            
            deselect_all();
            craft.selected = true;
        }

        public static void toggle_selected(CraftData craft){
            if(craft.selected){
                craft.selected = false;
            }else{
                CraftData.select_craft(craft);
            }
        }

        public static void group_select(CraftData craft){
            for(int i = 0; i < CraftData.all_craft.Count; i++){
                if(CraftData.all_craft[i].selected){
                    CraftData.all_craft[i].selected = false;
                    CraftData.all_craft[i].group_selected = true;
                }
            }
            craft.group_selected = true;
        }

        public static void toggle_group_select(CraftData craft){
            if(active_craft.Count == 0 || (active_craft.Count == 1 && active_craft[0] == craft)){
                toggle_selected(craft);
            } else{
                if(craft.group_selected){
                    craft.group_selected = false;
                } else{
                    CraftData.group_select(craft);
                }                
            }
            if(active_craft.Count == 1){
                CraftData.select_craft(active_craft[0]);
            }
        }

        public static void shift_select(CraftData craft){
            int sel_index = CraftData.filtered.IndexOf(craft);
            Vector2 cur_sel_indexes = new Vector2();
            Vector2 sel_between = new Vector2();
            cur_sel_indexes.x = CraftData.filtered.IndexOf(CraftData.active_craft[0]);
            cur_sel_indexes.y = CraftData.filtered.IndexOf(CraftData.active_craft[CraftData.active_craft.Count-1]);
            if(sel_index < (int)cur_sel_indexes.x){
                sel_between.x = sel_index; sel_between.y = cur_sel_indexes.x;
            }else if(sel_index > (int)cur_sel_indexes.y){
                sel_between.x = cur_sel_indexes.x; sel_between.y = sel_index;
            }
            deselect_all();
            for(int i = 0; i < CraftData.filtered.Count; i++){
                if(i >= (int)sel_between.x && i <= (int)sel_between.y){
                    group_select(CraftData.filtered[i]);
                }
            }
                
        }

        public static CraftData selected_craft { 
            get { 
                return all_craft.Find(c => c.selected == true);
            } 
        }

        public static List<CraftData> selected_group{
            get{
                return all_craft.FindAll(c => c.group_selected == true);
            }
        }

        //active_craft returns all craft that are selected, be that just the single selected_craft or the selected_group
        public static List<CraftData> active_craft{
            get{ 
                List<CraftData> all_selected_craft = new List<CraftData>(selected_group);
                if(CraftData.selected_craft != null){
                    all_selected_craft.AddUnique(CraftData.selected_craft);
                }
                return all_selected_craft;
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

        internal static string delete_active_craft(){
            return perform_bulk_action(craft => craft.delete());
        }

        internal static string transfer_active_craft_to(EditorFacility facility){
            return perform_bulk_action(craft => craft.transfer_to(facility));
        }

        internal static string move_copy_active_craft_to(string new_save_dir, bool move = false){
            return perform_bulk_action(craft => craft.move_copy_to(new_save_dir, move));
        }

        private delegate string BulkActionCallback(CraftData craft);
        private static string perform_bulk_action(BulkActionCallback action){
            string r = "200";
            foreach(CraftData craft in active_craft){
                if(r == "200"){
                    r = action(craft);
                }
            }
            return r;
        }



        //**Instance Methods/Variables**//

        //craft attributes - attributes with getters will automatically be stored in the in memory cache and written do persistent store cache
        //if they also have a setter then they can be restored from the cache.
        public string name { get; set; }
        public string file_name { get; set; }
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
        public bool has_locked_parts { get; set; }  //This is an exception. locked_parts will be cached to the in-memory cache but not to the persistent cache.


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
        public bool group_selected = false;
        public bool locked_parts_checked = false;
        public List<string> tag_name_cache = null;

        public string new_name = "";
        public float list_position = 0;
        public float list_height = 0;
        public bool draw = true;
        public bool menu_open = false;



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
            name = craft_name; file_name = craft_name; description = desc;
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
            missing_parts = has_locked_parts = false;           

            CraftData.all_craft.Add(this);
        }

        public void initialize(string full_path, bool stock = false){        
            path = full_path.Replace("\\", "/");
            checksum = Checksum.digest(File.ReadAllText(path));
            stock_craft = stock;
            has_locked_parts = false;
            locked_parts_checked = false;

            bool cache_after_load = false;

            //attempt to load craft data from the cache. If unable to fetch from cache then load 
            //craft data from the .craft file and cache the loaded info.
            if(!cache.try_fetch(this)){
                read_craft_info_from_file();
                check_locked_parts();
                part_sig = CraftDataCache.installed_part_sig;
                cache_after_load = true;
            }
                
            bool locked_parts_state = has_locked_parts;
            if(!locked_parts_checked){
                check_locked_parts();
                if(locked_parts_state != has_locked_parts){
                    cache_after_load = true;
                }
            }

            list_height = 85;
            if(this.missing_parts && this.has_locked_parts){
                list_height = 121;
            } else if(this.missing_parts || this.has_locked_parts){
                list_height = 95;
            }

            if(cache_after_load){
                cache.write(this);
            }

            //set timestamp data from the craft file
            create_time = System.IO.File.GetCreationTime(path).ToUniversalTime().ToBinary().ToString();
            last_updated_time = System.IO.File.GetLastWriteTime(path).ToUniversalTime().ToBinary().ToString();

            //thumbnails are no longer loaded at this point, they are loaded on demand when the craft is shown in the list using a coroutine 
        }

        //return the path to the craft's thumbnail based on craft properties. Overload method provides means to generate a 
        //different thumbnail path without changing the crafts properties.
        public string thumbnail_path(){
            return thumbnail_path(stock_craft ? "stock" : save_dir, construction_type, file_name);
        }
        public string thumbnail_path(string save_folder, string construct_type, string craft_name){
            if(save_folder == "stock"){
                return Paths.joined(CraftManager.ksp_root, "Ships", "@thumbs", construct_type,  craft_name + ".png");
            } else{
                return Paths.joined(CraftManager.ksp_root, "thumbs", save_folder + "_" + construct_type + "_" + craft_name + ".png");
            }
        }

        public System.Collections.IEnumerator load_thumbnail_image(){
            yield return true;
            string thumbnail_path = this.thumbnail_path();
            if(File.Exists(thumbnail_path)){
                thumbnail = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                byte[] pic_data = File.ReadAllBytes(thumbnail_path);  //read image file
                thumbnail.LoadImage(pic_data);          
            } else{
                thumbnail = (Texture2D)StyleSheet.assets[construction_type + "_placeholder"];                
            }
            CraftManager.main_ui.thumbnail_generating = false;
        }

        //Parse .craft file and read info
        private void read_craft_info_from_file(){
            file_name = Path.GetFileNameWithoutExtension(path);
            CraftData.file_load_count += 1; //increment count of craft loaded from file

            ConfigNode data = ConfigNode.Load(path);
            ConfigNode[] parts = data.GetNodes();
            AvailablePart matched_part;

            name = data.GetValue("ship");
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
            has_locked_parts = false;
            if(HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX){
                foreach(string p_name in part_name_list){
                    if(CraftDataCache.locked_parts.Contains(p_name)){
                        has_locked_parts = true;
                    }
                }
            }
            locked_parts_checked = true;
        }

        public List<string> list_parts(){
            ConfigNode data = ConfigNode.Load(path);
            ConfigNode[] parts = data.GetNodes();
            List<string> parts_list = new List<string>();

            foreach(ConfigNode part in parts){
                parts_list.AddUnique(get_part_name(part));
            }
            return parts_list;
        }

        public List<string> list_missing_parts(){
            
            AvailablePart matched_part;
            ConfigNode data = ConfigNode.Load(path);
            ConfigNode[] parts = data.GetNodes();
            List<string> missing_parts_list = new List<string>();
            foreach(ConfigNode part in parts){

                string part_name = get_part_name(part);
                matched_part = cache.fetch_part(part_name);
                if(matched_part == null){
                    missing_parts_list.AddUnique(part_name);
                }
            }
            return missing_parts_list;
        }

        //Rename the craft (both name in the craft file and the file itself). Does checks before attempting rename to ensure valid name.
        //the new name should be set on the craft object first;
        //craft.new_name = "I am Jeff";
        //craft.rename();
        //returns various strings depending on outcome, a "200" string means all went ok, yes it's an HTTP status code, I'm a web dev, deal with it.
        public string rename(){
            string os_safe_name = new_name;
            if(!String.IsNullOrEmpty(new_name) && new_name != name){
                
                foreach(char c in Path.GetInvalidFileNameChars()){                    
                    if(os_safe_name.Contains(c.ToString())){
                        os_safe_name = os_safe_name.Replace(c, '_');
                    }
                }

                string new_path = path.Replace(file_name + ".craft", os_safe_name + ".craft");
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
                        thumbnail = null;
                    }
                    return "200";
                } else{                    
                    return "error 404 - file not found";
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
                CraftManager.log("Deleting Craft: " + path);
                File.Delete(path);
                FileInfo thumbnail_file = new FileInfo(thumbnail_path());
                if(thumbnail_file.Exists){
                    thumbnail_file.Delete();
                }
                Tags.untag_craft(this);
                if(CraftManager.main_ui){CraftManager.main_ui.refresh();}
                return "200";
            } else{
                return "error 404 - file not found: " + path;
            }
        }

        public string save_description(){
            try{
                //update the description field in the editor if the current selected craft matches the name of the loaded craft (not ideal, has edge cases).
                if(EditorLogic.fetch.ship.shipName == CraftData.selected_craft.name){
                    EditorLogic.fetch.shipDescriptionField.text = description;
                }
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
                new_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir, "Ships", "SPH", file_name + ".craft");
            } else if(facility == EditorFacility.VAB){
                new_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir, "Ships", "VAB", file_name + ".craft");
            } else if(facility == EditorFacility.None){
                new_path = Paths.joined(CraftManager.ksp_root, "saves", save_dir, "Subassemblies",file_name + ".craft");
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
                    thumbnail = null;
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
                    new_path = Paths.joined(CraftManager.ksp_root, "saves", new_save_dir, "Subassemblies", file_name + ".craft");
                } else{                
                    new_path = Paths.joined(CraftManager.ksp_root, "saves", new_save_dir, "Ships", this.construction_type, file_name + ".craft");
                }
                if(File.Exists(new_path)){
                    return "A Craft with this name alread exists in " + new_save_dir;
                } else{
                    FileInfo file = new FileInfo(path);
                    FileInfo thumbnail_file = new FileInfo(thumbnail_path());
                    try{                        
                        if(move){
                            file.MoveTo(new_path);
                            if(thumbnail_file.Exists){
                                thumbnail_file.MoveTo(thumbnail_path(new_save_dir, this.construction_type, this.file_name));
                            }
                        }else{                        
                            file.CopyTo(new_path);
                            if(thumbnail_file.Exists){
                                thumbnail_file.CopyTo(thumbnail_path(new_save_dir, this.construction_type, this.file_name));
                            }
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

