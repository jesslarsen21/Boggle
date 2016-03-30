using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;
using static System.Net.HttpStatusCode;
using System.Diagnostics;
using System.Collections.Generic;

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

        /*[TestMethod]
        public void TestMethod1()
        {
            Response r = client.DoGetAsync("/numbers?length={0}", "5").Result;
            Assert.AreEqual(OK, r.Status);
            Assert.AreEqual(5, r.Data.Count);
            r = client.DoGetAsync("/numbers?length={0}", "-5").Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestMethod2()
        {
            List<int> list = new List<int>();
            list.Add(15);
            Response r = client.DoPostAsync("/first", list).Result;
            Assert.AreEqual(OK, r.Status);
            Assert.AreEqual(15, r.Data);
        }*/

        /// <summary>
        /// Has a null Nickname
        /// </summary>
        [TestMethod]
        public void CreateUserTest1()
        {
            dynamic d = new ExpandoObject();
            d.Nickname = null;
            Response r = client.DoPostAsync("/users", d).Result;
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
                Assert.IsNotNull(r.Data.UserToken);
                // Fails to add the token to the collection if it is a duplicate
                Assert.IsFalse(userTokens.Add(r.Data.UserToken));
            }
        }

        /// <summary>
        /// Has a null UserToken
        /// </summary>
        [TestMethod]
        public void JoinGameTest1()
        {
            dynamic d = new ExpandoObject();
            d.UserToken = null;
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
            d.TimeLimit = null;
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
            d.TimeLimit = 60;
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
            Response r = client.DoPutAsync("/games", d).Result;
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
            Response r = client.DoPutAsync("/games", d).Result;
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
            Response r = client.DoPutAsync("/games", d).Result;
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
            Response r = client.DoPutAsync("/games", d).Result;
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
            d.TimeLimit = 60;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            Response r3 = client.DoPutAsync("/games", d).Result;
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
            d.TimeLimit = 60;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            Response r3 = client.DoPutAsync("/games", d).Result;
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
            d.TimeLimit = 60;
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
            Response r = client.DoPutAsync("/games/", d).Result;
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
            d.TimeLimit = 60;
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
            Response r = client.DoPutAsync("/games/asdf", d).Result;
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
            d.TimeLimit = 60;
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
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
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
            d.TimeLimit = 60;
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
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
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
            d.TimeLimit = 60;
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
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
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
            d.TimeLimit = 60;
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
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
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
            d.TimeLimit = 60;
            Response r1 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);
            
            d.Word = null;
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
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
            Assert.AreEqual(Accepted, r1.Status);
            Assert.IsNotNull(r1.Data.GameID);

            d.UserToken = client.DoPostAsync("/users", d).Result.Data.UserToken;
            Response r2 = client.DoPostAsync("/games", d).Result;
            Assert.AreEqual(Created, r2.Status);
            Assert.IsNotNull(r2.Data.GameID);

            Assert.AreEqual(r1.Data.GameID, r2.Data.GameID);

            System.Threading.Thread.Sleep(5000);
            d.Word = null;
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
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
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
            Assert.AreEqual(OK, r.Status);
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
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
            Assert.AreEqual(OK, r.Status);
        }

        /// <summary>
        /// Duplicate words (second score = 0)
        /// </summary>
        [TestMethod]
        public void PlayWordTest11()
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

            d.Word = "a";
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
            Assert.AreEqual(OK, r.Status);
            r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
            Assert.AreEqual(OK, r.Status);
            Assert.AreEqual(0, r.Data.Score);
        }

        /// <summary>
        /// Duplicate words (second score = 0)
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

            d.Word = "a";
            Response r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
            Assert.AreEqual(OK, r.Status);
            d.Word = "A";
            r = client.DoPutAsync("/games/" + r1.Data.GameID, d).Result;
            Assert.AreEqual(OK, r.Status);
            Assert.AreEqual(0, r.Data.Score);
        }
    }
}
