using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; // CustomYieldInstruction
using UnityEngine.Assertions;

namespace Unity.Barracuda {

/// <summary>
/// The main interface to execute neural networks (a.k.a models).
/// `IWorker` abstracts implementation details associated with various hardware devices (CPU, GPU and NPU in the future)
/// that can execute neural networks and provides clean and simple interface to:
///   1) specify inputs, 2) schedule the work and 3) retrieve outputs.
/// Internally `IWorker` translates description of the neural network provided by `Model` instance
/// into the set of operations that are sent to hardware device for execution in a non-blocking (asynchronous) manner.
///
/// The following is a simple example of image classification using pretrained neural network:
/// <code>
///     using UnityEngine;
///     using Unity.Barracuda;
///
///     public class ImageRecognitionSample : MonoBehaviour
///     {
///         // small ready to use image classification neural network in ONNX format can be obtained from https://github.com/onnx/models/tree/master/vision/classification/mobilenet
///         public NNModel onnxAsset;
///         public Texture2D imageToRecognise;
///
///         private IWorker worker;
///         void Start()
///         {
///             worker = onnxAsset.CreateWorker();
///         }
///
///         void Update()
///         {
///             // convert texture into Tensor of shape [1, imageToRecognise.height, imageToRecognise.width, 3]
///             using (var input = new Tensor(imageToRecognise, channels:3))
///             {
///                 // execute neural network with specific input and get results back
///                 var output = worker.Execute(input).PeekOutput();
///
///                 // the following line will access values of the output tensor causing the main thread to block until neural network execution is done
///                 var indexWithHighestProbability = output.ArgMax()[0];
///
///                 UnityEngine.Debug.Log($"Image was recognised as class number: {indexWithHighestProbability}");
///             }
///         }
///
///         void OnDisable()
///         {
///             worker.Dispose();
///         }
///     }
/// </code>
///
/// The following example demonstrates the use of coroutine to continue smooth app execution while neural network executes in the background:
/// <code>
///     using UnityEngine;
///     using Unity.Barracuda;
///     using System.Collections;
///     public class CoroutineImageRecognitionSample : MonoBehaviour
///     {
///         // small ready to use image classification neural network in ONNX format can be obtained from https://github.com/onnx/models/tree/master/vision/classification/mobilenet
///         public NNModel onnxAsset;
///         public Texture2D imageToRecognise;
///
///         private IWorker worker;
///         void Start()
///         {
///             worker = onnxAsset.CreateWorker();
///             StartCoroutine(ImageRecognitionCoroutine());
///         }
///
///         IEnumerator ImageRecognitionCoroutine()
///         {
///             while (true)
///             {
///                 // convert texture into Tensor of shape [1, imageToRecognise.height, imageToRecognise.width, 3]
///                 using (var input = new Tensor(imageToRecognise, channels:3))
///                 {
///                     // execute neural network with specific input and get results back
///                     var output = worker.Execute(input).PeekOutput();
///
///                     // allow main thread to run until neural network execution has finished
///                     yield return new WaitForCompletion(output);
///
///                     var indexWithHighestProbability = output.ArgMax()[0];
///                     UnityEngine.Debug.Log($"Image was recognised as class number: {indexWithHighestProbability}");
///                 }
///
///                 // wait until a new image is provided
///                 var previousImage = imageToRecognise;
///                 while (imageToRecognise == previousImage)
///                    yield return null;
///             }
///         }
///
///         void OnDisable()
///         {
///             worker.Dispose();
///         }
///     }
/// </code>
///
/// Use `WorkerFactory.CreateWorker` or `Model.CreateWorker` to create new worker instance.
/// </summary>
public interface IWorker : IDisposable
{
    #region Inputs
    /// <summary>
    /// Optional API to prepare network execution for inputs of particular shapes.
    /// Useful to initialize execution device ahead of the first call to `Execute`.
    /// </summary>
    /// <param name="inputShapes">Dictionary of tensor name -> input shapes</param>
    void PrepareForInput(IDictionary<string, TensorShape> inputShapes);

    /// <summary>
    /// Specify single tensor `x` as the only input for the network.
    /// Useful when network has only one input and caller does not need to specify input's name.
    /// </summary>
    /// <param name="x">input Tensor</param>
    void SetInput(Tensor x);

