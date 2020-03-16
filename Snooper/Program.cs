using Snooper;
using Steam.Models.SteamCommunity;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Snoop
{
    class SteamSnooper
    {
        // factory to be used to generate various web interfaces
        public SteamWebInterfaceFactory webInterfaceFactory;

        // this will map to the ISteamUser endpoint
        // note that you have full control over HttpClient lifecycle here
        public SteamUser steamInterface;

        // If there are parallel connections, the mappedFriends doesn't have the full list of a person. (just a note)
        public Dictionary<ulong, HashSet<ulong>> mappedFriends = new Dictionary<ulong, HashSet<ulong>>();

        private ulong steamIDToSearch;

        public HashSet<ulong> privateFriends = new HashSet<ulong>();

        // These are the people that have a private friends list.
        public HashSet<ulong> privateUsers = new HashSet<ulong>();

        // These are the people that haven't been checked if the friends are private.
        public HashSet<ulong> uncheckedUsers = new HashSet<ulong>();

        public SteamSnooper(string webApiKey, ulong steamIDToSearch = 0)
        {
            webInterfaceFactory = new SteamWebInterfaceFactory(webApiKey);
            steamInterface = webInterfaceFactory.CreateSteamWebInterface<SteamUser>(new HttpClient());
            this.steamIDToSearch = steamIDToSearch;
        }

        public async Task OpenFriendsOfAsync(ulong steamId)
        {
            HashSet<ulong> steamIds = new HashSet<ulong>(); // Initialize an empty set for friends.

            // if the steamId isn't mapped with friends and it isn't private
            if (!mappedFriends.ContainsKey(steamId) && !privateUsers.Contains(steamId))
            {
                // this will map to ISteamUser/GetFriendsListAsync method in the Steam Web API
                // see FriendListResultContainer.cs for response 
                ISteamWebResponse<IReadOnlyCollection<FriendModel>> friendsListResponse;

                try
                {
                    friendsListResponse = await steamInterface.GetFriendsListAsync(steamId);
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    mappedFriends.Add(steamId, new HashSet<ulong>()); // put that user with no connections
                    privateUsers.Add(steamId);
                    return;
                }

                var friendsList = friendsListResponse.Data;
                var arrayFriendsList = friendsList.ToArray();

                foreach (var item in arrayFriendsList)
                {
                    ulong friendSteamId = Convert.ToUInt64(item.SteamId.ToString());

                    if (friendSteamId == this.steamIDToSearch) // if this person is the person needed to be found
                    {
                        privateFriends.Add(steamId);
                    }

                    // this checks if there exists a parallel connection
                    if (mappedFriends.ContainsKey(friendSteamId))
                    {
                        mappedFriends.TryGetValue(friendSteamId, out HashSet<ulong> friendValues);
                        if (!(friendValues.Contains(steamId)))
                            steamIds.Add(friendSteamId);
                    }
                    else
                    {
                        // The friends list hasn't been checked yet so make it unchecked.
                        if (!privateUsers.Contains(friendSteamId)) // if it doesn't exist in private
                            uncheckedUsers.Add(friendSteamId);
                        steamIds.Add(friendSteamId);
                    }
                }
            }

            mappedFriends.Add(steamId, steamIds); // the steamId is now visited.

            if (uncheckedUsers.Contains(steamId)) // If the steamId was unchecked.
                uncheckedUsers.Remove(steamId); // remove it from unchecked.
        }

        public async Task<HashSet<ulong>> GetClientFriendsWithPrivateFriendsListAsync(ulong client)
        {
            if (mappedFriends[client] != null)
            {
                var friendsOfClient = mappedFriends[client];

                HashSet<ulong> privateFriendsOfClient = new HashSet<ulong>();

                foreach (var friend in friendsOfClient) // for each friend
                {
                    ISteamWebResponse<IReadOnlyCollection<FriendModel>> friendsListResponse;

                    try
                    {
                        friendsListResponse = await steamInterface.GetFriendsListAsync(friend);
                    }
                    catch (System.Net.Http.HttpRequestException ex) // friends list is private
                    {
                        privateFriendsOfClient.Add(friend);

                    }
                }

                return privateFriendsOfClient;
            }

            return null;
        }

        public async Task OpenFriendsOfUncheckedUsersAsync()
        {
            var steamIDs = new HashSet<ulong>(uncheckedUsers); // Isolates the uncheckedUsers.
            uncheckedUsers.Clear(); // This is to keep from recursively openFriendsOf(steamID);
            foreach (ulong steamID in steamIDs)
            {
                // This is to keep "An item with the same key has already been added" from happening again.
                try
                {
                    await OpenFriendsOfAsync(steamID);
                }
                catch (System.ArgumentException ex)
                {
                    continue;
                }
            }
        }

        public void PrintSteamIDs()
        {
            foreach (var entry in mappedFriends)
            {
                foreach (var value in entry.Value)
                {
                    Console.WriteLine(entry.Key + " is friends with " + value);
                }
                Console.WriteLine();
            }
        }

        public string getCustomURL(ulong steamID64)
        {
            return "" + steamInterface.GetCommunityProfileAsync(steamID64).Result.CustomURL;
        }

        public string getName(ulong steamID64)
        {
            return "" + steamInterface.GetCommunityProfileAsync(steamID64).Result.SteamID;
        }

        public string getFriendsOfPersonToSearch()
        {
            StringBuilder sb = new StringBuilder("", 17);

            if (privateFriends.Count != 0)
            {
                foreach (var steamID in this.privateFriends)
                {
                    sb.AppendLine("" + steamID);
                }
            }
            return sb.ToString();
        }

        public void PrintPrivateSteamIDs()
        {
            foreach (var steamID in this.privateUsers)
            {
                Console.WriteLine(steamID);
            }
        }

        public void PrintFriendsOfPersonToSearch()
        {
            foreach (var friend in this.privateFriends)
            {
                Console.WriteLine(friend);
            }
        }

        public async Task LoadSavedFriendsAsync()
        {
            string path = Environment.CurrentDirectory + @"\saved_friends\" + steamIDToSearch + "_friends.txt"; // change this line 
            if (File.Exists(path))
            {
                // Open the file to read from.
                using (StreamReader sr = File.OpenText(path))
                {
                    string s = sr.ReadLine(); // go ahead and read the first line to get rid of the extra bit.

                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s == "")
                        {
                            return;
                        }
                        ulong steamID64 = Convert.ToUInt64(s);
                        try
                        {
                            await OpenFriendsOfAsync(steamID64);
                        }
                        catch (System.ArgumentException ex)
                        {
                            continue;
                        }
                    }
                }
            }
        }

        async Task PrintPrivateFriends(ulong steamID64)
        {
            mappedFriends.TryGetValue(steamID64, out HashSet<ulong> friends);

            foreach (ulong friend in friends)
            {
                try
                {
                    ISteamWebResponse<IReadOnlyCollection<FriendModel>> friendsListResponse = await steamInterface.GetFriendsListAsync(steamID64);
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    Console.WriteLine(friend + " private");
                }
            }
        }

        public void SaveImage(string uri, ulong steamID64) // saves it to the debug folder.
        {
            using (WebClient webClient = new WebClient())
            {
                // 49 is the length of the image filename.
                string image_filename = uri.Substring(uri.Length - 49);

                if (!Directory.Exists(Environment.CurrentDirectory + @"\saved_pictures\"))
                    Directory.CreateDirectory(Environment.CurrentDirectory + @"\saved_pictures\");
                webClient.DownloadFile(uri, Environment.CurrentDirectory + @"\saved_pictures\" + steamID64 + ".png");
            }
        }

        static async Task Run()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string webApiKey = "put yours here dumbass";
            ulong client = 1;
            ulong privatePerson = 1;

            SteamSnooper program = new SteamSnooper(webApiKey, privatePerson);
            await program.OpenFriendsOfAsync(client);
            await program.PrintPrivateFriends(client);

            // await program.DownloadAvatarsOfFriends(client); // 2000ms
            // await program.OpenFriendsOfAsync(privatePerson); // this cannot be called in the constructor
            // await program.OpenFriendsOfUncheckedUsersAsync(); // 57000 ms
            // await program.OpenFriendsOfUncheckedUsersAsync(); // this should take about 27 minutes 
            // program.PrintFriendsOfPersonToSearch();
            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds + "milliseconds");
        }

        static async Task TestsAsync()
        {
            /*
            ulong client = 123;
            var WIF = new SteamWebInterfaceFactory("");
            var SI = WIF.CreateSteamWebInterface<SteamUser>(new HttpClient());
            SteamCommunityProfileModel playerSummaryResponse = await SI.GetCommunityProfileAsync(client);
            var playerSummaryData = playerSummaryResponse.AvatarFull;
            string uri = playerSummaryData.AbsoluteUri
            SaveImage(uri);
            */
            //var groupsList = playerSummaryData.ToArray();

            /*
            foreach (var group in groupsList)
            {
                Console.WriteLine(group.ToString());
            }*/
        }

        static void RunGUI()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        static async Task Main(string[] args)
        {

            //string ye = Directory.GetCurrentDirectory();
            //Console.WriteLine(ye);
            //await Run();
            RunGUI();
            //await TestsAsync();
        }
    }
}