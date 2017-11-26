using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;


using KatLib;

namespace CraftManager
{

    public class CraftDataCache
    {


        public string cache_path = Paths.joined(CraftManager.ksp_root, "GameData", "CraftManager", "craft_data.cache");
        public Dictionary<string, ConfigNode> craft_data = new Dictionary<string, ConfigNode>();

        public Dictionary<string, AvailablePart> part_data = new Dictionary<string, AvailablePart>();  //name->part lookup for available parts
        public List<string> locked_parts = new List<string>();

        public string installed_part_sig;

        public CraftDataCache(){
            CraftManager.log("Initializing Cache");
            if(File.Exists(cache_path)){
                CraftManager.log("loading cached craft data from file");
                load(); 
            }

            if(part_data.Count == 0){
                locked_parts.Clear();
                CraftManager.log("caching game parts");
                List<string> part_names = new List<string>();
                foreach(AvailablePart part in PartLoader.LoadedPartsList){
                    part_data.Add(part.name, part);
                    part_names.Add(part.name);
                    if(!ResearchAndDevelopment.PartTechAvailable(part)){
                        locked_parts.AddUnique(part.name);
                    }
                }
                part_names.Sort();
                string s = "";
                foreach(string n in part_names){s = s + n;}
                installed_part_sig = Checksum.digest(s);
            }

            CraftManager.log("Cache Ready");
        }

        public AvailablePart fetch_part(string part_name){
            if(part_data.ContainsKey(part_name)){
                return part_data[part_name];
            }
            return null;
        }

        //takes a CraftData craft and creates a ConfigNode that contains all of it's public properties, ConfigNodes is held in 
        //a <string, ConfigNode> dict with the full path as the key. 
        public void write(CraftData craft){
            ConfigNode node = new ConfigNode();
            foreach(var prop in craft.GetType().GetProperties()){               
                node.AddValue(prop.Name, prop.GetValue(craft, null));
            }


            if(craft_data.ContainsKey(craft.path)){
                craft_data[craft.path] = node;
            }else{
                craft_data.Add(craft.path,node);
            }
            save();
        }

        //Takes a CraftData craft object and if the cached data contains a matching path AND the checksum value matches
        //then the craft's properties are populated from the ConfigNode in the cache.  Returns true if matching data was
        //found, otherwise returns false, in which case the data will have to be interpreted from the .craft file.
        public bool try_fetch(CraftData craft){
            if(craft_data.ContainsKey(craft.path) && craft_data[craft.path].GetValue("checksum") == craft.checksum && craft_data[craft.path].GetValue("part_sig") ==installed_part_sig){
                try{
                    ConfigNode node = craft_data[craft.path];                    
                    foreach(var prop in craft.GetType().GetProperties()){               
                        if(prop.CanWrite){
                            var node_value = node.GetValue(prop.Name);
                            if(!String.IsNullOrEmpty(node_value)){
                                var type = prop.GetValue(craft, null);
                                if(type is float){
                                    prop.SetValue(craft, float.Parse(node_value), null);                                
                                }else if(type is int){
                                    prop.SetValue(craft, int.Parse(node_value), null);                                
                                }else if(type is bool){
                                    prop.SetValue(craft, bool.Parse(node_value), null);                                
                                }else{
                                    prop.SetValue(craft, node_value, null);                                
                                }

                            }
                        }
                    }

                    return true;
                }

                catch(Exception e){
                    CraftManager.log("try_fetch failed: " + e.Message + "\n" + e.StackTrace);
                    return false;
                }

            } else{
                return false;
            }

        }

        private void save(){
            ConfigNode nodes = new ConfigNode();
            ConfigNode craft_nodes = new ConfigNode();

            foreach(KeyValuePair<string, ConfigNode> pair in craft_data){
                craft_nodes.AddNode("CRAFT", pair.Value);
            }
            nodes.AddNode("CraftData", craft_nodes);

            nodes.Save(cache_path);
        }

        private void load(){
            craft_data.Clear();
            ConfigNode nodes = ConfigNode.Load(cache_path);
            ConfigNode craft_nodes = nodes.GetNode("CraftData");
            foreach(ConfigNode node in craft_nodes.nodes){
                craft_data.Add(node.GetValue("path"), node);
            }
        }
    }


