using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Gusto
{

    internal sealed partial class GustoNet
    {
        #if UNITY_IOS && !UNITY_EDITOR
            const string gusto_engine_unity = "__Internal";
        #else
            const string gusto_engine_unity = "libGustoEngineUnity";
        #endif
        [DllImport(gusto_engine_unity)]
        public static extern Utility.ErrorType Gusto_Model_Compile(out IntPtr net, string modelpath, string cls_names_path);

        [DllImport(gusto_engine_unity)]
        public static extern void Gusto_Model_Inference_Image(
            IntPtr net,
            string ImagePath
        );

        [DllImport(gusto_engine_unity)]
        public static extern void Gusto_Model_Inference(
            IntPtr net,
            string ImagePath,
            Color32[] bitmap, int height, int width
        );
        // [DllImport("__Internal")]
        // public static extern Utility.ErrorType net_compile(
        //     IntPtr net,
        //     int inpHeight, int inpWidth,
        //     float confThreshold, float nmsThreshold, 
        //     StringBuilder modelpath, StringBuilder cls_names_path, int len_string = 1024);

        // [DllImport("__Internal")]
        // public static extern float infer(
        //     IntPtr net, 
        //     Color32[] bitmap, int height, int width, 
        //     float[] bboxes, float[] confidences, int[] classIds, int[] proposal_len
        // );

    }

}

