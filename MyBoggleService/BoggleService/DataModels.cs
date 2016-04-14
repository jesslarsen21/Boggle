using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace Boggle
{
    [DataContract]
    public class User
    {
        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string UserToken { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<Words> WordsPlayed { get; set; }

        public List<string> GetAllWordsPlayed()
        {
            List<string> words = new List<string>() ;
            foreach (var word in WordsPlayed)
            {
                words.Add(word.Word);
            }
            return words;
        }

        public User GetBrief()
        {
            User output = new User();
            output.Score = Score;
            return output;
        }

        public User GetActive()
        {
            User output = new User();
            output.Nickname = Nickname;
            output.Score = Score;
            return output;
        }

        public User GetComplete()
        {
            User output = new User();
            output.Nickname = Nickname;
            output.Score = Score;
            output.WordsPlayed = WordsPlayed;
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
        
        public HttpStatusCode Status { get; set; }

        public BoggleBoard internalBoard { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLimit { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLeft { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public User Player1 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public User Player2 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string GameID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public long StartTime { get; set; }

        public Game GetBrief()
        {
            Game output = new Game();
            output.GameState = GameState;
            output.TimeLeft = TimeLeft;
            output.Player1 = Player1.GetBrief();
            output.Player2 = Player2.GetBrief();
            return output;
        }

        public Game GetActive()
        {
            Game output = new Game();
            output.GameState = GameState;
            output.Board = Board;
            output.TimeLimit = TimeLimit;
            output.TimeLeft = TimeLeft;
            output.Player1 = Player1.GetActive();
            output.Player2 = Player2.GetActive();
            return output;
        }

        public Game GetPending()
        {
            Game output = new Game();
            output.GameState = "pending";
            return output;
        }

        public Game GetComplete()
        {
            Game output = new Game();
            output.GameState = GameState;
            output.Board = Board;
            output.TimeLimit = TimeLimit;
            output.TimeLeft = TimeLeft;
            output.Player1 = Player1.GetComplete();
            output.Player2 = Player2.GetComplete();
            return output;
        }
    }

    public class CreateUserInfo
    {
        public string Nickname { get; set; }
    }

    [DataContract]
    public class CreateUserReturn
    {
        [DataMember]
        public string UserToken { get; set; }
        
        public HttpStatusCode Status { get; set; }
    }

    public class CancelJoinRequestInfo
    {
        public string UserToken { get; set; }
    }
    
    public class CancelJoinRequestReturn
    {
        public HttpStatusCode Status { get; set; }
    }

    public class JoinGameInfo
    {
        public string UserToken { get; set; }

        public int TimeLimit { get; set; }
    }

    [DataContract]
    public class JoinGameReturn
    {
        [DataMember]
        public string GameID { get; set; }

        public HttpStatusCode Status { get; set; }
    }

    public class PlayWordInput
    {
        public string UserToken { get; set; }

        public string Word { get; set; }
    }

    [DataContract]
    public class PlayWordReturn
    {
        [DataMember]
        public string Score { get; set; }

        public HttpStatusCode Status { get; set; }
    }
}