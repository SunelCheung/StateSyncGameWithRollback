using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class XUnityEx
{
    [MenuItem("XGame/快速重开 %#Q",false,1)]
    private static void QuickRestart()
    {
        EditorApplication.isPlaying = false;
        EditorApplication.update += Start;
    }

    private static int PassedFrame;
    private static void Start()
    {
        if (PassedFrame++ > 0)
        {
            EditorApplication.update -= Start;
            AssetDatabase.Refresh();
            PassedFrame = 0;
            EditorApplication.isPlaying = true;
        }
    }
    private static readonly List<Transform> TempList = new();
    private static List<string> PathCache = new();
    private static StringBuilder StringBuilderCache = new();

    public static float Time => UnityEngine.Time.unscaledTime * UnityEngine.Time.timeScale;

    public static float DeltaTime => UnityEngine.Time.unscaledDeltaTime * UnityEngine.Time.timeScale;

    public static Object LoadFirstAsset(this AssetBundle assetBundle) => assetBundle.LoadAsset(assetBundle.GetAllAssetNames()[0]);

    public static AssetBundleRequest LoadFirstAssetAsync(this AssetBundle assetBundle) => assetBundle.LoadAssetAsync(assetBundle.GetAllAssetNames()[0]);

    public static bool Exist(this Object obj) => obj;

    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (!component)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }

    public static void DestroyChildren(this GameObject go) => go.transform.DestroyChildren();

    public static void DestroyChildren(this Transform tf)
    {
        bool isPlaying = Application.isPlaying;
        while (tf.childCount != 0)
        {
            Transform child = tf.GetChild(0);
            if (isPlaying)
            {
                child.SetParent(null);
                Object.Destroy(child.gameObject);
            }
            else
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    public static void Reset(this Transform tf)
    {
        tf.localPosition = Vector3.zero;
        tf.localScale = Vector3.one;
        tf.localEulerAngles = Vector3.zero;
    }

    public static GameObject FindGameObject(this GameObject go, string name) => !go ? null : go.transform.FindGameObject(name);

    public static GameObject FindGameObject(this Transform tf, string name)
    {
        Transform targetTransform = tf.FindTransform(name);
        return targetTransform ? targetTransform.gameObject : null;
    }

    public static Transform FindTransform(this GameObject go, string name) => !go ? null : go.transform.FindTransform(name);

    public static Transform FindTransform(this Transform tf, string name)
    {
        if (!tf || string.IsNullOrEmpty(name))
        {
            return null;
        }

        TempList.Clear();
        TempList.Add(tf);
        int index = 0;
        while (TempList.Count > index)
        {
            Transform transform = TempList[index++];
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform childTransform = transform.GetChild(i);
                if (childTransform.name == name)
                {
                    return childTransform;
                }

                TempList.Add(childTransform);
            }
        }

        return null;
    }

    public static GameObject FindGameObjectWithSplit(this GameObject go, string name) => !go ? null : go.transform.FindGameObjectWithSplit(name);

    public static GameObject FindGameObjectWithSplit(this Transform tf, string name)
    {
        Transform tempTransform = tf.FindTransformWithSplit(name);
        return tempTransform ? tempTransform.gameObject : null;
    }

    public static Transform FindTransformWithSplit(this GameObject go, string name) => !go ? null : go.transform.FindTransformWithSplit(name);

    public static Transform FindTransformWithSplit(this Transform tf, string name)
    {
        if (!tf || string.IsNullOrEmpty(name))
        {
            return null;
        }

        string[] names = name.Split('/');
        if (names.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            tf = tf.FindTransform(names[i]);

            if (!tf)
            {
                return null;
            }
        }

        return tf;
    }

    public static T FindComponent<T>(this GameObject go, string name)
        where T : Component
    {
        if (go == null)
        {
            return null;
        }

        return go.transform.FindComponent<T>(name);
    }

    public static T FindComponent<T>(this Transform tf, string name)
        where T : Component
    {
        if (tf == null)
        {
            return null;
        }

        Transform target = tf.FindTransform(name);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<T>();
    }

    public static void SetLayerRecursively(this GameObject go, int layer)
    {
        if (go)
        {
            go.transform.SetLayerRecursively(layer);
        }
    }

    public static void SetLayerRecursively(this Transform tf, int layer, bool force = true)
    {
        if (tf == null)
        {
            return;
        }

        TempList.Clear();
        TempList.Add(tf);
        int index = 0;
        while (TempList.Count > index)
        {
            Transform transform = TempList[index++];
            if (force || transform.gameObject.layer == 0)
            {
                transform.gameObject.layer = layer;
            }
            for (int i = 0; i < transform.childCount; ++i)
            {
                TempList.Add(transform.GetChild(i));
            }
        }
    }

    public static bool SetActiveEx(this GameObject gameObject, bool value)
    {
        if (gameObject.activeSelf == value)
        {
            return false;
        }

        gameObject.SetActive(value);
        return true;
    }

    private class WaitSetInfo
    {
        public readonly string Name;
        public readonly Action CallBack;

        public WaitSetInfo(string name, Action callback)
        {
            Name = name;
            CallBack = callback;
        }
    }

    public static void ResetType(this Image image)
    {
        image.fillAmount = 1.0f;
        image.type = Image.Type.Simple;
    }
    
    public static void SetKeyword(this Material material, string keyword, bool value)
    {
        if (value)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    public static bool RayCast(this Ray ray, out RaycastHit hit, int layerMask, float maxDistance = float.PositiveInfinity) => Physics.Raycast(ray, out hit, maxDistance, layerMask);

    public static Transform PhysicsRayCast(this Transform transform, Vector3 offset, Vector3 direction, int layerMask, float maxDistance = float.PositiveInfinity)
    {
        if (Physics.Raycast(transform.position + offset, direction, out RaycastHit hit, maxDistance, layerMask))
            return hit.transform;

        return null;
    }

    public static Transform PhysicsRayCastDown(this Transform transform, int layerMask, float maxDistance = float.PositiveInfinity)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxDistance, layerMask))
            return hit.transform;

        return null;
    }

    public static void SetEventMask(this PhysicsRaycaster physicsRaycaster, int mask)
    {
        if (physicsRaycaster)
        {
            physicsRaycaster.eventMask = mask;
        }
    }

    public static string GetPath(Transform transform)
    {
        int length = transform.name.Length;
        Transform parent = transform.parent;
        while (parent)
        {
            length += parent.name.Length;
            PathCache.Add(parent.name);
            parent = parent.parent;
        }
        StringBuilderCache = new StringBuilder(length + PathCache.Count);
        for (int i = PathCache.Count - 1; i >= 0; i--)
        {
            StringBuilderCache.Append(PathCache[i]);
            StringBuilderCache.Append('/');
        }
        StringBuilderCache.Append(transform.name);
        PathCache.Clear();
        return StringBuilderCache.ToString();
    }

    public static async void WrapErrors(this Task task) => await task;

    public static void SetSizeDeltaX(this RectTransform rectTransform, float x) => rectTransform.sizeDelta = new Vector2(x, rectTransform.sizeDelta.y);

    public static void SetSizeDeltaY(this RectTransform rectTransform, float y) => rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, y);

    public static bool EqualsEx(this Vector2 vec1, Vector2 vec2)
    {
        if (Math.Abs(vec1.x - vec2.x) >= 0.01 || Math.Abs(vec1.y - vec2.y) >= 0.01)
        {
            return false;
        }
        return true;
    }

    public static bool EqualsPosition(this Transform transform, Vector3 position, float magnitudeValue)
    {
        return (transform.position - position).magnitude <= magnitudeValue;
    }

    public static void UpdateLocalPositionX(this Transform transform, float x)
    {
        transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
    }

    public static void UpdateLocalPositionY(this Transform transform, float y)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
    }
}
