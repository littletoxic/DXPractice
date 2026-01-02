using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace DXDemo3;

internal static class Program {

    [STAThread]
    static void Main() {
        using var hInstance = GetModuleHandle();

        DX12Engine.Run(hInstance);
    }

}

internal unsafe class DX12Engine {

    private const int FrameCount = 3;
    private static readonly float[] SkyBlue = [0.529411793f, 0.807843208f, 0.921568692f, 1f];
    private static readonly float[] Red = [1f, 0f, 0f, 1f];
    private static readonly float[] Green = [0f, 1f, 0f, 1f];
    private static readonly float[] Yellow = [1f, 1f, 0f, 1f];
    private static readonly float[] Blue = [0f, 0f, 1f, 1f];

    // DX12 支持的所有功能版本，你的显卡最低需要支持 11
    private static readonly D3D_FEATURE_LEVEL[] DX12SupportLevels = [
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_2,        // 12.2
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_1,        // 12.1
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0,        // 12
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,        // 11.1
            D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0         // 11
    ];

    private const int WindowWidth = 640;
    private const int WindowHeight = 480;

    private HWND _hwnd;

    private ID3D12Debug _d3d12DebugDevice;
    private DXGI_CREATE_FACTORY_FLAGS _dxgiCreateFactoryFlag;

    private IDXGIFactory5 _dxgiFactory;
    private IDXGIAdapter1 _dxgiAdapter;
    private ID3D12Device4 _d3d12Device;

    private ID3D12CommandQueue _commandQueue;
    private ID3D12CommandAllocator _commandAllocator;
    private ID3D12GraphicsCommandList _commandList;

    private ID3D12DescriptorHeap _rtvHeap;
    private IDXGISwapChain3 _dxgiSwapChain;
    private ComPtr<ID3D12Resource>[] _renderTargets;
    private D3D12_CPU_DESCRIPTOR_HANDLE _rtvHandle;
    private uint _rtvDescriptorSize = 0;
    private uint _frameIndex = 0;

    private ID3D12Fence _fence;
    private ulong _fenceValue = 0;
    private SafeHandle _renderEvent;
    private D3D12_RESOURCE_BARRIER _beginBarrier;
    private D3D12_RESOURCE_BARRIER _endBarrier;

    private ComPtr<ID3D12RootSignature> _rootSignature;

    private ID3D12PipelineState _pipelineStateObject;

    private struct Vertex {
        internal __float_4 Position;
        internal __float_4 Color;
    }
    private ID3D12Resource _vertexResource;
    private D3D12_VERTEX_BUFFER_VIEW _vertexBufferView;

    private D3D12_VIEWPORT _viewPort = new() {
        TopLeftX = 0,
        TopLeftY = 0,
        Width = WindowWidth,
        Height = WindowHeight,
        MinDepth = D3D12_MIN_DEPTH,
        MaxDepth = D3D12_MAX_DEPTH
    };
    private RECT _scissorRect = new() {
        left = 0,
        top = 0,
        right = WindowWidth,
        bottom = WindowHeight
    };


    private void InitWindow(SafeHandle hInstance) {
        const string className = "DX12 Game";
        var pClassName = stackalloc char[className.Length + 1]; // +1 for null terminator

        className.AsSpan().CopyTo(new Span<char>(pClassName, className.Length));

        WNDCLASSW wc = new() {
            hInstance = new(hInstance.DangerousGetHandle()),
            lpfnWndProc = &WindowProc,
            lpszClassName = pClassName,
        };

        RegisterClass(wc);


        _hwnd = CreateWindowEx(
            0,
            className,
            "DX12 Game Window",
            WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_OVERLAPPED,
            10,
            10,
            WindowWidth,
            WindowHeight,
            HWND.Null,
            null,
            hInstance,
            null);

        ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
    }

    [Conditional("DEBUG")]
    private void CreateDebugDevice() {
        // [STAThread] attribute on Main method handles this
        //CoInitialize();

        D3D12GetDebugInterface(out _d3d12DebugDevice).ThrowOnFailure();
        _d3d12DebugDevice.EnableDebugLayer();

        _dxgiCreateFactoryFlag = DXGI_CREATE_FACTORY_FLAGS.DXGI_CREATE_FACTORY_DEBUG;
    }

