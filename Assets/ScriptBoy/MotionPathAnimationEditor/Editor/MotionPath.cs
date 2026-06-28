using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace ScriptBoy.MotionPathAnimEditor
{
    [Serializable]
    class MotionPath
    {
        public MotionPath(Transform target)
        {
            m_Transform = target;
            m_TransformPath = AnimationUtility.CalculateTransformPath(m_Transform, AnimEditor.root);
            Reload();
        }

        public MotionPath(string transformPath)
        {
            m_TransformPath = transformPath;
            Reload();
        }

        public void Reload()
        {
            m_Transform = AnimEditor.root.Find(m_TransformPath);
            m_Curves = new AnimationCurve[3];
            m_Keyframes = new Keyframe[3][];
            m_HasAxis = new bool[3];
            m_ControlHandleSequence = new ControlHandleSequence();
            m_ControlHandleSortedDictionary = new SortedDictionary<float, ControlHandle>();
            m_ControlHandles = new List<ControlHandle>();
            m_SelectedControlIndexList = new List<int>();
            m_Path = new List<Vector3>();
            m_Frames = new List<Vector3>();
        }

        #region Variables
        [SerializeField] string m_TransformPath;
        [SerializeField] bool m_Active = true;
        [SerializeField] bool m_Edit;
        [SerializeField] bool m_Loop;
        [SerializeField] bool m_AutoRotation;
        [SerializeField] Vector3 m_Upwards = Vector3.up;
        [SerializeField] bool m_Use2DRotation = true;
        [SerializeField] Vector3 m_RotationOffset;

        Transform m_Transform;
        PositionCurveBinding m_PositionCurveBinding;

        AnimationCurve[] m_Curves;
        Keyframe[][] m_Keyframes;
        bool[] m_HasAxis;
        int m_AxisCount;

        ControlHandleSequence m_ControlHandleSequence;
        SortedDictionary<float, ControlHandle> m_ControlHandleSortedDictionary;
        List<ControlHandle> m_ControlHandles;

        List<Vector3> m_Path;
        List<Vector3> m_Frames;

        float m_MinVelocity;
        float m_MaxVelocity;

        List<int> m_SelectedControlIndexList;
        #endregion

        #region Properties

        public Transform transform => m_Transform;

        public string transformPath => m_TransformPath;

        public bool hasCurveData => m_AxisCount > 1;

        public bool isEditable => m_Active && editable && hasCurveData;

        public string name
        {
            get
            {
                return m_Transform.name;
            }
        }

        public string fullName
        {
            get
            {
                if (m_TransformPath == "") return AnimEditor.root.name;

                return AnimEditor.root.name + "/" + m_TransformPath;
            }
        }

        public bool active
        {
            get => m_Active;
            set => m_Active = value;
        }

        public bool editable
        {
            get => m_Edit || !Settings.showPathEditButton;
            set => m_Edit = value;
        }

        public bool loop
        {
            get => m_Loop;
            set
            {
                if (m_Loop != value)
                {
                    m_Loop = value;

                    if (m_Loop && m_ControlHandles.Count > 1)
                    {
                        m_ControlHandles[0].SetDirty();
                        ApplyChages();
                    }
                }
            }
        }

        Matrix4x4 localToWorldMatrix
        {
            get
            {
                Matrix4x4 m;
                var parent = m_Transform.parent;
                m = parent == null ? Matrix4x4.identity : parent.localToWorldMatrix;

                if (Settings.useRootOffset && transform == AnimEditor.root)
                {
                    m *= RootOffset.matrix;
                }

                return m;
            }
        }


        public bool isMissing => m_Transform == null;
        #endregion

        #region Methods

        public void OnSettingsGUI(MotionPathClip motionPathClip)
        {

            using (new CustomGUILayout.FoldoutWindowScope($"Motion Path: {name}", "Motion Path", out bool open))
            {
                if (open)
                {

                    EditorGUIUtility.wideMode = true;

                    bool loop = m_Loop;
                    loop = EditorGUILayout.ToggleLeft("Loop", loop);
                    if (m_Loop != loop)
                    {
                        Undo.RecordObject(motionPathClip, "MotionPath Loop");
                        this.loop = loop;
                    }


                    bool autoRotation = m_AutoRotation;

                    autoRotation = EditorGUILayout.ToggleLeft("Auto Rotation", autoRotation);

                    if (m_AutoRotation != autoRotation)
                    {
                        Undo.RecordObject(motionPathClip, "MotionPath AutoRotation");
                        m_AutoRotation = autoRotation;
                        OnRotationSettingsChanged();
                    }

                    if (m_AutoRotation)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        bool use2DRotation = m_Use2DRotation;
                        Vector3 rotationOffset = m_RotationOffset;
                        Vector3 upwards = m_Upwards;

                        use2DRotation = EditorGUILayout.ToggleLeft("Use 2D Rotation", use2DRotation);
                        rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);
                        if (!use2DRotation) upwards = EditorGUILayout.Vector3Field("Rotation Upwards", upwards);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(motionPathClip, "MotionPath Rotation Settings");
                            m_Use2DRotation = use2DRotation;
                            m_RotationOffset = rotationOffset;
                            m_Upwards = upwards;

                            OnRotationSettingsChanged();
                        }

                        EditorGUILayout.LabelField("Current Local Rotation: " + transform.localRotation.eulerAngles);

                        EditorGUI.indentLevel--;
                    }

                    EditorGUIUtility.wideMode = false;

                }
            }
        }

        void OnRotationSettingsChanged()
        {
            Undo.RecordObject(AnimEditor.animationClip, "MotionPath Rotation Settings");
            GenerateRotationKeys();
            AnimEditor.animationWindow.RefreshState();
            SceneView.RepaintAll();
        }

        public void GenerateRotationKeys()
        {
            if (isMissing) return;
            if (!hasCurveData) return;

            var controlHandles = m_ControlHandles;

            int controlHandleCount = controlHandles.Count;
            if (controlHandleCount < 2) return;


            AnimationClip animationClip = AnimEditor.animationClip;

        
            float frameToTime = 1 / animationClip.frameRate;
            float time2Frame = animationClip.frameRate;


            float startTime = controlHandles[0].time;
            float endTime = controlHandles[controlHandleCount - 1].time;

            float duration = endTime - startTime;
            int frameCount = (int)(duration * animationClip.frameRate);

            if (frameCount == 0) return;
            frameCount++;

            Keyframe[] xKeys = new Keyframe[frameCount];
            Keyframe[] yKeys = new Keyframe[frameCount];
            Keyframe[] zKeys = new Keyframe[frameCount];

            GameObject go = AnimEditor.root.gameObject;
            float t = AnimEditor.animationWindow.time;


            List<Vector3> frames = new List<Vector3>();

            for (int i = 0; i < frameCount; i++)
            {
                float time = i * frameToTime + startTime;
                animationClip.SampleAnimation(go, time);
                Vector3 position = transform.position;
                frames.Add(position);
            }



            Vector3 prevEulerAngles = Vector3.zero;
            Vector3 upwards = m_Upwards;
            for (int i = 0; i < frameCount; i++)
            {
                float time = i * frameToTime + startTime;

                Vector3 dir;

                if (i > 0 && i < frameCount - 1 || m_Loop)
                {
                    int ia = i - 1;
                    int ib = i;
                    int ic = i + 1;

                    if (m_Loop)
                    {
                        if (i == 0)
                        {
                            ia = frameCount - 2;
                        }
                        else if (i == frameCount - 1)
                        {
                            ic = 1;
                        }
                    }

                    Vector3 a = frames[ia];
                    Vector3 b = frames[ib];
                    Vector3 c = frames[ic];

                    Vector3 ab = (b - a).normalized;
                    Vector3 bc = (c - b).normalized;
                    dir = ((ab + bc) / 2).normalized;
                }
                else if (i == 0)
                {
                    dir = (frames[1] - frames[0]).normalized;
                }
                else
                {
                    dir = (frames[frameCount - 1] - frames[frameCount - 2]).normalized;
                }

                if (m_Use2DRotation) { }

                Quaternion rotation = m_Use2DRotation ? Quaternion.LookRotation(Vector3.forward, dir) : Quaternion.LookRotation(dir, upwards);
                rotation *= Quaternion.Euler(m_RotationOffset);
                Vector3 eulerAngles = rotation.eulerAngles;

                if (i != 0)
                {
                    eulerAngles = GetClosestEulerAngles(prevEulerAngles, eulerAngles);
                }

                prevEulerAngles = eulerAngles;

                xKeys[i] = new Keyframe(time, (float)Math.Round(eulerAngles.x, 5));
                yKeys[i] = new Keyframe(time, (float)Math.Round(eulerAngles.y, 5));
                zKeys[i] = new Keyframe(time, (float)Math.Round(eulerAngles.z, 5));
            }

            AnimationCurve xCurve = new AnimationCurve(xKeys);
            AnimationCurve yCurve = new AnimationCurve(yKeys);
            AnimationCurve zCurve = new AnimationCurve(zKeys);




            for (int i = 0; i < frameCount; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(xCurve, i, TangentMode.Linear);
                AnimationUtility.SetKeyLeftTangentMode(yCurve, i, TangentMode.Linear);
                AnimationUtility.SetKeyLeftTangentMode(zCurve, i, TangentMode.Linear);

                AnimationUtility.SetKeyRightTangentMode(xCurve, i, TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(yCurve, i, TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(zCurve, i, TangentMode.Linear);
            }


            HashSet<int> hotFrames = new HashSet<int>();

            StartCachingWorldPath(frameCount, time2Frame, hotFrames);

            for (int i = 0; i < frameCount; i++)
            {
                int j = frameCount - i - 1;

                if (hotFrames.Contains(j)) continue;
                if (j % 5 == 0) continue;

                xCurve.RemoveKey(j);
                yCurve.RemoveKey(j);
                zCurve.RemoveKey(j);
            }


            EditorCurveBinding xCurveBinding = new EditorCurveBinding();
            EditorCurveBinding yCurveBinding = new EditorCurveBinding();
            EditorCurveBinding zCurveBinding = new EditorCurveBinding();

            xCurveBinding.type = typeof(Transform);
            yCurveBinding.type = typeof(Transform);
            zCurveBinding.type = typeof(Transform);

            xCurveBinding.propertyName = "localEulerAnglesRaw.x";
            yCurveBinding.propertyName = "localEulerAnglesRaw.y";
            zCurveBinding.propertyName = "localEulerAnglesRaw.z";

            xCurveBinding.path = m_TransformPath;
            yCurveBinding.path = m_TransformPath;
            zCurveBinding.path = m_TransformPath;


            AnimationUtility.SetEditorCurve(AnimEditor.animationClip, xCurveBinding, xCurve);
            AnimationUtility.SetEditorCurve(AnimEditor.animationClip, yCurveBinding, yCurve);
            AnimationUtility.SetEditorCurve(AnimEditor.animationClip, zCurveBinding, zCurve);

            animationClip.SampleAnimation(go, t);
        }

        public static Vector3 GetClosestEulerAngles(Vector3 current, Vector3 target)
        {
            current.x = GetClosestAngle(current.x, target.x);
            current.y = GetClosestAngle(current.y, target.y);
            current.z = GetClosestAngle(current.z, target.z);
            return current;
        }

        public static float GetClosestAngle(float current, float target)
        {
            float delta = Mathf.DeltaAngle(current, target);
            current += delta;
            return current;
        }

        public void SetAnimationCurves(PositionCurveBinding positionCurveBinding)
        {
            m_PositionCurveBinding = positionCurveBinding;
            m_AxisCount = 0;

            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                AnimationCurve curve;
                Keyframe[] keyframes;

                if (m_HasAxis[axisIndex] = positionCurveBinding.HasBinding(axisIndex))
                {
                    EditorCurveBinding binding = positionCurveBinding.GetBinding(axisIndex);
                    curve = AnimationUtility.GetEditorCurve(AnimEditor.animationClip, binding);
                    keyframes = curve.keys;
                    m_AxisCount++;
                }
                else
                {
                    curve = null;
                    keyframes = null;
                }
   
                m_Curves[axisIndex] = curve;
                m_Keyframes[axisIndex] = keyframes;
            }
        }

        public void ClearAnimationCurves()
        {
            m_AxisCount = 0;
            for (int i = 0; i < 3; i++)
            {
                m_Curves[i] = null;
                m_HasAxis[i] = false;
                m_Keyframes[i] = null;
            }
        }

        public void UpdateHandlesMatrix()
        {
            if (isMissing) return;

            Matrix4x4 matrix = localToWorldMatrix;
            int handleCount = m_ControlHandles.Count;
            for (int i = 0; i < handleCount; i++)
            {
                m_ControlHandles[i].SetMatrix(matrix);
            }
        }

        public void UpdateHandles()
        {
            if (isMissing) return;
            if (!hasCurveData) return;

            m_ControlHandleSortedDictionary.Clear();
            m_ControlHandles.Clear();
            m_ControlHandleSequence.Reset();

            for (int j = 0; j < 3; j++)
            {
                if (m_HasAxis[j])
                {
                    IList animationWindowKeyframes = m_PositionCurveBinding.animationWindowKeyframes[j];
                    Keyframe[] keys = m_Keyframes[j];
                    int n = keys.Length;
                    for (int i = 0; i < n; i++)
                    {
                        Keyframe key = keys[i];
                        float time = key.time;
                        ControlHandle handle;

                        if (!m_ControlHandleSortedDictionary.TryGetValue(time, out handle))
                        {
                            handle = m_ControlHandleSequence.Next();
                            handle.Reset();
                            handle.time = time;
                            m_ControlHandleSortedDictionary.Add(time, handle);
                        }

                        if (animationWindowKeyframes != null) handle.animationWindowKeyframes[j] = animationWindowKeyframes[i];
                        handle.keyframeIndexes[j] = i;
                        handle.localPosition[j] = key.value;
                        handle.leftTangent.localPosition[j] = key.inTangent;
                        handle.rightTangent.localPosition[j] = key.outTangent;
                    }
                }
            }

            m_ControlHandles.AddRange(m_ControlHandleSortedDictionary.Values);

            int controlHandleCount = m_ControlHandles.Count;


            for (int i = 0; i < controlHandleCount; i++)
            {

                ControlHandle prevHandle = m_ControlHandles[LoopUtility.Mod(i - 1, controlHandleCount)];
                ControlHandle handle = m_ControlHandles[i];
                ControlHandle nextHandle = m_ControlHandles[LoopUtility.Mod(i + 1, controlHandleCount)];

                float currentTime = handle.time;

                float prevTime = prevHandle.time;
                float nextTime = nextHandle.time;
                float leftTangentScale = (prevTime - currentTime) / 3;
                float rightTangentScale = (currentTime - nextTime) / 3;
                leftTangentScale = -Mathf.Abs(leftTangentScale);
                rightTangentScale = -Mathf.Abs(rightTangentScale);


                if (i == 0)
                {
                    float a = prevTime;
                    float b = m_ControlHandles[LoopUtility.Mod(i - 2, controlHandleCount)].time;

                    leftTangentScale = (b - a) / 3;
                }

                Vector3 handlePosition = handle.localPosition;
                Vector3 inTangents = handle.leftTangent.localPosition;
                Vector3 outTangents = handle.rightTangent.localPosition;


                TangentMode oldLeftTangentMode = TangentMode.Free;
                TangentMode oldRightTangentMode = TangentMode.Free;
                TangentHandle tangentLeft = handle.leftTangent;
                TangentHandle tangentRight = handle.rightTangent;
                tangentLeft.hasSimilarModes = true;
                tangentRight.hasSimilarModes = true;

                int axisCount = 0;
                int inTanCount = 0;
                int outTanCount = 0;

                for (int j = 0; j < 3; j++)
                {
                    AnimationCurve curve = m_Curves[j];
                    Keyframe[] keys = m_Keyframes[j];
                    int nKeys = keys.Length;

                    int keyIndex = handle.keyframeIndexes[j];
                    int prevKeyIndex;
                    int nextKeyIndex;
                    if (keyIndex == -1)
                    {
          
                        handlePosition[j] = curve.Evaluate(currentTime);
                        prevKeyIndex = 0;
                        nextKeyIndex = nKeys - 1;

                        for (int w = 0; w < nKeys; w++)
                        {
                            float t = keys[w].time;
                            if (t < currentTime) prevKeyIndex = Mathf.Max(prevKeyIndex, w);
                            if (t > currentTime) nextKeyIndex = Mathf.Min(nextKeyIndex, w);
                        }

                        if (prevKeyIndex == nextKeyIndex)
                        {

                        }
                        else
                        {
                            Keyframe prevKey = keys[prevKeyIndex];
                            Keyframe nextKey = keys[nextKeyIndex];

                            Keyframe k = AnimationCurveUtility.CreateKeyframeBetween(prevKey, nextKey, currentTime);

                            inTangents[j] = k.inTangent;
                            outTangents[j] = k.outTangent;
                        }
                    }
                    else
                    {
                        TangentMode leftTangentMode = AnimationUtility.GetKeyLeftTangentMode(curve, keyIndex);
                        TangentMode rightTangentMode = AnimationUtility.GetKeyRightTangentMode(curve, keyIndex);

                        tangentLeft.modes[j] = leftTangentMode;
                        tangentRight.modes[j] = rightTangentMode;

                        if (axisCount != 0)
                        {
                            tangentLeft.hasSimilarModes &= oldLeftTangentMode == leftTangentMode;
                            tangentRight.hasSimilarModes &= oldRightTangentMode == rightTangentMode;
                        }

                        oldLeftTangentMode = leftTangentMode;
                        oldRightTangentMode = rightTangentMode;



                        axisCount++;

                        if (keyIndex == 0 && i != 0)
                        {
                            inTanCount++;
                            inTangents[j] = 0;
                        }

                        if (keyIndex == nKeys - 1 && i != controlHandleCount - 1)
                        {
                            outTanCount++;
                            outTangents[j] = 0;
                        }
                    }
                }

                float inTangentsM = inTangents.magnitude;
                float outTangentsM = outTangents.magnitude;
                float tangentsRaito = inTangentsM / outTangentsM;
                bool hasParallelTangents = inTangents / inTangentsM == outTangents / outTangentsM;

                if (inTangents == outTangents && inTangents == Vector3.zero || inTangents.normalized == -outTangents.normalized)
                {
                    hasParallelTangents = true;
                    tangentsRaito = 1;
                }

                inTangents = handlePosition + inTangents * leftTangentScale;
                outTangents = handlePosition - outTangents * rightTangentScale;

     

                handle.localPosition = handlePosition;
                tangentLeft.localPosition = inTangents;
                tangentRight.localPosition = outTangents;
                tangentLeft.scale = leftTangentScale;
                tangentRight.scale = rightTangentScale;

                tangentLeft.mode = oldLeftTangentMode;
                tangentRight.mode = oldRightTangentMode;


                tangentLeft.hide = i == 0 || axisCount == inTanCount && axisCount > 0;
                tangentRight.hide = i == controlHandleCount - 1 || axisCount == outTanCount && axisCount > 0;
                handle.hide = !TimeRange.Contains(currentTime);
                handle.hasChanged = false;
                tangentLeft.hasChanged = false;
                tangentRight.hasChanged = false;
                handle.tangentsRaito = tangentsRaito;
                handle.hasParallelTangents = hasParallelTangents;
            }

            if (m_Loop)
            {
                var start = m_ControlHandles[0];
                var last = m_ControlHandles[controlHandleCount - 1];

                start.hide = start.hide && last.hide;

                start.leftTangent.hide = false;
                last.hide = true;
                last.position = start.position;


                if (last.leftTangent.mode != start.leftTangent.mode)
                {
                    last.leftTangent.mode = start.leftTangent.mode;
                    for (int i = 0; i < 3; i++)
                    {
                        last.leftTangent.modes[i] = start.leftTangent.modes[i];
                    }
                    last.leftTangent.hasSimilarModes = start.leftTangent.hasSimilarModes;

                    SetTangantModes(controlHandleCount - 1, start.leftTangent.modes);

                    last.leftTangent.position = start.leftTangent.position;
                    start.leftTangent.hasChanged = true;
                    GUI.changed = true;
                }

                if (last.leftTangent.mode == TangentMode.Free)
                    last.leftTangent.position = start.leftTangent.position;

                if (last.rightTangent.mode == TangentMode.Free)
                    last.rightTangent.position = start.rightTangent.position;
            }
        }

        /*
        public void UpdateHandles()
        {
            if (isMissing) return;

            if (!hasCurveData) return;
            Keyframe[] xKeys = m_HasXAxis ? m_XCurve.keys : null;
            Keyframe[] yKeys = m_HasYAxis ? m_YCurve.keys : null;
            Keyframe[] zKeys = m_HasZAxis ? m_ZCurve.keys : null;

            var curve = GetFirstActiveCurve();
            Keyframe[] keys = curve.keys;

            int keyCount = keys.Length;

            for (int i = 0; i < keyCount; i++)
            {
                float prevTime = keys[LoopUtility.Mod(i - 1, keyCount)].time;
                float currentTime = keys[i].time;
                float nextTime = keys[LoopUtility.Mod(i + 1, keyCount)].time;
                float leftTangentScale = (prevTime - currentTime) / 3;
                float rightTangentScale = (currentTime - nextTime) / 3;

                leftTangentScale = -Mathf.Abs(leftTangentScale);
                rightTangentScale = -Mathf.Abs(rightTangentScale);

                if (i == 0)
                {
                    float a = keys[LoopUtility.Mod(i - 1, keyCount)].time;
                    float b = keys[LoopUtility.Mod(i - 2, keyCount)].time;

                    leftTangentScale = (b - a) / 3;
                }

                Vector3 handlePosition;
                handlePosition.x = m_HasXAxis ? xKeys[i].value : 0;
                handlePosition.y = m_HasYAxis ? yKeys[i].value : 0;
                handlePosition.z = m_HasZAxis ? zKeys[i].value : 0;

                Vector3 inTangents;
                inTangents.x = m_HasXAxis ? xKeys[i].inTangent : 0;
                inTangents.y = m_HasYAxis ? yKeys[i].inTangent : 0;
                inTangents.z = m_HasZAxis ? zKeys[i].inTangent : 0;

                Vector3 outTangents;
                outTangents.x = m_HasXAxis ? xKeys[i].outTangent : 0;
                outTangents.y = m_HasYAxis ? yKeys[i].outTangent : 0;
                outTangents.z = m_HasZAxis ? zKeys[i].outTangent : 0;


                float inTangentsM = inTangents.magnitude;
                float outTangentsM = outTangents.magnitude;
                float tangentsRaito = inTangentsM / outTangentsM;
                bool hasParallelTangents = inTangents / inTangentsM == outTangents / outTangentsM;

                if (inTangents == outTangents && inTangents == Vector3.zero)
                {
                    hasParallelTangents = true;
                    tangentsRaito = 1;

                }




                inTangents = handlePosition + inTangents * leftTangentScale;
                outTangents = handlePosition - outTangents * rightTangentScale;

                ControlHandle handle = m_ControlHandles[i];
                TangentHandle tangentLeft = handle.leftTangent;
                TangentHandle tangentRight = handle.rightTangent;


                var xyzKeys = m_PositionCurveBinding.animationWindowKeyframes;
                if (xyzKeys != null)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var keys2 = xyzKeys[j];
                        if (i >= keys2.Count) continue;
                        handle.animationWindowKeyframes[j] = keys2[i];
                    }
                }

                handle.localPosition = handlePosition;
                tangentLeft.localPosition = inTangents;
                tangentRight.localPosition = outTangents;
                tangentLeft.scale = leftTangentScale;
                tangentRight.scale = rightTangentScale;
                tangentLeft.mode = AnimationUtility.GetKeyLeftTangentMode(curve, i);
                tangentRight.mode = AnimationUtility.GetKeyRightTangentMode(curve, i);
                tangentLeft.hide = i == 0;
                tangentRight.hide = i == keyCount - 1;
                handle.time = currentTime;
                handle.hide = !TimeRange.Contains(currentTime);
                handle.hasChanged = false;
                tangentLeft.hasChanged = false;
                tangentRight.hasChanged = false;
                handle.tangentsRaito = tangentsRaito;
                handle.hasParallelTangents = hasParallelTangents;
            }

            if (m_Loop)
            {
                var start = m_ControlHandles[0];
                var last = m_ControlHandles[keyCount - 1];

                start.hide = start.hide && last.hide;

                start.leftTangent.hide = false;
                last.hide = true;
                last.position = start.position;


                if (last.leftTangent.mode != start.leftTangent.mode)
                {
                    last.leftTangent.mode = start.leftTangent.mode;
                    SetTangantsMode(keyCount - 1, start.leftTangent.mode, true, true);
                    last.leftTangent.position = start.leftTangent.position;
                    start.leftTangent.hasChanged = true;
                    GUI.changed = true;
                }

                if (last.leftTangent.mode == TangentMode.Free)
                    last.leftTangent.position = start.leftTangent.position;

                if (last.rightTangent.mode == TangentMode.Free)
                    last.rightTangent.position = start.rightTangent.position;
            }
        }
        */

        public void ApplyChages()
        {
            if (isMissing) return;
            if (!hasCurveData) return;

            Event EVENT = Event.current;
            bool alt = EVENT.alt;
            bool restTangents = alt && HandleSelection.count < 2;


            int handleCount = m_ControlHandles.Count;

            if (m_Loop)
            {
                var start = m_ControlHandles[0];
                var end = m_ControlHandles[handleCount - 1];

                end.hasChanged = start.hasChanged;
                end.position = start.position;

                end.leftTangent.hasChanged = start.leftTangent.hasChanged;
                end.leftTangent.position = start.leftTangent.position;
                end.leftTangent.scale = start.leftTangent.scale;

                end.rightTangent.hasChanged = start.rightTangent.hasChanged;
                end.rightTangent.position = start.rightTangent.position;
                end.rightTangent.scale = start.rightTangent.scale;
            }

            for (int i = 0; i < handleCount; i++)
            {
                ControlHandle handle = m_ControlHandles[i];

                for (int axisIndex = 0; axisIndex < 3; axisIndex++)
                {
                    if (m_HasAxis[axisIndex])
                    {
                        int keyIndex = handle.keyframeIndexes[axisIndex];
                        Keyframe[] keys = m_Keyframes[axisIndex];

                        if (keyIndex != -1)
                        {
                            if (handle.hasChanged)
                            {
                                float local = handle.localPosition[axisIndex];
                                if (restTangents)
                                {
                                    float old = keys[keyIndex].value;
          
                                    if (handle.leftTangent.modes[axisIndex] == TangentMode.Free)
                                    {
                                        float inTangent = (local - old) / handle.leftTangent.scale;
                                        keys[keyIndex].inTangent = inTangent;
                                    }

                                    if (handle.rightTangent.modes[axisIndex] == TangentMode.Free)
                                    {
                                        float outTangent = (local - old) / handle.leftTangent.scale;
                                        keys[keyIndex].outTangent = outTangent;
                                    }

                                    continue;
                                }

                                keys[keyIndex].value = local;
                            }

                            if (handle.leftTangent.hasChanged)
                            {
                                float local = (handle.leftTangent.localPosition[axisIndex] - handle.localPosition[axisIndex]) / handle.leftTangent.scale;

                                keys[keyIndex].inTangent = local;

                                if (!alt && handle.hasParallelTangents)
                                {
                                    keys[keyIndex].outTangent = local / handle.tangentsRaito;
                                }
                            }

                            if (handle.rightTangent.hasChanged)
                            {
                                float local = (handle.localPosition[axisIndex] - handle.rightTangent.localPosition[axisIndex]) / handle.rightTangent.scale;

                                keys[keyIndex].outTangent = local;

                                if (!alt && handle.hasParallelTangents)
                                {
                                    float raito = 1 / handle.tangentsRaito;
                                    keys[keyIndex].inTangent = local / raito;
                                }
                            }
                        }
                    }
                }
            }

            UpdateKeysAndClip();


            if (m_AutoRotation) GenerateRotationKeys();
        }

        /*
        public void ApplyChages()
        {
            if (isMissing) return;

            if (!hasCurveData) return;

            Keyframe[] xKeys = m_HasXAxis ? m_XCurve.keys : null;
            Keyframe[] yKeys = m_HasYAxis ? m_YCurve.keys : null;
            Keyframe[] zKeys = m_HasZAxis ? m_ZCurve.keys : null;

            Event EVENT = Event.current;
            bool alt = EVENT.alt;
            bool restTangents = alt && HandleSelection.count < 2;


            int handleCount = m_ControlHandles.Count;

            if (m_Loop)
            {
                var start = m_ControlHandles[0];
                var end = m_ControlHandles[handleCount - 1];

                end.hasChanged = start.hasChanged;
                end.position = start.position;

                end.leftTangent.hasChanged = start.leftTangent.hasChanged;
                end.leftTangent.position = start.leftTangent.position;
                end.leftTangent.scale = start.leftTangent.scale;

                end.rightTangent.hasChanged = start.rightTangent.hasChanged;
                end.rightTangent.position = start.rightTangent.position;
                end.rightTangent.scale = start.rightTangent.scale;
            }

            for (int i = 0; i < handleCount; i++)
            {
                ControlHandle handle = m_ControlHandles[i];
                if (handle.hasChanged)
                {
                    Vector3 local = handle.localPosition;
                    if (restTangents)
                    {
                        Vector3 old = new Vector3();
                        if (m_HasXAxis) old.x = xKeys[i].value;
                        if (m_HasYAxis) old.y = yKeys[i].value;
                        if (m_HasZAxis) old.z = zKeys[i].value;

                        if (handle.leftTangent.mode == TangentMode.Free)
                        {
                            Vector3 inTangent = (local - old) / handle.leftTangent.scale;
                            if (m_HasXAxis) xKeys[i].inTangent = inTangent.x;
                            if (m_HasYAxis) yKeys[i].inTangent = inTangent.y;
                            if (m_HasZAxis) zKeys[i].inTangent = inTangent.z;
                        }

                        if (handle.rightTangent.mode == TangentMode.Free)
                        {
                            Vector3 outTangent = (local - old) / handle.leftTangent.scale;
                            if (m_HasXAxis) xKeys[i].outTangent = outTangent.x;
                            if (m_HasYAxis) yKeys[i].outTangent = outTangent.y;
                            if (m_HasZAxis) zKeys[i].outTangent = outTangent.z;
                        }

                        continue;
                    }

                    if (m_HasXAxis) xKeys[i].value = local.x;
                    if (m_HasYAxis) yKeys[i].value = local.y;
                    if (m_HasZAxis) zKeys[i].value = local.z;
                }

                if (handle.leftTangent.hasChanged)
                {
                    Vector3 local = handle.leftTangent.localPosition - handle.localPosition;
                    local /= handle.leftTangent.scale;


                    if (m_HasXAxis) xKeys[i].inTangent = local.x;
                    if (m_HasYAxis) yKeys[i].inTangent = local.y;
                    if (m_HasZAxis) zKeys[i].inTangent = local.z;


                    if (!alt && handle.hasParallelTangents)
                    {
                        if (m_HasXAxis) xKeys[i].outTangent = local.x / handle.tangentsRaito;
                        if (m_HasYAxis) yKeys[i].outTangent = local.y / handle.tangentsRaito;
                        if (m_HasZAxis) zKeys[i].outTangent = local.z / handle.tangentsRaito;
                    }
                }

                if (handle.rightTangent.hasChanged)
                {
                    Vector3 local = handle.localPosition - handle.rightTangent.localPosition;
                    local /= handle.rightTangent.scale;


                    if (m_HasXAxis) xKeys[i].outTangent = local.x;
                    if (m_HasYAxis) yKeys[i].outTangent = local.y;
                    if (m_HasZAxis) zKeys[i].outTangent = local.z;


                    if (!alt && handle.hasParallelTangents)
                    {
                        float raito = 1 / handle.tangentsRaito;
                        if (m_HasXAxis) xKeys[i].inTangent = local.x / raito;
                        if (m_HasYAxis) yKeys[i].inTangent = local.y / raito;
                        if (m_HasZAxis) zKeys[i].inTangent = local.z / raito;
                    }
                }
            }

            UpdateClip(xKeys, yKeys, zKeys);


            if (m_AutoRotation) GenerateRotationKeys();
        }
        */

        public void SyncSelection()
        {
            if (isMissing) return;

            AnimEditor.animationWindow.SyncSelection(m_ControlHandles);
        }



        void UpdateKeysAndClip()
        {
            for (int i = 0; i < 3; i++)
            {
                if (m_HasAxis[i])
                {
                    m_Curves[i].keys = m_Keyframes[i];
                }
            }

            RefreshTangents();
            UpdateClip();
        }

        void UpdateClip()
        {
            Undo.RecordObject(AnimEditor.animationClip, "Edit Curves");

            for (int i = 0; i < 3; i++)
            {
                if (m_HasAxis[i])
                {
                    AnimationUtility.SetEditorCurve(AnimEditor.animationClip, m_PositionCurveBinding.GetBinding(i), m_Curves[i]);
                }
            }
        }

        void RefreshTangents()
        {
            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                if (m_HasAxis[axisIndex])
                {
                    AnimationCurve curve = m_Curves[axisIndex];
                    int keyCount = m_Keyframes[axisIndex].Length;

                    for (int keyIndex = 0; keyIndex < keyCount; keyIndex++)
                    {
                        TangentMode leftTangentMode = AnimationUtility.GetKeyLeftTangentMode(curve, keyIndex);
                        TangentMode rightTangentMode = AnimationUtility.GetKeyRightTangentMode(curve, keyIndex);

                        bool broken = leftTangentMode != rightTangentMode || leftTangentMode != TangentMode.Auto && leftTangentMode != TangentMode.ClampedAuto;

                        AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, leftTangentMode);
                        AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, rightTangentMode);
                        AnimationUtility.SetKeyBroken(curve, keyIndex, broken);
                    }
                }
            }
        }

        void CalcVelocityRange()
        {
            if (Event.current.type != EventType.Repaint) return;

            int count = m_ControlHandles.Count;
            if (count < 2) return;

            int segmentCount = (int)Settings.pathAccuracy + 1;
            float minDis = float.PositiveInfinity;
            m_MinVelocity = float.PositiveInfinity;
            m_MaxVelocity = float.NegativeInfinity;

            for (int i = 1; i < count; i++)
            {
                ControlHandle startHandle = m_ControlHandles[i - 1];
                ControlHandle endHandle = m_ControlHandles[i];

                Vector3 start = startHandle.position;
                Vector3 end = endHandle.position;

                Vector3 startTangent = startHandle.rightTangent.position;
                Vector3 endTangent = endHandle.leftTangent.position;


                if (float.IsInfinity(startTangent.x) || float.IsInfinity(endTangent.x))
                {
                    continue;
                }
                else
                {
                    float deltaTime = startHandle.rightTangent.scale;
                    Vector3 a = start;
                    for (int j = 1; j <= segmentCount; j++)
                    {
                        float t = (float)j / segmentCount;
                        Vector3 b = BezierCurveRenderer.EvaluateBezierCurve(start, end, startTangent, endTangent, t);
                        float velocity = (b - a).magnitude / deltaTime;
                        m_MinVelocity = Mathf.Min(m_MinVelocity, velocity);
                        m_MaxVelocity = Mathf.Max(m_MaxVelocity, velocity);
                        float d = HandleUtility.DistanceToLine(a, b);
                        if (minDis > d) minDis = d;
                        a = b;
                    }
                }
            }
        }

        public Vector3 GetPositionAtTime(float time)
        {
            Vector3 p = new Vector3();

            for (int i = 0; i < 3; i++)
            {
                if (m_HasAxis[i])
                {
                    p[i] = m_Curves[i].Evaluate(time);
                }
            }

            return p;
        }

        public void StartMagnet()
        {
            if (isMissing) return;

            int handleCount = m_ControlHandles.Count;

            m_SelectedControlIndexList.Clear();

            for (int i = 0; i < handleCount; i++)
            {
                ControlHandle currentHandle = m_ControlHandles[i];
                ControlHandle nextHandle = m_ControlHandles[LoopUtility.Mod(i + 1, handleCount)];

                Vector3 currentPos = currentHandle.position;
                Vector3 currentOutTangentPos = currentHandle.rightTangent.position;
                Vector3 nextInTangentPos = nextHandle.leftTangent.position;
                Vector3 nextPos = nextHandle.position;

                currentHandle.length = i == handleCount - 1 ? 0 : BezierUtility.GetLength(currentPos, currentOutTangentPos, nextInTangentPos, nextPos);
                currentHandle.weight = 0;

                currentHandle.SavePosition();
                currentHandle.leftTangent.SavePosition();
                currentHandle.rightTangent.SavePosition();

                if (HandleSelection.Contains(currentHandle)) m_SelectedControlIndexList.Add(i);
            }

            foreach (var i in m_SelectedControlIndexList)
            {
                m_ControlHandles[i].weight = 1;

                float dis = 0;
                int follow = i;
                while (true)
                {
                    if (follow == 0 && !m_Loop) break;
                    follow = LoopUtility.Mod(follow - 1, handleCount);
                    if (m_SelectedControlIndexList.Contains(follow)) break;
                    dis += m_ControlHandles[follow].length;
                    m_ControlHandles[follow].weight = Mathf.Max(m_ControlHandles[follow].weight, Mathf.Sqrt(Mathf.InverseLerp(Magnet.radius, 0, dis)));
                }
                dis = 0;
                follow = i;
                while (true)
                {
                    if (follow == handleCount - 1 && !m_Loop) break;
                    dis += m_ControlHandles[follow].length;
                    follow = LoopUtility.Mod(follow + 1, handleCount);
                    if (m_SelectedControlIndexList.Contains(follow)) break;
                    m_ControlHandles[follow].weight = Mathf.Max(m_ControlHandles[follow].weight, Mathf.Sqrt(Mathf.InverseLerp(Magnet.radius, 0, dis)));
                }
            }
        }

        public void UpdateMagnet()
        {
            if (isMissing) return;

            if (m_SelectedControlIndexList == null) return;
            if (m_SelectedControlIndexList.Count == 0) return;
            if (HandleSelection.count == 0) return;


            if (GUIUtility.hotControl == 0)
            {
                m_SelectedControlIndexList.Clear();
                return;
            }

            if (Event.current.alt) return;
            if (!GUI.changed) return;


            var firstHandle = m_ControlHandles[m_SelectedControlIndexList[0]];
            Vector3 prev = firstHandle.savedPosition;
            Vector3 current = firstHandle.position;
            Vector3 delta = current - prev;

            int handleCount = m_ControlHandles.Count;

            if (Magnet.controls)
            {
                for (int i = 0; i < handleCount; i++)
                {
                    ControlHandle handle = m_ControlHandles[i];
                    handle.position = handle.savedPosition + delta * handle.weight;
                    handle.hasChanged = true;
                }
            }

            if (Magnet.tangents)
            {
                for (int i = 0; i < handleCount; i++)
                {
                    ControlHandle prevHandle = m_ControlHandles[LoopUtility.Mod(i - 1, handleCount)];
                    ControlHandle currentHandle = m_ControlHandles[i];
                    ControlHandle nextHandle = m_ControlHandles[LoopUtility.Mod(i + 1, handleCount)];

                    if (m_Loop)
                    {
                        if (i == handleCount - 1) continue;
                        if (i == handleCount - 2) nextHandle = m_ControlHandles[0];
                        if (i == 0) prevHandle = m_ControlHandles[handleCount - 2];
                    }

                    currentHandle.leftTangent.hasChanged = true;
                    currentHandle.rightTangent.hasChanged = true;

                    Vector3 prevHandleCurrentPos = prevHandle.position;
                    Vector3 currentHandleCurrentPos = currentHandle.position;
                    Vector3 nextHandleCurrentPos = nextHandle.position;

                    Vector3 prevHandlePrevPos = prevHandle.savedPosition;
                    Vector3 currentHandlePrevPos = currentHandle.savedPosition;
                    Vector3 nextHandlePrevPos = nextHandle.savedPosition;


                    Vector3 prevAB = (currentHandlePrevPos - prevHandlePrevPos).normalized;
                    Vector3 prevBC = (nextHandlePrevPos - currentHandlePrevPos).normalized;

                    Vector3 currentAB = (currentHandleCurrentPos - prevHandleCurrentPos).normalized;
                    Vector3 currentBC = (nextHandleCurrentPos - currentHandleCurrentPos).normalized;

                    if (!m_Loop)
                    {
                        if (i == 0)
                        {
                            prevAB = prevBC;
                            currentAB = currentBC;
                        }

                        if (i == handleCount - 1)
                        {
                            prevBC = prevAB;
                            currentBC = currentAB;
                        }
                    }

                    prevBC = Vector3.Slerp(prevAB, prevBC, 0.5f);
                    currentBC = Vector3.Slerp(currentAB, currentBC, 0.5f);

                    Quaternion q = Quaternion.FromToRotation(prevBC, currentBC);
                    Vector3 lt = currentHandle.leftTangent.savedPosition;
                    Vector3 rt = currentHandle.rightTangent.savedPosition;
                    lt -= currentHandlePrevPos;
                    rt -= currentHandlePrevPos;
                    lt = q * lt;
                    rt = q * rt;
                    lt += currentHandleCurrentPos;
                    rt += currentHandleCurrentPos;
                    currentHandle.leftTangent.position = lt;
                    currentHandle.rightTangent.position = rt;
                }
            }
        }

        #region Cache World Path
        public void StartCachingWorldFramePositions()
        {
            if (isMissing) return;

            m_Frames.Clear();
            m_FirstVisibleFrameIndex = 0;
        }

        public void StartCachingWorldPath(int frameCount, float time2Frame, HashSet<int> hotFrames)
        {
            if (isMissing) return;

            m_Path.Clear();

            if (!hasCurveData) return;

            var keys = m_ControlHandles;
            int keyCount = keys.Count;

            //timeChanged = true;
            for (int i = 0; i < keyCount; i++)
            {
                float t = keys[i].time;
                int f = (int)(t * time2Frame);
                hotFrames.Add(f);
            }
        }

        public void CacheWorldPosition(int frameIndex, float frame2Time, int frameCount)
        {
            if (isMissing) return;

            Vector3 po = m_Transform.position;
            int pCount = m_Path.Count;
            if (pCount == 0 || frameIndex == frameCount)
            {
                m_Path.Add(po);
            }
            else
            {
                Vector3 prev = m_Path[m_Path.Count - 1];
                if ((prev - po).sqrMagnitude > 0.0001f)
                {
                    m_Path.Add(po);
                }
            }

            if (m_ControlHandleSortedDictionary.TryGetValue(frame2Time * frameIndex, out ControlHandle controlHandle))
            {
                controlHandle.SetMatrix(localToWorldMatrix);
            }
        }

        public void CacheWorldFramePosition()
        {
            if (isMissing) return;

            Vector3 po = m_Transform.position;
            m_Frames.Add(po);
        }
        #endregion

        #region Draw Path/Curve
        public void DrawWorldPath()
        {
            if (isMissing) return;

            Handles.color = Settings.pathColor;
            Handles.DrawAAPolyLine(4, m_Path.ToArray());
        }

        public void CacheLocalFramePositions()
        {
            if (isMissing) return;

            if (Event.current.type != EventType.Repaint) return;

            m_Frames.Clear();

            if (!hasCurveData) return;
            int count = m_ControlHandles.Count;
            if (count < 2) return;
            float curveAccuracy = Settings.pathAccuracy;

            List<int> visibleHandleIndexes = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (TimeRange.Contains(m_ControlHandles[i].time)) visibleHandleIndexes.Add(i);
            }

            if (visibleHandleIndexes.Count < 2) return;

            float frameRate = AnimEditor.animationClip.frameRate;
            float frameToTime = 1 / frameRate;


            int firstVisibleHandleIndex = visibleHandleIndexes[0];
            int lastVisibleHandleIndex = visibleHandleIndexes[visibleHandleIndexes.Count - 1];
            float firstVisibleTime = m_ControlHandles[firstVisibleHandleIndex].time;
            float lastVisibleTime = m_ControlHandles[lastVisibleHandleIndex].time;
            int firstVisibleFrame = Mathf.RoundToInt(firstVisibleTime * frameRate);
            int lastVisibleFrame = Mathf.RoundToInt(lastVisibleTime * frameRate);

            float time = firstVisibleTime;
            int frame = firstVisibleFrame;

            for (int i = firstVisibleHandleIndex; i < lastVisibleHandleIndex; i++)
            {
                ControlHandle startHandle = m_ControlHandles[i];
                ControlHandle endHandle = m_ControlHandles[i + 1];

                Vector3 start = startHandle.position;
                Vector3 end = endHandle.position;

                Vector3 startTangent = startHandle.rightTangent.position;
                Vector3 endTangent = endHandle.leftTangent.position;

                if (float.IsInfinity(startTangent.x) || float.IsInfinity(endTangent.x))
                {
                    Handles.color = Settings.pathColor;
                    Handles.DrawDottedLine(start, end, 3f);
                }
                else
                {

                }

                float startTime = startHandle.time;
                float endTime = endHandle.time;

                while (time >= startTime && time <= endTime)
                {
                    float t = Mathf.InverseLerp(startTime, endTime, time);
                    m_Frames.Add(BezierUtility.EvaluateBezierCurve(start, end, startTangent, endTangent, t));
                    time += frameToTime;
                    frame++;
                }
            }

            m_FirstVisibleFrameIndex = firstVisibleFrame;
        }


        int m_FirstVisibleFrameIndex;


        public void DrawTimeTicks()
        {
            if (isMissing) return;

            if (Event.current.type != EventType.Repaint) return;
            if (!hasCurveData) return;
            if (m_Frames.Count == 0) return;

            Quaternion q = Quaternion.identity;

            float frameRate = AnimEditor.animationClip.frameRate;
            float frameToTime = 1 / frameRate;

            int maxTickLevel = AnimEditor.animationWindow.maxTickLevel;
            int tickFrameRate0 = AnimEditor.animationWindow.GetTickFrameRate(maxTickLevel);
            int tickFrameRate1 = AnimEditor.animationWindow.GetTickFrameRate(maxTickLevel - 1);
            int tickFrameRate2 = AnimEditor.animationWindow.GetTickFrameRate(maxTickLevel - 2);
            int tickFrameRate3 = AnimEditor.animationWindow.GetTickFrameRate(maxTickLevel - 3);

            int frameCount = m_Frames.Count;

            Color color = Settings.timeTicksColor;
            float alpha = color.a;
            float alphaMul = 1;

            for (int i = 0; i < frameCount; i++)
            {
                int frame = m_FirstVisibleFrameIndex + i;
                float time = frame * frameToTime;

                if (!TimeRange.Contains(time)) continue;

                float size;
                if (frame % tickFrameRate0 == 0)
                {
                    alphaMul = 1;
                    size = 0.075f;
                }
                else if (frame % tickFrameRate1 == 0)
                {
                    alphaMul = 0.8f;
                    size = 0.05f;
                }
                else if (frame % tickFrameRate2 == 0)
                {
                    alphaMul = 0.4f;
                    size = 0.05f;
                }
                else if (frame % tickFrameRate3 == 0)
                {
                    alphaMul = 0.2f;
                    size = 0.05f;
                }
                else
                {
                    continue;
                }

                Vector3 position = m_Frames[i];
                size *= HandleUtility.GetHandleSize(position);
                color.a = alpha * alphaMul;
                Handles.color = color;
                Handles.SphereHandleCap(-100, position, q, size, EventType.Repaint);
            }
        }

        static GUIStyle s_TimeLabelStyle = new GUIStyle(GUI.skin.label);

        public void DrawTimeLabels()
        {
            if (isMissing) return;

            if (Event.current.type != EventType.Repaint) return;
            if (m_Frames.Count == 0) return;
            if (!hasCurveData) return;

            s_TimeLabelStyle.normal.textColor = Settings.timeLabelsColor;
            s_TimeLabelStyle.fontStyle = FontStyle.Bold;

            float frameRate = AnimEditor.animationClip.frameRate;
            float frameToTime = 1 / frameRate;

            int maxTickLevel = AnimEditor.animationWindow.maxTickLevel;
            int tickFrameRate = AnimEditor.animationWindow.GetTickFrameRate(maxTickLevel);

            int frameCount = m_Frames.Count;
            for (int i = 0; i < frameCount; i++)
            {
                int frame = m_FirstVisibleFrameIndex + i;
                if (frame % tickFrameRate == 0)
                {
                    float time = frame * frameToTime;

                    if (!TimeRange.Contains(time)) continue;

                    string label = AnimEditor.animationWindow.FormatTickTime(time);
                    Handles.Label(m_Frames[i], label, s_TimeLabelStyle);
                }
            }
        }


        public void DrawCurves()
        {
            if (isMissing) return;

            if (!hasCurveData) return;
            if (Event.current.type != EventType.Repaint) return;

            int count = m_ControlHandles.Count;
            if (count < 2) return;

            CalcVelocityRange();

            ColorMode curveDrawMode = Settings.pathColorMode;
            float curveAccuracy = Settings.pathAccuracy;

            for (int i = 1; i < count; i++)
            {
                ControlHandle startHandle = m_ControlHandles[i - 1];
                ControlHandle endHandle = m_ControlHandles[i];


                if (startHandle.hide || (endHandle.hide && i != count - 1 || !TimeRange.Contains(endHandle.time))) continue;


                Vector3 start = startHandle.position;
                Vector3 end = endHandle.position;

                Vector3 startTangent = startHandle.rightTangent.position;
                Vector3 endTangent = endHandle.leftTangent.position;

                if (float.IsInfinity(startTangent.x) || float.IsInfinity(endTangent.x))
                {
                    Handles.color = Settings.pathColor;
                    Handles.DrawDottedLine(start, end, 3f);
                }
                else
                {
                    if (curveDrawMode == ColorMode.Gradient)
                    {
                        float deltaTime = startHandle.rightTangent.scale;
                        BezierCurveRenderer.Begin();
                        if (m_SelectedControlIndexList != null && m_SelectedControlIndexList.Count > 0 && HandleSelection.count > 0)
                        {
                            BezierCurveRenderer.Draw(start, end, startTangent, endTangent, startHandle.weight, endHandle.weight, (int)curveAccuracy + 1);
                        }
                        else
                        {
                            BezierCurveRenderer.Draw(start, end, startTangent, endTangent, deltaTime, m_MinVelocity, m_MaxVelocity, (int)curveAccuracy + 1);
                        }
                        BezierCurveRenderer.End();
                    }
                    else
                    {
                        Handles.DrawBezier(start, end, startTangent, endTangent, Settings.pathColor, null, 5);
                    }
                }
            }
        }
        #endregion

        #region Editor Handles
        public void DrawHandlesCap()
        {
            if (isMissing) return;

            foreach (var handle in m_ControlHandles)
            {
                handle.DrawHandlesCap();
            }
        }

        public void DrawSelectionButtons()
        {
            if (isMissing) return;

            Event EVENT = Event.current;
            bool mouseRightClick = EVENT.isMouse && EVENT.type == EventType.MouseDown && EVENT.button == 1;


            foreach (var handle in m_ControlHandles)
            {
                if (handle.DrawSelectableButtons())
                {
                    if (mouseRightClick) OpenHandleMenu(m_ControlHandles.IndexOf(handle));
                }
            }
        }

        public void EditCurves2D()
        {
            if (isMissing) return;

            if (Event.current.shift)
            {
                DrawSelectionButtons();
            }
            else
            {
                MouseRecords.Record();

                int handleCount = m_ControlHandles.Count;
                for (int i = 0; i < handleCount; i++)
                {
                    if (m_ControlHandles[i].DoFreeMoveHandles())
                    {
                        if (MouseRecords.RightClick) OpenHandleMenu(i);
                    }
                }
            }
        }

        public void EditCurves3D()
        {
            if (isMissing) return;

            Event EVENT = Event.current;
            bool control = EVENT.control;
            bool mouseClickRight = EVENT.isMouse && EVENT.type == EventType.MouseDown && EVENT.button == 1;
            bool mouseClickLeft = EVENT.isMouse && EVENT.type == EventType.MouseDown && EVENT.button == 0;
            int handleCount = m_ControlHandles.Count;

            DrawSelectionButtons();

            if (HandleSelection.count == 1)
            {
                HandleSelection.activeHandle.DoPositionHandle();
            }
        }

        public void CheckBoxSelection()
        {
            if (isMissing) return;

            foreach (var handle in m_ControlHandles)
            {
                handle.CheckBoxSelection();
            }
        }
        #endregion

        #region Handle Menu
        void OpenHandleMenu(int handleIndex)
        {
            GenericMenu menu = new GenericMenu();

            ControlHandle controlHandle = m_ControlHandles[handleIndex];

            TangentHandle leftTangent = controlHandle.leftTangent;
            TangentHandle rightTangent = controlHandle.rightTangent;

            bool isLClampedAuto = false;
            bool isLAuto = false;
            bool isLFree = false;
            bool isLLinear = false;
            bool isLConstant = false;

            bool isRClampedAuto = false;
            bool isRAuto = false;
            bool isRFree = false;
            bool isRLinear = false;
            bool isRConstant = false;

            TangentMode leftTangentMode = leftTangent.mode;
            TangentMode rightTangentMode = rightTangent.mode;

            if (leftTangent.hasSimilarModes)
            {
                isLClampedAuto = leftTangentMode == TangentMode.ClampedAuto;
                isLAuto = leftTangentMode == TangentMode.Auto;
                isLFree = leftTangentMode == TangentMode.Free;
                isLLinear = leftTangentMode == TangentMode.Linear;
                isLConstant = leftTangentMode == TangentMode.Constant;
            }

            if (rightTangent.hasSimilarModes)
            {
                isRClampedAuto = rightTangentMode == TangentMode.ClampedAuto;
                isRAuto = rightTangentMode == TangentMode.Auto;
                isRFree = rightTangentMode == TangentMode.Free;
                isRLinear = rightTangentMode == TangentMode.Linear;
                isRConstant = rightTangentMode == TangentMode.Constant;
            }

            bool areLRClampedAuto = isLClampedAuto && isRClampedAuto;
            bool areLRAuto = isLAuto && isRAuto;
            bool areLRFree = isLFree && isRFree;
            bool areLRLinear = isLLinear && isRLinear;
            bool areLRConstant = isLConstant && isRConstant;

            bool broken = leftTangentMode != rightTangentMode || leftTangentMode != TangentMode.Auto && leftTangentMode != TangentMode.ClampedAuto;

            menu.AddItem(new GUIContent("Delete"), false, () => DeleteHandle(handleIndex));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Clamped Auto"), areLRClampedAuto, () => SetTangantModes(handleIndex, TangentMode.ClampedAuto));
            menu.AddItem(new GUIContent("Auto"), areLRAuto, () => SetTangantModes(handleIndex, TangentMode.Auto));
            menu.AddItem(new GUIContent("Broken"), broken, () => SetTangantModes(handleIndex, TangentMode.Free));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Left Tangent/Free"), isLFree, () => SetLeftTangantMode(handleIndex, TangentMode.Free));
            menu.AddItem(new GUIContent("Left Tangent/Liner"), isLLinear, () => SetLeftTangantMode(handleIndex, TangentMode.Linear));
            menu.AddItem(new GUIContent("Left Tangent/Constant"), isLConstant, () => SetLeftTangantMode(handleIndex, TangentMode.Constant));

            menu.AddItem(new GUIContent("Right Tangent/Free"), isRFree, () => SetRightTangantMode(handleIndex, TangentMode.Free));
            menu.AddItem(new GUIContent("Right Tangent/Liner"), isRLinear, () => SetRightTangantMode(handleIndex, TangentMode.Linear));
            menu.AddItem(new GUIContent("Right Tangent/Constant"), isRConstant, () => SetRightTangantMode(handleIndex, TangentMode.Constant));

            menu.AddItem(new GUIContent("Both Tangents/Free"), areLRFree, () => SetTangantModes(handleIndex, TangentMode.Free));
            menu.AddItem(new GUIContent("Both Tangents/Liner"), areLRLinear, () => SetTangantModes(handleIndex, TangentMode.Linear));
            menu.AddItem(new GUIContent("Both Tangents/Constant"), areLRConstant, () => SetTangantModes(handleIndex, TangentMode.Constant));

            menu.ShowAsContext();
        }

        void DeleteHandle(int handleIndex)
        {
            ControlHandle handle = m_ControlHandles[handleIndex];

            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                int keyIndex = handle.keyframeIndexes[axisIndex];
                if (keyIndex != -1)
                {
                    m_Curves[axisIndex].RemoveKey(keyIndex);
                }
            }

            UpdateClip();
        }

        void SetTangantModes(int handleIndex, TangentMode tangentMode)
        {
            ControlHandle handle = m_ControlHandles[handleIndex];

            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                int keyIndex = handle.keyframeIndexes[axisIndex];
                if (keyIndex != -1)
                {
                    AnimationCurve curve = m_Curves[axisIndex];
                    AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, tangentMode);
                    AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, tangentMode);
                }
            }

            UpdateClip();
        }

        void SetTangantModes(int handleIndex, TangentMode[] tangentModes)
        {
            ControlHandle handle = m_ControlHandles[handleIndex];

            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                int keyIndex = handle.keyframeIndexes[axisIndex];
                if (keyIndex != -1)
                {
                    AnimationCurve curve = m_Curves[axisIndex];
                    AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, tangentModes[axisIndex]);
                    AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, tangentModes[axisIndex]);
                }
            }

            UpdateClip();
        }


        void SetLeftTangantMode(int handleIndex, TangentMode tangentMode)
        {
            ControlHandle handle = m_ControlHandles[handleIndex];

            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                int keyIndex = handle.keyframeIndexes[axisIndex];
                if (keyIndex != -1)
                {
                    AnimationCurve curve = m_Curves[axisIndex];

                    AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, tangentMode);

                    TangentMode rightTangentMode = AnimationUtility.GetKeyRightTangentMode(curve, keyIndex);

                    if (rightTangentMode == TangentMode.Auto || rightTangentMode == TangentMode.ClampedAuto)
                    {
                        AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, TangentMode.Free);
                    }
                }
            }

            UpdateClip();
        }

        void SetRightTangantMode(int handleIndex, TangentMode tangentMode)
        {
            ControlHandle handle = m_ControlHandles[handleIndex];

            for (int axisIndex = 0; axisIndex < 3; axisIndex++)
            {
                int keyIndex = handle.keyframeIndexes[axisIndex];
                if (keyIndex != -1)
                {
                    AnimationCurve curve = m_Curves[axisIndex];

                    AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, tangentMode);

                    TangentMode rightTangentMode = AnimationUtility.GetKeyLeftTangentMode(curve, keyIndex);

                    if (rightTangentMode == TangentMode.Auto || rightTangentMode == TangentMode.ClampedAuto)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, TangentMode.Free);
                    }
                }
            }

            UpdateClip();
        }
        #endregion
        #endregion
    }
}