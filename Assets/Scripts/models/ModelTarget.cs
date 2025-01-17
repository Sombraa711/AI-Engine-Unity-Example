using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Gusto{
    internal sealed partial class  ModelTarget{
        #if UNITY_IOS && !UNITY_EDITOR
            const string gusto_model_target_unity = "__Internal";
        #else
            const string gusto_model_target_unity = "libmodel_target";
        #endif
        [DllImport(gusto_model_target_unity)]
        public static extern int GustoModelTargetInit(out IntPtr net, int height, int width);

        [DllImport(gusto_model_target_unity)]
        public static extern int CADModelInit(
            IntPtr net,
            string model_name, string model_path, string model_metadata_path, // model_info,
            float start_threshold,
            float track_threshold,
            float[] init_pose_ret_ptr // init_pose for unity rendering
        );

        [DllImport(gusto_model_target_unity)]
        public static extern int TrackerInit(
            IntPtr net,
            float fov
        );
        [DllImport(gusto_model_target_unity)]
        public static extern int reinit(
            IntPtr net
        );
        [DllImport(gusto_model_target_unity)]
        public static extern int track(
            IntPtr net,
            IntPtr bitmap,
            float[] pose_ret_ptr,
            float[] confidences,
            bool debug_opengl
        );
    }
}