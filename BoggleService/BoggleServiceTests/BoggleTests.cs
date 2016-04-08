using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;
using static System.Net.HttpStatusCode;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System;

namespace Boggle
{
    /// <summary>
    /// Provides a way to start and stop the IIS web server from within the test
    /// cases.  If something prevents the test cases from stopping the web server,
    /// subsequent tests may not work properly until the stray process is killed
    /// manually.
    /// </summary>
    public static class IISAgent
    {
        // Reference to the running process
        private static Process process = null;

        /// <summary>
        /// Starts IIS
        /// </summary>
        public static void Start(string arguments)
        {
            if (process == null)
            {
                ProcessStartInfo info = new ProcessStartInfo(Properties.Resources.IIS_EXECUTABLE, arguments);
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = false;
                process = Process.Start(info);
            }
        }

        /// <summary>
        ///  Stops IIS
        /// </summary>
        public static void Stop()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }

    [TestClass]
    public class BoggleTests
    {
        /// <summary>
        /// This is automatically run prior to all the tests to start the server
        /// </summary>
        [ClassInitialize()]
        public static void StartIIS(TestContext testContext)
        {
            IISAgent.Start(@"/site:""BoggleService"" /apppool:""Clr4IntegratedAppPool"" /config:""..\..\..\.vs\config\applicationhost.config""");
        }

        /// <summary>
        /// This is automatically run when all tests have completed to stop the server
        /// </summary>
        [ClassCleanup()]
        public static void StopIIS()
        {
            IISAgent.Stop();
        }

        private RestTestClient client = new RestTestClient("http://localhost:60000/");

        /// <summary>
        /// Has a null Nickname
        /// </summary>
        [TestMethod]
        public void CreateUserTest1()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = null;
            Response r = client.DoPostAsync("users", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has an empty Nickname
        /// </summary>
        [TestMethod]
        public void CreateUserTest2()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "";
            Response r = client.DoPostAsync("/users", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has a Nickname that will trim to empty
        /// </summary>
        [TestMethod]
        public void CreateUserTest3()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = " ";
            Response r = client.DoPostAsync("/users", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has a Nickname that will trim to empty
        /// </summary>
        [TestMethod]
        public void CreateUserTest4()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "    ";
            Response r = client.DoPostAsync("/users", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has a valid name
        /// </summary>
        [TestMethod]
        public void CreateUserTest5()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            Response r = client.DoPostAsync("/users", d).Result;
            Assert.AreEqual(Created, r.Status);
            Assert.IsNotNull(r.Data.UserToken);
        }

        /// <summary>
        /// Asserts that generated UserTokens are unique
        /// </summary>
        [TestMethod]
        public void CreateUserTest6()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            Response r1 = client.DoPostAsync("/users", d).Result;
            Assert.AreEqual(Created, r1.Status);
            Assert.IsNotNull(r1.Data.UserToken);
            
            Response r2 = client.DoPostAsync("/users", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.UserToken);

            Assert.AreNotEqual(r1.Data.UserToken, r2.Data.UserToken);
        }

        /// <summary>
        /// Asserts that generated UserTokens are unique
        /// </summary>
        [TestMethod]
        public void CreateUserTest7()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            HashSet<string> userTokens = new HashSet<string>();

            for (int j = 1; j <= 100; j++)
            {
                Response r = client.DoPostAsync("/users", d).Result;
                Assert.AreEqual(Created, r.Status);
                string token = r.Data.UserToken;
                Assert.IsNotNull(token);
                // Fails to add the token to the collection if it is a duplicate
                Assert.IsTrue(userTokens.Add(token));
            }
        }

        /// <summary>
        /// Has a null UserToken
        /// </summary>
        [TestMethod]
        public void JoinGameTest1()
        {
            dynamic d = new ExpandoObject();
            d.UserToken = "";
            d.TimeLimit = 60;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has an invalid UserToken
        /// </summary>
        [TestMethod]
        public void JoinGameTest2()
        {
            dynamic d = new ExpandoObject();
            d.UserToken = "";
            d.TimeLimit = 60;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has an invalid UserToken
        /// </summary>
        [TestMethod]
        public void JoinGameTest3()
        {
            dynamic d = new ExpandoObject();
            d.UserToken = "asdf";
            d.TimeLimit = 60;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has a null TimeLimit
        /// </summary>
        [TestMethod]
        public void JoinGameTest4()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has a negative TimeLimit
        /// </summary>
        [TestMethod]
        public void JoinGameTest5()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = -1;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has too small of a TimeLimit
        /// </summary>
        [TestMethod]
        public void JoinGameTest6()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 4;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has too large of a TimeLimit
        /// </summary>
        [TestMethod]
        public void JoinGameTest7()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 121;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Tries to add the same user to a game twice.
        /// Also tests the boundary case of TimeLimit = 5
        /// </summary>
        [TestMethod]
        public void JoinGameTest8()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r.Status);
            Assert.IsNotNull(r.Data.GameID);

            r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Conflict, r.Status);

            // Remove the first user from the pending game
            r = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(OK, r.Status);
        }

        /// <summary>
        /// Tries to add the same user to a game twice.
        /// Also tests the boundary case of TimeLimit = 120
        /// </summary>
        [TestMethod]
        public void JoinGameTest9()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 120;
            Response r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r.Status);
            Assert.IsNotNull(r.Data.GameID);

