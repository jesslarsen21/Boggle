using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        private Dictionary<string, Game> games;
        private Dictionary<string, User> users;
        private Game pendingGame;
        private int gameCounter;

        public BoggleService()
        {
            games = new Dictionary<string, Game>();
            users = new Dictionary<string, User>();
            pendingGame = new Game();
            pendingGame.GameState = "pending";
            gameCounter = 1;
            games.Add(gameCounter.ToString(), pendingGame);
        }

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
            if (user.Nickname == null || user == null || user.Nickname.Trim().Length == 0)
            {
                SetStatus(Forbidden);
                return null;
            }
            else
            {
                string Token = Guid.NewGuid().ToString();
                CreateUserReturn newUser = new CreateUserReturn();
                newUser.UserToken = Token;

                User tmpUser = new User();
                tmpUser.Nickname = user.Nickname;
                tmpUser.UserToken = Token;
                users.Add(Token, tmpUser);

                SetStatus(Created);

                return newUser;
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
            User user;
            if (users.TryGetValue(info.UserToken, out user) && info.TimeLimit > 5 && info.TimeLimit < 120)
            {
                // If there is already one player in the pending game
                if (pendingGame.Player1 != null)
                {
                    // Convert the pending game to an active one
                    Game activeGame = new Game();
                    activeGame.Player1 = pendingGame.Player1;
                    activeGame.TimeLimit = pendingGame.TimeLimit;
                    activeGame.GameState = "active";
                    games.Remove(pendingGame.GameID);
                    activeGame.GameID = pendingGame.GameID;
                    games.Add(activeGame.GameID, activeGame);

                    // Add the second user and average the time limit
                    user.Score = 0;
                    user.WordsPlayed = new List<Words>();
                    activeGame.Player2 = user;
                    activeGame.TimeLimit = (activeGame.TimeLimit + info.TimeLimit) / 2;
                    activeGame.TimeLeft = activeGame.TimeLimit;

                    // Create a new pending game
                    gameCounter++;
                    pendingGame = new Game();
                    pendingGame.GameState = "pending";
                    pendingGame.GameID = gameCounter.ToString();
                    games.Add(pendingGame.GameID, pendingGame);

                    // Return info to user
                    JoinGameReturn output = new JoinGameReturn();
                    output.GameID = activeGame.GameID;
                    SetStatus(Created);
                    return output;
                }
                // If this is the first player in the pending game
                else
                {
                    // Add the first user and set the time limit
                    user.Score = 0;
                    user.WordsPlayed = new List<Words>();
                    pendingGame.Player1 = user;
                    pendingGame.TimeLimit = info.TimeLimit;

                    // Return info to user
                    JoinGameReturn output = new JoinGameReturn();
                    output.GameID = pendingGame.GameID;
                    SetStatus(Accepted);
                    return output;
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
            User tmpUser;
            if (user.UserToken == null || !users.TryGetValue(user.UserToken, out tmpUser) || user.UserToken != pendingGame.Player1.UserToken)
            {
                SetStatus(Forbidden);
            }
            else
            {
                string id = pendingGame.GameID;
                games.Remove(id);

                pendingGame = new Game();
                pendingGame.GameState = "pending";
                pendingGame.GameID = id;
                games.Add(pendingGame.GameID, pendingGame);

                SetStatus(OK);
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
        public PlayWordReturn PlayWord(PlayWordInfo info, string gameID)
        {
            Game currGame;
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
            else if (currGame.GameState != "active")
            {
                SetStatus(Conflict);
                return null;
            }
            string word = info.Word.Trim();
            tmpWord.Word = word;
            if (currGame.internalBoard.CanBeFormed(word))
            {
                foreach (string line in File.ReadLines("dictionary.txt"))
                {
                    if (line.Contains(word))
                    {
                        
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
                        }
                        else
                        {
                            currGame.Player2.WordsPlayed.Add(tmpWord);
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
        public Game GameStatus(GameStatusInfo info, string gameID)
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
                else if (info.Brief != null && info.Brief.ToLower() == "yes")
                {
                    // Update game.TimeLeft
                    if (game.GameState == "active")
                    {
                        DateTime time = DateTime.UtcNow;
                        game.TimeLeft -= (int)time.Subtract(game.StartTime).TotalSeconds;
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
                    // Update game.TimeLeft
                    if (game.GameState == "active")
                    {
                        DateTime time = DateTime.UtcNow;
                        game.TimeLeft -= (int)time.Subtract(game.StartTime).TotalSeconds;
                        // If the time is up, end the game.
                        if (game.TimeLeft <= 0)
                        {
                            game.GameState = "completed";
                            game.TimeLeft = 0;
                            SetStatus(OK);
                            return game;
                        }
                    }
                    else
                    {
                        game.TimeLeft = 0;
                    }
                    SetStatus(OK);
                    return game.GetActive();
                }
                // Otherwise, if the game is completed
                else
                {
                    game.TimeLeft = 0;
                    SetStatus(OK);
                    return game;
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
