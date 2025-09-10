using System;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;
using UDebug = UnityEngine.Debug;

namespace Code.Core.BaseDMDisposable.Scripts
{
    public class DDebug
    {
        private readonly DDebug _parent;
        private readonly string _name;
        private readonly string _fullName;

        public DDebug(string name, DDebug parent = null)
        {
            _name = name;
            _parent = parent;
            _fullName = $"{FullName}: ";
        }

        public bool Mute { get; set; }

        public bool ForceLog { private get; set; }

        private bool IsMute
            => (Mute || _parent is { IsMute: true }) && !IsForceLogged;

        private bool IsForceLogged
            => ForceLog || _parent is { ForceLog: true };

        private string FullName
            => _parent == null ? _name : $"{_parent.FullName}{'.'}{_name}";

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void Assert(bool condition)
        {
            UDebug.Assert(condition);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0, bool depthTest = true)
        {
            UDebug.DrawLine(start, end, color, duration, depthTest);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void DrawLine(Vector3 start, Vector3 end)
        {
            UDebug.DrawLine(start, end);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration = 0, bool depthTest = true)
        {
            UDebug.DrawRay(start, dir, color, duration, depthTest);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void DrawRay(Vector3 start, Vector3 dir)
        {
            UDebug.DrawRay(start, dir);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void Log(object message)
        {
            UDebug.Log(message);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void Log(object message, Object obj)
        {
            UDebug.Log(message, obj);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogFormat(string format, params object[] args)
        {
            UDebug.LogFormat(format, args);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogAssertion(object message)
        {
            UDebug.LogAssertion(message);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogWarning(object message)
        {
            UDebug.LogWarning(message);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogWarning(object message, Object obj)
        {
            UDebug.LogWarning(message, obj);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            UDebug.LogWarningFormat(format, args);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogError(object message)
        {
            UDebug.LogError(message);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogError(object message, Object obj)
        {
            UDebug.LogError(message, obj);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            UDebug.LogErrorFormat(format, args);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogException(Exception exception)
        {
            UDebug.LogException(exception);
        }

        public static void Break()
        {
            UDebug.Break();
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public void Info(string msg)
        {
            if (!IsMute)
                UDebug.Log(_fullName + msg);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public void Info(object msg)
        {
            if (!IsMute)
                UDebug.Log($"{_fullName} {msg}");
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public void Warn(string msg)
        {
            if (!IsMute)
                UDebug.LogWarning(_fullName + msg);
        }

        // [Conditional("DEBUG_ENABLE_LOG")]
        public void Err(string msg)
        {
            UDebug.LogError($"LogError of {_fullName}{msg}");
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public void Ex(Exception ex)
        {
            UDebug.LogError(_fullName + ex);
        }
    }
}