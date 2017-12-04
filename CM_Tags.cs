using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using KatLib;


namespace CraftManager
{

    public class Tag
    {
        public string name;
        public string save_dir;
        public List<string> craft = new List<string>();

        public Tag(string tag_name, string save_name){
            name = tag_name;
            save_dir = save_name;
            Tags.instance.data.Add(this);
        }

        public void add(CraftData craft){
            this.craft.AddUnique(Tags.craft_reference_key(craft));
            Tags.save();
        }

//        public void remove(string craft_ref){
//            if(craft.Contains(craft_ref)){
//                craft.Remove(craft_ref);
//            }
//        }
    }


    public class Tags
    {

        public List<Tag> data = new List<Tag>(); //Holds all the loaded tags
        public List<string> names_list = new List<string>(); //Holds the naames of Tags (used to draw UI list and dropdown menus).
        public Dictionary<string, bool> selected_lookup = new Dictionary<string, bool>(); //holds a reference to each loaded tag's name and if they are selected or not


        public Tags(){
            Tags.instance = this;  
        }


        //Static Stuff Below

        public static Tags instance;

        //Accessor Methods
        public static List<string> names {
            get{ return Tags.instance.names_list; }
        }

        //return the reference used to identify a craft; ie SPH_myRocket 
        public static string craft_reference_key(CraftData craft){            
            return craft.construction_type + "_" + craft.name;
        }

        //returns the path to the .tags file for a given save dir
        public static string tag_file_path(string save_dir){
            return Paths.joined(CraftManager.ksp_root, "saves", save_dir, "craft.tags");
        }


        //Finds a Tag given a name and save dir
        public static Tag find(string tag_name, string save_dir = "all"){
            if(save_dir == "all"){
                return Tags.instance.data.Find(t => (t.name == tag_name));
            } else{
                return Tags.instance.data.Find(t => (t.name == tag_name && t.save_dir == save_dir));
            }
        }

        //Find all tags which match a name in either a given save or in all saves if save_dir is given as "all"
        public static List<Tag> find_all(string tag_name, string save_dir = "all"){
            if(save_dir == "all"){
                return Tags.instance.data.FindAll(t => (t.name == tag_name));
            } else{
                return Tags.instance.data.FindAll(t => (t.name == tag_name && t.save_dir == save_dir));
            }                
        }

        //returns a matching Tag or creates a new on if none was fond.
        public static Tag find_or_create_by(string tag_name, string save_dir, bool save_on_create = true){            
            if(String.IsNullOrEmpty(tag_name)){
                return null;
            }
            Tag tag = Tags.find(tag_name, save_dir);
            if(tag == null){
                tag = new Tag(tag_name, save_dir);
            }
            if(save_on_create){
                Tags.save();
            }
            return tag;
        }

        public static string create(string tag_name, string save_dir, CraftData craft = null){
            if(String.IsNullOrEmpty(tag_name)){
                return "Tag Name cannot be blank";
            } else if(names.Contains(tag_name)){
                return "A tag with this name already exists";
            } else{
                Tag tag = Tags.find_or_create_by(tag_name, save_dir);
                if(craft != null){
                    tag.add(craft);
                }
                return "200";
            }
        }

        //Remove a Tag from a save or from all saves if save_dir is "all"
        public static string remove(string tag_name, string save_dir){          
            List<Tag> tags = Tags.find_all(tag_name, save_dir);                
            foreach(Tag tag in tags){
                Tags.instance.data.Remove(tag);
            }
            Tags.save();
            return "200";
        }
            
        public static string rename(string cur_tag_name, string new_tag_name, string save_dir){
            if(String.IsNullOrEmpty(new_tag_name)){
                return "Name cannot be blank";
            } else if(new_tag_name == cur_tag_name){
                return "200"; //do nothing if name is unchanged
            } else if(Tags.names.Contains(new_tag_name)){
                return "A tag with this name already exists";
            } else{                
                List<Tag> tags = Tags.find_all(cur_tag_name, save_dir);                
                foreach(Tag tag in tags){
                    tag.name = new_tag_name;    
                }
                Tags.save();
                return "200";
            }
        }

        //Associate a craft with a tag. Will create a Tag with the given name if it doesn't already exist
        public static void tag_craft(CraftData craft, string tag_name){
            Tag tag = Tags.find_or_create_by(tag_name, craft.save_dir);
            tag.add(craft);
        }
        //Associate a craft with a set of tags. Creates Tags as needed
        public static void tag_craft(CraftData craft, List<string> tags){
            foreach(string tag_name in tags){
                Tags.tag_craft(craft, tag_name);
            }
        }

        //Unassociate craft with the given tag
        public static void untag_craft(CraftData craft, string tag_name){
            Tag tag = Tags.instance.data.Find(t => (t.name == tag_name && t.save_dir == craft.save_dir));
            if(tag!=null && tag.craft.Contains(craft_reference_key(craft))){                
                tag.craft.Remove(craft_reference_key(craft));
            }
            Tags.save();
        }
        //Unassociates a craft with all the tags it is associated with. Returns a list of the tags
        public static List<string> untag_craft(CraftData craft){
            List<Tag> tags = Tags.instance.data.FindAll(t => (t.save_dir == craft.save_dir && t.craft.Contains(craft_reference_key(craft))));
            List<string> tag_names = new List<string>();
            foreach(Tag tag in tags){
                tag.craft.Remove(craft_reference_key(craft));
                tag_names.AddUnique(tag.name);

            }
            Tags.save();
            return tag_names;
        }


        //get a list of tags for a craft
        public static List<string> for_craft(CraftData craft){
            List<Tag> tags = Tags.instance.data.FindAll(t => (t.save_dir == craft.save_dir && t.craft.Contains(craft_reference_key(craft))));
            List<string> in_tags = new List<string>();

            foreach(Tag tag in tags){
                in_tags.AddUnique(tag.name);
            }
            return in_tags;
        }

