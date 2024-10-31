using UnityEngine;
using Unity.Sentis;

public class gusto_edit_model : MonoBehaviour
{

    public ModelAsset modelAsset;
    void Start()
    {
        // Load the source model from the model asset
        Model model = ModelLoader.Load(modelAsset);

        // Define the functional graph of the model.
        var graph = new FunctionalGraph();

        var input = graph.AddInput(model.inputs[0].dataType, new TensorShape(1, 3, 320, 320));

        // Apply the model forward function to the inputs to get the source model functional outputs.
        // Sentis will destructively change the loaded source model. To avoid this at the expense of
        // higher memory usage and compile time, use the Functional.ForwardWithCopy method.
        FunctionalTensor[] outputs = Functional.Forward(model, input);

        // Calculate the softmax of the first output with the functional API.
        // FunctionalTensor softmaxOutput = Functional.Softmax(outputs[0]);

        // Build the model from the graph using the `Compile` method with the desired outputs.
        var modelWithSoftmax = graph.Compile(outputs);
    }
}