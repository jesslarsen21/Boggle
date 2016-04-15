using CustomNetworking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Boggle
{
    public class WebServer
    {
        public static void Main()
        {
            new WebServer();
            Console.Read();
        }

        private TcpListener server;
        private BoggleService boggleServer;

        public WebServer()
        {
            server = new TcpListener(IPAddress.Any, 60000);
            server.Start();
            boggleServer = new BoggleService();
            server.BeginAcceptSocket(ConnectionRequested, null);
        }

        private void ConnectionRequested(IAsyncResult ar)
        {
            Socket s = server.EndAcceptSocket(ar);
            server.BeginAcceptSocket(ConnectionRequested, null);
            new HttpRequest(new StringSocket(s, new UTF8Encoding()), boggleServer);
        }
    }

    class HttpRequest
    {
        private StringSocket ss;
        private int methodNumber;
        private string gameID;
        private string brief;
        private BoggleService boggleServer;
        private int lineCount;
        private int contentLength;
        private static readonly object sync = new object();

        public HttpRequest(StringSocket stringSocket, BoggleService boggleServer)
        {
            ss = stringSocket;
            this.boggleServer = boggleServer;
            ss.BeginReceive(LineReceived, null);
        }

        private void LineReceived(string s, Exception e, object payload)
        {
            lineCount++;
            Console.WriteLine(s);
            if (s != null)
            {
                if (lineCount == 1)
                {
                    Regex r = new Regex(@"^(\S+)\s+(\S+)");
                    Match m = r.Match(s);
                    string method = m.Groups[1].Value;
                    string url = m.Groups[2].Value;
                    url.ToLower();

                    if (method == "POST" && url.EndsWith("/users"))
                    {
                        methodNumber = 0;
                    }
                    else if (method == "POST" && url.EndsWith("/games"))
                    {
                        methodNumber = 1;
                    }
                    else if (method == "PUT" && url.EndsWith("/games"))
                    {
                        methodNumber = 2;
                    }
                    else if (method == "PUT" && url.Contains("/games") && !url.EndsWith("/games"))
                    {
                        methodNumber = 3;
                        gameID = url.Substring(25);
                    }
                    else if (method == "GET" && url.Contains("/games") && !url.EndsWith("/games"))
                    {
                        methodNumber = 4;
                        if (url.Contains("brief") || url.Contains("Brief"))
                        {
                            string[] words = url.Split('?');
                            gameID = words[0].Substring(25);
                            brief = words[1].Substring(6);
                        }
                        else
                        {
                            gameID = url.Substring(25);
                        }
                    }
                    else
                    {
                        ss.BeginSend("HTTP/1.1 400 Bad Request \r\n", Ignore, null);
                    }
                }
                if (s.StartsWith("Content-Length:"))
                {
                    contentLength = int.Parse(s.Substring(16).Trim());
                }
                if (s == "\r")
                {
                    if (methodNumber == 4)
                    {
                        ContentReceived(s, null, null);
                    }
                    else
                    {
                        ss.BeginReceive(ContentReceived, null, contentLength);
                    }
                }
                else
                {
                    ss.BeginReceive(LineReceived, null);
                }
            }
        }

        private void ContentReceived(string s, Exception e, object payload)
        {
            lock (sync)
            {
                if (s != null)
                {
                    dynamic obj = JsonConvert.DeserializeObject(s);

                    switch (methodNumber)
                    {
                        // CreateUser
                        case 0:
                            try
                            {
                                CreateUserInfo createUser = new CreateUserInfo();
                                createUser.Nickname = obj.Nickname;
                                CreateUserReturn out0 = boggleServer.CreateUser(createUser);
                                string result0 = JsonConvert.SerializeObject(out0);
                                ss.BeginSend(GetHttpCode(out0.Status), Ignore, null);
                                ss.BeginSend("Content-Type: application/json \r\n", Ignore, null);
                                ss.BeginSend("Content-Length: " + result0.Length + " \r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend(result0, (ex, py) => { ss.Shutdown(); }, null);
                            }
                            catch (Exception)
                            {
                                ss.BeginSend("HTTP/1.1 403 Forbidden \r\n", Ignore, null);
                                ss.BeginSend("Content-Type: application/json \r\n", Ignore, null);
                                ss.BeginSend("Content-Length: 0\r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                            }
                            break;
                        // JoinGame
                        case 1:

                            try
                            {
                                JoinGameInfo joinGame = new JoinGameInfo();
                                joinGame.TimeLimit = obj.TimeLimit;
                                joinGame.UserToken = obj.UserToken;
                                JoinGameReturn out1 = boggleServer.JoinGame(joinGame);
                                if (out1.Status == HttpStatusCode.Conflict)
                                {
                                    ss.BeginSend("HTTP/1.1 409 Conflict\r\n", Ignore, null);
                                    ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                    ss.BeginSend("Content-Length: 0\r\n", Ignore, null);
                                    ss.BeginSend("\r\n", Ignore, null);
                                    ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                                }
                                else
                                {
                                    string result1 = JsonConvert.SerializeObject(out1);
                                    ss.BeginSend(GetHttpCode(out1.Status), Ignore, null);
                                    ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                    ss.BeginSend("Content-Length: " + result1.Length + "\r\n", Ignore, null);
                                    ss.BeginSend("\r\n", Ignore, null);
                                    ss.BeginSend(result1, (ex, py) => { ss.Shutdown(); }, null);
                                }
                            }
                            catch (Exception)
                            {
                                ss.BeginSend("HTTP/1.1 403 Forbidden\r\n", Ignore, null);
                                ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                ss.BeginSend("Content-Length: 0\r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                            }
                            break;
                        // CancelJoinRequest
                        case 2:
                            try
                            {
                                CancelJoinRequestInfo cancelGame = new CancelJoinRequestInfo();
                                cancelGame.UserToken = obj.UserToken;
                                CancelJoinRequestReturn out2 = boggleServer.CancelJoinRequest(cancelGame);
                                ss.BeginSend(GetHttpCode(out2.Status), Ignore, null);
                                ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                            }
                            catch (Exception)
                            {
                                ss.BeginSend("HTTP/1.1 403 Forbidden\r\n", Ignore, null);
                                ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                ss.BeginSend("Content-Length: 0\r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                            }
                            break;
                        // PlayWord
                        case 3:
                            try
                            {
                                PlayWordInput playWord = new PlayWordInput();
                                playWord.UserToken = obj.UserToken;
                                playWord.Word = obj.Word;
                                PlayWordReturn out3 = boggleServer.PlayWord(gameID, playWord);
                                if (out3.Status == HttpStatusCode.Conflict)
                                {
                                    ss.BeginSend("HTTP/1.1 409 Conflict\r\n", Ignore, null);
                                    ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                    ss.BeginSend("Content-Length: 0\r\n", Ignore, null);
                                    ss.BeginSend("\r\n", Ignore, null);
                                    ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                                }
                                else
                                {
                                    string result3 = JsonConvert.SerializeObject(out3);
                                    ss.BeginSend(GetHttpCode(out3.Status), Ignore, null);
                                    ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                    ss.BeginSend("Content-Length: " + result3.Length + "\r\n", Ignore, null);
                                    ss.BeginSend("\r\n", Ignore, null);
                                    ss.BeginSend(result3, (ex, py) => { ss.Shutdown(); }, null);
                                }
                            }
                            catch (Exception)
                            {
                                ss.BeginSend("HTTP/1.1 403 Forbidden\r\n", Ignore, null);
                                ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                ss.BeginSend("Content-Length: 0\r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                            }
                            break;
                        // GameStatus
                        case 4:
                            try
                            {
                                Game out4 = boggleServer.GameStatus(gameID, brief);
                                string result4 = JsonConvert.SerializeObject(out4);
                                ss.BeginSend(GetHttpCode(out4.Status), Ignore, null);
                                ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                ss.BeginSend("Content-Length: " + result4.Length + "\r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend(result4, (ex, py) => { ss.Shutdown(); }, null);
                            }
                            catch (Exception)
                            {
                                ss.BeginSend("HTTP/1.1 403 Forbidden\r\n", Ignore, null);
                                ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
                                ss.BeginSend("Content-Length: 0\r\n", Ignore, null);
                                ss.BeginSend("\r\n", Ignore, null);
                                ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                            }
                            break;
                        default:
                            ss.BeginSend("HTTP/1.1 400 Bad Request\r\n", Ignore, null);
                            ss.BeginSend("\r\n", Ignore, null);
                            ss.BeginSend("", (ex, py) => { ss.Shutdown(); }, null);
                            break;
                    }
                    /*string result =
                        JsonConvert.SerializeObject(
                                new Person { Name = "June", Eyes = "Blue" },
                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    ss.BeginSend("HTTP/1.1 200 OK\n", Ignore, null);
                    ss.BeginSend("Content-Type: application/json\n", Ignore, null);
                    ss.BeginSend("Content-Length: " + result.Length + "\n", Ignore, null);
                    ss.BeginSend("\r\n", Ignore, null);
                    ss.BeginSend(result, (ex, py) => { ss.Shutdown(); }, null);*/
                }
            }
        }

        private string GetHttpCode(HttpStatusCode status)
        {
            if (status == HttpStatusCode.OK)
            {
                return "HTTP/1.1 200 OK\r\n";
            }
            if (status == HttpStatusCode.Created)
            {
                return "HTTP/1.1 201 Created\r\n";
            }
            if (status == HttpStatusCode.Accepted)
            {
                return "HTTP/1.1 202 Accepted\r\n";
            }
            if (status == HttpStatusCode.Conflict)
            {
                return "HTTP/1.1 409 Conflict\r\n";
            }
            else
            {
                return "HTTP/1.1 403 Forbidden\r\n";
            }
        }

        private void Ignore(Exception e, object payload)
        {
        }
    }
}