﻿using HunterPie.Core.Domain.Memory;
using HunterPie.Core.System.Windows.Native;
using HunterPie.Core.Utils;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HunterPie.Core.System.Windows.Memory
{
    public class WindowsMemory : IMemory
    {
        const long NULLPTR = 0;

        IntPtr pHandle;

        public WindowsMemory(IntPtr processHandle)
        {
            pHandle = processHandle;
        }

        public string Read(long address, uint length, Encoding encoding = null)
        {
            byte[] buffer = new byte[length];

            Kernel32.ReadProcessMemory(pHandle, (IntPtr)address, buffer, buffer.Length, out int _);

            string raw = (encoding ?? Encoding.UTF8).GetString(buffer, 0, buffer.Length);
            int nullCharIdx = raw.IndexOf('\x00');

            if (nullCharIdx < 0)
                return raw;

            return raw.Substring(0, nullCharIdx);
        }

        public T Read<T>(long address) where T : struct
        {
            return Read<T>(address, 1)[0];
        }

        public T[] Read<T>(long address, uint count) where T : struct
        {
            Type type = typeof(T);

            if (type.IsPrimitive)
                return ReadPrimitive<T>(address, count);

            return ReadStructure<T>(address, count);
        }

        public long Read(long address, int[] offsets)
        {
            foreach (int offset in offsets)
            {
                long tmp = Read<long>(address);

                if (tmp == NULLPTR)
                    return NULLPTR;

                address = tmp + offset;
            }

            return address;
        }

        public long ReadPtr(long address, int[] offsets)
        {
            foreach (int offset in offsets)
            {
                long tmp = Read<long>(address + offset);

                if (tmp == NULLPTR)
                    return NULLPTR;

                address = tmp;
            }

            return address;
        }

        private T[] ReadStructure<T>(long address, uint count) where T : struct
        {
            int size = Marshal.SizeOf<T>() * (int)count;
            IntPtr bufferAddress = Marshal.AllocHGlobal(size);
            Kernel32.ReadProcessMemory(pHandle, (IntPtr)address, bufferAddress, size, out int _);

            var structures = MarshalHelper.BufferToStructures<T>(bufferAddress, (int)count);

            Marshal.FreeHGlobal(bufferAddress);

            return structures;
        }

        private T[] ReadPrimitive<T>(long address, uint count) where T : struct
        {
            int lpByteCount = Marshal.SizeOf<T>() * (int)count;
            T[] buffer = new T[count];

            Kernel32.ReadProcessMemory(pHandle, (IntPtr)address, buffer, lpByteCount, out int _);

            return buffer;
        }

        public bool Write<T>(long address, T data) where T : struct
        {
            throw new NotImplementedException();
        }

        public bool Write<T>(long address, T[] data) where T : struct
        {
            throw new NotImplementedException();
        }

        public T Deref<T>(long address, int[] offsets) where T : struct
        {
            long ptr = Read(address, offsets);
            return Read<T>(ptr);
        }

        public T DerefPtr<T>(long address, int[] offsets) where T : struct
        {
            long ptr = ReadPtr(address, offsets);
            return Read<T>(ptr);
        }
    }
}
