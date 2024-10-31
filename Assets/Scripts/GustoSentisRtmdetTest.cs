using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;
using Unity.Collections;
using Cysharp.Threading.Tasks;

public class GustoSentisRtmdetTest : MonoBehaviour
{

    [DllImport("nms")]
    public static extern void nms(float[] boxes, int[] boxes_shape, float[] scores, int[] scores_shape, float scoreThr, float nmsThr, int[] indices, int[] indices_cls, int[] num_detections);
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

    WebCamTexture m_webCamTexture;
    WebCamDevice[] m_devices;
    int camera_id = 0;
    float start_time = 0.0f;
    float end_time = 0.0f;
    float[] detsArray, scoresArray;

    [SerializeField] RectTransform m_debugRect;
    [SerializeField] RawImage m_rawImage;

    void OnGUI()
    {
        GUI.Label(new Rect(15, 125, 450, 100), "Running Platform: " + Application.platform);
        GUI.Label(new Rect(15, 150, 450, 100), "Time Estimation(ms): " + measure_time);
        GUI.Label(new Rect(15, 175, 450, 100), "Avg / Min / Max: " + total_det_time / frame_count + " / " + min_det_time + " / " + max_det_time);
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void OnEnable() => AsyncLoop().Forget();

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

        runtimeModel = ModelLoader.Load(Gusto.Utility.retrieve_streamingassets_data("Weights/rtmdet_t_v7_20241028.sentis"));
        output = runtimeModel.outputs;

        for (int i = 0; i < output.Count; i++)
        {
            Debug.Log("output: " + output[i].name);
        }
        Debug.Log("output: " + output);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }

    async UniTaskVoid AsyncLoop()
    {
        while (isActiveAndEnabled)
        {
            await UniTask.Yield(destroyCancellationToken);
            try
            {
                if (!m_webCamTexture.isPlaying) continue;
                var rect = m_rawImage.rectTransform.rect;
                m_rawImage.texture = m_webCamTexture; //display the image on the RawImage
                TextureConverter.ToTensor(m_webCamTexture, inputTensor, new TextureTransform());
                start_time = Time.realtimeSinceStartup;
                worker.Schedule(inputTensor);
                var ((dets, dets_shape), (scores, scores_shape)) = await UniTask.WhenAll(ReadbackAsync("dets"), ReadbackAsync("labels"));
                if (detsArray == null || detsArray.Length != dets.Length) detsArray = new float[dets.Length];
                if (scoresArray == null || scoresArray.Length != scores.Length) scoresArray = new float[scores.Length];
                dets.CopyTo(detsArray);
                scores.CopyTo(scoresArray);
                var pool = ArrayPool<int>.Shared;
                int[] indices = pool.Rent(100 * dets_shape[0]);
                int[] indices_cls = pool.Rent(100 * dets_shape[0]);
                int[] num_detections = pool.Rent(dets_shape[0]);
                try
                {
                    nms(detsArray, dets_shape, scoresArray, scores_shape, 0.5f, 0.5f, indices, indices_cls, num_detections);

                    Debug.Log("num_detections: " + num_detections[0]);
                    Debug.Log("valid index: " + indices[0]);
                    for (int i = 0; i < num_detections[0]; i++)
                    {
                        var x1 = dets[indices[i] * 4] / 320F;
                        var y1 =  dets[indices[i] * 4 + 1] / 320F;
                        var x2 = dets[indices[i] * 4 + 2] / 320F;
                        var y2 =  dets[indices[i] * 4 + 3] / 320F;
                        Debug.Log("x1: " + x1 + " y1: " + y1 + " x2: " + x2 + " y2: " + y2);
                        var finalRect = new Rect(x1 * rect.width, y1 * rect.height, (x2 - x1) * rect.width, (y2 - y1) * rect.height);
                        m_debugRect.anchoredPosition = finalRect.center;
                        m_debugRect.sizeDelta = finalRect.size;
                    }
                }
                finally
                {
                    pool.Return(indices);
                    pool.Return(indices_cls);
                    pool.Return(num_detections);
                }

                end_time = Time.realtimeSinceStartup;
                measure_time = (end_time - start_time) * 1000.0f;
                min_det_time = Math.Min(min_det_time, measure_time);
                max_det_time = Math.Max(max_det_time, measure_time);
                total_det_time += measure_time;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                frame_count++;
            }
        }
    }

    async UniTask<(NativeArray<float>, int[])> ReadbackAsync(string name)
    {
        var output = await worker.PeekOutput(name).ReadbackAsync() as Tensor<float>;
        destroyCancellationToken.ThrowIfCancellationRequested();
        return (output.DownloadToNativeArray(), output.shape.ToArray());
    }

    void OnDestroy()
    {
        // worker.Dispose();
        // inputTensor.Dispose();
    }
}

