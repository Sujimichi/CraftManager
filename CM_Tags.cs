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
        public List<string> craft = new List<string>();
        public bool selected = false;

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

        public int craft_count(string opt){
            if(opt == "filtered"){
                return CraftData.filtered.FindAll(c => this.craft.Contains(c.name)).Count;
            } else{
                return CraftData.all_craft.FindAll(c => this.craft.Contains(c.name)).Count;
            }
        }


    }


    public class Tags
    {

        public static Dictionary<string, Tag> all = new Dictionary<string, Tag>();
//        public static List<string> all = new List<string>();

        public static string file_path = Paths.joined(KSPUtil.ApplicationRootPath, "GameData", "CraftManager", "tag_data.json");

        public static string craft_reference_key(CraftData craft){
            return craft.name;
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
                all.Add(tag, new Tag(tag, new List<string>() ));
                //all.Add(tag, new List<string>());
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
        public static void tag_craft(string craft_ref, string tag){
            add(tag); //ensure tag exists
            all[tag].add(craft_ref);
            save();
        }

        //remove a craft reference from a tag. Does not remove tag if it is empty afterwards.
        public static void untag_craft(string craft_ref, string tag){
            if(all.ContainsKey(tag)){
                if(all[tag].craft.Contains(craft_ref)){
                    all[tag].remove(craft_ref);
                }
            }
            save();
        }

        //get a list of tags for a given craft reference
        public static List<string> tags_for(string craft_reference){
            List<string> in_tags = new List<string>();
            foreach(KeyValuePair<string, Tag> d in all){
                if(d.Value.craft.Contains(craft_reference)){
                    in_tags.Add(d.Key);
                }
            }
            return in_tags;
        }


        //convert Dictionary<string, List<string>> to ConfigNodes and write to file
        private static void save(){
            ConfigNode nodes = new ConfigNode();
            ConfigNode tag_nodes = new ConfigNode();

            foreach(KeyValuePair<string, Tag> pair in all){
                ConfigNode node = new ConfigNode();

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
            ConfigNode raw_data = ConfigNode.Load(file_path);
            ConfigNode tag_nodes = raw_data.GetNode("TAGS");

            foreach(ConfigNode tag_node in tag_nodes.nodes){
                string tag_name = tag_node.GetValue("tag_name");
                string[] craft = tag_node.GetValues("craft");
                Tag tag = new Tag(tag_name, new List<string>(craft));
                all.Add(tag_name, tag);
            }
        }


    }




}

