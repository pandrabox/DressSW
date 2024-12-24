using UnityEngine;
using UnityEditor;
using System;
using VRC.SDK3.Avatars.Components;
using System.IO;
using System.Linq;

namespace com.github.pandrabox.dresssw.editor
{
    public partial class DressSW : EditorWindow
    {
        private VRCAvatarDescriptor TargetAvatar;
        private DressConfig _dressConfig;
        private GUIStyle boldLabelStyle;

        private Vector2 scrollPosition;
        [MenuItem("Pan/DressSW")]
        private static void ShowWindow()
        {
            var window = GetWindow<DressSW>("UIElements");
            window.titleContent = new GUIContent("Pan/DressSW");
            window.Show();
        }

        private void OnGUI()
        {
            if (boldLabelStyle == null)
            {
                boldLabelStyle = new GUIStyle(EditorStyles.label);
                boldLabelStyle.fontStyle = FontStyle.Bold;
            }
            EditorGUILayout.LabelField("処理するアバターを選択：", boldLabelStyle);
            if (TargetAvatar == null)
            {
                TargetAvatar = (VRCAvatarDescriptor)FindObjectOfType<VRCAvatarDescriptor>();
                _dressConfig = new DressConfig(TargetAvatar);
            }
            VRCAvatarDescriptor tDesc = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Target Avatar", TargetAvatar, typeof(VRCAvatarDescriptor), true);
            if (tDesc != TargetAvatar)
            {
                TargetAvatar = tDesc;
                _dressConfig = new DressConfig(TargetAvatar);
            }
            if (GUILayout.Button("Reload"))
            {
                if (_dressConfig == null)
                {
                    _dressConfig = null;
                }
                _dressConfig = new DressConfig(TargetAvatar);
            }

            if (_dressConfig == null) return;


            EditorGUILayout.LabelField("デフォルトで表示する物をチェック：", boldLabelStyle);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var config in _dressConfig.Configs)
            {
                if(config.IsExist)
                {

                    bool tmp = EditorGUILayout.Toggle(config.Key, config.Enable);
                    if (tmp != config.Enable)
                    {
                        config.Enable = tmp;
                        config.apply();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