    /// <summary>
    /// Assign tensor `x` to the named input of the network. String `name` specifies the name of the input.
    /// </summary>
    /// <param name="name">Tensor name</param>
    /// <param name="x">Tensor</param>
    void SetInput(string name, Tensor x);
    #endregion

    #region Schedule the whole network
    /// <summary>
    /// Non-blocking API that schedules network execution in one go.
    /// </summary>
    /// <returns>IWorker instance</returns>
    IWorker Execute();


    /// <summary>
    /// Non-blocking API that takes single `input` tensor and schedules network execution in one go.
    /// Useful when network have only one input as input name is not needed.
    /// </summary>
    /// <param name="input">input Tensor</param>
    /// <returns>IWorker instance</returns>
    IWorker Execute(Tensor input);


    /// <summary>
    /// Non-blocking API that takes multiple input tensors and schedules network execution in one go.
    /// </summary>
    /// <param name="inputs">input Tensor Dictionary: name -> Tensor</param>
    /// <returns>IWorker instance</returns>
    IWorker Execute(IDictionary<string, Tensor> inputs);
    #endregion

    #region Schedule one layer at a time
    /// <summary>
    /// Non-blocking API that allows manual scheduling of the model one layer at the time.
    /// Call `MoveNext` on the `IEnumerator` obtained from calling this function to schedule next layer of the model.
    /// </summary>
    /// <returns>Manual schedule iterator</returns>
    IEnumerator StartManualSchedule();

    /// <summary>
    /// Non-blocking API that takes single `input` tensor and schedules network execution one layer at the time.
    /// Call `MoveNext` on the `IEnumerator` obtained from calling this function to schedule next layer of the model.
    /// </summary>
    /// <param name="input">input Tensor</param>
    /// <returns>Manual schedule iterator</returns>
    IEnumerator StartManualSchedule(Tensor input);

    /// <summary>
    /// Non-blocking API that takes mutliple input tensors and schedules network execution one layer at the time.
    /// Call `MoveNext` on the `IEnumerator` obtained from calling this function to schedule next layer of the model.
    /// </summary>
    /// <param name="inputs">input Tensor Dictionary: name -> Tensor</param>
    /// <returns>Manual schedule iterator</returns>
    IEnumerator StartManualSchedule(IDictionary<string, Tensor> inputs);

    /// <summary>
    /// Non-blocking API that starts immediate execution on the part of the network that was scheduled so far.
    /// Optional `blocking` flag can force this function to block until execution is complete.
    /// </summary>
    /// <param name="blocking">if blocking True, wait for completion</param>
    void FlushSchedule(bool blocking = false);

    /// <summary>
    /// Reports the fraction (from 0.0 to 1.0) of the model that was scheduled for the execution since the last call to `StartManualSchedule`.
    /// This property will return 0.0 immediately after calling `StartManualSchedule` and will return 1.0 once the complete model was scheduled.
    /// This property will monotonuosly increase with the every iteration of `IEnumerator` that was obtained by calling `StartManualSchedule`.
    /// </summary>
    float scheduleProgress { get; }
    #endregion

    #region Outputs
    /// <summary>
    /// Non-blocking API that returns a reference to the main output tensor. This reference will be valid only until the next `Execute()` or `Dispose()` method is called on the worker.
    /// Useful when network has only one output.
    /// IMPORTANT: if you want tensor to outlive the worker, use `CopyOutput()` method or follow with `TakeOwnership()` call on the tensor.
    /// </summary>
    /// <returns>output Tensor</returns>
    Tensor PeekOutput();

    /// <summary>
    /// Non-blocking API that returns a reference to output tensor by specified `name`. This reference will be valid only until the next `Execute()` or `Dispose()` method is called on the worker.
    /// IMPORTANT: if you want tensor to outlive the worker, use `CopyOutput()` method or follow with `TakeOwnership()` call on the tensor.
    /// </summary>
    /// <param name="name">output name</param>
    /// <returns>output Tensor</returns>
    Tensor PeekOutput(string name);
    #endregion

    /// <summary>
    /// Returns references to constants tensors for a layer. This reference might be valid only until the next `Execute()` or `Dispose()` method is called on the worker.
    /// IMPORTANT: if you want tensor to outlive the worker, use `CopyOutput()` method or follow with `TakeOwnership()` call on the tensor, also worker Execute()
    /// or PrepareForInput() should have been called at least once for the tensors to exist.
    /// </summary>
    /// <param name="layerName">Layer name</param>
    /// <returns>array of constant Tensors</returns>
    Tensor[] PeekConstants(string layerName);

