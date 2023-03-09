using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct SerializedSkeleton
{
    public List<SerializedBone> bones;
    public SerializedSkeleton(List<SerializedBone> bones) => this.bones = bones;
}

[Serializable]
public class UnitySkeleton
{
    public Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
    public UnitySkeleton(Transform rootBone)
    {
        GenerateLookup(rootBone);
    }
    private void GenerateLookup(Transform bone)
    {
        if (bone == null) return;
        bones.Add(bone.name,bone);
        for(int i = 0; i < bone.childCount; i++) GenerateLookup(bone.GetChild(i));
    }
}

public class OpenverseSkeleton : MonoBehaviour, ISerializationCallbackReceiver
{
    public Transform rootBone;
    public OpenverseBone OpenverseSkeletonRootBone
    {
        get => openverseSkeletonRootBone;
        private set => openverseSkeletonRootBone = value;
    }
    
    private OpenverseBone openverseSkeletonRootBone; //Manually serialized
    
    [SerializeField]public SerializedSkeleton skeleton;

    private void Awake() => CheckSkeletonInitialize();
    private void OnEnable() => CheckSkeletonInitialize();
    private void OnValidate() => CheckSkeletonInitialize();

    public OpenverseBone FindBone(string name)
    {
        return FindBone(openverseSkeletonRootBone, name);
    }

    private OpenverseBone FindBone(OpenverseBone bone, string name)
    {
        if (bone.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return bone;
        OpenverseBone resultInChildren = null;
        foreach (var openverseBone in bone.Children)
        {
            resultInChildren = FindBone(openverseBone, name);
            if (resultInChildren != null) return resultInChildren;
        }
        return null;
    }

    public void ApplySkeletonInterpolated(float positionalT, float rotationalT, float scaleT)
    {
        ApplyBoneInterpolated(OpenverseSkeletonRootBone, positionalT, rotationalT, scaleT,true);
    }

    public void ApplyBoneInterpolated(OpenverseBone bone, float positionalT, float rotationalT, float scaleT, bool chain = false)
    {
        CheckSkeletonInitialize();
        if (bone != null)
        {
            bone.wrappedBone.localPosition = Vector3.Lerp(bone.wrappedBone.localPosition, bone.position, positionalT);
            bone.wrappedBone.localRotation = Quaternion.Slerp(bone.wrappedBone.localRotation, bone.rotation, rotationalT);
            bone.wrappedBone.localScale = Vector3.Lerp(bone.wrappedBone.localScale, bone.scale, scaleT);
            if (chain) foreach (var theBoneChild in bone.Children) ApplyBoneInterpolated(theBoneChild, positionalT, rotationalT, scaleT, true);
        }
    }
    
    public void ApplySkeleton()
    {
        ApplyBone(OpenverseSkeletonRootBone);
    }

    public void ApplyBone(OpenverseBone bone, bool chain = false)
    {
        CheckSkeletonInitialize();
        if (bone != null)
        {
            bone.wrappedBone.localPosition = bone.position;
            bone.wrappedBone.localRotation = bone.rotation;
            bone.wrappedBone.localScale = bone.scale;
            if (chain) foreach (var theBoneChild in bone.Children) ApplyBone(theBoneChild, true);
        }
    }

    private void CheckSkeletonInitialize()
    {
        if (openverseSkeletonRootBone != null && !openverseSkeletonRootBone.isInitialized)
        {
            BindBones(openverseSkeletonRootBone,new UnitySkeleton(rootBone));
        }
    }

    private void BindBones(OpenverseBone openverseBone, UnitySkeleton unitySkeleton)
    {
        if (openverseBone.name.Length > 0)
        {
            if (unitySkeleton.bones.TryGetValue(openverseBone.name, out Transform bone))
            {
                openverseBone.wrappedBone = bone;
                foreach (var openverseBoneChild in openverseBone.Children)
                {
                    BindBones(openverseBoneChild, unitySkeleton);
                }
            }
            else Debug.LogWarning("Could not bind bones for " + openverseBone.name + "!");
        }
    }

    public void ConvertSkeleton(bool keepExisting = false)
    {
        OpenverseSkeletonRootBone = MapSkeletonBone(rootBone, true);
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
        Debug.Log("[SkeletonConverter] Conversion finished!");
    }

    private OpenverseBone MapSkeletonBone(Transform bone, bool isRootBone = false)
    {
        OpenverseBone result = new OpenverseBone(bone, new List<OpenverseBone>(), isRootBone);
        for (int i = 0; i < bone.childCount; i++)
        {
            result.AppendChild(MapSkeletonBone(bone.GetChild(i)));
        }
        return result;
    }

    private void OnDrawGizmos()
    {
        if (OpenverseSkeletonRootBone != null)
        {
            Gizmos.color = Color.green;
            DrawGizmosBone(OpenverseSkeletonRootBone);
            Gizmos.color = Color.white;
        }
    }

    private void DrawGizmosBone(OpenverseBone bone)
    {
        for (int i = 0; i < bone.Children.Count; i++)
        {
            Gizmos.DrawLine(transform.TransformPoint(bone.WorldPosition),transform.TransformPoint(bone.Children[i].WorldPosition));
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.TransformPoint(bone.WorldPosition), transform.TransformPoint(bone.WorldPosition + bone.WorldRotation * Vector3.forward * 0.05f));
            Gizmos.color = Color.green;
            DrawGizmosBone(bone.Children[i]);
        }
    }

