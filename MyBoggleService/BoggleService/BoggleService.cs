using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        /// TcpListener
        /// - Create a TcpListener object to listen on a given port
        ///   TcpListener (IPAddress, Port)
        /// - When an incoming connection is requested, invoke a callback method to initialize the connection
        ///   IAsyncResult BeginAcceptSocket (Callback, Parameter)
        /// - After the socket has been accepted, obtain the socket to secure the connection
        ///   Socket EndAcceptSocket (IAsyncResult)
        ///   
        /// Socket
        /// - Invoke a callback when bytes arrive
        ///   IAsyncResult BeginReceive (… byte[] … Callback …)
        /// - Return number of bytes received
        ///   int EndReceive (IAsyncResult)
        /// - Invoke a callback when bytes are sent
        ///   IAsyncResult BeginSend (… byte[] … Callback …)
        /// - Return number of bytes sent
        ///   int EndSend (IAsyncResult)
        ///   
        /// StringSocket (wrapper for a completed Socket)
        /// - Construct a StringSocket using a given encoding for the I/O byte stream
        ///   StringSocket (Socket s, Encoding e)
        /// - Sends the string and invokes the callback upon completion
        ///   void BeginSend (String s, SendCallback callback, object payload)
        ///   delegate void SendCallback (Exception e, object payload)
        /// - Reads a complete line of text (if length is less than 0) and invokes the callback upon completion
        ///   void BeginReceive (ReceiveCallback callback, object payload, int length = 0)
        ///   delegate void ReceiveCallback (String s, Exception e, object payload)
        /// - Shuts down and closes the StringSocket
        ///   void Shutdown ()
        ///   
        /// General Requirements
        /// - Can deal simultaneously with an arbitrary number of clients
        /// - Receives from and sends to client simultaneously
        /// - Each step of the outgoing process must be synchronized to prevent errors
        /// 
        /// Starting Server
        /// - Create TcpListener object to listen on port 60000
        /// 
        /// Accepting a connection request
        /// - Socket s = TcpListener.EndAcceptSocket(TcpListener.BeginAcceptSocket(null, null));
        /// - StringSocket socket = new StringSocket (s, Encoding e); // Not sure what to do for the Encoding yet... JSON?
        /// 
        /// Accepting client requests
        /// - Use a chooser method to determine which server method to call?
        ///

        private static readonly string BoggleDB = ConfigurationManager.ConnectionStrings["BoggleDB"].ConnectionString;
        private static int pendingGameID = -1;
        private static readonly object sync = new object();

        /// <summary>
        /// The most recent call to SetStatus determines the response code used when
        /// an http response is sent.
        /// </summary>
        private static void SetStatus(HttpStatusCode status)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        }

        /// <summary>
        /// Returns a Stream version of index.html.
        /// </summary>
        public Stream API()
        {
            SetStatus(OK);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        }

        /// <summary>
        /// Create a new user.
        /// 
        /// If Nickname is null, or is empty when trimmed, responds with
        /// status 403 (Forbidden).
        /// 
        /// Otherwise, creates a new user with a unique UserToken and the
        /// trimmed Nickname. The returned UserToken should be used to identify
        /// the user in subsequent requests. Responds with status 201 (Created).
        /// </summary>
        public CreateUserReturn CreateUser(CreateUserInfo user)
        {
            lock (sync)
            {
                if (user == null || user.Nickname == null || user.Nickname.Trim().Length == 0)
                {
                    return null;
                }
                else
                {
                    string Token = Guid.NewGuid().ToString();
                    CreateUserReturn newUser = new CreateUserReturn();
                    newUser.UserToken = Token;

                    using (SqlConnection conn = new SqlConnection(BoggleDB))
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            using (SqlCommand command = new SqlCommand(
                                "INSERT INTO Users(UserToken, Nickname, Score) VALUES (@UserToken, @Nickname, 0)", conn, trans))
                            {
                                command.Parameters.AddWithValue("@UserToken", Token);
                                command.Parameters.AddWithValue("@Nickname", user.Nickname);

                                try
                                {
                                    command.ExecuteNonQuery();

                                    newUser.Status = Created;
                                    trans.Commit();
                                }
                                catch (Exception)
                                {
                                    return null;
                                }
                            }
                        }
                        conn.Close();
                    }
                    return newUser;
                }
            }
        }

        /// <summary>
        /// Join a game.
        /// 
        /// If UserToken is invalid, TimeLimit< 5, or TimeLimit> 120,
        /// responds with status 403 (Forbidden).
        /// 
        /// Otherwise, if UserToken is already a player in the pending game,
        /// responds with status 409 (Conflict).
        /// 
        /// Otherwise, if there is already one player in the pending game,
        /// adds UserToken as the second player. The pending game becomes active
        /// and a new pending game with no players is created. The active game's
        /// time limit is the integer average of the time limits requested
        /// by the two players. Returns the new active game's GameID
        /// (which should be the same as the old pending game's GameID).
        /// Responds with status 201 (Created).
        /// 
        /// Otherwise, adds UserToken as the first player of the pending game,
        /// and the TimeLimit as the pending game's requested time limit.
        /// Returns the pending game's GameID. Responds with status 202 (Accepted).
        /// </summary>
        public JoinGameReturn JoinGame(JoinGameInfo info)
        {
            lock (sync)
            {
                // No need to check for a valid UserToken here,
                // because the database will handle that through exceptions
                User user = new User();
                if (info.TimeLimit >= 5 && info.TimeLimit <= 120)
                {
                    JoinGameReturn output = new JoinGameReturn();
                    using (SqlConnection conn = new SqlConnection(BoggleDB))
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            // If this is the first player in the pending game
                            if (pendingGameID == -1)
                            {
                                // Add the first user and set the time limit for the pending game
                                user.Score = 0;
                                user.WordsPlayed = new List<Words>();
                                // Create the pending game
                                using (SqlCommand command = new SqlCommand(
                                    "INSERT INTO Games(Player1, TimeLimit, GameState) output inserted.GameID VALUES (@UserToken, @TimeLimit, 0)", conn, trans))
                                {
                                    command.Parameters.AddWithValue("@UserToken", info.UserToken);
                                    command.Parameters.AddWithValue("@TimeLimit", info.TimeLimit);

                                    try
                                    {
                                        // Executes the command and returns the number of rows affected
                                        pendingGameID = (int)command.ExecuteScalar();
                                    }
                                    catch (Exception)
                                    {
                                        return null;
                                    }
                                }
                                // Return info to user and commit database changes
                                output.GameID = pendingGameID.ToString();
                                output.Status = Accepted;
                                trans.Commit();
                            }
                            // If there is already one player in the pending game
                            else
                            {
                                int oldTimeLimit = 0;
                                // Fetch Player1's UserToken to verify this isn't a duplicate user
                                using (SqlCommand command = new SqlCommand(
                                    "SELECT Player1,TimeLimit FROM Games WHERE GameID=@GameID", conn, trans))
                                {
                                    command.Parameters.AddWithValue("@GameID", pendingGameID);

                                    try
                                    {
                                        // Executes the command and returns an SqlDataReader for reading more than one item of output
                                        SqlDataReader reader = command.ExecuteReader();
                                        reader.Read();
                                        if (reader.GetString(0) == info.UserToken)
                                        {
                                            output.Status = Conflict;
                                            reader.Close();
                                            return output;
                                        }
                                        oldTimeLimit = reader.GetInt32(1);
                                        reader.Close();
                                    }
                                    catch (Exception)
                                    {
                                        return null;
                                    }
                                }
                                // Convert the pending game to an active one
                                using (SqlCommand command = new SqlCommand(
                                    "UPDATE Games SET Player2=@UserToken,Board=@Board,TimeLimit=@TimeLimit,StartTime=@StartTime,GameState=1 WHERE GameID=@GameID", conn, trans))
                                {
                                    command.Parameters.AddWithValue("@UserToken", info.UserToken);
                                    command.Parameters.AddWithValue("@Board", (new BoggleBoard()).ToString());
                                    command.Parameters.AddWithValue("@TimeLimit", (oldTimeLimit + info.TimeLimit) / 2);
                                    command.Parameters.AddWithValue("@StartTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                                    command.Parameters.AddWithValue("@GameID", pendingGameID);

                                    try
                                    {
                                        command.ExecuteNonQuery();

                                        output.GameID = pendingGameID.ToString();
                                        pendingGameID = -1;
                                        trans.Commit();
                                        output.Status = Created;
                                    }
                                    catch (Exception)
                                    {
                                        return null;
                                    }
                                }
                            }
                        }
                        conn.Close();
                    }
                    return output;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Cancel a pending request to join a game.
        /// 
        /// If UserToken is invalid or is not a player in the pending game,
        /// responds with status 403 (Forbidden).
        /// 
        /// Otherwise, removes UserToken from the pending game and responds
        /// with status 200 (OK).
        /// </summary>
        public CancelJoinRequestReturn CancelJoinRequest(CancelJoinRequestInfo user)
        {
            lock (sync)
            {
                CancelJoinRequestReturn output = new CancelJoinRequestReturn();

                if (user.UserToken == null)
                {
                    output.Status = Forbidden;
                    return output;
                }
                else
                {
                    using (SqlConnection conn = new SqlConnection(BoggleDB))
                    {
                        conn.Open();
                        using (SqlTransaction trans = conn.BeginTransaction())
                        {
                            // Fetch the UserToken of the player in a pending game
                            using (SqlCommand command = new SqlCommand(
                                "SELECT Player1 FROM Games WHERE GameState=0", conn, trans))
                            {
                                command.Parameters.AddWithValue("@UserToken", user.UserToken);

                                try
                                {
                                    // If this UserToken is not the player in the pending game,
                                    // set status as Forbidden.
                                    string player1 = (string)command.ExecuteScalar();
                                    if (player1 == null || player1 != user.UserToken)
                                    {
                                        throw new Exception();
                                    }
                                }
                                catch (Exception)
                                {
                                    output.Status = Forbidden;
                                    return output;
                                }
                            }
                            // Remove the pending game from the database, because the player canceled it
                            using (SqlCommand command = new SqlCommand(
                                "DELETE FROM Games WHERE GameState=0 AND Player1=@UserToken", conn, trans))
                            {
                                command.Parameters.AddWithValue("@UserToken", user.UserToken);

                                try
                                {
                                    command.ExecuteNonQuery();
                                    trans.Commit();
                                    output.Status = OK;
                                }
                                catch (Exception)
                                {
                                    output.Status = Forbidden;
                                    return output;
                                }
                            }
                        }
                        conn.Close();
                    }
                }
                output.Status = Forbidden;
                return output;
            }
        }

        /// <summary>
        /// Play a word in a game.
        /// 
        /// If Word is null or empty when trimmed, or if GameID or UserToken
        /// is missing or invalid, or if UserToken is not a player in the game
        /// identified by GameID, responds with response code 403 (Forbidden).
        /// 
        /// Otherwise, if the game state is anything other than "active",
        /// responds with response code 409 (Conflict).
        /// 
        /// Otherwise, records the trimmed Word as being played by UserToken
        /// in the game identified by GameID. Returns the score for Word
        /// in the context of the game (e.g. if Word has been played before
        /// the score is zero). Responds with status 200 (OK).
        /// 
        /// Note: The word is not case sensitive.
        /// </summary>
        public PlayWordReturn PlayWord(string gameID, PlayWordInput info)
        {
            lock (sync)
            {
                ///still need to update overall score for each player in the game!
                PlayWordReturn wordReturn = new PlayWordReturn();
                string player1 = "";
                string player2 = "";
                string boardstring = "";
                int timelimit = 0;
                int TimeLeft = 0;
                if (info.Word == null || info.UserToken == null || gameID == null || info.Word.Trim().Length == 0)
                {
                    return null;
                }
                int timeremaining;
                using (SqlConnection conn = new SqlConnection(BoggleDB))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        // Fetch the UserToken of the player in a pending game
                        using (SqlCommand command = new SqlCommand(
                            "SELECT GameState, StartTime, TimeLimit, Player1, Player2, Board FROM Games WHERE GameID = @GameID", conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", gameID);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    return null;
                                }
                                while (reader.Read())
                                {
                                    int state = (int)reader["GameState"];
                                    if (state != 1)
                                    {
                                        reader.Close();
                                        wordReturn.Status = Conflict;
                                        return wordReturn;
                                    }
                                    string date = reader["StartTime"].ToString();
                                    DateTime starttime = Convert.ToDateTime(date);
                                    timelimit = (int)reader["TimeLimit"];
                                    player1 = (string)reader["Player1"];
                                    player2 = (string)reader["Player2"];
                                    boardstring = (string)reader["Board"];

                                    long time = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                    long start = (long)(starttime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                    TimeLeft = timelimit - (int)(time - start);
                                }
                            }
                        }
                        if (TimeLeft <= 0)
                        {
                            using (SqlCommand command2 = new SqlCommand(
                                    "UPDATE Games SET GameState = 2 WHERE GameID = @GameID", conn, trans))
                            {
                                command2.Parameters.AddWithValue("@GameID", gameID);
                                try
                                {
                                    command2.ExecuteNonQuery();
                                    wordReturn.Status = Conflict;
                                    return wordReturn;
                                }
                                catch (Exception)
                                {
                                    return null;
                                }
                            }
                        }
                        else
                        {
                            if ((player1 != info.UserToken) && (player2 != info.UserToken))
                            {
                                return null;
                            }
                            timeremaining = TimeLeft;
                        }
                        // got through all tests to validate the ability to play a word. Game exists, user is in game, and the time is not up
                        // now we must get all words played by the users
                        List<String> p1Words = new List<string>();
                        List<String> p2Words = new List<string>();

                        using (SqlCommand command = new SqlCommand(
                            "SELECT Word FROM Words WHERE GameID = @GameID AND Player = @Player", conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", gameID);
                            command.Parameters.AddWithValue("@Player", player1);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    p1Words.Add((string)reader["Word"]);
                                }
                            }
                        }

                        using (SqlCommand command = new SqlCommand(
                            "SELECT Word FROM Words WHERE GameID = @GameID AND Player = @Player", conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", gameID);
                            command.Parameters.AddWithValue("@Player", player2);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    p2Words.Add((string)reader["Word"]);
                                }
                            }
                        }

                        BoggleBoard board = new BoggleBoard(boardstring);
                        string word = info.Word.Trim().ToUpper();
                        int Score = 0; ;
                        if (!board.CanBeFormed(word))
                        {
                            Score = -1;
                            wordReturn.Score = "-1";
                        }
                        else
                        {
                            foreach (string line in File.ReadLines(AppDomain.CurrentDomain.BaseDirectory + @"..\dictionary.txt"))
                            {
                                if (line.Contains(word))
                                {
                                    if (p1Words.Contains(word) || p2Words.Contains(word) || word.Length < 3)
                                    {
                                        wordReturn.Score = "0";
                                        wordReturn.Status = OK;
                                        return wordReturn;
                                    }
                                    if (word.Length == 3 || word.Length == 4)
                                    {
                                        Score = 1;
                                        wordReturn.Score = "1";
                                    }
                                    else if (word.Length == 5)
                                    {
                                        Score = 2;
                                        wordReturn.Score = "2";
                                    }
                                    else if (word.Length == 6)
                                    {
                                        Score = 3;
                                        wordReturn.Score = "3";
                                    }
                                    else if (word.Length == 7)
                                    {
                                        Score = 5;
                                        wordReturn.Score = "5";
                                    }
                                    else
                                    {
                                        Score = 11;
                                        wordReturn.Score = "11";
                                    }
                                }
                            }
                        }
                        using (SqlCommand command = new SqlCommand(
                            "INSERT INTO Words (Word, Player, Score, GameID) VALUES (@Word, @Player, @Score, @GameID)", conn, trans))
                        {
                            command.Parameters.AddWithValue("@Player", info.UserToken);
                            command.Parameters.AddWithValue("@word", word);
                            command.Parameters.AddWithValue("@Score", Score);
                            command.Parameters.AddWithValue("GameID", gameID);
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception)
                            {
                                return null;
                            }
                        }

                        using (SqlCommand command = new SqlCommand(
                            "UPDATE Users SET Score = Score + @WordScore WHERE UserToken = @UserToken", conn, trans))
                        {
                            command.Parameters.AddWithValue("@UserToken", info.UserToken);
                            command.Parameters.AddWithValue("@WordScore", Score);
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception)
                            {
                                return null;
                            }
                        }

                        wordReturn.Status = OK;
                        trans.Commit();
                    }
                    conn.Close();
                }
                return wordReturn;
            }
        }


        /// <summary>
        /// Get game status information.
        /// 
        /// If GameID is invalid, responds with status 403 (Forbidden).
        /// 
        /// Otherwise, returns information about the game named by GameID
        /// as discussed below. Note that the information returned depends on
        /// whether "Brief=yes" was included as a parameter as well as on
        /// the state of the game. Responds with status code 200 (OK).
        /// 
        /// Note: The Board and Words are not case sensitive.
        /// </summary>
        public Game GameStatus(string gameID, string brief)
        {
            lock (sync)
            {
                string player1 = "";
                string player2 = "";
                string board = "";
                int state = 0;
                int timelimit = 0;
                int TimeLeft = 0;
                DateTime starttime = new DateTime();
                Game tmpGame = new Game();
                using (SqlConnection conn = new SqlConnection(BoggleDB))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        // Fetch the UserToken of the player in a pending game
                        using (SqlCommand command = new SqlCommand(
                            "SELECT GameState, StartTime, TimeLimit, Player1, Player2, Board FROM Games WHERE GameID = @GameID", conn, trans))
                        {
                            command.Parameters.AddWithValue("@GameID", gameID);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    reader.Close();
                                    return null;
                                }
                                while (reader.Read())
                                {
                                    state = (int)reader["GameState"];
                                    if (state == 0)
                                    {
                                        //pending game
                                        tmpGame.Status = OK;
                                        tmpGame.GameState = "pending";
                                        reader.Close();
                                        trans.Commit();
                                        conn.Close();
                                        return tmpGame;
                                    }
                                    string date = reader["StartTime"].ToString();
                                    starttime = Convert.ToDateTime(date);
                                    timelimit = (int)reader["TimeLimit"];

                                    player1 = (string)reader["Player1"];
                                    player2 = (string)reader["Player2"];
                                    board = (string)reader["Board"];
                                }
                            }
                        }
                        // If game is active, update the time left
                        if (state == 1)
                        {
                            long time = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                            long start = (long)(starttime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                            TimeLeft = timelimit - (int)(time - start);
                            if (TimeLeft <= 0)
                            {
                                TimeLeft = 0;
                                state = 2;
                                using (SqlCommand command = new SqlCommand(
                                    "UPDATE Games SET GameState = 2 WHERE GameID = @GameID", conn, trans))
                                {
                                    command.Parameters.AddWithValue("@GameID", gameID);

                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                        // If the game is active or completed and Brief == "yes"
                        if (state > 0 && brief != null && brief.ToLower() == "yes")
                        {
                            User p1 = new User();
                            User p2 = new User();
                            using (SqlCommand command = new SqlCommand(
                                "SELECT Score FROM Users WHERE UserToken = @user", conn, trans))
                            {
                                command.Parameters.AddWithValue("@user", player1);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        reader.Close();
                                        return null;
                                    }
                                    while (reader.Read())
                                    {
                                        p1.Score = (int)reader["Score"];
                                    }
                                }
                            }
                            using (SqlCommand command = new SqlCommand(
                                "SELECT Score FROM Users WHERE UserToken = @user", conn, trans))
                            {
                                command.Parameters.AddWithValue("@user", player2);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        reader.Close();
                                        return null;
                                    }
                                    while (reader.Read())
                                    {
                                        p2.Score = (int)reader["Score"];
                                    }
                                }
                            }
                            if (state == 1) tmpGame.GameState = "active";
                            else tmpGame.GameState = "completed";
                            tmpGame.TimeLeft = TimeLeft;
                            tmpGame.Player1 = p1;
                            tmpGame.Player2 = p2;
                            tmpGame.Status = OK;

                            trans.Commit();
                        }
                        // Otherwise, if the game is active and Brief != "yes"
                        else if (state == 1)
                        {
                            User p1 = new User();
                            User p2 = new User();
                            tmpGame.TimeLeft = TimeLeft;
                            using (SqlCommand command = new SqlCommand(
                                "SELECT Score, Nickname FROM Users WHERE UserToken = @user", conn, trans))
                            {
                                command.Parameters.AddWithValue("@user", player1);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        reader.Close();
                                        return null;
                                    }
                                    while (reader.Read()) 
                                    {
                                        p1.Score = (int)reader["Score"];
                                        p1.Nickname = (string)reader["Nickname"];
                                    }
                                }
                            }
                            using (SqlCommand command = new SqlCommand(
                                "SELECT Score, Nickname FROM Users WHERE UserToken = @user", conn, trans))
                            {
                                command.Parameters.AddWithValue("@user", player2);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        reader.Close();
                                        return null;
                                    }
                                    while (reader.Read())
                                    {
                                        p2.Score = (int)reader["Score"];
                                        p2.Nickname = (string)reader["Nickname"];
                                    }
                                }
                            }

                            tmpGame.GameState = "active";
                            tmpGame.Board = board;
                            tmpGame.TimeLimit = timelimit;
                            tmpGame.TimeLeft = TimeLeft;
                            tmpGame.Player1 = p1;
                            tmpGame.Player2 = p2;
                            tmpGame.Status = OK;
                            trans.Commit();
                        }
                        // Otherwise, if the game is completed and Brief != "yes"
                        else if (state == 2)
                        {
                            User p1 = new User();
                            User p2 = new User();
                            tmpGame.TimeLeft = TimeLeft;
                            //the game is completed and brief was no or null.
                            List<Words> p1Words = new List<Words>();
                            List<Words> p2Words = new List<Words>();

                            using (SqlCommand command = new SqlCommand(
                                "SELECT Word, Score FROM Words WHERE GameID = @GameID AND Player = @Player", conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameID", gameID);
                                command.Parameters.AddWithValue("@Player", player1);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        Words tmpWord = new Words();
                                        tmpWord.Word = (string)reader["Word"];
                                        tmpWord.Score = (int)reader["Score"];
                                        p1Words.Add(tmpWord);
                                    }
                                }
                            }

                            using (SqlCommand command = new SqlCommand(
                                "SELECT Word FROM Words WHERE GameID = @GameID AND Player = @Player", conn, trans))
                            {
                                command.Parameters.AddWithValue("@GameID", gameID);
                                command.Parameters.AddWithValue("@Player", player2);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        Words tmpWord = new Words();
                                        tmpWord.Word = (string)reader["Word"];
                                        tmpWord.Score = (int)reader["Score"];
                                        p2Words.Add(tmpWord);
                                    }
                                }
                            }
                            using (SqlCommand command = new SqlCommand(
                                "SELECT Score, Nickname FROM Users WHERE UserToken = @user", conn, trans))
                            {
                                command.Parameters.AddWithValue("@user", player1);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        reader.Close();
                                        return null;
                                    }
                                    while (reader.Read())
                                    {
                                        p1.Nickname = (string)reader["Nickname"];
                                        p1.Score = (int)reader["Score"];
                                        p1.WordsPlayed = p1Words;
                                    }
                                }
                            }
                            using (SqlCommand command = new SqlCommand(
                                "SELECT Score, Nickname FROM Users WHERE UserToken = @user", conn, trans))
                            {
                                command.Parameters.AddWithValue("@user", player2);
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.HasRows)
                                    {
                                        reader.Close();
                                        return null;
                                    }
                                    while (reader.Read())
                                    {
                                        p2.Nickname = (string)reader["Nickname"];
                                        p2.Score = (int)reader["Score"];
                                        p2.WordsPlayed = p2Words;
                                    }
                                }
                            }
                            tmpGame.GameState = "completed";
                            tmpGame.Board = board;
                            tmpGame.TimeLimit = timelimit;
                            tmpGame.TimeLeft = 0;
                            tmpGame.Player1 = p1;
                            tmpGame.Player2 = p2;
                            tmpGame.Status = OK;
                            trans.Commit();
                        }
                    }
                    conn.Close();
                }
                return tmpGame;
            }
        }
    }
}
