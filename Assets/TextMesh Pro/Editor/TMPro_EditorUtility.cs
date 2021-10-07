// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;


namespace TMPro.EditorUtilities
{

    public static class TMPro_EditorUtility
    {
        // Static Fields Related to locating the TextMesh Pro Asset
        private static bool isTMProFolderLocated;
        private static string folderPath = "Not Found";
        
        private static EditorWindow Gameview;
        private static bool isInitialized = false;

        private static void GetGameview()
        {
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type type = assembly.GetType("UnityEditor.GameView");
            Gameview = EditorWindow.GetWindow(type);
        }


        public static void RepaintAll()
        {
            if (isInitialized == false)
            {
                GetGameview();
                isInitialized = true;
            }

            SceneView.RepaintAll();
            Gameview.Repaint();
        }


        /// <summary>
        /// Create and return a new asset in a smart location based on the current selection and then select it.
        /// </summary>
        /// <param name="name">
        /// Name of the new asset. Do not include the .asset extension.
        /// </param>
        /// <returns>
        /// The new asset.
        /// </returns>
        public static T CreateAsset<T>(string name) where T : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path.Length == 0)
            {
                // no asset selected, place in asset root
                path = "Assets/" + name + ".asset";
            }
            else if (Directory.Exists(path))
            {
                // place in currently selected directory
                path += "/" + name + ".asset";
            }
            else {
                // place in current selection's containing directory
                path = Path.GetDirectoryName(path) + "/" + name + ".asset";
            }
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            return asset;
        }



        // Function used to find all materials which reference a font atlas so we can update all their references.
        public static Material[] FindMaterialReferences(Material mat)
        {
            // Find all Materials used in the project.
            Material[] mats = Resources.FindObjectsOfTypeAll<Material>();
            List<Material> refs = new List<Material>();

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].HasProperty(ShaderUtilities.ID_MainTex) && mats[i].mainTexture != null && mat.mainTexture != null && mats[i].mainTexture.GetInstanceID() == mat.mainTexture.GetInstanceID())
                {
                    if (!refs.Contains(mats[i]))
                        refs.Add(mats[i]);
                }
            }

            return refs.ToArray();
        }


        /// <summary>
        /// Function to find the asset folder location in case it was moved by the user.
        /// </summary>
        /// <returns></returns>
        public static string GetAssetLocation()
        {
            if (isTMProFolderLocated == false)
            {
                isTMProFolderLocated = true;
                string projectPath = Directory.GetCurrentDirectory();
                
                // Find all the directories that match "TextMesh Pro"
                string[] matchingPaths = Directory.GetDirectories(projectPath + "/Assets", "TextMesh Pro", SearchOption.AllDirectories);

                folderPath = ValidateLocation(matchingPaths);
                if (folderPath != null) return folderPath;    

                // Check alternative Asset folder name.
                matchingPaths = Directory.GetDirectories(projectPath + "/Assets", "TextMeshPro", SearchOption.AllDirectories);
                folderPath = ValidateLocation(matchingPaths);
                if (folderPath != null) return folderPath;

            }

            if (folderPath != null) return folderPath;
            else
            {
                Debug.LogWarning("Could not located the \"TextMesh Pro/GUISkins\" Folder to load the Editor Skins.");
                return null;
            }
        }


        /// <summary>
        /// Method to validate the location of the asset folder by making sure the GUISkins folder exists.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private static string ValidateLocation(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                // Check if any of the matching directories contain a GUISkins directory.
                if (Directory.Exists(paths[i] + "/GUISkins"))
                {
                    folderPath = "Assets" + paths[i].Split(new string[] { "/Assets" }, System.StringSplitOptions.None)[1];
                    return folderPath;
                }
            }

            return null;
        }

    }
}
