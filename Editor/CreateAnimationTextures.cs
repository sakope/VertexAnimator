using System.Collections;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics.Eventing.Reader;

namespace VertexAnimater {

    public class CreateAnimationTextures : CreateAnimationTexture {
        [System.Flags]
        public new enum CreationModeFlags { NONE = 0, NEW_MESH = 1 << 0, SEPARATE_MESH = 1 << 1, BAKE_MATRIX_TEX = 1 << 2 }

        [MenuItem("VertexAnimation/Prefab/Separate Mesh/Bake As Local Coordinate With Model Matrix Animation Texture (Keep Pivot In Shader)/Reuse Mesh")]
        public static void CreateVertexAndMatrixTexturesForSeparatedReuseMesh() {
            CreateVertexTextures(CreationModeFlags.SEPARATE_MESH | CreationModeFlags.BAKE_MATRIX_TEX);
        }

        [MenuItem("VertexAnimation/Prefab/Separate Mesh/Bake As Local Coordinate With Model Matrix Animation Texture (Keep Pivot In Shader)/New Mesh")]
        public static void CreateVertexAndMatrixTexturesForSeparatedNewMesh() {
            CreateVertexTextures(CreationModeFlags.NEW_MESH | CreationModeFlags.SEPARATE_MESH | CreationModeFlags.BAKE_MATRIX_TEX);
        }

        [MenuItem("VertexAnimation/Prefab/Separate Mesh/Bake As World Coordinate (Break Pivot)/Reuse Mesh")]
        public static void CreateVertexTexturesForSeparateReuseMesh() {
            CreateVertexTextures(CreationModeFlags.SEPARATE_MESH);
        }

        [MenuItem("VertexAnimation/Prefab/Separate Mesh/Bake As World Coordinate (Break Pivot)/New Mesh")]
        public static void CreateVertexTexturesForSeparateNewMesh() {
            CreateVertexTextures(CreationModeFlags.NEW_MESH | CreationModeFlags.SEPARATE_MESH);
        }

        public static void CreateVertexTextures(CreationModeFlags flags) {
            GameObject selection;
            if (!TryGetActiveGameObject(out selection))
                return;
            StartCoroutine(selection, CreateVertexTextures(selection, flags));
        }

        public static IEnumerator CreateVertexTextures(GameObject selection, CreationModeFlags flags) {
            var bakeModelMatrix = ContainsAllFlags(flags, CreationModeFlags.BAKE_MATRIX_TEX);

            if (bakeModelMatrix) {
                selection.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                selection.transform.localScale = Vector3.one;
            }

            var sampler = new DividedMeshSampler(selection);
            var vtex = new VertexTexList(sampler, bakeModelMatrix);

            var folderPath = AssureExistAndGetRootFolder();
            folderPath = CreateTargetFolder(selection, folderPath);
            yield return 0;

            var renderers = selection.GetComponentsInChildren<SkinnedMeshRenderer>();
            GameObject go = new GameObject(selection.name);

            Texture2D modelMatrixTex;

            var meshCount = sampler.Outputs.Count;
            for (int i = 0; i < meshCount; i++) {
                var posPngPath = folderPath + "/" + sampler.Outputs[i].transform.name + ".png";
                var normPngPath = folderPath + "/" + sampler.Outputs[i].transform.name + "_normal.png";
                var posTex = Save(vtex.positionTextures[i], posPngPath);
                var normTex = Save(vtex.normalTextures[i], normPngPath);
                Material mat;

                if (bakeModelMatrix) {
                    var modelMatrixPngPath = folderPath + "/" + sampler.Outputs[i].transform.name + "._model.png";
                    modelMatrixTex = Save(vtex.matrixTextures[i], modelMatrixPngPath, TextureImporterFormat.RGBA32);
                    mat = CreateMaterial(i, sampler, vtex, posTex, normTex, renderers[i], modelMatrixTex);
                }
                else {
                    mat = CreateMaterial(i, sampler, vtex, posTex, normTex, renderers[i]);
                }

                SaveAsset(mat, folderPath + "/" + sampler.Outputs[i].transform.name + ".mat");

                var mesh = renderers[i].sharedMesh;

                if (ContainsAllFlags(flags, CreationModeFlags.NEW_MESH)) {
                    mesh = sampler.Outputs[i].mesh;
                    mesh.bounds = vtex.Bounds(i);
                    SaveAsset(mesh, folderPath + "/" + sampler.Outputs[i].transform.name + ".asset");
                }

                var child = new GameObject(sampler.Outputs[i].transform.name);
                child.AddComponent<MeshRenderer>().sharedMaterial = mat;
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.transform.parent = go.transform;
            }

            PrefabUtility.SaveAsPrefabAsset(go, folderPath + "/" + selection.name + ".prefab");
        }

