using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Ncnn
{
    internal sealed partial class NcnnNet
    {

        [DllImport("GustoEngine")]
        public static extern NativeUtility.ErrorType new_ncnn_net(out IntPtr net);

        [DllImport("GustoEngine")]
        public static extern NativeUtility.ErrorType load_ncnn_model_param_mem(IntPtr net, byte[] param_mem);
        [DllImport("GustoEngine")]
        public static extern NativeUtility.ErrorType load_ncnn_model_mem(IntPtr net, byte[] param_mem);

        [DllImport("GustoEngine")]
        public static extern NativeUtility.ErrorType load_model_config_from_csharp(
            out IntPtr config,
            int inpHeight,
            int inpWidth,
            float confThreshold,
            float nmsThreshold
        );

        [DllImport("GustoEngine")]
        public static extern NativeUtility.ErrorType config_ncnn_net(IntPtr net, IntPtr config);


        [DllImport("GustoEngine")]
        public static extern void delete_ncnn_net(IntPtr net);


        [DllImport("GustoEngine")]
        public static extern void infer(
            IntPtr net, 
            Color32[] bitmap, int height, int width, 
            float[] bboxes, float[] confidences, int[] classIds, int[] proposal_len
        );
    
        [DllImport("GustoEngine")]
        public static extern void random_test(
            IntPtr net, 
            Color32[] bitmap, int height, int width, 
            float[] bboxes, float[] confidences, int[] classIds, int[] proposal_len);
    }

}

