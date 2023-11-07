using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using System;
using BepInEx.Configuration;

namespace com.ayteeate.lethalcompany.fieldofview
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Lethal Company.exe")]
    public class FieldOfView : BaseUnityPlugin
    {
        public static FieldOfView Instance;
        private ConfigEntry<float> configFOV;
        public float ConfigFOV
        {
            get => configFOV.Value;
            set => configFOV.Value = value;
        }
        public static float defaultFOV = 66f;
        public static float targetFOV;
        public static float minFOV = 66f;
        public static float maxFOV = 110f;

        private void Awake()
        {
            Instance = this;
            configFOV = Config.Bind(
                "General", // The section under which the option is shown
                "FieldOfView", // The key of the configuration option in the configuration file
                defaultFOV, // The default value
                "Set FOV here. Value must be between 66-110" // Description of the option to show in the config file
            );
            targetFOV = Math.Clamp(configFOV.Value, minFOV, maxFOV);

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        }

        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerB_Patch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void PlayerControllerB_Update(PlayerControllerB __instance)
        {
            float fov = FieldOfView.targetFOV; // TODO: get FOV from slider
            switch (__instance.targetFOV)
            {
                case 46: // Inspect clipboard
                    fov *= 0.7f;
                    break;
                case 60: // View terminal
                    fov *= 0.9f;
                    break;
                case 68: // Sprint
                    fov *= 1.03f;
                    break;
                default:
                    break;
            }
            __instance.targetFOV = fov;
            FieldOfView.Instance.ConfigFOV = fov;
            __instance.gameplayCamera.fieldOfView = Mathf.Lerp(__instance.gameplayCamera.fieldOfView, __instance.targetFOV, 6f * Time.deltaTime);
        }
    }

    [HarmonyPatch(typeof(HUDManager))]
    public class HUDManager_Patch
    {
        [HarmonyPatch("SubmitChat_performed")]
        [HarmonyPrefix]
        public static void HUDManager_SubmitChat_performed(HUDManager __instance)
        {
            string text = __instance.chatTextField.text;
            if (text.StartsWith("fov"))
            {
                string[] fov = text.Split(' ');
                if (fov.Length > 1)
                {
                    if(float.TryParse(fov[1], out float newFOV))
                    {
                        newFOV = Math.Clamp(newFOV, FieldOfView.minFOV, FieldOfView.maxFOV);
                        FieldOfView.targetFOV = newFOV;
                        Traverse.Create(__instance).Method("AddChatMessage", new object[] { $"FOV changed to {newFOV}", "" }).GetValue();
                    }
                    else
                    {
                        UnityEngine.Debug.Log("fov change failed");
                        Traverse.Create(__instance).Method("AddChatMessage", new object[] { "Failed to change FOV", "" }).GetValue();
                    }
                }
            }
        }
    }
}
