using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using Gusto;

public class GustoEngineUnityTest : MonoBehaviour
{    
    float measure_time;
    float max_det_time;
    float min_det_time = 1000.0f;
    float total_det_time;
    int frame_count = 1;
    float start_time = 0.0f;
    float end_time = 0.0f;
    bool warm_up = false;
    public IntPtr _net;

    void OnGUI()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "Running Platform: " + Application.platform);
        GUI.Label(new Rect(15, 150, 450, 100), "Time Estimation(ms): " + (int)measure_time);
        GUI.Label(new Rect(15, 175, 450, 100), "Avg / Min / Max: " + (int)total_det_time / frame_count + " / " + (int)min_det_time + " / " + (int)max_det_time);
    }
    void Start()
    {
        
        string model_path = Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/end2end_nonms_fp16.onnx");
        string config_path = Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/base_model_config.json");
        Gusto.GustoNet.Gusto_Model_Compile(out _net, model_path, config_path);
    }
    void Update()
    {

        string ImagePath = Gusto.Utility.retrieve_streamingassets_data("gusto_engine_test/demo.png");
        
        start_time = Time.realtimeSinceStartup;
        Gusto.GustoNet.Gusto_Model_Inference_Image(_net, ImagePath);
        end_time = Time.realtimeSinceStartup;

        if (!warm_up)
        {
            frame_count++;
            if (frame_count > 30)
            {
                warm_up = true;
                frame_count = 1;
            }
            return;
        }

        measure_time = (end_time - start_time) * 1000.0f;
        min_det_time = Math.Min(min_det_time, measure_time);
        max_det_time = Math.Max(max_det_time, measure_time);
        total_det_time += measure_time;
        frame_count++;
    }
}
