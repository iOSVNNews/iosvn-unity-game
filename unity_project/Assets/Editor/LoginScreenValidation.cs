#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using IOSVN.TuTien.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Editor
{
    public static class LoginScreenValidation
    {
        public static void RenderAuthScreens()
        {
            RenderForm(false, "login.png");
            RenderForm(true, "register.png");
        }

        private static void RenderForm(bool register, string filename)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controller = new GameObject("LoginLayoutPreview").AddComponent<PrototypeBootstrap>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PrototypeBootstrap).GetMethod("BuildCanvas", flags).Invoke(controller, null);
            var canvas = GameObject.Find("GameCanvas").GetComponent<Canvas>();
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 295f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(1280, 590, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 100f;
            canvas.GetComponent<CanvasScaler>().SendMessage("Handle", SendMessageOptions.DontRequireReceiver);
            typeof(PrototypeBootstrap).GetMethod("ShowAccountForm", flags).Invoke(controller, new object[] { register });
            Canvas.ForceUpdateCanvases();
            GameObject.Find("LoginBackdrop").SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 590, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 590), 0, 0);
            texture.Apply();
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/login-layout-captures"));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, filename), texture.EncodeToPNG());
            RenderTexture.active = old;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            Debug.Log("LOGIN_LAYOUT_RENDERED: " + filename);
        }

    }
}
#endif
