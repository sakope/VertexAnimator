using UnityEngine;
using System.Collections.Generic;
using nobnak.Gist.ObjectExt;
using nobnak.Gist.Extensions.Texture2DExt;

namespace VertexAnimater {

    public class VertexTexList : System.IDisposable {
        public const float FPS = 5f;
        public const FilterMode ANIM_TEX_FILTER = FilterMode.Bilinear;

        public const float DT = 1f / FPS;
        public const float COLOR_DEPTH = 255f;
        public const float COLOR_DEPTH_INV = 1f / COLOR_DEPTH;

        public readonly List<Vector2[]> uv2s = new List<Vector2[]>();
        public readonly List<Vector4> scales = new List<Vector4>();
        public readonly List<Vector4> offsets = new List<Vector4>();
        public readonly List<float> mposScalers = new List<float>();
        public readonly List<float> mposOffsets = new List<float>();
        public readonly List<float> mnormScalers = new List<float>();
        public readonly List<float> mnormOffsets = new List<float>();
        public readonly List<Texture2D> positionTextures = new List<Texture2D>();
        public readonly List<Texture2D> normalTextures = new List<Texture2D>();
        public readonly List<Texture2D> matrixTextures = new List<Texture2D>();
        public readonly List<float> frameEnds = new List<float>();
        public readonly List<List<Vector3[]>> verticesLists = new List<List<Vector3[]>>();
        public readonly List<List<Vector3[]>> normalsLists = new List<List<Vector3[]>>();
        public readonly List<List<Matrix4x4>> mposLists = new List<List<Matrix4x4>>();
        public readonly List<List<Matrix4x4>> mnormLists = new List<List<Matrix4x4>>();

        public VertexTexList(IMeshesSampler sample, bool createModelTexture) {
            var meshCount = sample.Outputs.Count;

            for (int i = 0; i < meshCount; i++) {
                verticesLists.Add(new List<Vector3[]>());
                normalsLists.Add(new List<Vector3[]>());
                if (createModelTexture) {
                    mposLists.Add(new List<Matrix4x4>());
                    mnormLists.Add(new List<Matrix4x4>());
                }
            }

            for (float t = 0; t < (sample.Length + DT); t += DT) {
                var bakedSamples = sample.Sample(t);
                for (int i = 0; i < meshCount; i++) {
                    var vertices = bakedSamples[i].mesh.vertices;
                    var normals = bakedSamples[i].mesh.normals;

                    if (createModelTexture) {
                        mposLists[i].Add(bakedSamples[i].mpos);
                        mnormLists[i].Add(bakedSamples[i].mnorm);
                    }

                    if (!createModelTexture) {
                        for (var j = 0; j < vertices.Length; j++) {
                            vertices[j] = bakedSamples[i].mpos.MultiplyPoint3x4(vertices[j]);
                            normals[j] = bakedSamples[i].mnorm.MultiplyVector(normals[j]);
                        }
                    }

                    verticesLists[i].Add(vertices);
                    normalsLists[i].Add(normals);
                }
            }

            for (int i = 0; i < meshCount; i++) {
                var firstVertices = verticesLists[i][0];
                var firstVertex = firstVertices[0];
                var vertexCount = firstVertices.Length;
                frameEnds.Add(vertexCount - 1);

                var minX = firstVertex.x;
                var minY = firstVertex.y;
                var minZ = firstVertex.z;
                var maxX = firstVertex.x;
                var maxY = firstVertex.y;
                var maxZ = firstVertex.z;
                foreach (var vertices in verticesLists[i]) {
                    for (var j = 0; j < vertices.Length; j++) {
                        var v = vertices[j];
                        minX = Mathf.Min(minX, v.x);
                        minY = Mathf.Min(minY, v.y);
                        minZ = Mathf.Min(minZ, v.z);
                        maxX = Mathf.Max(maxX, v.x);
                        maxY = Mathf.Max(maxY, v.y);
                        maxZ = Mathf.Max(maxZ, v.z);
                    }
                }
                scales.Add(new Vector4(maxX - minX, maxY - minY, maxZ - minZ, 1f));
                offsets.Add(new Vector4(minX, minY, minZ, 1f));
                Debug.LogFormat("Scale={0} Offset={1}", scales[i], offsets[i]);

                var texWidth = LargerInPow2(vertexCount);
                var texHeight = LargerInPow2(verticesLists[i].Count * 2);
                Debug.Log(string.Format("tex({0}x{1}), nVertices={2} nFrames={3}", texWidth, texHeight, vertexCount, verticesLists[i].Count));

                positionTextures.Add(new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, true));
                positionTextures[i].filterMode = ANIM_TEX_FILTER;
                positionTextures[i].wrapMode = TextureWrapMode.Clamp;

                normalTextures.Add(new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, true));
                normalTextures[i].filterMode = ANIM_TEX_FILTER;
                normalTextures[i].wrapMode = TextureWrapMode.Clamp;

