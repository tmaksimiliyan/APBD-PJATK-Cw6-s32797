using System.ComponentModel.DataAnnotations;

namespace Cwiczenia6.DTOs;

public class CreateAppointmentRequestDto
{
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    public int IdDoctor { get; set; }
    
    [Required]
    public DateTime AppointmentDate { get; set; }
    
    [Required]
    [StringLength(250)]
    public string Reason { get; set; } = string.Empty;
    
}