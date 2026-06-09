namespace PRM.Models.DTOs.Manager;

public class EmployeeUtilisationPreviewDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int CurrentUtilisationPercent { get; set; }

    public string UtilisationNote { get; set; } = string.Empty;
}