        //returns true or false if the given tag_name is selected in the UI
        public static bool is_selected(string tag_name){
            return Tags.instance.selected_lookup[tag_name];
        }
        //Toggles a tags selected status.
        public static void toggle_tag(string tag_name){
            Tags.instance.selected_lookup[tag_name] = !Tags.instance.selected_lookup[tag_name];
        }

        //returns a list of all selected tags
        public static List<string> selected_tags(){
            List<string> s_tags = new List<string>();
            foreach(KeyValuePair<string, bool> tag in Tags.instance.selected_lookup){
                if(tag.Value){
                    s_tags.AddUnique(tag.Key);
                }
            }
            return s_tags;
        }

        //returns the number of craft associated with a given tag name. Takes a second optional argument to specify if the count
        //is for all loaded craft ("all"), or limited to the search results ("filtered")
        public static int craft_count_for(string tag_name, string mode = "all"){
            List<Tag> tags = Tags.find_all(tag_name);
            int count = 0;
            if(mode == "filtered"){
                foreach(Tag tag in tags){                    
                    count += CraftData.filtered.FindAll(c => tag.save_dir == c.save_dir && tag.craft.Contains(Tags.craft_reference_key(c))).Count;
                }
            } else if(mode == "raw_count"){
                foreach(Tag tag in tags){                    
                    count += tag.craft.Count;
                }
            } else {
                foreach(Tag tag in tags){                    
                    count += CraftData.all_craft.FindAll(c => tag.save_dir==c.save_dir && tag.craft.Contains(Tags.craft_reference_key(c))).Count;
                }
            }
            return count;
        }


        //maintains the two reference lists (selected_lookup and names_list). called after both load and save actions
        //takes the names of all tags and makes a unique list (names_list) and a unique <string, bool> dict (selected_lookup) 
        //but will preserve the state of the coresponding bools in selected_lookup
        public static void update_lists(){
            Dictionary<string, bool> new_list = new Dictionary<string, bool>();
            List<string> new_name_list = new List<string>();

            foreach(Tag tag in Tags.instance.data){
                if(!new_list.ContainsKey(tag.name)){
                    bool cur_val = Tags.instance.selected_lookup.ContainsKey(tag.name) ? Tags.instance.selected_lookup[tag.name] : false;
                    new_list.Add(tag.name, cur_val);
                }
                new_name_list.AddUnique(tag.name);
            }
            Tags.instance.selected_lookup = new_list;
            Tags.instance.names_list = new_name_list;
            sort_tag_list();
        }

        public static void sort_tag_list(){
            if(CraftManager.main_ui.tag_sort_by == "craft_count"){
                Tags.instance.names_list.Sort((x,y) => craft_count_for(y).CompareTo(craft_count_for(x)) );
            } else{
                Tags.instance.names_list.Sort((x,y) => x.CompareTo(y));
            }
        }

        //Loads Tag data from confignode file for a given save or for all saves if "all" is given
        //If a save does not have a craft.tags file it copies the default.tags file into the save.
        //Will also instanciate an instance of Tags if one does not already exist.
        public static void load(string save_name){
            if(Tags.instance == null){ new Tags(); }
            List<string> save_dirs = new List<string> { save_name };

            Tags.instance.data.Clear();
            if(save_name == "all"){
                save_dirs = CraftData.save_names();
            }

            foreach(string save_dir in save_dirs){
                CraftManager.log("loading tags for " + save_dir);
                if(!File.Exists(tag_file_path(save_dir))){
                    FileInfo default_tags = new FileInfo(Paths.joined(CraftManager.ksp_root, "GameData", "CraftManager", "default.tags"));
                    //TODO handle case where default.tags file is not present.
                    default_tags.CopyTo(tag_file_path(save_dir));
                }
                ConfigNode raw_data = ConfigNode.Load(tag_file_path(save_dir));
                ConfigNode tag_nodes = raw_data.GetNode("TAGS");

                foreach(ConfigNode tag_node in tag_nodes.nodes){
                    string tag_name = tag_node.GetValue("tag_name");
                    string[] craft = tag_node.GetValues("craft");
                    Tag tag = Tags.find_or_create_by(tag_name, save_dir, false);
                    foreach(string craft_ref in craft){
                        tag.craft.AddUnique(craft_ref);
                    }
                }
            }
            Tags.update_lists();
            
        }

        //Saves Tag data to config node craft.tags file.  
        public static void save(){
            //First group the tags by the save which they belong to
            Dictionary<string, List<Tag>> tags_by_save = new Dictionary<string, List<Tag>>();
            foreach(Tag tag in Tags.instance.data){
                if(!tags_by_save.ContainsKey(tag.save_dir)){
                    tags_by_save.Add(tag.save_dir, new List<Tag>());
                }
                tags_by_save[tag.save_dir].Add(tag);
            }

            //Then for each group of tags create ConfigNode of data and save to file in coresponding save
            foreach(KeyValuePair<string, List<Tag>> pair in tags_by_save){
                ConfigNode nodes = new ConfigNode();
                ConfigNode tag_nodes = new ConfigNode();
                List<Tag> tags = pair.Value;
                string save_dir = pair.Key;
                foreach(Tag tag in tags){
                    ConfigNode node = new ConfigNode();
                    node.AddValue("tag_name", tag.name);
                    foreach(string craft_ref in tag.craft){
                        node.AddValue("craft", craft_ref);
                    }
                    tag_nodes.AddNode("TAG", node);
                }
                nodes.AddNode("TAGS", tag_nodes);
                nodes.Save(tag_file_path(save_dir));
            }
            Tags.update_lists();
        }

    }

}

