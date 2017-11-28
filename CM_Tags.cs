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
        public List<string> craft = new List<string>();
        public bool selected = false;

        public Tag(string tag_name){
            name = tag_name;
            craft = new List<string>();
        }

        public Tag(string tag_name, List<string> assign_craft){
            name = tag_name;
            craft = assign_craft;
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
            if(String.IsNullOrEmpty(new_name)){
                return "Name cannot be blank";
            } else if(new_name == name){
                return "200"; //do nothing if name is unchanged
            } else if(Tags.all.ContainsKey(new_name)){
                return "A tag with this name already exists";
            } else{                
                Tags.all.Remove(name);
                name = new_name;
                Tags.all.Add(new_name, this);
                Tags.save();
                return "200";
            }
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

        public static string file_path = Paths.joined(CraftManager.ksp_root, "GameData", "CraftManager", "craft.tags");
        public static Dictionary<string, Tag> all = new Dictionary<string, Tag>();
        public static Dictionary<string, string> names = new Dictionary<string, string>();

        public static string craft_reference_key(CraftData craft){            
            return craft.construction_type + "_" + craft.name;
        }

        public static List<string> selected_tags(){
            List<Tag> tags = new List<Tag>(Tags.all.Values);
            List<string> s_tags = new List<string>();
            foreach(Tag t in tags.FindAll(tag => tag.selected)){
                s_tags.Add(t.name);
            }
            return s_tags;
        }

        //add a new tag. 
        public static void add(string tag){
            if(!all.ContainsKey(tag)){
                all.Add(tag, new Tag(tag));
                save();
            }
        }

        //remove a tag (and all included craft references)
        public static void remove(string tag){
            if(all.ContainsKey(tag)){
                all.Remove(tag);
                save();
            }
        }

        //add a craft reference to a tag. creates the tag if it doesn't exist
        public static void tag_craft(CraftData craft, string tag){
            add(tag); //ensure tag exists, doesn't do anything if tag exists
            all[tag].add(craft_reference_key(craft));
            save();
        }
        public static void tag_craft(CraftData craft, List<string> tags){
            foreach(string tag_name in tags){
                Tags.tag_craft(craft, tag_name);
            }
        }

        //remove a craft reference from a tag. Does not remove tag if it is empty afterwards.
        public static void untag_craft(CraftData craft, string tag){
            if(all.ContainsKey(tag)){
                string craft_ref = craft_reference_key(craft);
                if(all[tag].craft.Contains(craft_ref)){
                    all[tag].remove(craft_ref);
                }
            }
            save();
        }

        public static List<string> remove_from_all_tags(CraftData craft){
            List<string> tags = Tags.for_craft(craft);
            foreach(string tagname in tags){
                Tags.untag_craft(craft, tagname);
            }
            return tags;
        }

        //get a list of tags for a given craft reference
        public static List<string> for_craft(CraftData craft){
            string craft_ref = craft_reference_key(craft);
            List<string> in_tags = new List<string>();
            foreach(KeyValuePair<string, Tag> d in all){
                if(d.Value.craft.Contains(craft_ref)){
                    in_tags.Add(d.Key);
                }
            }
            return in_tags;
        }


        //convert Dictionary<string, List<string>> to ConfigNodes and write to file
        public static void save(){
            ConfigNode nodes = new ConfigNode();
            ConfigNode tag_nodes = new ConfigNode();
            names.Clear();

            foreach(KeyValuePair<string, Tag> pair in all){
                ConfigNode node = new ConfigNode();
                names.Add(pair.Key, pair.Key);
                node.AddValue("tag_name", pair.Key);
                foreach(string craft_ref in pair.Value.craft){
                    node.AddValue("craft", craft_ref);
                }
                tag_nodes.AddNode("TAG", node);
            }
            nodes.AddNode("TAGS", tag_nodes);
            nodes.Save(file_path);
        }

        //read ConfigNodes from file and convert to Dictonary<string, List<string>>
        public static void load(){
            all.Clear();
            names.Clear();
            ConfigNode raw_data = ConfigNode.Load(file_path);
            ConfigNode tag_nodes = raw_data.GetNode("TAGS");

            foreach(ConfigNode tag_node in tag_nodes.nodes){
                string tag_name = tag_node.GetValue("tag_name");
                string[] craft = tag_node.GetValues("craft");
                Tag tag = new Tag(tag_name, new List<string>(craft));
                names.Add(tag_name, tag_name);
                all.Add(tag_name, tag);
            }
        }


    }




}