                uv2s.Add(new Vector2[vertexCount]);
                var texSize = new Vector2(1f / texWidth, 1f / texHeight);
                var halfTexOffset = 0.5f * texSize;
                for (int j = 0; j < uv2s[i].Length; j++)
                    uv2s[i][j] = new Vector2((float)j * texSize.x, 0f) + halfTexOffset;
                for (int y = 0; y < verticesLists[i].Count; y++) {
                    var vertices = verticesLists[i][y];
                    var normals = normalsLists[i][y];
                    for (int x = 0; x < vertices.Length; x++) {
                        var pos = Normalize(vertices[x], offsets[i], scales[i]);
                        Color c0, c1;
                        Encode(pos, out c0, out c1);
                        positionTextures[i].SetPixel(x, y, c0);
                        positionTextures[i].SetPixel(x, y + (texHeight >> 1), c1);

                        var normal = 0.5f * (normals[x].normalized + Vector3.one);
                        Encode(normal, out c0, out c1);
                        normalTextures[i].SetPixel(x, y, c0);
                        normalTextures[i].SetPixel(x, y + (texHeight >> 1), c1);
                    }
                }
                positionTextures[i].Apply();
                normalTextures[i].Apply();

                if (!createModelTexture) continue;

                var firstFrameMpos = mposLists[i][0];
                var minMposElement = firstFrameMpos[0, 0];
                var maxMposElement = firstFrameMpos[0, 0];
                var firstFrameMnorm = mnormLists[i][0];
                var minMnormElement = firstFrameMnorm[0, 0];
                var maxMnormElement = firstFrameMnorm[0, 0];
                for (int f = 0; f < mposLists[i].Count; f++) {
                    for (int r = 0; r < 4; r++) {
                        for (int c = 0; c < 4; c++) {
                            minMposElement = Mathf.Min(minMposElement, mposLists[i][f][r, c]);
                            maxMposElement = Mathf.Max(maxMposElement, mposLists[i][f][r, c]);
                            minMnormElement = Mathf.Min(minMnormElement, mnormLists[i][f][r, c]);
                            maxMnormElement = Mathf.Max(maxMnormElement, mnormLists[i][f][r, c]);
                        }
                    }
                }
                mposScalers.Add(maxMposElement - minMposElement);
                mposOffsets.Add(minMposElement);
                mnormScalers.Add(maxMnormElement - minMnormElement);
                mnormOffsets.Add(minMnormElement);

