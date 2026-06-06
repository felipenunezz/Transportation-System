namespace Transportation_System.Models.Domain;

    public class TelemetryData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public int PassengerCount { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
