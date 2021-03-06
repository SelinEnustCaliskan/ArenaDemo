// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms


//#define DEBUG_ON

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_4_6 || UNITY_4_7 || UNITY_5
using UnityEngine.UI;


namespace TMPro
{

    public static class TMP_MaterialManager
    {
        private static List<MaskingMaterial> m_materialList = new List<MaskingMaterial>();
        //private static Mask[] m_maskComponents;

        private static List<FallbackMaterial> m_fallbackMaterialList = new List<FallbackMaterial>();


        /// <summary>
        /// Create a Masking Material Instance for the given ID
        /// </summary>
        /// <param name="baseMaterial"></param>
        /// <param name="stencilID"></param>
        /// <returns></returns>
        public static Material GetStencilMaterial(Material baseMaterial, int stencilID)
        {
            // Check if Material supports masking
            if (!baseMaterial.HasProperty(ShaderUtilities.ID_StencilID))
            {
                Debug.LogWarning("Selected Shader does not support Stencil Masking. Please select the Distance Field or Mobile Distance Field Shader.");
                return baseMaterial;
            }

            int baseMaterialID = baseMaterial.GetInstanceID();

            // If baseMaterial already has a corresponding masking material, return it.
            for (int i = 0; i < m_materialList.Count; i++)
            {
                if (m_materialList[i].baseMaterial.GetInstanceID() == baseMaterialID && m_materialList[i].stencilID == stencilID)
                {
                    m_materialList[i].count += 1;

                    #if DEBUG_ON
                    ListMaterials();
                    #endif

                    return m_materialList[i].stencilMaterial;
                }
            }

            // No matching masking material found. Create and return a new one.

            Material stencilMaterial;

            //Create new Masking Material Instance for this Base Material 
            stencilMaterial = new Material(baseMaterial);
            stencilMaterial.hideFlags = HideFlags.HideAndDontSave;
            stencilMaterial.name += " Masking ID:" + stencilID;
            stencilMaterial.shaderKeywords = baseMaterial.shaderKeywords;

            // Set Stencil Properties
            ShaderUtilities.GetShaderPropertyIDs();
            stencilMaterial.SetFloat(ShaderUtilities.ID_StencilID, stencilID);
            //stencilMaterial.SetFloat(ShaderUtilities.ID_StencilOp, 0);
            stencilMaterial.SetFloat(ShaderUtilities.ID_StencilComp, 4);
            //stencilMaterial.SetFloat(ShaderUtilities.ID_StencilReadMask, stencilID);
            //stencilMaterial.SetFloat(ShaderUtilities.ID_StencilWriteMask, 0);

            MaskingMaterial temp = new MaskingMaterial();
            temp.baseMaterial = baseMaterial;
            temp.stencilMaterial = stencilMaterial;
            temp.stencilID = stencilID;
            temp.count = 1;

            m_materialList.Add(temp);

            #if DEBUG_ON
            ListMaterials();
            #endif

            return stencilMaterial;
        }


        /// <summary>
        /// Function to release the stencil material.
        /// </summary>
        /// <param name="stencilMaterial"></param>
        public static void ReleaseStencilMaterial(Material stencilMaterial)
        {
            int stencilMaterialID = stencilMaterial.GetInstanceID();
            
            for (int i = 0; i < m_materialList.Count; i++)
            {
                if (m_materialList[i].stencilMaterial.GetInstanceID() == stencilMaterialID)
                {
                    if (m_materialList[i].count > 1)
                        m_materialList[i].count -= 1;
                    else
                    {
                        Object.DestroyImmediate(m_materialList[i].stencilMaterial);
                        m_materialList.RemoveAt(i);
                        stencilMaterial = null;
                    }

                    break;
                }
            }


            #if DEBUG_ON
            ListMaterials();
            #endif
        }


        // Function which returns the base material associated with a Masking Material
        public static Material GetBaseMaterial(Material stencilMaterial)
        {
            // Check if maskingMaterial already has a base material associated with it.
            int index = m_materialList.FindIndex(item => item.stencilMaterial == stencilMaterial);

            if (index == -1)
                return null;
            else
                return m_materialList[index].baseMaterial;

        }


        /// <summary>
        /// Function to set the Material Stencil ID
        /// </summary>
        /// <param name="material"></param>
        /// <param name="stencilID"></param>
        /// <returns></returns>
        public static Material SetStencil(Material material, int stencilID)
        {
            material.SetFloat(ShaderUtilities.ID_StencilID, stencilID);
            
            if (stencilID == 0)
                material.SetFloat(ShaderUtilities.ID_StencilComp, 8);
            else
                material.SetFloat(ShaderUtilities.ID_StencilComp, 4);

            return material;
        }


        public static void AddMaskingMaterial(Material baseMaterial, Material stencilMaterial, int stencilID)
        {
            // Check if maskingMaterial already has a base material associated with it.
            int index = m_materialList.FindIndex(item => item.stencilMaterial == stencilMaterial);

            if (index == -1)
            {
                MaskingMaterial temp = new MaskingMaterial();
                temp.baseMaterial = baseMaterial;
                temp.stencilMaterial = stencilMaterial;
                temp.stencilID = stencilID;
                temp.count = 1;

                m_materialList.Add(temp);
            }
            else
            {
                stencilMaterial = m_materialList[index].stencilMaterial;
                m_materialList[index].count += 1;
            }
        }



