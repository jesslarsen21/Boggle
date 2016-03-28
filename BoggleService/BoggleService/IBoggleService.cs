using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Boggle
{
    [ServiceContract]
    public interface IBoggleService
    {
        /// <summary>
        /// Sends back index.html as the response body.
        /// </summary>
        [WebGet(UriTemplate = "/api")]
        Stream API();

        /// <summary>
        /// Creates a new user and assigns them a unique UserToken
        /// for the passed Nickname.
        /// </summary>
        [WebInvoke(Method = "POST", UriTemplate = "/users")]
        CreateUserReturn CreateUser(CreateUserInfo user);

        /// <summary>
        /// Joins a pending game, after verifying the UserToken, with a
        /// time limit equal to the integer average time limits input
        /// by the players.
        /// </summary>
        [WebInvoke(Method = "POST", UriTemplate = "/games")]
        JoinGameReturn JoinGame(JoinGameInfo info);

        /// <summary>
        /// Cancels a pending request to join a game, by removing the player
        /// from the pending game.
        /// </summary>
        [WebInvoke(Method = "PUT", UriTemplate = "/games")]
        void CancelJoinRequest(CancelJoinRequestInfo user);

        /// <summary>
        /// Plays the given word for the player in the given game, and returns
        /// the score for the given word.
        /// </summary>
        [WebInvoke(Method = "PUT", UriTemplate = "/games/{gameID}")]
        PlayWordReturn PlayWord(PlayWordInfo info, string gameID);

        [WebGet(UriTemplate = "/games/{gameID}")]
        Game GameStatus(GameStatusInfo info, string gameID);
    }
}
