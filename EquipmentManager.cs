namespace EquipmentDiagnosisAgent
{
    public class EquipmentManager
    {
        private readonly List<Equipment> _equipments;

        public EquipmentManager()
        {
            _equipments = new List<Equipment>()
            {
                new Equipment(1, "Etching Machine", EEquipmentState.Error, 65),
                new Equipment(2, "Cleaning Machine", EEquipmentState.Running, 95, "E102"),
                new Equipment(3, "Inspection Machine", EEquipmentState.Idle, 25)
            };
        }

        public Equipment? GetEquipmentById(int id)
        {
            return _equipments.Find(equipment => equipment.Id == id);
        }

        public IReadOnlyList<Equipment> GetAllEquipments()
        {
            return _equipments.AsReadOnly();
        }
    }
}