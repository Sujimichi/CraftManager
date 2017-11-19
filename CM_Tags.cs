using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using KatLib;


namespace CraftManager
{
    public class Tags
    {

        public static Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
        public static List<string> empty_tags = new List<string>();
        public static List<string> all = new List<string>();

        public static string file_path = Paths.joined(KSPUtil.ApplicationRootPath, "GameData", "CraftManager", "tags_data.json");


        public static void add(string tag){
            if(!data.ContainsKey(tag)){
                data.Add(tag, new List<string>());
            }
            save();
        }

        public static void remove(string tag){
            if(data.ContainsKey(tag)){
                data.Remove(tag);
            }
            save();
        }

        public static void tag_craft(string craft_ref, string tag){
            add(tag); //ensure tag exists
            data[tag].AddUnique(craft_ref);
            save();
        }

        public static void untag_craft(string craft_ref, string tag){
            if(data.ContainsKey(tag)){
                if(data[tag].Contains(craft_ref)){
                    data[tag].Remove(craft_ref);
                }
            }
            save();
        }


        public static List<string> tags_for(string craft_reference){
            List<string> in_tags = new List<string>();
            foreach(KeyValuePair<string, List<string>> d in data){
                if(d.Value.Contains(craft_reference)){
                    in_tags.Add(d.Key);
                }
            }
            return in_tags;
        }

        //repopulate list of all tags. called after save or load actions
        public static void update_tags_list(){
            all = new List<string>(data.Keys);
        }


        public static void save(){
            ConfigNode nodes = new ConfigNode();
            ConfigNode tag_nodes = new ConfigNode();

            foreach(KeyValuePair<string, List<string>> pair in data){
                ConfigNode node = new ConfigNode();
                node.name = "fibble";
                node.AddValue("tag_name", pair.Key);
//                if(pair.Value.Count > 0){
//                    node.AddValue("craft_refs", String.Join(",", pair.Value.ToArray()));
//                }
                foreach(string craft_ref in pair.Value){
                    node.AddValue("craft", craft_ref);
                }
                tag_nodes.AddNode("TAG", node);
            }
            nodes.AddNode("TAGS", tag_nodes);
            nodes.Save(file_path);

            update_tags_list();
        }

        public static void load(){
            data.Clear();
            ConfigNode raw_data = ConfigNode.Load(file_path);
            ConfigNode tag_nodes = raw_data.GetNode("TAGS");


            foreach(ConfigNode tag_node in tag_nodes.nodes){
//                CraftManager.log("tag: " + tag.GetValue("tag_name"));
                string tag = tag_node.GetValue("tag_name");
                Tags.add(tag);

                string[] craft = tag_node.GetValues("craft");
                foreach(string craft_ref in craft){
                    Tags.tag_craft(craft_ref, tag);
//                    CraftManager.log("craft_name: " + craft_ref);
                }
            }

            update_tags_list();
        }
    }
}

