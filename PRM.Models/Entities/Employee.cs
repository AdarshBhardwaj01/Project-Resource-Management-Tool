using PRM.Models.Enums;



namespace PRM.Models.Entities;



public class Employee

{

    public int Id { get; set; }



    public int UserId { get; set; }



    public int? ManagerId { get; set; }



    public string FullName { get; set; } = string.Empty;



    public string Email { get; set; } = string.Empty;



    public string Department { get; set; } = string.Empty;



    public string Designation { get; set; } = string.Empty;



    public EmployeeStatus Status { get; set; } = EmployeeStatus.Bench;



    public int UtilisationPercent { get; set; }



    public bool IsActive { get; set; } = true;



    public User User { get; set; } = null!;



    public User? Manager { get; set; }



    public ICollection<EmployeeSkill> Skills { get; set; } = new List<EmployeeSkill>();



    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();



    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();

}


