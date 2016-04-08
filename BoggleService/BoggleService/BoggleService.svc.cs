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
        private static readonly string BoggleDB = ConfigurationManager.ConnectionStrings["BoggleDB"].ConnectionString;
        private static int pendingGameID = -1;

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
            if (user == null || user.Nickname == null || user.Nickname.Trim().Length == 0)
            {
                SetStatus(Forbidden);
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

                                SetStatus(Created);
                                trans.Commit();

                                return newUser;
                            }
                            catch (Exception)
                            {
                                SetStatus(Forbidden);
                                return null;
                            }
                        }
                    }
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
            // No need to check for a valid UserToken here,
            // because the database will handle that through exceptions
            User user = new User();
            if (info.TimeLimit >= 5 && info.TimeLimit <= 120)
            {
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
                                    //command.ExecuteNonQuery();
                                    pendingGameID = (int)command.ExecuteScalar();
                                }
                                catch (Exception)
                                {
                                    SetStatus(Forbidden);
                                    return null;
                                }
                            }
                            /*
                            // Fetch the gameID from the database
                            using (SqlCommand command = new SqlCommand(
                                "SELECT GameID FROM Games WHERE GameState=0", conn, trans))
                            {
                                try
                                {
                                    // Executes the command and fetches the item in position [0, 0] of the output
                                    pendingGameID = (int)command.ExecuteScalar();
                                }
                                catch (Exception)
                                {
                                    SetStatus(Forbidden);
                                    return null;
                                }
                            }
                            */
                            // Return info to user and commit database changes
                            JoinGameReturn output = new JoinGameReturn();
                            output.GameID = pendingGameID.ToString();
                            SetStatus(Accepted);
                            trans.Commit();
                            return output;

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
                                        SetStatus(Conflict);
                                        reader.Close();
                                        return null;
                                    }
                                    oldTimeLimit = reader.GetInt32(1);
                                    reader.Close();
                                }
                                catch (Exception ex)
                                {
                                    SetStatus(Forbidden);
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

                                    JoinGameReturn output = new JoinGameReturn();
                                    output.GameID = pendingGameID.ToString();
                                    pendingGameID = -1;
                                    trans.Commit();
                                    SetStatus(Created);
                                    return output;
                                }
                                catch (Exception)
                                {
                                    SetStatus(Forbidden);
                                    return null;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                SetStatus(Forbidden);
                return null;
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
        public void CancelJoinRequest(CancelJoinRequestInfo user)
        {
            if (user.UserToken == null)
            {
                SetStatus(Forbidden);
                return;
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
                                SetStatus(Forbidden);
                                return;
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
                                SetStatus(OK);
                                return;
                            }
                            catch (Exception)
                            {
                                SetStatus(Forbidden);
                                return;
                            }
                        }
                    }
                }
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
            ///still need to update overall score for each player in the game!
            PlayWordReturn wordReturn = new PlayWordReturn();
            string player1 = "";
            string player2 = "";
            string boardstring = "";
            int timelimit = 0;
            int TimeLeft = 0;
            if (info.Word == null || info.UserToken == null || gameID == null || info.Word.Trim().Length == 0)
            {
                SetStatus(Forbidden);
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
                                SetStatus(Forbidden);
                                return null;
                            }
                            while (reader.Read())
                            {
                                int state = (int)reader["GameState"];
                                if (state != 1)
                                {
                                    reader.Close();
                                    SetStatus(Conflict);
                                    trans.Commit();
                                    return null;
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
                    if (TimeLeft < timelimit)
                    {
                        using (SqlCommand command2 = new SqlCommand(
                                "UPDATE Games SET GameState = 2 WHERE GameID = @GameID", conn, trans))
                        {
                            command2.Parameters.AddWithValue("@GameID", gameID);
                            try
                            {
                                command2.ExecuteNonQuery();
                                SetStatus(Conflict);
                                return null;
                            }
                            catch (Exception Ex)
                            {
                                SetStatus(Forbidden);
                                return null;
                            }
                        }
                    }
                    else
                    {
                        if ((player1 != info.UserToken) && (player2 != info.UserToken))
                        {
                            SetStatus(Forbidden);
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
                        foreach (string line in File.ReadLines(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt"))
                        {
                            if (line.Contains(word))
                            {
                                if (p1Words.Contains(word) || p2Words.Contains(word) || word.Length < 3)
                                {
                                    wordReturn.Score = "0";
                                    SetStatus(OK);
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
                            SetStatus(Forbidden);
                            return null;
                        }
                    }

                    using (SqlCommand command = new SqlCommand(
                     "UPDATE Words SET Score = Score + @WordScore WHERE UserToken = @UserToken", conn, trans))
                    {
                        command.Parameters.AddWithValue("@UserToken", info.UserToken);
                        command.Parameters.AddWithValue("@WordScore", Score);
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception)
                        {
                            SetStatus(Forbidden);
                            return null;
                        }
                    }

                    SetStatus(OK);

                    trans.Commit();
                    return wordReturn;
                }
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
            string player1 = "";
            string player2 = "";
            string board = "";
            int state = 0;
            int timelimit = 0;
            int TimeLeft = 0;
            DateTime starttime = new DateTime();
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
                                SetStatus(Forbidden);
                                return null;
                            }
                            while (reader.Read())
                            {
                                state = (int)reader["GameState"];
                                if (state == 0)
                                {
                                    //pending game
                                    SetStatus(OK);
                                    Game tmpGame = new Game();
                                    tmpGame.GameState = "pending";
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
                                if (state == 1)
                                {
                                    long time = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                    long start = (long)(starttime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                    TimeLeft = timelimit - (int)(time - start);
                                    if (TimeLeft < timelimit)
                                    {
                                        TimeLeft = 0;
                                        state = 2;
                                        using (SqlCommand command2 = new SqlCommand(
                                                "UPDATE Games SET GameState = 2 WHERE GameID = @GameID", conn, trans))
                                        {
                                            command2.Parameters.AddWithValue("@GameID", gameID);

                                                command2.ExecuteNonQuery();
                                                SetStatus(Conflict);
                                                return null;

                            }
                        }
                    }
                    // If the game is active or completed and Brief == "yes"
                    if (brief != null && brief.ToLower() == "yes")
                    {
                        Game tmpGame = new Game();
                        // Update game.TimeLeft
                        if (state == 1 || state == 2)
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
                                        SetStatus(Forbidden);
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
                                        SetStatus(Forbidden);
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
                            SetStatus(OK);

                            trans.Commit();
                            return tmpGame;
                        }
                    }

                    // Otherwise, if the game is active and brief is not there or anything other than yes
                    if (state == 1)
                    {
                        Game tmpGame = new Game();
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
                                    SetStatus(Forbidden);
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
                                    SetStatus(Forbidden);
                                    return null;
                                }
                                while (reader.Read())
                                {
                                    p1.Score = (int)reader["Score"];
                                    p1.Nickname = (string)reader["Nickname"];
                                }
                            }
                        }

                        tmpGame.GameState = "active";
                        tmpGame.Board = board;
                        tmpGame.TimeLimit = timelimit;
                        tmpGame.TimeLeft = TimeLeft;
                        tmpGame.Player1 = p1;
                        tmpGame.Player1 = p2;
                        SetStatus(OK);
                        trans.Commit();
                        return tmpGame;
                    }
                    else
                    {
                        Game tmpGame = new Game();
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
                                    SetStatus(Forbidden);
                                    return null;
                                }
                                while(reader.Read())
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
                                    SetStatus(Forbidden);
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
                        tmpGame.Player1 = p2;
                        SetStatus(OK);
                        trans.Commit();
                        return tmpGame;

                    }
                }
            }
        }

        //end
    }
}
