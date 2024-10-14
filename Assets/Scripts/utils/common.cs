using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Gusto
{
    internal sealed partial class Utility
        {
        public static string retrieve_streamingassets_data(string rel_path_to_streamingassets)
        {
            string datapath;
            if (Application.platform == RuntimePlatform.Android)
            {
                // Android
                string oriPath = System.IO.Path.Combine(Application.streamingAssetsPath, rel_path_to_streamingassets);
                
                // Android only use WWW to read file
                WWW reader = new WWW(oriPath);
                while ( ! reader.isDone) {}
                
                datapath = System.IO.Path.Combine(Application.persistentDataPath, rel_path_to_streamingassets);

                System.IO.FileInfo file = new System.IO.FileInfo(datapath);
                file.Directory.Create();
                System.IO.File.WriteAllBytes(datapath, reader.bytes);
                
            }else if(Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor){
                // Desktop
                datapath = System.IO.Path.Combine(Application.streamingAssetsPath, rel_path_to_streamingassets);
            }
            else{
                // iOS
                datapath = System.IO.Path.Combine(Application.streamingAssetsPath, rel_path_to_streamingassets);
            }
            return datapath;
        }
    }

}

