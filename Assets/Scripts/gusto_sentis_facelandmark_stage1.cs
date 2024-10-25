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
using TMPro;


public class gusto_sentis_facelandmark : MonoBehaviour
{    
    // public ModelAsset modelAsset;
    Model runtimeModel;
    public ModelAsset modelAsset;
    List<Model.Output> output;
    double[,] anchors;
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
        inputTensor = new Tensor<float>(new TensorShape(1, 128, 128, 3));
        // Debug.Log($"modelAssets: {(modelAsset != null ? modelAsset.name : "<NULL>")}");

        runtimeModel = ModelLoader.Load(modelAsset);
        string anchors_bin_file = Gusto.Utility.retrieve_streamingassets_data("anchor.bin");
        anchors = Gusto.Utility.LoadBinaryFile2D(anchors_bin_file, 896, 4);
        // runtimeModel = ModelLoader.Load(Gusto.Utility.retrieve_streamingassets_data("Weights/face_detector.sentis"));
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


        TextureTransform face_landmark_transform = new TextureTransform();
        face_landmark_transform.SetTensorLayout(TensorLayout.NHWC);
        TextureConverter.ToTensor(m_webCamTexture, inputTensor, face_landmark_transform);


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
                regressors: (1,896,16)
                classificators: (1,896,1)
            */
            for (int i = 0; i < outputTensors.Count; i++)
            {
                if (outputTensors[i].IsReadbackRequestDone()){
                    if (output[i].name == "regressors"){
                        dets = outputTensors[i].DownloadToArray();
                        dets_shape = outputTensors[i].shape.ToArray();
                    }else if (output[i].name == "classificators"){
                        scores = outputTensors[i].DownloadToArray();
                        scores_shape = outputTensors[i].shape.ToArray();
                    }

                }else{
                    NotReady = true;
                }
            }
            if (!NotReady)
            {
                // Debug.Log("dets_shape: " + dets_shape[0] + " " + dets_shape[1] + " " + dets_shape[2]);
                // Debug.Log("scores_shape: " + scores_shape[0] + " " + scores_shape[1] + " " + scores_shape[2]);
                float [] boxes_drop_kpts = new float[dets_shape[0] * dets_shape[1] * 4];
                int [] boxes_drop_kpts_shape = {dets_shape[0], dets_shape[1], 4};
                int [] scores_shape_ = {scores_shape[0], scores_shape[2], scores_shape[1]};
                float[, ] boxes2d = new float[dets_shape[0] * dets_shape[1], 4];
                for (int i = 0; i < dets_shape[0] * dets_shape[1]; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        boxes2d[i, j] = dets[i * dets_shape[2] + j];
                    }
                }
                for (int i = 0; i < 10; i++)
                {
                    // $ string
                    Debug.Log($"i: {i}, boxes2d: {boxes2d[i, 0]} {boxes2d[i, 1]} {boxes2d[i, 2]} {boxes2d[i, 3]}");
                    // Debug.Log("boxes2d: " + boxes2d[i, 0] + " " + boxes2d[i, 1] + " " + boxes2d[i, 2] + " " + boxes2d[i, 3]);
                }
                float[, ] decoded_boxes = Gusto.Utility.decode_blazeface_output(boxes2d, anchors);
                // Debug.Log("decoded_boxes: " + decoded_boxes[307, 0] + " " + decoded_boxes[307, 1] + " " + decoded_boxes[307, 2] + " " + decoded_boxes[307, 3]);
                for (int i = 0; i < dets_shape[1]; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        boxes_drop_kpts[i * 4 + j] = decoded_boxes[i, j];
                        // if (i < 10)
                        // {
                        //     Debug.Log(i + " " + " " + j + " " + decoded_boxes[i, j]);
                        // }
                    }
                }
                int[] indices = new int[100 * dets_shape[0]];
                int[] indices_cls = new int[100 * dets_shape[0]];
                int[] num_detections = new int[dets_shape[0]];
                Gusto.Utility.nms_with_sigmoid(boxes_drop_kpts, boxes_drop_kpts_shape, scores, scores_shape_, 0.8f, 0.5f, indices, indices_cls, num_detections);
                // find the max score
                // print("max_score: " + scores.Max() + " " + Array.IndexOf(scores, scores.Max()));
                Debug.Log("max_score indices: " + decoded_boxes[ Array.IndexOf(scores, scores.Max()), 0] + " " + decoded_boxes[ Array.IndexOf(scores, scores.Max()), 1] + " " + decoded_boxes[ Array.IndexOf(scores, scores.Max()), 2] + " " + decoded_boxes[ Array.IndexOf(scores, scores.Max()), 3]);

                // print("num_detections: " + num_detections[0]);
                for (int i = 0; i < num_detections[0]; i++)
                {
                    Debug.Log("indices: " + scores[i]);
                    // Debug.Log("detection indices: " + indices[i]);
                    // Debug.Log("detection boxes_drop_kpts: " + boxes_drop_kpts[indices[i] * 4] + " " + boxes_drop_kpts[indices[i] * 4 + 1] + " " + boxes_drop_kpts[indices[i] * 4 + 2] + " " + boxes_drop_kpts[indices[i] * 4 + 3]);
                    Debug.Log("detection decoded_boxes: " + decoded_boxes[indices[i], 0] + " " + decoded_boxes[indices[i], 1] + " " + decoded_boxes[indices[i], 2] + " " + decoded_boxes[indices[i], 3]);
                    var x1 = Mathf.Lerp(rect.xMin, rect.xMax, decoded_boxes[indices[i], 0]);
                    var y1 = Mathf.Lerp(rect.yMin, rect.yMax, decoded_boxes[indices[i], 1]);
                    var x2 = Mathf.Lerp(rect.xMin, rect.xMax, decoded_boxes[indices[i], 2]);
                    var y2 = Mathf.Lerp(rect.yMin, rect.yMax, decoded_boxes[indices[i], 3]);
                    
                    Debug.Log("x1: " + x1 + " y1: " + y1 + " x2: " + x2 + " y2: " + y2);
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
        worker.Dispose();
        inputTensor.Dispose();
    }
}

