using EquipmentBooking.Data;
using EquipmentBooking.Models;

namespace EquipmentBooking.Services;

/// <summary>Каталог оборудования. Страницы и API получают данные только через этот сервис.</summary>
public class CatalogService
{
    public List<Equipment> GetAll() => EquipmentStore.Items.OrderBy(e => e.InventoryNumber).ToList();

    public Equipment? GetById(int id) => EquipmentStore.Items.FirstOrDefault(e => e.Id == id);
}
