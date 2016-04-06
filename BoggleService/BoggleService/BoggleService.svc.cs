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

        private static readonly Dictionary<string, Game> games = new Dictionary<string, Game>();
        private static readonly Dictionary<string, User> users = new Dictionary<string, User>();
        private static Game pendingGame = new Game();
        private static int gameCounter;
        private static bool firstConstruction = true;

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
                                "INSERT INTO Games(Player1, TimeLimit, GameState) VALUES (@UserToken, @TimeLimit, 0)", conn, trans))
                            {
                                command.Parameters.AddWithValue("@UserToken", info.UserToken);
                                command.Parameters.AddWithValue("@TimeLimit", info.TimeLimit);

                                try
                                {
                                    // Executes the command and returns the number of rows affected
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception)
                                {
                                    SetStatus(Forbidden);
                                    return null;
                                }
                            }
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
                                catch (Exception)
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
                                }
                                catch (Exception)
                                {
                                    SetStatus(Forbidden);
                                    return null;
                                }
                            }

                            JoinGameReturn output = new JoinGameReturn();
                            output.GameID = pendingGameID.ToString();
                            pendingGameID = -1;
                            trans.Commit();
                            SetStatus(Created);
                            return output;
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
                            "DELETE FROM Games WHERE GameState=0", conn, trans))
                        {
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception)
                            {
                                SetStatus(Forbidden);
                                return;
                            }
                        }
                        trans.Commit();
                        SetStatus(OK);
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
            Game currGame = null;
            User currUser;
            Words tmpWord = new Words();
            PlayWordReturn wordReturn = new PlayWordReturn();
            if (info.Word == null || info.UserToken == null || gameID == null || info.Word.Trim().Length == 0 ||
                !users.TryGetValue(info.UserToken, out currUser) || !games.TryGetValue(gameID, out currGame) ||
                (currGame.Player1.UserToken != info.UserToken && currGame.Player2.UserToken != info.UserToken))
            {
                SetStatus(Forbidden);
                return null;
            }

            // Update game.TimeLeft
            if (currGame.GameState == "active")
            {
                long time = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                currGame.TimeLeft = currGame.TimeLimit - (int)(time - currGame.StartTime);
                // If the time is up, end the game.
                if (currGame.TimeLeft <= 0)
                {
                    currGame.GameState = "completed";
                    currGame.TimeLeft = 0;
                }
            }

            if (currGame.GameState != "active")
            {
                SetStatus(Conflict);
                return null;
            }
            string word = info.Word.Trim().ToUpper();
            tmpWord.Word = word;
            var listofWordsPlayed = currGame.Player1.GetAllWordsPlayed();
            var listofWordsPlayed2 = currGame.Player2.GetAllWordsPlayed();

            if (currGame.internalBoard.CanBeFormed(word))
            {
                foreach (string line in File.ReadLines(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt"))
                {
                    if (line.Contains(word))
                    {
                        if (listofWordsPlayed.Contains(word) || listofWordsPlayed2.Contains(word) || word.Length < 3)
                        {
                            tmpWord.Score = 0;
                            wordReturn.Score = "0";
                            SetStatus(OK);
                            return wordReturn;
                        }
                        if (word.Length == 3 || word.Length == 4)
                        {
                            tmpWord.Score = 1;
                            wordReturn.Score = "1";
                        }
                        else if (word.Length == 5)
                        {
                            tmpWord.Score = 2;
                            wordReturn.Score = "2";
                        }
                        else if (word.Length == 6)
                        {
                            tmpWord.Score = 3;
                            wordReturn.Score = "3";
                        }
                        else if (word.Length == 7)
                        {
                            tmpWord.Score = 5;
                            wordReturn.Score = "5";
                        }
                        else
                        {
                            tmpWord.Score = 11;
                            wordReturn.Score = "11";
                        }

                        if (currGame.Player1.UserToken == info.UserToken)
                        {
                            currGame.Player1.WordsPlayed.Add(tmpWord);
                            currGame.Player1.Score += tmpWord.Score;
                        }
                        else
                        {
                            currGame.Player2.WordsPlayed.Add(tmpWord);
                            currGame.Player2.Score += tmpWord.Score;
                        }

                        SetStatus(OK);
                        return wordReturn;
                    }
                }
            }

            tmpWord.Score = -1;
            if (currGame.Player1.UserToken == info.UserToken)
            {
                currGame.Player1.WordsPlayed.Add(tmpWord);
            }
            else
            {
                currGame.Player2.WordsPlayed.Add(tmpWord);
            }
            wordReturn.Score = "-1";
            SetStatus(OK);
            return wordReturn;
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
            Game game;
            if (games.TryGetValue(gameID, out game))
            {
                // If the game is pending
                if (game.GameState == "pending")
                {
                    SetStatus(OK);
                    return game.GetPending();
                }
                // If the game is active or completed and Brief == "yes"
                else if (brief != null && brief.ToLower() == "yes")
                {
                    // Update game.TimeLeft
                    if (game.GameState == "active")
                    {
                        long time = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        game.TimeLeft = game.TimeLimit - (int)(time - game.StartTime);
                        // If the time is up, end the game.
                        if (game.TimeLeft <= 0)
                        {
                            game.GameState = "completed";
                            game.TimeLeft = 0;
                        }
                    }
                    else
                    {
                        game.TimeLeft = 0;
                    }
                    SetStatus(OK);
                    return game.GetBrief();
                }
                // Otherwise, if the game is active
                else if (game.GameState == "active")
                {
                        long time = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        game.TimeLeft = game.TimeLimit - (int)(time - game.StartTime);
                        // If the time is up, end the game.
                        if (game.TimeLeft <= 0)
                        {
                            game.GameState = "completed";
                            game.TimeLeft = 0;
                            SetStatus(OK);
                            return game.GetComplete();
                        }
                    SetStatus(OK);
                    return game.GetActive();
                }
                // Otherwise, if the game is completed
                else
                {
                    game.TimeLeft = 0;
                    SetStatus(OK);
                    return game.GetComplete();
                }
            }
            else
            {
                SetStatus(Forbidden);
                return null;
            }
        }
    }
}
