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
        /// keep track of pending game. If one requests to start a game, if there is a 
        /// game sitting in pending Game, add the player to that game, and set pending 
        /// game back to null. If one requests to start a game, and there is not game
        /// sitting in pending game (pendingGame==null), then add a new game to 
        /// pendingGame. 
        /// </summary>
        private Game pendingGame = null;

        /// <summary>
        /// The most recent call to SetStatus determines the response code used when
        /// an http response is sent.
        /// </summary>
        /// <param name="status"></param>
        private static void SetStatus(HttpStatusCode status)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        }

        /// <summary>
        /// Returns a Stream version of index.html.
        /// </summary>
        /// <returns></returns>
        public Stream API()
        {
            SetStatus(OK);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        }

        public void CancelJoinRequest(CancelJoinRequestInfo user)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
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

        public Game GameStatus(GameStatusInfo info, string gameID)
        {
            throw new NotImplementedException();
        }

        public JoinGameReturn JoinGame(JoinGameInfo info)
        {
            if (!users.ContainsKey(info.UserToken) || (info.TimeLimit < 5 || info.TimeLimit > 120) )
            {
                SetStatus(Forbidden);
                return null;
            }

            if(pendingGame != null && pendingGame.Player1.UserToken == info.UserToken)
            {
                SetStatus(Conflict);
                return null;
            }

            if (pendingGame == null)
            {
                //there is no pending game, so we much create one
                Game tmpGame = new Game();
                tmpGame.Player1 = users[info.UserToken];
                tmpGame.GameState = "pending";
                tmpGame.GameID = games.Count + 1;
                tmpGame.TimeLimit = info.TimeLimit;

                //user is player 1
                SetStatus(Accepted);
                JoinGameReturn returnGame = new JoinGameReturn();
                returnGame.GameID = tmpGame.GameID.ToString();
            }
            else
            {
                //There is already a pending game that has a player 1, a time limit, and a game id. The state is pending.
                Game tmpGame = new Game();
                tmpGame.Player1 = pendingGame.Player1;
                tmpGame.GameID = pendingGame.GameID;
                int average = (info.TimeLimit + pendingGame.TimeLimit) / 2;
                tmpGame.TimeLimit = average;
                tmpGame.Player2 = users[info.UserToken];
                //time in miliseconds.
                int ms = Convert.ToInt32((DateTime.Now - DateTime.MinValue).TotalMilliseconds);


                //status set to created, user is player two, returning pendingGame GameID.
                SetStatus(Created);
                JoinGameReturn returnGame = new JoinGameReturn();
                returnGame.GameID = pendingGame.GameID.ToString();
            }
        }

        public PlayWordReturn PlayWord(PlayWordInfo info, string gameID)
        {
            throw new NotImplementedException();
        }
    }
}
