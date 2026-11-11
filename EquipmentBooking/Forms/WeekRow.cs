namespace EquipmentBooking.Forms;

/// <summary>Строка недельного графика загрузки: оборудование и занятые часы по дням.</summary>
public class WeekRow
{
    public int EquipmentId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Для каждого из 7 дней — список отрезков «чч:мм–чч:мм кто».</summary>
    public List<string>[] Days { get; set; } = Enumerable.Range(0, 7).Select(_ => new List<string>()).ToArray();
}
