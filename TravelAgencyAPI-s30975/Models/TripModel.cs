namespace TravelAgencyAPI.Models
{
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