    /// <summary>
    /// Returns a string summary after execution.
    /// </summary>
    /// <returns>string summary after execution</returns>
    string Summary();
}

/// <summary>
/// IWorker interface extensions
/// </summary>
public static class WorkerExtensions
{
    // @TODO: add optional targetDevice argument of type WorkerFactory.Device
    /// <summary>
    /// Returns CPU copy of the first output tensor.
    /// This method is a blocking call and will wait until network execution is completed.
    /// Useful when network has only one output.
    /// </summary>
    /// <param name="worker">IWorker</param>
    /// <returns>output Tensor</returns>
    public static Tensor CopyOutput(this IWorker worker)
    {
        // @TODO: implement as PeekOutput()+DeepCopy() instead of Unpin()+TakeOwnership()
        var output = worker.PeekOutput();
        output.DetachFromDevice(); // detach will readback to CPU and
                                   // give allocator a chance to reuse allocated buffer
        output.TakeOwnership();
        return output;
    }

    // @TODO: add optional targetDevice argument of type WorkerFactory.Device
    /// <summary>
    /// Returns CPU copy of output tensor by name.
    /// This method is a blocking call and will wait until network execution is completed.
    /// </summary>
    /// <param name="worker">IWorker</param>
    /// <param name="name">output Tensor name</param>
    /// <returns>output Tensor</returns>
    public static Tensor CopyOutput(this IWorker worker, string name)
    {
        // @TODO: implement as PeekOutput()+DeepCopy() instead of Unpin()+TakeOwnership()
        var output = worker.PeekOutput(name);
        output.DetachFromDevice(); // detach will readback to CPU and
                                   // give allocator a chance to reuse allocated buffer
        output.TakeOwnership();
        return output;
    }
}

/// <summary>
/// Interface for device dependent representation of Tensor data.
/// </summary>
public interface ITensorData : IDisposable
{
    /// <summary>
    /// Reserve uninitialized memory.
    /// </summary>
    /// <param name="count">element count to reserve</param>
    void Reserve(int count);

    /// <summary>
    /// Initialize with `data`.
    /// `shape` is the TensorShape (and thus length) of the data to copy.
    /// `managedBufferStartIndex` is the offset where to start the copy in the `data`
    /// </summary>
    /// <param name="data">data as `float` array</param>
    /// <param name="shape">Tensor shape</param>
    /// <param name="managedBufferStartIndex">managed buffer start index</param>
    void Upload(float[] data, TensorShape shape, int managedBufferStartIndex = 0);

    /// <summary>
    /// Schedule an asynchronous download from device memory.
    /// `count` is the number of element to readback.
    /// </summary>
    /// <param name="count">count of elements to download</param>
    /// <returns>`false` until data from device arrives to CPU and is ready for access</returns>
    bool ScheduleAsyncDownload(int count);

    /// <summary>
    /// Returns an array filled with the values of a tensor.
    /// Depending on the implementation and underlying device this array might be a copy or direct reference to the tensor values.
    /// This is a blocking call, unless data from device was requested via `ScheduleAsyncDownload` beforehand and has already arrived.
    /// </summary>
    /// <param name="shape">the TensorShape (and thus length) of the data to copy</param>
    /// <returns>Tensor data as `float` arrary</returns>
    float[] Download(TensorShape shape);

    /// <summary>
    /// Returns an array filled with the values of multiple tensors that share the same tensorData on device.
    /// Depending on the implementation and underlying device this array might be a copy or direct reference to tensor values, no conversion from on device memory layout will occur.
    /// This is a blocking call, unless data from device was requested via `ScheduleAsyncDownload` beforehand and has already arrived.
    /// </summary>
    /// <param name="offset">This function outputs `offset` from the beginning of the array to location of values for specific tensor. `offset` parameters is specified in float elements</param>
    /// <returns>array filled with the values of multiple tensors that share the same tensorData on device</returns>
    float[] SharedAccess(out int offset);

    /// <summary>
    /// Returns the maximum number of element this tensorData can contain.
    /// </summary>
    int maxCapacity { get; }
}

/// <summary>
/// Interface for device dependent representation of Tensor data that provides a read fence for scheduling data consumer job.
/// </summary>
public interface IDependableTensorData : ITensorData
{
    /// <summary>
    /// Returns job handle that can be used as `dependsOn` argument when scheduling data consumer job.
    /// Consumer job will start execution once Tensor data is ready for read access.
    /// </summary>
    Unity.Jobs.JobHandle fence { get; }
}

/// <summary>
/// Object that represent memory (recurrent state) between the executions of a given model.
/// </summary>
public class RecurrentState : IDisposable
{
    private int m_BatchSize = 1;
    private Model m_Model;
    private Tensor[] m_Memories;