    private bool CreateDevice() {
        CreateDXGIFactory2(_dxgiCreateFactoryFlag, out _dxgiFactory).ThrowOnFailure();

        for (uint i = 0; _dxgiFactory.EnumAdapters1(i, out _dxgiAdapter) != HRESULT.DXGI_ERROR_NOT_FOUND; i++) {
            // 找到显卡，就创建 D3D12 设备，从高到低遍历所有功能版本，创建成功就跳出
            foreach (var level in DX12SupportLevels) {
                if (D3D12CreateDevice(_dxgiAdapter, level, out _d3d12Device).Succeeded) {
                    return true;
                }
            }
        }

        MessageBox(default, "找不到任何能支持 DX12 的显卡，请升级电脑上的硬件！", "错误", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        return false;
    }

    private void CreateCommandComponents() {
        const D3D12_COMMAND_LIST_TYPE type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT;

        var queueDesc = new D3D12_COMMAND_QUEUE_DESC() {
            Type = type,
        };

        _d3d12Device.CreateCommandQueue(queueDesc, out _commandQueue);

        _d3d12Device.CreateCommandAllocator(type, out _commandAllocator);

        _d3d12Device.CreateCommandList(
            0,
            type,
            _commandAllocator,
            null,
            out _commandList);

        _commandList.Close();
    }

    private void CreateRenderTarget() {
        const D3D12_DESCRIPTOR_HEAP_TYPE type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV;

        var rtvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC() {
            NumDescriptors = FrameCount,
            Type = type,
        };
        _d3d12Device.CreateDescriptorHeap(rtvHeapDesc, out _rtvHeap);

        var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1() {
            BufferCount = FrameCount,
            Width = WindowWidth,
            Height = WindowHeight,
            Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
            SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD,
            BufferUsage = DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            SampleDesc = new() {
                Count = 1,
            },
        };

        _dxgiFactory.CreateSwapChainForHwnd(
            _commandQueue,
            _hwnd,
            swapChainDesc,
            null,
            null,
            out var tempSwapChain);

        // CreateSwapChainForHwnd 不能直接用于创建高版本接口
        _dxgiSwapChain = tempSwapChain as IDXGISwapChain3;

        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _rtvDescriptorSize = _d3d12Device.GetDescriptorHandleIncrementSize(type);

        _renderTargets = new ComPtr<ID3D12Resource>[FrameCount];
        for (uint i = 0; i < FrameCount; i++) {
            _dxgiSwapChain.GetBuffer<ID3D12Resource>(i, out var resource);
            _renderTargets[i] = new(resource);

            _d3d12Device.CreateRenderTargetView(_renderTargets[i].Managed, null, _rtvHandle);

            _rtvHandle.ptr += _rtvDescriptorSize;
        }
    }

    private void CreateFenceAndBarrier() {
        _renderEvent = CreateEvent(null, false, true, null);

        _d3d12Device.CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, out _fence);

        // 设置资源屏障
        // _beginBarrier 起始屏障：Present 呈现状态 -> Render Target 渲染目标状态
        _beginBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _beginBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT;
        _beginBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;

        // _endBarrier 终止屏障：Render Target 渲染目标状态 -> Present 呈现状态
        _endBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        _endBarrier.Anonymous.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;
        _endBarrier.Anonymous.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT;
    }

