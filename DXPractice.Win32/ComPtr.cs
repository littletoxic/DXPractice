using System.Runtime.InteropServices.Marshalling;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Windows.Win32;
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配

public sealed unsafe class ComPtr<T>(T managed) {
    public T Managed { get; } = managed;
    public void* UnmanagedPointer { get; } = ComInterfaceMarshaller<T>.ConvertToUnmanaged(managed);

    ~ComPtr() {
        ComInterfaceMarshaller<T>.Free(UnmanagedPointer);
    }
}
