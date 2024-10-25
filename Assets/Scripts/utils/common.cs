using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Gusto
{
    internal sealed partial class Utility
        {
        #if UNITY_IOS && !UNITY_EDITOR
        const string DLL_NAME = "__Internal";
        #else
        const string DLL_NAME = "nms";
        #endif

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
                // Windows
                datapath = System.IO.Path.Combine(Application.streamingAssetsPath, rel_path_to_streamingassets);
            }
            else{
                // iOS
                datapath = System.IO.Path.Combine(Application.streamingAssetsPath, rel_path_to_streamingassets);
            }
            return datapath;
        }

        public static float[,] LoadBinaryFile2D(string filePath, int rows, int cols)
        {
            // Step 1: Read the binary file into a byte array
            byte[] byteArray = File.ReadAllBytes(filePath);

            // Step 2: Convert the byte array to a float array
            int floatSize = sizeof(float);
            float[,] floatArray = new float[rows, cols];

            Buffer.BlockCopy(byteArray, 0, floatArray, 0, rows * cols * floatSize);
            
            return floatArray;
        }

        public static float[, ] decode_blazeface_output(float[, ] raw_boxes, float[, ] anchors)
        {
            for (int i = 0; i < 10; i++){
                    Debug.Log("Anchor: " + anchors[0, 0] + " " + anchors[i, 1] + " " + anchors[i, 2] + " " + anchors[i, 3]);
                }
            float[,] boxes = new float[raw_boxes.GetLength(0), 4];
            for (int i = 0; i < raw_boxes.GetLength(0); i++)
            {
                float x_center = raw_boxes[i, 0] / 128.0f * anchors[i, 2] + anchors[i, 0];
                float y_center = raw_boxes[i, 1] / 128.0f * anchors[i, 3] + anchors[i, 1];

                float w = raw_boxes[i, 2] / 128.0f * anchors[i, 2];
                float h = raw_boxes[i, 3] / 128.0f * anchors[i, 3];

                boxes[i, 0] = y_center - h / 2.0f;  // ymin
                boxes[i, 1] = x_center - w / 2.0f;  // xmin
                boxes[i, 2] = y_center + h / 2.0f;  // ymax
                boxes[i, 3] = x_center + w / 2.0f;  // xmax

            }
    
            return boxes;
        }

        // IF IOS: __Internal
        // IF ANDROID: nms
        [DllImport(DLL_NAME)]
        public static extern void nms(float[] boxes, int [] boxes_shape, float[] scores, int [] scores_shape, float scoreThr, float nmsThr, int[] indices, int[] indices_cls, int[] num_detections);
        
        [DllImport(DLL_NAME)]
        public static extern void nms_with_sigmoid(float[] boxes, int [] boxes_shape, float[] scores, int [] scores_shape, float scoreThr, float nmsThr, int[] indices, int[] indices_cls, int[] num_detections);
    }

}

