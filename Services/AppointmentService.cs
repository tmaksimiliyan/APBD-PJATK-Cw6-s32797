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

    public async Task<AppointmentsDetailsDto?> GetAppointmentByIdAsync(int idAppointment)
    {
        const string sql = """
                           SELECT
                               a.IdAppointment,
                               a.AppointmentDate,
                               a.Status,
                               a.Reason,
                               a.InternalNotes,
                               a.CreatedAt,

                               p.IdPatient,
                               p.FirstName AS PatientFirstName,
                               p.LastName AS PatientLastName,
                               p.Email AS PatientEmail,
                               p.PhoneNumber AS PatientPhoneNumber,

                               d.IdDoctor,
                               d.FirstName AS DoctorFirstName,
                               d.LastName AS DoctorLastName,
                               d.LicenseNumber AS DoctorLicenseNumber,

                               s.Name AS SpecializationName
                           FROM dbo.Appointments a
                           JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                           JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
                           JOIN dbo.Specializations s ON s.IdSpecialization = d.IdSpecialization
                           WHERE a.IdAppointment = @IdAppointment;
                           """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AppointmentsDetailsDto()
        {
            IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
            AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            Reason = reader.GetString(reader.GetOrdinal("Reason")),
            InternalNotes = reader.IsDBNull(reader.GetOrdinal("InternalNotes"))
                ? null
                : reader.GetString(reader.GetOrdinal("InternalNotes")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),

            IdPatient = reader.GetInt32(reader.GetOrdinal("IdPatient")),
            PatientFirstName = reader.GetString(reader.GetOrdinal("PatientFirstName")),
            PatientLastName = reader.GetString(reader.GetOrdinal("PatientLastName")),
            PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail")),
            PatientPhoneNumber = reader.GetString(reader.GetOrdinal("PatientPhoneNumber")),

            IdDoctor = reader.GetInt32(reader.GetOrdinal("IdDoctor")),
            DoctorFirstName = reader.GetString(reader.GetOrdinal("DoctorFirstName")),
            DoctorLastName = reader.GetString(reader.GetOrdinal("DoctorLastName")),
            DoctorLicenseNumber = reader.GetString(reader.GetOrdinal("DoctorLicenseNumber")),
            SpecializationName = reader.GetString(reader.GetOrdinal("SpecializationName"))
        };
    }
}