    int InferBatchSize(int batchSize, int newBatchSize, string memoryName)
    {
        if (batchSize < 0)
            batchSize = newBatchSize;
        else
        {
            Assert.IsTrue(batchSize != -1);
            if (batchSize != newBatchSize)
                throw new ArgumentException("Batch size for all memories of the model must be the same value. " +
                    $"Expected batch size of {batchSize}, but got {newBatchSize} for memory `{memoryName}`");
        }
        return batchSize;
    }

    /// <summary>
    /// Constructs recurrent state for a specific model
    /// </summary>
    /// <param name="model">the associated model</param>
    /// <param name="batchSize">has to match the batch dimension of the input tensor(s). Specifying -1 will use batch size of the memory tensors as declared in the model</param>
    /// <param name="grabFromInputs">optional dictionary of named tensors that can be used as a memory. If name of the tensor matches the memory, tensor will be removed from the dictionary and used as memory</param>
    public RecurrentState(Model model, int batchSize = -1, Dictionary<string, Tensor> grabFromInputs = null)
    {
        bool overrideModelBatchSize = batchSize > 0;

        m_Model = model;
        m_Memories = new Tensor[m_Model.memories.Count];

        var index = 0;
        foreach (var memory in m_Model.memories)
        {
            var memoryName = memory.input;
            if (grabFromInputs != null && grabFromInputs.ContainsKey(memoryName))
            {
                // steal input from the inputs and use it as a memory
                var inputTensorToBecomeMemory = grabFromInputs[memoryName];
                m_Memories[index++] = inputTensorToBecomeMemory;
                grabFromInputs.Remove(memoryName);

                batchSize = InferBatchSize(batchSize, inputTensorToBecomeMemory.batch, memoryName);
            }
            else
            {
                if (!overrideModelBatchSize)
                    batchSize = InferBatchSize(batchSize, memory.shape.batch, memoryName);

                // create memory tensor
                var shape = new TensorShape(batchSize, memory.shape.height, memory.shape.width, memory.shape.channels);
                m_Memories[index++] = new Tensor(shape);
            }
        }

        m_BatchSize = batchSize;
    }

    /// <summary>
    /// Finalize RecurrentState
    /// </summary>
    ~RecurrentState()
    {
        Dispose();
    }

    /// <summary>
    /// Dispose RecurrentState
    /// </summary>
    public virtual void Dispose()
    {
        if (m_Memories == null)
            return;

        foreach (var x in m_Memories)
            x.Dispose();

        m_Memories = null;
    }

    /// <summary>
    /// Returns batch dimension used for the memories.
    /// </summary>
    /// <returns>batch dimension used for the memories</returns>
    public int GetBatchSize()
    {
        return m_BatchSize;
    }

    /// <summary>
    /// Internal callback called before the execution of the model.
    /// This callback prepares model for the next iteration according to the memory.
    /// </summary>
    /// <param name="worker">IWorker</param>
    public void BeforeExecution(IWorker worker)
    {
        Assert.AreEqual(m_Model.memories.Count, m_Memories.Length);

        var index = 0;
        foreach (var memory in m_Model.memories)
            worker.SetInput(memory.input, m_Memories[index++]);
    }

    /// <summary>
    /// Internal callback called after execution of the model finished.
    /// This callback stores results of the current iteration in the memory.
    /// </summary>
    /// <param name="worker">IWorker</param>
    public void AfterExecution(IWorker worker)
    {
        Assert.AreEqual(m_Model.memories.Count, m_Memories.Length);

        var index = 0;
        foreach (var memory in m_Model.memories)
        {
            var newTensor = worker.CopyOutput(memory.output);
            Assert.IsTrue(newTensor.tensorOnDevice != m_Memories[index]);
            m_Memories[index].Dispose();
            m_Memories[index] = newTensor;
            index++;
        }
    }
}

/// <summary>
/// Factory to create worker that executes specified model on a particular device  (GPU, CPU, etc) using particular backend.
/// See `IWorker` for usage of the worker itself.
/// </summary>
public class WorkerFactory
{
    /// <summary>
    /// Supported device type
    /// </summary>
    public enum Device
    {
        /// <summary>
        /// GPU
        /// </summary>
        GPU                 = 1 << 8,

