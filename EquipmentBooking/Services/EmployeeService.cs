using EquipmentBooking.Data;
using EquipmentBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBooking.Services;

/// <summary>Справочник сотрудников (не удаляются: на них ссылаются брони).</summary>
public class EmployeeService
{
    private readonly AppDbContext _db;

    public EmployeeService(AppDbContext db) => _db = db;

    public List<Employee> GetAll() => _db.Employees.AsNoTracking().OrderBy(e => e.FullName).ToList();

    public Employee? GetById(int id) => _db.Employees.AsNoTracking().FirstOrDefault(e => e.Id == id);

    public OperationResult Save(Employee e)
    {
        e.FullName = (e.FullName ?? "").Trim();
        e.Department = (e.Department ?? "").Trim();
        e.Email = string.IsNullOrWhiteSpace(e.Email) ? null : e.Email.Trim();
        if (e.FullName.Length == 0) return OperationResult.Invalid("Укажите ФИО");
        if (e.Department.Length == 0) return OperationResult.Invalid("Укажите подразделение");

        if (e.Id == 0)
        {
            _db.Employees.Add(e);
        }
        else
        {
            var existing = _db.Employees.FirstOrDefault(x => x.Id == e.Id);
            if (existing == null) return OperationResult.NotFound("Сотрудник не найден");
            existing.FullName = e.FullName;
            existing.Department = e.Department;
            existing.Email = e.Email;
        }
        _db.SaveChanges();
        return OperationResult.Ok(e.Id);
    }
}
