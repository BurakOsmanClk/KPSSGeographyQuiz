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
        LearnScreen learn;
        LevelCatalog catalog;
        ProgressStore progress;
        AdPolicy ads;
        ExamModeController exam;

        public void Initialize(CityQuizGame quizGame, QuizHud quizHud, TitleScreen titleScreen, LevelSelectScreen levelSelect, LearnScreen learnScreen,
            LevelCatalog levelCatalog, ProgressStore progressStore, AdPolicy adPolicy, ExamModeController examController)
        {
            ads = adPolicy;
            exam = examController;
            game = quizGame;
            hud = quizHud;
            title = titleScreen;
            levels = levelSelect;
            learn = learnScreen;
            catalog = levelCatalog;
            progress = progressStore;

            title.PlayRequested += () => ShowLevels(null);
            levels.BackRequested += ShowTitle;
            title.LearnRequested += ShowLearn;
            learn.BackRequested += ShowTitle;
            levels.PlayRequested += Play;
            // Bölüm seçiminde harita, seçili kartın sınav modunu destekleyip desteklemediğine göre renklenir
            levels.SelectionChanged += l => exam.SetLook(l != null && l.ExamActive, false, 0.6f);
            exam.Applies = () => levels.IsVisible && levels.Selected != null && levels.Selected.SupportsExam;

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
            learn.Hide();
            exam.SetLook(false, false); // giriş ekranında harita her zaman renkli
            title.Show();
        }

        void ShowLevels(LevelDefinition focus)
        {
            LeaveGame();
            title.Hide();
            learn.Hide();
            levels.Open(focus); // harita görünümünü seçili kart belirler (SelectionChanged)
        }

        void Play(LevelDefinition level)
        {
            if (level == null) return;
            title.Hide();
            levels.Hide();
            learn.Hide();
            exam.SetLook(level.ExamActive, false);
            game.StartLevel(level);
        }

        /// <summary>Öğren ekranı: süresiz konu anlatımı; harita her zaman renkli.</summary>
        void ShowLearn()
        {
            LeaveGame();
            title.Hide();
            levels.Hide();
            exam.SetLook(false, false);
            learn.Open();
        }

        void LeaveGame()
        {
            if (hud.IsVisible) hud.Hide();
            game.Abort();
        }
    }
}