        /// <summary>
        /// CPU
        /// </summary>
        CPU                 = 1 << 9,

        /// <summary>
        /// Auto
        /// </summary>
        Auto                = 1 << 15,

        // aliases
        /// <summary>
        /// Alias for GPU
        /// </summary>
        Compute             = GPU,

        /// <summary>
        /// Alias for CPU
        /// </summary>
        CSharp              = CPU,
    }

    /// <summary>
    /// Backend type
    /// </summary>
    public enum Type
    {
        /// <summary>
        /// Auto
        /// </summary>
        Auto                = 0 | Device.Auto,

        /// <summary>
        /// Compute Precompiled, least CPU overhead when scheduling
        /// </summary>
        ComputePrecompiled  = 0 | Device.GPU,

        /// <summary>
        /// Fast Compute implementation
        /// </summary>
        Compute             = 1 | Device.GPU,

        /// <summary>
        /// Reference Compute implementation, very slow
        /// </summary>
        ComputeRef          = 2 | Device.GPU,

        /// <summary>
        /// Unity Burst implementation, fastest CPU option
        /// </summary>
        CSharpBurst         = 0 | Device.CPU,

        /// <summary>
        /// Fast C# implementation when Burst is not available
        /// </summary>
        CSharp              = 1 | Device.CPU,

        /// <summary>
        /// Reference C# implementation, very very slow
        /// </summary>
        CSharpRef           = 2 | Device.CPU
    }

    /// <summary>
    /// Worker configuration
    /// `compareAgainstType` if different than the worker `type`, the model will be run on both backend and result of every layer will be compared, checking for divergence. Great for debugging, but very slow because of the sync needed.
    /// `verbose` will log scheduling of layers execution to the console (default == false).
    /// `compareLogLevel` define how difference will be reported (default == Warning).
    /// `compareEpsilon` the maximum tolerance before a difference is reported (default == 0.0001f).
    /// </summary>
    public struct WorkerConfiguration {
        /// <summary>
        /// Print debug information on model execution to the console
        /// </summary>
        public bool verbose;

        /// <summary>
        /// Compare layer by layer outputs against other worker type
        /// </summary>
        public Type compareAgainstType;

        /// <summary>
        /// Comparison log level
        /// </summary>
        public CompareOpsUtils.LogLevel compareLogLevel;

        /// <summary>
        /// Comparison error tolerance
        /// </summary>
        public float compareEpsilon;

        /// <summary>
        /// Construct worker configuration
        /// </summary>
        /// <param name="compareAgainstType">Compare layer by layer outputs against other worker type</param>
        /// <param name="verbose">Print debug information on model execution to the console</param>
        /// <param name="compareLogLevel">Comparison log level</param>
        /// <param name="compareEpsilon">Comparison error tolerance</param>
        public WorkerConfiguration(Type compareAgainstType, bool verbose=false, CompareOpsUtils.LogLevel compareLogLevel = CompareOpsUtils.LogLevel.Warning, float compareEpsilon = 0.0001f)
        {
            this.verbose = verbose;
            this.compareAgainstType = compareAgainstType;
            this.compareLogLevel = compareLogLevel;
            this.compareEpsilon = compareEpsilon;
        }
    }

    /// <summary>
    /// Create a worker with explicitly specified backend `type` to execute the given `model`.
    /// </summary>
    /// <param name="type">backend type to use. For example `WorkerFactory.Type.Compute` specifies the fast GPU path</param>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="additionalOutputs">the additional outputs to track but not directly specified by the model</param>
    /// <param name="trimOutputs">the outputs not discard even if they are specified by the model</param>
    /// <param name="verbose"> will log scheduling of layers execution to the console</param>
    /// <param name="compareAgainstType">if different than `type` model will be run on those two backend and result of every layer will be compared, checking for divergence. Great for debugging, but very slow because of the sync needed</param>
    /// <param name="differenceLogLevel">if `compareAgainstType` is used difference will be reported as error is this is true or warning otherwise</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Type type, Model model, string[] additionalOutputs, string[] trimOutputs, bool verbose, Type compareAgainstType, CompareOpsUtils.LogLevel differenceLogLevel=CompareOpsUtils.LogLevel.Warning)
    {
        var workerConfiguration = new WorkerConfiguration(type, verbose);
        workerConfiguration.compareAgainstType = compareAgainstType;
        workerConfiguration.compareLogLevel = differenceLogLevel;
        return BarracudaBackendsFactory.CreateWorker(type, model, additionalOutputs, trimOutputs, workerConfiguration);
    }

