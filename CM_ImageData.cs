using System;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Threading;

using UnityEngine;
using KatLib;




namespace CraftManager
{

    public class Image
    {

        public FileInfo file;
        public Texture2D texture;
        public bool loaded = false;

        public string name { get { return file.Name; } }
        public string path { get { return file.FullName;} }

        public Image(FileInfo image_file, Texture2D image_texture){
            file = image_file; 
            texture = image_texture;
        }

        //Takes a PicData object and reads the image bytes.
        //If the image is already a jpg then it just returns the bytes, otherwise it is converted into a jpg first
        public byte[] read_as_jpg(){
            byte[] original_image = File.ReadAllBytes(file.FullName);
            if(file.Extension.ToLower() == ".jpg"){
                return original_image;
            } else{
                CraftManager.log("compressing: " + file.Name);
                Texture2D converter = new Texture2D(2, 2);
                converter.LoadImage(original_image);
                return converter.EncodeToJPG();
            }
        }

    }

    public class ImageData
    {

        private string[] file_types = new string[] { "jpg", "png" };
        public List<Image> images = new List<Image>();
        public List<List<Image>> grouped_images = new List<List<Image>>();

        public ImageData(){
            prepare_images();
        }


        //Get file info for all files of defined file_types within the screenshot dir
        private void prepare_images(){
            DirectoryInfo dir = new DirectoryInfo(CraftManager.screenshot_dir);
            List<FileInfo> files = new List<FileInfo>();

            foreach(string file_type in file_types){
                foreach(FileInfo file in dir.GetFiles ("*." + file_type)){
                    files.Add(file);
                }
            }
            files.Sort((x, y) => x.CreationTime.CompareTo(y.CreationTime));
            Texture2D image_placeholder = (Texture2D)StyleSheet.assets["image_placeholder"];

            foreach(FileInfo file in files){
                images.Add(new Image(file, image_placeholder));
            }

        }

        public List<List<Image>> get_grouped_images(int group_size){
            if(grouped_images.Count == 0 || grouped_images[0].Count != group_size){
                grouped_images = images_in_groups_of(group_size);
            }
            return grouped_images;
        }

        public List<List<Image>> images_in_groups_of(int group_size){
            List<List<Image>> grouped = new List<List<Image>>();
            List<Image> group = new List<Image>();
            for(int i = 0; i < images.Count; i++){
                group.Add(images[i]);
                if((i+1) % group_size == 0){
                    if(group.Count != 0){
                        grouped.Add(new List<Image>(group.ToArray()));
                    }
                    group.Clear();
                }
            }
            return grouped;
        }

    }


}

