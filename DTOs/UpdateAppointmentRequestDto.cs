using System.ComponentModel.DataAnnotations;

namespace Cwiczenia6.DTOs;

public class UpdateAppointmentRequestDto
{
    [Required]
    public int IdPatient { get; set; }
    
    [Required]
    public int IdDoctor { get; set; }
    
    [Required]
    public DateTime AppointmentDate { get; set; }
    
    [Required]
    [StringLength(30)] public string Status { get; set; } = string.Empty;
    
    [Required]
    [StringLength(250)]
    public string Reason { get; set; } = string.Empty;
    
    [StringLength(250)]
    public string? InternalNotes { get; set; }
}