    /// <summary>
    /// Create a worker with explicitly specified backend `type` to execute the given `model`.
    /// </summary>
    /// <param name="type">backend type to use. For example `WorkerFactory.Type.Compute` specifies the fast GPU path</param>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="additionalOutputs">the additional outputs to track but not directly specified by the model</param>
    /// <param name="trimOutputs">the outputs not discard even if they are specified by the model</param>
    /// <param name="workerConfiguration">define configurations such as logging and comparison backend, see WorkerConfiguration API docs</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Type type, Model model, string[] additionalOutputs, string[] trimOutputs, WorkerConfiguration workerConfiguration)
    {
        return BarracudaBackendsFactory.CreateWorker(type, model, additionalOutputs, trimOutputs, workerConfiguration);
    }

    /// <summary>
    /// Create a worker that will execute `model` using the best backend that is available for a given `device` type.
    /// </summary>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="additionalOutputs">the additional outputs to track but not directly specified by the model</param>
    /// <param name="trimOutputs">the outputs not discard even if they are specified by the model</param>
    /// <param name="device">the device type to run worker on. For example `WorkerFactory.Device.GPU` specifies the fast GPU path</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Model model, string[] additionalOutputs, string[] trimOutputs, Device device = Device.Auto, bool verbose = false)
    {
        var type = GetBestTypeForDevice(device);
        var workerConfiguration = new WorkerConfiguration(type, verbose);
        return CreateWorker(type, model, additionalOutputs, trimOutputs, workerConfiguration);
    }

    /// <summary>
    /// Create a worker with explicitly specified backend `type` to execute the given `model`.
    /// </summary>
    /// <param name="type">backend type to use. For example `WorkerFactory.Type.Compute` specifies the fast GPU path</param>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="verbose">will log scheduling of layers execution to the console</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Type type, Model model, bool verbose)
    {
        var workerConfiguration = new WorkerConfiguration(type, verbose);
        return CreateWorker(type, model, null, null, workerConfiguration);
    }

    /// <summary>
    /// Create a worker with explicitly specified backend `type` to execute the given `model`.
    /// </summary>
    /// <param name="type">backend type to use. For example `WorkerFactory.Type.Compute` specifies the fast GPU path</param>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="additionalOutputs">the additional outputs to track but not directly specified by the model</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Type type, Model model, string[] additionalOutputs, bool verbose = false)
    {
        var workerConfiguration = new WorkerConfiguration(type, verbose);
        return CreateWorker(type, model, additionalOutputs, null, workerConfiguration);
    }

    /// <summary>
    /// Create a worker with explicitly specified backend `type` to execute the given `model`.
    /// </summary>
    /// <param name="type">backend type to use. For example `WorkerFactory.Type.Compute` specifies the fast GPU path</param>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="additionalOutputs">the additional outputs to track but not directly specified by the model</param>
    /// <param name="trimOutputs">the outputs not discard even if they are specified by the model</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Type type, Model model, string[] additionalOutputs = null, string[] trimOutputs = null, bool verbose = false)
    {
        var workerConfiguration = new WorkerConfiguration(type, verbose);
        return CreateWorker(type, model, additionalOutputs, trimOutputs, workerConfiguration);
    }

    /// <summary>
    /// Create a worker with explicitly specified backend `type` to execute the given `model`.
    /// </summary>
    /// <param name="type">backend type to use. For example `WorkerFactory.Type.Compute` specifies the fast GPU path</param>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="verbose">will log scheduling of layers execution to the console</param>
    /// <param name="compareAgainstType">if different than `type` model will be run on those two backend and result of every layer will be compared, checking for divergence. Great for debugging, but very slow because of the sync needed</param>
    /// <param name="differenceLogLevel">if `compareAgainstType` is used difference will be reported as error is this is true or warning otherwise</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Type type, Model model, bool verbose, Type compareAgainstType, CompareOpsUtils.LogLevel differenceLogLevel=CompareOpsUtils.LogLevel.Warning)
    {
        var workerConfiguration = new WorkerConfiguration(type, verbose);
        workerConfiguration.compareAgainstType = compareAgainstType;
        workerConfiguration.compareLogLevel = differenceLogLevel;
        return CreateWorker(type, model, additionalOutputs:null, trimOutputs:null, workerConfiguration);
    }

