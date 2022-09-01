// <copyright file="UnsafeUtilities.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Runtime.InteropServices;
  using System.Text;

  internal static unsafe class UnsafeUtilities
  {
    internal const int StackAllocationSizeLimit = 2048;

    public static string StringFromPtr(byte* ptr)
    {
      var characters = 0;
      while (ptr[characters] != 0)
      {
        characters++;
      }

      return Encoding.UTF8.GetString(ptr, characters);
    }

    internal static bool AreStringsEqual(byte* a, int aLength, byte* b)
    {
      for (var i = 0; i < aLength; i++)
      {
        if (a[i] != b[i])
        {
          return false;
        }
      }

      if (b[aLength] != 0)
      {
        return false;
      }

      return true;
    }

    internal static byte* Allocate(int byteCount)
    {
      return (byte*)Marshal.AllocHGlobal(byteCount);
    }

    internal static void Free(byte* ptr)
    {
      Marshal.FreeHGlobal((IntPtr)ptr);
    }

    internal static int GetUtf8(string s, byte* utf8Bytes, int utf8ByteCount)
    {
      fixed (char* utf16Ptr = s)
      {
        return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
      }
    }
  }
}
