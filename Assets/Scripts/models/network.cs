using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Gusto
{
    internal sealed partial class GustoNet
    {

        [DllImport("GustoEngine")]
        public static extern Utility.ErrorType net_new(out IntPtr net);
        [DllImport("GustoEngine")]
        public static extern Utility.ErrorType net_compile(
            IntPtr net,
            int inpHeight, int inpWidth,
            float confThreshold, float nmsThreshold, 
            StringBuilder modelpath, StringBuilder cls_names_path, int len_string = 1024);

        [DllImport("GustoEngine")]
        public static extern float infer(
            IntPtr net, 
            Color32[] bitmap, int height, int width, 
            float[] bboxes, float[] confidences, int[] classIds, int[] proposal_len
        );

    }

}

