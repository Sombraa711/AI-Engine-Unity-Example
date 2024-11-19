using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Gusto;
using Unity.Sentis;
using Unity.Collections;
using UnityEngine.UIElements;
using TMPro;
using Cysharp.Threading.Tasks;

namespace Gusto{
    namespace ModelLib{
    class BaseSentisModel{
        protected Model model;
        protected Worker worker;
        protected Tensor<float> inputTensor;
        protected List<Model.Output> output;
        protected TextureTransform texture_transformer;
        
        protected int input_width;
        protected int input_height;

        protected async UniTask<int[]> ReadbackAsync(string name, StrongBox<float[]> array = null)
        {
            var output = await worker.PeekOutput(name).ReadbackAsync() as Tensor<float>;
            var na = output.DownloadToNativeArray();
            if (array.Value == null || array.Value.Length < na.Length) array.Value = new float[na.Length];
            na.AsSpan().CopyTo(array.Value);
            return output.shape.ToArray();
        }

        public void LoadModel(string modelPath){
            model = ModelLoader.Load(modelPath);
            output = model.outputs;

            for (int i = 0; i < output.Count; i++)
            {
                Debug.Log("output: " + output[i].name);
            }
            Debug.Log("output: " + output);

            worker = new Worker(model, BackendType.CPU);
            return ;
        }
        virtual public void AsyncInfer(WebCamTexture m_webCamTexture){
            // frame_buffer = RenderTexture.GetTemporary(m_webCamTexture.width, m_webCamTexture.height, 0, RenderTextureFormat.ARGB32);
            // Graphics.Blit(m_webCamTexture, frame_buffer);
            TextureConverter.ToTensor(m_webCamTexture, inputTensor, texture_transformer);
            worker.Schedule(inputTensor);
            return ;
        }
        public void ReleaseModel(){
            inputTensor.Dispose();
            worker.Dispose();
            return ;
        }
    }

    class FaceDetector : BaseSentisModel 
    {
        public StrongBox<float[]> detsBox = new(), sourcesBox = new();
        public float[, ] anchors;
        public void LoadModel(string modelPath, string anchorsPath){
            base.LoadModel(modelPath);
            texture_transformer = new TextureTransform();
            texture_transformer.SetTensorLayout(TensorLayout.NHWC);
            anchors = Utility.LoadBinaryFile2D(anchorsPath, 896, 4);
            input_width = 128;
            input_height = 128;
            inputTensor = new Tensor<float>(new TensorShape(1, input_width, input_height, 3));
            return ;
        }

        


        public async UniTask<(List<Rect> bboxes, List<float> scores)> AsyncFetchOutput(float score_threshold = 0.8f, float nms_threshold = 0.5f, int max_dets = 100, bool is_debug = false){
            var (dets_shape, scores_shape) = await UniTask.WhenAll(ReadbackAsync("regressors", detsBox), ReadbackAsync("classificators", sourcesBox));
            var detsArray = detsBox.Value;
            var scoresArray = sourcesBox.Value;


            float [] boxes_drop_kpts = new float[dets_shape[0] * dets_shape[1] * 4];
            int [] boxes_drop_kpts_shape = {dets_shape[0], dets_shape[1], 4};
            int [] scores_shape_ = {scores_shape[0], scores_shape[2], scores_shape[1]};
            float[, ] boxes2d = new float[dets_shape[0] * dets_shape[1], 4];
            for (int i = 0; i < dets_shape[0] * dets_shape[1]; i++){
                for (int j = 0; j < 4; j++){
                    boxes2d[i, j] = detsArray[i * dets_shape[2] + j];
                }
            }
            float[, ] decoded_boxes = Gusto.Utility.decode_blazeface_output(boxes2d, anchors);
            for (int i = 0; i < dets_shape[1]; i++){
                for (int j = 0; j < 4; j++){
                    boxes_drop_kpts[i * 4 + j] = decoded_boxes[i, j];
                }
            }
            var pool = ArrayPool<int>.Shared;
            int[] indices = pool.Rent(max_dets * dets_shape[0]);
            int[] indices_cls = pool.Rent(max_dets * dets_shape[0]);
            int[] num_detections = pool.Rent(dets_shape[0]);
            List<Rect> ret_bboxes = new List<Rect>();
            List<float> ret_scores = new List<float>();
            try
            {
                Gusto.Utility.nms_with_sigmoid(boxes_drop_kpts, boxes_drop_kpts_shape, scoresArray, scores_shape_, score_threshold, nms_threshold, indices, indices_cls, num_detections);
                for (int i = 0; i < num_detections[0]; i++){
                    var bbox = Rect.MinMaxRect(decoded_boxes[indices[i], 1], decoded_boxes[indices[i], 0], decoded_boxes[indices[i], 3], decoded_boxes[indices[i], 2]);            
                    ret_bboxes.Add(bbox);
                    ret_scores.Add(scoresArray[indices[i]]);      
                    if (is_debug){
                        Debug.Log("scores: " + scoresArray[indices[i]]);
                        Debug.Log("detection decoded_boxes: " + decoded_boxes[indices[i], 0] + " " + decoded_boxes[indices[i], 1] + " " + decoded_boxes[indices[i], 2] + " " + decoded_boxes[indices[i], 3]);
                    }
                }  
            }
            finally
            {
                pool.Return(indices);
                pool.Return(indices_cls);
                pool.Return(num_detections);
            }
            
            return (ret_bboxes, ret_scores);
        }
    }
    class FaceLandMarker : BaseSentisModel{
        /*
            outputname: Identity
            name: face points: (1, 1, 1, 1434)
            outputname: Identity_1
            name: tougue out of mouth: (1, 1, 1, 1)
            outputname: Identity_2
            name: score: (1, 1)
        */
        public Matrix4x4 face_geometry_pose_mat;
        public StrongBox<float[]> detsBox = new(), sourcesBox = new();

