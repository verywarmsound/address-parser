using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using AddressValidator.Models;

namespace ConsoleApp1
{
    public class Clients 
    {
        public int Id { get; set; }
        public Guid GUID { get; set; }

        public string RegistrationNumber { get; set; }

        public string BirthRegistrationNumber { get; set; }
        public string Name { get; set; }

        public string OldStreet { get; set; }
        public string OldCity { get; set; }
        public string OldPSC { get; set; }
        public string City { get; set; }

        public string Region { get; set; }
        public string District { get; set; }

        public string StreetName { get; set; }

        public int? HouseNumber { get; set; }

        public string OrientationNumber { get; set; }

        public string PostalCode { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }

        public bool Deleted { get; set; }

        public decimal Probability { get; set; }

       
      

    }



}

