﻿using System;
using System.Linq;
using System.Collections.Generic;

using Core;

using CSharpFunctionalExtensions;

namespace WebUI.Services
{
    public class GamingComunication
    {
        private const short STANDARD_BALLS_VERSION_TOTAL = 75;
        private const short STANDARD_BALLS_VERSION_PER_BUCKET_COUNT = 5;

        private readonly List<Game> _games;
        private readonly Random _randomizer;

        public GamingComunication()
        {
            this._games = new List<Game>();
            this._randomizer = new Random();
        }

        public Result AddStandardGame(string name)
        {
            name = name.Trim();
            var existingGameFound = this._games.FirstOrDefault(game => game.Name == name);
            if (existingGameFound != null)
                return Result.Failure("There is already a game with the same name. Please try with a different one");

            var newGameResult = Game.Create(name, STANDARD_BALLS_VERSION_TOTAL, STANDARD_BALLS_VERSION_PER_BUCKET_COUNT);
            if (newGameResult.IsFailure)
                return newGameResult;

            this._games.Add(newGameResult.Value);

            return Result.Ok();
        }

        public Result<Game> AddNewPlayerToGame(string gameName, string playerName, string playerLogin, string playerPassword)
        {
            gameName = gameName.Trim();
            playerName = playerName.Trim();
            playerLogin = playerLogin.Trim().ToLower();
            playerPassword = playerPassword.Trim();

            var gameFound = this._games.FirstOrDefault(g => g.Name == gameName);
            if (gameFound == null)
                return Result.Failure<Game>("Game has not been found by its name");

            var newPlayerSecurityResult = PlayerSecurity.Create(playerLogin, playerPassword);
            if (newPlayerSecurityResult.IsFailure)
                return Result.Failure<Game>(newPlayerSecurityResult.Error);

            var newPlayerResult = Player.Create(playerName, newPlayerSecurityResult.Value);
            if (newPlayerResult.IsFailure)
                return Result.Failure<Game>(newPlayerResult.Error);
            
            var addPlayerResult = gameFound.AddPlayer(newPlayerResult.Value);
            if (addPlayerResult.IsFailure)
                return Result.Failure<Game>(addPlayerResult.Error);

            return Result.Ok(gameFound);
        }

        public Result<Game> AddBoardToPlayer(string inGameName, string toPlayerName)
        {
            inGameName = inGameName.Trim();
            toPlayerName = toPlayerName.Trim();

            var gameFound = this._games.FirstOrDefault(g => g.Name == inGameName);
            if (gameFound == null)
                return Result.Failure<Game>("Game has not been found by its name");

            var playerFound = gameFound.Players.FirstOrDefault(player => player.Name == toPlayerName);
            if (playerFound == null)
                return Result.Failure<Game>("Player has not been found by its name");

            var addBoardResult = gameFound.AddBoardToPlayer(this._randomizer, playerFound);
            if (addBoardResult.IsFailure)
                return Result.Failure<Game>(addBoardResult.Error);

            return Result.Ok(gameFound);
        }

        public Result<Game> StartGame(string gameName)
        {
            gameName = gameName.Trim();

            var gameFound = this._games.FirstOrDefault(g => g.Name == gameName);
            if (gameFound == null)
                return Result.Failure<Game>("Game has not been found by its name");

            var gameStartResult = gameFound.Start();
            if(gameStartResult.IsFailure)
                return Result.Failure<Game>(gameStartResult.Error);

            return Result.Ok(gameFound);
        }

        public Result<Game> PlayBall(string inGameName, string ballName)
        {
            inGameName = inGameName.Trim();

            var gameFound = this._games.FirstOrDefault(g => g.Name == inGameName);
            if (gameFound == null)
                return Result.Failure<Game>("Game has not been found by its name");

            var ballFound = gameFound.BallsConfigured.FirstOrDefault(ball => ball.Name == ballName);
            if(ballFound == null)
                return Result.Failure<Game>("Ball has not been found by its name");

            var playBallResult = gameFound.PlayBall(ballFound);
            if (playBallResult.IsFailure)
                return Result.Failure<Game>(playBallResult.Error);

            return Result.Ok(gameFound);
        }

        public Result<Game> RandomlyPlayBall(string inGameName)
        {
            inGameName = inGameName.Trim();

            var gameFound = this._games.FirstOrDefault(g => g.Name == inGameName);
            if (gameFound == null)
                return Result.Failure<Game>("Game has not been found by its name");

            var playBallResult = gameFound.RadmonlyPlayBall(this._randomizer);
            if (playBallResult.IsFailure)
                return Result.Failure<Game>(playBallResult.Error);

            return Result.Ok(gameFound);
        }

        public Result<Game> SetWinner(string inGameName, string winnerName)
        {
            inGameName = inGameName.Trim();
            winnerName = winnerName.Trim();

            var gameFound = this._games.FirstOrDefault(g => g.Name == inGameName);
            if (gameFound == null)
                return Result.Failure<Game>("Game has not been found by its name");

            var playerFound = gameFound.Players.FirstOrDefault(player => player.Name == winnerName);
            if (playerFound == null)
                return Result.Failure<Game>("Player has not been found by its name");

            var settingWinnerResult = gameFound.SetWinner(playerFound);
            if (settingWinnerResult.IsFailure)
                return Result.Failure<Game>(settingWinnerResult.Error);

            return Result.Ok(gameFound);
        }

        public Result<(bool loggedInSuccessfully, Player loggedInPlayer)> PerformLogIn(string inGameName, string login, string passwd)
        {
            inGameName = inGameName.Trim();
            login = login.Trim().ToLower();
            passwd = passwd.Trim();

            var gameFound = this._games.FirstOrDefault(g => g.Name == inGameName);
            if (gameFound == null)
                return Result.Failure<(bool, Player)>("Game has not been found by its name");

            var playerFound = gameFound.Players.FirstOrDefault(player => player.Security.Login == login && player.Security.Password == passwd);
            return Result.Ok<(bool, Player)>((playerFound != null, playerFound));
        }

        public IReadOnlyCollection<Game> GetAllGames() =>
            this._games
                .OrderBy(game => game.Name)
                .ToList()
                .AsReadOnly();

        public IReadOnlyCollection<Game> GetPlayableGames() =>
            this._games
                .Where(game => game.State == GameState.Started)
                .OrderBy(game => game.Name)
                .ToList()
                .AsReadOnly();

        private void PrintGames()
        {
            System.Console.WriteLine($"-> -> -> -> -> -> ->[GamingCom] {this._games.Count} games found: {string.Join("\n", this._games.Select(g => g.Name))}");
        }

        private void PrintBoard(Board board)
        {
            Console.WriteLine("-> -> -> -> -> -> -> ->[GameCom] Board added: ");
            var lastLetter = "";
            board.BallsConfigured
                .OrderBy(ball => ball.Number)
                .ToList()
                .ForEach(ball => {
                    if (lastLetter != ball.Letter.ToString())
                    {
                        lastLetter = ball.Letter.ToString();
                        Console.WriteLine();
                        Console.Write($"{ball.Letter}: ");
                    }
                    Console.Write($"{ball.Number} ");
                });
            Console.WriteLine($"\n------------------- {DateTime.Now} -----------------------------");
        }
    }
}