    private void CreateRootSignature() {
        var rootSignatureDesc = new D3D12_ROOT_SIGNATURE_DESC() {
            NumParameters = 0,
            pParameters = null,
            NumStaticSamplers = 0,
            pStaticSamplers = null,
            // 根签名标志，可以设置渲染管线不同阶段下的输入参数状态。注意这里！我们要从 IA 阶段输入顶点数据，所以要通过根签名，设置渲染管线允许从 IA 阶段读入数据
            Flags = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT
        };

        D3D12SerializeRootSignature(
            &rootSignatureDesc,
            D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1_0,
            out var signatureBlob,
            out var errorBlob).ThrowOnFailure();

        if (errorBlob != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlob.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        _d3d12Device.CreateRootSignature<ID3D12RootSignature>(
            0,
            signatureBlob.GetBufferPointer(),
            signatureBlob.GetBufferSize(),
            out var rootSignature);
        _rootSignature = new(rootSignature);
    }

    private void CreatePSO() {
        var psoDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC();

        var inputLayoutDesc = new D3D12_INPUT_LAYOUT_DESC();
        var inputElementDesc = stackalloc D3D12_INPUT_ELEMENT_DESC[2];

        var semanticNamePosition = "POSITION"u8;
        byte* pSemanticNamePosition = stackalloc byte[semanticNamePosition.Length + 1];
        semanticNamePosition.CopyTo(new Span<byte>(pSemanticNamePosition, semanticNamePosition.Length));

        inputElementDesc[0] = new() {
            SemanticName = new(pSemanticNamePosition),
            SemanticIndex = 0,
            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT,
            InputSlot = 0,
            AlignedByteOffset = 0,
            // 输入流类型，一种是我们现在用的 D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA 逐顶点输入流,还有一种叫逐实例输入流，后面再学
            InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        var semanticNameColor = "COLOR"u8;
        byte* pSemanticNameColor = stackalloc byte[semanticNameColor.Length + 1];
        semanticNameColor.CopyTo(new Span<byte>(pSemanticNameColor, semanticNameColor.Length));

        inputElementDesc[1] = new() {
            SemanticName = new(pSemanticNameColor),
            SemanticIndex = 0,
            Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT,
            InputSlot = 0,
            // 在输入槽中的偏移，因为 position 与 color 在同一输入槽(0号输入槽)
            // position 是 float4，有 4 个 float ，每个 float 占 4 个字节，所以要偏移 4*4=16 个字节，这样才能确定 color 参数的位置，不然装配的时候会覆盖原先 position 的数据
            AlignedByteOffset = 16,
            InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            InstanceDataStepRate = 0,
        };

        inputLayoutDesc.NumElements = 2;
        inputLayoutDesc.pInputElementDescs = inputElementDesc;
        psoDesc.InputLayout = inputLayoutDesc;


        D3DCompileFromFile(
            "shader.hlsl",
            null,
            null,
            "VSMain",
            "vs_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var vertexShaderBlob,
            out var errorBlobVS).ThrowOnFailure();

        if (errorBlobVS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobVS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        D3DCompileFromFile(
            "shader.hlsl",
            null,
            null,
            "PSMain",
            "ps_5_1",
#if DEBUG
            D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION,
#else
            0,
#endif
            0,
            out var pixelShaderBlob,
            out var errorBlobPS).ThrowOnFailure();

        if (errorBlobPS != null) {
            var errorMessage = Marshal.PtrToStringUTF8((nint)errorBlobPS.GetBufferPointer());
            Debug.WriteLine(errorMessage);
        }

        psoDesc.VS = new() {
            pShaderBytecode = vertexShaderBlob.GetBufferPointer(),
            BytecodeLength = vertexShaderBlob.GetBufferSize(),
        };
        psoDesc.PS = new() {
            pShaderBytecode = pixelShaderBlob.GetBufferPointer(),
            BytecodeLength = pixelShaderBlob.GetBufferSize(),
        };

        psoDesc.RasterizerState.CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_BACK;
        psoDesc.RasterizerState.FillMode = D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID;

        psoDesc.pRootSignature = (ID3D12RootSignature_unmanaged*)_rootSignature.Ptr;

        psoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
        psoDesc.NumRenderTargets = 1;
        psoDesc.RTVFormats._0 = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
        psoDesc.BlendState.RenderTarget._0.RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE.D3D12_COLOR_WRITE_ENABLE_ALL;
        psoDesc.SampleDesc.Count = 1;
        psoDesc.SampleMask = uint.MaxValue;

        _d3d12Device.CreateGraphicsPipelineState(psoDesc, out _pipelineStateObject);
    }

    private void CreateVertexResource() {
        var vertices = stackalloc Vertex[6] {
            new() { Position = new ReadOnlySpan<float>([-0.75f, 0.75f, 0f, 1f]), Color = new ReadOnlySpan<float>(Blue) },
            new() { Position = new ReadOnlySpan<float>([0.75f, 0.75f, 0f, 1f]), Color = new ReadOnlySpan<float>(Yellow) },
            new() { Position = new ReadOnlySpan<float>([0.75f, -0.75f, 0f, 1f]), Color = new ReadOnlySpan<float>(Red) },
            new() { Position = new ReadOnlySpan<float>([-0.75f, 0.75f, 0f, 1f]), Color = new ReadOnlySpan<float>(Blue) },
            new() { Position = new ReadOnlySpan<float>([0.75f, -0.75f, 0f, 1f]), Color = new ReadOnlySpan<float>(Red) },
            new() { Position = new ReadOnlySpan<float>([-0.75f, -0.75f, 0f, 1f]), Color = new ReadOnlySpan<float>(Green) },
        };

        var vertexDesc = new D3D12_RESOURCE_DESC() {
            Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
            Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Width = (ulong)(sizeof(Vertex) * 6),
            Height = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new() { Count = 1 },
        };

        _d3d12Device.CreateCommittedResource(
            new D3D12_HEAP_PROPERTIES() {
                Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD,
            },
            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
            vertexDesc,
            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            out _vertexResource);

        _vertexResource.Map(0, null, out var transferPointer);
        Buffer.MemoryCopy(vertices, transferPointer, sizeof(Vertex) * 6, sizeof(Vertex) * 6);
        _vertexResource.Unmap(0, null);

        _vertexBufferView.BufferLocation = _vertexResource.GetGPUVirtualAddress();
        _vertexBufferView.SizeInBytes = (uint)(sizeof(Vertex) * 6);
        _vertexBufferView.StrideInBytes = (uint)sizeof(Vertex);
    }

    private void Render() {
        _rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        _frameIndex = _dxgiSwapChain.GetCurrentBackBufferIndex();
        _rtvHandle.ptr += _frameIndex * _rtvDescriptorSize;

        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);

        _beginBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_beginBarrier]);

        _commandList.SetGraphicsRootSignature(_rootSignature.Managed);
        _commandList.SetPipelineState(_pipelineStateObject);

        _commandList.RSSetViewports([_viewPort]);
        _commandList.RSSetScissorRects([_scissorRect]);

        _commandList.OMSetRenderTargets(1, _rtvHandle, false, null);

        _commandList.ClearRenderTargetView(_rtvHandle, SkyBlue, 0, null);

        _commandList.IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        _commandList.IASetVertexBuffers(0, [_vertexBufferView]);

        _commandList.DrawInstanced(6, 1, 0, 0);

        _endBarrier.Anonymous.Transition.pResource = (ID3D12Resource_unmanaged*)_renderTargets[_frameIndex].Ptr;
        _commandList.ResourceBarrier([_endBarrier]);

        _commandList.Close();

        _commandQueue.ExecuteCommandLists([_commandList]);

        _dxgiSwapChain.Present(1, 0);


        _fenceValue++;
        _commandQueue.Signal(_fence, _fenceValue);
        _fence.SetEventOnCompletion(_fenceValue, _renderEvent);
    }

