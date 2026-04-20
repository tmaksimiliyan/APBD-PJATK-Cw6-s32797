using System.Data;
using Cwiczenia6.DTOs;
using Microsoft.Data.SqlClient;

namespace Cwiczenia6.Services;

public class AppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing DeafultConnection string");
    }

    public async Task<List<AppointmentListDto>> GetAppointmentsAsync(string? status, string? patientLastName)
    {
        const string sql = """
                           SELECT a.IdAppointment,
                           a.AppointmentDate,
                           a.Status,
                           a.Reason,
                           p.FirstName + N' ' + p.LastName AS PatientFullName,
                           p.Email AS PatientEmail
                           FROM dbo.Appointments a
                           JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                           WHERE(@Status IS NULL OR a.Status = @Status)
                           AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                           ORDER BY a.AppointmentDate;
                           """;

        var result = new List<AppointmentListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status;
        
        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 80).Value = string.IsNullOrWhiteSpace(patientLastName) ? DBNull.Value : patientLastName;
        
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Reason = reader.GetString(reader.GetOrdinal("Reason")),
                PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail")),
                PatientFullName = reader.GetString(reader.GetOrdinal("PatientFullName")),
            });
        }
        
        return result;
        
    }
}