                matrixTextures.Add(new Texture2D(8, texHeight, TextureFormat.RGBA32, false, true));
                matrixTextures[i].filterMode = ANIM_TEX_FILTER;
                matrixTextures[i].wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < verticesLists[i].Count; y++) {
                    var eachFrameMpos = mposLists[i][y];
                    var eachFrameMnorm = mnormLists[i][y];
                    for (int c = 0; c < 4; c++) {
                        var mposColumnVec = Normalize(eachFrameMpos.GetColumn(c), mposOffsets[i], mposScalers[i]);
                        Color c0, c1;
                        Encode(mposColumnVec, out c0, out c1);
                        matrixTextures[i].SetPixel(c, y, c0);
                        matrixTextures[i].SetPixel(c, y + (texHeight >> 1), c1);

                        var mnormColumnVec = Normalize(eachFrameMnorm.GetColumn(c), mnormOffsets[i], mnormScalers[i]);
                        Encode(mnormColumnVec, out c0, out c1);
                        matrixTextures[i].SetPixel(c + 4, y, c0);
                        matrixTextures[i].SetPixel(c + 4, y + (texHeight >> 1), c1);
                    }
                }
                matrixTextures[i].Apply();
            }
        }

        public Vector3 Position(int meshid, int vid, float frame) {
            frame = Mathf.Clamp(frame, 0f, frameEnds[meshid]);
            var uv = uv2s[meshid][vid];
            uv.y += frame * positionTextures[meshid].texelSize.y;
            var pos1 = positionTextures[meshid].GetPixelBilinear(uv.x, uv.y);
            var pos2 = positionTextures[meshid].GetPixelBilinear(uv.x, uv.y + 0.5f);
            return new Vector3(
                (pos1.r + pos2.r / COLOR_DEPTH) * scales[meshid].x + offsets[meshid].x,
                (pos1.g + pos2.g / COLOR_DEPTH) * scales[meshid].y + offsets[meshid].y,
                (pos1.b + pos2.b / COLOR_DEPTH) * scales[meshid].z + offsets[meshid].z);
        }
        public Bounds Bounds(int meshid) { return new Bounds((Vector3)(0.5f * scales[meshid] + offsets[meshid]), (Vector3)scales[meshid]); }
        public Vector3[] Vertices(int meshid, float frame) {
            frame = Mathf.Clamp(frame, 0f, frameEnds[meshid]);
            var index = Mathf.Clamp((int)frame, 0, verticesLists[meshid].Count - 1);
            var vertices = verticesLists[meshid][index];
            return vertices;
        }

        public static Vector3 Normalize(Vector3 pos, Vector3 offset, Vector3 scale) {
            return new Vector3(
                (pos.x - offset.x) / scale.x,
                (pos.y - offset.y) / scale.y,
                (pos.z - offset.z) / scale.z);
        }
        public static Vector4 Normalize(Vector4 v, float offset, float scale) {
            return new Vector4(
                (v.x - offset),
                (v.y - offset),
                (v.z - offset),
                (v.w - offset)) / scale;
        }
        public static void Encode(float v01, out float c0, out float c1) {
            c0 = Mathf.Clamp01(Mathf.Floor(v01 * COLOR_DEPTH) * COLOR_DEPTH_INV);
            c1 = Mathf.Clamp01(Mathf.Round((v01 - c0) * COLOR_DEPTH * COLOR_DEPTH) * COLOR_DEPTH_INV);
        }
        public static void Encode(Vector3 v01, out Color c0, out Color c1) {
            float c0x, c0y, c0z, c1x, c1y, c1z;
            Encode(v01.x, out c0x, out c1x);
            Encode(v01.y, out c0y, out c1y);
            Encode(v01.z, out c0z, out c1z);
            c0 = new Color(c0x, c0y, c0z, 1f);
            c1 = new Color(c1x, c1y, c1z, 1f);
        }
        public static void Encode(Vector4 v01, out Color c0, out Color c1) {
            float c0x, c0y, c0z, c0w, c1x, c1y, c1z, c1w;
            Encode(v01.x, out c0x, out c1x);
            Encode(v01.y, out c0y, out c1y);
            Encode(v01.z, out c0z, out c1z);
            Encode(v01.w, out c0w, out c1w);
            c0 = new Color(c0x, c0y, c0z, c0w);
            c1 = new Color(c1x, c1y, c1z, c1w);
        }
        public static int LargerInPow2(int width) {
            width--;
            var digits = 0;
            while (width > 0) {
                width >>= 1;
                digits++;
            }
            return 1 << digits;
        }

        #region IDisposable implementation
        public void Dispose() {
            positionTextures.ForEach(t => t.Destroy());
            normalTextures.ForEach(t => t.Destroy());
            matrixTextures.ForEach(t => t.Destroy());
            positionTextures.Clear();
            normalTextures.Clear();
            matrixTextures.Clear();
        }
        #endregion
    }
}
