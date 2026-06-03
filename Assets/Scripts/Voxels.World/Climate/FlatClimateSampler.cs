using Unity.Mathematics;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.World.Climate
{
    public sealed class FlatClimateSampler
    {
        readonly WorldSettings settings;
        readonly uint seed;
        readonly int worldHexRadius;

        public FlatClimateSampler(WorldSettings settings)
        {
            this.settings = settings;
            seed = (uint)math.max(1, settings.Seed);
            worldHexRadius = math.max(1, settings.WorldHexRadius);
        }

        public ClimateSample Sample(
            in HexCoord absoluteHex,
            int surfaceHeight,
            int seaLevel,
            int coastDistance)
        {
            float latitude = ComputeLatitude(absoluteHex);
            bool isOcean = surfaceHeight < seaLevel;
            int elevationAboveSea = math.max(0, surfaceHeight - seaLevel);

            float continentality = math.saturate(coastDistance / 24f);
            float2 noisePos = FlatHexGrid.AxialToWorld(absoluteHex, settings.BlockSize).xz;
            float leyLine = noise.snoise(noisePos * 0.12f + SeedOffset(401).xy);
            leyLine = leyLine * 0.5f + 0.5f;

            float tempNoise = noise.snoise(noisePos * 0.18f + SeedOffset(307).xy) * 0.08f;
            float temperature = math.lerp(1f, -1f, math.abs(latitude));
            temperature -= elevationAboveSea * 0.035f;
            temperature += continentality * 0.12f;
            temperature += tempNoise;

            float humidityBand = ComputeLatitudeHumidity(latitude);
            float humidity = humidityBand;
            humidity += math.saturate(1f - coastDistance / 10f) * 0.45f;
            humidity -= continentality * 0.35f;
            humidity += noise.snoise(noisePos * 0.25f + SeedOffset(509).xy) * 0.12f;
            humidity = math.saturate(humidity);

            return new ClimateSample(
                latitude * math.PI * 0.5f,
                temperature,
                humidity,
                continentality,
                leyLine,
                elevationAboveSea,
                coastDistance,
                isOcean);
        }

        float ComputeLatitude(in HexCoord hex)
        {
            float normalized = hex.R / (float)worldHexRadius;
            return math.clamp(normalized, -1f, 1f);
        }

        static float ComputeLatitudeHumidity(float latitude)
        {
            float absLat = math.abs(latitude);
            if (absLat < 0.2f)
            {
                return 0.85f;
            }

            if (absLat < 0.45f)
            {
                return 0.25f;
            }

            if (absLat < 0.7f)
            {
                return 0.65f;
            }

            return 0.35f;
        }

        float2 SeedOffset(int salt)
        {
            uint hash = math.hash(new int3((int)seed, salt, 0));
            return new float2(
                (hash & 0xFF) / 255f * 100f,
                ((hash >> 8) & 0xFF) / 255f * 100f);
        }
    }
}
