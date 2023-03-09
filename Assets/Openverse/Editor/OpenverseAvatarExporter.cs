using System.Collections;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Openverse.Avatars
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public class OpenverseAvatarExporter : EditorWindow
    {
        private GameObject sceneObject;
        private OpenverseAvatar avatar;

        private Texture2D iconCache;
        private bool triedToDownload = false;

        [MenuItem("Openverse/Avatar Exporter")]
        static void Init()
        {
            OpenverseAvatarExporter window = (OpenverseAvatarExporter)GetWindow(typeof(OpenverseAvatarExporter));
            window.titleContent = new GUIContent("Openverse Avatar Exporter");
            window.Show();
        }

        void OnGUI()
        {
            if (avatar == null)
            {
                GUILayout.Label("Avatar Conversion", EditorStyles.boldLabel);
                sceneObject = (GameObject)EditorGUILayout.ObjectField("Your Old Avatar", sceneObject,
                    typeof(GameObject),
                    true, Array.Empty<GUILayoutOption>());
                GUI.enabled = sceneObject != null;
                if (GUILayout.Button("Convert to Openverse avatar")) ConvertAvatar(sceneObject);
                GUI.enabled = true;
            }

            GUILayout.Label("Configure Openverse Avatar", EditorStyles.boldLabel);
            avatar = (OpenverseAvatar)EditorGUILayout.ObjectField("Your Openverse Avatar", avatar,
                typeof(OpenverseAvatar),
                true, Array.Empty<GUILayoutOption>());
            if (avatar != null)
            {
                if (iconCache == null && !triedToDownload) GetIcon();
                if (iconCache != null)
                {
                    int iconSize = 150;
                    GUI.DrawTexture(new Rect(10, EditorGUIUtility.singleLineHeight * 2f + 10f, iconSize, iconSize), iconCache);
                    GUILayout.BeginArea(new Rect(0,iconSize + EditorGUIUtility.singleLineHeight * 2f + 10f + 10f,position.width, position.height - iconSize + EditorGUIUtility.singleLineHeight * 4f));
                    if(GUILayout.Button("Refresh Icon")) GetIcon();
                }
                GUIStyle style = EditorStyles.boldLabel;
                style.richText = true;
                EditorGUILayout.LabelField("Name:",avatar.metadata.name,EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Author:",avatar.metadata.authorName,EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Description:",avatar.metadata.description,style);
                if(GUILayout.Button("Author Website")) Application.OpenURL(avatar.metadata.authorURL);
                if(GUILayout.Button("License")) Application.OpenURL(avatar.metadata.licenseURL);

                if (GUILayout.Button("Configure Avatar"))
                {
                    //Focus inspector
                    EditorWindow foundWindow = null;
                    foreach (var editorWindow in Resources.FindObjectsOfTypeAll<EditorWindow>())
                    {
                        if (editorWindow.titleContent.text.Equals("Inspector", StringComparison.OrdinalIgnoreCase))
                        {
                            foundWindow = editorWindow;
                            foundWindow.Focus();
                        }
                    }
                    if (foundWindow == null) //Open new window
                    {
                        EditorWindow win = GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow")); //This is stupid, thanks unity.
                        win.Show();
                        win.Focus();
                    }
                    Selection.activeObject = avatar;
                }
                GUILayout.EndArea();
            }
        }

        private void GetIcon()
        {
            iconCache = new Texture2D(1024, 1024);
            try
            {
                DownloadImage(avatar.metadata.iconURL);
                triedToDownload = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not get avatar icon image: " + e.Message);
                iconCache = null;
            }
        }
        
        private void ConvertAvatar(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Debug.Log("Converting...");

            }
            else
            {
                Debug.LogError("Please assign a GameObject to convert inside of the scene!");
            }
        }
        
        #region UTILITY
        public void DownloadImage(string MediaUrl)
        {   
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
            request.SendWebRequest().completed += (a) =>
            {
                if (request.result == UnityWebRequest.Result.ConnectionError)
                    Debug.Log(request.error);
                else
                    iconCache = ((DownloadHandlerTexture)request.downloadHandler).texture;
            };
        }
        #endregion
    }
}