using System.Runtime.InteropServices.Marshalling;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Windows.Win32;
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配

public sealed unsafe class ComPtr<T>(T managed) : IDisposable {
    public T Managed { get; } = managed;
    public void* UnmanagedPointer { get; } = ComInterfaceMarshaller<T>.ConvertToUnmanaged(managed);
    private bool _disposed;

    public void Dispose() {
        if (_disposed)
            return;

        ComInterfaceMarshaller<T>.Free(UnmanagedPointer);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~ComPtr() {
        Dispose();
    }
}