    /// <summary>
    /// Create a worker with explicitly specified backend `type` to execute the given `model`.
    /// </summary>
    /// <param name="type">backend type to use. For example `WorkerFactory.Type.Compute` specifies the fast GPU path</param>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="workerConfiguration">define configurations such as logging and comparison backend, see WorkerConfiguration API docs</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Type type, Model model, WorkerConfiguration workerConfiguration)
    {
        return CreateWorker(type, model, additionalOutputs:null, trimOutputs:null, workerConfiguration);
    }

    /// <summary>
    /// Create a worker that will execute `model` using the best backend that is available for a given `device` type.
    /// </summary>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="verbose">will log scheduling of layers execution to the console</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Model model, bool verbose = false)
    {;
        return CreateWorker(model, Device.Auto, verbose);
    }

    /// <summary>
    /// Create a worker that will execute `model` using the best backend that is available for a given `device` type.
    /// </summary>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="device">the preferred device for execution. For example `WorkerFactory.Device.GPU` specifies the fast GPU path</param>
    /// <param name="verbose">will log scheduling of layers execution to the console</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Model model, Device device, bool verbose = false)
    {
        return CreateWorker(model, additionalOutputs:null, device, verbose);
    }

    /// <summary>
    /// Create a worker that will execute `model` using the best backend that is available for a given `device` type.
    /// </summary>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="additionalOutputs">the additional outputs to track but not directly specified by the model</param>
    /// <param name="device">the device type to run worker on. For example `WorkerFactory.Device.GPU` specifies the fast GPU path</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(Model model, string[] additionalOutputs, Device device = Device.Auto, bool verbose = false)
    {
        return CreateWorker(model, additionalOutputs, trimOutputs:null, device, verbose);
    }

    /// <summary>
    /// Create a worker using the reference CPU backend for the given `model`.
    /// </summary>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateReferenceCPUWorker(Model model, bool verbose = false)
    {
        return CreateWorker(Type.CSharpRef, model, verbose);
    }

    /// <summary>
    /// Create a worker using the reference GPU backend for the given `model`.
    /// </summary>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateReferenceComputeWorker(Model model, bool verbose = false)
    {
        return CreateWorker(Type.ComputeRef, model, verbose);
    }

    /// <summary>
    /// Create a worker using the precompiled GPU backend for the given `model`.
    /// </summary>
    /// <param name="model">the associated model. See ModelLoader.cs</param>
    /// <param name="verbose"></param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateComputeWorker(Model model, bool verbose = false)
    {
        return CreateWorker(Type.ComputePrecompiled, model, verbose);
    }

    /// <summary>
    /// Check if a backend is of a given type.
    /// For example: IsType(Type.CSharpRef, Device.GPU) == true
    /// </summary>
    /// <param name="type">type to check against</param>
    /// <param name="device">device to check against</param>
    /// <returns>`true` if backend is of specified type</returns>
    /// <exception cref="ArgumentException">thrown if type is `Type.Auto`</exception>
    public static bool IsType(Type type, Device device)
    {
        type = BarracudaBackendsFactory.ResolveAutoType(type);
        if (type == Type.Auto)
            throw new ArgumentException($"Auto type is ambiguous in this context and not supported");
        return ((int)type & (int)device) == (int)device;
    }

    /// <summary>
    /// Returns the best backend type that can run on a `device` given the `model`.
    /// </summary>
    /// <param name="device">device</param>
    /// <returns>Best worker type for specified `device`</returns>
    public static Type GetBestTypeForDevice(Device device)
    {
        return BarracudaBackendsFactory.GetBestTypeForDevice(device);
    }

    /// <summary>
    /// Validate if a backend of `type` is supported, otherwise return a fallback type.
    /// </summary>
    /// <param name="type">type</param>
    /// <returns>returns `type` if valid, otherwise returns fallback type</returns>
    public static Type ValidateType(Type type)
    {
        return BarracudaBackendsFactory.ValidateType(type);
    }
}

/// <summary>
/// Suspends the coroutine execution until worker has completed execution on a device and
/// contents of the specified tensor are downloaded to the main CPU memory.
/// `WaitForCompletion` is not necessary and should NOT be used, unless tensor contents are accessed on CPU!
/// `WaitForCompletion` can only be used with a `yield` statement in coroutines.
/// </summary>
public class WaitForCompletion : CustomYieldInstruction
{
    private Tensor m_Tensor;

