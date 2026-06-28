using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class PositionCurveBindingsCollector
    {
        private static List<PositionCurveBinding> s_PositionCurveBindings;

        static class Constants
        {
            public static readonly Type RectTransformType = typeof(RectTransform);
            public static readonly Type TransformType = typeof(Transform);

            public const string m_LocalP = "m_LocalP";
            public const string m_AnchoredPosition = "m_AnchoredPosition";
            public const string x = "x";
            public const string y = "y";
            public const string z = "z";
        }

        public static List<PositionCurveBinding> GetList(AnimationClip clip, Transform root)
        {
            if (s_PositionCurveBindings == null) s_PositionCurveBindings = new List<PositionCurveBinding>();

            s_PositionCurveBindings.Clear();

            PositionCurveBinding positionCurveBinding = null;
            foreach (var curveBinding in AnimationUtility.GetCurveBindings(clip))
            {
                var type = curveBinding.type;

                if (type == Constants.TransformType || type == Constants.RectTransformType)
                {
                    var propertyName = curveBinding.propertyName;

                    var key = type == Constants.TransformType ? Constants.m_LocalP : Constants.m_AnchoredPosition;
                    if (propertyName.StartsWith(key))
                    {
                        var path = curveBinding.path;
                        if (positionCurveBinding == null || positionCurveBinding.path != path)
                        {
                            positionCurveBinding = new PositionCurveBinding();
                            positionCurveBinding.path = path;
                            positionCurveBinding.transform = root.Find(path);
                            s_PositionCurveBindings.Add(positionCurveBinding);
                        }

                        if (propertyName.EndsWith(Constants.x)) positionCurveBinding.x = curveBinding;
                        if (propertyName.EndsWith(Constants.y)) positionCurveBinding.y = curveBinding;
                        if (propertyName.EndsWith(Constants.z)) positionCurveBinding.z = curveBinding;
                    }
                }
            }

            return s_PositionCurveBindings;
        }
    }
}