            r = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Conflict, r.Status);

            // Remove the first user from the pending game
            r = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(OK, r.Status);
        }

        /// <summary>
        /// Adds two different users to a game.
        /// </summary>
        [TestMethod]
        public void JoinGameTest10()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);
        }

        /// <summary>
        /// Has a null UserToken
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestTest1()
        {
            dynamic d = new ExpandoObject();
            d.UserToken = null;
            Response r = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has an invalid UserToken
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestTest2()
        {
            dynamic d = new ExpandoObject();
            d.UserToken = "";
            Response r = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Has an invalid UserToken
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestTest3()
        {
            dynamic d = new ExpandoObject();
            d.UserToken = "asdf";
            Response r = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// UserToken not in pending game
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestTest4()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// UserToken not in pending game (game already active)
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestTest5()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            Response r3 = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(Forbidden, r3.Status);
        }

        /// <summary>
        /// Removes UserToken from pending game
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestTest6()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            Response r3 = client.DoPutAsync(d, "/games").Result;
            Assert.AreEqual(OK, r3.Status);
        }

        /// <summary>
        /// Missing GameID
        /// </summary>
        [TestMethod]
        public void PlayWordTest1()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.UserToken = null;
            d.Word = null;
            Response r = client.DoPutAsync(d, "/games/" + r1.Data.GameID).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Invalid GameID
        /// </summary>
        [TestMethod]
        public void PlayWordTest2()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.UserToken = null;
            d.Word = null;
            Response r = client.DoPutAsync(d, "/games/asdf").Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Null UserToken
        /// </summary>
        [TestMethod]
        public void PlayWordTest3()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.UserToken = null;
            d.Word = null;
            Response r = client.DoPutAsync(d, "/games/" + r1.Data.GameID).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Invalid UserToken
        /// </summary>
        [TestMethod]
        public void PlayWordTest4()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.UserToken = "";
            d.Word = null;
            Response r = client.DoPutAsync(d, "/games/" + r1.Data.GameID).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Invalid UserToken
        /// </summary>
        [TestMethod]
        public void PlayWordTest5()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.UserToken = "asdf";
            d.Word = null;
            Response r = client.DoPutAsync(d, "/games/" + r1.Data.GameID).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Invalid UserToken (user not in game)
        /// </summary>
        [TestMethod]
        public void PlayWordTest6()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.Word = null;
            Response r = client.DoPutAsync(d, "/games/" + r1.Data.GameID).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Game still pending
        /// </summary>
        [TestMethod]
        public void PlayWordTest7()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);
            
            d.Word = "word";
            string gameID = r1.Data.GameID;
            Response r = client.DoPutAsync(d, "/games/" + gameID).Result;
            Assert.AreEqual(Conflict, r.Status);
        }

        /// <summary>
        /// Game already completed
        /// </summary>
        [TestMethod]
        public void PlayWordTest8()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.IsNotNull(r1.Data.GameID);

            dynamic d2 = new ExpandoObject();
            d2.Nickname = "Name";
            d2.UserToken = client.DoPostAsync("/users", d2).Result.Data.UserToken;
            d2.TimeLimit = 5;
            r1 = client.DoPostAsync("/games", d2).Result;
            Assert.IsNotNull(r1.Data.GameID);

            Response res = client.DoGetAsync("/games/" + r1.Data.GameID).Result;
            while (res.Data.GameState == "active")
            {
                System.Threading.Thread.Sleep(1000);
                res = client.DoGetAsync("/games/" + r1.Data.GameID).Result;
            }
            string gameStatus = res.Data.GameState;

            d.Word = "word";
            string gameID = r1.Data.GameID;
            Response r = client.DoPutAsync(d, "/games/" + gameID).Result;
            Assert.AreEqual(Conflict, r.Status);
        }

        /// <summary>
        /// Word is null
        /// </summary>
        [TestMethod]
        public void PlayWordTest9()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.Word = null;
            Response r = client.DoPutAsync(d, "/games/" + r1.Data.GameID).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Word is empty
        /// </summary>
        [TestMethod]
        public void PlayWordTest10()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.Word = "";
            Response r = client.DoPutAsync(d, "/games/" + r1.Data.GameID).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Duplicate words (both invalid)
        /// </summary>
        [TestMethod]
        public void PlayWordTest11()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 60;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.Word = "word";
            string gameID = r1.Data.GameID;
            Response r = client.DoPutAsync(d, "/games/" + gameID).Result;
            Assert.AreEqual(OK, r.Status);
            Assert.AreEqual(-1, (int)r.Data.Score);

            Assert.AreEqual(OK, r.Status);
            r = client.DoPutAsync(d, "/games/" + gameID).Result;
            Assert.AreEqual(OK, r.Status);
            Assert.AreEqual(-1, (int)r.Data.Score);
        }

        /// <summary>
        /// Duplicate words (both invalid, different cases)
        /// </summary>
        [TestMethod]
        public void PlayWordTest12()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 5;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            d.Word = "word";
            string gameID = r1.Data.GameID;
            Response r = client.DoPutAsync(d, "/games/" + gameID).Result;
            Assert.AreEqual(OK, r.Status);
            d.Word = "WORD";
            r = client.DoPutAsync(d, "/games/" + gameID).Result;
            Assert.AreEqual(OK, r.Status);
            Assert.AreEqual(-1, (int)r.Data.Score);
        }

        /// <summary>
        /// testing the score of all playable words.
        /// </summary>
        [TestMethod]
        public void PlayWordTest13()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 60;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);
            string UserToken1 = d.UserToken;

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);
            string UserToken2 = d.UserToken;

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            Response res = client.DoGetAsync("/games/{0}", (string)r1.Data.GameID, "no").Result;
            Assert.AreEqual(OK, res.Status);

            

            string Board = res.Data.Board;

            BoggleBoard board = new BoggleBoard((string)res.Data.Board);
            List<string> twoOrLessLetter = new List<string>();
            List<string> threeOrFourLetter = new List<string>();
            List<string> fiveLetter = new List<string>();
            List<string> sixLetter = new List<string>();
            List<string> sevenLetter = new List<string>();
            List<string> longerLetter = new List<string>();

            foreach (string line in File.ReadLines(AppDomain.CurrentDomain.BaseDirectory + "/dictionary.txt"))
            {
                if (board.CanBeFormed(line.Trim()))
                {
                    var word = line.Trim();
                    if(word.Length < 3)
                    {
                        twoOrLessLetter.Add(word);
                    }
                    else if (word.Length == 3 || word.Length == 4)
                    {
                        threeOrFourLetter.Add(word);
                    }
                    else if (word.Length == 5)
                    {
                        fiveLetter.Add(word);
                    }
                    else if (word.Length == 6)
                    {
                        sixLetter.Add(word);
                    }
                    else if(word.Length == 7)
                    {
                        sevenLetter.Add(word);
                    }
                    else
                    {
                        longerLetter.Add(word);
                    }
                }
            }
            string gameID = r1.Data.GameID;
            foreach (string currWord in threeOrFourLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken1;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("1", (string)r3.Data.Score);
            }
            foreach (string currWord in fiveLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken2;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("2", (string)r3.Data.Score);
            }
            foreach (string currWord in sixLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken1;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("3", (string)r3.Data.Score);
            }
            foreach (string currWord in sevenLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken1;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("5", (string)r3.Data.Score);
            }
            foreach (string currWord in longerLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken1;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("11", (string)r3.Data.Score);
            }
            
            foreach (string currWord in threeOrFourLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken2;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("0", (string)r3.Data.Score);
            }
            foreach (string currWord in fiveLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken1;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("0", (string)r3.Data.Score);
            }
            foreach (string currWord in sixLetter)
            {
                dynamic d2 = new ExpandoObject();
                d2.UserToken = UserToken2;
                d2.Word = currWord;
                Response r3 = client.DoPutAsync(d2, "/games/" + gameID).Result;
                Assert.AreEqual("0", (string)r3.Data.Score);
            }

            dynamic d3 = new ExpandoObject();
            d3.UserToken = UserToken2;
            d3.Word = "kdljflskjdflskj";
            Response r4 = client.DoPutAsync(d3, "/games/" + gameID).Result;
            Assert.AreEqual("-1", (string)r4.Data.Score);

            d3.UserToken = UserToken1;
            d3.Word = "kdljflj";
            Response r5 = client.DoPutAsync(d3, "/games/" + gameID).Result;
            Assert.AreEqual("-1", (string)r5.Data.Score);

        }


        /// <summary>
        /// testing playing a word after time has run out, even if you don't ever request the game status.
        /// </summary>
        [TestMethod]
        public void PlayWordTest14()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 10;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);
            
            System.Threading.Thread.Sleep(15000);

            d.Word = "word";
            string gameID = r1.Data.GameID;
            Response r = client.DoPutAsync(d, "/games/" + gameID).Result;
            Assert.AreEqual(Conflict, r.Status);

        }
        
        /// <summary>
        /// first gamestatus test. Will test pending game state, and also invalid gameID
        /// </summary>
        [TestMethod]
        public void GameStatusTest1()
        {
            //Add a user, and start a pending game.
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 60;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            Response res = client.DoGetAsync("/games/" + r1.Data.GameID).Result;
            Assert.AreEqual("pending", (string)res.Data.GameState);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            res = client.DoGetAsync("/games/" + r1.Data.GameID).Result;
            Assert.AreEqual("active", (string)res.Data.GameState);

            res = client.DoGetAsync("/games/" + "324").Result;
            Assert.AreEqual(Forbidden, res.Status);
        }

        /// <summary>
        /// Testing the brief = 
        /// </summary>
        [TestMethod]
        public void GameStatusTest2()
        {
            //Add a user, and start a pending game.
            dynamic d = new ExpandoObject();
            d.Nickname = "Name";
            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            d.TimeLimit = 6;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            Response res = client.DoGetAsync("/games/" + r1.Data.GameID).Result;
            Assert.AreEqual("pending", (string)res.Data.GameState);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);
            string url = "/games/" + (string)r2.Data.GameID + @"?Brief=yes";
            Response r3 = client.DoGetAsync(url).Result;
            Assert.AreEqual(null, res.Data.Board);

            System.Threading.Thread.Sleep(7000);
            r3 = client.DoGetAsync(url).Result;
            Assert.AreEqual(null, r3.Data.Board);
            Assert.AreEqual("completed", (string)r3.Data.GameState);

            r3 = client.DoGetAsync(url).Result;
            Assert.AreEqual(null, res.Data.Board);
            Assert.AreEqual("completed", (string)r3.Data.GameState);

            string url2 = "/games/" + (string)r2.Data.GameID;
            r3 = client.DoGetAsync(url2).Result;
            Assert.AreEqual("completed", (string)r3.Data.GameState);
        }
    }
}
