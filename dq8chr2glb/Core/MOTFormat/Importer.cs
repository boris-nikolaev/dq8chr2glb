using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace dq8chr2glb.Core.MOTFormat;

public class Importer
{
    public List<MotionCurve> Import(byte[] data)
    {
        var offset = 0;
        var header = Utils.ReadStruct<MOTHeader>(data, ref offset);
        offset += header.bonesOffset;

        var boneCount = header.boneCount;
        var animation = new List<MotionCurve>();
        for (var i = 0; i < boneCount; i++)
        {
            var motionCurves = ReadBoneAnimation(data, ref offset);
            animation.AddRange(motionCurves);
        }

        return animation;
    }

    private List<TransformGroup> GetTransformGroups(byte[] data, BoneHeader header, int offset, out int offsetAfter)
    {
        var attribCount = TransformFlagsHelper.GetAttribCount(header.transformFlags);
        var attribHeadersOffset = attribCount > 1 ? 32 : 16;

        var transformGroups = new List<TransformGroup>();
        var transformGroupOffset = offset;
        for (var i = 0; i < attribCount; i++)
        {
            var group = Utils.ReadStruct<TransformGroup>(data, ref transformGroupOffset, true);
            transformGroups.Add(group);
        }

        offsetAfter = offset + attribHeadersOffset;

        return transformGroups;
    }

    private void ReadRotationKeyframes(byte[] data, MotionCurve curve, int offset)
    {
        curve.curveType = KeyframeType.Quaternion;
        var headerOffset = offset;
        var header = Utils.ReadStruct<KeyframesBlockHeader>(data, ref headerOffset);
        var valuesOffset = headerOffset + header.valuesOffset;

        if (header.toFramesOffset == 0)
        {
            var count = header.framesCount;
            for (var i = 0; i < count; i++)
            {
                var keyframe = new KeyFrame();
                keyframe.frame = i;

                var scale = new Vector4(header.scaleX, header.scaleY,
                                        header.scaleZ, header.scaleW);

                keyframe.rotation = ReadQuaternion(data, valuesOffset, scale);
                curve.keyframes.Add(keyframe);
                valuesOffset += 8;
            }
        }
        else
        {
            var framesCount = header.framesCount;
            var toFramesOffset = headerOffset + header.toFramesOffset;
            var frames = ReadFrames(data, toFramesOffset, framesCount);
            if (frames.Length == 0)
            {
                return;
            }

            var maxFrame = frames.Max();
            curve.keyframes = new List<KeyFrame>();
            for (var i = 0; i < maxFrame + 1; i++)
            {
                curve.keyframes.Add(null);
            }

            var toValues = header.valuesOffset;
            foreach (var frame in frames)
            {
                var scale = new Vector4(header.scaleX, header.scaleY,
                                        header.scaleZ, header.scaleW);

                var keyframe = new KeyFrame();
                keyframe.frame = frame;
                keyframe.type = KeyframeType.Quaternion;
                keyframe.rotation = ReadQuaternion(data, valuesOffset, scale);

                curve.keyframes[frame] = keyframe;
                valuesOffset += 8;
            }
        }
    }

    private void ReadPositionKeyframes(byte[] data, MotionCurve curve, int offset)
    {
        curve.curveType = KeyframeType.Translation;
        var headerOffset = offset;
        var header = Utils.ReadStruct<KeyframesBlockHeader>(data, ref offset, false);
        var valuesOffset = headerOffset + header.valuesOffset;

        if (header.toFramesOffset == 0)
        {
            var count = header.framesCount;
            for (var i = 0; i < count; i++)
            {
                var keyframe = new KeyFrame();
                keyframe.frame = i;

                var scale = new Vector3(header.scaleX, header.scaleY,
                                        header.scaleZ);

                var translation = ReadVector3(data, valuesOffset, scale);
                keyframe.translation = translation;
                curve.keyframes.Add(keyframe);

                valuesOffset += 6;
            }
        }
        else
        {
            var framesCount = header.framesCount;
            var toFramesOffset = headerOffset + header.toFramesOffset;
            var frames = ReadFrames(data, toFramesOffset, framesCount);
            if (frames.Length == 0)
            {
                return;
            }

            var maxFrame = frames.Max();
            curve.keyframes = new List<KeyFrame>();
            for (var i = 0; i < maxFrame + 1; i++)
            {
                curve.keyframes.Add(null);
            }

            var toValues = header.valuesOffset;
            foreach (var frame in frames)
            {
                var scale = new Vector3(header.scaleX, header.scaleY,
                                        header.scaleZ);

                var keyframe = new KeyFrame();
                keyframe.frame = frame;
                keyframe.type = KeyframeType.Translation;
                keyframe.translation = ReadVector3(data, valuesOffset, scale);

                curve.keyframes[frame] = keyframe;
                valuesOffset += 6;
            }
        }
    }

    private List<MotionCurve> ReadBoneAnimation(byte[] data, ref int offset)
    {
        var headerPosition = offset;
        var header = Utils.ReadStruct<BoneHeader>(data, ref offset, true);

        var groupsBlockPosition = offset;
        var transformGroups = GetTransformGroups(data, header, groupsBlockPosition, out var offsetAfter);

        var boneCurves = new List<MotionCurve>();
        foreach (var group in transformGroups)
        {
            var curve = new MotionCurve();
            curve.boneIndex = header.boneIndex;
            curve.keyframes = new List<KeyFrame>();

            switch (group.keyframeType)
            {
                case KeyframeType.Quaternion:
                    ReadRotationKeyframes(data, curve, headerPosition + group.offset);
                    break;
                case KeyframeType.Translation:
                    ReadPositionKeyframes(data, curve, headerPosition + group.offset);
                    break;
                default:
                    // Debug.Log($"[UNK KF TYPE]: {group.keyframeType}");
                    break;
            }

            boneCurves.Add(curve);
        }

        switch (header.transformFlags)
        {
            case TransformFlags.Rotation:
                // Debug.Log($"Rotation: {(int)header.transformFlags}.");
                break;
            case TransformFlags.TR:
                // Debug.Log($"Rot/Pos: {(int)header.transformFlags}");
                break;
            default:
                // Debug.Log($"Unknown flag: {(int)header.transformFlags}, {header.transformFlags}");
                break;
        }

        offset = headerPosition + header.toNext;
        return boneCurves;
    }

    private static Quaternion ReadQuaternion(byte[] data, int offset, Vector4 scale)
    {
        var x = ShortToFloat(data, offset, scale.X);
        offset += 2;
        var y = ShortToFloat(data, offset, scale.Y);
        offset += 2;
        var z = ShortToFloat(data, offset, scale.Z);
        offset += 2;
        var w = ShortToFloat(data, offset, scale.W);
        return new Quaternion(x, y, z, -w);
    }

    private static Vector3 ReadVector3(byte[] data, int offset, Vector3 scale)
    {
        var x = ShortToFloat(data, offset, scale.X);
        offset += 2;
        var y = ShortToFloat(data, offset, scale.Y);
        offset += 2;
        var z = ShortToFloat(data, offset, scale.Z);
        return new Vector3(x, y, z);
    }

    private static float ShortToFloat(byte[] data, int offset, float scale)
    {
        var value = ReadShort(data, offset);
        return (float)value / (float)short.MaxValue * scale;
    }

    private static short ReadShort(byte[] data, int offset)
    {
        return BitConverter.ToInt16(new[] { data[offset], data[offset + 1] });
    }

    public static int[] ReadFrames(byte[] data, int offset, int count)
    {
        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = ReadShort(data, offset + i * 2);
        }

        return result;
    }
}