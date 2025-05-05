using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace TravelAgencyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly string _connectionString;

        public TripsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("TravelAgencyDb");
        }

        /// <summary>
        /// GET /api/trips
        /// Pobiera wszystkie dostępne wycieczki wraz z ich podstawowymi informacjami i krajami docelowymi
        /// </summary>
        [HttpGet]
        public IActionResult GetTrips()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var trips = new List<TripDto>();
                    using (var command = new SqlCommand("SELECT IdTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip ORDER BY DateFrom", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            trips.Add(new TripDto
                            {
                                IdTrip = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.GetString(2),
                                DateFrom = reader.GetDateTime(3),
                                DateTo = reader.GetDateTime(4),
                                MaxPeople = reader.GetInt32(5)
                            });
                        }
                    }

                    foreach (var trip in trips)
                    {
                        using (var command = new SqlCommand(
                            "SELECT c.IdCountry, c.Name FROM Country c " +
                            "JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry " +
                            "WHERE ct.IdTrip = @IdTrip", connection))
                        {
                            command.Parameters.AddWithValue("@IdTrip", trip.IdTrip);
                            using (var reader = command.ExecuteReader())
                            {
                                trip.Countries = new List<CountryDto>();
                                while (reader.Read())
                                {
                                    trip.Countries.Add(new CountryDto
                                    {
                                        IdCountry = reader.GetInt32(0),
                                        Name = reader.GetString(1)
                                    });
                                }
                            }
                        }
                    }

                    return Ok(trips);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// GET /api/trips/clients/{id}
        /// Pobiera wszystkie wycieczki powiązane z konkretnym klientem
        /// </summary>
        [HttpGet("clients/{id}")]
        public IActionResult GetClientTrips(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @IdClient", connection))
                    {
                        command.Parameters.AddWithValue("@IdClient", id);
                        var clientExists = (int)command.ExecuteScalar() > 0;

                        if (!clientExists)
                        {
                            return NotFound();
                        }
                    }

                    var clientTrips = new List<ClientTripDto>();

                    using (var command = new SqlCommand(
                        "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, " +
                        "ct.RegisteredAt, ct.PaymentDate " +
                        "FROM Trip t " +
                        "JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip " +
                        "WHERE ct.IdClient = @IdClient " +
                        "ORDER BY ct.RegisteredAt DESC", connection))
                    {
                        command.Parameters.AddWithValue("@IdClient", id);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                clientTrips.Add(new ClientTripDto
                                {
                                    IdTrip = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Description = reader.GetString(2),
                                    DateFrom = reader.GetDateTime(3),
                                    DateTo = reader.GetDateTime(4),
                                    MaxPeople = reader.GetInt32(5),
                                    RegisteredAt = reader.GetDateTime(6),
                                    PaymentDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7)
                                });
                            }
                        }
                    }

                    foreach (var trip in clientTrips)
                    {
                        using (var command = new SqlCommand(
                            "SELECT c.IdCountry, c.Name FROM Country c " +
                            "JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry " +
                            "WHERE ct.IdTrip = @IdTrip", connection))
                        {
                            command.Parameters.AddWithValue("@IdTrip", trip.IdTrip);
                            using (var reader = command.ExecuteReader())
                            {
                                trip.Countries = new List<CountryDto>();
                                while (reader.Read())
                                {
                                    trip.Countries.Add(new CountryDto
                                    {
                                        IdCountry = reader.GetInt32(0),
                                        Name = reader.GetString(1)
                                    });
                                }
                            }
                        }
                    }

                    return Ok(clientTrips);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// POST /api/trips/clients
        /// Tworzy nowego klienta
        /// </summary>
        [HttpPost("clients")]
        public IActionResult CreateClient([FromBody] ClientCreateDto clientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM Client WHERE Pesel = @Pesel", connection))
                    {
                        command.Parameters.AddWithValue("@Pesel", clientDto.Pesel);
                        var peselExists = (int)command.ExecuteScalar() > 0;

                        if (peselExists)
                        {
                            return Conflict("Klient o podanym PESEL już istnieje");
                        }
                    }

                    using (var command = new SqlCommand(
                        "INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) " +
                        "OUTPUT INSERTED.IdClient " +
                        "VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)", connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
                        command.Parameters.AddWithValue("@LastName", clientDto.LastName);
                        command.Parameters.AddWithValue("@Email", clientDto.Email);
                        command.Parameters.AddWithValue("@Telephone", clientDto.Telephone);
                        command.Parameters.AddWithValue("@Pesel", clientDto.Pesel);

                        var newClientId = (int)command.ExecuteScalar();

                        return CreatedAtAction(nameof(GetClientTrips), new { id = newClientId }, new { IdClient = newClientId });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// PUT /api/trips/clients/{id}/trips/{tripId}
        /// Rejestruje klienta na wycieczkę
        /// </summary>
        [HttpPut("clients/{id}/trips/{tripId}")]
        public IActionResult RegisterClientForTrip(int id, int tripId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM Client WHERE IdClient = @IdClient", connection))
                    {
                        command.Parameters.AddWithValue("@IdClient", id);
                        var clientExists = (int)command.ExecuteScalar() > 0;

                        if (!clientExists)
                        {
                            return NotFound("Klient nie istnieje");
                        }
                    }

                    using (var command = new SqlCommand("SELECT COUNT(*), MaxPeople FROM Trip WHERE IdTrip = @IdTrip GROUP BY MaxPeople", connection))
                    {
                        command.Parameters.AddWithValue("@IdTrip", tripId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return NotFound("Wycieczka nie istnieje");
                            }

                            reader.Read();
                            var maxPeople = reader.GetInt32(1);

                            using (var countCommand = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip", connection))
                            {
                                countCommand.Parameters.AddWithValue("@IdTrip", tripId);
                                var currentParticipants = (int)countCommand.ExecuteScalar();

                                if (currentParticipants >= maxPeople)
                                {
                                    return Conflict("Osiągnięto maksymalną liczbę uczestników");
                                }
                            }
                        }
                    }

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection))
                    {
                        command.Parameters.AddWithValue("@IdClient", id);
                        command.Parameters.AddWithValue("@IdTrip", tripId);
                        var alreadyRegistered = (int)command.ExecuteScalar() > 0;

                        if (alreadyRegistered)
                        {
                            return Conflict("Klient jest już zapisany na tę wycieczkę");
                        }
                    }

                    using (var command = new SqlCommand(
                        "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) " +
                        "VALUES (@IdClient, @IdTrip, @RegisteredAt)", connection))
                    {
                        command.Parameters.AddWithValue("@IdClient", id);
                        command.Parameters.AddWithValue("@IdTrip", tripId);
                        command.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);

                        var rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok();
                        }
                        else
                        {
                            return StatusCode(500, "Nie udało się zarejestrować klienta");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// DELETE /api/trips/clients/{id}/trips/{tripId}
        /// Usuwa rejestrację klienta z wycieczki
        /// </summary>
        [HttpDelete("clients/{id}/trips/{tripId}")]
        public IActionResult DeleteClientFromTrip(int id, int tripId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand(
                        "SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection))
                    {
                        command.Parameters.AddWithValue("@IdClient", id);
                        command.Parameters.AddWithValue("@IdTrip", tripId);
                        var registrationExists = (int)command.ExecuteScalar() > 0;

                        if (!registrationExists)
                        {
                            return NotFound("Rejestracja nie istnieje");
                        }
                    }

                    using (var command = new SqlCommand(
                        "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection))
                    {
                        command.Parameters.AddWithValue("@IdClient", id);
                        command.Parameters.AddWithValue("@IdTrip", tripId);

                        var rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok();
                        }
                        else
                        {
                            return StatusCode(500, "Nie udało się usunąć rejestracji");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }

    // Klasy DTO
    public class TripDto
    {
        public int IdTrip { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int MaxPeople { get; set; }
        public List<CountryDto> Countries { get; set; }
    }

    public class ClientTripDto : TripDto
    {
        public DateTime RegisteredAt { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    public class CountryDto
    {
        public int IdCountry { get; set; }
        public string Name { get; set; }
    }

    public class ClientCreateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string Pesel { get; set; }
    }
}