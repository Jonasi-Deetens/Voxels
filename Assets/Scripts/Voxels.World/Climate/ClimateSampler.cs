using Unity.Mathematics;
using Voxels.Core.Sphere;
using Voxels.World;

namespace Voxels.World.Climate
{
    public sealed class ClimateSampler
    {
        readonly PlanetSettings settings;
        readonly float3 spinAxis;
        readonly uint seed;

        public float3 SpinAxis => spinAxis;

        public ClimateSampler(PlanetSettings settings)
        {
            this.settings = settings;
            seed = (uint)math.max(1, settings.Seed);
            spinAxis = settings.ResolveSpinAxis();
        }

        public ClimateSample Sample(
            in SphereHexCell cell,
            int surfaceHeight,
            int seaLevel,
            int crustTop,
            OceanDistanceField coastField)
        {
            float3 normal = cell.Normal;
            float latitude = ComputeLatitude(normal);
            bool isOcean = surfaceHeight <= seaLevel;
            int elevationAboveSea = math.max(0, surfaceHeight - seaLevel);
            int coastDistance = coastField.GetCoastDistance(cell.Index);

            float continentality = math.saturate(coastDistance / 24f);
            float leyLine = noise.snoise(normal * 1.35f + SeedOffset(401));
            leyLine = leyLine * 0.5f + 0.5f;

            float tempNoise = noise.snoise(normal * 2.1f + SeedOffset(307)) * 0.08f;
            float temperature = math.lerp(1f, -1f, math.abs(latitude) / (math.PI * 0.5f));
            temperature -= elevationAboveSea * 0.035f;
            temperature += continentality * 0.12f;
            temperature += tempNoise;

            float humidityBand = ComputeLatitudeHumidity(latitude);
            float humidity = humidityBand;
            humidity += math.saturate(1f - coastDistance / 10f) * 0.45f;
            humidity -= continentality * 0.35f;
            humidity += noise.snoise(normal * 3.2f + SeedOffset(509)) * 0.12f;
            humidity = math.saturate(humidity);

            return new ClimateSample(
                latitude,
                temperature,
                humidity,
                continentality,
                leyLine,
                elevationAboveSea,
                coastDistance,
                isOcean);
        }

        float ComputeLatitude(float3 normal)
        {
            float dot = math.clamp(math.dot(normal, spinAxis), -1f, 1f);
            return math.acos(dot);
        }

        static float ComputeLatitudeHumidity(float latitude)
        {
            float latDeg = math.degrees(latitude);
            float absLat = math.abs(latDeg);
            if (absLat < 12f)
            {
                return 0.85f;
            }

            if (absLat < 32f)
            {
                return 0.25f;
            }

            if (absLat < 55f)
            {
                return 0.65f;
            }

            return 0.35f;
        }

        float3 SeedOffset(int salt)
        {
            uint hash = math.hash(new int3((int)seed, salt, 0));
            return new float3(
                (hash & 0xFF) / 255f * 100f,
                ((hash >> 8) & 0xFF) / 255f * 100f,
                ((hash >> 16) & 0xFF) / 255f * 100f);
        }
    }
}
