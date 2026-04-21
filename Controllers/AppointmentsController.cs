using Cwiczenia6.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cwiczenia6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;

    public AppointmentsController(AppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] string? status, [FromQuery] string? patientLastName)
    {
        var appointments = await _appointmentService.GetAppointmentsAsync(status, patientLastName);
        return Ok(appointments);
    }

    [HttpGet("{idAppointment:int}")]
    public async Task<IActionResult> GetAppointmentById(int idAppointment)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(idAppointment);

        if (appointment is null)
        {
          return   NotFound(new { Message = $"Appointment with id {idAppointment} was not found" });
        }
        
        return Ok(appointment);
    }
}