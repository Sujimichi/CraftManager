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
        public string new_name;
        public string save_dir;
        public List<string> craft = new List<string>();
//        public bool selected = false;

        public Tag(string tag_name, string save_name){
            new Tag(tag_name, save_name, new List<string>());
        }

        public Tag(string tag_name, string save_name, List<string> assign_craft){
            name = tag_name;
            save_dir = save_name;
            craft = assign_craft;
            Tags.instance.data.Add(this);
            Tags.update_lists();
        }

        public void add(string craft_ref){
            if(!craft.Contains(craft_ref)){
                craft.Add(craft_ref);
            }
        }

        public void remove(string craft_ref){
            if(craft.Contains(craft_ref)){
                craft.Remove(craft_ref);
            }
        }

        public string rename(){
//            if(String.IsNullOrEmpty(new_name)){
//                return "Name cannot be blank";
//            } else if(new_name == name){
//                return "200"; //do nothing if name is unchanged
//            } else if(Tags.all.ContainsKey(new_name)){
//                return "A tag with this name already exists";
//            } else{                
//                Tags.all.Remove(name);
//                name = new_name;
//                Tags.all.Add(new_name, this);
//                Tags.save();
                return "200";
//            }
        }

        public int craft_count(string opt){
            if(opt == "filtered"){
                return CraftData.filtered.FindAll(c => this.craft.Contains(Tags.craft_reference_key(c))).Count;
            }else{
                return CraftData.all_craft.FindAll(c => this.craft.Contains(Tags.craft_reference_key(c))).Count;
            }
        }
    }


    public class Tags
    {

        public List<Tag> data = new List<Tag>();
        public Dictionary<string, bool> list = new Dictionary<string, bool>();
        public Dictionary<string, string> name_list = new Dictionary<string, string>();

        public Tags(){
            Tags.instance = this;    
        }

        public static Tags instance;

        public static Dictionary<string, bool> all {
            get{
                return Tags.instance.list;
            }
        }
        public static Dictionary<string, string> names {
            get{
                return Tags.instance.name_list;
            }
        }

        public static string craft_reference_key(CraftData craft){            
            return craft.construction_type + "_" + craft.name;
        }

        public static string tag_file_path(string save_dir){
            return Paths.joined(CraftManager.ksp_root, "saves", save_dir, "craft.tags");
        }

        public static Tag find_or_create_by(string tag_name, string save_dir){
            Tag tag = Tags.instance.data.Find(t => (t.name == tag_name && t.save_dir == save_dir));
            if(tag == null){
                tag = new Tag(tag_name, save_dir);
            }
            Tags.save();
            return tag;
        }

        public static void remove(string tag_name, string save_dir){          
            Tags.instance.data.RemoveAll(t => (t.name == tag_name && t.save_dir == save_dir)); //should only have to remove 1 instance, but this ensures all matching are removed (and it could be written as one line)
            Tags.save();
        }



        public static void tag_craft(CraftData craft, string tag_name){
            Tag tag = Tags.find_or_create_by(tag_name, craft.save_dir);
            tag.craft.AddUnique(craft_reference_key(craft));
            Tags.save();
        }
        public static void tag_craft(CraftData craft, List<string> tags){
            foreach(string tag_name in tags){
                Tags.tag_craft(craft, tag_name);
            }
        }

        public static void untag_craft(CraftData craft, string tag_name){
            Tag tag = Tags.instance.data.Find(t => (t.name == tag_name && t.save_dir == craft.save_dir));
            if(tag!=null && tag.craft.Contains(craft_reference_key(craft))){                
                tag.craft.Remove(craft_reference_key(craft));
            }
            Tags.save();
        }

        public static List<string> untag(CraftData craft){
            List<Tag> tags = Tags.instance.data.FindAll(t => (t.save_dir == craft.save_dir && t.craft.Contains(craft_reference_key(craft))));
            List<string> tag_names = new List<string>();
            foreach(Tag tag in tags){
                tag.craft.Remove(craft_reference_key(craft));
                tag_names.AddUnique(tag.name);

            }
            Tags.save();
            return tag_names;
        }

        public static List<string> remove_from_all_tags(CraftData craft){
            return Tags.untag(craft);
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

        public static void toggle_tag(string tag_name){
            Tags.instance.list[tag_name] = !Tags.instance.list[tag_name];

        }

        public static List<string> selected_tags(){
            List<string> s_tags = new List<string>();
            foreach(KeyValuePair<string, bool> tag in Tags.instance.list){
                if(tag.Value){
                    s_tags.AddUnique(tag.Key);
                }
            }
            return s_tags;
        }

        public static void update_lists(){
            Dictionary<string, bool> new_list = new Dictionary<string, bool>();
            Dictionary<string, string> new_name_list = new Dictionary<string, string>();

            foreach(Tag tag in Tags.instance.data){
                if(!new_list.ContainsKey(tag.name)){
                    bool cur_val = Tags.instance.list.ContainsKey(tag.name) ? Tags.instance.list[tag.name] : false;
                    new_list.Add(tag.name, cur_val);
                }
                if(!new_name_list.ContainsKey(tag.name)){
                    new_name_list.Add(tag.name, tag.name);
                }
            }
            Tags.instance.list = new_list;
            Tags.instance.name_list = new_name_list;
        }

        public static void load(string save_dir){            
            ConfigNode raw_data = ConfigNode.Load(tag_file_path(save_dir));
            ConfigNode tag_nodes = raw_data.GetNode("TAGS");

            foreach(ConfigNode tag_node in tag_nodes.nodes){
                string tag_name = tag_node.GetValue("tag_name");
                string[] craft = tag_node.GetValues("craft");
                new Tag(tag_name, save_dir, new List<string>(craft));
            }
            Tags.update_lists();
        }


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


//    public class Tags_old
//    {
//
//        public static string file_path = Paths.joined(CraftManager.ksp_root, "GameData", "CraftManager", "craft.tags");
//        public static Dictionary<string, Tag> all = new Dictionary<string, Tag>();
//        public static Dictionary<string, string> names = new Dictionary<string, string>();
//
//
//
//        public static string craft_reference_key(CraftData craft){            
//            return craft.construction_type + "_" + craft.name;
//        }
//
//        public static List<string> selected_tags(){
//            List<Tag> tags = new List<Tag>(Tags.all.Values);
//            List<string> s_tags = new List<string>();
//            foreach(Tag t in tags.FindAll(tag => tag.selected)){
//                s_tags.Add(t.name);
//            }
//            return s_tags;
//        }
//
//        //add a new tag. 
//        public static void add(string tag){
//            if(!all.ContainsKey(tag)){
//                all.Add(tag, new Tag(tag));
//                save();
//            }
//        }
//
//        //remove a tag (and all included craft references)
//        public static void remove(string tag){
//            if(all.ContainsKey(tag)){
//                all.Remove(tag);
//                save();
//            }
//        }
//
//        //add a craft reference to a tag. creates the tag if it doesn't exist
//        public static void tag_craft(CraftData craft, string tag){
//            add(tag); //ensure tag exists, doesn't do anything if tag exists
//            all[tag].add(craft_reference_key(craft));
//            save();
//        }
//        public static void tag_craft(CraftData craft, List<string> tags){
//            foreach(string tag_name in tags){
//                Tags.tag_craft(craft, tag_name);
//            }
//        }
//
//        //remove a craft reference from a tag. Does not remove tag if it is empty afterwards.
//        public static void untag_craft(CraftData craft, string tag){
//            if(all.ContainsKey(tag)){
//                string craft_ref = craft_reference_key(craft);
//                if(all[tag].craft.Contains(craft_ref)){
//                    all[tag].remove(craft_ref);
//                }
//            }
//            save();
//        }
//
//        public static List<string> remove_from_all_tags(CraftData craft){
//            List<string> tags = Tags.for_craft(craft);
//            foreach(string tagname in tags){
//                Tags.untag_craft(craft, tagname);
//            }
//            return tags;
//        }
//
//        //get a list of tags for a given craft reference
//        public static List<string> for_craft(CraftData craft){
//            string craft_ref = craft_reference_key(craft);
//            List<string> in_tags = new List<string>();
//            foreach(KeyValuePair<string, Tag> d in all){
//                if(d.Value.craft.Contains(craft_ref)){
//                    in_tags.Add(d.Key);
//                }
//            }
//            return in_tags;
//        }
//
//
//        //convert Dictionary<string, List<string>> to ConfigNodes and write to file
//        public static void save(){
//            ConfigNode nodes = new ConfigNode();
//            ConfigNode tag_nodes = new ConfigNode();
//            names.Clear();
//
//            foreach(KeyValuePair<string, Tag> pair in all){
//                ConfigNode node = new ConfigNode();
//                names.Add(pair.Key, pair.Key);
//                node.AddValue("tag_name", pair.Key);
//                foreach(string craft_ref in pair.Value.craft){
//                    node.AddValue("craft", craft_ref);
//                }
//                tag_nodes.AddNode("TAG", node);
//            }
//            nodes.AddNode("TAGS", tag_nodes);
//            nodes.Save(file_path);
//        }
//
//        //read ConfigNodes from file and convert to Dictonary<string, List<string>>
//        public static void load(){
//            all.Clear();
//            names.Clear();
//            ConfigNode raw_data = ConfigNode.Load(file_path);
//            ConfigNode tag_nodes = raw_data.GetNode("TAGS");
//
//            foreach(ConfigNode tag_node in tag_nodes.nodes){
//                string tag_name = tag_node.GetValue("tag_name");
//                string[] craft = tag_node.GetValues("craft");
//                Tag tag = new Tag(tag_name, new List<string>(craft));
//                names.Add(tag_name, tag_name);
//                all.Add(tag_name, tag);
//            }
//        }
//
//
//    }




}