    public void OnBeforeSerialize()
    {
        if (openverseSkeletonRootBone != null)
        {
            List<SerializedBone> bones = new List<SerializedBone>();
            GetSerializedBoneList(openverseSkeletonRootBone, ref bones);
            if (skeleton.bones.Count != bones.Count)
            {
                skeleton = new SerializedSkeleton(bones);
                #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                #endif
            }
        }
    }

    private void GetSerializedBoneList(OpenverseBone bone, ref List<SerializedBone> bones)
    {
        bones.Add(new SerializedBone(bone));
        if(bone.Children != null) foreach (var openverseBone in bone.Children) GetSerializedBoneList(openverseBone, ref bones);
    }

    public void OnAfterDeserialize()
    {
        Dictionary<string, SerializedBone> serializedBonesLookup = new Dictionary<string, SerializedBone>();
        foreach (var serializedBone in skeleton.bones)
        {
            serializedBonesLookup.Add(serializedBone.name,serializedBone);
        }
        OpenverseSkeletonRootBone = GetOpenverseBone(serializedBonesLookup, skeleton.bones[0]);
    }

    private OpenverseBone GetOpenverseBone(Dictionary<string, SerializedBone> serializedBonesLookup, SerializedBone skeletonBone)
    {
        List<OpenverseBone> children = new List<OpenverseBone>();
        foreach (var skeletonBoneChild in skeletonBone.children)
        {
            if (serializedBonesLookup.TryGetValue(skeletonBoneChild, out SerializedBone childBone))
            {
                children.Add(GetOpenverseBone(serializedBonesLookup,childBone));
            }
            else Debug.LogError("Could not deserialize the child with the name " + childBone.name + ". This can only happen when the lookup is modified or your skeleton is corrupt!");
        }
        return new OpenverseBone()
        {
            position = skeletonBone.position,
            rotation = skeletonBone.rotation,
            name = skeletonBone.name,
            Children = children,
            isRootBone = skeletonBone.isRootBone,
            isInitialized = false
        };
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(OpenverseSkeleton))]
public class OpenverseSkeletonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Label("Openverse Skeleton Bone Count: " + ((OpenverseSkeleton)target).skeleton.bones.Count);
        if(GUILayout.Button("Convert Skeleton")) ((OpenverseSkeleton)target).ConvertSkeleton();
    }
}
#endif