    private void RenderLoop() {
        bool exit = false;
        while (!exit) {
            var activeEvent = MsgWaitForMultipleObjects(
                [new(_renderEvent.DangerousGetHandle())],
                false,
                INFINITE,
                QUEUE_STATUS_FLAGS.QS_ALLINPUT);

            switch (activeEvent - WAIT_EVENT.WAIT_OBJECT_0) {
                case 0: // ActiveEvent 是 0，说明渲染事件已经完成了，进行下一次渲染
                    Render();
                    break;
                case 1: // ActiveEvent 是 1，说明渲染事件未完成，CPU 主线程同时处理窗口消息，防止界面假死
                    while (PeekMessage(out MSG msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE)) {
                        if (msg.message == WM_QUIT) {
                            exit = true;
                            break;
                        }

                        TranslateMessage(msg);
                        DispatchMessage(msg);
                    }
                    break;
                case (uint)WAIT_EVENT.WAIT_TIMEOUT: // 渲染超时
                    break;
            }


        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT WindowProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
        switch (msg) {
            case WM_DESTROY:
                PostQuitMessage(0);
                break;
            default:
                return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        return new(0);
    }

    internal static void Run(SafeHandle hInstance) {
        DX12Engine engine = new();
        engine.InitWindow(hInstance);
        engine.CreateDebugDevice();
        engine.CreateDevice();
        engine.CreateCommandComponents();
        engine.CreateRenderTarget();
        engine.CreateFenceAndBarrier();

        engine.CreateRootSignature();
        engine.CreatePSO();
        engine.CreateVertexResource();

        engine.RenderLoop();
    }

}
