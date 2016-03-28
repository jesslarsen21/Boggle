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

        public CreateUserReturn CreateUser(CreateUserInfo user)
        {
            throw new NotImplementedException();
        }

        public Game GameStatus(GameStatusInfo info, string gameID)
        {
            throw new NotImplementedException();
        }

        public JoinGameReturn JoinGame(JoinGameInfo info)
        {
            throw new NotImplementedException();
        }

        public PlayWordReturn PlayWord(PlayWordInfo info, string gameID)
        {
            throw new NotImplementedException();
        }
    }
}
