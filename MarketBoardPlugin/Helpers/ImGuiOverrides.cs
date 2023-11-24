// <copyright file="ImGuiOverrides.cs" company="Florian Maunier">
// Copyright (c) Florian Maunier. All rights reserved.
// </copyright>

namespace MarketBoardPlugin.Helpers
{
  using System;
  using System.Runtime.CompilerServices;
  using System.Text;

  using ImGuiNET;

  /// <summary>
  /// ImGui.NET overrides.
  /// </summary>
  public static unsafe class ImGuiOverrides
  {
    /// <summary>
    /// Create an Input Text with Hint widget.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="hint">The hint.</param>
    /// <param name="input">The input.</param>
    /// <param name="maxLength">The max length.</param>
    /// <returns>A boolean value indicating if the input has been created or not.</returns>
    public static bool InputTextWithHint(
      string label,
      string hint,
      ref string input,
      uint maxLength)
    {
      return InputTextWithHint(label, hint, ref input, maxLength, 0, null, IntPtr.Zero);
    }

    /// <summary>
    /// Create an Input Text with Hint widget.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="hint">The hint.</param>
    /// <param name="input">The input.</param>
    /// <param name="maxLength">The max length.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="callback">The callback.</param>
    /// <param name="userData">The user data.</param>
    /// <returns>A boolean value indicating if the input has been created or not.</returns>
    public static bool InputTextWithHint(
      string label,
      string hint,
      ref string input,
      uint maxLength,
      ImGuiInputTextFlags flags,
      ImGuiInputTextCallback callback,
      IntPtr userData)
    {
      ArgumentNullException.ThrowIfNull(label);

      ArgumentNullException.ThrowIfNull(hint);

      ArgumentNullException.ThrowIfNull(input);

      var utf8LabelByteCount = Encoding.UTF8.GetByteCount(label);
      byte* utf8LabelBytes;
      if (utf8LabelByteCount > UnsafeUtilities.StackAllocationSizeLimit)
      {
        utf8LabelBytes = UnsafeUtilities.Allocate(utf8LabelByteCount + 1);
      }
      else
      {
        var stackPtr = stackalloc byte[utf8LabelByteCount + 1];
        utf8LabelBytes = stackPtr;
      }

      UnsafeUtilities.GetUtf8(label, utf8LabelBytes, utf8LabelByteCount);

      var utf8HintByteCount = Encoding.UTF8.GetByteCount(hint);
      byte* utf8HintBytes;
      if (utf8HintByteCount > UnsafeUtilities.StackAllocationSizeLimit)
      {
        utf8HintBytes = UnsafeUtilities.Allocate(utf8HintByteCount + 1);
      }
      else
      {
        var stackPtr = stackalloc byte[utf8HintByteCount + 1];
        utf8HintBytes = stackPtr;
      }

      UnsafeUtilities.GetUtf8(hint, utf8HintBytes, utf8HintByteCount);

      var utf8InputByteCount = Encoding.UTF8.GetByteCount(input);
      var inputBufSize = Math.Max((int)maxLength + 1, utf8InputByteCount + 1);

      byte* utf8InputBytes;
      byte* originalUtf8InputBytes;
      if (inputBufSize > UnsafeUtilities.StackAllocationSizeLimit)
      {
        utf8InputBytes = UnsafeUtilities.Allocate(inputBufSize);
        originalUtf8InputBytes = UnsafeUtilities.Allocate(inputBufSize);
      }
      else
      {
        var inputStackBytes = stackalloc byte[inputBufSize];
        utf8InputBytes = inputStackBytes;
        var originalInputStackBytes = stackalloc byte[inputBufSize];
        originalUtf8InputBytes = originalInputStackBytes;
      }

      UnsafeUtilities.GetUtf8(input, utf8InputBytes, inputBufSize);
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
      if (!UnsafeUtilities.AreStringsEqual(originalUtf8InputBytes, inputBufSize, utf8InputBytes))
      {
        input = UnsafeUtilities.StringFromPtr(utf8InputBytes);
      }

      if (utf8LabelByteCount > UnsafeUtilities.StackAllocationSizeLimit)
      {
        UnsafeUtilities.Free(utf8LabelBytes);
      }

      if (utf8HintByteCount > UnsafeUtilities.StackAllocationSizeLimit)
      {
        UnsafeUtilities.Free(utf8HintBytes);
      }

      if (inputBufSize > UnsafeUtilities.StackAllocationSizeLimit)
      {
        UnsafeUtilities.Free(utf8InputBytes);
        UnsafeUtilities.Free(originalUtf8InputBytes);
      }

      return result != 0;
    }
  }
}