        public static void RemoveStencilMaterial(Material stencilMaterial)
        {
            // Check if maskingMaterial is already on the list.
            int index = m_materialList.FindIndex(item => item.stencilMaterial == stencilMaterial);

            if (index != -1)
            {
                m_materialList.RemoveAt(index);
            }

            #if DEBUG_ON
            ListMaterials();
            #endif
        }



        public static void ReleaseBaseMaterial(Material baseMaterial)
        {
            // Check if baseMaterial already has a masking material associated with it.
            int index = m_materialList.FindIndex(item => item.baseMaterial == baseMaterial);

            if (index == -1)
            {
                Debug.Log("No Masking Material exists for " + baseMaterial.name);
            }
            else
            {
                if (m_materialList[index].count > 1)
                {
                    m_materialList[index].count -= 1;
                    Debug.Log("Removed (1) reference to " + m_materialList[index].stencilMaterial.name + ". There are " + m_materialList[index].count + " references left.");
                }
                else
                {
                    Debug.Log("Removed last reference to " + m_materialList[index].stencilMaterial.name + " with ID " + m_materialList[index].stencilMaterial.GetInstanceID());
                    Object.DestroyImmediate(m_materialList[index].stencilMaterial);
                    m_materialList.RemoveAt(index);
                }
            }

            #if DEBUG_ON
            ListMaterials();
            #endif
        }


        public static void ClearMaterials()
        {
            if (m_materialList.Count() == 0)
            {
                Debug.Log("Material List has already been cleared.");
                return;
            }

            for (int i = 0; i < m_materialList.Count(); i++)
            {
                //Material baseMaterial = m_materialList[i].baseMaterial;
                Material stencilMaterial = m_materialList[i].stencilMaterial;

                Object.DestroyImmediate(stencilMaterial);
                m_materialList.RemoveAt(i);
            }
        }


        /// <summary>
        /// Function to get the Stencil ID
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetStencilID(GameObject obj)
        {
            int count = 0;

            var maskComponents = TMP_ListPool<Mask>.Get();

            obj.GetComponentsInParent<Mask>(false, maskComponents);
            for (int i = 0; i < maskComponents.Count; i++)
            {
#if UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                if (maskComponents[i].IsActive())
                    count += 1;
#else
                if (maskComponents[i].MaskEnabled())
                    count += 1;
#endif
            }

            TMP_ListPool<Mask>.Release(maskComponents);

            return Mathf.Min((1 << count) - 1, 255);
        }


#if DEBUG_ON
        /// <summary>
        /// 
        /// </summary>
        public static void ListMaterials()
        {

            if (m_materialList.Count() == 0)
            {
                Debug.Log("Material List is empty.");
                return;
            }

            //Debug.Log("List contains " + m_materialList.Count() + " items.");

            for (int i = 0; i < m_materialList.Count(); i++)
            {
                Material baseMaterial = m_materialList[i].baseMaterial;
                Material stencilMaterial = m_materialList[i].stencilMaterial;

                Debug.Log("Item #" + (i + 1) + " - Base Material is [" + baseMaterial.name + "] with ID " + baseMaterial.GetInstanceID() + " is associated with [" + (stencilMaterial != null ? stencilMaterial.name : "Null") + "] Stencil ID " + m_materialList[i].stencilID + " with ID " + (stencilMaterial != null ? stencilMaterial.GetInstanceID() : 0) + " and is referenced " + m_materialList[i].count + " time(s).");
            }
        }
#endif

        public static Material GetFallbackMaterial (Material source, Texture mainTex)
        {
            int sourceID = source.GetInstanceID();
            int texID = mainTex.GetInstanceID();

            for (int i = 0; i < m_fallbackMaterialList.Count; i++)
            {
                if (m_fallbackMaterialList[i].fallbackMaterial == null)
                {
                    m_fallbackMaterialList.RemoveAt(i);
                    continue;
                }

                if (m_fallbackMaterialList[i].baseMaterial.GetInstanceID() == sourceID && m_fallbackMaterialList[i].fallbackMaterial.mainTexture.GetInstanceID() == texID)
                {
                    m_fallbackMaterialList[i].count += 1;

                    return m_fallbackMaterialList[i].fallbackMaterial;
                }
            }

            // Create new material from the source material
            Material fallbackMaterial = new Material(source);
            fallbackMaterial.name += " (Fallback Instance)";
            fallbackMaterial.mainTexture = mainTex;


            FallbackMaterial fallback = new FallbackMaterial();
            fallback.baseID = sourceID;
            fallback.baseMaterial = source;
            fallback.fallbackMaterial = fallbackMaterial;
            fallback.count = 1;

            m_fallbackMaterialList.Add(fallback);

            return fallbackMaterial;
        }



        private class FallbackMaterial
        {
            public int baseID;
            public Material baseMaterial;
            public Material fallbackMaterial;
            public int count;
        }


        private class MaskingMaterial
        {
            public Material baseMaterial;
            public Material stencilMaterial;
            public int count;
            public int stencilID;
        }

    }

}

#endif
