﻿using HunterPie.Memory.Native;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using static HunterPie.Memory.Native.Kernel32;

namespace HunterPie.Memory.Core
{
    public class Scanner
    {
        public string Name { get; private set; }
        public Signatures Signatures { get; private set; }
        private IntPtr pHandle;
        private Process process;
        private byte[] memory;

        public static Scanner Create(string name)
        {
            return new() { Name = name };
        }

        public Scanner WithSignatures(Signatures signatures)
        {
            Signatures = signatures;
            return this;
        }

        public Scanner FindProcess()
        {
            process = Process.GetProcessesByName(Name)
                .First();

            pHandle = Kernel32.OpenProcess(Kernel32.PROCESS_ALL_ACCESS, false, process.Id);
            
            return this;
        }

        public Scanner GetMemory()
        {
            memory = new byte[process.MainModule.ModuleMemorySize];
            Kernel32.ReadProcessMemory(pHandle, process.MainModule.BaseAddress, memory, memory.Length, out int _);
            return this;
        }

        public Scanner FindSignatures()
        {
            int pageSize = 0;
            IntPtr lastBase = IntPtr.Zero;
            byte[] buffer = new byte[Signatures.MaximumLength];
            for (int i = 0; i < memory.Length && Signatures.PatternsLeft > 0; i++)
            {
                if (pageSize == 0)
                {
                    MEMORY_BASIC_INFORMATION pageInformation;

                    Kernel32.VirtualQueryEx(pHandle, process.MainModule.BaseAddress + i, out pageInformation, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>());
                    
                    // Skip pages that aren't related to our signatures
                    if (!pageInformation.Protect.HasFlag(AllocationProtectEnum.PAGE_EXECUTE_READ) && lastBase != pageInformation.BaseAddress)
                    {
                        i += (int)pageInformation.RegionSize;
                        pageSize = 0;
                        lastBase = pageInformation.BaseAddress;
                        continue;
                    }

                    pageSize = (int)pageInformation.RegionSize;
                }
                
                Span<byte> span = new Span<byte>(memory, i, Signatures.MaximumLength);

                foreach (Signature signature in Signatures.Patterns)
                {
                    if (signature.HasBeenFound)
                        continue;

                    if (signature.Pattern.Bytes[0] != span[0])
                        continue;

                    span.CopyTo(buffer);

                    bool match = signature.Pattern.Equals(buffer);
                    
                    if (!match)
                        continue;

                    long movAddress = BitConverter.ToInt32(buffer, (int)signature.Offset);

                    // 7 is the size of the full mov ???, [0x14????????] instruction
                    Signatures.Found(signature, i, i + movAddress + 7);
                }

                pageSize--;
            }

            return this;
        }

        public void Results()
        {
            foreach (Signature signature in Signatures)
            {
                if (signature.HasBeenFound)
                    Console.WriteLine("Address {0} {2:X08}", signature.Name, signature.AtAddress, signature.Value);
                else
                    Console.WriteLine("Failed to find pattern for {0}", signature.Name);

            }
        }
    }
}
