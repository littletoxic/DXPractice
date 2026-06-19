using System.Runtime.InteropServices.Marshalling;

namespace DXDemo15;

internal sealed unsafe class ComPtr<T>(T managed) : IDisposable {
    public T Managed { get; } = managed;
    public void* Ptr { get; } = ComInterfaceMarshaller<T>.ConvertToUnmanaged(managed);
    private bool _disposed;

    public void Dispose() {
        if (_disposed)
            return;

        ComInterfaceMarshaller<T>.Free(Ptr);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~ComPtr() {
        Dispose();
    }
}
