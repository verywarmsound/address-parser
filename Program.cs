using System;
using RestSharp;
using RestSharp.Authenticators;
using Microsoft.EntityFrameworkCore;
using AddressValidator.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp.Deserializers;
using Newtonsoft.Json;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json.Serialization;

namespace ConsoleApp1
{
    public class Program
    {
        public static void ElapsedTime(string msg, Action action)
        {
            var watch = new Stopwatch();
            watch.Start();
            action();
            watch.Stop();
            Console.WriteLine($"{msg}\nElapsed time: {watch.Elapsed.TotalSeconds} s.\n");
        }

        static void Main(string[] args)
        {
            var prog = new Program();
            var context = new SynergyContext();

            Console.WriteLine("Program started");

            IQueryable<Table1> Table1 = context.Table1.FromSql("Select * from Table1");

            var watch = new Stopwatch();
            watch.Start();



            var partnerGroups = Table1
                .Where(a => a.column1 != null && a.column2 != null && a.column3 == "C")
                .GroupBy(a => a.CmpWwn.ToString()[0])
                .ToList();


            var clientGroups = Table1
                .Where(a => a.column4 != null && a.column2 != null && a.column3 == "A")
                .GroupBy(a => a.CmpWwn.ToString()[0])
                .ToList();

            watch.Stop();
            Console.WriteLine($"Gropuped partners and clients\nElapsed time: {watch.Elapsed.TotalMilliseconds} ms.\n");

            IList<Action> partnersActions = new List<Action>();
            foreach (var group in partnerGroups)
            {
                partnersActions.Add(() => prog.ImportPartners(group, group.Key));
            }

            IList<Action> clientActions = new List<Action>();
            foreach (var group in clientGroups)
            {
                clientActions.Add(() => prog.ImportClients(group, group.Key));
            }

            ElapsedTime("Whole program run time", () =>
            {
                ElapsedTime("Clients Imported", () =>
                    Parallel.Invoke(clientActions.ToArray())
                );
                ElapsedTime("Partners Imported", () =>
                    Parallel.Invoke(partnersActions.ToArray())
                );
            });



            //prog.Lookup();



        }


        public void ImportPartners(IEnumerable<Table1> partners, char groupKey)
        {

            var restClient = new RestClient();

            restClient.BaseUrl = new Uri("http://uri");
            var recordsImported = 0;
            foreach (var partnerRecord in partners)
            {
                var result = TransformRecord(partnerRecord, restClient, MapPartner);
                if (recordsImported % 250 == 0)
                {
                    Console.WriteLine($"Partners 'group-{groupKey}' imported so far ... {recordsImported}");
                }
                recordsImported = recordsImported + 1;
            }
        }


        public void ImportClients(IEnumerable<Table1> clients, char groupKey)
        {

            var restClient = new RestClient();
            restClient.BaseUrl = new Uri("http://uri");
            var recordsImported = 0;
            foreach (var clientRecord in clients)
            {
                var client = TransformRecord(clientRecord, restClient, MapClient);
                // TODO:  Save to DB
                if (recordsImported % 250 == 0)
                {
                    Console.WriteLine($"Clients 'group-{groupKey}' imported so far ... {recordsImported}");
                }
                recordsImported = recordsImported + 1;

            }
        }

        private Clients MapRecord(Table1 sourceRecord, SearchResults searchResult, int resultCount)
        {
            return new Clients()
            {
                GUID = sourceRecord.CmpWwn,
                Name = sourceRecord.CmpName,
                OldCity = sourceRecord.CmpFcity,
                OldPSC = sourceRecord.column2,

                City = searchResult?.townName,
                StreetName = searchResult?.streetName,
                HouseNumber = searchResult?.houseNumber,
                OrientationNumber = searchResult?.orientationNumber,

                PostalCode = searchResult?.postalCode,
                DateCreated = DateTime.Now,
                DateModified = DateTime.Now,
                Probability =resultCount == 0 ? 0 : (decimal) 1 / resultCount,
                Deleted = false
            };
        }

        private Clients MapClient(Table1 clientRecord, SearchResults searchResult, int resultCount)
        {
            var client = MapRecord(clientRecord, searchResult, resultCount);
            client.BirthRegistrationNumber = clientRecord.CmpCode;
            client.OldStreet = clientRecord.column4;
            return client;
        }
        private Clients MapPartner(Table1 partnerRecord, SearchResults searchResult, int resultCount)
        {
            var partner = MapRecord(partnerRecord, searchResult, resultCount);
            partner.RegistrationNumber = partnerRecord.CmpCode;
            partner.OldStreet = partnerRecord.column1;
            return partner;
        }

        private Clients TransformRecord(Table1 clientRecord, RestClient restClient, Func<Table1, SearchResults, int, Clients> mapFn)
        {
            var request = new RestRequest($"/api/v1/search/{clientRecord.column4 + " " + clientRecord.column2.Replace(" ", "")}", Method.GET);
            IRestResponse response;
            response = restClient.Execute(request);

            if (response.IsSuccessful)
            {
                JObject rss = JObject.Parse(response.Content.ToString());

                IEnumerable<JToken> results = rss["searchResults"].Children();
                IList<SearchResults> searchResults = new List<SearchResults>();
                foreach (JToken result in results)
                {
                    SearchResults searchResult = result.ToObject<SearchResults>();
                    searchResults.Add(searchResult);
                }

                var matches = searchResults.Count();
                if (matches >= 1)
                {
                    var searchResult = searchResults.FirstOrDefault();
                    return mapFn(clientRecord, searchResult, matches);
                }


                if (matches == 0)
                {
                    try
                    {
                        var request_try = new RestRequest($"/api/v1/search/{clientRecord.column4}", Method.GET);
                        IRestResponse response_try;
                        response_try = restClient.Execute(request_try);
                        JObject answer = JObject.Parse(response_try.Content.ToString());
                        IEnumerable<JToken> another_results = answer["searchResults"].Children();
                        IList<SearchResults> another_searchResults = new List<SearchResults>();
                        foreach (JToken result in another_results)
                        {
                            SearchResults searchResult = result.ToObject<SearchResults>();
                            another_searchResults.Add(searchResult);
                        }

                        var try_matches = another_results.Count();
                        var chosen = another_searchResults.FirstOrDefault();
                        return mapFn(clientRecord, chosen, try_matches);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return mapFn(clientRecord, null, 0);
                    }
                }
            }
            return mapFn(clientRecord, null, 0);

        }


        public void Lookup()
        {

            var clientpost = new RestClient();
            clientpost.BaseUrl = new Uri("https://b2c.cpost.cz/");
            var contextclient = new ClientContext();
            IQueryable<Clients> ent = contextclient.Clients.Where(x => x.PostalCode != null);
            var howmuch = ent.Count();
            foreach (var psc in ent)
            {
                var requestpost = new RestRequest($"/services/PostOfficeInformation/getDataAsJson?postCode={psc.PostalCode}", Method.GET);
                IRestResponse responsepost;
                responsepost = clientpost.Execute(requestpost);
                if (responsepost.IsSuccessful)
                {
                    JArray postSearch = JArray.Parse(responsepost.Content.ToString());

                    dynamic data = JObject.Parse(postSearch[0].ToString());
                    string region = (string)data["attributes"]["region"];
                    string district = (string)data["attributes"]["district"];
                    psc.Region = region;
                    psc.District = district;

                    //contextclient.Update(psc);
                }

                //contextclient.SaveChanges();
            }

        }

    }
}
