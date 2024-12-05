using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using Gusto;

public class gusto_opencv_example : MonoBehaviour
{    
    #if UNITY_IOS && !UNITY_EDITOR
        const string libdet2d = "__Internal";
    #else
        const string libdet2d = "libdetection_csharp_example";
    #endif
    [DllImport(libdet2d)]
    public static extern void Open_Session(
        StringBuilder _detector_path,
        int input_w,
        int input_h
    );
    [DllImport(libdet2d)]
    public static extern void Start_Session(
        StringBuilder _frame_path
    );
    void Start()
    {
        StringBuilder face_detector_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("Weights/epoch_150_nonms_fp16.onnx"));
        
        // uint8 may be slower than fp16 since some of cpus from mobile devices do not support uint8
        // StringBuilder face_detector_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("Weights/epoch_150_nonms_uint8.onnx"));

        // StringBuilder _detector_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("Weights/rtmdet_t_v7_20241028_preprocessor.onnx"));
        Open_Session(
            face_detector_path,
            320,
            320
        );

    }
    void Update()
    {
        StringBuilder frame_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/demo.png"));
        Start_Session(frame_path);
    }
}