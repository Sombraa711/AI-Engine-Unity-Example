using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Gusto;
using Unity.Sentis;
using UnityEngine.UIElements;


public class gusto_sentis_rtmdet_example : MonoBehaviour
{    

    [DllImport("nms")]
    public static extern void nms(float[] boxes, int [] boxes_shape, float[] scores, int [] scores_shape, float scoreThr, float nmsThr, int[] indices, int[] indices_cls, int[] num_detections);
    // public ModelAsset modelAsset;
    Model runtimeModel;
    List<Model.Output> output;
    Worker worker;
    Tensor<float> inputTensor;
    float measure_time;
    float max_det_time;
    float min_det_time = 1000.0f;
    float total_det_time;
    int frame_count = 1;

    bool has_det = false;
    WebCamTexture m_webCamTexture;
    WebCamDevice[] m_devices;
    int camera_id = 0;
    
    [SerializeField] RectTransform m_debugRect;
    [SerializeField] RawImage m_rawImage;

    void OnGUI ()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "Running Platform: " + Application.platform);
        GUI.Label(new Rect(15, 150, 450, 100), "Time Estimation(ms): " + measure_time);
        GUI.Label(new Rect(15, 175, 450, 100), "Avg / Min / Max: " + total_det_time / frame_count + " / " + min_det_time + " / " + max_det_time);
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        Debug.Log("Running Platform: " + Application.platform);
        m_devices = WebCamTexture.devices;

        if (m_devices.Length == 0)
        {
            throw new Exception("No camera device found");
        }

        int max_id = m_devices.Length - 1;
        if (camera_id > max_id)
        {
            if (m_devices.Length == 1)
            {
                throw new Exception("Camera with id " + camera_id + " not found. camera_id value should be 0");
            }
            else
            {
                throw new Exception("Camera with id " + camera_id +
                                    " not found. camera_id value should be between 0 and " + max_id.ToString());
            }
        }

        m_webCamTexture = new WebCamTexture();

        m_webCamTexture.Play(); //Start capturing image using webcam
        inputTensor = new Tensor<float>(new TensorShape(1, 3, 320, 320));
        // Debug.Log($"modelAssets: {(modelAsset != null ? modelAsset.name : "<NULL>")}");
        // runtimeModel = ModelLoader.Load(modelAsset);
        
        runtimeModel = ModelLoader.Load(Gusto.Utility.retrieve_streamingassets_data("Weights/rtmdet_t_v6_20241023.sentis"));
        output = runtimeModel.outputs;

        for (int i = 0; i < output.Count; i++)
        {
            Debug.Log("output: " + output[i].name);
        }
        Debug.Log("output: " + output);
        worker = new Worker(runtimeModel, BackendType.CPU);
    }
    bool inferencePending = false;
    List<Tensor<float>> outputTensors = new List<Tensor<float>>();
    float start_time = 0.0f;
    float end_time = 0.0f;

    void Update()
    {
        if (m_webCamTexture.isPlaying == false)
        {
            return;
        }

        var rect = m_rawImage.rectTransform.rect;
        m_rawImage.texture = m_webCamTexture; //display the image on the RawImage

        float h_ratio = 1 / 320.0f;
        float w_ratio = 1 / 320.0f;

        TextureConverter.ToTensor(m_webCamTexture, inputTensor, new TextureTransform());


        if (!inferencePending)
        {
            start_time = Time.realtimeSinceStartup;
            worker.Schedule(inputTensor);

            for (int i = 0; i < output.Count; i++)
            {
                var outputTensor = worker.PeekOutput(output[i].name) as Tensor<float>;
                // outputTensor.ReadbackRequest();
                // outputTensor.ReadbackAndClone(); // not blocking
                outputTensor.ReadbackRequest();
                outputTensors.Add(outputTensor);
                // var cpuCopyTensor = await outputTensor.ReadbackAndCloneAsync();
            }
            inferencePending = true;
        }

        if (inferencePending) 
        {
            bool NotReady = false;
            // Debug.Log(outputTensors.Count);
            float[] dets = new float[10000];
            int[] dets_shape = new int[3];
            float[] scores = new float[10000];
            int[] scores_shape = new int[3];
            /*
                dets: (1, 2100, 4)
                labels: (1, 3, 2100)
            */
            for (int i = 0; i < outputTensors.Count; i++)
            {
                if (outputTensors[i].IsReadbackRequestDone()){
                    if (output[i].name == "dets"){
                        dets = outputTensors[i].DownloadToArray();
                        dets_shape = outputTensors[i].shape.ToArray();
                    }else if (output[i].name == "labels"){
                        scores = outputTensors[i].DownloadToArray();
                        scores_shape = outputTensors[i].shape.ToArray();
                    }

                }else{
                    NotReady = true;
                }
            }
            if (!NotReady)
            {
                int[] indices = new int[100 * dets_shape[0]];
                int[] indices_cls = new int[100 * dets_shape[0]];
                int[] num_detections = new int[dets_shape[0]];
                nms(dets, dets_shape, scores, scores_shape, 0.5f, 0.5f, indices, indices_cls, num_detections);

                Debug.Log("num_detections: " + num_detections[0]);
                Debug.Log("valid index: " + indices[0]);
                for (int i = 0; i < num_detections[0]; i++)
                {
                    var x1 = Mathf.Lerp(rect.xMin, rect.xMax, dets[indices[i] * 4] * w_ratio);
                    var y1 = Mathf.Lerp(rect.yMin, rect.yMax, dets[indices[i] * 4 + 1] * h_ratio);
                    var x2 = Mathf.Lerp(rect.xMin, rect.xMax, dets[indices[i] * 4 + 2] * w_ratio);
                    var y2 = Mathf.Lerp(rect.yMin, rect.yMax, dets[indices[i] * 4 + 3] * h_ratio);
                    // Debug.Log("x1: " + x1 + " y1: " + y1 + " x2: " + x2 + " y2: " + y2);
                    var finalRect = Rect.MinMaxRect(Mathf.Min(x1, x2), Mathf.Min(y1, y2), Mathf.Max(x1, x2), Mathf.Max(y1, y2));
                    m_debugRect.anchoredPosition = finalRect.center;
                    m_debugRect.sizeDelta = finalRect.size;
                }
                inferencePending = false;
                outputTensors.Clear(); 

                end_time = Time.realtimeSinceStartup;
                measure_time = (end_time - start_time) * 1000.0f;
                min_det_time = Math.Min(min_det_time, measure_time);
                max_det_time = Math.Max(max_det_time, measure_time);
                total_det_time += measure_time;
            }
        }
        frame_count += 1;

        

    }

    void OnDestroy()
    {
        // worker.Dispose();
        // inputTensor.Dispose();
    }
}

