namespace EquipmentDiagnosisAgent
{
    public enum EEquipmentState
    {
        Idle,
        Running,
        Error,
    }

    public class Equipment
    {
        public int Id { get; }
        public string Name { get; }
        public EEquipmentState Status { get; }
        public double Temperature { get; }
        public string? AlarmCode { get; }

        public Equipment(int id, string name, EEquipmentState status, double temperature, string? alarmCode = null)
        {
            Id = id;
            Name = name;
            Status = status;
            AlarmCode = alarmCode;
            Temperature = temperature;
        }
    }
}