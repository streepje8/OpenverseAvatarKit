using System;
using System.Collections.Generic;
using UnityEngine;
public class OpenverseBone
{
    public string name;
    public Transform wrappedBone;
    public Vector3 position;
    public Vector3 scale;
    public Quaternion rotation;

    public bool isRootBone;
    public bool isInitialized;
    public OpenverseBone parent;

    public Vector3 WorldPosition
    {
        get
        {
            if (parent == null) return position;
            return parent.WorldPosition + parent.WorldRotation * position;
        }
        set
        {
            Vector3 parentWorldPosition = parent.WorldPosition;
            Quaternion parentWorldRotation = parent.WorldRotation;
            Vector3 localPosition = Quaternion.Inverse(parentWorldRotation) * (value - parentWorldPosition);
            position = localPosition;
        }
    }

    public Quaternion WorldRotation
    {
        get
        {
            if (parent == null) return rotation;
            return parent.WorldRotation * rotation;
        }
        set
        {
            Quaternion parentWorldRotation = parent.WorldRotation;
            Quaternion localRotation = Quaternion.Inverse(parentWorldRotation) * value;
            rotation = localRotation;
        }
    }
    
    public List<OpenverseBone> Children
    {
        get => children;
        set
        {
            if(value != null) foreach (var child in value) child.parent = this;
            children = value;
        }
    }

    private List<OpenverseBone> children;

    public OpenverseBone() { }

    public OpenverseBone(Transform bone, List<OpenverseBone> children = null, bool isRootBone = false)
    {
        name = bone.gameObject.name;
        wrappedBone = bone;
        position = bone.localPosition;
        rotation = bone.localRotation;
        scale = bone.localScale;
        Children = children ?? new List<OpenverseBone>();
        this.isRootBone = isRootBone;
        isInitialized = true;
    }

    public void AppendChild(OpenverseBone bone)
    {
        bone.parent = this;
        Children.Add(bone);
    }

    public void ApplyGPUBone(GPUBone bone)
    {
        position = bone.position;
        rotation = bone.rotation;
    }
}

[Serializable]
public struct GPUBone
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public GPUBone(OpenverseBone bone)
    {
        position = bone.position;
        rotation = bone.rotation;
        scale = bone.scale;
    }
}

[Serializable]
public struct SerializedBone
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public bool isRootBone;
    public List<string> children;

    public SerializedBone(OpenverseBone bone)
    {
        if (bone.Children == null) bone.Children = new List<OpenverseBone>();
        name = bone.name;
        position = bone.position;
        rotation = bone.rotation;
        scale = bone.scale;
        isRootBone = bone.isRootBone;
        children = new List<string>();
        foreach (var openverseBone in bone.Children)
        {
            children.Add(openverseBone.name);
        }
    }
}