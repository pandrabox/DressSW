using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace com.github.pandrabox.dresssw.editor
{
    /// <summary>
    /// 1ExParameterを単位とする管理クラスです
    /// 実動作のほとんどはここです
    /// </summary>
    public class UnitConfig
    {
        public string Key { get; set; }
        public int Type { get; set; }
        public List<string> Items { get; set; }
        public bool Enable { get; set; }
        public bool IsExist;    //Descriptor以下にTargetTransformsが存在するかどうか。
        public VRCAvatarDescriptor Descriptor { get; }
        public UnitConfig(VRCAvatarDescriptor descriptor, string key, int type, List<string> items)
        {
            Descriptor = descriptor;
            Key = key;
            Type = type;
            Items = items;
            if (Type==1 || Type==2 || Type==4)
            {
                Enable = TargetTransforms().FirstOrDefault()?.gameObject?.activeSelf ?? false;
            }
            else if(Type == 3)
            {
                Enable = TargetTransforms().FirstOrDefault()?.gameObject?.GetComponent<SkinnedMeshRenderer>().enabled ?? false;
            }
            IsExist = IsExistCheck();
        }

        private bool IsExistCheck()
        {
            if (TargetTransforms().Count == 0) return false;
            if (Key.StartsWith("!")) return true;
            if (Type == 1 || Type == 4)
            {
                ModularAvatarParameters[] MAParams = Descriptor.transform.GetComponentsInChildren<ModularAvatarParameters>();
                foreach (var param in MAParams)
                {
                    for (int i = 0; i < param.parameters.Count; i++)
                    {
                        var p = param.parameters[i];
                        if (p.nameOrPrefix == Key)
                        {
                            return true;
                        }
                    }
                }
            }
            else if (Type==2 || Type==3)
            {
                VRCExpressionParameters ExParams = Descriptor.expressionParameters;
                if(ExParams == null) return false;
                for (int i = 0; i < ExParams.parameters.Length; i++)
                {
                    var param = ExParams.parameters[i];
                    if (param.name == Key)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // 当該UnitConfigのEnableを反映する
        public void apply()
        {
            // 初期状態そのものの設定
            foreach (var item in TargetTransforms())
            {
                if (Type == 1 || Type == 2 || Type == 4)
                {
                    item.gameObject.SetActive(Enable);
                    //Debug.LogWarning(item.name);
                }
                if (Type == 3)
                {
                    item.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = Enable;
                }
            }
            // Parameter初期値の設定
            if (Type == 1 || Type == 4)
            {
                ModularAvatarParameters[] MAParams = Descriptor.transform.GetComponentsInChildren<ModularAvatarParameters>();
                foreach (var param in MAParams)
                {
                    for (int i = 0; i < param.parameters.Count; i++)
                    {
                        var p = param.parameters[i];
                        if (p.nameOrPrefix == Key)
                        {
                            p.defaultValue = Enable ? 1 : 0;
                            if (Type == 4) p.defaultValue = 1 - p.defaultValue;
                            param.parameters[i] = p;
                            EditorUtility.SetDirty(param);
                        }
                    }
                }
            }
            if (Type == 2 || Type == 3)
            {
                VRCExpressionParameters ExParams = Descriptor.expressionParameters;
                for (int i = 0; i < ExParams.parameters.Length; i++)
                {
                    var param = ExParams.parameters[i];
                    if (param.name == Key)
                    {
                        param.defaultValue = Enable ? 1 : 0;
                        EditorUtility.SetDirty(ExParams);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                Descriptor.expressionParameters = null;
                Descriptor.expressionParameters = ExParams;
            }
        }

        // 当該UnitConfigに対応するTransformを返す
        private List<Transform> TargetTransforms()
        {
            var res = new List<Transform>();
            if (Descriptor == null) return res;

            foreach (var item in Items)
            {
                var pathSegments = item.Replace("*", ".*").Split('/');
                Transform currentTransform = Descriptor.transform;
                var allDescendants = currentTransform.GetComponentsInChildren<Transform>(true); // true includes the currentTransform itself
                List<Transform> matchingTransforms;
                if (pathSegments[0].Length > 0)
                {
                    matchingTransforms = allDescendants
                        .Where(t =>
                        {
                            string pattern = pathSegments[0];
                            return System.Text.RegularExpressions.Regex.IsMatch(t.name, pattern);
                        })
                        .ToList();
                }
                else
                {
                    matchingTransforms = new List<Transform>() { Descriptor.transform };
                }
                foreach (var child in matchingTransforms)
                {
                    if (pathSegments.Length > 1)
                    {
                        res.AddRange(FindTransformsInDescendants(child, pathSegments.Skip(1).ToArray()));
                    }
                }
            }

            return res;
        }

        // 親から残りの相対パスの配列を与えて取得可能なTransformのリストを返す
        private List<Transform> FindTransformsInDescendants(Transform parent, string[] remainingSegments)
        {
            List<Transform> results = new List<Transform>();
            if (remainingSegments.Length == 0)
            {
                results.Add(parent);
                return results;
            }
            string pattern = remainingSegments[0];
            bool isPartialMatch = pattern.Contains("*");
            // 正規表現パターンを適用する
            var matchingTransforms = parent.Cast<Transform>()
                .Where(t =>
                    t != parent && // 親オブジェクト自身を除外
                    (isPartialMatch
                        ? System.Text.RegularExpressions.Regex.IsMatch(t.name, pattern) // 部分一致
                        : t.name == pattern)) // 完全一致
                .ToList();
            foreach (var match in matchingTransforms)
            {
                var foundTransforms = FindTransformsInDescendants(match, remainingSegments.Skip(1).ToArray());
                results.AddRange(foundTransforms);
            }
            return results;
        }
    }
}
