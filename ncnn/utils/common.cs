using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Ncnn
{
    internal sealed partial class NativeUtility
    {
        // [DllImport("GustoEngine")]
        // public static extern int read_frame_buffer_from_csharp(IntPtr net, Color32[] bitmap, int height, int width);

        [DllImport("GustoEngine")]
        public static extern ErrorType configure(out IntPtr net);
    }

}

