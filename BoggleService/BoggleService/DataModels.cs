using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Boggle
{
    [DataContract]
    public class User
    {
        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string UserToken { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int Score { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<Words> WordsPlayed { get; set; }

        public User GetBrief()
        {
            User output = new User();
            output.Score = Score;
            return output;
        }
    }

    public class Words
    {
        public string Word { get; set; }
        public int Score { get; set; }
    }

    [DataContract]
    public class Game
    {
        [DataMember(EmitDefaultValue = false)]
        public string GameState { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Board { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLimit { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLeft { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public User Player1 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public User Player2 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int GameID { get; set; }

        public int StartTime { get; set; }

        public Game GetBrief()
        {
            Game output = new Game();
            output.GameState = GameState;
            output.TimeLeft = TimeLeft;
            output.Player1 = Player1.GetBrief();
            output.Player2 = Player2.GetBrief();
            return output;
        }
    }

    public class CreateUserInfo
    {
        public string Nickname { get; set; }
    }

    public class CreateUserReturn
    {
        public string UserToken { get; set; }
    }

    public class CancelJoinRequestInfo
    {
        public string UserToken { get; set; }
    }

    public class JoinGameInfo
    {
        public string UserToken { get; set; }
        public int TimeLimit { get; set; }
    }

    public class JoinGameReturn
    {
        public string GameID { get; set; }
    }

    public class PlayWordInfo
    {
        public string UserToken { get; set; }
        public string Word { get; set; }
    }

    public class PlayWordReturn
    {
        public string Score { get; set; }
    }

    public class GameStatusInfo
    {
        public string Brief { get; set; }
    }
}