    public class CraftData
    {
        //**Class Methods/Variables**//

        public static List<CraftData> all_craft = new List<CraftData>();  //will hold all the craft loaded from disk
        public static List<CraftData> filtered  = new List<CraftData>();  //will hold the results of search/filtering to be shown in the UI.
        public static CraftDataCache cache = null;

        public static int save_state = 0;
        public static bool loading_craft = false;
        public static bool craft_saved {
            get{
                return save_state <= 0;
            }
        }




        public static void load_craft(string save_dir = null){
            if(cache == null){
                cache = new CraftDataCache();                
            }

            string[] craft_file_paths;
            if(save_dir == null){
                craft_file_paths = Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "saves"), "*.craft", SearchOption.AllDirectories);
            } else{
                craft_file_paths = Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "saves", save_dir), "*.craft", SearchOption.AllDirectories);
            }

            all_craft.Clear();
            foreach(string path in craft_file_paths){
                all_craft.Add(new CraftData(path));
            }

            foreach(string path in Directory.GetFiles(Paths.joined(CraftManager.ksp_root, "Ships"), "*.craft", SearchOption.AllDirectories)){
                all_craft.Add(new CraftData(path, true));
            }
                

        }


        public static void filter_craft(Dictionary<string, object> criteria){
            filtered = all_craft;    
//            if(criteria.ContainsKey("save_dir")){
//                filtered = filtered.FindAll(craft => craft.save_dir == (string)criteria["save_dir"]);
//            }
            if(criteria.ContainsKey("exclude_stock")){
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
            if(criteria.ContainsKey("tags")){
                List<string> s_tags = (List<string>)criteria["tags"];
                if((bool)criteria["tag_mode_reduce"]){
                    foreach(string tag in s_tags){
                        filtered = filtered.FindAll(craft => craft.tags().Contains(tag));
                    }
                } else{
                    filtered = filtered.FindAll(craft =>{
                        bool sel = false;
                        foreach(string tag in craft.tags()){
                            if(s_tags.Contains(tag)){
                                sel = true;
                            }
                        }
                        return sel;
                    });
                }
            }
            if(criteria.ContainsKey("sort")){
                string sort_by = (string)criteria["sort"];
                filtered.Sort((x,y) => {
                    //{"name", "part_count", "mass", "date_created", "date_updated", "stage_count"};
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
                    }else if(sort_by == "date_created"){
                        return x.create_time.CompareTo(y.create_time);
                    }else if(sort_by == "date_updated"){
                        return x.last_updated_time.CompareTo(y.last_updated_time);
                    }else{
                        return x.name.CompareTo(y.name);
                    }
                });
                if(criteria.ContainsKey("reverse_sort") && (bool)criteria["reverse_sort"]){
                    filtered.Reverse();
                }
            }
        }
            
        public static void select_craft(CraftData craft){
            foreach(CraftData list_craft in filtered){
                list_craft.selected = list_craft == craft;
            }
        }

        public static CraftData selected_craft { 
            get { 
                return filtered.Find(c => c.selected == true);
            } 
        }




        //**Instance Methods/Variables**//

        //craft attributes - attributes with getters will automatically be stored in the cache and 
        //if they also have a setter then they can be restored from the cache.
        public string path { get; set; }
        public string checksum { get; set; }
        public string part_sig { get; set; }
        public string name { get; set; }
        public string alt_name { get; set; }
        public string description { get; set; }
        public string construction_type { get; set; }
        public int stage_count { get; set; }
        public int part_count { get; set; }
        public bool missing_parts { get; set; }
        public bool stock_craft { get; set; }
        public float cost_dry { get; set; }
        public float cost_fuel { get; set; }
        public float cost_total { get; set; }
        public float mass_dry { get; set; }
        public float mass_fuel { get; set; }
        public float mass_total { get; set; }



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

        public bool locked_parts = false;
        public void check_locked_parts() {
            locked_parts = false;
            foreach(string p_name in part_name_list){
                if(cache.locked_parts.Contains(p_name)){
                    locked_parts = true;
                }
            }
        }

        //Attribues which are always set from craft file/path, not loaded from cache
        public Texture2D thumbnail;
        public string create_time;
        public string last_updated_time;
        public string save_dir;
        public bool selected = false;


        //Other Attributes
        public string new_name = "";

        //Initialize a new CraftData object. Takes a path to a .craft file and either populates it from attributes from the craft file
        //or loads information from the CraftDataCache
        public CraftData(string full_path, bool stock = false){
            initialize(full_path, stock);
        }

        protected void initialize(string full_path, bool stock = false){
            path = full_path;
            checksum = Checksum.digest(File.ReadAllText(path));
            stock_craft = stock;

            //attempt to load craft data from the cache. If unable to fetch from cache then load 
            //craft data from the .craft file and cache the loaded info.
            if(!cache.try_fetch(this)){
                read_craft_info_from_file();
                part_sig = cache.installed_part_sig;
                cache.write(this);
            }
            //HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX
            check_locked_parts();

            //set timestamp data from the craft file
            create_time = System.IO.File.GetCreationTime(path).ToBinary().ToString();
            last_updated_time = System.IO.File.GetLastWriteTime(path).ToBinary().ToString();

            string thumbnail_path;
            if(stock_craft){
                save_dir = "Stock Craft";
                thumbnail_path = Paths.joined(CraftManager.ksp_root, "Ships", "@thumbs", construction_type,  name + ".png");
            } else{
                save_dir = path.Replace(Paths.joined(CraftManager.ksp_root, "saves", ""), "").Split('/')[0];
                thumbnail_path = Paths.joined(CraftManager.ksp_root, "thumbs/" + save_dir + "_" + construction_type + "_" + name + ".png");
            }

            if(File.Exists(thumbnail_path)){
                thumbnail = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                byte[] pic_data = File.ReadAllBytes(thumbnail_path);  //read image file
                thumbnail.LoadImage(pic_data);          
            } else{
                thumbnail = (Texture2D)StyleSheet.assets[construction_type + "_placeholder"];                
            }

//            thumbnail = ShipConstruction.GetThumbnail("/thumbs/" + save_dir + "_" + construction_type + "_" + name);
        }


        //Parse .craft file and read info
        private void read_craft_info_from_file(){
            name = Path.GetFileNameWithoutExtension(path);
            CraftManager.log("Loading craft data from file for " + name);

            ConfigNode data = ConfigNode.Load(path);
            ConfigNode[] parts = data.GetNodes();
            AvailablePart matched_part;

            alt_name = data.GetValue("ship");
            description = data.GetValue("description");
            construction_type = data.GetValue("type");
            if(!(construction_type == "SPH" || construction_type == "VAB")){
                construction_type = "Subassembly";
            }
            part_count = parts.Length;
            stage_count = 0;
            missing_parts = false;
//            locked_parts = false;

            cost_dry = 0;cost_fuel = 0;cost_total = 0;mass_dry = 0;mass_fuel = 0;mass_total = 0;

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
                    cost_dry += dry_cost;
                    cost_fuel += fuel_cost;
                    mass_dry += dry_mass;
                    mass_fuel += fuel_mass;
//                    if(!ResearchAndDevelopment.PartTechAvailable(matched_part)){
//                        locked_parts = true;
//                    }

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
            return Tags.tags_for(Tags.craft_reference_key(this));
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
                    FileInfo file = new FileInfo(path);
                    if(file.Exists){                    
                        try{
                            file.MoveTo(new_path);
                        }
                        catch(Exception e){
                            return "Unable to rename file\n" + e.Message;
                        }
                        ConfigNode nodes = ConfigNode.Load(new_path);
                        nodes.SetValue("ship", new_name);
                        nodes.Save(new_path);
                        initialize(new_path, stock_craft);  //reprocess the craft file
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
                    return "name must be different";                                       
                }
            }
        }

        public string delete(){
            if(File.Exists(path)){
                File.Delete(path);
                return "200";
            } else{
                return "error 404 - file not found";
            }

        }

    }

}

