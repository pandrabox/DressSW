using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;

namespace com.github.pandrabox.dresssw.editor
{
    /// <summary>
    /// DressConfigはプロジェクト全体から全てのDressSW.txtを読み込んでUnitConfigを生成します
    /// DressSWはカンマ・改行区切りの情報です。１行が「UnitConfig」１つにあたります。
    /// DressSWのフォーマットは次の通りですkey,type,values
    ///     key…ExpressionParameter名
    ///     type…制御タイプのint
    ///         1…MAParametersで定義、Object-Activeで制御[1が有効,0が無効]
    ///         2…ExpressionParametersで定義、Object-Activeで制御
    ///         3…ExpressionParametersで定義、SkinnedMeshRenderersで制御
    ///         4…MAParametersで定義、Object-Activeで制御[0が有効,1が無効]
    ///     values…
    ///         オブジェクトのパス。
    ///         ワイルドカード*が使用可能。
    ///         カンマ区切りで複数設定可能。
    ///         絶対パスの場合は先頭を/にする
    /// 設計思想としては、DressSW.txtは原則それぞれの衣装に添付されているものです。
    /// これによって本アセット自体の更新をせずに対応衣装を増やすことができます。
    /// </summary>
    public class DressConfig
    {
        public string[] Files { get; }
        public List<UnitConfig> Configs { get; }
        public VRCAvatarDescriptor Descriptor { get; }
        public DressConfig(VRCAvatarDescriptor desc)
        {
            Descriptor = desc;
            Files = GetFilesRecursively(Environment.CurrentDirectory, "DressSW.txt");
            Configs = new List<UnitConfig>();
            foreach (var file in Files)
            {
                ParseFile(file);
            }
            NormalizeDefaultState();
        }

        /// <summary>
        /// Type2,3へ干渉するギミックを無効化
        /// </summary>
        private void NormalizeDefaultState()
        {
            var type23s = Configs.Where(x => x.Type == 2 || x.Type == 3);
            var type23Keys = type23s.Select(x => x.Key).ToList();
            var mamas = Descriptor.GetComponentsInChildren<ModularAvatarMergeAnimator>(true);
            foreach (var mama in mamas)
            {
                var controller = (AnimatorController)mama.animator;
                var states = controller?.layers?.SelectMany(x => x.stateMachine.states)?.Select(x => x.state)
                    .Where(state => state.behaviours.OfType<VRCAvatarParameterDriver>().Any()).ToList();
                foreach (var state in states)
                {
                    var driver = state.behaviours.OfType<VRCAvatarParameterDriver>().FirstOrDefault();
                    if (driver == null) continue;
                    for (int i = driver.parameters.Count - 1; i >= 0; i--)
                    {
                        if (type23Keys.Contains(driver.parameters[i].name))
                        {
                            driver.parameters.RemoveAt(i);
                            PrefabUtility.UnpackPrefabInstance(mama.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        }
                    }
                }

                for (int i = controller.parameters.Length - 1; i >= 0; i--)
                {
                    if (type23Keys.Contains(controller.parameters[i].name))
                    {
                        controller.RemoveParameter(controller.parameters[i]);
                        PrefabUtility.UnpackPrefabInstance(mama.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                }
            }
            var animators = Descriptor.GetComponentsInChildren<Animator>(true);
            foreach (var animator in animators)
            {
                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller == null) continue;
                var states = controller.layers.SelectMany(x => x.stateMachine.states).Select(x => x.state)
                    .Where(state => state.behaviours.OfType<VRCAvatarParameterDriver>().Any()).ToList();
                foreach (var state in states)
                {
                    var driver = state.behaviours.OfType<VRCAvatarParameterDriver>().FirstOrDefault();
                    if (driver == null) continue;
                    for (int i = driver.parameters.Count - 1; i >= 0; i--)
                    {
                        if (type23Keys.Contains(driver.parameters[i].name))
                        {
                            driver.parameters.RemoveAt(i);
                            PrefabUtility.UnpackPrefabInstance(animator.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        }
                    }
                }
                for (int i = controller.parameters.Length - 1; i >= 0; i--)
                {
                    if (type23Keys.Contains(controller.parameters[i].name))
                    {
                        controller.RemoveParameter(controller.parameters[i]);
                        PrefabUtility.UnpackPrefabInstance(animator.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                }
            }
            var maps = Descriptor.GetComponentsInChildren<ModularAvatarParameters>(true);
            foreach (var map in maps)
            {
                for (int i = map.parameters.Count - 1; i >= 0; i--)
                {
                    if (type23Keys.Contains(map.parameters[i].nameOrPrefix))
                    {
                        map.parameters.RemoveAt(i);
                        PrefabUtility.UnpackPrefabInstance(map.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                }
            }
        }

        static string[] GetFilesRecursively(string directoryPath, string fileName)
        {
            var files = Directory.GetFiles(directoryPath, fileName);
            var subdirectories = Directory.GetDirectories(directoryPath);
            foreach (var subdirectory in subdirectories)
            {
                files = files.Concat(GetFilesRecursively(subdirectory, fileName)).ToArray();
            }
            return files;
        }

        private void ParseFile(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    //Debug.Log(line);
                    if (!line.Contains(',')) continue; // コンマを持たない行は無視
                    if (line.StartsWith("//")) continue; // // で始まる行は無視
                    var parts = line.Split(',');
                    if (parts.Length > 0)
                    {
                        var key = parts[0].Trim();
                        var values = parts.Skip(2).Select(v => v.Trim()).ToList();
                        var unitConfig = new UnitConfig(Descriptor, parts[0], int.Parse(parts[1]), values);
                        Configs.Add(unitConfig);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error reading file {filePath}: {ex.Message}");
            }
        }

        public UnitConfig get(string key)
        {
            return Configs.Find(x => x.Key == key);
        }
    }


}
