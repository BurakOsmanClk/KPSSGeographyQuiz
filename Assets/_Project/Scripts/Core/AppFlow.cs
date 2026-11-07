using System;
using KPSSGeography.Ads;
using KPSSGeography.Meta;
using KPSSGeography.Quiz;
using KPSSGeography.UI;
using UnityEngine;

namespace KPSSGeography.Core
{
    /// <summary>
    /// Ekranlar arası akış: Giriş → Bölümler → Oyun → Sonuç → (Tekrar | Sonraki | Bölümler).
    /// Harita her ekranda arka planda durur. Menülerde kamera vitrin modunda süzülür.
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        CityQuizGame game;
        QuizHud hud;
        TitleScreen title;
        LevelSelectScreen levels;
        LevelCatalog catalog;
        ProgressStore progress;
        AdPolicy ads;

        public void Initialize(CityQuizGame quizGame, QuizHud quizHud, TitleScreen titleScreen, LevelSelectScreen levelSelect,
            LevelCatalog levelCatalog, ProgressStore progressStore, AdPolicy adPolicy)
        {
            ads = adPolicy;
            game = quizGame;
            hud = quizHud;
            title = titleScreen;
            levels = levelSelect;
            catalog = levelCatalog;
            progress = progressStore;

            title.PlayRequested += () => ShowLevels(null);
            levels.BackRequested += ShowTitle;
            levels.PlayRequested += Play;

            hud.LevelsRequested += () => AfterResults(() => ShowLevels(game.Level));
            hud.RetryRequested += () => AfterResults(() => Play(game.Level));
            hud.NextRequested += () => AfterResults(() =>
            {
                var next = catalog.NextPlayable(game.Level);
                if (next != null && progress.IsUnlocked(next)) Play(next);
                else ShowLevels(game.Level);
            });
            hud.RequestRewarded = done => ads.ShowRewarded("continue", done);
            game.CanOfferContinue = () => ads.CanOfferContinue;
            game.GameEnded += _ => ads.OnLevelFinished();
            hud.NextAvailable = level =>
            {
                var next = catalog.NextPlayable(level);
                return next != null && progress.IsUnlocked(next);
            };

            ShowTitle();
        }

        /// <summary>Sonuç ekranından ayrılırken uygunsa geçiş reklamı gösterir (duraklatma menüsünden ayrılışta göstermez).</summary>
        void AfterResults(Action next)
        {
            if (game.State == QuizState.Finished) ads.BeforeLeavingResults(next);
            else next();
        }

        void ShowTitle()
        {
            LeaveGame();
            levels.Hide();
            title.Show();
        }

        void ShowLevels(LevelDefinition focus)
        {
            LeaveGame();
            title.Hide();
            levels.Open(focus);
        }

        void Play(LevelDefinition level)
        {
            if (level == null) return;
            title.Hide();
            levels.Hide();
            game.StartLevel(level);
        }

        void LeaveGame()
        {
            if (hud.IsVisible) hud.Hide();
            game.Abort();
        }
    }
}
