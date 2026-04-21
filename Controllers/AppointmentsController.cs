using Cwiczenia6.DTOs;
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

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequestDto request)
    {
        var appointment = await _appointmentService.CreateAppointmentAsync(request);
        if (!appointment.Success)
        {
            if (appointment.StatusCode == 409)
            {
                return Conflict(new ErrorResponseDto
                {
                  Message = appointment.ErrorMessage! 
                });
            }
            
            return BadRequest(new ErrorResponseDto
            {
                Message = appointment.ErrorMessage! 
            });
        }
        
        return CreatedAtAction(
            nameof(GetAppointmentById),
            new {idAppointment = appointment.NewAppointmentId},
            new {IdAppointment = appointment.NewAppointmentId});
    }

    [HttpPut("{idAppointment:int}")]
    public async Task<IActionResult> UpdateAppointment(int idAppointment,
        [FromBody] UpdateAppointmentRequestDto request)
    {
        var appointment = await _appointmentService.UpdateAppointmentAsync(idAppointment, request);

        if (!appointment.Success)
        {
            if (appointment.StatusCode == 404)
            {
                return NotFound(new ErrorResponseDto
                {
                    Message = appointment.ErrorMessage!
                });
            }
            
            if (appointment.StatusCode == 409)
            {
                return Conflict(new ErrorResponseDto
                {
                    Message = appointment.ErrorMessage! 
                });
            }
            
            return BadRequest(new ErrorResponseDto
            {
                Message = appointment.ErrorMessage! 
            });
        }

        var updatedAppointment = await _appointmentService.GetAppointmentByIdAsync(idAppointment);
        return Ok(updatedAppointment);
    }
}