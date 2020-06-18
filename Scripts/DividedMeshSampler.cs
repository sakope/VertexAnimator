using UnityEngine;
using System.Collections.Generic;

namespace VertexAnimater {
    public class MeshSample {
        public Mesh mesh;
        public Matrix4x4 mpos, mnorm;
        public Transform transform;
    }

    public class DividedMeshSampler : IMeshesSampler {
        public GameObject Target { get; private set; }
        public List<MeshSample> Outputs { get; private set; }
        public float Length { get; private set; }

        SkinnedMeshRenderer[] _skines;
        Animation[] _animations;
        AnimationState[] _state;

        public DividedMeshSampler(GameObject target) {
            Target = target;
            _skines = target.GetComponentsInChildren<SkinnedMeshRenderer>();
            Outputs = new List<MeshSample>(_skines.Length);

            for (var i = 0; i < _skines.Length; i++) {
                Outputs.Add(new MeshSample() { mesh = new Mesh(), transform = _skines[i].gameObject.transform });
            }

            _animations = target.GetComponentsInChildren<Animation>();
            _state = new AnimationState[_animations.Length];
            for (var i = 0; i < _animations.Length; i++) {
                var animation = _animations[i];
                var state = _state[i] = animation[animation.clip.name];
                state.speed = 0f;
                Length = Mathf.Max(Length, state.length);
                animation.Play(state.name);
            }
        }

        public void Dispose() {
            foreach (var meshsample in Outputs) {
                Object.Destroy(meshsample.mesh);
            }
            Outputs = null;
        }

        public List<MeshSample> Sample(float time) {
            time = Mathf.Clamp(time, 0f, Length);
            for (var i = 0; i < _animations.Length; i++) {
                _state[i].time = time;
                _animations[i].Sample();
            }

            for (var i = 0; i < _skines.Length; i++) {
                _skines[i].BakeMesh(Outputs[i].mesh);
                Outputs[i].mpos = _skines[i].transform.localToWorldMatrix;
                Outputs[i].mnorm = _skines[i].transform.worldToLocalMatrix.transpose;
            }

            return Outputs;
        }
    }

    public interface IMeshesSampler : System.IDisposable {
        GameObject Target { get; }
        List<MeshSample> Outputs { get; }
        float Length { get; }
        List<MeshSample> Sample(float time);
    }
}