using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace dq8chr2glb.Core.MOTFormat
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct MOTHeader
    {
        [FieldOffset(00)] public int MagicCode;
        [FieldOffset(08)] public int headerSize;
        [FieldOffset(28)] public int boneCount;
        [FieldOffset(32)] public int bonesOffset;
        [FieldOffset(48)] public int fileSizeClaim;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct BoneHeader
    {
        [FieldOffset(00)] public int toNext;
        [FieldOffset(04)] public int boneIndex;
        [FieldOffset(12)] public TransformFlags transformFlags;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct TransformGroup
    {
        [FieldOffset(00)] public KeyframeType keyframeType;
        [FieldOffset(04)] public int offset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct KeyframesBlockHeader
    {
        [FieldOffset(00)] public short flag0;
        [FieldOffset(02)] public short flag1;
        [FieldOffset(06)] public short framesCount;
        [FieldOffset(08)] public short toFramesOffset;
        [FieldOffset(12)] public short valuesOffset;
        [FieldOffset(16)] public float scaleX;
        [FieldOffset(20)] public float scaleY;
        [FieldOffset(24)] public float scaleZ;
        [FieldOffset(28)] public float scaleW;
    }

    [Flags]
    public enum TransformFlags : short
    {
        None = 0,
        Translation = 1 << 0,
        Rotation = 1 << 1,
        Scale = 1 << 2,

        TR = Translation | Rotation,
        TS = Translation | Scale,
        RS = Rotation | Scale,
        TRS = Translation | Rotation | Scale
    }

    public enum KeyframeType : int
    {
        None = 0,
        Quaternion = 1,
        Translation = 3,
        Scale = 6,
    }

    public class MotionCurve
    {
        public int boneIndex;
        public KeyframeType curveType;
        public List<KeyFrame> keyframes;
    }

    public class KeyFrame
    {
        public int frame;
        public KeyframeType type;
        public Quaternion rotation;
        public Vector3 translation;
        public Vector3 scale;
    }

    public static class TransformFlagsHelper
    {
        public static int GetAttribCount(TransformFlags flag)
        {
            var value = (int)flag;
            return (value & 1) + ((value >> 1) & 1) + ((value >> 2) & 1);
        }
    }
}