    /// <summary>
    /// Returns `true` while results are not yet ready
    /// </summary>
    public override bool keepWaiting
    {
        get
        {
            bool cpuCacheIsReady = m_Tensor.PrepareCacheForAccess(blocking:false);
            return !cpuCacheIsReady;
        }
    }

    /// <summary>
    /// Suspends the coroutine execution until worker has completed execution on a device and
    /// contents of the specified tensor are downloaded to the main CPU memory.
    /// </summary>
    /// <param name="tensor">`Tensor` that will be downloaded once worker execution is finished</param>
    public WaitForCompletion(Tensor tensor)
    {
        m_Tensor = tensor;
    }
}

/// <summary>
/// Extensions for `Model` class
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// Create a worker that will execute `model` using the best backend that is available for a given `device` type.
    /// This is just a convenience function that internally calls `ModelLoader.Load` followed by ``WorkerFactory.CreateWorker`.
    /// </summary>
    /// <param name="model">the associated Model to execute</param>
    /// <param name="device">the preferred device for execution. For example `WorkerFactory.Device.GPU` specifies the fast GPU path</param>
    /// <param name="verbose">will log scheduling of layers execution to the console</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(this Model model,
        WorkerFactory.Device device = WorkerFactory.Device.Auto, bool verbose = false)
    {
        return WorkerFactory.CreateWorker(model, device, verbose);
    }

    /// <summary>
    /// Create a worker that will execute `model` using the best backend that is available for a given `device` type.
    /// This is just a convenience function that internally calls `ModelLoader.Load` followed by ``WorkerFactory.CreateWorker`.
    /// </summary>
    /// <param name="model">the associated Model to execute</param>
    /// <param name="additionalOutputs">are the additional outputs to track but not directly specified by the model</param>
    /// <param name="trimOutputs">are the outputs not discard even if they are specified by the model</param>
    /// <param name="device">the device type to run worker on. For example `WorkerFactory.Device.GPU` specifies the fast GPU path</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(this Model model,
        string[] additionalOutputs, string[] trimOutputs, WorkerFactory.Device device = WorkerFactory.Device.Auto, bool verbose = false)
    {
        return WorkerFactory.CreateWorker(model, additionalOutputs, trimOutputs, device, verbose);
    }
}

/// <summary>
/// Extensions for `NNModel` class
/// </summary>
public static class NNModelExtensions
{
    /// <summary>
    /// Create a worker that will execute `asset` using the best backend that is available for a given `device` type.
    /// This is just a convenience function that internally calls `ModelLoader.Load` followed by ``WorkerFactory.CreateWorker`.
    /// </summary>
    /// <param name="asset">the associated NNModel asset</param>
    /// <param name="device">the preferred device for execution. For example `WorkerFactory.Device.GPU` specifies the fast GPU path</param>
    /// <param name="verbose">will log scheduling of layers execution to the console</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(this NNModel asset,
        WorkerFactory.Device device = WorkerFactory.Device.Auto, bool verbose = false)
    {
        var model = ModelLoader.Load(asset);
        return model.CreateWorker(device, verbose);
    }

    /// <summary>
    /// Create a worker that will execute `asset` using the best backend that is available for a given `device` type.
    /// This is just a convenience function that internally calls `ModelLoader.Load` followed by ``WorkerFactory.CreateWorker`.
    /// </summary>
    /// <param name="asset">the associated NNModel asset</param>
    /// <param name="additionalOutputs">the additional outputs to track but not directly specified by the model</param>
    /// <param name="trimOutputs">the outputs not discard even if they are specified by the model</param>
    /// <param name="device">the device type to run worker on. For example `WorkerFactory.Device.GPU` specifies the fast GPU path</param>
    /// <param name="verbose">will log scheduling of layers execution to the console (default == false)</param>
    /// <returns>Worker instance</returns>
    public static IWorker CreateWorker(this NNModel asset,
        string[] additionalOutputs, string[] trimOutputs, WorkerFactory.Device device = WorkerFactory.Device.Auto, bool verbose = false)
    {
        var model = ModelLoader.Load(asset);
        return model.CreateWorker(additionalOutputs, trimOutputs, device, verbose);
    }
}

} // namespace Unity.Barracuda
