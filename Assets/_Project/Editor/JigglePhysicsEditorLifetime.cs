using System;
using System.Reflection;
using UnityEditor;

namespace Bellerophon.Editor
{
    /// <summary>Release JigglePhysics' global buffers even when no JiggleUpdateExample exists in the scene.</summary>
    [InitializeOnLoad]
    internal static class JigglePhysicsEditorLifetime
    {
        static JigglePhysicsEditorLifetime()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Also covers projects with domain reload disabled; scene teardown has finished here.
            if (state == PlayModeStateChange.EnteredEditMode) Release();
        }

        private static void Release()
        {
            // Keep the optional package dependency consistent with existing project integration.
            var type = Type.GetType("GatorDragonGames.JigglePhysics.JigglePhysics, com.gator-dragon-games.jigglephysics");
            if (type == null) return;
            var jobs = type.GetField("jobs", BindingFlags.NonPublic | BindingFlags.Static);
            if (jobs == null) throw new MissingFieldException(type.FullName, "jobs");
            if (jobs.GetValue(null) == null) return;
            // The package's owner completes its jobs and disposes all native buffers together.
            var dispose = type.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Static);
            if (dispose == null) throw new MissingMethodException(type.FullName, "Dispose");
            dispose.Invoke(null, null);
        }
    }
}
