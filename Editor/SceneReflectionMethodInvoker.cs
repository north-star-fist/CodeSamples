using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

using UnityEditor;

using UnityEngine;
using System.Collections.Generic;

namespace Sergey.Safonov.Utility
{

    /**
     * <summary>Invokes a void method with no parameters of arbitrary class-component that is on scene (on each gameobject)</summary>
     */
    public class SceneReflectionMethodInvoker : ScriptableWizard {

        public string assemblyName;
        public string className;
        public string methodName;

        [Tooltip("Use with UnityEngine.Object descendants only")]
        public bool includeInactive;

        public bool selectedOnly = true;
        [Tooltip("Works only if selectedOnly checked")]
        public bool topLevelOnly = true;

        [Tooltip("Uncheck if you deal with Unity stuff (gameobjects, monobehaviours)")]
        public bool multiThreaded;

        static readonly object[] EMPTY_OBJECTS_ARRAY = new object[0];

        static string assemblyNameCache = "Assembly-CSharp";
        static string classNameCache;
        static string methodNameCache;
        static bool includeInactiveCache;
        static bool selectedOnlyCache = true;
        static bool topLevelOnlyCache = true;
        static bool multiThreadedCache;

        SceneReflectionMethodInvoker() {
            assemblyName = assemblyNameCache;
            className = classNameCache;
            methodName = methodNameCache;
            includeInactive = includeInactiveCache;
            selectedOnly = selectedOnlyCache;
            topLevelOnly = topLevelOnlyCache;
            multiThreaded = multiThreadedCache;
    }

        [MenuItem("Tools/Sergey.Safonov/Invoke Method...")]
        static void CreateWizard() {
            ScriptableWizard.DisplayWizard(
                "Reflection Invocation", typeof(SceneReflectionMethodInvoker), "Invoke!");
        }

        private void OnWizardUpdate() {
            helpString = "Input class name of the component that you want to use. And input name of the method to invoke. " +
                "Fill in assembly name if the class is not system. It is 'Assembly-CSharp' by default";
            assemblyNameCache = assemblyName;
            classNameCache = className;
            methodNameCache = methodName;
            includeInactiveCache = includeInactive;
            selectedOnlyCache = selectedOnly;
            topLevelOnlyCache = topLevelOnly;
            multiThreadedCache = multiThreaded;
        }

        void OnWizardCreate() {
            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(methodName)) {
                Debug.LogWarning("Please specify class name and method name!");
                return;
            }
            string assemblyQualifiedClassName = string.IsNullOrWhiteSpace(assemblyName) ? className : className + ',' + assemblyName;
            Type type = Type.GetType(assemblyQualifiedClassName);
            if (type == null) {
                Debug.LogWarningFormat("Type {0} was not found! Check the className!", assemblyQualifiedClassName);
                return;
            }
            MethodInfo mInfo = type.GetMethod(methodName, new Type[] { });
            if (mInfo == null) {
                Debug.LogWarningFormat("Type {0} was not found! Check the methodName!", methodName);
                return;
            }

            IEnumerable<UnityEngine.Object> components;
            if (selectedOnly) {
                components = Selection.GetFiltered(type, topLevelOnly ? SelectionMode.TopLevel : SelectionMode.Deep);
                if (!includeInactive) {
                    components = components.Where((c) => {
                        Behaviour b = c as Behaviour;
                        if (b != null) {
                            return b.isActiveAndEnabled;
                        }
                        return false;
                    });
                }
            } else {
                components = FindObjectsOfType(type, includeInactive).AsEnumerable();
            }
            
            if (components == null) {
                Debug.LogWarningFormat("Components array is null!");
                return;
            }
            if (components.Count() == 0) {
                Debug.LogWarningFormat("Components array is empty!");
                return;
            }

            if (multiThreaded) {
                invokeCodeInParallel(components, mInfo);
            } else {
                foreach (UnityEngine.Object c in components) {
                    mInfo.Invoke(c, EMPTY_OBJECTS_ARRAY);
                }
            }
            Debug.LogFormat("Executed {0} methods!", components.Count());
        }

        static void invokeCodeInParallel(IEnumerable<UnityEngine.Object> components, MethodInfo mInfo) {
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = Mathf.Max(1, System.Environment.ProcessorCount - 1);
            Parallel.ForEach(components, parallelOptions, (mb) => { mInfo.Invoke(mb, EMPTY_OBJECTS_ARRAY); });
        }
    }
}