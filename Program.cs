/*
 
 140JournosTweetMapper
 by Umut Oğuz Çelenli
 
 Reads the tweets from twitter, geocodes the addresses and puts the tweets into an excel file
 Uses YandexGeoCoder from exister / https://github.com/exister/YandexGeocoder
 
 */

using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Yandex;

namespace JournosTweetMapper
{
    class Program
    {

        static void Main(string[] args)
        {
            int NUMBER_OF_TWEETS_TO_READ = 5000;

            var filename = "";

            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            // these are the acces information for the Twitter Application that will pull data from Twitter API
            var userAccessToken = ConfigurationManager.AppSettings["userAccessToken"];
            var userAccessSecret = ConfigurationManager.AppSettings["userAccessSecret"];

            // consumer key and secret may expire from time to time, remember to change them from app.config
            var userConsumerKey = ConfigurationManager.AppSettings["userConsumerKey"];
            var userConsumerSecret = ConfigurationManager.AppSettings["userConsumerSecret"];

            // this is the twitter account to follow and geocode its tweets
            var twitterAccountToFollow = ConfigurationManager.AppSettings["twitterAccount"];

            var list = new List<TweetRecord>();
            TwitterCredentials.SetCredentials(userAccessToken, userAccessSecret, userConsumerKey, userConsumerSecret);
            var user = User.GetUserFromScreenName(twitterAccountToFollow);

            // create the API call parameter object
            var timelineParameter = Timeline.CreateUserTimelineRequestParameter(user.Id);
            timelineParameter.ExcludeReplies = true;
            timelineParameter.IncludeRTS = false;
            timelineParameter.TrimUser = false;
            timelineParameter.MaximumNumberOfTweetsToRetrieve = NUMBER_OF_TWEETS_TO_READ;

            // get the timeline tweets
            var timelineTweets = user.GetUserTimeline(timelineParameter);
            foreach (var tweet in timelineTweets)
            {
                // get a meaningful address chunk
                var address = getAddress(tweet.Text.ToLower());

                // try to geocode the given address
                double lat = 0, lng = 0;
                var results = YandexGeocoder.Geocode(address, 1, LangType.tr_TR);
                if (results.Count() > 0)
                {
                    var point = results.First().Point;
                    lat = point.Lat;
                    lng = point.Long;

                    // if the lat/lng is negative, the address was not sucessfully parsed
                    // there should be a spatial check for country boundries for better error correction but still.
                    if (lat < 0 || lng < 0)
                    {
                        lat = 0;
                        lng = 0;
                    }
                    Console.WriteLine("[{0}] --- {1}:{2} ", tweet.Id, address, point.ToString());
                }
                list.Add(new TweetRecord
                {
                    Body = tweet.Text,
                    Created = tweet.CreatedAt,
                    Id = tweet.Id,
                    Lat = lat,
                    Lng = lng
                });
            }

            DumpToExcel(list, filename);
            Console.ReadLine();

        }

        /// <summary>
        /// Takes a tweet and tries to extract addess information from it
        /// </summary>
        /// <param name="tweettext">the tweet to be parsed</param>
        /// <returns>an address string</returns>
        private static string getAddress(string tweettext)
        {
            string[] chunks = new string[] { };
            string address = "";
            if (tweettext.IndexOf(',') < 0)
            {
                int i = 20;
                while (tweettext.Substring(i, 1) != " ")
                {
                    i++;
                }
                address = tweettext.Substring(5, i);
            }
            else
            {
                chunks = tweettext.Substring(5, tweettext.Length - 5).Split(',');
                address = chunks[0];
            }

            address = address.Replace("ist.", "istanbul ");
            address = address.Replace("adl.", "adliyesi ");
            address = address.Replace("gs myd.", "galatasaray meydanı");
            address = address.Replace(" myd.", " meydanı ");
            address = address.Replace("cd.", "caddesi ");
            address = address.Replace(" cd ", "caddesi ");
            address = address.Replace("sk.", "sokağı ");
            address = address.Replace(" sk ", "sokağı ");
            address = address.Replace(" gs ", "galatasaray ");
            address = address.Replace("uni.", "üniversitesi ");
            address = address.Replace("üni.", "üniversitesi ");
            var dashIndex = address.IndexOf('-');
            if (dashIndex > -1)
            {
                address = address.Substring(0, dashIndex);
            }
            var dotIndex = address.IndexOf('.');
            if (dotIndex > -1)
            {
                address = address.Substring(0, dotIndex);
            }
            address = address.Replace("  ", " ").Replace("#", "");
            return address;
        }

        /// <summary>
        /// dumps the list of geocoded tweets to an excel file
        /// </summary>
        /// <param name="tweets"></param>
        /// <param name="filename"></param>
        public static void DumpToExcel(List<TweetRecord> tweets, string filename = "")
        {
            if (string.IsNullOrEmpty(filename))
            {
                filename = "Test" + Guid.NewGuid().ToString() + ".xlsx";
            }
            else
            {
                if (string.IsNullOrEmpty(System.IO.Path.GetExtension(filename)))
                {
                    filename += ".xlsx";
                }
            }
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

            using (var p = new ExcelPackage())
            {
                p.Workbook.Properties.Author = "Umut Celenli";
                p.Workbook.Properties.Title = "140 Journos";
                p.Workbook.Worksheets.Add("Tweets");

                var sheet = p.Workbook.Worksheets[1] as ExcelWorksheet;
                sheet.Name = "Tweets";
                sheet.Cells.Style.Font.Size = 10;
                sheet.Cells.Style.Font.Name = "Calibri";

                sheet.Cells[1, 1].Value = "ID";
                sheet.Cells[1, 2].Value = "LAT";
                sheet.Cells[1, 3].Value = "LNG";
                sheet.Cells[1, 4].Value = "Date";
                sheet.Cells[1, 5].Value = "Body";

                var row = 2;
                foreach (var tweet in tweets)
                {
                    sheet.Cells[row, 1].Value = tweet.Id.ToString();
                    sheet.Cells[row, 2].Value = tweet.Lat;
                    sheet.Cells[row, 3].Value = tweet.Lng;
                    sheet.Cells[row, 4].Value = tweet.Created;
                    sheet.Cells[row, 5].Value = tweet.Body;
                    row += 1;
                }
                System.IO.File.WriteAllBytes(savePath, p.GetAsByteArray());

                var errorCount = tweets.Count(x => x.Lat == 0 || x.Lng == 0);
                Console.WriteLine("Done.");
                Console.WriteLine("{0} tweets parsed, {1} tweets have error.", row - 1, errorCount);
                Console.WriteLine("Saved Excel file to {0}", savePath);
            }

        }
    }

    /// <summary>
    /// A small struct for putting tweets into a list
    /// </summary>
    public struct TweetRecord
    {
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Body { get; set; }

    }
}
