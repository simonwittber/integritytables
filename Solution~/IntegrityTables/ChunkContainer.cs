using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IntegrityTables;

public abstract class ChunkContainer
{
    protected struct Chunk
    {
        public unsafe byte* basePtr;
        public int rowCount;
        public bool IsFull => rowCount == chunkLength;
    }

    protected Chunk[] chunks = new Chunk[1];
    protected int chunkCount;
    public int[] offsets;
    protected abstract void Init();
    protected const int chunkLength = 512;
    public int chunkSize = 0;
    protected Stack<int> freeChunks = new();

    protected static int Align(int offset, int align)
    {
        int mask = align - 1;
        return (offset + mask) & ~mask;
    }

    protected static int AlignmentOf<T>() where T : unmanaged
    {
        return Unsafe.SizeOf<T>();
    }

    protected ChunkContainer()
    {
        // Don't call Init() here - let derived classes control when it's called
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void GetSlotAndChunkForIndex(int index, out int slot, out byte* basePtr)
    {
        int chunkIndex = index / chunkLength;
        slot = index % chunkLength;
        basePtr = chunks[chunkIndex].basePtr;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe int GetFreeChunkAndSlot(out int slot, out byte* basePtr)
    {
        if (freeChunks.Count == 0)
            AllocateNewChunk();

        slot = 0;
        int chunkIndex = 0;
        int finalIndex = 0;
        while (true)
        {
            chunkIndex = freeChunks.Pop();
            ref Chunk chunk = ref chunks[chunkIndex];
            if (chunk.IsFull) continue;
            slot = chunk.rowCount;
            chunk.rowCount = Math.Max(chunk.rowCount, slot + 1);
            finalIndex = chunkIndex * chunkLength + slot;
            basePtr = chunk.basePtr;
            if (chunk.rowCount < chunkLength)
            {
                freeChunks.Push(chunkIndex);
            }

            return finalIndex;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Remove(int index)
    {
        int chunkIndex = index / chunkLength;
        int slot = index % chunkLength;
        ref var chunk = ref chunks[chunkIndex];

        var basePtr = chunk.basePtr;
        var last = chunk.rowCount;
        if (slot < last - 1)
        {
            // Move the last element to the slot being removed
            Unsafe.CopyBlockUnaligned(basePtr + slot * chunkSize, basePtr + (last - 1) * chunkSize, (uint) chunkSize);
        }

        chunk.rowCount--;

        if (chunk.rowCount == chunkLength - 1)
            freeChunks.Push(chunkIndex);
    }

    protected unsafe void AllocateNewChunk()
    {
        int stride = chunkSize; // per-entity bytes
        long bytes = (long) chunkLength * stride;
        var chunk = new Chunk()
        {
            basePtr = (byte*) Marshal.AllocHGlobal((int) bytes),
            rowCount = 0
        };
        if (chunkCount == chunks.Length)
        {
            Array.Resize(ref chunks, chunks.Length * 2);
        }

        freeChunks.Push(chunkCount);
        chunks[chunkCount++] = chunk;
    }

    public unsafe void Dispose()
    {
        foreach (var chunk in chunks)
        {
            if (chunk.basePtr != null)
            {
                Marshal.FreeHGlobal((IntPtr) chunk.basePtr);
            }
        }

        Array.Clear(chunks);
    }
}

public class ChunkContainer<T1> : ChunkContainer where T1 : unmanaged, IEquatable<T1>
{
    public ChunkContainer()
    {
        Init();
        AllocateNewChunk();
    }

    protected override void Init()
    {
        chunkSize = Unsafe.SizeOf<T1>();
        offsets = [0];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe int Add(T1 v1)
    {
        var finalIndex = GetFreeChunkAndSlot(out var slot, out var basePtr);
        Unsafe.AsRef<T1>(basePtr + offsets[0] + slot * Unsafe.SizeOf<T1>()) = v1;
        return finalIndex;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T1 GetT1(int index)
    {
        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        return ref Unsafe.AsRef<T1>(basePtr + offsets[0] + slot * Unsafe.SizeOf<T1>());
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetT1(int index, T1 value)
    {
        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        Unsafe.AsRef<T1>(basePtr + offsets[0] + slot * Unsafe.SizeOf<T1>()) = value;
    }
}

public class ChunkContainer<T1, T2> : ChunkContainer where T1 : unmanaged, IEquatable<T1> where T2 : unmanaged, IEquatable<T2>
{
    public ChunkContainer()
    {
        Init();
        AllocateNewChunk();
    }

    protected override void Init()
    {
        chunkSize = Unsafe.SizeOf<T1>() + Unsafe.SizeOf<T2>();
        offsets = [0, Unsafe.SizeOf<T1>()];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe int Add(T1 v1, T2 v2)
    {
        var finalIndex = GetFreeChunkAndSlot(out var slot, out var basePtr);
        Unsafe.AsRef<T1>(basePtr + offsets[0] + slot * chunkSize) = v1;
        Unsafe.AsRef<T2>(basePtr + offsets[1] + slot * chunkSize) = v2;
        return finalIndex;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T1 GetT1(int index)
    {
        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        return ref Unsafe.AsRef<T1>(basePtr + offsets[0] + slot * chunkSize);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetT1(int index, T1 value)
    {
        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        Unsafe.AsRef<T1>(basePtr + offsets[0] + slot * chunkSize) = value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T2 GetT2(int index)
    {
        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        return ref Unsafe.AsRef<T2>(basePtr + offsets[1] + slot * chunkSize);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetT2(int index, T2 value)
    {
        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        Unsafe.AsRef<T2>(basePtr + offsets[1] + slot * chunkSize) = value;
    }
}

public class DynamicChunkContainer : ChunkContainer
{
    private readonly Type[] types;
    private readonly int[] typeSizes;
    private readonly int[] typeAlignments;

    public DynamicChunkContainer(params Type[] types)
    {
        this.types = types ?? throw new ArgumentNullException(nameof(types));
        if (types.Length == 0)
            throw new ArgumentException("At least one type must be provided", nameof(types));
        
        typeSizes = new int[types.Length];
        typeAlignments = new int[types.Length];
        
        for (int i = 0; i < types.Length; i++)
        {
            if (!types[i].IsValueType)
                throw new ArgumentException($"Type {types[i].Name} must be a value type", nameof(types));
            
            typeSizes[i] = Marshal.SizeOf(types[i]);
            typeAlignments[i] = GetAlignment(types[i]);
        }
        
        Init();
        AllocateNewChunk();
    }

    private static int GetAlignment(Type type)
    {
        // Simple alignment rules - in practice you might want more sophisticated logic
        int size = Marshal.SizeOf(type);
        if (size >= 8) return 8;
        if (size >= 4) return 4;
        if (size >= 2) return 2;
        return 1;
    }

    protected override void Init()
    {
        // Calculate offsets and total chunk size
        offsets = new int[types.Length];
        int currentOffset = 0;
        
        for (int i = 0; i < types.Length; i++)
        {
            currentOffset = Align(currentOffset, typeAlignments[i]);
            offsets[i] = currentOffset;
            currentOffset += typeSizes[i];
        }
        
        chunkSize = Align(currentOffset, 8); // Align to 8 bytes for performance
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe int Add(params object[] values)
    {
        if (values.Length != types.Length)
            throw new ArgumentException($"Expected {types.Length} values, got {values.Length}");

        var finalIndex = GetFreeChunkAndSlot(out var slot, out var basePtr);
        
        for (int i = 0; i < types.Length; i++)
        {
            if (values[i] == null || !types[i].IsAssignableFrom(values[i].GetType()))
                throw new ArgumentException($"Value at index {i} is not of type {types[i].Name}");
            
            var valuePtr = basePtr + offsets[i] + slot * chunkSize;
            Marshal.StructureToPtr(values[i], (IntPtr)valuePtr, false);
        }
        
        return finalIndex;
    }
    
    public unsafe int Add()
    {
        var finalIndex = GetFreeChunkAndSlot(out var slot, out var basePtr);
        // clear from basePtr        
        for(var i = 0; i<chunkSize; i++)
        {
            *(basePtr + i) = 0; // zero out the memory
        }
        return finalIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T Get<T>(int index, int typeIndex) where T : struct
    {
        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        var valuePtr = basePtr + offsets[typeIndex] + slot * chunkSize;
        return ref Unsafe.AsRef<T>(valuePtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Set<T>(int index, int typeIndex, T value) where T : struct
    {
        if (typeIndex < 0 || typeIndex >= types.Length)
            throw new ArgumentOutOfRangeException(nameof(typeIndex));
        
        if (typeof(T) != types[typeIndex])
            throw new ArgumentException($"Type {typeof(T).Name} does not match expected type {types[typeIndex].Name}");

        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        var valuePtr = basePtr + offsets[typeIndex] + slot * chunkSize;
        Marshal.StructureToPtr(value, (IntPtr)valuePtr, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe object GetObject(int index, int typeIndex)
    {
        if (typeIndex < 0 || typeIndex >= types.Length)
            throw new ArgumentOutOfRangeException(nameof(typeIndex));

        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        var valuePtr = basePtr + offsets[typeIndex] + slot * chunkSize;
        return Marshal.PtrToStructure((IntPtr)valuePtr, types[typeIndex]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetObject(int index, int typeIndex, object value)
    {
        if (typeIndex < 0 || typeIndex >= types.Length)
            throw new ArgumentOutOfRangeException(nameof(typeIndex));
        
        if (value == null || !types[typeIndex].IsAssignableFrom(value.GetType()))
            throw new ArgumentException($"Value is not of type {types[typeIndex].Name}");

        GetSlotAndChunkForIndex(index, out var slot, out var basePtr);
        var valuePtr = basePtr + offsets[typeIndex] + slot * chunkSize;
        Marshal.StructureToPtr(value, (IntPtr)valuePtr, false);
    }

    public Type GetTypeAt(int typeIndex)
    {
        if (typeIndex < 0 || typeIndex >= types.Length)
            throw new ArgumentOutOfRangeException(nameof(typeIndex));
        return types[typeIndex];
    }

    public int TypeCount => types.Length;
}
