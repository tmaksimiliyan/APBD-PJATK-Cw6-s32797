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

    private async Task<bool> PatientExistsAndIsActiveAsync(SqlConnection connection, int idPatient)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM dbo.Patients
                           WHERE IdPatient = @IdPatient
                             AND IsActive = 1;
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = idPatient;
        
        var count = (int)await  command.ExecuteScalarAsync();
        return count > 0;
    }

    private async Task<bool> DoctorExistsAndIsActiveAsync(SqlConnection connection, int idDoctor)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM dbo.Doctors
                           WHERE IdDoctor = @IdDoctor
                             AND IsActive = 1;
                           """;
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        
        var count = (int)await command.ExecuteScalarAsync();
        return count > 0;
    }

    private async Task<bool> DoctorHasAppointmentConflictAsync(SqlConnection connection, int idDoctor, DateTime appointmentDate, int? excludedAppointmentId = null)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM dbo.Appointments
                           WHERE IdDoctor = @IdDoctor
                             AND AppointmentDate = @AppointmentDate
                             AND Status = N'Scheduled'
                             AND (@ExcludedAppointmentId IS NULL OR IdAppointment <> @ExcludedAppointmentId);
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = appointmentDate;
        command.Parameters.Add("@ExcludedAppointmentId", SqlDbType.Int).Value =
            excludedAppointmentId.HasValue ? excludedAppointmentId.Value : DBNull.Value;

        var count = (int)await command.ExecuteScalarAsync();
        return count > 0;
    }
    
    private async Task<(bool Exists, string Status, DateTime AppointmentDate)> GetAppointmentStateAsync(SqlConnection connection, int idAppointment)
    {
        const string sql = """
                           SELECT Status, AppointmentDate
                           FROM dbo.Appointments
                           WHERE IdAppointment = @IdAppointment;
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return (false, string.Empty, default);
        }

        return (
            true,
            reader.GetString(reader.GetOrdinal("Status")),
            reader.GetDateTime(reader.GetOrdinal("AppointmentDate"))
        );
    }
    
    public async Task<(bool Success, string? ErrorMessage, int? NewAppointmentId, int StatusCode)> CreateAppointmentAsync(CreateAppointmentRequestDto request)
    {
        if (request.AppointmentDate <= DateTime.UtcNow)
        {
            return (false, "Appointment date cannot be in the past.", null, 400);
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return (false, "Reason is required.", null, 400);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        if (!await PatientExistsAndIsActiveAsync(connection, request.IdPatient))
        {
            return (false, "Patient does not exist or is inactive.", null, 400);
        }

        if (!await DoctorExistsAndIsActiveAsync(connection, request.IdDoctor))
        {
            return (false, "Doctor does not exist or is inactive.", null, 400);
        }

        if (await DoctorHasAppointmentConflictAsync(connection, request.IdDoctor, request.AppointmentDate))
        {
            return (false, "Doctor already has a scheduled appointment at this time.", null, 409);
        }

        const string sql = """
                           INSERT INTO dbo.Appointments
                               (IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes)
                           OUTPUT INSERTED.IdAppointment
                           VALUES
                               (@IdPatient, @IdDoctor, @AppointmentDate, N'Scheduled', @Reason, NULL);
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.IdPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.AppointmentDate;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.Reason;

        var newId = (int)await command.ExecuteScalarAsync();

        return (true, null, newId, 201);
    }

    public async Task<(bool Success, string? ErrorMessage, int StatusCode)> UpdateAppointmentAsync(int idAppointment,
        UpdateAppointmentRequestDto request)
    {
        var allowedStatuses = new[] { "Scheduled", "Completed", "Cancelled" };

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return (false, "Reason is required.", 400);
        }

        if (!allowedStatuses.Contains(request.Status))
        {
            return (false, "Status must be one of: Scheduled, Completed, Cancelled.", 400);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentState = await GetAppointmentStateAsync(connection, idAppointment);

        if (!currentState.Exists)
        {
            return (false, $"Appointment with id {idAppointment} was not found.", 404);
        }

        if (!await PatientExistsAndIsActiveAsync(connection, request.IdPatient))
        {
            return (false, "Patient does not exist or is inactive.", 400);
        }

        if (!await DoctorExistsAndIsActiveAsync(connection, request.IdDoctor))
        {
            return (false, "Doctor does not exist or is inactive.", 400);
        }

        var appointmentDateChanged = currentState.AppointmentDate != request.AppointmentDate;

        if (currentState.Status == "Completed" && appointmentDateChanged)
        {
            return (false, "Cannot change appointment date for a completed appointment.", 409);
        }

        if (request.Status == "Scheduled")
        {
            var hasConflict = await DoctorHasAppointmentConflictAsync(
                connection,
                request.IdDoctor,
                request.AppointmentDate,
                idAppointment);

            if (hasConflict)
            {
                return (false, "Doctor already has a scheduled appointment at this time.", 409);
            }
        }

        const string sql = """
            UPDATE dbo.Appointments
            SET
                IdPatient = @IdPatient,
                IdDoctor = @IdDoctor,
                AppointmentDate = @AppointmentDate,
                Status = @Status,
                Reason = @Reason,
                InternalNotes = @InternalNotes
            WHERE IdAppointment = @IdAppointment;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.IdPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.AppointmentDate;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = request.Status;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.Reason;
        command.Parameters.Add("@InternalNotes", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(request.InternalNotes) ? DBNull.Value : request.InternalNotes;

        await command.ExecuteNonQueryAsync();

        return (true, null, 200);
    } 
    
    public async Task<(bool Success, string? ErrorMessage, int StatusCode)> DeleteAppointmentAsync(int idAppointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var currentState = await GetAppointmentStateAsync(connection, idAppointment);

        if (!currentState.Exists)
        {
            return (false, $"Appointment with id {idAppointment} was not found.", 404);
        }

        if (currentState.Status == "Completed")
        {
            return (false, "Completed appointment cannot be deleted.", 409);
        }

        const string sql = """
                           DELETE FROM dbo.Appointments
                           WHERE IdAppointment = @IdAppointment;
                           """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await command.ExecuteNonQueryAsync();

        return (true, null, 204);
    }
   
}