        private static Material CreateMaterial(int meshid, IMeshesSampler sampler, VertexTexList vtex,
            Texture2D posTex = null, Texture2D normTex = null, Renderer renderer = null, Texture2D modelTex = null) {

            Material mat = new Material(Shader.Find(modelTex ? ShaderMatrixConst.SHADER_MODEL_NAME : ShaderConst.SHADER_NAME));
            if (renderer != null && renderer.sharedMaterial != null)
                mat.mainTexture = renderer.sharedMaterial.mainTexture;
            if (posTex != null)
                mat.SetTexture(ShaderConst.SHADER_ANIM_TEX, posTex);
            mat.SetVector(ShaderConst.SHADER_SCALE, vtex.scales[meshid]);
            mat.SetVector(ShaderConst.SHADER_OFFSET, vtex.offsets[meshid]);
            mat.SetVector(ShaderConst.SHADER_ANIM_END, new Vector4(sampler.Length, vtex.verticesLists[meshid].Count - 1, 0f, 0f));
            mat.SetFloat(ShaderConst.SHADER_FPS, FPS);
            if (normTex != null)
                mat.SetTexture(ShaderConst.SHADER_NORM_TEX, normTex);
            if (modelTex != null) {
                mat.SetTexture(ShaderMatrixConst.SHADER_MODEL_TEX, modelTex);
                mat.SetFloat(ShaderMatrixConst.SHADER_MPOS_SCALE, vtex.mposScalers[meshid]);
                mat.SetFloat(ShaderMatrixConst.SHADER_MPOS_OFFSET, vtex.mposOffsets[meshid]);
                mat.SetFloat(ShaderMatrixConst.SHADER_MNORM_SCALE, vtex.mnormScalers[meshid]);
                mat.SetFloat(ShaderMatrixConst.SHADER_MNORM_OFFSET, vtex.mnormOffsets[meshid]);
            }
            return mat;
        }

        protected static bool ContainsAllFlags(CreationModeFlags flags, CreationModeFlags contains) {
            return (flags & contains) == contains;
        }

        protected static Texture2D Save(Texture2D tex, string pngPath, TextureImporterFormat texFormat = TextureImporterFormat.RGB24) {
#if UNITY_5_5_OR_NEWER
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
            var pngImporter = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            var pngSettings = new TextureImporterSettings();
            pngImporter.ReadTextureSettings(pngSettings);
            pngSettings.filterMode = ANIM_TEX_FILTER;
            pngSettings.mipmapEnabled = false;
            pngSettings.sRGBTexture = false;
            pngSettings.wrapMode = TextureWrapMode.Clamp;
            pngImporter.SetTextureSettings(pngSettings);
            var platformSettings = pngImporter.GetDefaultPlatformTextureSettings();
            platformSettings.format = texFormat;
            platformSettings.maxTextureSize = Mathf.Max(platformSettings.maxTextureSize, Mathf.Max(tex.width, tex.height));
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            pngImporter.SetPlatformTextureSettings(platformSettings);
            AssetDatabase.WriteImportSettingsIfDirty(pngPath);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

#else
            File.WriteAllBytes (pngPath, tex.EncodeToPNG ());
            AssetDatabase.ImportAsset (pngPath, ImportAssetOptions.ForceUpdate);
            var pngImporter = (TextureImporter)AssetImporter.GetAtPath (pngPath);
            var pngSettings = new TextureImporterSettings ();
            pngImporter.ReadTextureSettings (pngSettings);
            pngSettings.filterMode = ANIM_TEX_FILTER;
            pngSettings.mipmapEnabled = false;
            pngSettings.linearTexture = true;
            pngSettings.wrapMode = TextureWrapMode.Clamp;
            pngImporter.SetTextureSettings (pngSettings);
            pngImporter.textureFormat = TextureImporterFormat.RGB24;
            pngImporter.maxTextureSize = Mathf.Max (pngImporter.maxTextureSize, Mathf.Max (tex.width, tex.height));
            pngImporter.SaveAndReimport();
            //AssetDatabase.WriteImportSettingsIfDirty (pngPath);
            //AssetDatabase.ImportAsset (pngPath, ImportAssetOptions.ForceUpdate);
#endif

            return (Texture2D)AssetDatabase.LoadAssetAtPath(pngPath, typeof(Texture2D));
        }
    }
}