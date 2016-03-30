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
        private Dictionary<string, Game> games = new Dictionary<string, Game>();
        private Dictionary<string, User> users = new Dictionary<string, User>();

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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
