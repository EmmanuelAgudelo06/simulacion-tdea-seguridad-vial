using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// Menú "TdeA" en la barra de Unity: arma la escena de RA y la configuración en un clic.
public static class ConfigurarSimulacion
{
    const string Carpeta = "Assets/SimulacionTdeA";
    const string RutaEscena = Carpeta + "/SimulacionTdeA.unity";

    [MenuItem("TdeA/1. Crear escena de la simulación")]
    public static void CrearEscena()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CrearMateriales();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // AR Session
        bool okSesion = EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
        if (!okSesion || Object.FindFirstObjectByType<ARSession>() == null)
        {
            var s = new GameObject("AR Session");
            s.AddComponent<ARSession>();
            s.AddComponent<ARInputManager>();
        }

        // XR Origin (cámara del teléfono)
        bool okOrigen = EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");
        var origin = Object.FindFirstObjectByType<XROrigin>();
        if (!okOrigen || origin == null) origin = CrearOrigenManual();

        if (origin.GetComponent<ARRaycastManager>() == null) origin.gameObject.AddComponent<ARRaycastManager>();
        var planos = origin.GetComponent<ARPlaneManager>();
        if (planos == null) planos = origin.gameObject.AddComponent<ARPlaneManager>();
        planos.requestedDetectionMode = PlaneDetectionMode.Horizontal;

        var cam = origin.Camera != null ? origin.Camera : Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        }

        // Luz
        var sol = new GameObject("Sol").AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.shadows = LightShadows.Soft;
        sol.intensity = 1.2f;
        sol.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Sistema de eventos para los botones
        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>();

        // Simulación
        new GameObject("SimulacionVial").AddComponent<SimulacionVial>();

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };

        ConfigurarProyecto();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Simulación TdeA",
            "Escena creada y guardada en " + RutaEscena + ".\n\nAhora revisa en Project Settings > XR Plug-in Management que ARKit (pestaña iOS) y ARCore (pestaña Android) estén activados, y compila siguiendo el LEEME.",
            "Listo");
    }

    [MenuItem("TdeA/2. Recrear materiales")]
    public static void CrearMateriales()
    {
        string res = Carpeta + "/Resources";
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "SimulacionTdeA");
        if (!AssetDatabase.IsValidFolder(res)) AssetDatabase.CreateFolder(Carpeta, "Resources");

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");

        var mLit = new Material(lit);
        if (mLit.HasProperty("_Smoothness")) mLit.SetFloat("_Smoothness", 0.12f);
        if (mLit.HasProperty("_Glossiness")) mLit.SetFloat("_Glossiness", 0.12f);
        GuardarMaterial(mLit, res + "/TdeA_Lit.mat");
        GuardarMaterial(new Material(unlit), res + "/TdeA_Unlit.mat");
    }

    static void GuardarMaterial(Material m, string ruta)
    {
        if (File.Exists(ruta)) AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
    }

    static XROrigin CrearOrigenManual()
    {
        var go = new GameObject("XR Origin");
        var origin = go.AddComponent<XROrigin>();
        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(go.transform, false);
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(offset.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<ARCameraManager>();
        camGo.AddComponent<ARCameraBackground>();
        camGo.AddComponent<AudioListener>();
        var tpd = camGo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        var posA = new InputAction("Posicion", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
        posA.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rotA = new InputAction("Rotacion", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
        rotA.AddBinding("<HandheldARInputDevice>/deviceRotation");
        tpd.positionInput = new InputActionProperty(posA);
        tpd.rotationInput = new InputActionProperty(rotA);
        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offset;
        return origin;
    }

    static void ConfigurarProyecto()
    {
        PlayerSettings.productName = "Baja la velocidad";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.sensoria.bajalavelocidad");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.sensoria.bajalavelocidad");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
            PlayerSettings.iOS.cameraUsageDescription = "La cámara se usa para mostrar la simulación de seguridad vial en realidad aumentada.";
    }
}
