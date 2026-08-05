using UnityEditor;
using UnityEngine;

namespace TubityWAI
{
    public static class BuildPipeline
    {
        [MenuItem("Build/Export iOS Project")]
        public static void BuildiOSProject()
        {
            string[] scenes = { "Assets/Scenes/SampleScene.unity" };
            string buildPath = "Build-iOS";

            Debug.Log("[BuildPipeline] Setting Application Identifier and Product Name from Env...");
            string envBundleId = System.Environment.GetEnvironmentVariable("BUNDLE_ID");
            if (!string.IsNullOrEmpty(envBundleId))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, envBundleId);
                Debug.Log($"[BuildPipeline] Set Application Identifier to: {envBundleId}");
            }

            string envAppName = System.Environment.GetEnvironmentVariable("APP_NAME");
            if (!string.IsNullOrEmpty(envAppName))
            {
                PlayerSettings.productName = envAppName;
                Debug.Log($"[BuildPipeline] Set Product Name to: {envAppName}");
            }

            Debug.Log("[BuildPipeline] Configuring App Icon...");
            AssetDatabase.ImportAsset("Assets/AppIcon.png", ImportAssetOptions.ForceUpdate);
            Texture2D appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon.png");
            if (appIcon != null)
            {
                TextureImporter importer = AssetImporter.GetAtPath("Assets/AppIcon.png") as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.textureShape = TextureImporterShape.Texture2D;
                    importer.isReadable = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                // Apply legacy icon fallback
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new Texture2D[] { appIcon });

                // Set iOS Application and App Store Marketing Icons using platform-specific APIs
                var targetGroup = BuildTargetGroup.iOS;
                
                // 1. Marketing Icon (1024x1024 App Store icon)
                var marketingKind = UnityEditor.iOS.iOSPlatformIconKind.Marketing;
                var marketingIcons = PlayerSettings.GetPlatformIcons(targetGroup, marketingKind);
                for (int i = 0; i < marketingIcons.Length; i++)
                {
                    marketingIcons[i].SetTextures(new Texture2D[] { appIcon });
                }
                PlayerSettings.SetPlatformIcons(targetGroup, marketingKind, marketingIcons);

                // 2. Application Icons (Springboard, Spotlight, Settings, etc.)
                var appKind = UnityEditor.iOS.iOSPlatformIconKind.Application;
                var appIcons = PlayerSettings.GetPlatformIcons(targetGroup, appKind);
                for (int i = 0; i < appIcons.Length; i++)
                {
                    appIcons[i].SetTextures(new Texture2D[] { appIcon });
                }
                PlayerSettings.SetPlatformIcons(targetGroup, appKind, appIcons);

                Debug.Log("[BuildPipeline] App Icon and iOS Marketing Icon successfully assigned.");
            }
            else
            {
                Debug.LogWarning("[BuildPipeline] Assets/AppIcon.png not found. Skipping icon assignment.");
            }

            Debug.Log("[BuildPipeline] Starting iOS export for SampleScene...");

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes;
            options.locationPathName = buildPath;
            options.target = BuildTarget.iOS;
            options.options = BuildOptions.None;

            var report = UnityEditor.BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[BuildPipeline] Success! Xcode project exported to: {buildPath}");
            }
            else
            {
                Debug.LogError($"[BuildPipeline] Failed! Result: {summary.result}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        [MenuItem("Build/Export Android Project")]
        public static void BuildAndroidProject()
        {
            string[] scenes = { "Assets/Scenes/SampleScene.unity" };
            string buildPath = "Build-Android/TubityX.aab";

            Debug.Log("[BuildPipeline] Setting Application Identifier and Product Name from Env...");
            string envBundleId = System.Environment.GetEnvironmentVariable("BUNDLE_ID");
            if (!string.IsNullOrEmpty(envBundleId))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, envBundleId);
                Debug.Log($"[BuildPipeline] Set Application Identifier to: {envBundleId}");
            }

            string envAppName = System.Environment.GetEnvironmentVariable("APP_NAME");
            if (!string.IsNullOrEmpty(envAppName))
            {
                PlayerSettings.productName = envAppName;
                Debug.Log($"[BuildPipeline] Set Product Name to: {envAppName}");
            }

            Debug.Log("[BuildPipeline] Configuring App Icon...");
            AssetDatabase.ImportAsset("Assets/AppIcon.png", ImportAssetOptions.ForceUpdate);
            Texture2D appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon.png");
            if (appIcon != null)
            {
                TextureImporter importer = AssetImporter.GetAtPath("Assets/AppIcon.png") as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.textureShape = TextureImporterShape.Texture2D;
                    importer.isReadable = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { appIcon });
                Debug.Log("[BuildPipeline] App Icon successfully assigned to Android Target Group.");
            }
            else
            {
                Debug.LogWarning("[BuildPipeline] Assets/AppIcon.png not found. Skipping icon assignment.");
            }

            Debug.Log("[BuildPipeline] Configuring Keystore Signing Settings...");
            string keystorePath = System.Environment.GetEnvironmentVariable("KEYSTORE_PATH");
            string keystorePass = System.Environment.GetEnvironmentVariable("KEYSTORE_PASS");
            string keyAlias = System.Environment.GetEnvironmentVariable("KEY_ALIAS");
            string keyPass = System.Environment.GetEnvironmentVariable("KEY_PASS");

            if (!string.IsNullOrEmpty(keystorePath) && !string.IsNullOrEmpty(keystorePass) &&
                !string.IsNullOrEmpty(keyAlias) && !string.IsNullOrEmpty(keyPass))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.keyaliasName = keyAlias;
                PlayerSettings.Android.keyaliasPass = keyPass;
                Debug.Log("[BuildPipeline] Keystore configuration applied.");
            }
            else
            {
                Debug.LogWarning("[BuildPipeline] Keystore variables missing. Building an unsigned/debug build.");
            }

            Debug.Log("[BuildPipeline] Starting Android App Bundle (.aab) build...");
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.scenes = scenes;
            options.locationPathName = buildPath;
            options.target = BuildTarget.Android;
            options.options = BuildOptions.None;

            var report = UnityEditor.BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[BuildPipeline] Success! Android App Bundle compiled to: {buildPath}");
            }
            else
            {
                Debug.LogError($"[BuildPipeline] Failed! Result: {summary.result}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
