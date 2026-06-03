using UnityEngine;

namespace Voxels.Runtime
{
    public sealed class BlockEditFeedback : MonoBehaviour
    {
        [SerializeField] ParticleSystem breakParticles;
        [SerializeField] ParticleSystem placeParticles;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip breakClip;
        [SerializeField] AudioClip placeClip;

        public void PlayBreak(Vector3 worldPosition)
        {
            PlayEffect(breakParticles, breakClip, worldPosition);
        }

        public void PlayPlace(Vector3 worldPosition)
        {
            PlayEffect(placeParticles, placeClip, worldPosition);
        }

        void PlayEffect(ParticleSystem particles, AudioClip clip, Vector3 worldPosition)
        {
            if (particles != null)
            {
                particles.transform.position = worldPosition;
                particles.Emit(12);
            }

            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, 0.35f);
            }
        }

        public static BlockEditFeedback Ensure(Transform parent)
        {
            var existing = parent.GetComponentInChildren<BlockEditFeedback>();
            if (existing != null)
            {
                return existing;
            }

            var feedbackObject = new GameObject("BlockEditFeedback");
            feedbackObject.transform.SetParent(parent, false);

            var feedback = feedbackObject.AddComponent<BlockEditFeedback>();
            feedback.audioSource = feedbackObject.AddComponent<AudioSource>();
            feedback.audioSource.spatialBlend = 1f;
            feedback.audioSource.maxDistance = 24f;

            var breakPs = new GameObject("BreakParticles").transform;
            breakPs.SetParent(feedbackObject.transform, false);
            feedback.breakParticles = breakPs.gameObject.AddComponent<ParticleSystem>();
            ConfigureBurstParticles(feedback.breakParticles, new Color(0.55f, 0.42f, 0.28f));

            var placePs = new GameObject("PlaceParticles").transform;
            placePs.SetParent(feedbackObject.transform, false);
            feedback.placeParticles = placePs.gameObject.AddComponent<ParticleSystem>();
            ConfigureBurstParticles(feedback.placeParticles, new Color(0.7f, 0.75f, 0.65f));

            return feedback;
        }

        static void ConfigureBurstParticles(ParticleSystem system, Color color)
        {
            var main = system.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 2.5f;
            main.startSize = 0.12f;
            main.startColor = color;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
        }
    }
}