        private IntPtr face_geometry_calculator;
        private int calculator_frame_width;
        private int calculator_frame_height;
        private float x_bias;
        private float y_bias;
        private bool bias_lock;
        public void LoadModel(string modelPath, string face_mesh_metadata_path){
            base.LoadModel(modelPath);
            texture_transformer = new TextureTransform();
            texture_transformer.SetTensorLayout(TensorLayout.NHWC);
            input_width = 256;
            input_height = 256;
            inputTensor = new Tensor<float>(new TensorShape(1, input_width, input_height, 3));
            
            Utility.face_mesh_calculator_new(out face_geometry_calculator);
            StringBuilder sb = new StringBuilder(1024);
            sb.Append(Utility.retrieve_streamingassets_data(face_mesh_metadata_path));
            Utility.face_mesh_calculator_open(face_geometry_calculator, sb, 1024);

            return ;
        }
        public Texture2D CropTexture(WebCamTexture texture, Rect bbox)
        {
            int x = Math.Max((int)((bbox.x - 0.125 * bbox.width) * texture.width), 0);
            int y = Math.Max((int)((bbox.y - 0.125 * bbox.height) * texture.height), 0);
            int width = Math.Max((int)(bbox.width * texture.width * 1.25), texture.width - x);
            int height = Math.Max((int)(bbox.height * texture.height), texture.height - y); 
            
            x_bias = x;
            y_bias = y;
            bias_lock = true;
            
            Texture2D croppedTexture = new Texture2D(width, height);
            Color[] pixels = texture.GetPixels(x, y, width, height);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            return croppedTexture;
        }
        public void AsyncInfer(Texture2D m_webCamTexture){
            // frame_buffer = RenderTexture.GetTemporary(m_webCamTexture.width, m_webCamTexture.height, 0, RenderTextureFormat.ARGB32);
            // Graphics.Blit(m_webCamTexture, frame_buffer);
            calculator_frame_width = m_webCamTexture.width;
            calculator_frame_height = m_webCamTexture.height;
            // Debug.Log("calculator_frame_width: " + calculator_frame_width + " calculator_frame_height: " + calculator_frame_height);
            TextureConverter.ToTensor(m_webCamTexture, inputTensor, texture_transformer);
            worker.Schedule(inputTensor);
            return ;
        }
        public async UniTask<(float[, ] landmarks, float score, Matrix4x4 pose)> AsyncFetchOutput(){
            var (dets_shape, scores_shape) = await UniTask.WhenAll(ReadbackAsync("Identity", detsBox), ReadbackAsync("Identity_2", sourcesBox));
            var dets = detsBox.Value;
            var scoresArray = sourcesBox.Value;

            float[, ] landmarks = new float[dets_shape[0] * dets_shape[1] * dets_shape[2] * dets_shape[3], 3];
            float score = scoresArray[0];


            for (int i = 0; i < dets_shape[0] * dets_shape[1] * dets_shape[2] * dets_shape[3] / 3; i++){
                var scale = 2 * (float)Math.Sqrt(calculator_frame_width / 1000);
                landmarks[i, 0] = (dets[i * 3] + x_bias) / calculator_frame_width;
                landmarks[i, 1] = (dets[i * 3 + 1] + y_bias) / calculator_frame_height;
                // landmarks[i, 2] = -dets[i * 3 + 2] / 500 * (float)scale;
                landmarks[i, 2] = dets[i * 3 + 2] / 500;
            }
            float [] pose = new float[16];
            Utility.ErrorType FaceMeshStatus = Utility.face_mesh_calculator_process(face_geometry_calculator, calculator_frame_width, calculator_frame_height, landmarks, 1, pose);
            // Utility.ErrorType FaceMeshStatus = Utility.face_mesh_calculator_process(face_geometry_calculator, 256, 256, landmarks, 1, pose);
            
            if (FaceMeshStatus == Utility.ErrorType.OK || FaceMeshStatus == Utility.ErrorType.PARTIAL_FAIL)
            {
                if (FaceMeshStatus == Utility.ErrorType.PARTIAL_FAIL)
                {
                    Debug.Log("FaceMeshStatus: " + FaceMeshStatus);
                }
                face_geometry_pose_mat = new Matrix4x4(
                    new Vector4(pose[0], pose[4], pose[8], pose[12]),
                    new Vector4(pose[1], pose[5], pose[9], pose[13]),
                    new Vector4(pose[2], pose[6], pose[10], pose[14]),
                    new Vector4(pose[3], pose[7], pose[11], pose[15])
                );
            }
                
            return (landmarks, score, face_geometry_pose_mat);
        }

    }


        
    //     private BaseSentisModel face_landmarker;
    //     private float[,] face_detector_anchors;
    //     private RenderTexture m_tempRenderTexture;

    // }
    }
}