using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using Gusto;

public class gusto_det2d_android_example : MonoBehaviour
{    
    #if UNITY_IOS && !UNITY_EDITOR
        const string libface_geometry_example_mobile = "__Internal";
    #else
        const string libface_geometry_example_mobile = "face_geometry_example_mobile";
    #endif
    [DllImport(libface_geometry_example_mobile)]
    public static extern void Open_Session(
        StringBuilder _face_detector_path,
        StringBuilder _face_landmarker_path, 
        StringBuilder _face_GeometryPipelineMetadata, 
        StringBuilder _anchor_path
    );
    [DllImport(libface_geometry_example_mobile)]
    public static extern void Start_Session(
        StringBuilder _frame_path
    );
    void Start()
    {
        StringBuilder face_detector_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/face_detector.onnx"));
        StringBuilder face_landmarker_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/face_landmarks_detector.onnx"));
        StringBuilder face_GeometryPipelineMetadata = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/geometry_pipeline_metadata_including_iris_landmarks.json"));
        StringBuilder anchor_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/anchor.bin"));
        Open_Session(
            face_detector_path,
            face_landmarker_path,
            face_GeometryPipelineMetadata,
            anchor_path
        );

    }
    void Update()
    {
        StringBuilder frame_path = new StringBuilder(Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/demo.png"));
        Start_Session(frame_path);
    }
}