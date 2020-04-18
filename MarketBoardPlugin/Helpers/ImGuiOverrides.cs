// <copyright file="ImGuiOverrides.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Runtime.CompilerServices;
  using System.Text;

  using ImGuiNET;

  public static unsafe class ImGuiOverrides
  {
    public static bool InputTextWithHint(
      string label,
      string hint,
      ref string input,
      uint maxLength)
    {
      return InputTextWithHint(label, hint, ref input, maxLength, 0, null, IntPtr.Zero);
    }

    public static bool InputTextWithHint(
      string label,
      string hint,
      ref string input,
      uint maxLength,
      ImGuiInputTextFlags flags,
      ImGuiInputTextCallback callback,
      IntPtr userData)
    {
      if (label == null)
      {
        throw new ArgumentNullException(nameof(label));
      }

      if (hint == null)
      {
        throw new ArgumentNullException(nameof(hint));
      }

      if (input == null)
      {
        throw new ArgumentNullException(nameof(input));
      }

      if (callback == null)
      {
        throw new ArgumentNullException(nameof(callback));
      }

      if (label == null)
      {
        throw new ArgumentNullException(nameof(label));
      }

      var utf8LabelByteCount = Encoding.UTF8.GetByteCount(label);
      byte* utf8LabelBytes;
      if (utf8LabelByteCount > Util.StackAllocationSizeLimit)
      {
        utf8LabelBytes = Util.Allocate(utf8LabelByteCount + 1);
      }
      else
      {
        var stackPtr = stackalloc byte[utf8LabelByteCount + 1];
        utf8LabelBytes = stackPtr;
      }

      Util.GetUtf8(label, utf8LabelBytes, utf8LabelByteCount);

      var utf8HintByteCount = Encoding.UTF8.GetByteCount(hint);
      byte* utf8HintBytes;
      if (utf8HintByteCount > Util.StackAllocationSizeLimit)
      {
        utf8HintBytes = Util.Allocate(utf8HintByteCount + 1);
      }
      else
      {
        var stackPtr = stackalloc byte[utf8HintByteCount + 1];
        utf8HintBytes = stackPtr;
      }

      Util.GetUtf8(hint, utf8HintBytes, utf8HintByteCount);

      var utf8InputByteCount = Encoding.UTF8.GetByteCount(input);
      var inputBufSize = Math.Max((int)maxLength + 1, utf8InputByteCount + 1);

      byte* utf8InputBytes;
      byte* originalUtf8InputBytes;
      if (inputBufSize > Util.StackAllocationSizeLimit)
      {
        utf8InputBytes = Util.Allocate(inputBufSize);
        originalUtf8InputBytes = Util.Allocate(inputBufSize);
      }
      else
      {
        var inputStackBytes = stackalloc byte[inputBufSize];
        utf8InputBytes = inputStackBytes;
        var originalInputStackBytes = stackalloc byte[inputBufSize];
        originalUtf8InputBytes = originalInputStackBytes;
      }

      Util.GetUtf8(input, utf8InputBytes, inputBufSize);
      var clearBytesCount = (uint)(inputBufSize - utf8InputByteCount);
      Unsafe.InitBlockUnaligned(utf8InputBytes + utf8InputByteCount, 0, clearBytesCount);
      Unsafe.CopyBlock(originalUtf8InputBytes, utf8InputBytes, (uint)inputBufSize);

      var result = ImGuiNative.igInputTextWithHint(
        utf8LabelBytes,
        utf8HintBytes,
        utf8InputBytes,
        (uint)inputBufSize,
        flags,
        callback,
        userData.ToPointer());
      if (!Util.AreStringsEqual(originalUtf8InputBytes, inputBufSize, utf8InputBytes))
      {
        input = Util.StringFromPtr(utf8InputBytes);
      }

      if (utf8LabelByteCount > Util.StackAllocationSizeLimit)
      {
        Util.Free(utf8LabelBytes);
      }

      if (utf8HintByteCount > Util.StackAllocationSizeLimit)
      {
        Util.Free(utf8HintBytes);
      }

      if (inputBufSize > Util.StackAllocationSizeLimit)
      {
        Util.Free(utf8InputBytes);
        Util.Free(originalUtf8InputBytes);
      }

      return result != 0;
    }
  }
}
