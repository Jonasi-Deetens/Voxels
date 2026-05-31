namespace Voxels.World.Climate
{
    public readonly struct ClimateSample
    {
        public readonly float Latitude;
        public readonly float Temperature;
        public readonly float Humidity;
        public readonly float Continentality;
        public readonly float LeyLine;
        public readonly int ElevationAboveSea;
        public readonly int CoastDistance;
        public readonly bool IsOcean;

        public ClimateSample(
            float latitude,
            float temperature,
            float humidity,
            float continentality,
            float leyLine,
            int elevationAboveSea,
            int coastDistance,
            bool isOcean)
        {
            Latitude = latitude;
            Temperature = temperature;
            Humidity = humidity;
            Continentality = continentality;
            LeyLine = leyLine;
            ElevationAboveSea = elevationAboveSea;
            CoastDistance = coastDistance;
            IsOcean = isOcean;
        }
    }
}
