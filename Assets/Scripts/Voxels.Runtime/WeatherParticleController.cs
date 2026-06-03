using UnityEngine;
using Voxels.World.Climate;

namespace Voxels.Runtime
{
    public sealed class WeatherParticleController : MonoBehaviour
    {
        [SerializeField] float followHeight = 18f;

        ParticleSystem rainSystem;
        ParticleSystem snowSystem;
        Transform followTarget;

        public void Initialize(Transform playerOrCamera)
        {
            followTarget = playerOrCamera;
            EnsureSystems();
        }

        void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 position = followTarget.position + Vector3.up * followHeight;
            if (rainSystem != null)
            {
                rainSystem.transform.position = position;
            }

            if (snowSystem != null)
            {
                snowSystem.transform.position = position;
            }
        }

        public void ApplyWeather(in WeatherSnapshot snapshot)
        {
            if (rainSystem == null || snowSystem == null)
            {
                return;
            }

            bool snowing = snapshot.Kind == WeatherKind.Snow;
            float intensity = snapshot.Precipitation;

            var rainEmission = rainSystem.emission;
            var snowEmission = snowSystem.emission;

            if (snowing)
            {
                rainEmission.rateOverTime = 0f;
                snowEmission.rateOverTime = intensity * 420f;
                if (!snowSystem.isPlaying && intensity > 0.05f)
                {
                    snowSystem.Play();
                }
            }
            else
            {
                snowEmission.rateOverTime = 0f;
                rainEmission.rateOverTime = intensity * 650f;
                if (!rainSystem.isPlaying && intensity > 0.05f)
                {
                    rainSystem.Play();
                }
            }

            if (intensity <= 0.05f)
            {
                rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                snowSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        void EnsureSystems()
        {
            if (rainSystem == null)
            {
                rainSystem = CreatePrecipitationSystem("Rain", true);
            }

            if (snowSystem == null)
            {
                snowSystem = CreatePrecipitationSystem("Snow", false);
            }
        }

        static ParticleSystem CreatePrecipitationSystem(string label, bool isRain)
        {
            var root = new GameObject(label);
            var system = root.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = isRain ? 1.1f : 2.4f;
            main.startSpeed = isRain ? 14f : 2.5f;
            main.startSize = isRain ? 0.06f : 0.12f;
            main.gravityModifier = isRain ? 1.2f : 0.15f;
            main.maxParticles = isRain ? 1200 : 800;
            main.startColor = isRain
                ? new Color(0.75f, 0.82f, 0.95f, 0.55f)
                : new Color(0.95f, 0.97f, 1f, 0.85f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 0.1f, 28f);

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = !isRain;
            if (!isRain)
            {
                velocity.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
                velocity.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            }

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(isRain);

            return system;
        }

        static Material CreateParticleMaterial(bool isRain)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            Color color = isRain ? new Color(0.8f, 0.88f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.7f);
            material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
