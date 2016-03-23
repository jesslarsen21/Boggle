﻿using System;
using System.Net.Http;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text;
using System.Net;
using System.Collections.Generic;

namespace BoggleClient
{
    class Controller
    {
        private IBoggleView window;
        private static string serverDomain;
        private static string userToken;
        private static string gameID;
        private static string gameState;
        private static string board;
        private static int timeLeft;
        private static dynamic player1;
        private static dynamic player2;

        public Controller(IBoggleView window)
        {
            this.window = window;
            serverDomain = "";
            userToken = "";
            gameID = "";
            gameState = "";
            board = "";
            timeLeft = 0;
            player1 = null;
            player2 = null;
            window.NewGameEvent += Window_NewGameEvent;
            window.EnterWordEvent += Window_EnterWordEvent;
            window.QuitGameEvent += Window_QuitGameEvent;
        }

        /// <summary>
        /// Creates an HttpClient for communicating with the Boggle server.
        /// The API requires specific information in each request header.
        /// </summary>
        /// <returns></returns>
        public static HttpClient CreateClient()
        {
            // Create a client whose base address is the Boggle server
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(serverDomain);

            // Tell the server that the client will accept the response data
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            return client;
        }

        /// <summary>
        /// Creates a new game with the given server domain, player nickname,
        /// and game duration.
        /// </summary>
        private void Window_NewGameEvent(string domain, string nickname, int duration)
        {
            serverDomain = domain;

            // Create a new user, with the given nickname.
            CreateUser(nickname);

            // Attempt to join/create a game with the given user token.
            using (LoadingGame form = new LoadingGame())
            {
                form.InfoText = "User created successfully.\nNow connecting to game...\n"
                    + "To cancel operation, click Abort.\nThe window will close upon completion.";

                // TODO Split up the ShowDialog() and JoinGame() into two threads.
                if (form.ShowDialog() == DialogResult.Abort)
                {
                    // TODO Cancel game creation process.
                }

                JoinGame(form, duration);
            }
        }

        /// <summary>
        /// Private helper method that uses an HttpClient to create a new user
        /// for the Boggle server.
        /// </summary>
        private void CreateUser(string nickname)
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();
                data.Nickname = nickname;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("/BoggleService.svc/users", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    dynamic newUser = JsonConvert.DeserializeObject(result);
                    userToken = newUser.UserToken;
                }
                else
                {
                    string str = "Error creating user: Nickname is null or empty.\n"
                        + "Press OK to enter info again, or press Cancel to cancel game creation.";
                    if (window.Error(str) == DialogResult.OK)
                    {
                        window = new BoggleGUI();
                    }
                }
            }
        }

        /// <summary>
        /// Private helper method that uses an HttpClient to join a pending Boggle game.
        /// </summary>
        private void JoinGame(Form form, int duration)
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();
                data.UserToken = userToken;
                data.TimeLimit = duration;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("/BoggleService.svc/games", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    dynamic newGame = JsonConvert.DeserializeObject(result);
                    gameID = newGame.GameID;
                    form.Close();

                    //TODO Invoke this method once per second in a new thread.
                    UpdateGameStatus();
                }
                else
                {
                    string str = "";
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        str = "Unable to join: Invalid user token or game duration. "
                            + "Game duration must be >= 5 and <= 120.\n"
                            + "Press OK to enter info again, or press Cancel to cancel game creation.";
                    }
                    else
                    {
                        str = "Unable to join: This user token already in pending game.\n"
                            + "Press OK to enter info again, or press Cancel to cancel game creation.";
                    }
                    if (window.Error(str) == DialogResult.OK)
                    {
                        window = new BoggleGUI();
                    }
                }
            }
        }

        /// <summary>
        /// Private helper method that updates the game status, score, etc.
        /// </summary>
        private string UpdateGameStatus()
        {
            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = client.GetAsync("/BoggleService.svc/games/" + gameID).Result;

                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    dynamic game = JsonConvert.DeserializeObject(result);
                    gameState = game.GameState;

                    if (gameState == "pending")
                    {
                        window.Time = "Game pending...";
                    }
                    else if (gameState == "active")
                    {
                        board = game.Board;
                        timeLeft = game.TimeLeft;
                        player1 = game.Player1;
                        player2 = game.Player2;

                        // Display the board
                        LinkedList<string> letters = new LinkedList<string>();
                        for (int j = 0; j < board.Length; j++)
                        {
                            string s = board.Substring(j, 1);
                            if (s == "Q")
                            {
                                s = "QU";
                            }
                            letters.AddLast(s);
                        }
                        window.ShowLetters(letters);

                        // Display the time remaining
                        window.Time = "Time remaining: " + timeLeft;

                        // Display the score
                        window.Score = player1.Nickname + ": " + player1.Score + "  "
                            + player2.NickName + ": " + player2.Score;
                    }
                    else if (gameState == "completed")
                    {
                        player1 = game.Player1;
                        player2 = game.Player2;

                        // Display the final results in a new window
                        using (FinalScore form = new FinalScore())
                        {
                            form.Player1Name = player1.Name + ": " + player1.Score;
                            form.Player2Name = player2.Name + ": " + player2.Score;

                            string player1Words = "";
                            foreach (dynamic wordPlayed in player1.WordsPlayed)
                            {
                                player1Words += wordPlayed.Word + ": " + wordPlayed.Score + "\n";
                            }
                            string player2Words = "";
                            foreach (dynamic wordPlayed in player2.WordsPlayed)
                            {
                                player2Words += wordPlayed.Word + ": " + wordPlayed.Score + "\n";
                            }

                            form.Player1Words = player1Words;
                            form.Player2Words = player2Words;

                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                window = new BoggleGUI();
                            }
                            else
                            {
                                window.DoClose();
                            }
                        }
                    }
                }
                else
                {
                    string str = "Unable to fetch game status: Invalid game ID.\n"
                        + "Press OK to enter info again, or press Cancel to cancel game creation.";
                    if (window.Error(str) == DialogResult.OK)
                    {
                        window = new BoggleGUI();
                    }
                }
            }
            return gameState;
        }

        /// <summary>
        /// Handles when the player enters a word, by sending it to the server and
        /// updating the player's score.
        /// </summary>
        private void Window_EnterWordEvent(string word)
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();
                data.UserToken = userToken;
                data.Word = word;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PutAsync("/BoggleService.svc/games/" + gameID, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    UpdateGameStatus();
                }
                else
                {
                    UpdateGameStatus();
                    //string str = "Unable to play word: Invalid game ID, user token, or empty word.\n"
                    //    + "If problem persists, exit game and start over.";
                    //window.Message = str;
                }
            }
        }

        /// <summary>
        /// Handles when the player quits a running game (or cancels a loading game).
        /// </summary>
        private void Window_QuitGameEvent()
        {
            using (HttpClient client = CreateClient())
            {
                if (gameState == "pending")
                {
                    dynamic d = new ExpandoObject();
                    d.UserToken = userToken;

                    StringContent c = new StringContent(JsonConvert.SerializeObject(d), Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = client.PutAsync("/BoggleService.svc/games", c).Result;

                    if (resp.IsSuccessStatusCode)
                    {
                        window.Message = "Successfully quit pending game.";
                    }
                    else
                    {
                        window.Message = "Unable to abort game connection.\nUser token is invalid, "
                            + "or player is not in a pending game.";
                    }
                }
                else if (gameState == "active")
                {
                    window = new BoggleGUI();
                }
            }
        }
    }
}
