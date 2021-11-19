﻿using System.Collections.Generic;
using ShipIt.Models.ApiModels;

namespace ShipItTest.Builders
{
    public class CompanyBuilder
    {
        private string Gcp = "0000346";
        private string Name = "Robert Bosch Tool Corporation";
        private string Addr2 = "1800 West Central";
        private string Addr3 = "";
        private string Addr4 = "IL";
        private string PostalCode = "60056";
        private string City = "Mount Prospect";
        private string Tel = "(224) 232-2407";
        private string Mail = "info@gs1us.org";

        public CompanyBuilder SetGcp(string gcp)
        {
            Gcp = gcp;
            return this;
        }

        public CompanyBuilder SetName(string name)
        {
            Name = name;
            return this;
        }

        public CompanyBuilder SetAddr2(string addr2)
        {
            Addr2= addr2;
            return this;
        }

        public CompanyBuilder SetAddr3(string addr3)
        {
            Addr3 = addr3;
            return this;
        }

        public CompanyBuilder SetAddr4(string addr4)
        {
            Addr4 = addr4;
            return this;
        }

        public CompanyBuilder SetPostalCode(string postalCode)
        {
            PostalCode = postalCode;
            return this;
        }

        public CompanyBuilder SetCity(string city)
        {
            City = city;
            return this;
        }

        public CompanyBuilder SetTel(string tel)
        {
            Tel = tel;
            return this;
        }

        public CompanyBuilder SetMail(string mail)
        {
            Mail = mail;
            return this;
        }

        public Company CreateCompany()
        {
            return new Company()
            {
                Gcp = Gcp,
                Name = Name,
                Addr2 = Addr2,
                Addr3 = Addr3,
                Addr4 = Addr4,
                PostalCode = PostalCode,
                City = City,
                Tel = Tel,
                Mail = Mail
            };
        }

        public AddCompaniesRequest CreateAddCompaniesRequest()
        {
            return new AddCompaniesRequest()
            {
                companies = new List<Company>()
                {
                    new ()
                    {
                        Gcp = Gcp,
                        Name = Name,
                        Addr2 = Addr2,
                        Addr3 = Addr3,
                        Addr4 = Addr4,
                        PostalCode = PostalCode,
                        City = City,
                        Tel = Tel,
                        Mail = Mail
                    }
                }
            };
        }
    }
}
