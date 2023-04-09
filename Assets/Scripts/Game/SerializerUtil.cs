using System.Linq;
using System.Runtime.InteropServices;

public static class SerializerUtil
{
    public static byte[] Serialize<T>(T raw) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        var arr = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(raw, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    public static T Deserialize<T>(byte[] arr) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(arr, 0, ptr, size);
        var raw = Marshal.PtrToStructure<T>(ptr);
        Marshal.FreeHGlobal(ptr);
        return raw;
    }

    public static byte[] SerializeArray<T>(T[] raw) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        var arr = new byte[size * raw.Length];
        var ptr = Marshal.AllocHGlobal(size);
        for (var i = 0; i < raw.Length; i++)
        {
            Marshal.StructureToPtr(raw[i], ptr, true);
            Marshal.Copy(ptr, arr, i * size, size);
        }
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    public static T[] DeserializeArray<T>(byte[] arr) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        var raw = new T[arr.Length / size];
        var ptr = Marshal.AllocHGlobal(size);
        for (var i = 0; i < raw.Length; i++)
        {
            Marshal.Copy(arr, i * size, ptr, size);
            raw[i] = Marshal.PtrToStructure<T>(ptr);
        }
        Marshal.FreeHGlobal(ptr);
        return raw;
    }

    public static byte[] SerializeItem(ItemBase item)
    {
        byte[] id = Serialize(item.Id);
        byte[] instanceId = Serialize(item.InstanceId);
        return id.Concat(instanceId).ToArray();
    }

    public static ItemBase DeserializeItem(byte[] arr)
    {
        
    }
}
