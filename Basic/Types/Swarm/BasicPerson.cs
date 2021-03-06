using System;
using Swarmops.Common.Enums;
using Swarmops.Common.Interfaces;

namespace Swarmops.Basic.Types.Swarm
{
    [Serializable]
    public class BasicPerson : IEmailPerson, IHasIdentity
    {
        public BasicPerson (string name, string email)
        {
            this.Name = name;
            this.Email = email;
        }

        public BasicPerson (int personId, string passwordHash, string name, string email, string street,
            string postalCode, string cityName, int countryId, string phone, int geographyId,
            DateTime birthdate, PersonGender gender, string twitterId)
        {
            this.PersonId = personId;
            this.PasswordHash = passwordHash;
            this.Name = name;
            this.Email = email;
            this.Street = street;
            this.PostalCode = postalCode;
            this.CityName = cityName;
            this.CountryId = countryId;
            this.Phone = phone;
            this.GeographyId = geographyId;
            this.Birthdate = birthdate;
            this.Gender = gender;
            this.TwitterId = twitterId;
        }

        public BasicPerson (BasicPerson original)
            : this (original.PersonId, original.PasswordHash, original.Name, original.Email, original.Street,
                original.PostalCode, original.CityName, original.CountryId, original.Phone,
                original.GeographyId, original.Birthdate, original.Gender, original.TwitterId)
        {
        }


        public bool IsMale
        {
            get { return this.Gender == PersonGender.Male; }
        }

        public bool IsFemale
        {
            get { return this.Gender == PersonGender.Female; }
        }


        public int PersonId { get; private set; }
        public string Street { get; protected set; }
        public string PostalCode { get; protected set; }
        public string CityName { get; protected set; }
        public string Phone { get; protected set; }
        public DateTime Birthdate { get; protected set; }
        public string PasswordHash { get; protected set; }
        public int GeographyId { get; protected set; }
        public int CountryId { get; protected set; }
        public PersonGender Gender { get; protected set; }
        public string Name { get; protected set; }
        public string Email { get; protected set; }
        public string TwitterId { get; protected set; }


        public int Identity
        {
            get { return this.PersonId; }
        }
    }
}