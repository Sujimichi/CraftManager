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
//        private static Texture2D image_placeholder = (Texture2D)StyleSheet.assets["image_placeholder"];
        public FileInfo file;
        public Texture2D texture = new Texture2D(125, 125);
        public bool loaded = false;

        public string name { get { return file.Name; } }
        public string path { get { return file.FullName;} }

        public Image(FileInfo image_file){
            file = image_file; 
        }

        //Takes a PicData object and reads the image bytes.
        //If the image is already a jpg then it just returns the bytes, otherwise it is converted into a jpg first
        public byte[] read_as_jpg(){
            byte[] original_image = File.ReadAllBytes(file.FullName);
            if(file.Extension.ToLower() == ".jpg"){
                return original_image;
            } else{
                Texture2D converter = new Texture2D(2, 2);
                converter.LoadImage(original_image);
                return converter.EncodeToJPG();
            }
        }


        //Does the loading of the picture onto the Texture2D object, returns IEnumerator as this is called in a Coroutine.
        public IEnumerator load_image(){            
            yield return true;                          //doesn't seem to matter what this returns
            byte[] pic_data = File.ReadAllBytes(file.FullName);  //read image file
            texture.LoadImage(pic_data);                //wop it all upside the texture.
            CraftManager.main_ui.image_data.images_being_loaded_count -=1;
            if(CraftManager.main_ui.image_data.images_being_loaded_count < 0){
                CraftManager.main_ui.image_data.images_being_loaded_count = 0;
            }
                
        }
    }

    public class ImageData
    {

        private string[] file_types = new string[] { "jpg", "png" };
        public List<Image> images = new List<Image>();
        public List<List<Image>> grouped_images = new List<List<Image>>();
        public int images_being_loaded_count = 0;

        public ImageData(){
            prepare_images();
        }


        //Get file info for all files of defined file_types within the screenshot dir
        private void prepare_images(){
            DirectoryInfo dir = new DirectoryInfo(CraftManager.screenshot_dir);
            List<FileInfo> files = new List<FileInfo>();
            images.Clear();
            grouped_images.Clear();

            foreach(string file_type in file_types){
                foreach(FileInfo file in dir.GetFiles ("*." + file_type)){
                    files.Add(file);
                }
            }
            files.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));

            foreach(FileInfo file in files){
                images.Add(new Image(file));
            }

        }

        public List<List<Image>> get_grouped_images(int group_size){
            if(grouped_images.Count == 0 || grouped_images[0].Count != group_size){                
                grouped_images = ImageData.images_in_groups_of(images, group_size);
            }
            return grouped_images;
        }

        public static List<List<Image>> images_in_groups_of(List<Image> source_images, int group_size){
            List<List<Image>> grouped = new List<List<Image>>();
            List<Image> group = new List<Image>();
            for(int i = 0; i < source_images.Count; i++){
                group.Add(source_images[i]);
                if((i + 1) % group_size == 0 && group.Count != 0){
                    grouped.Add(new List<Image>(group.ToArray()));
                    group.Clear();
                } else if(i + 1 == source_images.Count && group.Count != 0){ //if this is the last image and the group isn't empty (but also isn't full) then add it to response 
                    grouped.Add(new List<Image>(group.ToArray()));
                }

            }
            return grouped;
        }

    }

    public class GrabImage : CMUI
    {
        private void Start(){
            window_title = null;

            window_pos = new Rect(Screen.width-220, 50, 200, 5);
            CraftManager.camera = this;
        }

        protected override void WindowContent(int win_id) { 
            label("Position the craft how you want and", "h2");
            section("Button", () =>{
                label("Take Screenshot", "button.text.large");
                GUILayout.Label(StyleSheet.assets["camera"], width(42f), height(42f));
            }, evt =>{
                if(evt.single_click){
                    grab_screenshot();
                }
            });
            button("cancel", close);
        }

        private void grab_screenshot(){
            hide();
            string filename = "screenshot - " + string.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now) + ".png";
            CraftManager.log("grabbing screenshot: " + filename);

            Application.CaptureScreenshot(filename);
//            ScreenCapture.CaptureScreenshot(filename);
            StartCoroutine(shutter(filename));        //shutter re-opens the windows. well, it's kinda the exact opposite of what a shutter does, but yeah....whatever
        }

        public IEnumerator shutter(string filename){    
            yield return true;                           //doesn't seem to matter what this returns
            Thread.Sleep(100);                           //delay before re-opening windows
            //Application.CaptureScreenshot seems insistant on plonking the picture in KSP_Data, so this next bit relocates the pic to join it's friends in the screenshot folder
            string origin_path = Paths.joined(KSPUtil.ApplicationRootPath, "KSP_Data", filename);    //location where screenshot is created (as a png)
            string png_path = Paths.joined(CraftManager.screenshot_dir, filename);                        //location where it will be moved to
            if(File.Exists((origin_path))){
                CraftManager.log("moving file: " + origin_path + " to: " + png_path);
                File.Move(origin_path, png_path);
            } else{                                                                                       //TODO find less hacky way of solving which Data folder to look in
                origin_path = Paths.joined(KSPUtil.ApplicationRootPath, "KSP_x64_Data", filename);        //location where screenshot is created (as a png)
                if(File.Exists((origin_path))){
                    CraftManager.log("moving file: " + origin_path + " to: " + png_path);
                    File.Move(origin_path, png_path);
                }
            }
            close();
        }

        public void close(){
            CraftManager.main_ui.show();
            GameObject.Destroy(CraftManager.camera);            
        }
    }

}

